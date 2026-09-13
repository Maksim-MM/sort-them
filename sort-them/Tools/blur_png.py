"""Blur a PNG for a static UI backdrop: python3 Tools/blur_png.py in.png out.png [downscale=6] [passes=3] [radius=2] [darken=0.75]"""
import sys, zlib, struct
sys.path.insert(0, __file__.rsplit('/', 1)[0])
from imgdiff import read_png


def write_png(path, w, h, rgb):
    raw = bytearray()
    stride = w * 3
    for y in range(h):
        raw.append(0)
        raw += rgb[y * stride:(y + 1) * stride]
    def chunk(t, b):
        c = struct.pack('>I', len(b)) + t + b
        return c + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(bytes(raw), 9)) + chunk(b'IEND', b'')
    open(path, 'wb').write(png)


def downsample(w, h, bpp, data, f):
    nw, nh = w // f, h // f
    out = bytearray(nw * nh * 3)
    for y in range(nh):
        for x in range(nw):
            r = g = b = 0
            for dy in range(f):
                row = (y * f + dy) * w * bpp
                for dx in range(f):
                    i = row + (x * f + dx) * bpp
                    r += data[i]; g += data[i + 1]; b += data[i + 2]
            n = f * f
            o = (y * nw + x) * 3
            out[o] = r // n; out[o + 1] = g // n; out[o + 2] = b // n
    return nw, nh, out


def box_blur(w, h, rgb, radius):
    tmp = bytearray(len(rgb))
    for y in range(h):
        for x in range(w):
            r = g = b = n = 0
            for dx in range(-radius, radius + 1):
                xx = min(w - 1, max(0, x + dx))
                i = (y * w + xx) * 3
                r += rgb[i]; g += rgb[i + 1]; b += rgb[i + 2]; n += 1
            o = (y * w + x) * 3
            tmp[o] = r // n; tmp[o + 1] = g // n; tmp[o + 2] = b // n
    out = bytearray(len(rgb))
    for y in range(h):
        for x in range(w):
            r = g = b = n = 0
            for dy in range(-radius, radius + 1):
                yy = min(h - 1, max(0, y + dy))
                i = (yy * w + x) * 3
                r += tmp[i]; g += tmp[i + 1]; b += tmp[i + 2]; n += 1
            o = (y * w + x) * 3
            out[o] = r // n; out[o + 1] = g // n; out[o + 2] = b // n
    return out


def upsample(w, h, rgb, f, darken):
    nw, nh = w * f, h * f
    out = bytearray(nw * nh * 3)
    for y in range(nh):
        sy = (y + 0.5) / f - 0.5
        y0 = max(0, min(h - 1, int(sy))); y1 = min(h - 1, y0 + 1); ty = sy - y0
        for x in range(nw):
            sx = (x + 0.5) / f - 0.5
            x0 = max(0, min(w - 1, int(sx))); x1 = min(w - 1, x0 + 1); tx = sx - x0
            o = (y * nw + x) * 3
            for c in range(3):
                a = rgb[(y0 * w + x0) * 3 + c] * (1 - tx) + rgb[(y0 * w + x1) * 3 + c] * tx
                b = rgb[(y1 * w + x0) * 3 + c] * (1 - tx) + rgb[(y1 * w + x1) * 3 + c] * tx
                out[o + c] = int((a * (1 - ty) + b * ty) * darken)
    return nw, nh, out


if __name__ == '__main__':
    src, dst = sys.argv[1], sys.argv[2]
    f = int(sys.argv[3]) if len(sys.argv) > 3 else 6
    passes = int(sys.argv[4]) if len(sys.argv) > 4 else 3
    radius = int(sys.argv[5]) if len(sys.argv) > 5 else 2
    darken = float(sys.argv[6]) if len(sys.argv) > 6 else 0.75
    w, h, bpp, data = read_png(src)
    sw, sh, small = downsample(w, h, bpp, data, f)
    for _ in range(passes):
        small = box_blur(sw, sh, small, radius)
    ow, oh, out = upsample(sw, sh, small, f, darken)
    write_png(dst, ow, oh, out)
    print(f'{src} {w}x{h} -> {dst} {ow}x{oh}')
