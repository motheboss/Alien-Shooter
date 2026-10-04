using UnityEngine;

public enum ShapeType { Square, Circle, Triangle }

/// <summary>
/// Generates simple white shape sprites (square, circle, triangle) in code so the
/// project needs no external art. Colour is applied through SpriteRenderer.color.
/// </summary>
public static class SpriteFactory
{
    public const int Size = 64;
    public const float PixelsPerUnit = 64f;

    static readonly Sprite[] cache = new Sprite[3];

    public static Sprite Get(ShapeType shape)
    {
        int index = (int)shape;
        if (cache[index] == null)
        {
            Texture2D tex = CreateTexture(shape);
            tex.hideFlags = HideFlags.HideAndDontSave;
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = "Generated" + shape;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            cache[index] = sprite;
        }
        return cache[index];
    }

    /// <summary>Builds an anti-aliased white texture of the given shape (1 world unit at 64 PPU).</summary>
    public static Texture2D CreateTexture(ShapeType shape)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 3; sy++)
                {
                    for (int sx = 0; sx < 3; sx++)
                    {
                        float u = (x + (sx + 0.5f) / 3f) / Size;
                        float v = (y + (sy + 0.5f) / 3f) / Size;
                        if (Inside(shape, u, v)) hits++;
                    }
                }
                byte alpha = (byte)(hits * 255 / 9);
                pixels[y * Size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static bool Inside(ShapeType shape, float u, float v)
    {
        switch (shape)
        {
            case ShapeType.Circle:
                float dx = u - 0.5f;
                float dy = v - 0.5f;
                return dx * dx + dy * dy <= 0.25f;

            case ShapeType.Triangle:
                // Apex at the top, flat base at the bottom.
                if (v < 0.03f || v > 0.97f) return false;
                float halfWidth = 0.47f * (0.97f - v) / 0.94f;
                return Mathf.Abs(u - 0.5f) <= halfWidth;

            default:
                return true;
        }
    }
}
