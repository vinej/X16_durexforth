import struct, sys, os

IMG = r"c:\quartus\projects\x16_forth\emulator\sdcard.img"
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

print(f"bps={bps} spc={spc} rsvd={rsvd} nfat={nfat} fatsz={fatsz} rootclus={rootclus}")
print(f"fat_start={fat_start} data_start={data_start} clus_bytes={clus_bytes} fat_entries={fat_entries}")

def clus_off(c):
    return data_start + (c - 2) * clus_bytes

def fat_get(c):
    return struct.unpack('<I', rd(fat_start + c*4, 4))[0] & 0x0FFFFFFF

def fat_set(c, val):
    val &= 0x0FFFFFFF
    for i in range(nfat):
        base = fat_start + i * fatsz * bps
        cur = struct.unpack('<I', rd(base + c*4, 4))[0]
        new = (cur & 0xF0000000) | val
        wr(base + c*4, struct.pack('<I', new))

def alloc_cluster():
    for c in range(3, fat_entries):
        if fat_get(c) == 0:
            fat_set(c, 0x0FFFFFFF)
            # zero the cluster
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
    """yield (abs_offset, entry_bytes) over every 32-byte slot in the root dir chain."""
    for c in chain(rootclus):
        base = clus_off(c)
        for i in range(clus_bytes // 32):
            off = base + i*32
            yield off, rd(off, 32)

def name83(fn):
    n, e = (fn.split('.') + [''])[:2]
    return (n.upper().ljust(8)[:8] + e.upper().ljust(3)[:3]).encode('ascii')

def delete_existing(fn):
    tag = name83(fn)
    for off, e in root_entries():
        if e[0] in (0x00, 0xE5): continue
        if e[11] & 0x0F == 0x0F: continue  # LFN
        if e[0:11] == tag:
            first = (struct.unpack('<H', e[20:22])[0] << 16) | struct.unpack('<H', e[26:28])[0]
            if first >= 2:
                free_chain(first)
            wr(off, b'\xE5' + e[1:])
            print(f"  deleted existing {fn}")

def find_free_slot():
    for off, e in root_entries():
        if e[0] in (0x00, 0xE5):
            return off
    raise RuntimeError("root dir full (extension not implemented)")

def add_file(fn, data):
    delete_existing(fn)
    size = len(data)
    # allocate + write clusters
    nclus = max(1, (size + clus_bytes - 1) // clus_bytes)
    clus = [alloc_cluster() for _ in range(nclus)]
    for i, c in enumerate(clus):
        fat_set(c, clus[i+1] if i+1 < nclus else 0x0FFFFFFF)
    for i, c in enumerate(clus):
        part = data[i*clus_bytes:(i+1)*clus_bytes]
        wr(clus_off(c), part)  # tail already zero-padded (cluster was zeroed)
    first = clus[0]
    off = find_free_slot()
    ent = bytearray(32)
    ent[0:11] = name83(fn)
    ent[11] = 0x20  # archive
    date = ((2024-1980) << 9) | (7 << 5) | 4  # 2024-07-04
    ent[16:18] = struct.pack('<H', date)  # create date
    ent[18:20] = struct.pack('<H', date)  # access date
    ent[24:26] = struct.pack('<H', date)  # write date
    ent[20:22] = struct.pack('<H', (first >> 16) & 0xFFFF)
    ent[26:28] = struct.pack('<H', first & 0xFFFF)
    ent[28:32] = struct.pack('<I', size)
    wr(off, bytes(ent))
    print(f"  wrote {fn}: {size} bytes, {nclus} clus, first={first}")

EMU = r"c:\quartus\projects\x16_forth\emulator"
files = ["TK.DIC", "TK.TOK", "TK.VAR", "AUTORUN.FTH"]
for fn in files:
    data = open(os.path.join(EMU, fn), "rb").read()
    add_file(fn, data)

f.flush(); os.fsync(f.fileno()); f.close()
print("done")
