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

PY="${PYTHON:-python}"
IMG=sdcard/sdcard.img

# Source files that make up the plain-Forth core. base.fs is loaded first by
# the kernel; it in turn includes wordlist/labels/doloop/debug/ls/require/open/
# accept/asm/turnkey. compat/see/io/dos/rnd/timer are optional libraries.
SRCS="base wordlist labels doloop debug ls require open accept asm turnkey \
      compat see io dos rnd timer"

echo "==> assembling kernel"
mkdir -p build
[ -f build/version.asm ] || printf '!pet "durexforth x16"\n' > build/version.asm
acme/acme.exe -I asm asm/durexforth.asm
cp durexforth.prg emulator/durexforth.prg
echo "    durexforth.prg = $(stat -c%s durexforth.prg) bytes"

echo "==> writing sources to $IMG"
FILES=""
for n in $SRCS; do FILES="$FILES forth/$n.fs"; done
"$PY" build/mkcard.py "$IMG" $FILES

if [ "${1:-}" = "run" ]; then
  echo "==> launching x16emu (close the window to exit)"
  ( cd emulator && ./x16emu.exe -sdcard ../sdcard/sdcard.img \
        -prg durexforth.prg -run )
fi
echo "==> done"
