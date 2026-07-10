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

cr .( testromdisk: ROM>MEM across the bank 40/41 boundary ) cr
\ Content-independent: one 16-byte read straddling the seam must equal the
\ same region read as two per-bank halves.
create bb 16 allot   create bh 16 allot
40 $3ff8 bb 16 rom>mem            \ spans the boundary in one copy
40 $3ff8 bh 8 rom>mem             \ tail of bank 40
41 0 bh 8 + 8 rom>mem             \ head of bank 41
: bb= ( -- flag ) -1 16 0 do bb i + c@ bh i + c@ <> if drop 0 leave then loop ;
T{ bb= -> -1 }T

nf0 'notfound !                   \ unhook float literals before NEEDS FILE
cr .( testromdisk: NEEDS FILE - store data crosses into ROM bank 41 ) cr
needs file                        \ first module past the 16 KB bank boundary
variable rfd
T{ s" romf" w/o create-file swap rfd ! -> 0 }T
T{ s" AB" rfd @ write-file -> 0 }T
T{ rfd @ close-file -> 0 }T
T{ s" romf" delete-file -> 0 }T

nf0 'notfound !                   \ unhook float literals before the forget

cr .( testromdisk ok ) cr

---testromdisk---
