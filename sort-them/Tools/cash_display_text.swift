// Replaces the two item lines on the cash register display texture (lambert1_albedo/emissive)
// with segmented "[ CAR 1 ]" / "[ CAR 2 ]". Text on the texture is rotated 90° CW and read top to bottom.
// Usage: swift Tools/cash_display_text.swift <textures folder>
import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers

let dir = CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : "Assets/_Game/Art/Models/CashRegister/Textures"

let glyphs: [Character: [String]] = [
    "C": ["111", "100", "100", "100", "111"],
    "A": ["111", "101", "111", "101", "101"],
    "R": ["110", "101", "110", "101", "101"],
    "1": ["010", "110", "010", "010", "111"],
    "2": ["111", "001", "111", "100", "111"],
    "[": ["11", "10", "10", "10", "11"],
    "]": ["11", "01", "01", "01", "11"],
    " ": ["0", "0", "0", "0", "0"],
]

struct Line { let colX: Int; let startY: Int; let clearTo: Int; let text: String }
// colX: left edge of the glyph column (across reading direction); startY: where the new text begins (old "[" position)
let lines = [
    Line(colX: 877, startY: 258, clearTo: 585, text: "[ CAR 1 ]"),
    Line(colX: 787, startY: 258, clearTo: 585, text: "[ CAR 2 ]"),
]
let colWidth = 42      // cleared band across reading direction
let dot = 5, pitch = 6 // dot size and spacing in texels
let glyphAcross = 5 * pitch // 30 texels across (letter height)

func load(_ path: String) -> CGImage? {
    guard let src = CGImageSourceCreateWithURL(URL(fileURLWithPath: path) as CFURL, nil) else { return nil }
    return CGImageSourceCreateImageAtIndex(src, 0, nil)
}
func save(_ img: CGImage, _ path: String, quality: Double) {
    guard let dest = CGImageDestinationCreateWithURL(URL(fileURLWithPath: path) as CFURL, UTType.jpeg.identifier as CFString, 1, nil) else { fatalError("dest") }
    CGImageDestinationAddImage(dest, img, [kCGImageDestinationLossyCompressionQuality: quality] as CFDictionary)
    CGImageDestinationFinalize(dest)
}

func process(file: String, background: CGColor, ink: CGColor, useSampledBackground: Bool) {
    let path = dir + "/" + file
    guard let img = load(path) else { fatalError("cannot load " + path) }
    let w = img.width, h = img.height
    let ctx = CGContext(data: nil, width: w, height: h, bitsPerComponent: 8, bytesPerRow: w * 4, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
    ctx.draw(img, in: CGRect(x: 0, y: 0, width: w, height: h))
    // sample the display background between the two text columns (memory rows run top to bottom)
    let data = ctx.data!.assumingMemoryBound(to: UInt8.self)
    let si = (430 * w + 850) * 4
    let sampled = CGColor(red: CGFloat(data[si]) / 255, green: CGFloat(data[si + 1]) / 255, blue: CGFloat(data[si + 2]) / 255, alpha: 1)
    let fillBg = useSampledBackground ? sampled : background
    // CoreGraphics origin is bottom-left; flip only for our own drawing so that y grows downward like texture pixels
    ctx.translateBy(x: 0, y: CGFloat(h)); ctx.scaleBy(x: 1, y: -1)
    ctx.setShouldAntialias(false)
    for line in lines {
        ctx.setFillColor(fillBg)
        ctx.fill(CGRect(x: line.colX - (colWidth - glyphAcross) / 2, y: line.startY - 4, width: colWidth, height: line.clearTo - line.startY + 4))
        ctx.setFillColor(ink)
        var y = line.startY
        for ch in line.text {
            let rows = glyphs[ch] ?? glyphs[" "]!
            let cols = rows[0].count
            for (r, row) in rows.enumerated() {
                for (c, bit) in row.enumerated() where bit == "1" {
                    // reading direction = +y; glyph top faces +x
                    let x = line.colX + (4 - r) * pitch
                    let yy = y + c * pitch
                    ctx.fill(CGRect(x: x, y: yy, width: dot, height: dot))
                }
            }
            y += cols * pitch + pitch // one dot gap between glyphs
        }
    }
    guard let out = ctx.makeImage() else { fatalError("makeImage") }
    save(out, path, quality: 0.95)
    print("written " + path)
}

process(file: "lambert1_albedo.jpg", background: CGColor(red: 0.125, green: 0.125, blue: 0.125, alpha: 1), ink: CGColor(red: 0.92, green: 0.92, blue: 0.92, alpha: 1), useSampledBackground: true)
process(file: "lambert1_emissive.jpg", background: CGColor(red: 0, green: 0, blue: 0, alpha: 1), ink: CGColor(red: 1, green: 1, blue: 1, alpha: 1), useSampledBackground: false)
