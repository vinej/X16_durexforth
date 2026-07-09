durexForth for the Commander X16 - prebuilt binaries
====================================================

Fastest start - nothing else needed:
    x16emu -cart durexforth_full.crt
  Boots straight into Forth with audio, graphics, VRAM disk, SEE, etc. resident.

Core cartridge - smaller; load libraries from the SD card as needed:
    x16emu -cart durexforth.crt -sdcard sdcard.img
  then e.g.   INCLUDE AUDIO      INCLUDE VRAMDISK

As a RAM program (compiles the core from the card on boot):
    x16emu -prg durexforth.prg -sdcard sdcard.img

sdcard.img is a FAT32 image with the Forth source libraries and is where EDIT
saves and INCLUDE loads your own .FS files.  On real hardware write it to an
SD card and load a .crt via a cartridge (or run the .prg).
