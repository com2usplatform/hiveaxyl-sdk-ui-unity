// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The Kit's bundled font and the procedural sprites left for dynamic cases. Authored
    /// visuals live in the prefabs and the 9-slice sprites under Common/Sprites;
    /// what stays here is the font accessor and the shapes that depend on runtime values
    /// (an app-supplied chip gradient, host-app chrome at arbitrary radii).
    /// </summary>
    public static class AxylUIRuntimeAssets
    {
        // Noto Sans KR SDF: dynamic TMP font asset baked by the Kit's AxylUIBaker from the
        // bundled Noto Sans fonts; JP fallback and the 600/700 weights are wired
        // inside the asset.
        private const string k_FontPath = "UIKit/Fonts/NotoSansKR-SDF";

        private static readonly Dictionary<string, Sprite> s_sprites = new();
        private static TMP_FontAsset s_font;

        /// <summary>The Kit's Noto Sans font asset, able to render every demo
        /// locale. Falls back to the TMP default (no CJK) so a mis-copied Kit still lays out
        /// instead of throwing.</summary>
        public static TMP_FontAsset Font()
        {
            if (s_font != null)
            {
                return s_font;
            }

            s_font = Resources.Load<TMP_FontAsset>(k_FontPath);
            if (s_font == null)
            {
                Debug.LogWarning(
                    $"[UIKit] Bundled font asset missing at Resources/{k_FontPath}; " +
                    "falling back to the TMP default (CJK will not render).");
                s_font = TMP_Settings.defaultFontAsset;
            }

            return s_font;
        }

        /// <summary>A white 9-sliced rounded rectangle; tint it for fills and borders.</summary>
        public static Sprite RoundedRect(float cornerRadius)
        {
            var key = $"rr{cornerRadius}";
            if (s_sprites.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // Texture corners hold the radius; the flat middle stretches via the 9-slice border.
            int r = Mathf.CeilToInt(cornerRadius);
            int size = r * 2 + 8;
            var tex = NewTexture(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, RoundedRectAlpha(x, y, size, size, r)));
                }
            }

            tex.Apply();
            var border = Vector4.one * (r + 2);
            var sprite = Sprite.Create(
                tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            s_sprites[key] = sprite;
            return sprite;
        }

        /// <summary>A white 9-sliced rounded outline, <paramref name="thickness"/> px wide with
        /// a transparent middle — the focus indicator's shape; tint it for the ring color.</summary>
        public static Sprite RoundedRing(float cornerRadius, float thickness)
        {
            var key = $"ring{cornerRadius}/{thickness}";
            if (s_sprites.TryGetValue(key, out var cached))
            {
                return cached;
            }

            int r = Mathf.CeilToInt(cornerRadius);
            int t = Mathf.CeilToInt(thickness);
            int size = r * 2 + 8;
            var tex = NewTexture(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Outer rounded rect minus the same shape inset by the thickness.
                    float outer = RoundedRectAlpha(x, y, size, size, r);
                    float inner = RoundedRectAlpha(x - t, y - t, size - 2 * t, size - 2 * t, Mathf.Max(0, r - t));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(outer - inner)));
                }
            }

            tex.Apply();
            var border = Vector4.one * (r + 2);
            var sprite = Sprite.Create(
                tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            s_sprites[key] = sprite;
            return sprite;
        }

        /// <summary>A diagonal two-color gradient square (app-supplied fallback-chip colors).</summary>
        public static Sprite Gradient(Color from, Color to)
        {
            var key = $"g{(Color32)from}{(Color32)to}";
            if (s_sprites.TryGetValue(key, out var cached))
            {
                return cached;
            }

            const int size = 32;
            var tex = NewTexture(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = (x + (size - 1 - y)) / (2f * (size - 1));
                    tex.SetPixel(x, y, Color.Lerp(from, to, t));
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            s_sprites[key] = sprite;
            return sprite;
        }

        /// <summary>A plain white square.</summary>
        public static Sprite Solid()
        {
            if (s_sprites.TryGetValue("solid", out var cached))
            {
                return cached;
            }

            var tex = NewTexture(4, 4);
            var px = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                px[i] = Color.white;
            }

            tex.SetPixels(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            s_sprites["solid"] = sprite;
            return sprite;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        private static float RoundedRectAlpha(int x, int y, int w, int h, int r)
        {
            float cx = Mathf.Clamp(x, r, w - 1 - r);
            float cy = Mathf.Clamp(y, r, h - 1 - r);
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }
    }
}
