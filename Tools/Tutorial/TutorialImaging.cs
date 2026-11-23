// Helper for Tools/Tutorial/*.ps1 (compiled by Windows PowerShell 5.1 with Add-Type, so C# 5 only).
// Title cards, screen capture of the Unity Editor window, and caption overlays for the
// handover tutorial video. Uses System.Drawing only (built into Windows), no downloads.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

public static class TutorialImaging
{
    static readonly Color Midnight = ColorTranslator.FromHtml("#1B1030");
    static readonly Color Deep = ColorTranslator.FromHtml("#2A1640");
    static readonly Color Panel = ColorTranslator.FromHtml("#120A22");
    static readonly Color Yellow = ColorTranslator.FromHtml("#FFD83D");
    static readonly Color Orange = ColorTranslator.FromHtml("#F57F32");
    static readonly Color OffWhite = ColorTranslator.FromHtml("#FBF0D5");

    // ------------------------------------------------------------------ title cards

    /// <summary>Renders a 16:9 title card. bullets may be empty; code may be null.</summary>
    public static void RenderCard(string outPath, int w, int h, string logoPath, string kicker,
                                  string title, string[] bullets, string code, string footer, bool bigLogo)
    {
        using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
        using (var g = Graphics.FromImage(bmp))
        {
            Setup(g);
            using (var bg = new LinearGradientBrush(new Rectangle(0, 0, w, h), Midnight, Deep, 60f))
                g.FillRectangle(bg, 0, 0, w, h);
            float s = w / 1920f;

            using (var logo = Image.FromFile(logoPath))
            {
                if (bigLogo)
                {
                    float lw = 760 * s, lh = lw * logo.Height / logo.Width;
                    g.DrawImage(logo, (w - lw) / 2, 120 * s, lw, lh);
                }
                else
                {
                    float lw = 250 * s, lh = lw * logo.Height / logo.Width;
                    g.DrawImage(logo, 90 * s, 60 * s, lw, lh);
                }
            }

            using (var kFont = new Font("Segoe UI Semibold", 26 * s, GraphicsUnit.Pixel))
            using (var tFont = new Font("Segoe UI", 68 * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var bFont = new Font("Segoe UI", 38 * s, GraphicsUnit.Pixel))
            using (var cFont = new Font("Consolas", 34 * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var fFont = new Font("Segoe UI", 24 * s, GraphicsUnit.Pixel))
            using (var orange = new SolidBrush(Orange))
            using (var yellow = new SolidBrush(Yellow))
            using (var white = new SolidBrush(OffWhite))
            using (var dim = new SolidBrush(Color.FromArgb(170, OffWhite)))
            using (var panel = new SolidBrush(Panel))
            {
                float x = 150 * s;
                float y = bigLogo ? 640 * s : 300 * s;
                var center = new StringFormat { Alignment = StringAlignment.Center };

                if (bigLogo)
                {
                    g.DrawString(title, tFont, yellow, new RectangleF(0, y, w, 100 * s), center);
                    y += 110 * s;
                    g.DrawString(kicker, bFont, white, new RectangleF(0, y, w, 60 * s), center);
                    y += 90 * s;
                    foreach (string b in bullets)
                    {
                        g.DrawString(b, kFont, dim, new RectangleF(0, y, w, 50 * s), center);
                        y += 50 * s;
                    }
                }
                else
                {
                    g.DrawString(kicker.ToUpperInvariant(), kFont, orange, x, y - 60 * s);
                    g.DrawString(title, tFont, yellow, x, y);
                    y += 130 * s;
                    if (code != null)
                    {
                        SizeF cs = g.MeasureString(code, cFont);
                        var box = new RectangleF(x, y, Math.Max(cs.Width + 80 * s, 900 * s), cs.Height + 50 * s);
                        using (var path = Rounded(box, 18 * s)) g.FillPath(panel, path);
                        g.DrawString(code, cFont, yellow, x + 40 * s, y + 25 * s);
                        y += box.Height + 50 * s;
                    }
                    foreach (string b in bullets)
                    {
                        g.FillEllipse(orange, x + 4 * s, y + 18 * s, 14 * s, 14 * s);
                        var rect = new RectangleF(x + 44 * s, y, w - x - 200 * s, 200 * s);
                        SizeF size = g.MeasureString(b, bFont, (int)rect.Width);
                        g.DrawString(b, bFont, white, rect);
                        y += size.Height + 26 * s;
                    }
                }

                if (!string.IsNullOrEmpty(footer))
                    g.DrawString(footer, fFont, dim, new RectangleF(0, h - 70 * s, w - 90 * s, 40 * s),
                                 new StringFormat { Alignment = StringAlignment.Far });
            }
            SaveJpeg(bmp, outPath, 92);
        }
    }

    // ------------------------------------------------------------------ capture

    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr pid);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    /// <summary>Maximizes a window and brings it to the front. Returns true if it is now the foreground window.</summary>
    public static bool BringToFront(IntPtr hwnd)
    {
        ShowWindow(hwnd, 3); // SW_MAXIMIZE
        IntPtr fg = GetForegroundWindow();
        uint fgThread = GetWindowThreadProcessId(fg, IntPtr.Zero);
        uint me = GetCurrentThreadId();
        if (fgThread != me) AttachThreadInput(me, fgThread, true);
        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);
        if (fgThread != me) AttachThreadInput(me, fgThread, false);
        // Raise in z-order even if focus stays elsewhere (TOPMOST then NOTOPMOST; no move/size).
        SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        return GetForegroundWindow() == hwnd;
    }

    /// <summary>
    /// Captures a screen region to JPEG files named by Unix time in ms, about fps times a second,
    /// until the process exits or stopFile appears. Returns the number of frames written.
    /// </summary>
    public static int CaptureLoop(int processId, int x, int y, int w, int h, int fps, string outDir, string stopFile, int maxSeconds)
    {
        Directory.CreateDirectory(outDir);
        Process proc = Process.GetProcessById(processId);
        int count = 0;
        long interval = 1000 / fps;
        var codec = Jpeg();
        var prms = new EncoderParameters(1);
        prms.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 88L);
        using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
        using (var g = Graphics.FromImage(bmp))
        {
            var sw = Stopwatch.StartNew();
            long next = 0;
            while (!proc.HasExited && !File.Exists(stopFile) && sw.ElapsedMilliseconds < maxSeconds * 1000L)
            {
                long now = sw.ElapsedMilliseconds;
                if (now < next) { Thread.Sleep((int)Math.Min(next - now, 20)); continue; }
                next = now + interval;
                long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                try
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
                    bmp.Save(Path.Combine(outDir, stamp + ".jpg"), codec, prms);
                    count++;
                }
                catch (Exception) { /* screen locked or busy: skip this frame */ }
            }
        }
        return count;
    }

    // ------------------------------------------------------------------ captioned frames

    static Bitmap _canvas;
    static Graphics _g;
    static Font _capFont, _labelFont;

    /// <summary>
    /// Composes one video frame: the captured Editor image scaled into the top area,
    /// and a caption strip (label + caption) underneath.
    /// </summary>
    public static void ComposeTourFrame(string capturePath, string outPath, int w, int h, int stripHeight,
                                        string label, string caption)
    {
        if (_canvas == null || _canvas.Width != w || _canvas.Height != h)
        {
            _canvas = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            _g = Graphics.FromImage(_canvas);
            Setup(_g);
            _capFont = new Font("Segoe UI Semibold", 40 * w / 1920f, GraphicsUnit.Pixel);
            _labelFont = new Font("Segoe UI", 24 * w / 1920f, GraphicsUnit.Pixel);
        }
        var g = _g;
        g.Clear(Midnight);
        using (var img = Image.FromFile(capturePath))
        {
            float areaH = h - stripHeight;
            float scale = Math.Min(w / (float)img.Width, areaH / img.Height);
            float dw = img.Width * scale, dh = img.Height * scale;
            g.DrawImage(img, (w - dw) / 2, (areaH - dh) / 2, dw, dh);
        }
        using (var strip = new SolidBrush(Panel)) g.FillRectangle(strip, 0, h - stripHeight, w, stripHeight);
        using (var bar = new SolidBrush(Orange)) g.FillRectangle(bar, 0, h - stripHeight, w, 4);
        using (var yellow = new SolidBrush(Yellow))
        using (var white = new SolidBrush(OffWhite))
        {
            float s = w / 1920f;
            g.DrawString(label, _labelFont, yellow, 60 * s, h - stripHeight + 22 * s);
            g.DrawString(caption, _capFont, white, new RectangleF(60 * s, h - stripHeight + 58 * s, w - 120 * s, stripHeight - 60 * s));
        }
        SaveJpeg(_canvas, outPath, 90);
    }

    // ------------------------------------------------------------------ utilities

    static void Setup(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    }

    static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        float d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static ImageCodecInfo Jpeg()
    {
        foreach (var c in ImageCodecInfo.GetImageEncoders())
            if (c.FormatID == ImageFormat.Jpeg.Guid) return c;
        throw new InvalidOperationException("No JPEG encoder");
    }

    static void SaveJpeg(Bitmap bmp, string path, long quality)
    {
        var prms = new EncoderParameters(1);
        prms.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
        bmp.Save(path, Jpeg(), prms);
    }
}
