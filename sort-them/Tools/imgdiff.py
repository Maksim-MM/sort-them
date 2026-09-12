import sys, zlib, struct
def read_png(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n'
    pos = 8; idat = b''; w = h = 0; ctype = 0
    while pos < len(data):
        ln, = struct.unpack('>I', data[pos:pos+4]); typ = data[pos+4:pos+8]; body = data[pos+8:pos+8+ln]; pos += 12 + ln
        if typ == b'IHDR': w, h, bd, ctype = struct.unpack('>IIBB', body[:10])
        elif typ == b'IDAT': idat += body
    bpp = {2: 3, 6: 4}[ctype]
    raw = zlib.decompress(idat); stride = w * bpp; out = bytearray(); prev = bytearray(stride); p = 0
    for y in range(h):
        f = raw[p]; p += 1; line = bytearray(raw[p:p+stride]); p += stride
        for i in range(stride):
            a = line[i-bpp] if i >= bpp else 0; b = prev[i]; c = prev[i-bpp] if i >= bpp else 0
            if f == 1: line[i] = (line[i] + a) & 255
            elif f == 2: line[i] = (line[i] + b) & 255
            elif f == 3: line[i] = (line[i] + (a + b) // 2) & 255
            elif f == 4:
                pa = abs(b - c); pb = abs(a - c); pc = abs(a + b - 2 * c)
                pr = a if pa <= pb and pa <= pc else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 255
        out += line; prev = line
    return w, h, bpp, bytes(out)
w1, h1, b1, d1 = read_png(sys.argv[1]); w2, h2, b2, d2 = read_png(sys.argv[2])
assert (w1, h1) == (w2, h2), 'size mismatch'
thr = int(sys.argv[3]) if len(sys.argv) > 3 else 40
n = w1 * h1; changed = 0; total = 0
for k in range(n):
    i = k * b1; j = k * b2
    d = abs(d1[i]-d2[j]) + abs(d1[i+1]-d2[j+1]) + abs(d1[i+2]-d2[j+2]); total += d
    if d > thr: changed += 1
print(f"pixels={n} changed>{thr}: {changed} ({100*changed/n:.2f}%) meanAbsDiff={total/n/3:.2f}")
