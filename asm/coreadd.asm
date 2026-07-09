; Core additions: small primitives requested for the X16 build.
; 2- SGN CATNIB SBIT CBIT FBIT ROLL 2ROT SLEEP MS REBOOT TICKS
; (TIME@ DATE@ SETTIME deferred: need the clock_get/set_date_time ABI.)

    +BACKLINK "2-", 2
TWO_MINUS ; ( n -- n-2 )
    sec
    lda LSB, x
    sbc #2
    sta LSB, x
    lda MSB, x
    sbc #0
    sta MSB, x
    rts

    +BACKLINK "sgn", 3
SGN ; ( n -- -1|0|1 )
    lda LSB, x
    ora MSB, x
    beq +                       ; zero -> leave 0
    lda MSB, x
    bmi ++                      ; negative
    lda #1                      ; positive -> 1
    sta LSB, x
    stz MSB, x
    rts
++  lda #$ff                    ; negative -> -1
    sta LSB, x
    sta MSB, x
+   rts

    +BACKLINK "catnib", 6
CATNIB ; ( nh nl -- byte ) (nh<<4) | nl
    lda LSB, x                  ; nl
    and #$0f
    sta W
    lda LSB+1, x                ; nh
    asl
    asl
    asl
    asl
    ora W
    inx
    sta LSB, x
    stz MSB, x
    rts

    +BACKLINK "sbit", 4
SBIT ; ( addr mask -- ) set the masked bits at addr
    lda LSB+1, x
    sta W
    lda MSB+1, x
    sta W+1
    lda LSB, x                  ; mask
    ldy #0
    ora (W), y
    sta (W), y
    inx
    inx
    rts

    +BACKLINK "cbit", 4
CBIT ; ( addr mask -- ) clear the masked bits at addr
    lda LSB+1, x
    sta W
    lda MSB+1, x
    sta W+1
    lda LSB, x                  ; mask
    eor #$ff
    ldy #0
    and (W), y
    sta (W), y
    inx
    inx
    rts

    +BACKLINK "fbit", 4
FBIT ; ( flag addr mask -- ) set masked bits if flag, else clear
    lda LSB+1, x
    sta W
    lda MSB+1, x
    sta W+1
    lda LSB+2, x                ; flag
    ora MSB+2, x
    beq +                       ; false -> clear
    lda LSB, x                  ; mask
    ldy #0
    ora (W), y
    sta (W), y
    bra ++
+   lda LSB, x
    eor #$ff
    ldy #0
    and (W), y
    sta (W), y
++  inx
    inx
    inx
    rts

; ROLL is deferred to a Forth definition: the split ZP stack is indexed with
; zp,X (which has no zp,Y counterpart for LDA), so variable-depth access is
; awkward in assembly.  ( : roll ?dup if swap >r 1- recurse r> swap then ; )

    +BACKLINK "2rot", 4
TWO_ROT ; ( a b c d e f -- c d e f a b )
    lda LSB+5, x
    sta W
    lda MSB+5, x
    sta W+1
    lda LSB+4, x
    sta W2
    lda MSB+4, x
    sta W3
    lda LSB+3, x
    sta LSB+5, x
    lda MSB+3, x
    sta MSB+5, x
    lda LSB+2, x
    sta LSB+4, x
    lda MSB+2, x
    sta MSB+4, x
    lda LSB+1, x
    sta LSB+3, x
    lda MSB+1, x
    sta MSB+3, x
    lda LSB, x
    sta LSB+2, x
    lda MSB, x
    sta MSB+2, x
    lda W
    sta LSB+1, x
    lda W+1
    sta MSB+1, x
    lda W2
    sta LSB, x
    lda W3
    sta MSB, x
    rts

    +BACKLINK "sleep", 5
SLEEP ; ( jiffies -- ) wait jiffies 1/60s ticks
    stx W3
    jsr $ffde                   ; RDTIM: A=lo X=mid Y=hi
    sta W                       ; start lo
    stx W+1                     ; start mid
    ldx W3
-   stx W3
    jsr $ffde
    sec
    sbc W                       ; elapsed lo
    sta W2
    txa
    sbc W+1                     ; elapsed hi
    ldx W3
    cmp MSB, x                  ; elapsed vs jiffies (hi)
    bcc -
    bne +
    lda W2
    cmp LSB, x                  ; elapsed vs jiffies (lo)
    bcc -
+   inx
    rts

    +BACKLINK "ms", 2
MS ; ( u -- ) wait ~u milliseconds (calibrated 8 MHz busy loop)
-   lda LSB, x
    ora MSB, x
    beq +++                     ; u = 0 -> done
    lda LSB, x
    sec
    sbc #1
    sta LSB, x
    lda MSB, x
    sbc #0
    sta MSB, x
    lda #8
    sta W
--  ldy #200
--- dey
    bne ---
    dec W
    bne --
    bra -
+++ inx
    rts

    +BACKLINK "reboot", 6
REBOOT ; ( -- ) soft reboot through the reset vector
    jmp ($fffc)

    +BACKLINK "ticks", 5
TICKS ; ( -- ud ) 24-bit jiffy counter as an unsigned double
    stx W3
    jsr $ffde                   ; A=lo X=mid Y=hi
    sta W
    stx W+1
    sty W2
    ldx W3
    dex
    dex
    lda W
    sta LSB+1, x                ; low cell lo
    lda W+1
    sta MSB+1, x                ; low cell hi
    lda W2
    sta LSB, x                  ; high cell lo (bits 16-23)
    stz MSB, x                  ; high cell hi = 0
    rts
