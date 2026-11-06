import Foundation
import ImageIO
import CoreGraphics
import UniformTypeIdentifiers

// usage: swift Tools/png_rgba.swift stats <png>        -> prints size, alpha stats
//        swift Tools/png_rgba.swift convert <in> <out>  -> writes 32-bit RGBA PNG (alpha 255 everywhere)
//        swift Tools/png_rgba.swift resize <in> <out> <w> <h> -> writes 24-bit RGB PNG scaled to w x h

func load(_ path: String) -> CGImage? {
    guard let src = CGImageSourceCreateWithURL(URL(fileURLWithPath: path) as CFURL, nil) else { return nil }
    return CGImageSourceCreateImageAtIndex(src, 0, nil)
}

func rgba(_ img: CGImage) -> (ctx: CGContext, data: UnsafeMutablePointer<UInt8>)? {
    let w = img.width, h = img.height
    let data = UnsafeMutablePointer<UInt8>.allocate(capacity: w * h * 4)
    data.initialize(repeating: 0, count: w * h * 4)
    guard let ctx = CGContext(data: data, width: w, height: h, bitsPerComponent: 8, bytesPerRow: w * 4,
                              space: CGColorSpace(name: CGColorSpace.sRGB)!,
                              bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { return nil }
    ctx.draw(img, in: CGRect(x: 0, y: 0, width: w, height: h))
    return (ctx, data)
}

let args = CommandLine.arguments
guard args.count >= 3, let img = load(args[2]) else { print("usage / load failed"); exit(1) }
print("input \(img.width)x\(img.height) bpp=\(img.bitsPerPixel) alphaInfo=\(img.alphaInfo.rawValue)")
if args[1] == "resize", args.count >= 6, let w = Int(args[4]), let h = Int(args[5]) {
    guard let ctx = CGContext(data: nil, width: w, height: h, bitsPerComponent: 8, bytesPerRow: w * 4,
                              space: CGColorSpace(name: CGColorSpace.sRGB)!,
                              bitmapInfo: CGImageAlphaInfo.noneSkipLast.rawValue) else { print("ctx failed"); exit(1) }
    ctx.interpolationQuality = .high
    ctx.draw(img, in: CGRect(x: 0, y: 0, width: w, height: h))
    guard let out = ctx.makeImage(),
          let dst = CGImageDestinationCreateWithURL(URL(fileURLWithPath: args[3]) as CFURL, UTType.png.identifier as CFString, 1, nil) else { print("dest failed"); exit(1) }
    CGImageDestinationAddImage(dst, out, nil)
    print(CGImageDestinationFinalize(dst) ? "written \(args[3]) \(w)x\(h)" : "finalize failed")
    exit(0)
}
guard let (ctx, data) = rgba(img) else { print("ctx failed"); exit(1) }
let n = img.width * img.height
if args[1] == "stats" {
    var a0 = 0, a255 = 0
    for i in 0..<n { let a = data[i * 4 + 3]; if a == 0 { a0 += 1 } else if a == 255 { a255 += 1 } }
    print(String(format: "alpha0=%.1f%% alpha255=%.1f%% partial=%.1f%%", 100.0 * Double(a0) / Double(n), 100.0 * Double(a255) / Double(n), 100.0 * Double(n - a0 - a255) / Double(n)))
} else if args[1] == "convert", args.count >= 4 {
    for i in 0..<n { data[i * 4 + 3] = 255 }
    guard let out = ctx.makeImage(),
          let dst = CGImageDestinationCreateWithURL(URL(fileURLWithPath: args[3]) as CFURL, UTType.png.identifier as CFString, 1, nil) else { print("dest failed"); exit(1) }
    CGImageDestinationAddImage(dst, out, nil)
    print(CGImageDestinationFinalize(dst) ? "written \(args[3])" : "finalize failed")
}
