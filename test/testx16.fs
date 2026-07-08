\ Regression tests specific to the Commander X16 port:
\ number parsing across bases, the relocated zero-page parameter
\ stack, the golden-RAM buffers pad and hold, and the 6502/65C02
\ inline assembler. Requires tester.fs and testcore.fs (<TRUE>).

marker ---testx16---

decimal

cr .( testx16: number parsing ) cr
T{ hex ff decimal -> 255 }T
T{ hex FF decimal -> 255 }T
T{ hex a decimal -> 10 }T
T{ hex 10 decimal -> 16 }T
T{ hex abcd decimal -> 43981 }T
T{ hex ABCD decimal -> 43981 }T
T{ $ff -> 255 }T
T{ $-1a -> -26 }T

cr .( testx16: deep zeropage stack ) cr
T{ 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 -> 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 }T
T{ 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 + + + + + + + + + + + + + + -> 120 }T
: sum1-29 0 30 1 do i + loop ;
T{ sum1-29 -> 435 }T

cr .( testx16: pad buffer ) cr
T{ pad 200 + pad u> -> <TRUE> }T
T{ 65 pad c! pad c@ -> 65 }T
T{ pad here u< -> <TRUE> }T

cr .( testx16: number formatting ) cr
: fmt 0 <# #s #> ;
T{ 12345 fmt nip -> 5 }T
T{ 12345 fmt drop c@ -> 49 }T
T{ 0 fmt nip -> 1 }T
T{ 255 fmt drop c@ -> 50 }T

cr .( testx16: double-cell math ) cr
T{ 2 3 m* -> 6 0 }T
T{ -1 1 m* -> -1 -1 }T
T{ 1000 1000 um* -> 16960 15 }T

cr .( testx16: strings ) cr
T{ s" hello" nip -> 5 }T
T{ s" hello" drop c@ -> 104 }T

cr .( testx16: inline assembler ) cr
code x2
lsb asl,x msb rol,x
rts, end-code
T{ 21 x2 -> 42 }T
T{ 100 x2 -> 200 }T
T{ -3 x2 -> -6 }T

cr .( testx16 ok ) cr

---testx16---
