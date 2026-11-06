using UnityEngine;

/// <summary>
/// Procedural pixel-art factory. Every sprite in the game is painted here at runtime,
/// so the project has zero imported art assets.
/// </summary>
public static class PixelArt
{
    /// <summary>Art pixels per world unit. The camera is 10 units tall, so the "virtual screen" is 240px high.</summary>
    public const float PPU = 24f;

    static Color32 C(int r, int g, int b, int a = 255) { return PixelCanvas.C(r, g, b, a); }

    // ------------------------------------------------------------------ bird

    public struct BirdSkin
    {
        public Color32 Light, Main, Dark, Wing, WingShade, Beak, BeakDark;
    }

    public static readonly BirdSkin[] Skins =
    {
        new BirdSkin { Light = C(255, 240, 130), Main = C(250, 204, 48), Dark = C(232, 150, 34),
                       Wing = C(255, 250, 225), WingShade = C(240, 214, 150), Beak = C(250, 110, 40), BeakDark = C(214, 70, 32) },
        new BirdSkin { Light = C(255, 170, 150), Main = C(240, 84, 72), Dark = C(190, 48, 52),
                       Wing = C(255, 236, 226), WingShade = C(244, 190, 176), Beak = C(255, 200, 60), BeakDark = C(226, 150, 30) },
        new BirdSkin { Light = C(150, 226, 255), Main = C(70, 160, 240), Dark = C(44, 110, 200),
                       Wing = C(236, 248, 255), WingShade = C(182, 214, 240), Beak = C(255, 168, 50), BeakDark = C(226, 120, 34) },
    };

    /// <summary>Three wing-flap frames for the given skin.</summary>
    public static Sprite[] MakeBird(BirdSkin s)
    {
        float[] wingY = { 12f, 9.2f, 6.4f };
        var frames = new Sprite[3];
        for (int i = 0; i < 3; i++) frames[i] = DrawBird(s, wingY[i]);
        return frames;
    }

    static Sprite DrawBird(BirdSkin s, float wingY)
    {
        var cv = new PixelCanvas(26, 20);
        Color32 outline = C(58, 34, 24);
        Color32 white = C(255, 255, 255);

        // body
        Color32 light = s.Light, main = s.Main, dark = s.Dark;
        cv.FillEllipse(11f, 9.5f, 8.5f, 6.5f, (nx, ny) =>
        {
            if (ny > 0.55f) return light;
            if (ny < -0.25f) return dark;
            return main;
        });

        // eye
        cv.FillEllipse(15f, 11.5f, 3.6f, 3.6f, outline);
        cv.FillEllipse(15f, 11.5f, 3.0f, 3.0f, white);
        cv.FillRect(16, 10, 17, 12, outline);

        // beak (upper, mouth line, lower)
        cv.FillRect(18, 7, 23, 10, s.Beak);
        cv.FillRect(18, 6, 22, 6, C(150, 40, 28));
        cv.FillRect(18, 4, 22, 5, s.BeakDark);

        // wing
        Color32 wing = s.Wing, wingShade = s.WingShade;
        cv.FillEllipse(8f, wingY, 5.2f, 3.4f, C(96, 58, 32));
        cv.FillEllipse(8f, wingY, 4.4f, 2.7f, (nx, ny) => ny > 0.1f ? wing : wingShade);

        // small highlight on the head
        cv.Set(7, 14, white);
        cv.Set(8, 14, white);

        cv.Outline(outline, true);
        return cv.ToSprite(PPU, new Vector2(11f / 26f, 9.5f / 20f));
    }

    // ----------------------------------------------------------------- pipes

    static Color32 PipeColumn(int x, int w)
    {
        if (x == 0 || x == w - 1) return C(54, 78, 28);
        float t = (x - 1f) / (w - 3f);
        if (t < 0.10f) return C(160, 222, 90);
        if (t < 0.28f) return C(190, 244, 110);
        if (t < 0.42f) return C(140, 214, 56);
        if (t < 0.70f) return C(110, 190, 40);
        if (t < 0.88f) return C(84, 160, 34);
        return C(66, 132, 30);
    }

    /// <summary>Pipe shaft: 26px wide, 24px (= 1 unit) tall. Stretch vertically to any length.</summary>
    public static Sprite PipeBody()
    {
        const int w = 26, h = 24;
        var cv = new PixelCanvas(w, h);
        for (int x = 0; x < w; x++)
        {
            Color32 col = PipeColumn(x, w);
            for (int y = 0; y < h; y++) cv.Set(x, y, col);
        }
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    /// <summary>Pipe cap: 30x13. Drawn with the opening facing UP; flip Y for the ceiling pipe.</summary>
    public static Sprite PipeCap()
    {
        const int w = 30, h = 13;
        var cv = new PixelCanvas(w, h);
        Color32 outline = C(54, 78, 28);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 col;
                if (y == 0 || y == h - 1 || x == 0 || x == w - 1) col = outline;
                else if (y == 1) col = C(66, 120, 28);          // shadow where the cap meets the shaft
                else if (y == h - 2) col = C(200, 248, 130);    // lip highlight
                else col = PipeColumn(x, w);
                cv.Set(x, y, col);
            }
        }
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    // ---------------------------------------------------------------- ground

    /// <summary>Scrolling ground strip, 32x24px, pivot at top-centre. Tiles seamlessly in X.</summary>
    public static Sprite Ground()
    {
        const int w = 32, h = 24;
        var cv = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
        {
            int row = h - 1 - y;
            for (int x = 0; x < w; x++)
            {
                Color32 c;
                if (row == 0) c = C(84, 56, 22);
                else if (row <= 4) c = (((x + row) / 4) % 2 == 0) ? C(156, 232, 80) : C(118, 200, 52);
                else if (row == 5) c = C(88, 150, 36);
                else if (row == 6) c = C(238, 232, 164);
                else if (row >= h - 2) c = C(190, 146, 70);
                else c = ((x * 5 + row * 3) % 16 == 0) ? C(200, 160, 80) : C(222, 184, 100);
                cv.Set(x, y, c);
            }
        }
        return cv.ToSprite(PPU, new Vector2(0.5f, 1f));
    }

    // ------------------------------------------------------------ background

    /// <summary>1x64 banded sky gradient, ppu 64 (so 1 unit tall). Scale it to cover the screen.</summary>
    public static Sprite Sky(Color32 top, Color32 bottom)
    {
        const int h = 64, bands = 18;
        var cv = new PixelCanvas(1, h);
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);                        // 0 bottom .. 1 top
            t = Mathf.Floor(t * bands) / (bands - 1);
            cv.Set(0, y, Color32.Lerp(bottom, top, Mathf.Clamp01(t)));
        }
        return cv.ToSprite(64f, new Vector2(0.5f, 0.5f));
    }

    static void Cloud(PixelCanvas cv, float cx, float cy, float s)
    {
        Color32 white = C(255, 255, 255), shade = C(212, 234, 246);
        System.Func<float, float, Color32> shader = (nx, ny) => ny < -0.35f ? shade : white;
        cv.FillEllipse(cx - 8f * s, cy - 1f * s, 6f * s, 4.5f * s, shader, true);
        cv.FillEllipse(cx, cy + 2f * s, 8f * s, 6f * s, shader, true);
        cv.FillEllipse(cx + 9f * s, cy - 1f * s, 6f * s, 4.5f * s, shader, true);
        cv.FillEllipse(cx, cy - 3f * s, 14f * s, 3f * s, shader, true);
    }

    /// <summary>Tileable cloud strip (224x56), centre pivot.</summary>
    public static Sprite Clouds()
    {
        var cv = new PixelCanvas(224, 56);
        Cloud(cv, 24f, 30f, 1.2f);
        Cloud(cv, 90f, 14f, 0.8f);
        Cloud(cv, 146f, 36f, 1.0f);
        Cloud(cv, 204f, 20f, 0.7f);
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    /// <summary>City skyline (buildings) and a matching "lit windows" overlay for night. Both 192x64, bottom pivot.</summary>
    public static void Skyline(out Sprite buildings, out Sprite windows)
    {
        const int W = 192, H = 64;
        var rnd = new System.Random(7);
        var b = new PixelCanvas(W, H);
        var lit = new PixelCanvas(W, H);

        int x = 0;
        while (x < W)
        {
            int bw = rnd.Next(14, 29);
            if (W - x - bw < 14) bw = W - x;
            int bh = rnd.Next(26, 58);
            Color32 baseC = rnd.Next(2) == 0 ? C(176, 218, 226) : C(160, 206, 218);
            b.FillRect(x, 0, x + bw - 1, bh - 1, baseC);
            b.FillRect(x, 0, x, bh - 1, C(126, 180, 196));
            b.FillRect(x, bh - 1, x + bw - 1, bh - 1, C(206, 238, 242));
            if (rnd.Next(3) == 0) b.FillRect(x + bw / 2, bh, x + bw / 2, Mathf.Min(H - 1, bh + 4), C(126, 180, 196));

            for (int wy = bh - 7; wy >= 4; wy -= 5)
            {
                for (int wx = x + 3; wx + 1 < x + bw - 2; wx += 4)
                {
                    b.FillRect(wx, wy, wx + 1, wy + 2, C(134, 188, 202));
                    if (rnd.Next(100) < 45) lit.FillRect(wx, wy, wx + 1, wy + 2, C(255, 226, 110));
                }
            }
            x += bw;
        }
        buildings = b.ToSprite(PPU, new Vector2(0.5f, 0f));
        windows = lit.ToSprite(PPU, new Vector2(0.5f, 0f));
    }

    /// <summary>Tileable hedge/tree line (160x36), bottom pivot.</summary>
    public static Sprite Bushes()
    {
        const int W = 160, H = 36;
        var rnd = new System.Random(21);
        var cv = new PixelCanvas(W, H);
        Color32 lightG = C(112, 204, 72), midG = C(74, 168, 56), baseG = C(66, 150, 50);
        System.Func<float, float, Color32> shader = (nx, ny) => (-nx + ny) > 0.75f ? lightG : midG;

        cv.FillRect(0, 0, W - 1, 12, baseG);
        int count = 11;
        for (int i = 0; i < count; i++)
        {
            float cx = i * (W / (float)count) + rnd.Next(-4, 5);
            float r = rnd.Next(8, 13);
            float cy = rnd.Next(12, 17);
            cv.FillEllipse(cx, cy, r, r, shader, true);
        }
        cv.Outline(C(40, 100, 44), true, true);
        return cv.ToSprite(PPU, new Vector2(0.5f, 0f));
    }

    /// <summary>Star field (256x140), bottom pivot. Only shown at night.</summary>
    public static Sprite Stars()
    {
        const int W = 256, H = 140;
        var rnd = new System.Random(99);
        var cv = new PixelCanvas(W, H);
        for (int i = 0; i < 70; i++)
        {
            int x = rnd.Next(W);
            double r = rnd.NextDouble();
            int y = H - 1 - (int)(r * r * (H - 4));
            if (rnd.Next(5) == 0)
            {
                Color32 arm = C(190, 196, 255);
                cv.Set(x - 1, y, arm); cv.Set(x + 1, y, arm); cv.Set(x, y - 1, arm); cv.Set(x, y + 1, arm);
                cv.Set(x, y, C(255, 255, 255));
            }
            else cv.Set(x, y, C(255, 255, 240));
        }
        return cv.ToSprite(PPU, new Vector2(0.5f, 0f));
    }

    public static Sprite Sun()
    {
        var cv = new PixelCanvas(40, 40);
        cv.FillEllipse(20f, 20f, 15f, 15f, (nx, ny) => (-nx + ny) > 0.8f ? C(255, 250, 190) : C(255, 232, 110));
        cv.Outline(C(255, 190, 60), true);
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    public static Sprite Moon()
    {
        var cv = new PixelCanvas(36, 36);
        cv.FillEllipse(18f, 18f, 14f, 14f, C(252, 246, 210));
        cv.ClearEllipse(24f, 21f, 12f, 12f);
        cv.FillEllipse(10f, 14f, 1.6f, 1.6f, C(226, 218, 176));
        cv.FillEllipse(12f, 22f, 1.2f, 1.2f, C(226, 218, 176));
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    // -------------------------------------------------------------------- UI

    /// <summary>Game-over panel, 112x72px.</summary>
    public static Sprite Panel()
    {
        var cv = new PixelCanvas(112, 72);
        cv.FillRoundRect(0, 0, 111, 71, 6, C(84, 56, 22));
        cv.FillRoundRect(2, 2, 109, 69, 4, C(253, 250, 222));
        cv.FillRoundRect(4, 4, 107, 67, 3, C(226, 218, 152));
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    /// <summary>Score medal: tier 0 bronze, 1 silver, 2 gold, 3 platinum. 32x32px.</summary>
    public static Sprite Medal(int tier)
    {
        Color32[] ringC = { C(205, 127, 50), C(190, 196, 205), C(250, 200, 40), C(120, 220, 230) };
        Color32[] lightC = { C(240, 176, 108), C(238, 242, 248), C(255, 238, 124), C(204, 250, 255) };
        Color32[] darkC = { C(140, 80, 30), C(120, 128, 140), C(190, 140, 20), C(60, 150, 170) };
        tier = Mathf.Clamp(tier, 0, 3);
        Color32 ring = ringC[tier], light = lightC[tier], dark = darkC[tier];

        var cv = new PixelCanvas(32, 32);
        cv.FillEllipse(16f, 16f, 15.5f, 15.5f, C(74, 44, 24));
        cv.FillEllipse(16f, 16f, 14f, 14f, (nx, ny) => (-nx + ny) > 0.6f ? light : ring);
        cv.FillEllipse(16f, 16f, 10f, 10f, dark);
        cv.FillEllipse(16f, 16f, 9f, 9f, (nx, ny) => (-nx + ny) > 0.9f ? light : ring);

        // gem in the middle: dark rim, bright face
        for (int dy = -5; dy <= 5; dy++)
        {
            for (int dx = -5; dx <= 5; dx++)
            {
                int d = (dx < 0 ? -dx : dx) + (dy < 0 ? -dy : dy);
                if (d == 5) cv.Set(16 + dx, 16 + dy, dark);
                else if (d < 5) cv.Set(16 + dx, 16 + dy, (dy - dx > 1) ? light : ring);
            }
        }

        cv.FillRect(7, 21, 8, 24, C(255, 255, 255));
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    // ------------------------------------------------------------------ misc

    public static Sprite Particle()
    {
        var cv = new PixelCanvas(6, 6);
        cv.FillEllipse(3f, 3f, 3f, 3f, C(255, 255, 255));
        return cv.ToSprite(PPU, new Vector2(0.5f, 0.5f));
    }

    /// <summary>1x1 white pixel, 1 unit big. Scale it for flashes and fills.</summary>
    public static Sprite White()
    {
        var cv = new PixelCanvas(1, 1);
        cv.Set(0, 0, C(255, 255, 255));
        return cv.ToSprite(1f, new Vector2(0.5f, 0.5f));
    }
}
