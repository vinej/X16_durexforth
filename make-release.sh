#!/usr/bin/env bash
# Build the distributable binaries into release/ for users who don't rebuild.
#
#   release/durexforth_full.crt  bootable cartridge, everything baked in (no SD needed)
#   release/durexforth.crt       bootable cartridge, core (loads libs from the SD card)
#   release/durexforth.prg       RAM program (x16emu -prg, needs the SD card)
#   release/sdcard.img           ready-to-use FAT32 card: Forth libraries + file storage
#
# Reuses build-cart.sh (carts) and build.sh (prg + sdcard image).
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p release

echo "==> [1/3] core cartridge"
./build-cart.sh            # -> build/durexforth.crt
echo "==> [2/3] full cartridge"
./build-cart.sh full       # -> build/durexforth_full.crt
echo "==> [3/3] prg + sdcard image (repopulates release/sdcard.img with the Forth libs)"
./build.sh                 # -> durexforth.prg, release/sdcard.img

echo "==> collecting into release/"
cp build/durexforth.crt       release/durexforth.crt
cp build/durexforth_full.crt  release/durexforth_full.crt
cp build/durexforth.prg             release/durexforth.prg

cat > release/README.txt <<'EOF'
durexForth for the Commander X16 - prebuilt binaries
====================================================

Fastest start - nothing else needed:
    x16emu -cart durexforth_full.crt
  Boots straight into Forth with audio, graphics, VRAM disk, SEE, etc. all
  resident.  No SD card required.

Core cartridge - smaller; load libraries from the SD card as you need them:
    x16emu -cart durexforth.crt -sdcard sdcard.img
  then e.g.   INCLUDE AUDIO      INCLUDE VRAMDISK

Both cartridges carry on-demand modules in ROM (no SD card needed for them):
    NEEDS GRAPHIC      ( 320x240x256 bitmap drawing - HELP GRAPHIC )
    NEEDS FLOAT        ( floating point + literals  - HELP FLOAT )
    NEEDS FLOATX       ( extended float set, after FLOAT )

As a RAM program (compiles the core from the card on boot):
    x16emu -prg durexforth.prg -sdcard sdcard.img

sdcard.img is a FAT32 image holding the Forth source libraries (AUDIO,
VRAMDISK, SEE, ...); it is also where EDIT saves and INCLUDE loads your own
.FS files.  On real hardware, write it to an SD card and insert a cartridge
programmed with a .crt (or load the .prg).
EOF

echo "==> release/ ready:"
ls -la release/
