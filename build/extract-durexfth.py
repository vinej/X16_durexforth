#!/usr/bin/env python3
# Extract the saved "durexfth" turnkey image from a FAT32 SD-card image and
# write its raw bytes (load-address header stripped) to an output file.
#   python extract-durexfth.py <sdcard.img> <out.raw>
import struct, sys

imgpath, outpath = sys.argv[1], sys.argv[2]
PB = 2048 * 512                      # MBR partition base
b = open(imgpath, 'rb').read()[PB:]
bps = struct.unpack('<H', b[11:13])[0]; spc = b[13]
rsvd = struct.unpack('<H', b[14:16])[0]; nfat = b[16]
spf = struct.unpack('<I', b[36:40])[0]; rc = struct.unpack('<I', b[44:48])[0]
fs = rsvd * bps; ds = (rsvd + nfat * spf) * bps
co = lambda c: ds + (c - 2) * spc * bps

def nx(c):
    o = fs + c * 4
    return struct.unpack('<I', b[o:o + 4])[0] & 0x0FFFFFFF

def ch(c):
    d = b''
    while 2 <= c < 0x0FFFFFF8:
        d += b[co(c):co(c) + spc * bps]; c = nx(c)
    return d

root = ch(rc); lfn = ''
for i in range(0, len(root), 32):
    e = root[i:i + 32]
    if e[0] == 0: break
    if e[0] == 0xE5: continue
    if e[11] & 0x0F == 0x0F:
        lfn = (e[1:11] + e[14:26] + e[28:32]).decode('utf-16-le', 'ignore').split('\x00')[0] + lfn
        continue
    if lfn.lower() == 'durexfth':
        clo = struct.unpack('<H', e[26:28])[0]; chi = struct.unpack('<H', e[20:22])[0]
        size = struct.unpack('<I', e[28:32])[0]
        open(outpath, 'wb').write(ch((chi << 16) | clo)[2:size])   # strip 2-byte load addr
        print(f"    image = {size - 2} bytes")
        sys.exit(0)
    lfn = ''
sys.exit("durexfth not found on the card")
