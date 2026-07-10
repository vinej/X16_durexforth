#!/usr/bin/env bash
# Build a standalone bootable X16 cartridge (.crt) for durexForth.
#
#   ./build-cart.sh          core-only cart (durexforth.crt)
#   ./build-cart.sh full     core + audio/vramdisk/compat/io/dos/rnd/timer
#
# The cart lives in ROM bank 32.  On power-up the KERNAL sees the "CX16"
# signature at $C000 and jumps to the boot stub (asm/cartboot.asm), which
# copies the packed durexForth turnkey image (appended right after the stub)
# down to RAM at $0801, switches to the KERNAL ROM bank, and enters durexForth
# via its SYS entry.  No SD card is required to boot.
set -euo pipefail
cd "$(dirname "$0")"

# Tool binaries (override on non-Windows / CI: ACME=acme/acme  EMU=x16emu  MAKECART=makecart)
ACME="${ACME:-acme/acme.exe}"
EMU="${EMU:-x16emu.exe}"
MAKECART="${MAKECART:-makecart.exe}"

PY="${PYTHON:-/c/Users/jyv/AppData/Local/Programs/Python/Python312/python.exe}"
IMG=release/sdcard.img
MODE="${1:-core}"
mkdir -p build

# Core forth libs base.fs pulls in, plus (for 'full') the optional feature libs.
CORE="wordlist labels doloop debug ls require open accept asm turnkey"
OPT="compat see io dos rnd timer audio loadsave vramdisk romdisk"
# On-demand modules (forth/mod/): packed into ROM bank 40+ of BOTH carts,
# loaded at runtime with NEEDS <name> (romdisk.fs, baked into both carts).
MODS="graphic float floatx file string system extras"

echo "==> assembling kernel + boot stub"
printf '!text "durexforth x16"\n' > build/version.asm
"$ACME" -I asm asm/durexforth.asm
"$ACME" asm/cartboot.asm            # -> build/cartboot.bin (64 bytes)
cp build/durexforth.prg emulator/durexforth.prg

# Bake extra libs into the saved image for a 'full' cart by inserting includes
# right before base.fs's save-pack step.
if [ "$MODE" = "full" ]; then
  BAKE='include compat\ninclude io\ninclude dos\ninclude rnd\ninclude timer\ninclude audio\ninclude loadsave\ninclude vramdisk\ninclude romdisk\n'
  sed "s/^\.( save new durexforth\.\.)/${BAKE}.( save new durexforth..)/" forth/base.fs > build/base.fs
  OUTNAME=durexforth_full.crt
else
  # the core cart still gets the on-demand module loader (NEEDS)
  BAKE='include romdisk\n'
  sed "s/^\.( save new durexforth\.\.)/${BAKE}.( save new durexforth..)/" forth/base.fs > build/base.fs
  OUTNAME=durexforth.crt
fi

echo "==> booting once to produce the packed turnkey image (durexfth)"
FILES="build/base.fs"
for n in $CORE $OPT; do FILES="$FILES forth/$n.fs"; done
"$PY" build/mkcard.py "$IMG" $FILES >/dev/null
( cd emulator && MSYS_NO_PATHCONV=1 timeout 75 "./$EMU" \
    -sdcard ../release/sdcard.img -prg durexforth.prg -run -echo -warp \
    </dev/null >/dev/null 2>&1 ) || true

echo "==> extracting durexfth image from the SD image"
"$PY" build/extract-durexfth.py "$IMG" build/durexfth.raw

echo "==> packing on-demand modules (ROM bank 40+)"
MODFILES=""; for n in $MODS; do MODFILES="$MODFILES forth/mod/$n.fs"; done
"$PY" build/mkmods.py build/mods.bin $MODFILES

echo "==> packing cartridge ($OUTNAME)"
# boot image at bank 32, zero-pad to bank 40, then the module store.
cat build/cartboot.bin build/durexfth.raw > build/cartfull.bin
IMGSZ=$(stat -c%s build/cartfull.bin)
[ "$IMGSZ" -gt 131072 ] && { echo "boot image spills past bank 39!"; exit 1; }
"$PY" -c "open('build/cartfull.bin','ab').write(b'\0'*(131072-$IMGSZ))"
cat build/mods.bin >> build/cartfull.bin
( cd emulator && MSYS_NO_PATHCONV=1 "./$MAKECART" \
    -desc "durexForth X16" -author "durexForth" -version "$MODE" \
    -fill 0 -rom_file 32 ../build/cartfull.bin -o "../build/$OUTNAME" )

STUB=$(stat -c%s build/cartboot.bin); IMGB=$(stat -c%s build/durexfth.raw)
MODB=$(stat -c%s build/mods.bin)
USED=$((STUB+IMGB)); BANKS=$(((USED+16383)/16384)); FREE=$((BANKS*16384-USED))
MBANKS=$(((MODB+16383)/16384))
echo "----------------------------------------------------------"
echo "  cart: build/$OUTNAME"
echo "  stub $STUB + image $IMGB = $USED B -> $BANKS ROM bank(s) (bank 32..$((31+BANKS))), $FREE B free in last bank"
echo "  modules $MODB B -> $MBANKS bank(s) at bank 40 (NEEDS)"
echo "----------------------------------------------------------"
