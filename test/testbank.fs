\ BANK tests: X16 8 KB high-RAM banks ($A000-$BFFF, reg $00). Requires tester.fs.
\ Bank 0 is KERNAL-reserved, so tests use banks 1..7 (emulator default = 64 banks).

marker ---testbank---

decimal

cr .( testbank: B! / B@ round-trip ) cr
T{ $42 1 100 b!    1 100 b@ -> $42 }T
T{ $7e 3 8191 b!   3 8191 b@ -> $7e }T    \ last byte of the bank-3 window

cr .( testbank: bank register $00 preserved ) cr
T{ 0 c@   $11 2 5 b!     0 c@ = -> -1 }T   \ b! restores $00
T{ 0 c@   2 5 b@ drop    0 c@ = -> -1 }T   \ b@ restores $00

cr .( testbank: SETBANK persists ) cr
T{ 7 setbank   0 c@ -> 7 }T
0 setbank                                  \ back to bank 0

cr .( testbank: MEM>BANK / BANK>MEM bulk ) cr
: fillpad 16 0 do i pad i + c! loop ;      \ pad = 0,1,..,15
fillpad
pad 5 0 16 mem>bank                        \ pad      -> bank5:0 (16 bytes)
5 0 pad 16 + 16 bank>mem                   \ bank5:0  -> pad+16
T{ pad 16 + c@   pad 31 + c@ -> 0 15 }T     \ copied back correctly
T{ pad c@        pad 15 + c@ -> 0 15 }T     \ source untouched
T{ 5 7 b@ -> 7 }T                           \ spot-check via B@

cr .( testbank: cross-bank auto-advance ) cr
: fill4 4 0 do $a0 i + pad i + c! loop ;    \ $a0,$a1,$a2,$a3
fill4
pad 2 8190 4 mem>bank                       \ spills bank2:8190,8191 -> bank3:0,1
T{ 2 8190 b@   2 8191 b@ -> $a0 $a1 }T
T{ 3 0 b@      3 1 b@ -> $a2 $a3 }T

cr .( testbank: DATABANK plausible ) cr
T{ databank 1 > -> -1 }T                    \ at least a couple of banks present

cr .( testbank ok ) cr

---testbank---
