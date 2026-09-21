// Radio VEF 202 base-color texture: Russian labels -> English, Olympic emblem -> antenna icon.
// All text on this texture reads bottom-to-top with glyph tops facing -x (rotated 90° CCW).
// Usage: swift Tools/radio_english.swift Assets/_Game/Art/Models/Radio/Textures/DefaultMaterial_Base_Color.png
import Foundation
import CoreGraphics
import CoreText
import ImageIO
import UniformTypeIdentifiers

let path = CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : "Assets/_Game/Art/Models/Radio/Textures/DefaultMaterial_Base_Color.png"
let src = CGImageSourceCreateWithURL(URL(fileURLWithPath: path) as CFURL, nil)!
let img = CGImageSourceCreateImageAtIndex(src, 0, nil)!
let W = img.width, H = img.height
let ctx = CGContext(data: nil, width: W, height: H, bitsPerComponent: 8, bytesPerRow: W * 4, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
ctx.draw(img, in: CGRect(x: 0, y: 0, width: W, height: H))
let px = ctx.data!.assumingMemoryBound(to: UInt8.self)

typealias RGB = (Int, Int, Int)
func get(_ x: Int, _ y: Int) -> RGB { let i = (y * W + x) * 4; return (Int(px[i]), Int(px[i+1]), Int(px[i+2])) }
func set(_ x: Int, _ y: Int, _ c: RGB, _ a: Double = 1) {
    if x < 0 || y < 0 || x >= W || y >= H { return }
    let i = (y * W + x) * 4
    px[i] = UInt8(max(0, min(255, Double(px[i]) * (1 - a) + Double(c.0) * a)))
    px[i+1] = UInt8(max(0, min(255, Double(px[i+1]) * (1 - a) + Double(c.1) * a)))
    px[i+2] = UInt8(max(0, min(255, Double(px[i+2]) * (1 - a) + Double(c.2) * a)))
}
func fill(_ x0: Int, _ y0: Int, _ x1: Int, _ y1: Int, _ c: RGB) { for y in y0...y1 { for x in x0...x1 { set(x, y, c) } } }

struct Box { var x0: Int; var x1: Int; var y0: Int; var y1: Int; var count: Int }
func components(x0: Int, x1: Int, y0: Int, y1: Int, test: (RGB) -> Bool, minCount: Int, gap: Int) -> [Box] {
    let cw = x1 - x0 + 1, ch = y1 - y0 + 1
    var mark = [Bool](repeating: false, count: cw * ch)
    var out: [Box] = []
    for y in 0..<ch { for x in 0..<cw where !mark[y * cw + x] && test(get(x0 + x, y0 + y)) {
        var stack = [(x, y)]; mark[y * cw + x] = true
        var b = Box(x0: x, x1: x, y0: y, y1: y, count: 0)
        while let (cx, cy) = stack.popLast() {
            b.count += 1; b.x0 = min(b.x0, cx); b.x1 = max(b.x1, cx); b.y0 = min(b.y0, cy); b.y1 = max(b.y1, cy)
            for dy in -gap...gap { for dx in -gap...gap {
                let nx = cx + dx, ny = cy + dy
                if nx < 0 || ny < 0 || nx >= cw || ny >= ch || mark[ny * cw + nx] { continue }
                if test(get(x0 + nx, y0 + ny)) { mark[ny * cw + nx] = true; stack.append((nx, ny)) }
            } }
        }
        if b.count >= minCount { out.append(Box(x0: b.x0 + x0, x1: b.x1 + x0, y0: b.y0 + y0, y1: b.y1 + y0, count: b.count)) }
    } }
    return out.sorted { $0.y0 < $1.y0 }
}
let isYellow: (RGB) -> Bool = { $0.0 > 140 && $0.1 > 100 && $0.2 < 100 }
let isWhite: (RGB) -> Bool = { $0.0 > 170 && $0.1 > 170 && $0.2 > 170 }

// Renders upright text into a coverage bitmap and blits it rotated. Measured on the mesh (dP/du, dP/dv):
// front and back panels map texture +x to world DOWN and texture -y to the reading direction, so glyph tops go to -x
// and text runs toward -y. The band button is rotated 180° relative to that (flip = true).
// capX: texture x of the cap-height top line; yStart: texture y where the text begins; align 0 = start at yStart, 0.5 = centered on yStart.
@discardableResult
func drawText(_ text: String, font fontName: String, cap: Double, color: RGB, capX: Int, yStart: Int, align: Double = 0, maxLen: Int? = nil, flip: Bool = false) -> Int {
    var size = cap / 0.714 // Helvetica cap height ratio
    var font = CTFontCreateWithName(fontName as CFString, CGFloat(size), nil)
    var line = CTLineCreateWithAttributedString(NSAttributedString(string: text, attributes: [kCTFontAttributeName as NSAttributedString.Key: font, kCTForegroundColorAttributeName as NSAttributedString.Key: CGColor(gray: 1, alpha: 1)]))
    var width = CTLineGetTypographicBounds(line, nil, nil, nil)
    if let m = maxLen, width > Double(m) { size *= Double(m) / width; font = CTFontCreateWithName(fontName as CFString, CGFloat(size), nil)
        line = CTLineCreateWithAttributedString(NSAttributedString(string: text, attributes: [kCTFontAttributeName as NSAttributedString.Key: font, kCTForegroundColorAttributeName as NSAttributedString.Key: CGColor(gray: 1, alpha: 1)]))
        width = CTLineGetTypographicBounds(line, nil, nil, nil) }
    let capPx = CTFontGetCapHeight(font)
    let bw = Int(ceil(width)) + 4, bh = Int(ceil(capPx)) + 12
    let tctx = CGContext(data: nil, width: bw, height: bh, bitsPerComponent: 8, bytesPerRow: bw, space: CGColorSpaceCreateDeviceGray(), bitmapInfo: CGImageAlphaInfo.none.rawValue)!
    tctx.setFillColor(gray: 0, alpha: 1); tctx.fill(CGRect(x: 0, y: 0, width: bw, height: bh))
    tctx.setShouldSmoothFonts(true); tctx.setAllowsAntialiasing(true)
    tctx.textPosition = CGPoint(x: 2, y: 6) // baseline 6px above bottom (descenders); cap top at 6 + capPx
    CTLineDraw(line, tctx)
    let t = tctx.data!.assumingMemoryBound(to: UInt8.self)
    let along = Int(width)
    let start = flip ? yStart - Int(Double(along) * align) : yStart + Int(Double(along) * align)
    let capTopRow = Double(bh) - 6 - capPx // memory row 0 is the top of the bitmap; baseline sits 6 px above the bottom
    for m in 0..<bh { for u in 0..<bw {
        let a = Double(t[m * bw + u]) / 255
        if a < 0.02 { continue }
        let dv = Int(round(Double(m) - capTopRow)) // 0 at cap top, grows toward the baseline
        let x = flip ? capX - dv : capX + dv
        let y = flip ? start + (u - 2) : start - (u - 2)
        set(x, y, color, a)
    } }
    return along
}

let condensed = "HelveticaNeue-CondensedBold"
let medium = "HelveticaNeue-Medium"

// ---- 1. Dial: city names (yellow, two columns) ----
let yellow: RGB = (215, 183, 0)
let dial = components(x0: 3330, x1: 3415, y0: 2700, y1: 3800, test: isYellow, minCount: 150, gap: 4).filter { $0.x1 - $0.x0 >= 14 && $0.y1 - $0.y0 >= 44 }
let colA = dial.filter { $0.x0 < 3370 }, colB = dial.filter { $0.x0 >= 3370 }
let namesA = ["RIGA", "VILNIUS", "KIEV", "LVOV", "TALLINN", "KHARKOV"], namesB = ["MOSCOW", "LENINGRAD", "KISHINEV", "LUXEMBOURG"]
print("dial labels: colA \(colA.count) colB \(colB.count)")
guard colA.count == namesA.count && colB.count == namesB.count else { fatalError("unexpected dial label count") }
for (i, b) in colA.enumerated() { fill(b.x0 - 1, b.y0 - 2, b.x1 + 1, b.y1 + 2, (0, 0, 0)); drawText(namesA[i], font: condensed, cap: 15, color: yellow, capX: b.x0, yStart: (b.y0 + b.y1) / 2, align: 0.5) }
for (i, b) in colB.enumerated() { fill(b.x0 - 1, b.y0 - 2, b.x1 + 1, b.y1 + 2, (0, 0, 0)); drawText(namesB[i], font: condensed, cap: 15, color: yellow, capX: b.x0, yStart: (b.y0 + b.y1) / 2, align: 0.5) }

// ---- 2. Knob labels (white) ----
let white: RGB = (240, 240, 240)
struct Label { let box: (Int, Int, Int, Int); let text: String; let maxLen: Int }
let knobs = [
    Label(box: (3373, 2228, 3393, 2281), text: "BAND", maxLen: 58),
    Label(box: (3400, 2225, 3420, 2285), text: "TONE", maxLen: 58),
    Label(box: (3442, 2314, 3460, 2375), text: "TUNING", maxLen: 66),
    Label(box: (3470, 2313, 3489, 2376), text: "VOLUME", maxLen: 66),
]
for k in knobs {
    fill(k.box.0 - 1, k.box.1 - 1, k.box.2 + 1, k.box.3 + 1, (0, 0, 0))
    drawText(k.text, font: condensed, cap: Double(k.box.2 - k.box.0) * 0.92, color: white, capX: k.box.0 + 1, yStart: k.box.3, maxLen: k.maxLen)
}

// ---- 3. Backlight label "ПОДСВ." -> "LIGHT" ----
if let p = components(x0: 3400, x1: 3475, y0: 3930, y1: 4090, test: isWhite, minCount: 200, gap: 3).first {
    print("podsv box x \(p.x0)-\(p.x1) y \(p.y0)-\(p.y1)")
    fill(p.x0 - 1, p.y0 - 1, p.x1 + 1, p.y1 + 1, (0, 0, 0))
    drawText("LIGHT", font: condensed, cap: Double(p.x1 - p.x0) * 0.9, color: white, capX: p.x0 + 1, yStart: p.y1, maxLen: p.y1 - p.y0 + 10)
} else { print("podsv not found") }

// ---- 4. Band button "СВ" -> "MW" ----
let btnRed = get(2634, 3845)
fill(2634, 3843, 2676, 3892, btnRed)
drawText("MW", font: condensed, cap: 26, color: (250, 250, 250), capX: 2669, yStart: 3868, align: 0.5, flip: true)

// ---- 5. Olympic emblem -> antenna ----
let gold: RGB = (219, 186, 10)
let emb = (x0: 3133, x1: 3341, y0: 3927, y1: 4029)
fill(emb.x0 - 3, emb.y0 - 3, emb.x1 + 3, emb.y1 + 3, (0, 0, 0))
// draw upright on a canvas (u right, v down), tip at top; map: x = emb.x0 + v, y = emb.y1 - u
let cw = emb.y1 - emb.y0 + 1, ch = emb.x1 - emb.x0 + 1
let ac = CGContext(data: nil, width: cw, height: ch, bitsPerComponent: 8, bytesPerRow: cw, space: CGColorSpaceCreateDeviceGray(), bitmapInfo: CGImageAlphaInfo.none.rawValue)!
ac.setFillColor(gray: 0, alpha: 1); ac.fill(CGRect(x: 0, y: 0, width: cw, height: ch))
ac.translateBy(x: 0, y: CGFloat(ch)); ac.scaleBy(x: 1, y: -1) // v down
ac.setStrokeColor(gray: 1, alpha: 1); ac.setFillColor(gray: 1, alpha: 1); ac.setLineCap(.round)
let mid = Double(cw) / 2, tipV = 46.0, baseV = Double(ch) - 4
// lattice mast: two legs, cross bars, zig-zag braces
ac.setLineWidth(4)
ac.move(to: CGPoint(x: mid - 5, y: tipV)); ac.addLine(to: CGPoint(x: mid - 30, y: baseV))
ac.move(to: CGPoint(x: mid + 5, y: tipV)); ac.addLine(to: CGPoint(x: mid + 30, y: baseV))
ac.strokePath()
ac.setLineWidth(3)
let bars = 6
for i in 0...bars {
    let t = Double(i) / Double(bars)
    let v = tipV + (baseV - tipV) * t
    let half = 5 + 25 * t
    ac.move(to: CGPoint(x: mid - half, y: v)); ac.addLine(to: CGPoint(x: mid + half, y: v))
    if i < bars {
        let t2 = Double(i + 1) / Double(bars), v2 = tipV + (baseV - tipV) * t2, half2 = 5 + 25 * t2
        if i % 2 == 0 { ac.move(to: CGPoint(x: mid - half, y: v)); ac.addLine(to: CGPoint(x: mid + half2, y: v2)) }
        else { ac.move(to: CGPoint(x: mid + half, y: v)); ac.addLine(to: CGPoint(x: mid - half2, y: v2)) }
    }
}
ac.strokePath()
// spire and beacon
ac.setLineWidth(4); ac.move(to: CGPoint(x: mid, y: tipV)); ac.addLine(to: CGPoint(x: mid, y: 22)); ac.strokePath()
ac.fillEllipse(in: CGRect(x: mid - 5, y: 17, width: 10, height: 10))
// signal arcs around the beacon
ac.setLineWidth(3.5)
for r in [16.0, 28.0, 40.0] {
    ac.addArc(center: CGPoint(x: mid, y: 22), radius: r, startAngle: CGFloat(-Double.pi * 0.85), endAngle: CGFloat(-Double.pi * 0.15), clockwise: false)
    ac.strokePath()
}
let ad = ac.data!.assumingMemoryBound(to: UInt8.self)
for v in 0..<ch { for u in 0..<cw {
    let a = Double(ad[v * cw + u]) / 255 // flipped context: drawing row v is memory row v
    if a < 0.02 { continue }
    set(emb.x0 + v, emb.y1 - u, gold, a)
} }

// ---- 6. Back panel: Russian spec text -> English ----
let panel: RGB = (26, 26, 26), gray: RGB = (150, 150, 150)
// table cell (frequency ranges): erase text interior, keep frame
fill(618, 2882, 928, 3190, panel)
var lineX = 640 // first line is the top one; top = -x on this model
func backLine(_ s: String, cap: Double, x: Int, yStart: Int, maxLen: Int) { drawText(s, font: medium, cap: cap, color: gray, capX: x, yStart: yStart, maxLen: maxLen) }
backLine("FREQUENCY RANGES", cap: 15, x: lineX, yStart: 3165, maxLen: 270); lineX += 42
for s in ["LW   150 -  408 kHz", "MW   525 - 1605 kHz", "52m  3.95 -  5.7 MHz", "49m  5.85 -  6.3 MHz", "41m  7.0  -  7.4 MHz", "31m  9.5  - 9.775 MHz", "25m  11.7 - 12.1 MHz"] {
    backLine(s, cap: 13, x: lineX, yStart: 3165, maxLen: 270); lineX += 36
}
// power cell: erase the two text columns, keep the battery drawing
fill(848, 3262, 926, 3566, panel)
fill(818, 3528, 846, 3612, panel)
backLine("POWER SUPPLY", cap: 13, x: 822, yStart: 3612, maxLen: 84)
backLine("9 V FROM 6 CELLS", cap: 13, x: 856, yStart: 3560, maxLen: 290)
backLine("TYPE 373 (R20)", cap: 13, x: 892, yStart: 3560, maxLen: 290)
// bottom cell: erase text right of the logo
fill(789, 3630, 928, 3784, panel)
fill(761, 3630, 781, 3784, panel) // tail of the old Russian lines left of the divider (x 782-785)
backLine("TRANSISTOR RADIO RECEIVER", cap: 12, x: 796, yStart: 3780, maxLen: 148)
backLine("CLASS 1  ·  VEF 202", cap: 12, x: 834, yStart: 3780, maxLen: 148)
backLine("MADE IN RIGA", cap: 12, x: 872, yStart: 3780, maxLen: 148)

guard let out = ctx.makeImage(), let dest = CGImageDestinationCreateWithURL(URL(fileURLWithPath: path) as CFURL, UTType.png.identifier as CFString, 1, nil) else { fatalError("save") }
CGImageDestinationAddImage(dest, out, nil)
CGImageDestinationFinalize(dest)
print("written " + path)

// ---- 7. Normal map: flatten the embossed Russian text on the back panel (frames and battery drawing stay) ----
let normalPath = (path as NSString).deletingLastPathComponent + "/DefaultMaterial_Normal_OpenGL.png"
if let nsrc = CGImageSourceCreateWithURL(URL(fileURLWithPath: normalPath) as CFURL, nil), let nimg = CGImageSourceCreateImageAtIndex(nsrc, 0, nil) {
    let nctx = CGContext(data: nil, width: nimg.width, height: nimg.height, bitsPerComponent: 8, bytesPerRow: nimg.width * 4, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
    nctx.draw(nimg, in: CGRect(x: 0, y: 0, width: nimg.width, height: nimg.height))
    let np = nctx.data!.assumingMemoryBound(to: UInt8.self)
    let flatBoxes = [(618, 2882, 928, 3190), (848, 3262, 926, 3566), (818, 3528, 846, 3612), (789, 3630, 928, 3784), (761, 3630, 781, 3784)]
    for b in flatBoxes { for y in b.1...b.3 { for x in b.0...b.2 { let i = (y * nimg.width + x) * 4; np[i] = 128; np[i+1] = 128; np[i+2] = 255; np[i+3] = 255 } } }
    if let nout = nctx.makeImage(), let ndest = CGImageDestinationCreateWithURL(URL(fileURLWithPath: normalPath) as CFURL, UTType.png.identifier as CFString, 1, nil) {
        CGImageDestinationAddImage(ndest, nout, nil); CGImageDestinationFinalize(ndest); print("written " + normalPath)
    }
} else { print("normal map not found: " + normalPath) }
