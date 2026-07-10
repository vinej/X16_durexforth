#!/usr/bin/env bash
# Assemble durexForth, boot it in x16emu with a test bootstrap that runs the
# Forth test suite (test/test.fs), and report PASS/FAIL from the echoed output.
#
# The kernel normally loads base.fs, which ends by saving the turnkey image.
# For a test run we swap that last step for `include test`, so the suite runs
# automatically at boot with no keyboard interaction.
set -euo pipefail
cd "$(dirname "$0")"

# Tool binaries (override on non-Windows / CI: ACME=acme/acme  EMU=x16emu  MAKECART=makecart)
ACME="${ACME:-acme/acme.exe}"
EMU="${EMU:-x16emu.exe}"
MAKECART="${MAKECART:-makecart.exe}"

PY="${PYTHON:-/c/Users/jyv/AppData/Local/Programs/Python/Python312/python.exe}"
# Work on a throwaway copy of the release card so test files don't dirty it.
SEED=release/sdcard.img
IMG=build/testcard.img
TIMEOUT="${TIMEOUT:-75}"

# Forth core libraries (everything base.fs pulls in), minus base itself.
FORTH="wordlist labels doloop debug ls require open accept asm turnkey \
       compat see io dos rnd timer audio loadsave vramdisk romdisk"
# Cart ROM modules (forth/mod/): packed into build/modcart.crt for the NEEDS
# test AND written to the card so `include <mod>` works too.
MODS="graphic float floatx file"
# Test-suite files.
TESTS="tester testcore testcoreplus testcoreext testexception testx16 testvideo testsprite testtile testpalfx testinput testcoreadd testaudio testbank testvramdisk testloadsave testgraphic testromdisk testfloat testfile test 1"

echo "==> assembling kernel"
mkdir -p build
printf '!text "durexforth x16"\n' > build/version.asm
"$ACME" -I asm asm/durexforth.asm
cp build/durexforth.prg emulator/durexforth.prg

echo "==> building test bootstrap (base -> include test)"
sed 's/^save-pack durexfth$/include test/' forth/base.fs > build/base.fs

echo "==> writing sources + tests to $IMG (copy of $SEED)"
cp "$SEED" "$IMG"
FILES="build/base.fs"
for n in $FORTH; do FILES="$FILES forth/$n.fs"; done
for n in $MODS; do FILES="$FILES forth/mod/$n.fs"; done
for n in $TESTS; do FILES="$FILES test/$n.fs"; done
"$PY" build/mkcard.py "$IMG" $FILES >/dev/null

echo "==> packing module cart (ROM bank 40, non-bootable) for the NEEDS test"
MODFILES=""; for n in $MODS; do MODFILES="$MODFILES forth/mod/$n.fs"; done
"$PY" build/mkmods.py build/mods.bin $MODFILES
# pad banks 32..39 with zeros so the store sits at bank 40 exactly like the
# release carts; zeros at bank 32 $C000 = no "CX16" sig = the cart won't boot.
"$PY" -c "open('build/modpad.bin','wb').write(b'\0'*131072+open('build/mods.bin','rb').read())"
( cd emulator && MSYS_NO_PATHCONV=1 "./$MAKECART" \
    -desc "durexForth modules" -author "durexForth" -version test \
    -fill 0 -rom_file 32 ../build/modpad.bin -o ../build/modcart.crt )

echo "==> running suite in x16emu (warp, up to ${TIMEOUT}s)"
OUT=$(cd emulator && MSYS_NO_PATHCONV=1 timeout "$TIMEOUT" "./$EMU" \
        -sdcard "../$IMG" -prg durexforth.prg -run -cart ../build/modcart.crt \
        -echo -warp </dev/null 2>&1 | tr -d '\r') || true

echo "-------------------- emulator output (tail) --------------------"
echo "$OUT" | tail -25
echo "----------------------------------------------------------------"
if echo "$OUT" | grep -qi "ALL TESTS PASSED"; then
  echo "==> RESULT: PASS"
else
  echo "==> RESULT: FAIL"
  echo "$OUT" | grep -iE "incorrect result|wrong number|empty|\?$" | head -10 || true
  exit 1
fi
