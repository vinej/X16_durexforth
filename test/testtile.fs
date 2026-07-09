\ TILE group tests: VERA layer configuration. Requires tester.fs.
\ Config regs are read straight back. MAPBASE/TILEBASE/LAYER-MODE are tested
\ on layer 0 (not shown in text mode) so the live text screen is untouched.
\ (TILE/TDATA/TATTR are covered by testvideo.)

marker ---testtile---

decimal

cr .( testtile: layer enable ) cr
T{ 0 layer-on  $9f29 c@ $10 and -> $10 }T
T{ 0 layer-off $9f29 c@ $10 and -> 0 }T
T{ 1 layer-on  $9f29 c@ $20 and -> $20 }T   \ layer 1 (text) left enabled

cr .( testtile: layer-0 config registers ) cr
T{ 0 0 $2000 mapbase  $9f2e c@ -> $10 }T    \ $2000>>9 = $10
T{ 0 1 $2000 mapbase  $9f2e c@ -> $90 }T    \ bank bit -> reg bit7
T{ 0 0 $4000 tilebase $9f2f c@ -> $20 }T    \ ($4000>>11)<<2 = $20
T{ 0 $63 layer-mode   $9f2d c@ -> $63 }T

cr .( testtile ok ) cr

---testtile---
