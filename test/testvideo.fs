\ VIDEO group tests (X16 VERA + KERNAL). Requires tester.fs and testcore.fs.
\ Round-trip tests only; pure side-effect words (SCREEN/CLS) are exercised
\ but not asserted (they change the display, not readable state).

marker ---testvideo---

decimal

cr .( testvideo: VRAM data port ) cr
T{ 0 $1000 65 vpoke 0 $1000 vpeek -> 65 }T
T{ 0 $1001 66 vpoke 0 $1001 vpeek -> 66 }T
\ VADDR sets the port with auto-increment; V! / V@ stream
T{ 0 $1100 vaddr 10 v! 20 v! 30 v! -> }T
T{ 0 $1100 vaddr v@ v@ v@ -> 10 20 30 }T
\ V!W writes a 16-bit word low byte first
T{ 0 $1200 vaddr $abcd v!w -> }T
T{ 0 $1200 vaddr v@ v@ -> $cd $ab }T

cr .( testvideo: colour / border ) cr
\ COLOR sets the screen colour byte (bg<<4 | fg)
T{ 1 6 color $0376 c@ -> $61 }T
\ BORDER writes VERA DC_BORDER (DCSEL 0), which we can read straight back
T{ 7 border $9f2c c@ -> 7 }T

cr .( testvideo: tile cells ) cr
\ TILE writes code+attr at (x,y); TDATA/TATTR read them back
T{ 5 3 90 12 tile 5 3 tdata 5 3 tattr -> 90 12 }T

cr .( testvideo: cursor ) cr
T{ 7 12 locate cursor -> 7 12 }T
T{ 7 12 locate pos -> 12 }T

\ Side-effect-only words: just make sure they run without crashing.
0 border

cr .( testvideo ok ) cr

---testvideo---
