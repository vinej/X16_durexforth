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
    NEEDS ADVGFX       ( clipping, flood fill, FX copy, rotozoom - after GRAPHIC )
    NEEDS ADVANCED     ( PRNG, sin/cos/atan2/lerp, rings, ZX0 - HELP ADVANCED )
    NEEDS ADVSND       ( PSG envelopes, background PCM, ADPCM )
    NEEDS BMX          ( BMX image load/save )
    NEEDS FLOAT        ( floating point + literals  - HELP FLOAT )
    NEEDS FLOATX       ( extended float set, after FLOAT )
    NEEDS FILE         ( ANS file words + CD/DIR    - HELP FILE )
    NEEDS STRING       ( S\" C" COMPARE STR VAL ...  - HELP STRING )
    NEEDS SYSTEM       ( SYSCALL USR RANDOM BYE ...  - HELP SYSTEM )
    NEEDS EXTRAS       ( structures, FORGET, DEFER@ .. - HELP STRUCTURE )

As a RAM program (compiles the core from the card on boot):
    x16emu -prg durexforth.prg -run -sdcard sdcard.img
  NEEDS is cartridge-only; on the prg use INCLUDE GRAPHIC etc. - the same
  modules ship on the card as source files.

sdcard.img also carries the HELP pages (HELP, or HELP STRING for one topic)
and is checked at boot for an AUTORUN file to include automatically.

sdcard.img is a FAT32 image holding the Forth source libraries (AUDIO,
VRAMDISK, SEE, ...); it is also where EDIT saves and INCLUDE loads your own
.FS files.  On real hardware, write it to an SD card and insert a cartridge
programmed with a .crt (or load the .prg).

durexforth.bin / durexforth_full.bin are the same cartridges as RAW ROM
bank images (no .crt header) for the MiSTer X16 core; x16emu wants the
.crt files.  The card also carries DUREXFORTH.PRG, so on MiSTer you can
alternatively boot to BASIC and LOAD"DUREXFORTH.PRG",8 then RUN.
