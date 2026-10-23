using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// psdtool — read-only PSD inspector for building Unity UI to a mock.
//   tree   <psd> <out>                         full layer tree + composite.png
//   spec   <psd> <out> [--project P] [--group G]...   one spec_<group>.md per top-level group
//   layers <psd> <out> (--group G | idx...)     PNG of each layer's own pixels (no effects)
//   fonts  <psd> --project P [--group G] [--out D] [--write-presets D]   font + TMP preset check
//   prefab <prefab> --project P [--psd F --group G] [--out D]           prefab rects vs PSD
static class Program
{
    static int Main(string[] a)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (a.Length < 2) { Console.WriteLine("usage: tree|spec|layers|fonts|prefab ... (see Program.cs header)"); return 1; }
        var opt = Opts(a);
        switch (a[0])
        {
            case "tree": Tree(Psd.Open(a[1]), a[2]); break;
            case "spec": Spec(Psd.Open(a[1]), a[2], opt); break;
            case "layers": Layers(Psd.Open(a[1]), a[2], opt, a.Skip(3).Where(x => int.TryParse(x, out _)).Select(int.Parse)); break;
            case "fonts": Fonts(Psd.Open(a[1]), opt); break;
            case "prefab": PrefabCmd(a[1], opt); break;
            case "pair": Pair(a[1], a[2], opt); break;
            default: Console.WriteLine("unknown command " + a[0]); return 1;
        }
        return 0;
    }

    static Dictionary<string, List<string>> Opts(string[] a)
    {
        var d = new Dictionary<string, List<string>>();
        for (int i = 0; i < a.Length - 1; i++)
            if (a[i].StartsWith("--")) { if (!d.TryGetValue(a[i], out var l)) d[a[i]] = l = new(); l.Add(a[i + 1]); }
        return d;
    }
    static string One(Dictionary<string, List<string>> o, string k) => o.TryGetValue(k, out var l) ? l[0] : null;

    static IEnumerable<Layer> Groups(Psd p, Dictionary<string, List<string>> o) =>
        o.TryGetValue("--group", out var names)
            ? names.Select(n => p.Roots.Concat(p.Layers).FirstOrDefault(l => l.Name.Trim() == n.Trim()) ?? throw new Exception("group not found: " + n))
            : p.Roots.Where(l => l.IsGroup);

    static IEnumerable<Layer> Leaves(Layer g)
    {
        foreach (var c in g.Children)
        {
            if (c.IsGroup) { foreach (var x in Leaves(c)) yield return x; }
            else yield return c;
        }
    }

    // ---------- tree ----------
    static void Tree(Psd p, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var sb = new StringBuilder();
        sb.AppendLine($"Canvas {p.W}x{p.H}, {p.Ppi} ppi, {p.Layers.Count} layer records, global light {p.GlobalAngle}°");
        sb.AppendLine("Guides x: " + string.Join(", ", p.GuidesX.Select(v => v.ToString("0.##"))));
        sb.AppendLine("Guides y: " + string.Join(", ", p.GuidesY.Select(v => v.ToString("0.##"))));
        void Walk(Layer l, int d)
        {
            sb.AppendLine($"{l.Index,4} {new string(' ', d * 2)}[{(l.IsGroup ? "GROUP" : l.Kind)}] \"{l.Name}\"{(l.Hidden ? " HIDDEN" : "")} op={l.Opacity * 100 / 255}%{(l.FillOpacity != 255 ? $" fill={l.FillOpacity * 100 / 255}%" : "")} {l.Blend}" + (l.IsGroup ? "" : $" @({l.Left},{l.Top}) {l.W}x{l.H}"));
            if (l.Text != null) sb.AppendLine($"     {new string(' ', d * 2)}text \"{Esc(l.Text.Text)}\" " + string.Join(" | ", l.Text.Runs.Select(RunStr)));
            var fx = FxStr(l, p.GlobalAngle); if (fx.Length > 0) sb.AppendLine($"     {new string(' ', d * 2)}fx {fx}");
            foreach (var e in l.Errors) sb.AppendLine($"     {new string(' ', d * 2)}! {e}");
            foreach (var c in l.Children) Walk(c, d + 1);
        }
        foreach (var r in p.Roots) Walk(r, 0);
        File.WriteAllText(Path.Combine(outDir, "tree.txt"), sb.ToString());
        Png.Write(Path.Combine(outDir, "composite.png"), p.W, p.H, p.CompositePixels());
        Console.WriteLine($"tree.txt + composite.png → {outDir}");
    }

    static string Esc(string s) => (s ?? "").Replace("\r", "⏎").Replace("\n", "⏎").Replace("|", "\\|");
    static string RunStr(TextRun r) => $"\"{Esc(r.Text)}\" {r.Font} {r.SizePx:0.##}px {r.Color}" + (r.Tracking != 0 ? $" tracking {r.Tracking}" : "") + (!r.AutoLeading ? $" leading {r.Leading:0.#}px" : "") + (r.HScale != 1 || r.VScale != 1 ? $" scale {r.HScale:0.##}x{r.VScale:0.##}" : "") + (r.FauxBold ? " FAUX-BOLD" : "") + (r.Caps == 2 ? " ALL-CAPS" : "");

    static string FxStr(Layer l, int ga)
    {
        var parts = new List<string>();
        parts.AddRange(Fx.Strokes(l).Select(s => $"stroke {s.Position} {s.Size:0.#}px {s.Color} {s.Opacity:0}%"));
        parts.AddRange(Fx.Shadows(l, ga).Select(s => $"dropShadow {s.Color} {s.Opacity:0}% angle {s.Angle:0}° dist {s.Distance:0.#} spread {s.Spread:0}% size {s.Blur:0.#} {s.Blend}"));
        parts.AddRange(Fx.Others(l).Select(o => o.Kind + " " + o.Desc));
        return string.Join("; ", parts);
    }

    // ---------- fonts context ----------
    class FontCtx
    {
        public Project Pr;
        public List<FontFile> Files = new();
        public List<TmpFont> Assets = new();
        public List<TmpMat> Mats = new();

        public FontCtx(string root)
        {
            Pr = new Project(root);
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, "Assets"), "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                try
                {
                    if (ext is ".ttf" or ".otf") Files.Add(FontFile.Read(f));
                    else if (ext == ".asset" && new FileInfo(f).Length < 60_000_000 && FileHead(f).Contains("TMP_FontAsset") | FileHead(f).Contains("m_FaceInfo")) { var t = TmpFont.TryRead(f, Pr); if (t != null) Assets.Add(t); }
                    else if (ext == ".mat") { var m = TmpMat.TryRead(f); if (m != null) Mats.Add(m); }
                }
                catch { /* unreadable file: skip */ }
            }
        }

        static string FileHead(string f) { using var s = new StreamReader(f); var b = new char[4096]; int n = s.Read(b, 0, b.Length); return new string(b, 0, n); }

        public FontFile FileFor(string ps) => Files.FirstOrDefault(f => f.Matches(ps));
        public List<TmpFont> AssetsFor(FontFile ff)
        {
            if (ff == null) return new();
            var g = Pr.GuidToPath.FirstOrDefault(kv => string.Equals(kv.Value, ff.Path, StringComparison.OrdinalIgnoreCase)).Key;
            return Assets.Where(a => string.Equals(a.SourceGuid, g, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        public List<TmpMat> MatsFor(TmpFont t) => Mats.Where(m => string.Equals(m.AtlasGuid, t.Guid, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    record Style(string Font, double Size, string Fill, Stroke Stroke, Shadow Shadow, List<string> Texts, List<Overlay> Others);

    static List<Style> Styles(Psd p, IEnumerable<Layer> groups)
    {
        var list = new List<Style>();
        foreach (var g in groups)
            foreach (var l in Leaves(g).Where(l => l.Text != null && l.VisibleFrom(g)))
            {
                var st = Fx.Strokes(l).FirstOrDefault(); var sh = Fx.Shadows(l, p.GlobalAngle).FirstOrDefault(); var ot = Fx.Others(l);
                foreach (var r in l.Text.Runs)
                {
                    var ex = list.FirstOrDefault(s => s.Font == r.Font && Math.Abs(s.Size - r.SizePx) < 0.5 && s.Fill == r.Color && s.Stroke == st && s.Shadow == sh);
                    if (ex != null) ex.Texts.Add(g.Name + ": " + r.Text);
                    else list.Add(new Style(r.Font, r.SizePx, r.Color, st, sh, new() { g.Name + ": " + r.Text }, ot));
                }
            }
        return list;
    }

    /// <summary>What an existing preset renders at a given font size, in canvas px.</summary>
    record PresetEval(double Outer, double FaceShift, double OffX, double OffY, double UEdge, string OutlineColor, string UnderlayColor, bool Underlay);

    static PresetEval Eval(TmpMat m, TmpFont f, double px)
    {
        double gs = m.Get("_GradientScale", f.Padding + 1), r = px / f.PointSize;
        double weight = Math.Max(m.Get("_WeightNormal"), m.Get("_WeightBold", .75)) / 4;
        double fd = m.Get("_FaceDilate"), w = m.Get("_OutlineWidth"), os = m.Get("_OutlineSoftness");
        double ra = (gs - 1) / (gs * Math.Max(1, weight + fd + w + os));
        double ox = m.Get("_UnderlayOffsetX"), oy = m.Get("_UnderlayOffsetY"), ud = m.Get("_UnderlayDilate"), us = m.Get("_UnderlaySoftness");
        double range = (weight + fd) * (gs - 1);
        double rc = Math.Max(0, gs - 1 - range) / (gs * Math.Max(1, Math.Max(Math.Abs(ox), Math.Abs(oy)) + ud + us));
        double ua = ra * gs * r, uc = rc * gs * r;
        bool hasOutline = w > 0.0001 && m.C.TryGetValue("_OutlineColor", out var oc) && oc[3] > 0.01;
        return new PresetEval(hasOutline ? (fd + w) * ua : fd * ua, (fd - (hasOutline ? w : 0)) * ua, ox * uc, oy * uc, fd * ua + ud * uc,
            hasOutline ? m.Hex("_OutlineColor") : null, m.Underlay ? m.Hex("_UnderlayColor") : null, m.Underlay);
    }

    static double ColorDist(string a, string b)
    {
        if (a == null || b == null) return a == b ? 0 : 999;
        var x = Tmp.Rgba(a); var y = Tmp.Rgba(b);
        return Enumerable.Range(0, 3).Max(i => Math.Abs(x[i] - y[i]) * 255) + Math.Abs(x[3] - y[3]) * 100;
    }

    /// <summary>The font asset the project actually styles: the one with the most presets on its atlas.</summary>
    static TmpFont Primary(FontCtx fc, string psFont) =>
        fc.AssetsFor(fc.FileFor(psFont)).OrderByDescending(a => fc.MatsFor(a).Count).FirstOrDefault();

    static (TmpMat mat, string verdict, double score) BestPreset(FontCtx fc, TmpFont fa, Style s)
    {
        if (s.Stroke == null && s.Shadow == null)
            return (null, "plain text — use the font asset's own default material (no preset); set only fontSize and colour", 0);
        (TmpMat, string, double) best = (null, "no preset for this font", 1e9);
        foreach (var a in fc.AssetsFor(fc.FileFor(s.Font)).DefaultIfEmpty(fa))
        {
            var b = BestPresetOn(fc, a, s);
            if (b.score < best.Item3) best = (b.mat, b.verdict + (a == fa ? "" : $" (atlas of {Path.GetFileNameWithoutExtension(a.Path)})"), b.score);
        }
        return best;
    }

    static (TmpMat mat, string verdict, double score) BestPresetOn(FontCtx fc, TmpFont fa, Style s)
    {
        double S = s.Stroke == null ? 0 : s.Stroke.Position switch { "center" => s.Stroke.Size / 2, "inside" => 0, _ => s.Stroke.Size };
        double dx = 0, dy = 0;
        if (s.Shadow != null) { double a = s.Shadow.Angle * Math.PI / 180; dx = -Math.Cos(a) * s.Shadow.Distance; dy = -Math.Sin(a) * s.Shadow.Distance; }
        string want = s.Stroke?.Color + (s.Stroke != null && s.Stroke.Opacity < 99.5 ? ((int)Math.Round(s.Stroke.Opacity * 2.55)).ToString("X2") : "");
        string wantU = s.Shadow == null ? null : s.Shadow.Color + ((int)Math.Round(s.Shadow.Opacity * 2.55)).ToString("X2");
        (TmpMat, string, double) best = (null, "no preset for this font asset", 1e9);
        foreach (var m in fc.MatsFor(fa))
        {
            var e = Eval(m, fa, s.Size);
            double geo = Math.Abs(e.Outer - S) + Math.Abs(e.FaceShift) + (s.Shadow == null ? (e.Underlay ? 3 : 0) : (e.Underlay ? Math.Abs(e.OffX - dx) + Math.Abs(e.OffY - dy) : 6));
            double col = (s.Stroke == null ? (e.OutlineColor == null ? 0 : 20) : ColorDist(e.OutlineColor, want)) + (s.Shadow == null ? 0 : ColorDist(e.UnderlayColor, wantU));
            double score = geo + col / 10;
            if (score < best.Item3)
            {
                string v = geo <= 1 && col <= 6 ? "MATCH" : geo <= 2.5 && col <= 15 ? "CLOSE" : "DIFFERENT";
                best = (m, $"{v} — renders stroke {e.Outer:0.0}px (want {S:0.0}), face {e.FaceShift:+0.0;-0.0}px, shadow ({e.OffX:0.0},{e.OffY:0.0}) want ({dx:0.0},{dy:0.0}), outline {e.OutlineColor ?? "none"} want {(s.Stroke == null ? "none" : want)}, underlay {e.UnderlayColor ?? "none"} want {wantU ?? "none"}", score);
            }
        }
        return best;
    }

    static string PresetLine(Tmp.Target t) =>
        $"`_OutlineWidth {t.Outline:0.###}` `_FaceDilate {t.Dilate:0.###}` `_OutlineColor {t.OutlineColor ?? "-"}`" +
        (t.HasUnderlay ? $" `UNDERLAY_ON` `_UnderlayColor {t.UnderlayColor}` `_UnderlayOffsetX {t.OffX:0.###}` `_UnderlayOffsetY {t.OffY:0.###}` `_UnderlayDilate {t.UDilate:0.###}` `_UnderlaySoftness {t.USoft:0.###}`" : "") +
        (t.Clamped ? " ⚠ exceeds the atlas padding — needs a font asset with more padding" : "") + (t.Note.Length > 0 ? " (" + t.Note.Trim() + ")" : "");

    // ---------- spec ----------
    static void Spec(Psd p, string outDir, Dictionary<string, List<string>> o)
    {
        Directory.CreateDirectory(outDir);
        FontCtx fc = One(o, "--project") is string root ? new FontCtx(root) : null;
        foreach (var g in Groups(p, o))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {g.Name} — spec from `{Path.GetFileName(p.FilePath)}`");
            sb.AppendLine();
            sb.AppendLine($"Canvas {p.W}×{p.H} ({p.Ppi} ppi: 1 pt = {72 / p.Ppi:0.##} px). Coordinates are PSD pixels, top-left origin. " +
                          $"`Unity` = anchored position of the element's centre on a centre-anchored {p.W}×{p.H} canvas (x right, y up). " +
                          "**Bounds are the layer's own pixels — effects (stroke, shadow, glow) are NOT included**; `visual` adds them back.");
            sb.AppendLine();
            sb.AppendLine("| # | Path | Kind | x | y | w | h | Unity (cx, cy) | visual (x,y,w,h incl. fx) | Opacity | Effects |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var l in Leaves(g).Where(l => l.VisibleFrom(g) && l.Kind != "adjustment"))
            {
                var path = l.Path[(g.Path.Length + 1)..];
                if (l.W <= 0 || l.H <= 0) { sb.AppendLine($"| {l.Index} | {Esc(path)} | {l.Kind} | – | – | – | – | – | – | {l.Opacity * 100 / 255}% | {Esc(FxStr(l, p.GlobalAngle))} |"); continue; }
                var e = Fx.Extent(l, p.GlobalAngle);
                double cx = l.Left + l.W / 2.0 - p.W / 2.0, cy = p.H / 2.0 - (l.Top + l.H / 2.0);
                string vis = e == (0, 0, 0, 0) ? "=" : $"{l.Left - e.l:0.#},{l.Top - e.t:0.#},{l.W + e.l + e.r:0.#},{l.H + e.t + e.b:0.#}";
                string op = $"{l.Opacity * 100 / 255}%" + (l.FillOpacity != 255 ? $" fill {l.FillOpacity * 100 / 255}%" : "") + (l.Blend != "norm" ? " " + l.Blend : "") + (l.Clipped ? " clip" : "");
                sb.AppendLine($"| {l.Index} | {Esc(path)} | {l.Kind} | {l.Left} | {l.Top} | {l.W} | {l.H} | ({cx:0.#}, {cy:0.#}) | {vis} | {op} | {Esc(FxStr(l, p.GlobalAngle))} |");
            }
            var texts = Leaves(g).Where(l => l.Text != null && l.VisibleFrom(g)).ToList();
            if (texts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Text");
                sb.AppendLine();
                sb.AppendLine("Glyph bounds (x,y,w,h) are the ink of the glyphs, not a text box: centre a TMP box on them, then fix the baseline by measurement (TMP `Middle` centres line metrics, not ink). " +
                              "TMP `characterSpacing` ≈ Photoshop tracking ÷ 10 `[INFERRED]`.");
                foreach (var l in texts)
                {
                    sb.AppendLine();
                    sb.AppendLine($"### \"{Esc(l.Text.Text)}\" (layer {l.Index}, `{Esc(l.Path[(g.Path.Length + 1)..])}`)");
                    sb.AppendLine($"- glyph bounds {l.Left},{l.Top} {l.W}×{l.H}; align **{l.Text.Justify}**; {l.Text.Kind} text" + (l.Text.Box != null ? $", box {string.Join(",", l.Text.Box.Select(v => v.ToString("0.#")))}" : ""));
                    foreach (var r in l.Text.Runs) sb.AppendLine($"- run {RunStr(r)}");
                    var st = Fx.Strokes(l).FirstOrDefault(); var sh = Fx.Shadows(l, p.GlobalAngle).FirstOrDefault();
                    var fx = FxStr(l, p.GlobalAngle); if (fx.Length > 0) sb.AppendLine($"- effects: {fx}");
                    if (Fx.Strokes(l).Count > 1 || Fx.Shadows(l, p.GlobalAngle).Count > 1) sb.AppendLine("- ⚠ several strokes/shadows: TMP SDF has one outline + one underlay — needs a second text layer or a sprite");
                    if (Fx.Others(l).Count > 0) sb.AppendLine("- ⚠ overlay/glow/bevel on text: TMP needs a vertex colour gradient, a texture, or a pre-rendered sprite");
                    if (fc != null)
                        foreach (var r in l.Text.Runs.GroupBy(r => (r.Font, Math.Round(r.SizePx, 1))).Select(x => x.First()))
                        {
                            var fa = Primary(fc, r.Font);
                            if (fa == null) { sb.AppendLine($"- TMP: ⚠ no font asset for {r.Font} — run `fonts`"); continue; }
                            var t = Tmp.Solve(r.SizePx, st, sh, fa);
                            sb.AppendLine($"- TMP @ {r.SizePx:0.##}px with `{Path.GetFileNameWithoutExtension(fa.Path)}` (sampling {fa.PointSize}, padding {fa.Padding}): fontSize **{r.SizePx:0.##}**, colour {r.Color}, preset {PresetLine(t)}");
                            var best = BestPreset(fc, fa, new Style(r.Font, r.SizePx, r.Color, st, sh, new(), new()));
                            sb.AppendLine(best.mat != null ? $"  - closest existing preset: `{best.mat.Name}` — {best.verdict}" : "  - " + best.verdict);
                        }
                }
            }
            var file = Path.Combine(outDir, "spec_" + Regex.Replace(g.Name.Trim(), @"[^\w\-]+", "_") + ".md");
            File.WriteAllText(file, sb.ToString());
            Console.WriteLine(file);
        }
    }

    // ---------- layers ----------
    static void Layers(Psd p, string outDir, Dictionary<string, List<string>> o, IEnumerable<int> idx)
    {
        Directory.CreateDirectory(outDir);
        var list = o.ContainsKey("--group") ? Groups(p, o).SelectMany(g => Leaves(g).Where(l => l.VisibleFrom(g))).ToList() : idx.Select(i => p.Layers[i]).ToList();
        foreach (var l in list)
        {
            var px = p.LayerPixels(l); if (px == null) continue;
            var f = Path.Combine(outDir, $"{l.Index}_{Regex.Replace(l.Name, @"[^\w\-]+", "_")}.png");
            Png.Write(f, l.W, l.H, px);
            Console.WriteLine($"{f} @({l.Left},{l.Top}) {l.W}x{l.H}");
        }
    }

    // ---------- fonts ----------
    static void Fonts(Psd p, Dictionary<string, List<string>> o)
    {
        var fc = new FontCtx(One(o, "--project") ?? throw new Exception("--project required"));
        var groups = o.ContainsKey("--group") ? Groups(p, o).ToList() : p.Roots.ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"# Font check — `{Path.GetFileName(p.FilePath)}`" + (o.ContainsKey("--group") ? " — " + string.Join(", ", o["--group"]) : " (all groups, hidden included)"));
        sb.AppendLine();
        var runs = groups.SelectMany(g => (g.IsGroup ? Leaves(g) : new[] { g }).Where(l => l.Text != null).SelectMany(l => l.Text.Runs.Select(r => (g, l, r)))).ToList();
        sb.AppendLine("## Fonts used");
        sb.AppendLine();
        sb.AppendLine("| PSD font (PostScript) | Runs | Font file in project | TMP font asset(s) | Atlas | Missing glyphs |");
        sb.AppendLine("|---|---|---|---|---|---|");
        var missing = new List<string>();
        foreach (var grp in runs.GroupBy(x => x.r.Font ?? "(unknown)"))
        {
            var ff = fc.FileFor(grp.Key);
            var assets = fc.AssetsFor(ff);
            var chars = grp.SelectMany(x => x.r.Text).Where(c => c >= 32).Distinct().ToList();
            string miss = "";
            if (ff != null)
            {
                var noFile = chars.Where(c => !ff.Cmap.Contains(c)).ToList();
                if (noFile.Count > 0) miss += "not in font file: " + string.Concat(noFile) + "; ";
                foreach (var a in assets.Where(a => a.Mode == 0))
                {
                    var noAtlas = chars.Where(c => !a.Chars.Contains(c)).ToList();
                    if (noAtlas.Count > 0) miss += $"not in static atlas {Path.GetFileNameWithoutExtension(a.Path)}: {string.Concat(noAtlas)}; ";
                }
            }
            if (ff == null) missing.Add(grp.Key);
            sb.AppendLine($"| {grp.Key} | {grp.Count()} | {(ff == null ? "**MISSING**" : fc.Pr.Rel(ff.Path))} | " +
                          $"{(assets.Count == 0 ? (ff == null ? "–" : "**NONE** — create with Font Asset Creator") : string.Join("<br>", assets.Select(a => $"{fc.Pr.Rel(a.Path)} (sampling {a.PointSize}, padding {a.Padding})")))} | " +
                          $"{string.Join(", ", assets.Select(a => a.Mode == 1 ? "Dynamic" : "Static"))} | {(miss.Length == 0 ? "none" : Esc(miss))} |");
        }
        if (missing.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**⚠ Missing from the project — ask the developer/designer to add:** " + string.Join(", ", missing) +
                          ". Get the exact font file the designer used (same weight; check the PostScript name). Then create a TMP font asset (SDF, sampling ≈ largest size used, padding ≥ 10% of sampling, enough for the widest stroke).");
        }
        sb.AppendLine();
        sb.AppendLine("## Text styles → TMP material presets");
        sb.AppendLine();
        sb.AppendLine("Values solved from the TMP source (`TMP_ShaderUtilities.UpdateShaderRatios`/`GetPadding`, `TMP_SDF.shader`) for the size each style is used at. " +
                      "A preset is size-specific: the same px stroke at another font size needs other values. `[INFERRED: derived, not render-verified]` — confirm with the overlay test.");
        sb.AppendLine("Fill colour is NOT part of a preset — it is the TMP component's vertex colour, so styles that differ only in fill share one preset. " +
                      "Styles that differ only in size share a preset while the rendered stroke/shadow stays within 0.5 px of the PSD at every size in the group.");
        var styles = Styles(p, groups.Where(g => g.IsGroup));
        var write = One(o, "--write-presets");
        if (write != null) Directory.CreateDirectory(write);
        int n = 0, written = 0;
        var families = styles.GroupBy(s => (s.Font, s.Stroke, s.Shadow)).OrderBy(g => g.Key.Font).ThenByDescending(g => g.Max(s => s.Size));
        foreach (var fam in families)
        {
            // split the size range so a single preset stays within 0.5 px (effects scale linearly with font size)
            double S = fam.Key.Stroke?.Size ?? 0, D = fam.Key.Shadow == null ? 0 : fam.Key.Shadow.Distance + fam.Key.Shadow.Blur;
            double tol = Math.Max(S, D) <= 0 ? double.MaxValue : 0.5 / Math.Max(S, D);
            var sizes = fam.OrderBy(s => s.Size).ToList();
            var bands = new List<List<Style>>();
            foreach (var s in sizes)
                if (bands.Count == 0 || s.Size > bands[^1][0].Size * (1 + 2 * tol)) bands.Add(new() { s }); else bands[^1].Add(s);
            foreach (var band in bands)
            {
                n++;
                double lo = band.Min(s => s.Size), hi = band.Max(s => s.Size), refSize = Math.Sqrt(lo * hi);
                var s0 = band[0] with { Size = refSize };
                sb.AppendLine();
                sb.AppendLine($"### S{n}: {fam.Key.Font} {(lo == hi ? $"{lo:0.##}px" : $"{lo:0.##}–{hi:0.##}px")}" +
                              (fam.Key.Stroke != null ? $", stroke {fam.Key.Stroke.Position} {fam.Key.Stroke.Size:0.#}px {fam.Key.Stroke.Color}" + (fam.Key.Stroke.Opacity < 99.5 ? $" {fam.Key.Stroke.Opacity:0}%" : "") : ", no stroke") +
                              (fam.Key.Shadow != null ? $", shadow {fam.Key.Shadow.Color} {fam.Key.Shadow.Opacity:0}% {fam.Key.Shadow.Angle:0}° dist {fam.Key.Shadow.Distance:0.#} spread {fam.Key.Shadow.Spread:0}% size {fam.Key.Shadow.Blur:0.#}" : ""));
                var uses = band.SelectMany(s => s.Texts.Select(t => $"{t} ({s.Size:0.#}px {s.Fill})")).Distinct().ToList();
                sb.AppendLine("- used by: " + Esc(string.Join(" · ", uses.Take(10))) + (uses.Count > 10 ? $" … (+{uses.Count - 10})" : ""));
                var others = band.SelectMany(s => s.Others).Select(x => x.Kind).Distinct().ToList();
                if (others.Count > 0) sb.AppendLine("- ⚠ also has " + string.Join(", ", others) + " — not expressible in one TMP preset (vertex colour gradient, a second text layer, or a sprite)");
                var fa = Primary(fc, fam.Key.Font);
                if (fa == null) { sb.AppendLine("- no TMP font asset → cannot solve a preset"); continue; }
                var t = Tmp.Solve(refSize, fam.Key.Stroke, fam.Key.Shadow, fa);
                sb.AppendLine($"- needed (solved at {refSize:0.#}px on `{Path.GetFileNameWithoutExtension(fa.Path)}`): " + PresetLine(t) + (t.WeightBold == 0 ? " `_WeightBold 0`" : ""));
                var best = BestPreset(fc, fa, s0);
                sb.AppendLine(best.mat == null ? "- existing: " + best.verdict : $"- closest existing: `{fc.Pr.Rel(best.mat.Path)}` — {best.verdict}");
                if (write != null && (best.mat == null || !best.verdict.StartsWith("MATCH")) && (fam.Key.Stroke != null || fam.Key.Shadow != null))
                {
                    var file = WritePreset(fc, fa, s0, t, write, lo, hi);
                    if (file != null) { written++; sb.AppendLine($"- proposed preset written: `{Path.GetFileName(file)}`"); }
                }
            }
        }
        if (write != null) { sb.AppendLine(); sb.AppendLine($"{written} proposed preset(s) in `{write}` — not in the project. Review, then copy into the font's folder (Unity creates the .meta)."); }
        var outFile = Path.Combine(One(o, "--out") ?? ".", "fonts.md");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile)));
        File.WriteAllText(outFile, sb.ToString());
        Console.WriteLine(outFile);
    }

    /// <summary>Clones a same-atlas preset (one with underlay when needed) and sets the solved values. No .meta is written — Unity creates it on import.</summary>
    static string WritePreset(FontCtx fc, TmpFont fa, Style s, Tmp.Target t, string dir, double lo, double hi)
    {
        var mats = fc.MatsFor(fa);
        var baseMat = mats.FirstOrDefault(m => m.Underlay == t.HasUnderlay) ?? mats.FirstOrDefault();
        if (baseMat == null) return null;
        var txt = File.ReadAllText(baseMat.Path);
        string nl = txt.Contains("\r\n") ? "\r\n" : "\n";
        string name = $"{Path.GetFileNameWithoutExtension(fa.Path).Replace(" SDF", "")} PSD {(s.Stroke == null ? "NoOL" : $"OL{s.Stroke.Size:0.#} {s.Stroke.Color.TrimStart('#')}")}" +
                      $"{(t.HasUnderlay ? $" UL{s.Shadow.Distance:0.#} {s.Shadow.Color.TrimStart('#')}" : "")} {(Math.Round(lo) == Math.Round(hi) ? $"{lo:0}" : $"{lo:0}-{hi:0}")}px";
        // [^\r\n]* keeps the file's own CRLF/LF line endings intact
        txt = Regex.Replace(txt, @"(?m)^([ \t]*m_Name: )[^\r\n]*", "${1}" + name);
        void F(string k, double v) => txt = Regex.Replace(txt, $@"(?m)^([ \t]*- {k}: )[^\r\n]*", "${1}" + v.ToString("0.#####", CultureInfo.InvariantCulture));
        void C(string k, double[] c) => txt = Regex.Replace(txt, $@"(?m)^([ \t]*- {k}: )[^\r\n]*", "${1}" + string.Format(CultureInfo.InvariantCulture, "{{r: {0:0.######}, g: {1:0.######}, b: {2:0.######}, a: {3:0.######}}}", c[0], c[1], c[2], c[3]));
        F("_OutlineWidth", t.Outline); F("_FaceDilate", t.Dilate); F("_OutlineSoftness", 0); F("_ScaleRatioA", t.RatioA); F("_WeightBold", t.WeightBold);
        C("_FaceColor", new double[] { 1, 1, 1, 1 });
        C("_OutlineColor", t.OutlineColor == null ? new double[] { 0, 0, 0, 0 } : Tmp.Rgba(t.OutlineColor));
        if (t.HasUnderlay)
        {
            F("_UnderlayOffsetX", t.OffX); F("_UnderlayOffsetY", t.OffY); F("_UnderlayDilate", t.UDilate); F("_UnderlaySoftness", t.USoft); F("_ScaleRatioC", t.RatioC);
            C("_UnderlayColor", Tmp.Rgba(t.UnderlayColor));
            if (!Regex.IsMatch(txt, "UNDERLAY_ON")) txt = txt.Replace("m_ValidKeywords: []", $"m_ValidKeywords:{nl}  - UNDERLAY_ON");
        }
        else
        {
            txt = Regex.Replace(txt, @"(?m)^[ \t]*- UNDERLAY_ON\r?\n", "");
            txt = Regex.Replace(txt, @"m_ValidKeywords:(\r?\n)(?![ \t]*-)", "m_ValidKeywords: []$1");
            C("_UnderlayColor", new double[] { 0, 0, 0, 0 });
        }
        var file = Path.Combine(dir, name + ".mat");
        File.WriteAllText(file, txt);
        return file;
    }

    // ---------- prefab ----------
    static void PrefabCmd(string prefab, Dictionary<string, List<string>> o)
    {
        var pr = new Project(One(o, "--project") ?? throw new Exception("--project required"));
        Psd psd = One(o, "--psd") is string pf ? Psd.Open(pf) : null;
        double cw = psd?.W ?? 1080, ch = psd?.H ?? 1920;
        if (One(o, "--canvas") is string cs && cs.Split('x') is [var a0, var a1]) { cw = double.Parse(a0, CultureInfo.InvariantCulture); ch = double.Parse(a1, CultureInfo.InvariantCulture); }
        var lay = new PrefabLayout(pr, prefab, cw, ch);
        var sb = new StringBuilder();
        sb.AppendLine($"# Prefab `{pr.Rel(prefab)}` on a {cw}×{ch} canvas");
        sb.AppendLine();
        sb.AppendLine("Rects resolved from the serialized RectTransforms (anchors, pivot, sizeDelta, anchoredPosition, scale; rotation ignored), PSD-style top-left coordinates. Runtime changes (SafeArea, layout groups, code) are not applied.");
        sb.AppendLine();
        sb.AppendLine("| Path | Active | Kind | x | y | w | h | Sprite / text | Notes |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var n in lay.All)
        {
            var notes = new List<string>();
            if (n.Sx != 1 || n.Sy != 1) notes.Add($"scale {n.Sx:0.###}×{n.Sy:0.###}");
            if (n.Kind.StartsWith("image") && n.ImageType == "Simple" && n.Native is (int nw, int nh) && (Math.Abs(nw * n.Sx - n.W) > 0.5 || Math.Abs(nh * n.Sy - n.H) > 0.5)) notes.Add($"⚠ not native size ({nw}×{nh})");
            if (n.Fitter != null) notes.Add("AspectRatioFitter " + n.Fitter);
            if (n.Scripts.Contains("SafeArea")) notes.Add("SafeArea");
            if (n.Kind == "text") { notes.Add($"{n.FontAsset} {n.FontSize}px {n.Color} mat {n.Material ?? "default"}{(n.AutoSize ? " AUTOSIZE" : "")} align {n.HAlign}/{n.VAlign}"); }
            if (n.Raycast && n.Kind is "image" or "text") notes.Add("raycast");
            string what = n.Kind == "text" ? $"\"{Esc(n.Text)}\"" : n.Sprite ?? "";
            sb.AppendLine($"| {Esc(n.Path)} | {(n.ActiveInHierarchy ? "✓" : "–")} | {n.Kind} | {n.X:0.#} | {n.Y:0.#} | {n.W:0.#} | {n.H:0.#} | {Esc(what)} | {Esc(string.Join("; ", notes))} |");
        }
        if (psd != null)
        {
            var g = Groups(psd, o).First();
            var leaves = Leaves(g).Where(l => l.VisibleFrom(g) && l.W > 0 && l.H > 0 && l.Kind != "adjustment").ToList();
            sb.AppendLine();
            sb.AppendLine($"## vs PSD group \"{g.Name}\"");
            sb.AppendLine();
            sb.AppendLine("Text is matched by string; images by best overlap (IoU) of the sprite's opaque ink (transparent margins trimmed) with a PSD layer's own or effect-extended (+fx) bounds. Δ = prefab − PSD in px; over 1 px is flagged. Effect extents are estimates (stroke + shadow offset + size). " +
                          "For text, only the horizontal centre and the font values are comparable statically — the TMP box is not the glyph ink.");
            sb.AppendLine();
            sb.AppendLine("| Prefab node | PSD layer | Δx | Δy | Δw | Δh | Check |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            static string Norm(string s) => Regex.Replace(Regex.Replace(s ?? "", "<[^>]+>", ""), @"\s+", "").ToLowerInvariant();
            var used = new HashSet<Layer>();
            foreach (var n in lay.All.Where(n => n.ActiveInHierarchy && n.Kind == "text"))
            {
                var l = leaves.FirstOrDefault(l => l.Text != null && Norm(l.Text.Text) == Norm(n.Text)) ?? leaves.FirstOrDefault(l => l.Text != null && Norm(n.Text).Length > 2 && Norm(l.Text.Text).Contains(Norm(n.Text)));
                if (l == null) { sb.AppendLine($"| {Esc(n.Path)} \"{Esc(n.Text)}\" | (no PSD text with this string) | | | | | |"); continue; }
                used.Add(l);
                var r = l.Text.Runs.FirstOrDefault();
                double dcx = (n.X + n.W / 2) - (l.Left + l.W / 2.0);
                var chk = new List<string>();
                if (r != null && Math.Abs(n.FontSize - r.SizePx) > 0.5) chk.Add($"⚠ size {n.FontSize} vs {r.SizePx:0.##}");
                if (r != null && n.Color != null && ColorDist(n.Color, r.Color) > 3 && l.Text.Runs.Count == 1) chk.Add($"⚠ colour {n.Color} vs {r.Color}");
                if (l.Text.Runs.Select(x => x.Color).Distinct().Count() > 1) chk.Add("multi-colour: " + string.Join(" ", l.Text.Runs.Select(x => $"\"{x.Text}\"{x.Color}")) + " → rich text <color>");
                if (r != null && n.FontAsset != null && !Regex.Replace(n.FontAsset, "[^a-z0-9]", "", RegexOptions.IgnoreCase).ToLowerInvariant().Contains(Regex.Replace(r.Font ?? "", "[^a-z0-9]", "", RegexOptions.IgnoreCase).ToLowerInvariant())) chk.Add($"⚠ font {n.FontAsset} vs {r.Font}");
                if (n.AutoSize) chk.Add("auto-size ON");
                if (l.Text.Justify == "center" && n.HAlign != null && n.HAlign != "2") chk.Add($"PSD centred, TMP HAlign {n.HAlign}");
                if (Math.Abs(dcx) > 1) chk.Add($"⚠ centre x off {dcx:+0.#;-0.#}px");
                chk.Add($"mat {n.Material ?? "default"}");
                sb.AppendLine($"| {Esc(n.Path)} | {l.Index} \"{Esc(l.Text.Text)}\" | {dcx:+0.#;-0.#;0} (centre) | box | | | {Esc(string.Join("; ", chk))} |");
            }
            foreach (var n in lay.All.Where(n => n.ActiveInHierarchy && n.Kind.StartsWith("image") && n.W > 0 && n.H > 0))
            {
                var ink = n.Ink;
                Layer best = null; double bestIou = 0; bool withFx = false;
                foreach (var l in leaves.Where(l => l.Text == null && !used.Contains(l)))
                {
                    var e = Fx.Extent(l, psd.GlobalAngle);
                    double a = Iou(ink.x, ink.y, ink.w, ink.h, l.Left, l.Top, l.W, l.H);
                    double b = Iou(ink.x, ink.y, ink.w, ink.h, l.Left - e.l, l.Top - e.t, l.W + e.l + e.r, l.H + e.t + e.b);
                    if (a > bestIou) { bestIou = a; best = l; withFx = false; }
                    if (b > bestIou) { bestIou = b; best = l; withFx = true; }
                }
                if (best == null || bestIou < 0.5) { sb.AppendLine($"| {Esc(n.Path)} ({n.Sprite}) | (no overlapping PSD layer) | | | | | |"); continue; }
                var ex = withFx ? Fx.Extent(best, psd.GlobalAngle) : (0, 0, 0, 0);
                double bx = best.Left - ex.Item1, by = best.Top - ex.Item2, bw = best.W + ex.Item1 + ex.Item3, bh = best.H + ex.Item2 + ex.Item4;
                double dx = ink.x - bx, dy = ink.y - by, dw = ink.w - bw, dh = ink.h - bh;
                string flag = new[] { dx, dy, dw, dh }.Any(v => Math.Abs(v) > 1) ? "⚠ > 1 px" : "ok";
                sb.AppendLine($"| {Esc(n.Path)} ({n.Sprite}) | {best.Index} \"{Esc(best.Name)}\"{(withFx ? " +fx" : "")} IoU {bestIou:0.00} | {dx:+0.#;-0.#;0} | {dy:+0.#;-0.#;0} | {dw:+0.#;-0.#;0} | {dh:+0.#;-0.#;0} | {flag} |");
            }
        }
        var outDir = One(o, "--out") ?? ".";
        Directory.CreateDirectory(outDir);
        var file = Path.Combine(outDir, "prefab_" + Path.GetFileNameWithoutExtension(prefab) + ".md");
        File.WriteAllText(file, sb.ToString());
        Console.WriteLine(file);
    }

    // ---------- pair: two screens that must look identical at a hand-off (DataLoading ↔ SceneTransition) ----------
    // pair <prefabA> <prefabB> --project P [--screens 1080x1920,1080x2400,...] [--ref 1080x1920]
    //      [--matchA expand|0..1] [--matchB expand|0..1] [--ignore name,name] [--out D]
    // Lays both prefabs out on the canvas each would get on every screen, converts to SCREEN pixels
    // (each side with its own CanvasScaler mode), pairs elements by sprite / text and reports every Δ.
    static void Pair(string a, string b, Dictionary<string, List<string>> o)
    {
        var pr = new Project(One(o, "--project") ?? throw new Exception("--project required"));
        var screens = (One(o, "--screens") ?? "1080x1920,1080x2340,1080x2400,1080x2640,1536x2048,1600x2000,720x1280")
            .Split(',').Select(s => s.Split('x')).Select(p => (w: double.Parse(p[0], CultureInfo.InvariantCulture), h: double.Parse(p[1], CultureInfo.InvariantCulture))).ToList();
        var rf = (One(o, "--ref") ?? "1080x1920").Split('x').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        var ignore = (One(o, "--ignore") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        double Scale(string match, double w, double h) => match is null or "expand"
            ? Math.Min(w / rf[0], h / rf[1])
            : Math.Pow(2, (1 - double.Parse(match, CultureInfo.InvariantCulture)) * Math.Log2(w / rf[0]) + double.Parse(match, CultureInfo.InvariantCulture) * Math.Log2(h / rf[1]));
        string Key(UNode n) => n.Kind == "text" ? "text:" + n.Text : n.Kind.StartsWith("image") && n.SpritePath != null ? "sprite:" + n.Sprite : null;
        bool Skip(UNode n) => !n.ActiveInHierarchy || ignore.Any(i => n.Path.Contains(i, StringComparison.OrdinalIgnoreCase)) || n.W <= 0 || n.H <= 0;

        var sb = new StringBuilder();
        sb.AppendLine($"# Pair check — `{pr.Rel(a)}` ↔ `{pr.Rel(b)}`");
        sb.AppendLine();
        sb.AppendLine($"Both laid out from their serialized RectTransforms (AspectRatioFitter applied; SafeArea, layout groups and code not applied) on the canvas each gets per screen " +
                      $"(A: {One(o, "--matchA") ?? "expand"}, B: {One(o, "--matchB") ?? "expand"}, reference {rf[0]}×{rf[1]}). Δ in **screen pixels**. Elements pair by sprite or text; ignored: {(ignore.Length == 0 ? "none" : string.Join(", ", ignore))}.");
        double worst = 0;
        foreach (var (w, h) in screens)
        {
            double sa = Scale(One(o, "--matchA"), w, h), sbb = Scale(One(o, "--matchB"), w, h);
            var la = new PrefabLayout(pr, a, w / sa, h / sa);
            var lb = new PrefabLayout(pr, b, w / sbb, h / sbb);
            var na = la.All.Where(n => !Skip(n) && Key(n) != null).ToList();
            var nb = lb.All.Where(n => !Skip(n) && Key(n) != null).ToList();
            sb.AppendLine();
            sb.AppendLine($"## {w}×{h} (scale A {sa:0.###}, B {sbb:0.###})");
            sb.AppendLine();
            sb.AppendLine("| Element | A (x,y,w,h px) | B (x,y,w,h px) | Δx | Δy | Δw | Δh | |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            var usedB = new HashSet<UNode>();
            foreach (var x in na)
            {
                var y = nb.FirstOrDefault(n => !usedB.Contains(n) && Key(n) == Key(x));
                if (y == null) { sb.AppendLine($"| {Esc(x.Path)} ({Esc(Key(x))}) | only in A | | | | | | ⚠ |"); continue; }
                usedB.Add(y);
                double dx = y.X * sbb - x.X * sa, dy = y.Y * sbb - x.Y * sa, dw = y.W * sbb - x.W * sa, dh = y.H * sbb - x.H * sa;
                double m = new[] { dx, dy, dw, dh }.Max(Math.Abs); worst = Math.Max(worst, m);
                var flags = new List<string>();
                if (m >= 0.5) flags.Add("⚠ moves");
                if (x.UnderSafeArea != y.UnderSafeArea) flags.Add($"⚠ SafeArea only on {(x.UnderSafeArea ? "A" : "B")} — shifts on notched phones");
                if ((x.Fitter ?? "") != (y.Fitter ?? "")) flags.Add($"fitter A {x.Fitter ?? "none"} / B {y.Fitter ?? "none"}");
                if (x.PreserveAspect != y.PreserveAspect || x.ImageType != y.ImageType) flags.Add($"⚠ image A {x.ImageType}{(x.PreserveAspect ? " preserveAspect" : "")} / B {y.ImageType}{(y.PreserveAspect ? " preserveAspect" : "")}");
                if (x.ColorTint != y.ColorTint) flags.Add($"⚠ tint A {x.ColorTint} / B {y.ColorTint}");
                if (x.Sx != y.Sx || x.Sy != y.Sy) flags.Add($"scale A {x.Sx:0.###} / B {y.Sx:0.###}");
                sb.AppendLine($"| {Esc(Key(x))} | {x.X * sa:0.#},{x.Y * sa:0.#},{x.W * sa:0.#},{x.H * sa:0.#} | {y.X * sbb:0.#},{y.Y * sbb:0.#},{y.W * sbb:0.#},{y.H * sbb:0.#} | {dx:+0.#;-0.#;0} | {dy:+0.#;-0.#;0} | {dw:+0.#;-0.#;0} | {dh:+0.#;-0.#;0} | {(flags.Count == 0 ? "ok" : Esc(string.Join("; ", flags)))} |");
            }
            foreach (var y in nb.Where(n => !usedB.Contains(n))) sb.AppendLine($"| {Esc(y.Path)} ({Esc(Key(y))}) | | only in B | | | | | ⚠ |");
        }
        sb.Insert(0, $"<!-- worst Δ {worst:0.##} px -->\n");
        sb.AppendLine();
        sb.AppendLine(worst < 0.5 ? $"**Result: identical on every screen tested (worst Δ {worst:0.##} px).**" : $"**Result: ⚠ elements move at the hand-off — worst Δ {worst:0.#} px.**");
        sb.AppendLine("Static check only: confirm with a capture of both at the same frame on 2–3 ratios (§5).");
        var outDir = One(o, "--out") ?? ".";
        Directory.CreateDirectory(outDir);
        var file = Path.Combine(outDir, $"pair_{Path.GetFileNameWithoutExtension(a)}_{Path.GetFileNameWithoutExtension(b)}.md");
        File.WriteAllText(file, sb.ToString());
        Console.WriteLine($"{file} — worst Δ {worst:0.##} px");
    }

    static double Iou(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh)
    {
        double x0 = Math.Max(ax, bx), y0 = Math.Max(ay, by), x1 = Math.Min(ax + aw, bx + bw), y1 = Math.Min(ay + ah, by + bh);
        double inter = Math.Max(0, x1 - x0) * Math.Max(0, y1 - y0);
        return inter / (aw * ah + bw * bh - inter);
    }
}
