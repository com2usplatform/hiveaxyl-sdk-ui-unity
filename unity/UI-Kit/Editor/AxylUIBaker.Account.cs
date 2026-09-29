// Copyright (c) Com2uS Platform Corp. All rights reserved.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit.Editor
{
    /// <summary>The account screen's bake step: imports the account glyphs and builds
    /// AccountLinkRow.prefab with the shared helpers. Lives with the baker as a partial so
    /// the screen adds itself to the bake without touching the shared file.</summary>
    public static partial class AxylUIBaker
    {
        private const string k_AccountIconsDir =
            k_Root + "/Account/Sprites/Resources/UIKit/AccountIcons";

        [AxylUIBakeStep]
        private static void BakeAccountWidgets(TMP_FontAsset font)
        {
            ImportIcons(k_AccountIconsDir);
            SavePrefab(
                BuildAccountLinkRow(font),
                k_Root + "/Account/AccountLinkRow/Resources/UIKit/Prefabs/AccountLinkRow.prefab");
        }

        // One provider row of the account popup: the shared pill (border + 1px-inset fill) with
        // icon/glyph/label on the left and the state icons — plus (disconnected) and check
        // (connected) — on the right. AccountLinkRow.Configure toggles the state icons.
        private static GameObject BuildAccountLinkRow(TMP_FontAsset font)
        {
            var go = new GameObject("AccountLinkRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(AxylUITheme.ButtonWidthMobile, AxylUITheme.AccountRowHeight);

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

            var icon = NewImage("Icon", rowRect, LoadSprite("uikit_rounded_4"), Image.Type.Sliced);
            var iconRect = (RectTransform)icon.transform;
            iconRect.sizeDelta = new Vector2(AxylUITheme.ButtonIconSize, AxylUITheme.ButtonIconSize);
            icon.color = AxylUITheme.SurfaceStrong;
            icon.preserveAspect = true;
            var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = AxylUITheme.ButtonIconSize;
            iconLayout.preferredHeight = AxylUITheme.ButtonIconSize;

            var glyph = NewText("Glyph", iconRect, font, 10f, AxylUITheme.TextMuted);
            glyph.fontWeight = FontWeight.Bold;
            glyph.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)glyph.transform, 0f);
            glyph.gameObject.SetActive(false);

            var label = NewText("Label", rowRect, font, AxylUITheme.ButtonFontSize, AxylUITheme.TextPrimary);
            label.fontWeight = FontWeight.Medium;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            // One line first; a label too long for its row wraps to a second line and the
            // row grows (never a third line, never an ellipsis on the provider's name).
            label.textWrappingMode = TextWrappingModes.Normal;
            label.maxVisibleLines = 2;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var plus = StateIcon(rowRect, "Plus", "plus", AxylUITheme.TextMuted);
            var check = StateIcon(rowRect, "Check", "check", AxylUITheme.Primary);
            check.gameObject.SetActive(false);

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = border;

            var component = go.AddComponent<AccountLinkRow>();
            Wire(component,
                ("m_borderImage", border), ("m_fillImage", fill),
                ("m_icon", icon), ("m_glyph", glyph), ("m_label", label),
                ("m_plus", plus), ("m_check", check), ("m_button", button));
            return go;

            static Image StateIcon(RectTransform parent, string name, string sprite, Color tint)
            {
                var image = NewImage(name, parent,
                    LoadIcon(k_AccountIconsDir, sprite), Image.Type.Simple);
                image.color = tint;
                image.preserveAspect = true;
                var element = image.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = AxylUITheme.AccountStateIconSize;
                element.preferredHeight = AxylUITheme.AccountStateIconSize;
                return image;
            }
        }

    }
}
