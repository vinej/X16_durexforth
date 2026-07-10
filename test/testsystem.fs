\ SYSTEM module tests. Requires tester.fs.  BYE only gets an existence check
\ (it reboots); FREE only gets a smoke run (it prints).

marker ---testsystem---

include system

decimal

cr .( testsystem: platform flags / ver ) cr
T{ x16 c64 f256 -> -1 0 0 }T
T{ ver 0<> -> -1 }T

cr .( testsystem: syscall + random ) cr
T{ 0 0 0 $fecf syscall 2drop drop depth -> 0 }T   \ entropy call round-trips
T{ random random = random random = and random random = and -> 0 }T
T{ random drop depth -> 0 }T

cr .( testsystem: usr calls RAM machine code ) cr
create mc7 $a9 c, 7 c, $60 c,                     \ lda #7 / rts
T{ 0 0 0 mc7 usr drop drop -> 7 }T                \ a' = 7
create mc9 $a2 c, 9 c, $60 c,                     \ ldx #9 / rts
T{ 0 0 0 mc9 usr drop swap drop -> 9 }T           \ x' = 9

cr .( testsystem: free / bye exist ) cr
free
T{ ' bye 0<> -> -1 }T

cr .( testsystem ok ) cr

---testsystem---
