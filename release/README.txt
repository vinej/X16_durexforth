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
    NEEDS FILE         ( ANS file words + CD/DIR    - HELP FILE )
    NEEDS STRING       ( S\" C" COMPARE STR VAL ...  - HELP STRING )
    NEEDS SYSTEM       ( SYSCALL USR RANDOM BYE ...  - HELP SYSTEM )
    NEEDS EXTRAS       ( structures, FORGET, DEFER@ .. - HELP STRUCTURE )

As a RAM program (compiles the core from the card on boot):
    x16emu -prg durexforth.prg -sdcard sdcard.img

sdcard.img is a FAT32 image holding the Forth source libraries (AUDIO,
VRAMDISK, SEE, ...); it is also where EDIT saves and INCLUDE loads your own
.FS files.  On real hardware, write it to an SD card and insert a cartridge
programmed with a .crt (or load the .prg).
