// Copyright (c) Com2uS Platform Corp. All rights reserved.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// Reading direction: a right-to-left language mirrors text alignment
    /// and the order of every horizontal row (icon + label, header, actions) while paddings,
    /// sizes, brand marks and direction-fixed glyphs stay as they are. The app judges the
    /// direction from its current language (<see cref="IsRightToLeft"/>) and passes it to
    /// each screen's options; screens and widgets apply it through the two
    /// <c>Apply</c> overloads so the rule lives in one place.
    /// </summary>
    /// <remarks>TextMeshPro reverses the glyph run for right-to-left text but does not shape
    /// Arabic or Persian letters; an app shipping those languages adds its shaping step
    /// before handing the strings in, as it would for any TextMeshPro UI.</remarks>
    public static class AxylUIDirection
    {
        /// <summary>True for the languages written right-to-left — Arabic, Hebrew,
        /// Persian — given a language tag such as <c>"ar"</c>, <c>"ar-SA"</c>,
        /// <c>"he"</c> or <c>"fa"</c>.</summary>
        public static bool IsRightToLeft(string languageTag)
        {
            if (string.IsNullOrEmpty(languageTag))
            {
                return false;
            }

            string primary = languageTag.Split('-', '_')[0].ToLowerInvariant();
            return primary == "ar" || primary == "he" || primary == "iw" || primary == "fa";
        }

        /// <summary>Runs a row's children right-to-left and mirrors where its free space
        /// goes — reversing the order alone would leave a row that does not fill its width
        /// packed on the left; its paddings and size stay. Call it after the row's
        /// <c>childAlignment</c> is authored, since the mirror reads it.</summary>
        public static void Apply(HorizontalLayoutGroup row, bool rightToLeft)
        {
            if (row == null)
            {
                return;
            }

            row.reverseArrangement = rightToLeft;
            if (rightToLeft)
            {
                row.childAlignment = MirrorHorizontal(row.childAlignment);
            }
        }

        // Left <-> Right on each vertical band; centered stays centered.
        private static TextAnchor MirrorHorizontal(TextAnchor anchor) => anchor switch
        {
            TextAnchor.UpperLeft => TextAnchor.UpperRight,
            TextAnchor.UpperRight => TextAnchor.UpperLeft,
            TextAnchor.MiddleLeft => TextAnchor.MiddleRight,
            TextAnchor.MiddleRight => TextAnchor.MiddleLeft,
            TextAnchor.LowerLeft => TextAnchor.LowerRight,
            TextAnchor.LowerRight => TextAnchor.LowerLeft,
            _ => anchor,
        };

        /// <summary>Fills a grid from the top-right corner instead of the top-left.</summary>
        public static void Apply(GridLayoutGroup grid, bool rightToLeft)
        {
            if (grid != null)
            {
                grid.startCorner = rightToLeft
                    ? GridLayoutGroup.Corner.UpperRight
                    : GridLayoutGroup.Corner.UpperLeft;
            }
        }

        /// <summary>Mirrors a text authored left-to-right for the reading direction: its
        /// horizontal alignment swaps sides (centered text stays) and the glyph run renders
        /// right-to-left. Left-to-right leaves the authored alignment alone.</summary>
        public static void Apply(TMP_Text text, bool rightToLeft)
        {
            if (text == null)
            {
                return;
            }

            text.isRightToLeftText = rightToLeft;
            if (!rightToLeft)
            {
                return;
            }

            switch (text.horizontalAlignment)
            {
                case HorizontalAlignmentOptions.Left:
                    text.horizontalAlignment = HorizontalAlignmentOptions.Right;
                    break;
                case HorizontalAlignmentOptions.Right:
                    text.horizontalAlignment = HorizontalAlignmentOptions.Left;
                    break;
            }
        }
    }
}
