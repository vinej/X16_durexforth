; IRQ - run a Forth word once per 60 Hz frame (SYSTEM.TXT).
;   ' MYWORD IRQ   arms it;   0 IRQ   disarms.
;
; Hooks CINV ($0314), which the KERNAL IRQ stub jumps through after saving
; A/X/Y, then chains to the previous handler (jiffy clock, keyboard).
; The armed word runs on the interrupted context's data stack, so it MUST be
; stack-balanced, short (well under a frame), and must not touch disk
; channels or compile.  W/W2/W3 ($9C-$A1), the Forth stack pointer and the
; RAM bank register are saved around it; a busy flag skips frames if the
; word overruns.  Disarm BEFORE forgetting the armed word.

CINV = $0314

    +BACKLINK "irq", 3
IRQ_ARM ; ( xt -- )  arm xt as the per-frame word; 0 disarms
    lda LSB, x
    ora MSB, x
    beq .disarm
    lda LSB, x
    sta irq_xt
    lda MSB, x
    sta irq_xt + 1
    inx
    lda irq_on
    bne .armed                  ; already hooked: just swap the xt
    sei
    lda CINV
    sta irq_chain + 1
    lda CINV + 1
    sta irq_chain + 2
    lda #<irq_tramp
    sta CINV
    lda #>irq_tramp
    sta CINV + 1
    lda #1
    sta irq_on
    cli
.armed
    rts

.disarm
    inx
    lda irq_on
    beq .off
    sei
    lda irq_chain + 1
    sta CINV
    lda irq_chain + 2
    sta CINV + 1
    stz irq_on
    cli
.off
    rts

irq_on   !byte 0
irq_busy !byte 0
irq_xt   !word 0
irq_zp   !fill 6, 0             ; saved W/W2/W3
irq_bank !byte 0                ; saved RAM bank

; The armed word gets its own 16-cell stack window at the deep end of the
; zp stacks: the interrupted X CANNOT be trusted as the Forth SP (um*, the
; KERNAL wrappers and others borrow X internally), and the KERNAL IRQ
; epilogue restores the real X afterwards anyway.  Keep the armed word's
; stack use under 16 cells or it reaches into deep foreground data.
X_IRQ = $d8

irq_tramp
    lda irq_busy
    bne irq_chain               ; still inside last frame's word: skip
    inc irq_busy
    ldy #5                      ; save W/W2/W3 (indexed absolute - X is not
-   lda+2 $9c, y                ; ours to use, and lda zp,y does not exist)
    sta irq_zp, y
    dey
    bpl -
    lda $00
    sta irq_bank
    lda irq_xt
    sta W
    lda irq_xt + 1
    sta W + 1
    ldx #X_IRQ                  ; private stack window for the armed word
    jsr .go                     ; execute it
    lda irq_bank
    sta $00
    ldy #5
-   lda irq_zp, y
    sta+2 $9c, y
    dey
    bpl -
    stz irq_busy
irq_chain
    jmp $ffff                   ; patched: previous CINV handler
.go
    jmp (W)
