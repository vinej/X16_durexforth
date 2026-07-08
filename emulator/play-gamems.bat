@echo off
REM ---------------------------------------------------------------------------
REM Launch ForthX16 with GAMEMS.FTH (mouse + joystick catch-the-dots).
REM
REM Flags:
REM   -scale 2            comfortable 1280x960 window
REM   -capture            relative-motion mouse -> correct range at any size/DPI
REM   -nokeyboardcapture  let OS keys through (Alt+Tab switches window / frees mouse)
REM   -joy1               bind a gamepad to SNES port 1 (keyboard joystick still works)
REM
REM In the emulator, type  PLAY  to start.  Use Alt+Tab to leave / free the mouse.
REM ---------------------------------------------------------------------------
cd /d "%~dp0"
"%~dp0x16emu.exe" -prg forthx16.prg -run -scale 2 -capture -nokeyboardcapture -joy1
