; BANK - use the X16 8 KB high-RAM banks ($A000-$BFFF window, bank reg $00).
; SETBANK B@ B! BANK>MEM MEM>BANK DATABANK
;
; The KERNAL IRQ routines that touch bank 0 (keyboard tables, cursor) save and
; restore the RAM-bank register themselves, so user code may leave $00 pointing
; at any bank - no SEI needed here.  B@/B!/BANK>MEM/MEM>BANK preserve $00;
; SETBANK deliberately changes it.  'off' is 0..8191 into the $A000 window;
; the bulk copies auto-advance across bank boundaries ($BFFF -> next bank $A000).

RAM_BANK = 0                    ; zeropage RAM-bank register

    +BACKLINK "setbank", 7
SETBANK ; ( bank -- ) select the RAM bank visible at $A000-$BFFF
    lda LSB, x
    sta RAM_BANK
    inx
    rts

    +BACKLINK "b@", 2
B_FETCH ; ( bank off -- byte )
    lda RAM_BANK
    pha                         ; save current bank
    lda LSB+1, x                ; bank -> select
    sta RAM_BANK
    lda LSB, x                  ; off low  = addr low
    sta W
    lda MSB, x                  ; off high + $A0 = addr high
    clc
    adc #$a0
    sta W+1
    ldy #0
    lda (W), y                  ; read banked byte
    tay                         ; stash (pla clobbers A)
    pla
    sta RAM_BANK                ; restore bank
    inx                         ; drop off; TOS slot reused for result
    tya
    sta LSB, x
    stz MSB, x
    rts

    +BACKLINK "b!", 2
B_STORE ; ( byte bank off -- )
    lda RAM_BANK
    pha
    lda LSB+1, x                ; bank
    sta RAM_BANK
    lda LSB, x                  ; off low
    sta W
    lda MSB, x
    clc
    adc #$a0                    ; off high + $A0
    sta W+1
    lda LSB+2, x                ; byte
    ldy #0
    sta (W), y
    pla
    sta RAM_BANK
    inx
    inx
    inx
    rts

    +BACKLINK "bank>mem", 8
BANK_TO_MEM ; ( bank boff addr u -- ) copy u bytes bank:boff -> low RAM addr
    ; stack: u=+0  addr=+1  boff=+2  bank=+3
    lda RAM_BANK
    pha
    lda LSB+3, x                ; bank
    sta RAM_BANK
    lda LSB+2, x                ; src W = $A000 + boff
    sta W
    lda MSB+2, x
    clc
    adc #$a0
    sta W+1
    lda LSB+1, x                ; dst W2 = addr
    sta W2
    lda MSB+1, x
    sta W2+1
    ldy #0
b2m_lp
    lda LSB, x                  ; count == 0 ?
    ora MSB, x
    beq b2m_end
    lda (W), y                  ; banked -> low RAM
    sta (W2), y
    inc W                       ; advance src, wrap $BFFF->next bank $A000
    bne b2m_sok
    inc W+1
    lda W+1
    cmp #$c0
    bne b2m_sok
    lda #$a0
    sta W+1
    inc RAM_BANK
b2m_sok
    inc W2                      ; advance dst
    bne b2m_dok
    inc W2+1
b2m_dok
    lda LSB, x                  ; count-- (16-bit)
    bne b2m_nb
    dec MSB, x
b2m_nb
    dec LSB, x
    bra b2m_lp
b2m_end
    pla
    sta RAM_BANK
    inx
    inx
    inx
    inx
    rts

    +BACKLINK "mem>bank", 8
MEM_TO_BANK ; ( addr bank boff u -- ) copy u bytes low RAM addr -> bank:boff
    ; stack: u=+0  boff=+1  bank=+2  addr=+3
    lda RAM_BANK
    pha
    lda LSB+2, x                ; bank
    sta RAM_BANK
    lda LSB+1, x                ; dst W = $A000 + boff
    sta W
    lda MSB+1, x
    clc
    adc #$a0
    sta W+1
    lda LSB+3, x                ; src W2 = addr
    sta W2
    lda MSB+3, x
    sta W2+1
    ldy #0
m2b_lp
    lda LSB, x
    ora MSB, x
    beq m2b_end
    lda (W2), y                 ; low RAM -> banked
    sta (W), y
    inc W                       ; advance dst, wrap across bank boundary
    bne m2b_dok
    inc W+1
    lda W+1
    cmp #$c0
    bne m2b_dok
    lda #$a0
    sta W+1
    inc RAM_BANK
m2b_dok
    inc W2                      ; advance src
    bne m2b_sok
    inc W2+1
m2b_sok
    lda LSB, x
    bne m2b_nb
    dec MSB, x
m2b_nb
    dec LSB, x
    bra m2b_lp
m2b_end
    pla
    sta RAM_BANK
    inx
    inx
    inx
    inx
    rts

    +BACKLINK "databank", 8
DATABANK ; ( -- bank ) highest RAM bank present (dictionary is in low RAM, so
         ; every bank 1..this is free for data). Via KERNAL MEMTOP ($FF99).
    phx                         ; MEMTOP trashes X/Y; X is the Forth SP
    sec
    jsr $ff99                   ; .A = number of RAM banks (0 => 256)
    plx
    dec                         ; highest bank = count-1 (0 -> 255)
    dex
    sta LSB, x
    stz MSB, x
    rts
