using UnityEngine;

namespace WorldGen.Flow
{
    public static class ProceduralShapeGenerator
    {
        private static Sprite _squareSprite;
        private static Sprite _circleSprite;

        public static Sprite GetSquareSprite()
        {
            if (_squareSprite != null) return _squareSprite;

            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            _squareSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            _squareSprite.name = "Proc_Square";
            return _squareSprite;
        }

        public static Sprite GetCircleSprite(int resolution = 128)
        {
            if (_circleSprite != null) return _circleSprite;

            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[resolution * resolution];
            float radius = (resolution - 2) * 0.5f;
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    // Suavizado anti-alias en el borde
                    float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
            _circleSprite.name = "Proc_Circle";
            return _circleSprite;
        }

        public static Sprite CreateBorderFrameSprite(int width = 128, int height = 128, int thickness = 4)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x < thickness || x >= width - thickness || y < thickness || y >= height - thickness);
                    pixels[y * width + x] = isBorder ? Color.white : new Color(1f, 1f, 1f, 0.05f);
                }
            }
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            Sprite spr = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
            spr.name = "Proc_Frame";
            return spr;
        }
    }
}
