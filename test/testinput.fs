\ INPUT group tests. Requires tester.fs.
\ Headless: no live gamepad/mouse, so we can only assert an absent gamepad
\ reads 0, and that MOUSE config runs without crashing.

marker ---testinput---

decimal

cr .( testinput: joystick ) cr
T{ 1 joy -> 0 }T        \ gamepad 1 not attached -> 0

cr .( testinput: mouse ) cr
1 mouse                 \ enable the pointer
T{ mb -> 0 }T           \ no buttons pressed
T{ mx -> 0 }T           \ pointer at origin (headless)
T{ my -> 0 }T
mwheel drop             \ runs
0 mouse                 \ off

cr .( testinput ok ) cr

---testinput---
