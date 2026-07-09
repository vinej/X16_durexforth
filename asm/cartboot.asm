; X16 boot cartridge stub for durexForth (multi-bank capable).
;
; The KERNAL sees "CX16" at $C000 and JMPs $C004 with interrupts disabled.
; The packed durexForth image is appended after this 256-byte stub, so it
; starts page-aligned at bank32:$C100 and, if larger than one bank, continues
; at bank33:$C000.  Because selecting a ROM bank swaps the code under $C000,
; the copy loop is relocated to low RAM ($0400) and run from there: it copies
; bank 32 ($C100-$FFFF, 63 pages) then bank 33 ($C000-$FFFF, 64 pages) down to
; RAM $0801, selects the KERNAL ROM bank, and enters durexForth at its SYS
; entry ($080D).  Copying a fixed two banks over-copies past the real image
; end into free RAM (harmless) and covers every image up to ~32 KB; a one-bank
; .crt simply reads an unpopulated bank 33 for the (discarded) tail.
!cpu 65c02
!to "build/cartboot.bin", plain

IMG_DST = $0801                 ; durexForth load address
LOADER  = $0400                 ; RAM home for the relocated copy loop (golden
                                ; RAM; below $0801 so the image copy never hits
                                ; it, and durexForth re-inits it on startup)

* = $c000
    !byte $43,$58,$31,$36        ; "CX16" signature

; --- entry at $C004: relocate the loader to RAM and run it ---
    ldx #loader_end - loader_src
.reloc
    lda loader_src-1, x
    sta LOADER-1, x
    dex
    bne .reloc
    jmp LOADER

; ---- copy loop, assembled to run at LOADER but stored here in the stub ----
loader_src
!pseudopc LOADER {
    lda #<IMG_DST
    sta $fd
    lda #>IMG_DST
    sta $fe                     ; dst = $0801
    ; --- bank 32: $C100-$FFFF (63 pages) ---
    lda #$20
    sta $01                     ; ROM bank 32
    lda #0
    sta $fb
    lda #$c1
    sta $fc                     ; src = $C100
    ldx #63
    jsr .copypages
    ; --- bank 33: $C000-$FFFF (64 pages) ---
    lda #$21
    sta $01                     ; ROM bank 33
    lda #0
    sta $fb
    lda #$c0
    sta $fc                     ; src = $C000
    ldx #64
    jsr .copypages
    stz $01                     ; KERNAL ROM bank
    cli                         ; re-enable IRQ (keyboard etc.)
    jmp IMG_DST+12              ; SYS 2061 entry ($080D)
.copypages
    ldy #0
.pl
    lda ($fb),y
    sta ($fd),y
    iny
    bne .pl
    inc $fc
    inc $fe
    dex
    bne .copypages
    rts
}
loader_end

    !fill $c100-*, $ff          ; pad stub to 256 bytes; image starts at $C100
