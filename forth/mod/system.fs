\ SYSTEM - platform and system words (SYSTEM.TXT + KERNAL.TXT SYSCALL).
\ Cart: NEEDS SYSTEM      SD card: INCLUDE SYSTEM

decimal

\ Call a KERNAL routine at addr with .A/.X/.Y in and out (durexForth runs with
\ ROM bank 0, and BCALL restores the bank afterwards either way).
\   0 0 0 $FECF SYSCALL  ( three random bytes from the entropy routine )
: syscall ( a x y addr -- a' x' y' ) 0 swap bcall ;

\ Call machine code in RAM, same register convention.
\   0 0 0 MYCODE USR
: usr ( a x y addr -- a' x' y' ) 0 swap bcall ;

\ 16-bit pseudo-random number from the KERNAL entropy source.
: random ( -- n )
  0 0 0 $fecf syscall drop $ff and swap 8 lshift or ;

: bye ( -- ) reboot ;                 \ leave Forth = cold restart

$0100 constant ver                    \ version 1.0: high byte * 256 + low
-1 constant x16                       \ platform flags
0 constant c64
0 constant f256

: free ( -- ) latest here - u. ." bytes free" cr ;
