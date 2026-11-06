using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiny software canvas used to "paint" all of the game's pixel art in code.
/// (0,0) is the BOTTOM-left pixel, like a Unity texture.
/// </summary>
public sealed class PixelCanvas
{
    public readonly int Width;
    public readonly int Height;
    public readonly Color32[] Pixels;

    public PixelCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new Color32[width * height];
    }

    public static Color32 C(int r, int g, int b, int a = 255)
    {
        return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    int WrapX(int x)
    {
        return ((x % Width) + Width) % Width;
    }

    public void Set(int x, int y, Color32 c, bool wrapX = false)
    {
        if (wrapX) x = WrapX(x);
        if (!InBounds(x, y)) return;
        Pixels[y * Width + x] = c;
    }

    public Color32 Get(int x, int y)
    {
        return InBounds(x, y) ? Pixels[y * Width + x] : new Color32(0, 0, 0, 0);
    }

    /// <summary>Fills an inclusive rectangle.</summary>
    public void FillRect(int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Set(x, y, c);
    }

    public void FillEllipse(float cx, float cy, float rx, float ry, Color32 c, bool wrapX = false)
    {
        FillEllipse(cx, cy, rx, ry, (nx, ny) => c, wrapX);
    }

    /// <summary>Fills an ellipse; the shader receives normalised coordinates in -1..1 (y up).</summary>
    public void FillEllipse(float cx, float cy, float rx, float ry, Func<float, float, Color32> shader, bool wrapX = false)
    {
        int x0 = Mathf.FloorToInt(cx - rx) - 1;
        int x1 = Mathf.CeilToInt(cx + rx) + 1;
        int y0 = Mathf.FloorToInt(cy - ry) - 1;
        int y1 = Mathf.CeilToInt(cy + ry) + 1;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                    Set(x, y, shader(dx, dy), wrapX);
            }
        }
    }

    public void ClearEllipse(float cx, float cy, float rx, float ry)
    {
        FillEllipse(cx, cy, rx, ry, new Color32(0, 0, 0, 0));
    }

    /// <summary>Fills a rounded rectangle (inclusive bounds).</summary>
    public void FillRoundRect(int x0, int y0, int x1, int y1, int r, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                int dx = Mathf.Max(Mathf.Max(x0 + r - x, 0), x - (x1 - r));
                int dy = Mathf.Max(Mathf.Max(y0 + r - y, 0), y - (y1 - r));
                if (dx * dx + dy * dy <= r * r + r / 2)
                    Set(x, y, c);
            }
        }
    }

    /// <summary>Adds a 1px outline around every opaque shape (draws only on transparent pixels).</summary>
    public void Outline(Color32 c, bool diagonal = true, bool wrapX = false)
    {
        if (c.a == 0) return;
        var toPaint = new List<int>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (Pixels[y * Width + x].a != 0) continue;
                bool near = Solid(x - 1, y, wrapX) || Solid(x + 1, y, wrapX) || Solid(x, y - 1, wrapX) || Solid(x, y + 1, wrapX);
                if (!near && diagonal)
                    near = Solid(x - 1, y - 1, wrapX) || Solid(x + 1, y - 1, wrapX) || Solid(x - 1, y + 1, wrapX) || Solid(x + 1, y + 1, wrapX);
                if (near) toPaint.Add(y * Width + x);
            }
        }
        for (int i = 0; i < toPaint.Count; i++) Pixels[toPaint[i]] = c;
    }

    bool Solid(int x, int y, bool wrapX)
    {
        if (wrapX) x = WrapX(x);
        return InBounds(x, y) && Pixels[y * Width + x].a != 0;
    }

    public Texture2D ToTexture(FilterMode filter = FilterMode.Point)
    {
        var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        tex.filterMode = filter;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(Pixels);
        tex.Apply(false, false);
        return tex;
    }

    public Sprite ToSprite(float pixelsPerUnit, Vector2 pivot, FilterMode filter = FilterMode.Point)
    {
        return Sprite.Create(ToTexture(filter), new Rect(0, 0, Width, Height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }
}
