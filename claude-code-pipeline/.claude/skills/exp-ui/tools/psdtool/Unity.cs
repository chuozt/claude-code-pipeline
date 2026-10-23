using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// Unity-side readers: guid index, TTF/OTF name+cmap, TMP font assets, TMP materials, prefab YAML.

class Project
{
    public string Root;
    public Dictionary<string, string> GuidToPath = new(StringComparer.OrdinalIgnoreCase);

    public Project(string root)
    {
        Root = root;
        foreach (var meta in Directory.EnumerateFiles(Path.Combine(root, "Assets"), "*.meta", SearchOption.AllDirectories))
        {
            using var sr = new StreamReader(meta);
            for (int i = 0; i < 3; i++)
            {
                var line = sr.ReadLine(); if (line == null) break;
                if (line.StartsWith("guid: ")) { GuidToPath[line[6..].Trim()] = meta[..^5]; break; }
            }
        }
    }

    public string Rel(string p) => p == null ? "?" : Path.GetRelativePath(Root, p).Replace('\\', '/');
    public string ByGuid(string g) => g != null && GuidToPath.TryGetValue(g, out var p) ? p : null;
}

// ---------- fonts ----------
class FontFile
{
    public string Path, PostScript, Family, Sub, Full;
    public HashSet<int> Cmap = new();

    public static FontFile Read(string path)
    {
        var f = new FontFile { Path = path };
        var d = File.ReadAllBytes(path);
        int U16(int o) => d[o] << 8 | d[o + 1];
        uint U32(int o) => (uint)(d[o] << 24 | d[o + 1] << 16 | d[o + 2] << 8 | d[o + 3]);
        int n = U16(4);
        var tables = new Dictionary<string, int>();
        for (int i = 0; i < n; i++) { int r = 12 + i * 16; tables[Encoding.ASCII.GetString(d, r, 4)] = (int)U32(r + 8); }
        if (tables.TryGetValue("name", out int nt))
        {
            int count = U16(nt + 2), so = nt + U16(nt + 4);
            for (int i = 0; i < count; i++)
            {
                int r = nt + 6 + i * 12, pid = U16(r), nid = U16(r + 6), len = U16(r + 8), off = U16(r + 10);
                string v = pid == 3 || pid == 0 ? Encoding.BigEndianUnicode.GetString(d, so + off, len) : Encoding.Latin1.GetString(d, so + off, len);
                if (nid == 6 && (f.PostScript == null || pid == 3)) f.PostScript = v;
                if (nid == 1 && (f.Family == null || pid == 3)) f.Family = v;
                if (nid == 2 && (f.Sub == null || pid == 3)) f.Sub = v;
                if (nid == 4 && (f.Full == null || pid == 3)) f.Full = v;
            }
        }
        if (tables.TryGetValue("cmap", out int ct))
        {
            int num = U16(ct + 2);
            for (int i = 0; i < num; i++)
            {
                int pid = U16(ct + 4 + i * 8), eid = U16(ct + 6 + i * 8), so = ct + (int)U32(ct + 8 + i * 8);
                int fmt = U16(so);
                if (fmt == 4 && pid == 3)
                {
                    int segX2 = U16(so + 6), ends = so + 14, starts = ends + segX2 + 2, deltas = starts + segX2, ranges = deltas + segX2;
                    for (int s = 0; s < segX2 / 2; s++)
                    {
                        int end = U16(ends + s * 2), start = U16(starts + s * 2), delta = (short)U16(deltas + s * 2), ro = U16(ranges + s * 2);
                        for (int c = start; c <= end && c != 0xFFFF; c++)
                        {
                            int g = ro == 0 ? (c + delta) & 0xFFFF : U16(ranges + s * 2 + ro + (c - start) * 2);
                            if (g != 0) f.Cmap.Add(c);
                        }
                    }
                }
                else if (fmt == 12)
                {
                    uint groups = U32(so + 12);
                    for (int g = 0; g < groups; g++) { uint a = U32(so + 16 + g * 12), b = U32(so + 20 + g * 12); for (uint c = a; c <= b && c < 0x110000; c++) f.Cmap.Add((int)c); }
                }
            }
        }
        return f;
    }

    static string Norm(string s) => Regex.Replace(s ?? "", "[^a-z0-9]", "", RegexOptions.IgnoreCase).ToLowerInvariant();
    public bool Matches(string psName) => Norm(PostScript) == Norm(psName) || Norm(Full) == Norm(psName) || Norm(Family + Sub) == Norm(psName);
}

class TmpFont
{
    public string Path, Guid, SourceGuid, Family, Style;
    public double PointSize = 90, Padding = 9;
    public int Mode; // 0 static, 1 dynamic
    public HashSet<int> Chars = new();

    public static TmpFont TryRead(string path, Project pr)
    {
        var fi = new FileInfo(path);
        if (fi.Length > 60_000_000) return null;
        var txt = File.ReadAllText(path);
        if (!txt.Contains("m_AtlasPadding")) return null;
        var t = new TmpFont { Path = path, Guid = pr.GuidToPath.FirstOrDefault(kv => kv.Value == path).Key };
        t.SourceGuid = M(txt, @"m_SourceFontFileGUID: (\w+)");
        t.Family = M(txt, @"m_FamilyName: (.*)"); t.Style = M(txt, @"m_StyleName: (.*)");
        if (double.TryParse(M(txt, @"m_PointSize: ([\d.]+)"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ps)) t.PointSize = ps;
        if (double.TryParse(M(txt, @"m_AtlasPadding: ([\d.]+)"), NumberStyles.Float, CultureInfo.InvariantCulture, out var pd)) t.Padding = pd;
        t.Mode = int.TryParse(M(txt, @"m_AtlasPopulationMode: (\d+)"), out var m) ? m : 0;
        foreach (Match u in Regex.Matches(txt, @"m_Unicode: (\d+)")) t.Chars.Add(int.Parse(u.Groups[1].Value));
        return t;
        static string M(string s, string re) { var m = Regex.Match(s, re); return m.Success ? m.Groups[1].Value.Trim() : null; }
    }
}

class TmpMat
{
    public string Path, Name, AtlasGuid;
    public Dictionary<string, double> F = new();
    public Dictionary<string, double[]> C = new();
    public bool Underlay;

    public static TmpMat TryRead(string path)
    {
        var txt = File.ReadAllText(path);
        if (!txt.Contains("_OutlineWidth") || !txt.Contains("_GradientScale")) return null;
        var m = new TmpMat { Path = path, Name = System.IO.Path.GetFileNameWithoutExtension(path) };
        var mt = Regex.Match(txt, @"- _MainTex:\s*\n\s*m_Texture: \{fileID: -?\d+, guid: (\w+)");
        m.AtlasGuid = mt.Success ? mt.Groups[1].Value : null;
        foreach (Match f in Regex.Matches(txt, @"^\s*- (_\w+): (-?[\d.eE+-]+)\s*$", RegexOptions.Multiline))
            m.F[f.Groups[1].Value] = double.Parse(f.Groups[2].Value, CultureInfo.InvariantCulture);
        foreach (Match c in Regex.Matches(txt, @"^\s*- (_\w+): \{r: ([-\d.eE]+), g: ([-\d.eE]+), b: ([-\d.eE]+), a: ([-\d.eE]+)\}", RegexOptions.Multiline))
            m.C[c.Groups[1].Value] = Enumerable.Range(2, 4).Select(i => double.Parse(c.Groups[i].Value, CultureInfo.InvariantCulture)).ToArray();
        m.Underlay = Regex.IsMatch(txt, @"UNDERLAY_ON|UNDERLAY_INNER");
        return m;
    }

    public double Get(string k, double def = 0) => F.TryGetValue(k, out var v) ? v : def;
    public string Hex(string k) => C.TryGetValue(k, out var c) ? Tmp.Hex(c) : null;
}

/// <summary>
/// Photoshop text effects → TMP SDF material values, from the TMP 3.2 / Unity 6 source:
/// TMP_ShaderUtilities.UpdateShaderRatios + GetPadding and TMP_SDF.shader. With sd measured in screen
/// pixels, one material unit moves an edge by (ratio · GradientScale) atlas pixels, and one atlas pixel
/// is (fontSize / samplingPointSize) canvas units. Outline is centred on the (dilated) glyph edge, so a
/// Photoshop OUTSIDE stroke of S px is OutlineWidth = FaceDilate = S / (2·ratioA·GS·r).
/// </summary>
static class Tmp
{
    public static string Hex(double[] c) => $"#{B(c[0])}{B(c[1])}{B(c[2])}" + (c.Length > 3 && c[3] < 0.999 ? B(c[3]) : "");
    static string B(double x) => ((int)Math.Round(Math.Clamp(x, 0, 1) * 255)).ToString("X2");

    public static double[] Rgba(string hex, double alpha = 1)
    {
        hex = hex.TrimStart('#');
        double P(int i) => Convert.ToInt32(hex.Substring(i, 2), 16) / 255.0;
        return new[] { P(0), P(2), P(4), hex.Length >= 8 ? P(6) : alpha };
    }

    public class Target
    {
        public double Outline, Dilate, OffX, OffY, UDilate, USoft, RatioA, RatioC, UnderlayBudget, UnderlayNeed, WeightBold = 0.75;
        public string OutlineColor, UnderlayColor;
        public bool HasUnderlay, Clamped;
        public string Note = "";
    }

    public static Target Solve(double fontPx, Stroke stroke, Shadow shadow, TmpFont font, double weightBold = 0.75)
    {
        var t = SolveAt(fontPx, stroke, shadow, font, weightBold);
        if (!t.Clamped || weightBold == 0) return t;
        // _WeightBold eats the underlay budget even for regular text; zero it if that is what makes the style fit.
        var z = SolveAt(fontPx, stroke, shadow, font, 0);
        if (z.Clamped) return t;
        z.Note += "_WeightBold set to 0 to free atlas padding (<b> no longer thickens this preset); ";
        return z;
    }

    static Target SolveAt(double fontPx, Stroke stroke, Shadow shadow, TmpFont font, double weightBold)
    {
        double gs = font.Padding + 1, r = fontPx / font.PointSize, weight = weightBold / 4;
        var t = new Target { WeightBold = weightBold };
        double S = stroke == null ? 0 : stroke.Position switch { "center" => stroke.Size / 2, "inside" => 0, _ => stroke.Size };
        // Ratio A depends on the values it scales — iterate to a fixed point.
        double ra = (gs - 1) / gs, w = 0;
        for (int i = 0; i < 20; i++)
        {
            w = S / (2 * ra * gs * r);
            double tt = Math.Max(1, weight + 2 * w);
            ra = (gs - 1) / (gs * tt);
        }
        t.Outline = w; t.Dilate = w; t.RatioA = ra;
        if (weight + 2 * w > 1) { t.Clamped = true; t.Note += $"stroke {S:0.#}px exceeds the outline budget at this size; "; }
        if (stroke != null) t.OutlineColor = stroke.Color + (stroke.Opacity < 99.5 ? ((int)Math.Round(stroke.Opacity * 2.55)).ToString("X2") : "");
        if (stroke != null && stroke.Position == "inside") t.Note += "inside stroke approximated as none; ";
        if (shadow != null)
        {
            t.HasUnderlay = true;
            t.UnderlayColor = shadow.Color + ((int)Math.Round(shadow.Opacity * 2.55)).ToString("X2");
            double a = shadow.Angle * Math.PI / 180, dx = -Math.Cos(a) * shadow.Distance, dyUp = -Math.Sin(a) * shadow.Distance;
            // Ratio C = max(0, GS-1-range) / (GS·t), t = max(1, max|offset| + dilate + softness): the underlay
            // has a fixed budget of (GS-1-range)·r canvas px for offset + dilate + softness together.
            // Past it, raising a value lowers the ratio in step — there is no solution, so clamp.
            double range = (weight + t.Dilate) * (gs - 1);
            double budget = Math.Max(0, gs - 1 - range) * r;
            double spread = shadow.Blur * shadow.Spread / 100.0;
            double nOff = Math.Max(Math.Abs(dx), Math.Abs(dyUp));
            double nDil = S / 2 + spread;                 // the underlay already inherits the face dilate (S/2)
            double nSoft = Math.Max(0, shadow.Blur - spread) / 2;
            t.UnderlayBudget = budget; t.UnderlayNeed = nOff + nDil + nSoft;
            if (t.UnderlayNeed > budget)
            {
                t.Clamped = true;
                t.Note += $"underlay needs {t.UnderlayNeed:0.#}px of a {budget:0.#}px budget at this size — softness dropped first, then dilate; ";
                double left = budget;
                double off = Math.Min(nOff, left); left -= off;
                double dil = Math.Min(nDil, left); left -= dil;
                double soft = Math.Min(nSoft, left);
                double k = nOff > 0 ? off / nOff : 1; dx *= k; dyUp *= k; nDil = dil; nSoft = soft;
            }
            double unit = Math.Max(1e-6, budget); // canvas px per material unit with t = 1
            t.OffX = dx / unit; t.OffY = dyUp / unit; t.UDilate = nDil / unit; t.USoft = nSoft / unit;
            t.RatioC = Math.Max(0, gs - 1 - range) / gs;
        }
        return t;
    }
}

// ---------- prefab YAML ----------
class UObj
{
    public int Cls; public long Id; public bool Stripped;
    public Dictionary<string, string> KV = new();
    public List<long> Children = new();
    public List<(long target, string targetGuid, string path, string value, string objRef)> Mods = new();
    public string Get(string k) => KV.TryGetValue(k, out var v) ? v : null;
}

class UNode
{
    public string Name = "", Path = "";
    public bool Active = true, ActiveInHierarchy = true;
    public double X, Y, W, H; // canvas rect, PSD-style (top-left origin, y down)
    public double Sx = 1, Sy = 1;
    public string Kind = "";
    public string Sprite, SpritePath, ImageType; public (int w, int h)? Native; public (int l, int t, int r, int b)? Trim;
    /// <summary>Rect of the sprite's opaque ink (transparent margins removed), canvas px.</summary>
    public (double x, double y, double w, double h) Ink
    {
        get
        {
            if (Native is not (int nw, int nh) || Trim is not (int l, int t, int r, int b) || nw == 0 || nh == 0) return (X, Y, W, H);
            double kx = ImageType == "Sliced" ? Math.Abs(Sx) : W / nw, ky = ImageType == "Sliced" ? Math.Abs(Sy) : H / nh;
            return (X + l * kx, Y + t * ky, W - (l + r) * kx, H - (t + b) * ky);
        }
    }
    public string Text, FontAsset, Material; public double FontSize; public string Color; public bool AutoSize;
    public string HAlign, VAlign; public double CharSpacing;
    public string Fitter; public List<string> Scripts = new(); public UNode Parent; public bool PreserveAspect; public string ColorTint;
    public bool UnderSafeArea { get { for (var p = this; p != null; p = p.Parent) if (p.Scripts.Contains("SafeArea")) return true; return false; } }
    public bool Raycast;
    public List<UNode> Kids = new();
}

static class PrefabReader
{
    static readonly Regex Hdr = new(@"^--- !u!(\d+) &(-?\d+)( stripped)?");

    public static Dictionary<long, UObj> Parse(string file)
    {
        var objs = new Dictionary<long, UObj>();
        UObj cur = null; string listKey = null; string lastKey = null;
        (long, string, string, string, string) mod = default; bool inMod = false;
        foreach (var raw in File.ReadLines(file))
        {
            var m = Hdr.Match(raw);
            if (m.Success) { cur = new UObj { Cls = int.Parse(m.Groups[1].Value), Id = long.Parse(m.Groups[2].Value), Stripped = m.Groups[3].Success }; objs[cur.Id] = cur; listKey = null; continue; }
            if (cur == null || raw.Length == 0) continue;
            if (!raw.StartsWith(" ")) continue; // class name line
            var line = raw;
            int ind = line.Length - line.TrimStart().Length;
            var tl = line.Trim();
            if (cur.Cls == 1001)
            {
                if (tl.StartsWith("- target:")) { if (inMod) cur.Mods.Add(mod); inMod = true; var mm = Regex.Match(tl, @"fileID: (-?\d+), guid: (\w+)"); mod = (long.Parse(mm.Groups[1].Value), mm.Groups[2].Value, null, null, null); continue; }
                if (inMod && tl.StartsWith("propertyPath:")) { mod.Item3 = tl[13..].Trim(); continue; }
                if (inMod && tl.StartsWith("value:")) { mod.Item4 = tl[6..].Trim(); continue; }
                if (inMod && tl.StartsWith("objectReference:")) { mod.Item5 = tl[16..].Trim(); cur.Mods.Add(mod); inMod = false; continue; }
            }
            if (ind > 2 && lastKey != null && cur.KV.TryGetValue(lastKey, out var open) && (open.StartsWith("\"") || open.StartsWith("'")) && !Closed(open))
            {
                cur.KV[lastKey] = open + (open.EndsWith("\\") ? "" : " ") + tl; // YAML folds a line break into one space
                continue;
            }
            // a block-list item ("  - {fileID: …}") also contains ':' — handle lists before keys
            if (tl.StartsWith("- ") && listKey != null)
            {
                if (listKey == "m_Children" && tl.StartsWith("- {fileID:")) cur.Children.Add(long.Parse(Regex.Match(tl, @"fileID: (-?\d+)").Groups[1].Value));
                continue;
            }
            if (ind == 2 && tl.Contains(':'))
            {
                int c = tl.IndexOf(':');
                lastKey = tl[..c]; var val = tl[(c + 1)..].Trim();
                cur.KV[lastKey] = val;
                listKey = val.Length == 0 ? lastKey : null;
                continue;
            }
            if (ind == 4 && lastKey != null && cur.KV.ContainsKey(lastKey) && !tl.StartsWith("-")) // nested map like m_TransformParent under m_Modification
            {
                int c = tl.IndexOf(':'); if (c > 0) cur.KV[lastKey + "." + tl[..c]] = tl[(c + 1)..].Trim();
            }
        }
        if (inMod && cur != null) cur.Mods.Add(mod);
        return objs;
    }

    static bool Closed(string v)
    {
        char q = v[0];
        if (v.Length < 2) return false;
        if (q == '\'') return v.EndsWith("'") && (v.Length - v.TrimEnd('\'').Length) % 2 == 1;
        int bs = 0; for (int i = v.Length - 2; i >= 0 && v[i] == '\\'; i--) bs++;
        return v.EndsWith("\"") && bs % 2 == 0;
    }

    public static double[] Vec(string v)
    {
        if (v == null) return null;
        var ms = Regex.Matches(v, @"[xyzwrgba]: (-?[\d.eE+-]+)");
        return ms.Select(m => double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToArray();
    }

    public static long Ref(string v) { var m = Regex.Match(v ?? "", @"fileID: (-?\d+)"); return m.Success ? long.Parse(m.Groups[1].Value) : 0; }
    public static string RefGuid(string v) { var m = Regex.Match(v ?? "", @"guid: (\w+)"); return m.Success ? m.Groups[1].Value : null; }

    public static string Unquote(string v)
    {
        if (v == null) return null;
        v = v.Trim();
        if (v.StartsWith("\"") && v.EndsWith("\"") && v.Length >= 2) v = Regex.Unescape(v[1..^1]);
        else if (v.StartsWith("'") && v.EndsWith("'") && v.Length >= 2) v = v[1..^1].Replace("''", "'");
        return v;
    }
}

/// <summary>Builds the RectTransform tree of a prefab and resolves every rect on a reference canvas.</summary>
class PrefabLayout
{
    readonly Project pr;
    readonly double cw, ch;
    public UNode Root;
    public List<UNode> All = new();

    public PrefabLayout(Project pr, string prefab, double canvasW, double canvasH)
    {
        this.pr = pr; cw = canvasW; ch = canvasH;
        var objs = PrefabReader.Parse(prefab);
        var rts = objs.Values.Where(o => o.Cls == 224 && !o.Stripped).ToList();
        var root = rts.FirstOrDefault(o => PrefabReader.Ref(o.Get("m_Father")) == 0);
        if (root == null) throw new Exception("no root RectTransform");
        // Local frame: origin at canvas bottom-left, y up.
        Root = Build(objs, root, new Frame(0, 0, 1, 1), new Rect(0, 0, cw, ch), "", true, null);
    }

    record Frame(double Ox, double Oy, double Sx, double Sy) { public (double, double) Map(double x, double y) => (Ox + x * Sx, Oy + y * Sy); }
    record Rect(double X, double Y, double W, double H);

    UNode Build(Dictionary<long, UObj> objs, UObj rt, Frame parentFrame, Rect parentRect, string parentPath, bool parentActive, Dictionary<long, Dictionary<string, string>> overrides)
    {
        var go = objs.TryGetValue(PrefabReader.Ref(rt.Get("m_GameObject")), out var g) ? g : null;
        string O(UObj o, string k) => overrides != null && o != null && overrides.TryGetValue(o.Id, out var ov) && ov.TryGetValue(k, out var v) ? v : o?.Get(k);
        double V(UObj o, string k, int i, double def)
        {
            string axis = "xyzw"[i].ToString();
            if (overrides != null && overrides.TryGetValue(o.Id, out var ov) && ov.TryGetValue(k + "." + axis, out var s) && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
            var a = PrefabReader.Vec(o.Get(k)); return a != null && a.Length > i ? a[i] : def;
        }
        var n = new UNode { Name = PrefabReader.Unquote(O(go, "m_Name")) ?? "?" };
        n.Path = parentPath.Length == 0 ? n.Name : parentPath + "/" + n.Name;
        n.Active = O(go, "m_IsActive") != "0";
        n.ActiveInHierarchy = parentActive && n.Active;
        double aminx = V(rt, "m_AnchorMin", 0, .5), aminy = V(rt, "m_AnchorMin", 1, .5), amaxx = V(rt, "m_AnchorMax", 0, .5), amaxy = V(rt, "m_AnchorMax", 1, .5);
        double px = V(rt, "m_Pivot", 0, .5), py = V(rt, "m_Pivot", 1, .5);
        double apx = V(rt, "m_AnchoredPosition", 0, 0), apy = V(rt, "m_AnchoredPosition", 1, 0);
        double sdx = V(rt, "m_SizeDelta", 0, 100), sdy = V(rt, "m_SizeDelta", 1, 100);
        double sx = V(rt, "m_LocalScale", 0, 1), sy = V(rt, "m_LocalScale", 1, 1);
        double ax0 = parentRect.X + aminx * parentRect.W, ay0 = parentRect.Y + aminy * parentRect.H;
        double ax1 = parentRect.X + amaxx * parentRect.W, ay1 = parentRect.Y + amaxy * parentRect.H;
        double w = ax1 - ax0 + sdx, h = ay1 - ay0 + sdy;
        double pivX = ax0 + (ax1 - ax0) * px + apx, pivY = ay0 + (ay1 - ay0) * py + apy;
        // AspectRatioFitter drives the rect at runtime: 1 WidthControlsHeight, 2 HeightControlsWidth,
        // 3 FitInParent, 4 EnvelopeParent (the last two stretch to the parent and zero the position).
        var fit = go == null ? null : objs.Values.FirstOrDefault(o => o.Cls == 114 && PrefabReader.Ref(o.Get("m_GameObject")) == go.Id && o.KV.ContainsKey("m_AspectMode"));
        if (fit != null && O(fit, "m_Enabled") != "0" && int.TryParse(O(fit, "m_AspectMode"), out var mode) && mode != 0
            && double.TryParse(O(fit, "m_AspectRatio"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ratio) && ratio > 0)
        {
            n.Fitter = mode switch { 1 => "WidthControlsHeight", 2 => "HeightControlsWidth", 3 => "FitInParent", 4 => "EnvelopeParent", _ => "?" } + $" {ratio:0.###}";
            if (mode == 1) h = w / ratio;
            else if (mode == 2) w = h * ratio;
            else if (mode is 3 or 4)
            {
                double pw = parentRect.W, ph = parentRect.H;
                bool wide = pw / ph > ratio;
                (w, h) = (mode == 3) == wide ? (ph * ratio, ph) : (pw, pw / ratio);
                pivX = parentRect.X + pw * px; pivY = parentRect.Y + ph * py;
            }
        }
        var (ox, oy) = parentFrame.Map(pivX, pivY);
        var frame = new Frame(ox, oy, parentFrame.Sx * sx, parentFrame.Sy * sy);
        var local = new Rect(-w * px, -h * py, w, h);
        var (x0, y0) = frame.Map(local.X, local.Y);
        var (x1, y1) = frame.Map(local.X + w, local.Y + h);
        n.X = Math.Min(x0, x1); n.W = Math.Abs(x1 - x0);
        n.H = Math.Abs(y1 - y0); n.Y = ch - Math.Max(y0, y1);
        n.Sx = frame.Sx; n.Sy = frame.Sy;
        if (go != null) ReadComponents(objs, go, n, overrides);
        All.Add(n);
        foreach (var cid in rt.Children)
        {
            if (!objs.TryGetValue(cid, out var c)) continue;
            if (c.Stripped) { n.Kids.AddRange(Nested(objs, c, frame, local, n.Path, n.ActiveInHierarchy, overrides)); continue; }
            n.Kids.Add(Build(objs, c, frame, local, n.Path, n.ActiveInHierarchy, overrides));
        }
        // nested prefab instances whose stripped transform is not listed in m_Children
        foreach (var pi in objs.Values.Where(o => o.Cls == 1001 && PrefabReader.Ref(o.Get("m_Modification.m_TransformParent")) == rt.Id))
            if (!rt.Children.Any(cid => objs.TryGetValue(cid, out var c) && c.Stripped && PrefabReader.Ref(c.Get("m_PrefabInstance")) == pi.Id))
                n.Kids.Add(Instance(pi, frame, local, n.Path, n.ActiveInHierarchy, overrides));
        foreach (var k in n.Kids) k.Parent = n;
        return n;
    }

    IEnumerable<UNode> Nested(Dictionary<long, UObj> objs, UObj stripped, Frame frame, Rect parentRect, string path, bool active, Dictionary<long, Dictionary<string, string>> outer)
    {
        var pi = objs.TryGetValue(PrefabReader.Ref(stripped.Get("m_PrefabInstance")), out var p) ? p : null;
        if (pi != null) yield return Instance(pi, frame, parentRect, path, active, outer);
    }

    /// <summary>
    /// Expands a nested prefab instance (or a variant's base). Overrides are re-keyed into the source file's
    /// fileIDs: an object seen through instance PI has fileID (sourceId ^ PI.id) &amp; 0x7FFF…, so a
    /// modification from further out is translated back with the same XOR. Outer modifications win.
    /// </summary>
    UNode Instance(UObj pi, Frame frame, Rect parentRect, string path, bool active, Dictionary<long, Dictionary<string, string>> outer, int depth = 0)
    {
        var src = pr.ByGuid(PrefabReader.RefGuid(pi.Get("m_SourcePrefab")));
        if (src == null || !File.Exists(src) || depth > 6) return new UNode { Name = "(nested prefab not resolved)", Path = path + "/?" };
        var objs = PrefabReader.Parse(src);
        var ov = new Dictionary<long, Dictionary<string, string>>();
        void Put(long id, string k, string v) { if (!ov.TryGetValue(id, out var d)) ov[id] = d = new(); d[k] = v; }
        foreach (var m in pi.Mods.Where(m => m.path != null)) Put(m.target, m.path, m.objRef != null && m.objRef.Contains("guid") ? m.objRef : m.value);
        if (outer != null)
            foreach (var (k, d) in outer)
                foreach (var (p, v) in d) Put((k ^ pi.Id) & 0x7FFFFFFFFFFFFFFF, p, v);
        UNode node;
        var root = objs.Values.FirstOrDefault(o => o.Cls == 224 && !o.Stripped && PrefabReader.Ref(o.Get("m_Father")) == 0);
        if (root != null) node = Build(objs, root, frame, parentRect, path, active, ov);
        else
        {
            // a prefab variant: its root comes from the base prefab's instance
            var basePi = objs.Values.FirstOrDefault(o => o.Cls == 1001 && PrefabReader.Ref(o.Get("m_Modification.m_TransformParent")) == 0);
            if (basePi == null) return new UNode { Name = "(nested prefab not resolved)", Path = path + "/?" };
            node = Instance(basePi, frame, parentRect, path, active, ov, depth + 1);
        }
        node.Name += " [prefab " + System.IO.Path.GetFileNameWithoutExtension(src) + "]";
        return node;
    }

    void ReadComponents(Dictionary<long, UObj> objs, UObj go, UNode n, Dictionary<long, Dictionary<string, string>> ov)
    {
        string O(UObj o, string k) => ov != null && ov.TryGetValue(o.Id, out var d) && d.TryGetValue(k, out var v) ? v : o.Get(k);
        foreach (var comp in objs.Values.Where(o => o.Cls == 114 && PrefabReader.Ref(o.Get("m_GameObject")) == go.Id))
            if (pr.ByGuid(PrefabReader.RefGuid(comp.Get("m_Script"))) is string sp) n.Scripts.Add(System.IO.Path.GetFileNameWithoutExtension(sp));
        foreach (var comp in objs.Values.Where(o => o.Cls == 114 && PrefabReader.Ref(o.Get("m_GameObject")) == go.Id))
        {
            if (comp.KV.ContainsKey("m_fontAsset"))
            {
                n.Kind = "text";
                n.Text = PrefabReader.Unquote(O(comp, "m_text"));
                n.FontAsset = Name(PrefabReader.RefGuid(O(comp, "m_fontAsset")));
                n.Material = Name(PrefabReader.RefGuid(O(comp, "m_sharedMaterial")));
                double.TryParse(O(comp, "m_fontSize"), NumberStyles.Float, CultureInfo.InvariantCulture, out n.FontSize);
                var col = ov != null && ov.TryGetValue(comp.Id, out var d) && d.TryGetValue("m_fontColor.r", out var rr)
                    ? new[] { rr, d.GetValueOrDefault("m_fontColor.g", "1"), d.GetValueOrDefault("m_fontColor.b", "1"), d.GetValueOrDefault("m_fontColor.a", "1") }.Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray()
                    : PrefabReader.Vec(comp.Get("m_fontColor"));
                if (col != null && col.Length >= 3) n.Color = Tmp.Hex(col);
                n.AutoSize = O(comp, "m_enableAutoSizing") == "1";
                n.HAlign = O(comp, "m_HorizontalAlignment"); n.VAlign = O(comp, "m_VerticalAlignment");
                double.TryParse(O(comp, "m_characterSpacing"), NumberStyles.Float, CultureInfo.InvariantCulture, out n.CharSpacing);
                n.Raycast = O(comp, "m_RaycastTarget") == "1";
            }
            else if (comp.KV.ContainsKey("m_Sprite") && comp.KV.ContainsKey("m_Type"))
            {
                if (n.Kind != "text") n.Kind = "image";
                var guid = PrefabReader.RefGuid(O(comp, "m_Sprite"));
                n.SpritePath = pr.ByGuid(guid);
                n.Sprite = n.SpritePath == null ? (guid == null ? "(none)" : "(missing " + guid + ")") : System.IO.Path.GetFileNameWithoutExtension(n.SpritePath);
                n.ImageType = O(comp, "m_Type") switch { "0" => "Simple", "1" => "Sliced", "2" => "Tiled", "3" => "Filled", var s => s };
                n.PreserveAspect = O(comp, "m_PreserveAspect") == "1";
                if (PrefabReader.Vec(comp.Get("m_Color")) is { Length: >= 4 } ic) n.ColorTint = Tmp.Hex(ic);
                if (n.SpritePath != null && n.SpritePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !(File.Exists(n.SpritePath + ".meta") && File.ReadAllText(n.SpritePath + ".meta").Contains("spriteMode: 2")))
                { n.Native = Png.Size(n.SpritePath); n.Trim = Png.Trim(n.SpritePath); }
                n.Raycast = O(comp, "m_RaycastTarget") == "1";
            }
            else if (comp.KV.ContainsKey("m_OnClick")) n.Kind = n.Kind.Length == 0 ? "button" : n.Kind + "+button";
        }
    }

    string Name(string guid) => guid == null ? null : pr.ByGuid(guid) is string p ? System.IO.Path.GetFileNameWithoutExtension(p) : "(missing " + guid + ")";
}
