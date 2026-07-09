\ ROMDISK tests: NEEDS loads a module from cart ROM bank 40 (the suite attaches
\ build/modcart.crt, a non-bootable cart holding only the module store).
\ Requires tester.fs.

marker ---testromdisk---
'notfound @ constant nf0          \ NEEDS FLOAT hooks it; restore before forget

include romdisk

decimal

cr .( testromdisk: raw ROM>MEM probe ) cr
create tb 16 allot
40 0 tb 16 rom>mem                \ read the module directory header
T{ tb c@ -> 7 }T                  \ name length of "graphic"

cr .( testromdisk: NEEDS GRAPHIC from cart ROM ) cr
needs graphic                     \ scans bank 40, stages in DATABANK, evaluates
ginit gcls
12 34 8 pset
T{ 0 34 320 * 12 + vpeek -> 8 }T  \ a module-defined word works
120 40 150 60 3 rect
T{ 0 50 320 * 130 + vpeek -> 3 }T
0 screen page

cr .( testromdisk: NEEDS FLOAT - second directory entry ) cr
needs float                       \ exercises the entry-skip in the scanner
T{ 2 s>f 3 s>f f+ f>s -> 5 }T

nf0 'notfound !                   \ unhook float literals before the forget

cr .( testromdisk ok ) cr

---testromdisk---
