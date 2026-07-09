; SYSX - small KERNAL-wrapper primitives: I2C byte access + text charset.
; These jsr KERNAL routines that clobber .X (our Forth stack pointer), so each
; saves it with phx and restores with plx.  durexForth runs with ROM bank 0
; (KERNAL), so the $FExx / $FFxx entry points are callable directly.

    +BACKLINK "i2cpeek", 7
I2CPEEK ; ( dev reg -- byte )  i2c_read_byte $FEC6 (.X=dev .Y=reg -> .A, c=err)
    lda LSB, x                  ; reg
    tay
    lda LSB+1, x                ; dev
    phx                         ; save Forth SP
    tax                         ; .X = dev
    jsr $fec6
    plx                         ; restore Forth SP
    inx                         ; drop reg; result reuses the dev slot
    sta LSB, x
    stz MSB, x
    rts

    +BACKLINK "i2cpoke", 7
I2CPOKE ; ( dev reg val -- )  i2c_write_byte $FEC9 (.X=dev .Y=reg .A=val)
    lda LSB+1, x                ; reg
    tay
    lda LSB+2, x                ; dev
    sta W                       ; stash (tax would clobber the Forth SP too early)
    lda LSB, x                  ; val -> .A
    phx                         ; save Forth SP
    ldx W                       ; .X = dev
    jsr $fec9
    plx                         ; restore Forth SP
    inx
    inx
    inx
    rts

    +BACKLINK "charset", 7
CHARSET ; ( n -- )  screen_set_charset $FF62 (.A=charset: 1=ISO 2/3=PET .. 12=Katakana)
    lda LSB, x                  ; n
    phx                         ; save Forth SP (kernal clobbers .X/.Y)
    jsr $ff62
    plx
    inx                         ; drop n
    rts

    +BACKLINK "monitor", 7
MONITOR ; ( -- )  enter the KERNAL machine-language monitor. Does NOT return.
    jmp $fecc
