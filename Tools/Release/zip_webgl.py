"""
Packages a WebGL build folder into an upload-ready zip (itch.io, Netlify drop, etc.).

Why this script exists: Windows PowerShell 5.1's Compress-Archive writes entry names with
backslashes ("Build\\WebGL.data.unityweb"). Linux-based hosts such as itch.io then see no
"Build" or "TemplateData" folders, so the page loads unstyled and shows
"Could not load the game files". This script always writes forward-slash paths.

Usage (from the repository root):
    py -I Tools/Release/zip_webgl.py Builds/WebGL Builds/SayHeyDash_M2_WebGL.zip
"""
import os
import sys
import zipfile


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 2
    src, dst = sys.argv[1], sys.argv[2]
    if not os.path.isfile(os.path.join(src, "index.html")):
        print(f"error: {src} has no index.html - is it a WebGL build folder?")
        return 1
    if os.path.exists(dst):
        os.remove(dst)
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED) as z:
        for root, _, files in os.walk(src):
            for name in sorted(files):
                full = os.path.join(root, name)
                arc = os.path.relpath(full, src).replace(os.sep, "/")
                # .unityweb / .br / .gz are already compressed: store them as-is.
                stored = name.endswith((".unityweb", ".br", ".gz", ".wasm"))
                z.write(full, arc, compress_type=zipfile.ZIP_STORED if stored else zipfile.ZIP_DEFLATED)
    with zipfile.ZipFile(dst) as z:
        for info in z.infolist():
            print(f"{info.filename:45} {info.file_size:>12,}")
    print(f"wrote {dst} ({os.path.getsize(dst) / 1048576:.2f} MB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
