#!/usr/bin/env bash
# Build durexForth for the Commander X16 and (optionally) boot it in x16emu.
#
#   ./build.sh          assemble + populate the SD-card image
#   ./build.sh run      the above, then launch the emulator
#
# Requirements (paths relative to the repo root):
#   acme/acme.exe       ACME assembler
#   emulator/x16emu.exe Commander X16 emulator (+ rom.bin)
#   sdcard/sdcard.img   FAT32 SD-card image
#   python              (any Python 3)
set -euo pipefail
cd "$(dirname "$0")"

# Tool binaries (override on non-Windows / CI: ACME=acme/acme  EMU=x16emu  MAKECART=makecart)
ACME="${ACME:-acme/acme.exe}"
EMU="${EMU:-x16emu.exe}"
MAKECART="${MAKECART:-makecart.exe}"

PY="${PYTHON:-python}"
IMG=release/sdcard.img

# Source files that make up the plain-Forth core. base.fs is loaded first by
# the kernel; it in turn includes wordlist/labels/doloop/debug/ls/require/open/
# accept/asm/turnkey. compat/see/io/dos/rnd/timer are optional libraries.
SRCS="base wordlist labels doloop debug ls require open accept help asm turnkey \
      compat see io dos rnd timer audio loadsave vramdisk romdisk"

echo "==> assembling kernel"
mkdir -p build
[ -f build/version.asm ] || printf '!pet "durexforth x16"\n' > build/version.asm
"$ACME" -I asm asm/durexforth.asm
cp build/durexforth.prg emulator/durexforth.prg
echo "    durexforth.prg = $(stat -c%s build/durexforth.prg) bytes"

echo "==> writing sources to $IMG"
FILES=""
for n in $SRCS; do FILES="$FILES forth/$n.fs"; done
FILES="$FILES forth/mod/graphic.fs forth/mod/float.fs forth/mod/floatx.fs forth/mod/file.fs forth/mod/string.fs forth/mod/system.fs forth/mod/extras.fs forth/mod/advanced.fs forth/mod/advgfx.fs forth/mod/bmx.fs forth/mod/advsnd.fs"  # modules also usable via INCLUDE
FILES="$FILES $(ls help/helpdoc/*.TXT)"   # pages for the HELP word
"$PY" build/mkcard.py "$IMG" $FILES

if [ "${1:-}" = "run" ]; then
  echo "==> launching x16emu (close the window to exit)"
  ( cd emulator && "./$EMU" -sdcard ../release/sdcard.img \
        -prg durexforth.prg -run )
fi
echo "==> done"
