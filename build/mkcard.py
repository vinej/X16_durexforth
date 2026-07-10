#!/usr/bin/env python3
"""Populate the Commander X16 FAT32 SD-card image with the durexForth
source files (PETSCII-encoded, with a 2-byte load-address header, exactly
as the INCLUDED word expects). Existing root-directory files are removed
first. durexforth.prg itself is loaded by the emulator via -prg, not from
the card, so it is not written here.

Usage: python build/mkcard.py <sdcard.img> <file.fs> [file.fs ...]
"""
import struct, sys, os

IMG = sys.argv[1]
SRCS = sys.argv[2:]
PART_BASE = 2048 * 512  # from MBR partition entry

f = open(IMG, "r+b")

def rd(off, n):
    f.seek(off); return f.read(n)
def wr(off, data):
    f.seek(off); f.write(data)

bpb = rd(PART_BASE, 512)
bps   = struct.unpack('<H', bpb[11:13])[0]
spc   = bpb[13]
rsvd  = struct.unpack('<H', bpb[14:16])[0]
nfat  = bpb[16]
fatsz = struct.unpack('<I', bpb[36:40])[0]
rootclus = struct.unpack('<I', bpb[44:48])[0]

fat_start = PART_BASE + rsvd * bps
data_start = PART_BASE + (rsvd + nfat * fatsz) * bps
clus_bytes = spc * bps
fat_entries = fatsz * bps // 4

def clus_off(c):
    return data_start + (c - 2) * clus_bytes
def fat_get(c):
    return struct.unpack('<I', rd(fat_start + c*4, 4))[0] & 0x0FFFFFFF
def fat_set(c, val):
    val &= 0x0FFFFFFF
    for i in range(nfat):
        base = fat_start + i * fatsz * bps
        cur = struct.unpack('<I', rd(base + c*4, 4))[0]
        wr(base + c*4, struct.pack('<I', (cur & 0xF0000000) | val))
def alloc_cluster():
    for c in range(3, fat_entries):
        if fat_get(c) == 0:
            fat_set(c, 0x0FFFFFFF)
            wr(clus_off(c), b'\x00' * clus_bytes)
            return c
    raise RuntimeError("no free clusters")
def chain(c):
    out = []
    while c and c < 0x0FFFFFF8:
        out.append(c); c = fat_get(c)
    return out
def free_chain(start):
    for c in chain(start):
        fat_set(c, 0)
def root_entries():
    for c in chain(rootclus):
        base = clus_off(c)
        for i in range(clus_bytes // 32):
            off = base + i*32
            yield off, rd(off, 32)
def name83(fn):
    n, e = (fn.split('.') + [''])[:2]
    return (n.upper().ljust(8)[:8] + e.upper().ljust(3)[:3]).encode('ascii')

def fits_83(fn):
    n, dot, e = fn.partition('.')
    if not (1 <= len(n) <= 8): return False
    if dot and len(e) > 3: return False
    ok = lambda s: all(c.isalnum() or c in "_-" for c in s)
    return ok(n) and ok(e)

USED_SHORT = set()

def make_short(fn):
    """Generate a unique uppercase 8.3 short name for a long filename."""
    n, dot, e = fn.partition('.')
    ext = ''.join(c for c in e.upper() if c.isalnum())[:3]
    core = ''.join(c for c in n.upper() if c.isalnum()) or "FILE"
    for i in range(1, 100000):
        suf = "~" + str(i)
        s = (core[:8-len(suf)] + suf).ljust(8)[:8] + ext.ljust(3)[:3]
        if s not in USED_SHORT:
            USED_SHORT.add(s)
            return s.encode('ascii')
    raise RuntimeError("cannot allocate short name")

def lfn_checksum(short11):
    s = 0
    for c in short11:
        s = ((((s & 1) << 7) | (s >> 1)) + c) & 0xFF
    return s

def lfn_entries(longname, chksum):
    """Build the VFAT long-filename directory entries (on-disk order)."""
    units = [longname.encode('utf-16-le')[i:i+2]
             for i in range(0, len(longname)*2, 2)]
    units.append(b'\x00\x00')                 # NUL terminator
    while len(units) % 13:                     # pad with 0xFFFF
        units.append(b'\xff\xff')
    n = len(units) // 13
    ents = []
    for seq in range(n):
        p = units[seq*13:(seq+1)*13]
        e = bytearray(b'\xff' * 32)
        e[0] = (seq + 1) | (0x40 if seq == n-1 else 0)
        e[11] = 0x0F                           # LFN attribute
        e[12] = 0
        e[13] = chksum
        e[26:28] = b'\x00\x00'
        e[1:11]  = b''.join(p[0:5])
        e[14:26] = b''.join(p[5:11])
        e[28:32] = b''.join(p[11:13])
        ents.append(bytes(e))
    ents.reverse()                             # highest sequence stored first
    return ents

def wipe_all():
    """Remove every regular file (and its long-filename entries) from root."""
    for off, e in root_entries():
        if e[0] in (0x00, 0xE5): continue
        if e[11] & 0x10: continue          # subdirectory - leave alone
        if e[11] & 0x0F != 0x0F:           # real 8.3 entry: free its clusters
            first = (struct.unpack('<H', e[20:22])[0] << 16) | struct.unpack('<H', e[26:28])[0]
            if first >= 2:
                free_chain(first)
        wr(off, b'\xE5' + e[1:])           # tombstone (covers LFN entries too)

def extend_root():
    """Grow the root directory by one (zeroed) cluster."""
    ch = chain(rootclus)
    c = alloc_cluster()
    fat_set(ch[-1], c)

def find_free_run(n):
    """Find n consecutive free root-directory slots (growing root if full)."""
    for _ in range(2):
        run = []
        for off, e in root_entries():
            if e[0] in (0x00, 0xE5):
                run.append(off)
                if len(run) == n:
                    return run
            else:
                run = []
        extend_root()
    raise RuntimeError("no run of %d free directory slots" % n)

def add_file(fn, data):
    size = len(data)
    nclus = max(1, (size + clus_bytes - 1) // clus_bytes)
    clus = [alloc_cluster() for _ in range(nclus)]
    for i, c in enumerate(clus):
        fat_set(c, clus[i+1] if i+1 < nclus else 0x0FFFFFFF)
    for i, c in enumerate(clus):
        wr(clus_off(c), data[i*clus_bytes:(i+1)*clus_bytes])
    # Long names (> 8.3) get VFAT LFN entries + a synthesised short name;
    # short names are stored as a plain 8.3 entry (matched case-insensitively).
    if fits_83(fn):
        short, lfns = name83(fn), []
    else:
        short = make_short(fn)
        lfns = lfn_entries(fn, lfn_checksum(short))
    slots = find_free_run(len(lfns) + 1)
    for slot, ent in zip(slots, lfns):
        wr(slot, ent)
    off = slots[-1]
    ent = bytearray(32)
    ent[0:11] = short
    ent[11] = 0x20
    date = ((2026-1980) << 9) | (7 << 5) | 8
    ent[16:18] = ent[18:20] = ent[24:26] = struct.pack('<H', date)
    ent[20:22] = struct.pack('<H', (clus[0] >> 16) & 0xFFFF)
    ent[26:28] = struct.pack('<H', clus[0] & 0xFFFF)
    ent[28:32] = struct.pack('<I', size)
    wr(off, bytes(ent))
    print(f"  wrote {fn}: {size} bytes, {nclus} clus"
          + (f" (LFN, short {short.decode()!r})" if lfns else ""))

def encode_source(ascii_bytes):
    """durexForth runs in the X16 ISO (ASCII) charset, so source is kept as
    plain ASCII. Only prepend a 2-byte load-address header (skipped by the
    INCLUDED word) and normalise line endings to CR ($0d)."""
    out = bytearray(b'\x00\x00')  # load-address header (skipped by INCLUDED)
    for b in ascii_bytes:
        if b == 0x0a:            # LF -> CR
            out.append(0x0d)
        elif b == 0x0d:          # ignore CR (CRLF -> single CR)
            pass
        else:
            out.append(b)
    return bytes(out)

wipe_all()
for path in SRCS:
    with open(path, "rb") as src:
        data = encode_source(src.read())
    # forth source file names have no extension on the card
    base = os.path.splitext(os.path.basename(path))[0]
    add_file(base, data)

f.flush(); os.fsync(f.fileno()); f.close()
print("done")
