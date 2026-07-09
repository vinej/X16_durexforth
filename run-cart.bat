@echo off
REM ---------------------------------------------------------------------------
REM  Launch durexForth from the cartridge (ROM bank 32) with the SD card mounted.
REM  Boots straight into Forth; the SD card is read/write so EDIT can save files
REM  and you can INCLUDE them.  Use this to test interactively (e.g. the EDIT word:
REM     S" PROG.FS" EDIT      ( write code, Ctrl-S to save, Ctrl-Q to quit )
REM     S" PROG.FS" INCLUDED  ( compile it )
REM
REM  Rebuild the .crt first (in Git Bash) if you changed the sources:
REM     ./build-cart.sh          ( core cart )   or   ./build-cart.sh full
REM ---------------------------------------------------------------------------
setlocal
set HERE=%~dp0
set CRT=%HERE%build\durexforth.crt
set IMG=%HERE%release\sdcard.img

if not exist "%CRT%" (
    echo Cartridge not found: %CRT%
    echo Build it first from Git Bash:  ./build-cart.sh
    pause
    exit /b 1
)
if not exist "%IMG%" (
    echo SD card image not found: %IMG%
    pause
    exit /b 1
)

REM x16emu looks for rom.bin in its own folder, so run from there.
cd /d "%HERE%emulator"
echo Launching durexForth cart (bank 32) with SD card...
x16emu.exe -cart "%CRT%" -sdcard "%IMG%" %*

endlocal
