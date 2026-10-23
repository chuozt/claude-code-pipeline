using System.Text;

// Read-only PSD (v1) parser: layer records, groups, text engine data, layer effects, pixels.
// Pixel data is only read on demand, so a 800 MB file parses in well under a second.

class BE
{
    public readonly Stream S;
    readonly byte[] b = new byte[8];
    public BE(Stream s) { S = s; }
    public long Pos { get => S.Position; set => S.Position = value; }
    public byte U8() { int v = S.ReadByte(); if (v < 0) throw new EndOfStreamException(); return (byte)v; }
    public byte[] Bytes(int n) { var a = new byte[n]; S.ReadExactly(a, 0, n); return a; }
    public ushort U16() { S.ReadExactly(b, 0, 2); return (ushort)(b[0] << 8 | b[1]); }
    public short I16() => (short)U16();
    public uint U32() { S.ReadExactly(b, 0, 4); return (uint)(b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3]); }
    public int I32() => (int)U32();
    public ulong U64() => (ulong)U32() << 32 | U32();
    public double F64() => BitConverter.Int64BitsToDouble((long)U64());
    public string Ascii(int n) => Encoding.ASCII.GetString(Bytes(n));
    public string Uni() { uint n = U32(); return Encoding.BigEndianUnicode.GetString(Bytes((int)n * 2)).TrimEnd('\0'); }
    public string Id() { uint n = U32(); return Ascii(n == 0 ? 4 : (int)n); }
}

class TextRun
{
    public string Text, Font, Color;
    public double SizePx, Tracking, Leading, HScale = 1, VScale = 1, Baseline;
    public bool AutoLeading = true, FauxBold, FauxItalic;
    public int Caps;
}

class TextInfo
{
    public string Text = "", Justify = "left", Kind = "point";
    public double Scale = 1;
    public List<TextRun> Runs = new();
    public double[] Box;
}

class Layer
{
    public int Index, Top, Left, Bottom, Right, Section;
    public string Name = "", Blend = "", Kind = "pixel";
    public byte Opacity, Flags;
    public int FillOpacity = 255;
    public bool Hidden => (Flags & 2) != 0;
    public bool Clipped;
    public List<(short id, long len)> Channels = new();
    public long DataOffset;
    public Dictionary<string, object> Fx;
    public bool FxMulti;
    public TextInfo Text;
    public string FillColor;
    public List<string> Errors = new();
    // tree (built after parsing)
    public Layer Parent;
    public List<Layer> Children = new();
    public bool IsGroup => Section is 1 or 2;
    public int W => Right - Left;
    public int H => Bottom - Top;
    public string Path => Parent == null ? Name : Parent.Path + "/" + Name;
    public bool VisibleFrom(Layer root)
    {
        for (var l = this; l != null && l != root; l = l.Parent) if (l.Hidden) return false;
        return true;
    }
}

class Psd
{
    public int W, H, Ch, Depth, Mode;
    public double Ppi = 72;
    public int GlobalAngle = 90;
    public List<double> GuidesX = new(), GuidesY = new();
    public List<Layer> Layers = new();
    public List<Layer> Roots = new();
    public long CompositeOffset;
    public string FilePath;

    public static Psd Open(string path)
    {
        var p = new Psd { FilePath = path };
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        p.Read(new BE(fs));
        return p;
    }

    void Read(BE r)
    {
        if (r.Ascii(4) != "8BPS") throw new Exception("not a PSD file");
        if (r.U16() != 1) throw new Exception("PSB (large document) is not supported");
        r.Bytes(6); Ch = r.U16(); H = r.I32(); W = r.I32(); Depth = r.U16(); Mode = r.U16();
        uint cm = r.U32(); r.Pos += cm;
        uint ir = r.U32(); long irEnd = r.Pos + ir;
        while (r.Pos < irEnd)
        {
            r.Ascii(4); int id = r.U16(); int nl = r.U8(); r.Bytes(nl); if ((nl + 1) % 2 == 1) r.U8();
            uint len = r.U32(); long st = r.Pos;
            if (id == 1005) Ppi = r.U32() / 65536.0;
            if (id == 1037) GlobalAngle = r.I32();
            if (id == 1032)
            {
                r.U32(); r.U32(); r.U32(); uint n = r.U32();
                for (int i = 0; i < n; i++) { double loc = r.I32() / 32.0; int dir = r.U8(); (dir == 0 ? GuidesX : GuidesY).Add(loc); }
            }
            r.Pos = st + len + (len % 2);
        }
        r.Pos = irEnd;
        uint lmLen = r.U32(); long lmEnd = r.Pos + lmLen;
        r.U32();
        int count = Math.Abs(r.I16());
        for (int i = 0; i < count; i++) Layers.Add(ReadRecord(r, i));
        long off = r.Pos;
        foreach (var l in Layers) { l.DataOffset = off; foreach (var c in l.Channels) off += c.len; }
        CompositeOffset = lmEnd;
        BuildTree();
    }

    void BuildTree()
    {
        var stack = new Stack<Layer>();
        for (int i = Layers.Count - 1; i >= 0; i--)
        {
            var l = Layers[i];
            if (l.Section == 3) { if (stack.Count > 0) stack.Pop(); continue; }
            l.Parent = stack.Count > 0 ? stack.Peek() : null;
            (l.Parent?.Children ?? Roots).Add(l);
            if (l.IsGroup) stack.Push(l);
        }
    }

    Layer ReadRecord(BE r, int idx)
    {
        var l = new Layer { Index = idx };
        l.Top = r.I32(); l.Left = r.I32(); l.Bottom = r.I32(); l.Right = r.I32();
        int nc = r.U16();
        for (int c = 0; c < nc; c++) l.Channels.Add((r.I16(), r.U32()));
        r.Ascii(4); l.Blend = r.Ascii(4); l.Opacity = r.U8(); l.Clipped = r.U8() != 0; l.Flags = r.U8(); r.U8();
        uint extra = r.U32(); long end = r.Pos + extra;
        uint ml = r.U32(); r.Pos += ml;
        uint bl = r.U32(); r.Pos += bl;
        int nl = r.U8(); l.Name = Encoding.Latin1.GetString(r.Bytes(nl)); int pad = (nl + 1) % 4; if (pad != 0) r.Pos += 4 - pad;
        while (r.Pos + 12 <= end)
        {
            string sig = r.Ascii(4); if (sig != "8BIM" && sig != "8B64") break;
            string key = r.Ascii(4); uint len = r.U32(); long st = r.Pos;
            try { ReadInfo(r, l, key); } catch (Exception e) { l.Errors.Add($"{key}: {e.Message}"); }
            r.Pos = st + len;
        }
        r.Pos = end;
        return l;
    }

    void ReadInfo(BE r, Layer l, string key)
    {
        switch (key)
        {
            case "luni": l.Name = r.Uni(); break;
            case "lsct": case "lsdk": l.Section = r.I32(); break;
            case "iOpa": l.FillOpacity = r.U8(); break;
            case "SoLd": case "PlLd": l.Kind = "smart-object"; break;
            case "SoCo": { l.Kind = "solid-fill"; r.I32(); var d = Desc.Read(r); l.FillColor = Desc.Color(Desc.Get(d, "Clr ")); break; }
            case "GdFl": l.Kind = "gradient-fill"; break;
            case "vmsk": case "vsms": case "vogk": if (l.Kind == "pixel") l.Kind = "shape"; break;
            case "levl": case "curv": case "hue2": case "blnc": case "brit": case "selc": case "mixr": case "phfl":
            case "expA": case "vibA": case "grdm": case "thrs": case "post": case "nvrt": case "blwh": case "CgEd":
                l.Kind = "adjustment"; break;
            case "lfx2": if (l.Fx == null) { r.I32(); r.I32(); l.Fx = Desc.Read(r); } break;
            case "lmfx": { r.I32(); r.I32(); l.Fx = Desc.Read(r); l.FxMulti = true; break; }
            case "TySh": ReadText(r, l); break;
        }
    }

    void ReadText(BE r, Layer l)
    {
        l.Kind = "text";
        r.U16();
        double xx = r.F64(), xy = r.F64(), yx = r.F64(), yy = r.F64(); r.F64(); r.F64();
        r.U16(); r.I32();
        var d = Desc.Read(r);
        var t = new TextInfo { Scale = Math.Sqrt(yx * yx + yy * yy) };
        if (Desc.Get(d, "Txt ") is string s) t.Text = s;
        if (Desc.Get(d, "EngineData") is byte[] raw) Engine.Fill(raw, t);
        l.Text = t;
    }

    // ---------- pixels ----------
    static byte[] Rle(BE r, int rows, int width, ushort[] counts, int start)
    {
        var o = new byte[rows * width];
        int p = 0;
        for (int y = 0; y < rows; y++)
        {
            var src = r.Bytes(counts[start + y]);
            int i = 0, rowEnd = (y + 1) * width;
            while (i < src.Length && p < rowEnd)
            {
                int n = (sbyte)src[i++];
                if (n >= 0) { for (int k = 0; k <= n && i < src.Length && p < rowEnd; k++) o[p++] = src[i++]; }
                else if (n != -128) { byte v = src[i++]; for (int k = 0; k < 1 - n && p < rowEnd; k++) o[p++] = v; }
            }
            p = rowEnd;
        }
        return o;
    }

    /// <summary>RGBA of one layer's own pixels — layer effects (stroke, shadow, overlays) are NOT included.</summary>
    public byte[] LayerPixels(Layer l)
    {
        int w = l.W, h = l.H;
        if (w <= 0 || h <= 0) return null;
        using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        var r = new BE(fs) { Pos = l.DataOffset };
        var planes = new Dictionary<short, byte[]>();
        foreach (var (id, len) in l.Channels)
        {
            long st = r.Pos;
            int comp = r.U16();
            if (id >= -1)
            {
                if (comp == 0) planes[id] = r.Bytes(w * h);
                else if (comp == 1) { var counts = new ushort[h]; for (int y = 0; y < h; y++) counts[y] = r.U16(); planes[id] = Rle(r, h, w, counts, 0); }
            }
            r.Pos = st + len;
        }
        var rgba = new byte[w * h * 4];
        for (int i = 0; i < w * h; i++)
        {
            rgba[i * 4] = planes.TryGetValue(0, out var R) ? R[i] : (byte)0;
            rgba[i * 4 + 1] = planes.TryGetValue(1, out var G) ? G[i] : (byte)0;
            rgba[i * 4 + 2] = planes.TryGetValue(2, out var B) ? B[i] : (byte)0;
            rgba[i * 4 + 3] = planes.TryGetValue(-1, out var A) ? A[i] : (byte)255;
        }
        return rgba;
    }

    /// <summary>The flattened image as saved — only the layers visible at save time, effects included.</summary>
    public byte[] CompositePixels()
    {
        using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        var r = new BE(fs) { Pos = CompositeOffset };
        int comp = r.U16();
        var planes = new byte[Ch][];
        if (comp == 1)
        {
            var counts = new ushort[H * Ch];
            for (int i = 0; i < counts.Length; i++) counts[i] = r.U16();
            for (int c = 0; c < Ch; c++) planes[c] = Rle(r, H, W, counts, c * H);
        }
        else if (comp == 0) for (int c = 0; c < Ch; c++) planes[c] = r.Bytes(W * H);
        else throw new Exception("composite compression " + comp + " not supported");
        var rgba = new byte[W * H * 4];
        for (int i = 0; i < W * H; i++)
        {
            rgba[i * 4] = planes[0][i]; rgba[i * 4 + 1] = planes[Math.Min(1, Ch - 1)][i]; rgba[i * 4 + 2] = planes[Math.Min(2, Ch - 1)][i];
            rgba[i * 4 + 3] = Ch > 3 ? planes[3][i] : (byte)255;
        }
        return rgba;
    }
}

// ---------- effects ----------
record Stroke(double Size, string Color, double Opacity, string Position);
record Shadow(string Color, double Opacity, double Angle, double Distance, double Spread, double Blur, string Blend);
record Overlay(string Kind, string Desc);

static class Fx
{
    static IEnumerable<Dictionary<string, object>> Enabled(Layer l, string single, string multi)
    {
        if (l.Fx == null) yield break;
        if (Desc.Get(l.Fx, "masterFXSwitch") is bool on && !on) yield break;
        var list = new List<object>();
        if (Desc.Get(l.Fx, multi) is List<object> m) list.AddRange(m);
        else if (Desc.Get(l.Fx, single) is object o) list.Add(o);
        foreach (var it in list)
            if (it is Dictionary<string, object> e && !(Desc.Get(e, "enab") is bool en && !en) && !(Desc.Get(e, "present") is bool pr && !pr))
                yield return e;
    }

    public static List<Stroke> Strokes(Layer l) => Enabled(l, "FrFX", "frameFXMulti").Select(e => new Stroke(
        Desc.Num(Desc.Get(e, "Sz  ")), Desc.Color(Desc.Get(e, "Clr ")) ?? "(gradient/pattern)", Desc.Num(Desc.Get(e, "Opct")),
        (Desc.Get(e, "Styl") as string)?.TrimStart('#') switch { "OutF" => "outside", "InsF" => "inside", "CtrF" => "center", var s => s })).ToList();

    public static List<Shadow> Shadows(Layer l, int globalAngle, string key = "DrSh", string multi = "dropShadowMulti") =>
        Enabled(l, key, multi).Select(e => new Shadow(
            Desc.Color(Desc.Get(e, "Clr ")), Desc.Num(Desc.Get(e, "Opct")),
            Desc.Get(e, "uglg") is bool g && g ? globalAngle : Desc.Num(Desc.Get(e, "lagl")),
            Desc.Num(Desc.Get(e, "Dstn")), Desc.Num(Desc.Get(e, "Ckmt")), Desc.Num(Desc.Get(e, "blur")),
            (Desc.Get(e, "Md  ") as string)?.TrimStart('#'))).ToList();

    public static List<Overlay> Others(Layer l)
    {
        var o = new List<Overlay>();
        foreach (var e in Enabled(l, "SoFi", "solidFillMulti")) o.Add(new("colorOverlay", $"{Desc.Color(Desc.Get(e, "Clr "))} {Desc.Num(Desc.Get(e, "Opct")):0}%"));
        foreach (var e in Enabled(l, "GrFl", "gradientFillMulti"))
        {
            var cols = (Desc.Get(Desc.Get(e, "Grad") as Dictionary<string, object>, "Clrs") as List<object>)?
                .Select(c => c is Dictionary<string, object> cd ? $"{Desc.Color(Desc.Get(cd, "Clr "))}@{Desc.Num(Desc.Get(cd, "Lctn")) / 40.96:0}%" : "?") ?? Enumerable.Empty<string>();
            o.Add(new("gradientOverlay", $"{string.Join(" → ", cols)} angle {Desc.Num(Desc.Get(e, "Angl")):0}° {Desc.Num(Desc.Get(e, "Opct")):0}%"));
        }
        foreach (var e in Enabled(l, "IrSh", "innerShadowMulti")) o.Add(new("innerShadow", $"{Desc.Color(Desc.Get(e, "Clr "))} {Desc.Num(Desc.Get(e, "Opct")):0}% dist {Desc.Num(Desc.Get(e, "Dstn")):0} blur {Desc.Num(Desc.Get(e, "blur")):0}"));
        foreach (var e in Enabled(l, "OrGl", "outerGlowMulti")) o.Add(new("outerGlow", $"{Desc.Color(Desc.Get(e, "Clr "))} {Desc.Num(Desc.Get(e, "Opct")):0}% size {Desc.Num(Desc.Get(e, "blur")):0}"));
        foreach (var e in Enabled(l, "IrGl", "innerGlowMulti")) o.Add(new("innerGlow", $"{Desc.Color(Desc.Get(e, "Clr "))} {Desc.Num(Desc.Get(e, "Opct")):0}% size {Desc.Num(Desc.Get(e, "blur")):0}"));
        foreach (var e in Enabled(l, "ebbl", "bevelEmbossMulti")) o.Add(new("bevel", $"size {Desc.Num(Desc.Get(e, "blur")):0}"));
        return o;
    }

    /// <summary>How far effects reach beyond the layer's own pixels (left, top, right, bottom) — bounds exclude effects.</summary>
    public static (double l, double t, double r, double b) Extent(Layer layer, int globalAngle)
    {
        double s = Strokes(layer).Where(x => x.Position != "inside").Select(x => x.Position == "center" ? x.Size / 2 : x.Size).DefaultIfEmpty(0).Max();
        double L = s, T = s, R = s, B = s;
        foreach (var sh in Shadows(layer, globalAngle))
        {
            double a = sh.Angle * Math.PI / 180, dx = -Math.Cos(a) * sh.Distance, dy = Math.Sin(a) * sh.Distance, g = s + sh.Blur;
            L = Math.Max(L, g - dx); R = Math.Max(R, g + dx); T = Math.Max(T, g - dy); B = Math.Max(B, g + dy);
        }
        return (L, T, R, B);
    }
}

// ---------- action descriptors ----------
static class Desc
{
    public static Dictionary<string, object> Read(BE r)
    {
        r.Uni(); r.Id();
        uint n = r.U32();
        var d = new Dictionary<string, object>();
        for (int i = 0; i < n; i++) { var k = r.Id(); d[k] = Val(r, r.Ascii(4)); }
        return d;
    }

    public static object Get(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v : null;

    public static double Num(object v) => v switch { double d => d, int i => i, ValueTuple<string, double> u => u.Item2, _ => 0 };

    public static string Color(object v)
    {
        if (v is not Dictionary<string, object> d) return null;
        if (d.ContainsKey("Rd  ")) return $"#{B(Num(d["Rd  "]))}{B(Num(d["Grn "]))}{B(Num(d["Bl  "]))}";
        if (d.ContainsKey("redFloat")) return $"#{B(Num(d["redFloat"]) * 255)}{B(Num(d["greenFloat"]) * 255)}{B(Num(d["blueFloat"]) * 255)}";
        return null;
        static string B(double x) => ((int)Math.Round(Math.Clamp(x, 0, 255))).ToString("X2");
    }

    static object Val(BE r, string t)
    {
        switch (t)
        {
            case "Objc": case "GlbO": return Read(r);
            case "VlLs": { uint n = r.U32(); var l = new List<object>(); for (int i = 0; i < n; i++) l.Add(Val(r, r.Ascii(4))); return l; }
            case "doub": return r.F64();
            case "UntF": { var u = r.Ascii(4); return (u.Trim('#'), r.F64()); }
            case "UnFl": { var u = r.Ascii(4); uint n = r.U32(); var a = new double[n]; for (int i = 0; i < n; i++) a[i] = r.F64(); return a; }
            case "TEXT": return r.Uni();
            case "enum": { r.Id(); return "#" + r.Id(); }
            case "long": return r.I32();
            case "comp": return (long)r.U64();
            case "bool": return r.U8() != 0;
            case "type": case "GlbC": r.Uni(); return "class:" + r.Id();
            case "alis": case "Pth ": { uint n = r.U32(); r.Bytes((int)n); return "<alias>"; }
            case "tdta": { uint n = r.U32(); return r.Bytes((int)n); }
            case "obj ":
                {
                    uint n = r.U32();
                    for (int i = 0; i < n; i++)
                        switch (r.Ascii(4))
                        {
                            case "prop": r.Uni(); r.Id(); r.Id(); break;
                            case "Clss": r.Uni(); r.Id(); break;
                            case "Enmr": r.Uni(); r.Id(); r.Id(); r.Id(); break;
                            case "rele": r.Uni(); r.Id(); r.I32(); break;
                            case "Idnt": case "indx": r.I32(); break;
                            case "name": r.Uni(); r.Id(); r.Uni(); break;
                        }
                    return "<ref>";
                }
            case "ObAr":
                {
                    r.I32(); r.Uni(); r.Id(); uint n = r.U32();
                    for (int i = 0; i < n; i++) { r.Id(); r.Ascii(4); r.Ascii(4); uint c = r.U32(); for (int k = 0; k < c; k++) r.F64(); }
                    return "<objarray>";
                }
            default: throw new Exception("unknown descriptor type " + t);
        }
    }
}

// ---------- text EngineData (PostScript-like dictionary) ----------
static class Engine
{
    static int p; static byte[] s;

    public static void Fill(byte[] raw, TextInfo t)
    {
        s = raw; p = 0;
        var root = Parse();
        var ed = G(root, "EngineDict");
        var rd = G(root, "ResourceDict") ?? G(root, "DocumentResources");
        var fonts = (G(rd, "FontSet") as List<object>)?.Select(f => G(f, "Name") as string ?? "?").ToList() ?? new();
        var text = G(G(ed, "Editor"), "Text") as string ?? t.Text;
        var sr = G(ed, "StyleRun");
        var runs = G(sr, "RunArray") as List<object>;
        var lens = G(sr, "RunLengthArray") as List<object>;
        int pos = 0;
        for (int i = 0; runs != null && i < runs.Count; i++)
        {
            var sd = G(G(runs[i], "StyleSheet"), "StyleSheetData");
            int len = lens != null && i < lens.Count ? Convert.ToInt32(lens[i]) : 0;
            var seg = pos + len <= text.Length ? text.Substring(pos, len) : "";
            pos += len;
            if (seg.Trim('\r', '\n', ' ', '\u0003').Length == 0) continue;
            var run = new TextRun { Text = seg.TrimEnd('\r', '\n', '\u0003') };
            if (G(sd, "Font") is double fi && fi < fonts.Count) run.Font = fonts[(int)fi];
            if (G(sd, "FontSize") is double fz) run.SizePx = fz * t.Scale;
            if (G(G(sd, "FillColor"), "Values") is List<object> cv && cv.Count == 4)
                run.Color = $"#{C(cv[1])}{C(cv[2])}{C(cv[3])}" + (Convert.ToDouble(cv[0]) < 0.999 ? C(cv[0]) : "");
            if (G(sd, "Tracking") is double tr) run.Tracking = tr;
            if (G(sd, "Leading") is double le) run.Leading = le * t.Scale;
            if (G(sd, "AutoLeading") is bool al) run.AutoLeading = al;
            if (G(sd, "HorizontalScale") is double hs) run.HScale = hs;
            if (G(sd, "VerticalScale") is double vs) run.VScale = vs;
            if (G(sd, "BaselineShift") is double bs) run.Baseline = bs * t.Scale;
            if (G(sd, "FauxBold") is bool fb) run.FauxBold = fb;
            if (G(sd, "FauxItalic") is bool fit) run.FauxItalic = fit;
            if (G(sd, "FontCaps") is double cp) run.Caps = (int)cp;
            t.Runs.Add(run);
        }
        var pr = G(G(ed, "ParagraphRun"), "RunArray") as List<object>;
        if (pr != null && pr.Count > 0 && G(G(G(pr[0], "ParagraphSheet"), "Properties"), "Justification") is double j)
            t.Justify = j switch { 0 => "left", 1 => "right", 2 => "center", _ => "justify" };
        var shape = G(G(G(ed, "Rendered"), "Shapes"), "Children") as List<object>;
        if (shape != null && shape.Count > 0)
        {
            var st = G(G(shape[0], "Cookie"), "Photoshop");
            if (G(st, "ShapeType") is double sh && sh == 1) t.Kind = "box";
            if (G(st, "BoxBounds") is List<object> bb) t.Box = bb.Select(x => Convert.ToDouble(x) * t.Scale).ToArray();
        }
    }

    static string C(object v) => ((int)Math.Round(Math.Clamp(Convert.ToDouble(v), 0, 1) * 255)).ToString("X2");
    static object G(object o, string k) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) ? v : null;
    static void Ws() { while (p < s.Length && s[p] <= 32) p++; }

    static object Parse()
    {
        Ws();
        if (p >= s.Length) return null;
        if (s[p] == '<' && p + 1 < s.Length && s[p + 1] == '<')
        {
            p += 2; var d = new Dictionary<string, object>();
            while (p < s.Length)
            {
                Ws();
                if (s[p] == '>' && s[p + 1] == '>') { p += 2; return d; }
                if (s[p] != '/') { p++; continue; }
                p++; d[Token()] = Parse();
            }
            return d;
        }
        if (s[p] == '[')
        {
            p++; var l = new List<object>();
            while (p < s.Length) { Ws(); if (s[p] == ']') { p++; return l; } l.Add(Parse()); }
            return l;
        }
        if (s[p] == '(')
        {
            p++; var bytes = new List<byte>();
            while (p < s.Length && s[p] != ')')
            {
                if (s[p] == '\\') { p++; byte c = s[p++]; bytes.Add(c switch { (byte)'n' => (byte)'\n', (byte)'r' => (byte)'\r', (byte)'t' => (byte)'\t', _ => c }); }
                else bytes.Add(s[p++]);
            }
            p++;
            var a = bytes.ToArray();
            return a.Length >= 2 && a[0] == 0xFE && a[1] == 0xFF ? Encoding.BigEndianUnicode.GetString(a, 2, a.Length - 2) : Encoding.Latin1.GetString(a);
        }
        if (s[p] == '/') { p++; return "/" + Token(); }
        var t = Token();
        if (t.Length == 0) { p++; return null; }
        if (t == "true") return true;
        if (t == "false") return false;
        return double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : t;
    }

    static string Token()
    {
        int st = p;
        while (p < s.Length && s[p] > 32 && s[p] != '/' && s[p] != '[' && s[p] != ']' && s[p] != '(' && s[p] != '<' && s[p] != '>') p++;
        return Encoding.ASCII.GetString(s, st, p - st);
    }
}

static class Png
{
    static readonly uint[] T = Enumerable.Range(0, 256).Select(n => { uint c = (uint)n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; return c; }).ToArray();
    static uint Crc(byte[] b) { uint c = 0xFFFFFFFF; foreach (var x in b) c = T[(c ^ x) & 0xFF] ^ (c >> 8); return c ^ 0xFFFFFFFF; }
    static void Chunk(Stream o, string type, byte[] data)
    {
        var len = BitConverter.GetBytes(data.Length); Array.Reverse(len); o.Write(len);
        var td = Encoding.ASCII.GetBytes(type).Concat(data).ToArray(); o.Write(td);
        var crc = BitConverter.GetBytes(Crc(td)); Array.Reverse(crc); o.Write(crc);
    }
    public static void Write(string file, int w, int h, byte[] rgba)
    {
        using var o = File.Create(file);
        o.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ih = new byte[13];
        ih[0] = (byte)(w >> 24); ih[1] = (byte)(w >> 16); ih[2] = (byte)(w >> 8); ih[3] = (byte)w;
        ih[4] = (byte)(h >> 24); ih[5] = (byte)(h >> 16); ih[6] = (byte)(h >> 8); ih[7] = (byte)h;
        ih[8] = 8; ih[9] = 6;
        Chunk(o, "IHDR", ih);
        using var ms = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
            for (int y = 0; y < h; y++) { z.WriteByte(0); z.Write(rgba, y * w * 4, w * 4); }
        Chunk(o, "IDAT", ms.ToArray());
        Chunk(o, "IEND", Array.Empty<byte>());
    }

    /// <summary>Opaque-ink bounds of an 8-bit non-interlaced PNG: (left, top, right, bottom) margins in px, alpha > 8.</summary>
    public static (int l, int t, int r, int b)? Trim(string file)
    {
        try
        {
            var d = File.ReadAllBytes(file);
            int p = 8, w = 0, h = 0, ct = 0, depth = 0, inter = 0;
            byte[] trns = null; var idat = new MemoryStream();
            while (p + 8 <= d.Length)
            {
                int len = d[p] << 24 | d[p + 1] << 16 | d[p + 2] << 8 | d[p + 3];
                string type = Encoding.ASCII.GetString(d, p + 4, 4);
                int s = p + 8;
                if (type == "IHDR") { w = d[s] << 24 | d[s + 1] << 16 | d[s + 2] << 8 | d[s + 3]; h = d[s + 4] << 24 | d[s + 5] << 16 | d[s + 6] << 8 | d[s + 7]; depth = d[s + 8]; ct = d[s + 9]; inter = d[s + 12]; }
                else if (type == "tRNS") trns = d[s..(s + len)];
                else if (type == "IDAT") idat.Write(d, s, len);
                else if (type == "IEND") break;
                p = s + len + 4;
            }
            if (depth != 8 || inter != 0) return null;
            int bpp = ct switch { 6 => 4, 2 => 3, 4 => 2, 0 => 1, 3 => 1, _ => 0 };
            if (bpp == 0) return null;
            idat.Position = 0;
            using var z = new System.IO.Compression.ZLibStream(idat, System.IO.Compression.CompressionMode.Decompress);
            int stride = w * bpp;
            var prev = new byte[stride]; var cur = new byte[stride];
            int L = w, T = h, R = -1, B = -1;
            for (int y = 0; y < h; y++)
            {
                int f = z.ReadByte(); z.ReadExactly(cur, 0, stride);
                for (int i = 0; i < stride; i++)
                {
                    int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                    int pr = f switch { 1 => a, 2 => b, 3 => (a + b) / 2, 4 => Paeth(a, b, c), _ => 0 };
                    cur[i] = (byte)(cur[i] + pr);
                }
                for (int x = 0; x < w; x++)
                {
                    int alpha = ct switch { 6 => cur[x * 4 + 3], 4 => cur[x * 2 + 1], 3 => trns != null && cur[x] < trns.Length ? trns[cur[x]] : 255, _ => 255 };
                    if (alpha > 8) { if (x < L) L = x; if (x > R) R = x; if (y < T) T = y; if (y > B) B = y; }
                }
                (prev, cur) = (cur, prev);
            }
            return R < 0 ? null : (L, T, w - 1 - R, h - 1 - B);
        }
        catch { return null; }
        static int Paeth(int a, int b, int c) { int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c); return pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
    }

    public static (int w, int h)? Size(string file)
    {
        try
        {
            using var f = File.OpenRead(file);
            var b = new byte[24]; if (f.Read(b, 0, 24) < 24 || b[1] != 'P') return null;
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }
        catch { return null; }
    }
}
