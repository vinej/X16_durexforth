; EDIT ( c-addr u -- ) - open a file in the X16 built-in editor (x16edit).
; Mirrors BASIC's EDIT (bannex/x16edit.s): filename in r0/r1L, RAM banks 10-255,
; jsrfar $C006 bank $0D (u = 0 opens a new empty buffer).
;
; x16edit clobbers zeropage up to $7F - and durexForth's split data stack lives
; at $09-$78 - so we save that range and restore it afterwards.  The editor also
; leaves the RAM-bank register at 10 (kernal calls need 0), keeps files open,
; and turns the mouse on, so we reset bank / files / mouse on the way back.
; (durexForth's rderr uses raw LISTEN/TALK, not a persistent LF 15, so the
; command-channel reopen that the x16_forth port needed does not apply here.)

JSRFAR = $ff6e
CLALL  = $ffe7
MOUSE_CONFIG = $ff68
BANK_X16EDIT = $0d

    +BACKLINK "edit", 4
EDIT
    lda LSB, x                  ; u -> r1L (0 = new empty buffer)
    sta $04
    lda LSB+1, x                ; c-addr -> r0
    sta $02
    lda MSB+1, x
    sta $03
    inx
    inx                         ; consume ( c-addr u )
    phx                         ; save the Forth stack pointer (editor trashes X)
    ldy #0                      ; save Forth's data-stack ZP $09-$78
edsave
    lda $0009, y
    sta edit_zpsave, y
    iny
    cpy #$70
    bne edsave
    stz $05                     ; editor options: all defaults
    stz $06
    stz $07
    stz $09
    stz $0a
    stz $0b
    lda #8
    sta $08                     ; device 8 (SD card)
    ldx #10                     ; first / last RAM bank the editor may use
    ldy #255
    jsr JSRFAR
    !word $c006                 ; x16edit main_loadfile_with_options
    !byte BANK_X16EDIT
    ldy #0                      ; restore Forth's data-stack ZP
edrest
    lda edit_zpsave, y
    sta $0009, y
    iny
    cpy #$70
    bne edrest
    stz $00                     ; RAM bank 0 (editor left it at 10)
    jsr CLALL                   ; close the editor's files, reset default I/O
    lda #0                      ; hide the mouse (editor turns it on)
    ldx #0
    ldy #0
    jsr MOUSE_CONFIG
    ; The editor can leave stray bytes in the keyboard buffer and a stuck
    ; modifier; durexForth reads keys with GETIN, so the next REPL line would be
    ; garbled (-> a ?STACK underflow, and a mangled INCLUDE).  Flush them, and
    ; default the current device to 8 so INCLUDE / LOADB work after EDIT (BASIC
    ; sets that on a -prg RUN, but a cart boot doesn't).  ndx/shflag are KERNAL
    ; vars in banked RAM bank 0, so select bank 0 first.
    stz $00                     ; RAM bank 0 (CLALL/MOUSE_CONFIG may have changed it)
    stz $a80a                   ; ndx = 0   (keyboard buffer count)
    stz $a80c                   ; shflag = 0 (modifier flags)
    lda #8
    sta $0292                   ; current device = 8 (SD card)
    plx                         ; restore the Forth stack pointer
    rts

edit_zpsave !fill $70, 0
