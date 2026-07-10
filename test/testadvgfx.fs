\ ADVGFX module tests (needs GRAPHIC). Requires tester.fs.
\ Probe rows stay under y=204 so pixel addresses fit VRAM bank 0.

marker ---testadvgfx---

include graphic
include advgfx

decimal

: pix ( x y -- c ) 320 * + 0 swap vpeek ;

cr .( testadvgfx: clip-line ) cr
T{ 10 10 50 50 clip-line -> 10 10 50 50 -1 }T          \ fully inside: unchanged
T{ -400 10 -350 20 clip-line 0= nip nip nip nip -> -1 }T   \ fully left: rejected
T{ -10 100 10 100 clip-line -> 0 100 10 100 -1 }T      \ clipped at x=0
T{ 300 -20 300 20 clip-line -> 300 0 300 20 -1 }T      \ clipped at y=0
T{ 10 20 300 250 clip-rect 100 10 100 300 clip-line -> 100 20 100 250 -1 }T
0 0 319 239 clip-rect

cr .( testadvgfx: cline draws only the visible part ) cr
ginit gcls
-20 50 40 50 5 cline
T{ 0 50 pix  40 50 pix  319 50 pix -> 5 5 0 }T

cr .( testadvgfx: flood fill ) cr
gcls
20 20 60 60 3 frame                     \ hollow box outline in colour 3
40 40 7 flood                           \ fill the inside
T{ 40 40 pix  21 21 pix  59 59 pix -> 7 7 7 }T
T{ 20 40 pix  10 40 pix -> 3 0 }T       \ border kept, outside untouched
70 70 9 flood                           \ seed on the outside: fills the screen
T{ 0 0 pix  200 100 pix  10 40 pix -> 9 9 9 }T
T{ 40 40 pix -> 7 }T                    \ ...but not the earlier region

cr .( testadvgfx: fx-copy ) cr
gcls
0 100 8 pset  1 100 8 pset  2 100 8 pset  3 100 8 pset
8 100 4 pset  9 100 4 pset
\ copy 10 bytes of row 100 (offset 32000, 4-aligned) to row 200 (64000):
\ 2 cache quads + a 2-byte plain tail
0 32000 0 64000 10 fx-copy
T{ 0 200 pix  3 200 pix  8 200 pix  9 200 pix -> 8 8 4 4 }T
T{ 10 200 pix -> 0 }T
\ unaligned length tail (5 = 1 quad + 1 single)
0 32000 0 64320 5 fx-copy
T{ 0 201 pix  3 201 pix  4 201 pix -> 8 8 0 }T

cr .( testadvgfx ok ) cr

---testadvgfx---
