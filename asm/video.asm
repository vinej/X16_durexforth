; VIDEO - X16 video / screen / cursor (VERA + KERNAL)
; VPOKE VPEEK VADDR V! V@ V!W SCREEN COLOR BORDER CLS
; LOCATE CURSOR POS SCROLLX SCROLLY TILE TDATA TATTR

; VERA registers
VERA_ADDR_L = $9f20
VERA_ADDR_M = $9f21
VERA_ADDR_H = $9f22
VERA_DATA0  = $9f23
VERA_CTRL   = $9f25
VERA_DC_BORDER = $9f2c
VERA_L1_HSCROLL_L = $9f37
VERA_L1_HSCROLL_H = $9f38
VERA_L1_VSCROLL_L = $9f39
VERA_L1_VSCROLL_H = $9f3a
SCREEN_COLOR = $0376
K_PLOT      = $fff0
K_SCREEN_MODE = $ff5f

    +BACKLINK "vpoke", 5
VPOKE ; ( bank addr value -- )
    stz VERA_CTRL       ; ADDRSEL 0
    lda LSB+1, x        ; addr lo
    sta VERA_ADDR_L
    lda MSB+1, x        ; addr hi
    sta VERA_ADDR_M
    lda LSB+2, x        ; bank
    and #1
    sta VERA_ADDR_H     ; no auto-increment
    lda LSB, x          ; value
    sta VERA_DATA0
    inx
    inx
    inx
    rts

    +BACKLINK "vpeek", 5
VPEEK ; ( bank addr -- value )
    stz VERA_CTRL
    lda LSB, x          ; addr lo
    sta VERA_ADDR_L
    lda MSB, x          ; addr hi
    sta VERA_ADDR_M
    lda LSB+1, x        ; bank
    and #1
    sta VERA_ADDR_H
    inx                 ; drop addr
    lda VERA_DATA0
    sta LSB, x
    stz MSB, x
    rts

    +BACKLINK "vaddr", 5
VADDR ; ( bank addr -- ) point data port at VRAM, auto-increment 1
    stz VERA_CTRL
    lda LSB, x
    sta VERA_ADDR_L
    lda MSB, x
    sta VERA_ADDR_M
    lda LSB+1, x        ; bank
    and #1
    ora #$10            ; auto-increment 1
    sta VERA_ADDR_H
    inx
    inx
    rts

    +BACKLINK "v!", 2
V_STORE ; ( byte -- )
    lda LSB, x
    sta VERA_DATA0
    inx
    rts

    +BACKLINK "v@", 2
V_FETCH ; ( -- byte )
    dex
    lda VERA_DATA0
    sta LSB, x
    stz MSB, x
    rts

    +BACKLINK "v!w", 3
V_STOREW ; ( w -- ) low byte first
    lda LSB, x
    sta VERA_DATA0
    lda MSB, x
    sta VERA_DATA0
    inx
    rts

    +BACKLINK "screen", 6
SCREEN ; ( mode -- )
    lda LSB, x
    stx W
    clc                 ; carry clear = set mode
    jsr K_SCREEN_MODE
    ldx W
    inx
    rts

    +BACKLINK "color", 5
COLOR ; ( fg bg -- )
    lda LSB, x          ; bg
    asl
    asl
    asl
    asl
    ora LSB+1, x        ; | fg
    sta SCREEN_COLOR
    inx
    inx
    rts

    +BACKLINK "border", 6
BORDER ; ( color -- )
    stz VERA_CTRL       ; DCSEL 0
    lda LSB, x
    sta VERA_DC_BORDER
    inx
    rts

    +BACKLINK "cls", 3
CLS ; ( -- )
    lda #$93
    jmp PUTCHR

    +BACKLINK "locate", 6
LOCATE ; ( row col -- )
    lda LSB, x          ; col
    tay
    lda LSB+1, x        ; row
    stx W
    tax
    clc                 ; set cursor
    jsr K_PLOT
    ldx W
    inx
    inx
    rts

    +BACKLINK "cursor", 6
CURSOR ; ( -- row col )
    stx W
    sec                 ; read cursor
    jsr K_PLOT          ; x=row y=col
    stx W2
    ldx W
    dex
    dex
    tya                 ; col
    sta LSB, x
    stz MSB, x
    lda W2              ; row
    sta LSB+1, x
    stz MSB+1, x
    rts

    +BACKLINK "pos", 3
POS ; ( -- col )
    stx W
    sec
    jsr K_PLOT          ; y=col
    ldx W
    dex
    sty LSB, x
    stz MSB, x
    rts

    +BACKLINK "scrollx", 7
SCROLLX ; ( n -- )
    lda LSB, x
    sta VERA_L1_HSCROLL_L
    lda MSB, x
    and #$0f
    sta VERA_L1_HSCROLL_H
    inx
    rts

    +BACKLINK "scrolly", 7
SCROLLY ; ( n -- )
    lda LSB, x
    sta VERA_L1_VSCROLL_L
    lda MSB, x
    and #$0f
    sta VERA_L1_VSCROLL_H
    inx
    rts

; Text tilemap helpers. Default 80x60 mode: 128-wide map at VRAM $1b000,
; two bytes/cell (code, attribute). Address = $b000 + y*256 + x*2, bank 1.
    +BACKLINK "tile", 4
TILE ; ( x y code attr -- )
    lda LSB+3, x        ; x
    asl                 ; x*2
    sta VERA_ADDR_L
    lda LSB+2, x        ; y
    clc
    adc #$b0
    sta VERA_ADDR_M
    lda #$11            ; bank 1 + auto-increment 1
    sta VERA_ADDR_H
    lda LSB+1, x        ; code
    sta VERA_DATA0
    lda LSB, x          ; attr
    sta VERA_DATA0
    inx
    inx
    inx
    inx
    rts

    +BACKLINK "tdata", 5
TDATA ; ( x y -- code )
    lda LSB+1, x        ; x
    asl
    sta VERA_ADDR_L
    lda LSB, x          ; y
    clc
    adc #$b0
    sta VERA_ADDR_M
    lda #$01            ; bank 1, no increment
    sta VERA_ADDR_H
    inx
    lda VERA_DATA0
    sta LSB, x
    stz MSB, x
    rts

    +BACKLINK "tattr", 5
TATTR ; ( x y -- attr )
    lda LSB+1, x        ; x
    asl
    ora #1              ; attr byte at x*2+1
    sta VERA_ADDR_L
    lda LSB, x          ; y
    clc
    adc #$b0
    sta VERA_ADDR_M
    lda #$01
    sta VERA_ADDR_H
    inx
    lda VERA_DATA0
    sta LSB, x
    stz MSB, x
    rts
