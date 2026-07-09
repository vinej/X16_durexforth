; BCALL - call a routine in any ROM/RAM bank with A/X/Y, return A/X/Y.
; The enabler for the audio ROM API and any banked-ROM access.
; ( a x y bank addr -- a' x' y' )

.bc_sp   !byte 0
.bc_rom  !byte 0
.bc_bank !byte 0
.bc_a    !byte 0
.bc_x    !byte 0
.bc_y    !byte 0

    +BACKLINK "bcall", 5
BCALL
    lda LSB, x                  ; addr -> patch the jsr target
    sta .bc_jsr+1
    lda MSB, x
    sta .bc_jsr+2
    lda LSB+4, x                ; a
    sta .bc_a
    lda LSB+3, x                ; x
    sta .bc_x
    lda LSB+2, x                ; y
    sta .bc_y
    lda LSB+1, x                ; bank
    sta .bc_bank
    stx .bc_sp                  ; save forth stack pointer
    sei                         ; kernal is banked out during the call
    lda $01
    sta .bc_rom                 ; save current ROM bank
    lda .bc_bank
    sta $01                     ; select target bank
    lda .bc_a
    ldx .bc_x
    ldy .bc_y
.bc_jsr
    jsr $0000                   ; (patched)
    sta .bc_a                   ; capture results
    stx .bc_x
    sty .bc_y
    lda .bc_rom
    sta $01                     ; restore ROM bank
    cli
    ldx .bc_sp                  ; restore forth stack pointer
    inx
    inx                         ; 5 args in, 3 results out
    lda .bc_a
    sta LSB+2, x
    stz MSB+2, x
    lda .bc_x
    sta LSB+1, x
    stz MSB+1, x
    lda .bc_y
    sta LSB, x
    stz MSB, x
    rts
