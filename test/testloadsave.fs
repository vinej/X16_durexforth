\ LOADSAVE tests: device-aware SAVE/BLOAD, LOAD-to-header, BVLOAD, BVERIFY.
\ Requires tester.fs.

marker ---testloadsave---

include loadsave

decimal

create srcbuf 8 allot
create dstbuf 8 allot
create vbuf  10 allot
: fillsrc 8 0 do i 10 + srcbuf i + c! loop ;   \ srcbuf = 10,11,..,17
: fillvbuf                                     \ vbuf = exact LSTMP file bytes
  srcbuf 255 and vbuf c!                        \   [0] PRG header lo (= srcbuf addr)
  srcbuf 8 rshift vbuf 1 + c!                   \   [1] PRG header hi
  8 0 do i 10 + vbuf 2 + i + c! loop ;          \   [2..9] the data 10..17

cr .( testloadsave: SAVE + BLOAD round-trip ) cr
fillsrc
s" lstmp" 8 srcbuf srcbuf 8 + save             \ PRG: [addr-lo addr-hi 10..17]
s" lstmp" 8 dstbuf bload                        \ relocate back to dstbuf
T{ dstbuf c@  dstbuf 7 + c@ -> 10 17 }T
T{ srcbuf 4 + c@  dstbuf 4 + c@ -> 14 14 }T

cr .( testloadsave: LOAD to header address ) cr
0 srcbuf c!  0 srcbuf 7 + c!                    \ clobber first + last
s" lstmp" 8 load                                \ reload to header addr (= srcbuf)
T{ srcbuf c@  srcbuf 7 + c@ -> 10 17 }T

cr .( testloadsave: BVLOAD headerless -> VRAM ) cr
s" lstmp" 8 0 $8000 bvload                       \ keeps ALL bytes (header included)
0 $8002 vaddr                                    \ data sits right after the 2 header bytes
T{ v@ v@ -> 10 11 }T
0 $8000 vaddr
T{ v@ -> srcbuf 255 and }T                       \ byte 0 is the header => headerless load

cr .( testloadsave: BVERIFY ) cr
fillvbuf
T{ s" lstmp" 8 vbuf bverify -> -1 }T             \ matches memory
99 vbuf 5 + c!                                   \ corrupt one byte
T{ s" lstmp" 8 vbuf bverify -> 0 }T              \ now mismatches

cr .( testloadsave ok ) cr

---testloadsave---
