using System.Collections.Generic;
using UnityEngine;

/// <summary>Colours + outline settings for a piece of pixel text.</summary>
public struct TextStyle
{
    public Color32 Top, Bottom, Outline, Outline2;
    public bool HasOutline2;

    public TextStyle(Color32 top, Color32 bottom, Color32 outline)
    {
        Top = top; Bottom = bottom; Outline = outline;
        Outline2 = new Color32(0, 0, 0, 0);
        HasOutline2 = false;
    }

    public TextStyle(Color32 top, Color32 bottom, Color32 outline, Color32 outline2)
    {
        Top = top; Bottom = bottom; Outline = outline;
        Outline2 = outline2;
        HasOutline2 = true;
    }

    static Color32 C(int r, int g, int b, int a = 255) { return PixelCanvas.C(r, g, b, a); }

    public static readonly TextStyle White = new TextStyle(C(255, 255, 255), C(232, 232, 232), C(62, 40, 28));
    public static readonly TextStyle Title = new TextStyle(C(255, 238, 110), C(255, 146, 38), C(92, 44, 12));
    public static readonly TextStyle Red = new TextStyle(C(255, 128, 96), C(226, 60, 40), C(90, 24, 16));
    public static readonly TextStyle Label = new TextStyle(C(222, 110, 40), C(222, 110, 40), C(0, 0, 0, 0));
    public static readonly TextStyle Badge = new TextStyle(C(255, 255, 255), C(255, 255, 255), C(214, 50, 40), C(214, 50, 40));
}

/// <summary>
/// 5x7 bitmap font rendered into sprites at runtime, so the game needs no font assets,
/// no TextMeshPro and no uGUI package.
/// </summary>
public static class PixelFont
{
    const int GlyphW = 5;
    const int GlyphH = 7;
    const int Pad = 3;
    const int Spacing = 2;

    static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>();
    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    static void G(char c, string r0, string r1, string r2, string r3, string r4, string r5, string r6)
    {
        Glyphs[c] = new[] { r0, r1, r2, r3, r4, r5, r6 };
    }

    static PixelFont()
    {
        G('A', ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
        G('B', "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####.");
        G('C', ".####", "#....", "#....", "#....", "#....", "#....", ".####");
        G('D', "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####.");
        G('E', "#####", "#....", "#....", "####.", "#....", "#....", "#####");
        G('F', "#####", "#....", "#....", "####.", "#....", "#....", "#....");
        G('G', ".####", "#....", "#....", "#.###", "#...#", "#...#", ".###.");
        G('H', "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
        G('I', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####");
        G('J', "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##..");
        G('K', "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#");
        G('L', "#....", "#....", "#....", "#....", "#....", "#....", "#####");
        G('M', "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#");
        G('N', "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#");
        G('O', ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
        G('P', "####.", "#...#", "#...#", "####.", "#....", "#....", "#....");
        G('Q', ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#");
        G('R', "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#");
        G('S', ".####", "#....", "#....", ".###.", "....#", "....#", "####.");
        G('T', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..");
        G('U', "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
        G('V', "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#..");
        G('W', "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#");
        G('X', "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#");
        G('Y', "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..");
        G('Z', "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####");
        G('0', ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###.");
        G('1', "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###.");
        G('2', ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####");
        G('3', "####.", "....#", "....#", ".###.", "....#", "....#", "####.");
        G('4', "#...#", "#...#", "#...#", "#####", "....#", "....#", "....#");
        G('5', "#####", "#....", "####.", "....#", "....#", "#...#", ".###.");
        G('6', ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###.");
        G('7', "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#...");
        G('8', ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###.");
        G('9', ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###.");
        G('.', ".....", ".....", ".....", ".....", ".....", ".....", "..#..");
        G('!', "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#..");
        G('?', ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#..");
        G(':', ".....", "..#..", ".....", ".....", ".....", "..#..", ".....");
        G('-', ".....", ".....", ".....", ".###.", ".....", ".....", ".....");
        G('/', "....#", "....#", "...#.", "..#..", ".#...", "#....", "#....");
    }

    static int Pack(Color32 c)
    {
        return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
    }

    /// <summary>Renders text to a sprite. pixelSize = world units covered by one font pixel.</summary>
    public static Sprite Render(string text, float pixelSize, TextStyle style)
    {
        text = text.ToUpperInvariant();
        string key = text + "|" + pixelSize + "|" + Pack(style.Top) + "," + Pack(style.Bottom) + "," +
                     Pack(style.Outline) + "," + (style.HasOutline2 ? Pack(style.Outline2) : 0);
        Sprite cached;
        if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

        int n = Mathf.Max(1, text.Length);
        int textW = n * GlyphW + (n - 1) * Spacing;
        var cv = new PixelCanvas(textW + Pad * 2, GlyphH + Pad * 2);

        for (int i = 0; i < text.Length; i++)
        {
            string[] g;
            if (!Glyphs.TryGetValue(text[i], out g)) continue;
            int gx = Pad + i * (GlyphW + Spacing);
            for (int r = 0; r < GlyphH; r++)
            {
                Color32 col = Color32.Lerp(style.Top, style.Bottom, r / (float)(GlyphH - 1));
                for (int c = 0; c < GlyphW; c++)
                    if (g[r][c] == '#') cv.Set(gx + c, Pad + (GlyphH - 1 - r), col);
            }
        }

        cv.Outline(style.Outline, true);
        if (style.HasOutline2) cv.Outline(style.Outline2, true);

        var sprite = cv.ToSprite(1f / pixelSize, new Vector2(0.5f, 0.5f));
        Cache[key] = sprite;
        return sprite;
    }
}
