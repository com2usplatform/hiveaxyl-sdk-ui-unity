// Copyright (c) Com2uS Platform Corp. All rights reserved.

using TMPro;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// Type metrics the Kit states as ratios. TextMeshPro has no line-height setting —
    /// it lays lines out at the font's own line height plus <c>lineSpacing</c>, given in
    /// hundredths of the font size — so a ratio (Button 1.15, the login button's 1.4)
    /// becomes a per-font <c>lineSpacing</c> here.
    /// </summary>
    public static class AxylUITypography
    {
        /// <summary>Lays <paramref name="text"/> out at <paramref name="ratio"/> × font size
        /// per line, whatever font asset it renders with.</summary>
        public static void SetLineHeight(TMP_Text text, float ratio)
        {
            if (text == null)
            {
                return;
            }

            var font = text.font;
            if (font == null || font.faceInfo.pointSize <= 0f)
            {
                return;
            }

            // The font's natural line height as a ratio of its point size; lineSpacing adds
            // (or removes) the difference, in percent of the rendered font size.
            float natural = font.faceInfo.lineHeight / font.faceInfo.pointSize;
            text.lineSpacing = (ratio - natural) * 100f;
        }
    }
}
