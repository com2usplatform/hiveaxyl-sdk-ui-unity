// Copyright (c) Com2uS Platform Corp. All rights reserved.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit.Editor
{
    /// <summary>The login screen's bake step: builds LoginOptionButton.prefab with the shared
    /// helpers. Lives with the baker as a partial so the screen adds itself to the bake
    /// without touching the shared file.</summary>
    public static partial class AxylUIBaker
    {
        [AxylUIBakeStep]
        private static void BakeLoginWidgets(TMP_FontAsset font)
        {
            SavePrefab(
                BuildLoginOptionButton(font),
                k_Root + "/Login/LoginOptionButton/Resources/UIKit/Prefabs/LoginOptionButton.prefab");
        }

        // One provider row (the standard anatomy): pill border + 1px-inset pill fill +
        // icon/glyph/label row. No focus visual — Focus keeps the Default look.
        private static GameObject BuildLoginOptionButton(TMP_FontAsset font)
        {
            var go = new GameObject("LoginOptionButton", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(AxylUITheme.ButtonWidthPc, AxylUITheme.ButtonHeightPc);

            var border = go.AddComponent<Image>();
            border.sprite = LoadSprite("uikit_pill");
            border.type = Image.Type.Sliced;
            border.color = AxylUITheme.Border;

            var fill = NewImage("Fill", rect, LoadSprite("uikit_pill_fill"), Image.Type.Sliced);
            Stretch((RectTransform)fill.transform, AxylUITheme.ButtonBorderWidth);
            fill.color = AxylUITheme.Surface;

            var row = new GameObject("Content", typeof(RectTransform));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(rect, false);
            Stretch(rowRect, 0f);
            rowRect.offsetMin = new Vector2(AxylUITheme.ButtonPaddingH, 0f);
            rowRect.offsetMax = new Vector2(-AxylUITheme.ButtonPaddingH, 0f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = AxylUITheme.ButtonIconTextGap;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            // Icon slot: the letter-chip look by default (the standard icon chip colors); the
            // screen swaps in a provider mark or an app gradient at Configure time.
            var icon = NewImage("Icon", rowRect, LoadSprite("uikit_rounded_4"), Image.Type.Sliced);
            var iconRect = (RectTransform)icon.transform;
            iconRect.sizeDelta = new Vector2(AxylUITheme.ButtonIconSize, AxylUITheme.ButtonIconSize);
            icon.color = AxylUITheme.SurfaceStrong;
            icon.preserveAspect = true;
            // The glyph never shrinks under a long label: the label absorbs the width.
            var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = AxylUITheme.ButtonIconSize;
            iconLayout.minHeight = iconLayout.preferredHeight = AxylUITheme.ButtonIconSize;
            iconLayout.flexibleWidth = 0f;

            var glyph = NewText("Glyph", iconRect, font, 10f, AxylUITheme.TextMuted);
            glyph.fontWeight = FontWeight.Bold;
            glyph.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)glyph.transform, 0f);
            glyph.gameObject.SetActive(false);

            // Provider label, Typography.Button (14/500); wraps to a second line on long copy
            // and the button grows through LoginOptionButton.Configure.
            var label = NewText("Label", rowRect, font, AxylUITheme.ButtonFontSize, AxylUITheme.TextPrimary);
            label.fontWeight = FontWeight.Medium;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = border;

            var option = go.AddComponent<LoginOptionButton>();
            Wire(option,
                ("m_borderImage", border), ("m_fillImage", fill),
                ("m_icon", icon), ("m_glyph", glyph), ("m_label", label), ("m_button", button));
            return go;
        }

    }
}
