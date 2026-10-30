using System.Windows;
using System.Windows.Media;

namespace DesafioTiempoReaccion.Ui;

/// <summary>
/// Display de 7 segmentos dibujado con vectores (no necesita ninguna fuente externa).
/// Soporta dígitos 0-9, '.', '-' y espacio. Los segmentos apagados se ven tenues.
/// </summary>
public class SevenSegmentDisplay : FrameworkElement
{
    // ---- Geometría de una celda (alto = 1) ----
    private const double W = 0.58;        // ancho de celda
    private const double T = 0.12;        // grosor de segmento
    private const double M = T / 2;
    private const double Gap = 0.026;     // hueco entre segmentos
    private const double Spacing = 0.10;  // espacio entre celdas
    private const double DotW = 0.26;     // ancho reservado para el punto
    private const double SkewDeg = 8;     // inclinación estilo cursiva

    private static readonly string[] Patterns =
    {
        "abcdef", "bc", "abdeg", "abcdg", "bcfg", "acdfg", "acdefg", "abc", "abcdefg", "abcdfg"
    };

    private static readonly Brush DefaultOff = MakeBrush(0x16, 0x26, 0x0C);
    private static readonly Geometry[] Seg = BuildSegments();

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(SevenSegmentDisplay),
        new FrameworkPropertyMetadata("00.00", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OnBrushProperty = DependencyProperty.Register(
        nameof(OnBrush), typeof(Brush), typeof(SevenSegmentDisplay),
        new FrameworkPropertyMetadata(Brushes.Lime, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OffBrushProperty = DependencyProperty.Register(
        nameof(OffBrush), typeof(Brush), typeof(SevenSegmentDisplay),
        new FrameworkPropertyMetadata(DefaultOff, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public Brush OnBrush { get => (Brush)GetValue(OnBrushProperty); set => SetValue(OnBrushProperty, value); }
    public Brush OffBrush { get => (Brush)GetValue(OffBrushProperty); set => SetValue(OffBrushProperty, value); }

    private static Brush MakeBrush(byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(Color.FromRgb(r, g, b));
        br.Freeze();
        return br;
    }

    private static Geometry[] BuildSegments()
    {
        double xl = M, xr = W - M, yt = M, ym = 0.5, yb = 1 - M;
        double hLen = (xr - xl) - 2 * Gap;
        double vLen = (ym - yt) - 2 * Gap;
        double cx = W / 2;
        return new[]
        {
            Hex(cx, yt, hLen, true),                 // a (arriba)
            Hex(xr, (yt + ym) / 2, vLen, false),     // b (arriba-derecha)
            Hex(xr, (ym + yb) / 2, vLen, false),     // c (abajo-derecha)
            Hex(cx, yb, hLen, true),                 // d (abajo)
            Hex(xl, (ym + yb) / 2, vLen, false),     // e (abajo-izquierda)
            Hex(xl, (yt + ym) / 2, vLen, false),     // f (arriba-izquierda)
            Hex(cx, ym, hLen, true),                 // g (centro)
        };
    }

    private static Geometry Hex(double cx, double cy, double len, bool horizontal)
    {
        double hl = len / 2, ht = T / 2;
        Point[] p = horizontal
            ? new[]
            {
                new Point(cx - hl, cy), new Point(cx - hl + ht, cy - ht), new Point(cx + hl - ht, cy - ht),
                new Point(cx + hl, cy), new Point(cx + hl - ht, cy + ht), new Point(cx - hl + ht, cy + ht)
            }
            : new[]
            {
                new Point(cx, cy - hl), new Point(cx + ht, cy - hl + ht), new Point(cx + ht, cy + hl - ht),
                new Point(cx, cy + hl), new Point(cx - ht, cy + hl - ht), new Point(cx - ht, cy - hl + ht)
            };

        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(p[0], true, true);
            ctx.PolyLineTo(new List<Point> { p[1], p[2], p[3], p[4], p[5] }, true, false);
        }
        g.Freeze();
        return g;
    }

    private static double NaturalWidth(string text)
    {
        double w = 0;
        foreach (char c in text) w += c == '.' ? DotW : W + Spacing;
        if (text.Length > 0 && text[^1] != '.') w -= Spacing;
        return w;
    }

    protected override void OnRender(DrawingContext dc)
    {
        double h = ActualHeight, aw = ActualWidth;
        if (h <= 0 || aw <= 0) return;

        string text = Text ?? "";
        double skew = Math.Tan(SkewDeg * Math.PI / 180);
        double units = NaturalWidth(text) + skew;

        double scale = h;
        if (units * scale > aw) scale = aw / units;      // si no cabe, se reduce

        double ox = (aw - units * scale) / 2;
        double oy = (h - scale) / 2;

        dc.PushTransform(new TranslateTransform(ox, oy));
        dc.PushTransform(new SkewTransform(-SkewDeg, 0, 0, scale));
        dc.PushTransform(new ScaleTransform(scale, scale));

        Brush on = OnBrush, off = OffBrush;
        double x = 0;
        foreach (char c in text)
        {
            if (c == '.')
            {
                dc.DrawEllipse(on, null, new Point(x + 0.12, 1 - M), T * 0.62, T * 0.62);
                x += DotW;
                continue;
            }

            string lit = c >= '0' && c <= '9' ? Patterns[c - '0'] : (c == '-' ? "g" : "");
            dc.PushTransform(new TranslateTransform(x, 0));
            for (int i = 0; i < 7; i++) dc.DrawGeometry(off, null, Seg[i]);          // fantasma
            foreach (char s in lit) dc.DrawGeometry(on, null, Seg[s - 'a']);         // encendidos
            dc.Pop();
            x += W + Spacing;
        }

        dc.Pop(); dc.Pop(); dc.Pop();
    }
}
