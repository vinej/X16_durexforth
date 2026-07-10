\ Channel words. The Forth stack pointer is saved on the hardware stack,
\ NOT in zeropage: the X16 kernal's channel calls can far-call into CBDOS
\ and clobber zp workspace (the C64-era "w stx," pattern crashed).

\ Use logical file as input device
\ ioresult is 0 on success, kernal
\ error # on failure.
code chkin ( file# -- ioresult )
txa, pha,
lsb lda,x tax, \ x = file#
$ffc6 jsr, \ CHKIN
+branch bcs, \ carry set = error
0 lda,# \ A is only valid on error
:+
tay, pla, tax, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

\ Use logical file as output device
\ ioresult is 0 on success, kernal
\ error # on failure.
code chkout ( file# -- ioresult )
txa, pha,
lsb lda,x tax, \ x = file#
$ffc9 jsr, \ CHKOUT
+branch bcs, \ carry set = error
0 lda,# \ A is only valid on error
:+
tay, pla, tax, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

\ Reset input and output to console
code clrchn ( -- )
txa, pha,
$ffcc jsr,  \ CLRCH
pla, tax,
rts, end-code

\ Read status of last IO operation
code readst ( -- status )
txa, pha,
$ffb7 jsr, \ READST
tay, pla, tax, dex, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

\ Get a byte from input device
code chrin ( -- chr )
txa, pha,
$ffcf jsr, \ CHRIN
tay, pla, tax, dex, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code
