; ROMDISK - read cartridge ROM banks (the on-demand module store).
; ROM>MEM
;
; Cart ROM banks (32+) appear in the $C000-$FFFF window selected by zp $01.
; Unlike the KERNAL/BASIC banks, a DATA bank has no interrupt vectors at
; $FFFA-$FFFF, so an IRQ fired while one is selected jumps into garbage.
; ROM>MEM therefore runs the whole copy under SEI and calls no KERNAL code.
; Worst case (8 KB module) keeps IRQs off for ~30 ms - a dropped jiffy or two.

ROM_BANK_REG = 1                ; zeropage ROM-bank register

    +BACKLINK "rom>mem", 7
ROM_TO_MEM ; ( rombank off dst u -- ) copy u bytes from cart ROM bank:off
           ; (off = 0..$3FFF into the $C000 window) to low RAM dst.
           ; The copy runs across bank boundaries ($FFFF -> next bank $C000).
    ; stack: u=+0  dst=+1  off=+2  rombank=+3
    sei
    lda ROM_BANK_REG
    pha                         ; save current ROM bank (0 = KERNAL)
    lda LSB+3, x                ; rombank
    sta ROM_BANK_REG
    lda LSB+2, x                ; src W = $C000 + off
    sta W
    lda MSB+2, x
    clc
    adc #$c0
    sta W+1
    lda LSB+1, x                ; dst W2
    sta W2
    lda MSB+1, x
    sta W2+1
    ldy #0
r2m_lp
    lda LSB, x                  ; count == 0 ?
    ora MSB, x
    beq r2m_end
    lda (W), y                  ; cart ROM -> low RAM
    sta (W2), y
    inc W                       ; advance src, wrap $FFFF -> next bank $C000
    bne r2m_sok
    inc W+1
    bne r2m_sok                 ; W+1 rolled $FF -> $00: past the window
    lda #$c0
    sta W+1
    inc ROM_BANK_REG
r2m_sok
    inc W2                      ; advance dst
    bne r2m_dok
    inc W2+1
r2m_dok
    lda LSB, x                  ; count-- (16-bit)
    bne r2m_nb
    dec MSB, x
r2m_nb
    dec LSB, x
    bra r2m_lp
r2m_end
    pla
    sta ROM_BANK_REG            ; restore ROM bank
    cli
    inx
    inx
    inx
    inx
    rts
