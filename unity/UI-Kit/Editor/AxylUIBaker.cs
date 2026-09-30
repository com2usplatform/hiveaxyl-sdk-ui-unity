// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit.Editor
{
    /// <summary>
    /// Regenerates the Kit's baked assets — sprite import settings, the Noto Sans TMP font
    /// assets and the widget prefabs — from the committed sources.
    /// Run it after changing a widget's structure, a sprite, a bundled font, or any
    /// <see cref="AxylUITheme"/> token marked <c>Baked</c> — those are written into the
    /// prefabs and sprites here, so a change shows only after a rebake. Unmarked tokens are
    /// read at runtime and need none.
    /// </summary>
    /// <remarks>
    /// <para>Menu: <c>Axyl/UI Kit/Bake Assets</c>, or headless:
    /// <c>Unity -batchmode -executeMethod Hive.Axyl.UIKit.Editor.AxylUIBaker.BakeAll -quit</c>.
    /// Expects the Kit at Assets/UI-Kit, where a game copies it
    /// (unity/Sandbox/sync-kit.sh makes the same copy).</para>
    /// <para>This file owns only what Common owns: the shared sprites and fonts, the provider
    /// marks, and the Common widget prefabs. Each screen bakes its own widgets through a
    /// <see cref="AxylUIBakeStepAttribute"/> step in its own partial file, using the shared
    /// helpers below — the baker never needs a list of screens.</para>
    /// </remarks>
    public static partial class AxylUIBaker
    {
        private const string k_Root = "Assets/UI-Kit";
        private const string k_SpritesDir = k_Root + "/Common/Sprites";
        private const string k_FontsDir = k_Root + "/Fonts";
        private const string k_FontAssetsDir = k_FontsDir + "/Resources/UIKit/Fonts";
        private const string k_MarksDir =
            k_Root + "/Common/ProviderMarks/Resources/UIKit/ProviderMarks";
        private const string k_InputIconsDir =
            k_Root + "/Common/InputGroup/Resources/UIKit/InputIcons";
        private const string k_CursorPath =
            k_Root + "/Common/Cursor/Resources/UIKit/Cursor/hand.png";

        [MenuItem("Axyl/UI Kit/Bake Assets")]
        public static void BakeAll()
        {
            ConfigureSpriteImporters();
            var font = BakeFontAssets();
            BakeCommonPrefabs(font);
            RunScreenBakeSteps(font);
            AssetDatabase.SaveAssets();
            Debug.Log("[AxylUIBaker] Bake complete.");
        }

        /// <summary>
        /// Rebakes only the screens' own widgets, against the committed fonts and Common
        /// prefabs. This is the bake for screen work: it leaves every Common asset untouched,
        /// so a screen change never rewrites shared binaries (and screen branches never
        /// collide on them). Run <see cref="BakeAll"/> instead when Common itself — a font,
        /// a shared sprite, a Common widget — changed.
        /// </summary>
        [MenuItem("Axyl/UI Kit/Bake Screen Assets")]
        public static void BakeScreens()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                k_FontAssetsDir + "/NotoSansKR-SDF.asset");
            if (font == null)
            {
                throw new System.IO.FileNotFoundException(
                    "The baked font asset is missing — copy the Kit whole (Fonts/ included) "
                    + "or run Axyl/UI Kit/Bake Assets once first.");
            }

            RunScreenBakeSteps(font);
            AssetDatabase.SaveAssets();
            Debug.Log("[AxylUIBaker] Screen bake complete.");
        }

        // Every screen bake step in the project, discovered by attribute so this file never
        // carries a screen list. Ordered by name for a deterministic bake.
        private static void RunScreenBakeSteps(TMP_FontAsset font)
        {
            var steps = TypeCache.GetMethodsWithAttribute<AxylUIBakeStepAttribute>()
                .OrderBy(m => m.DeclaringType?.FullName + "." + m.Name)
                .ToList();
            foreach (var step in steps)
            {
                var parameters = step.GetParameters();
                if (!step.IsStatic || parameters.Length != 1
                    || parameters[0].ParameterType != typeof(TMP_FontAsset))
                {
                    Debug.LogError(
                        $"[AxylUIBaker] Bake step {step.DeclaringType?.Name}.{step.Name} must be "
                        + "'static void (TMP_FontAsset)'; skipped.");
                    continue;
                }

                step.Invoke(null, new object[] { font });
            }

            Debug.Log($"[AxylUIBaker] Screen bake steps run: {steps.Count}.");
        }

        // ---- Sprites -------------------------------------------------------------------------

        // The baked textures are 2x (crisper corners when the canvas scales up); importing at
        // 200 px/unit keeps every 9-slice border at its design size on a 100-ppu canvas.
        // Radii come from AxylUITheme so the tokens stay the single source; the PNG textures
        // under Common/Sprites are drawn at these same radii — redraw them too if a radius
        // token moves, or the border will slice into the curve.
        private static void ConfigureSpriteImporters()
        {
            // The pill is baked at the PC radius; other button heights rescale the slice at
            // runtime through pixelsPerUnitMultiplier.
            float pillRadius = AxylUITheme.BakedPillRadius;

            // path -> 9-slice border in texture px (uniform), 0 = no border.
            var borders = new Dictionary<string, float>
            {
                [k_SpritesDir + "/uikit_pill.png"] = TextureBorder(pillRadius),
                [k_SpritesDir + "/uikit_pill_fill.png"] =
                    TextureBorder(pillRadius - AxylUITheme.ButtonBorderWidth),
                [k_SpritesDir + "/uikit_rounded_12.png"] = TextureBorder(AxylUITheme.InputRadius),
                [k_SpritesDir + "/uikit_rounded_16.png"] = TextureBorder(AxylUITheme.ToastRadius),
                [k_SpritesDir + "/uikit_rounded_24.png"] =
                    TextureBorder(AxylUITheme.CommonPopupRadius),
                [k_SpritesDir + "/uikit_rounded_24_fill.png"] =
                    TextureBorder(AxylUITheme.CommonPopupRadius - AxylUITheme.CommonPopupBorderWidth),
                [k_SpritesDir + "/uikit_rounded_4.png"] = TextureBorder(AxylUITheme.ChipRadius),
                [k_SpritesDir + "/uikit_circle.png"] = 0f,
            };
            foreach (var (path, border) in borders)
            {
                ImportAsSprite(path, 200f, border);
            }

            ImportIcons(k_MarksDir);
            ImportIcons(k_InputIconsDir);
            ImportAsCursor(k_CursorPath);
        }

        // The pointer-hand texture Cursor.SetCursor takes: Unity's Cursor texture type (the
        // hardware cursor path), unfiltered so the 32×32 pixels stay crisp, no mipmaps.
        private static void ImportAsCursor(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new System.IO.FileNotFoundException($"UI Kit cursor missing: {path}");
            }

            importer.textureType = TextureImporterType.Cursor;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        // Imports every PNG in a directory as a plain sprite (icons, marks): a screen's bake
        // step calls this on its own icon folder before building prefabs from them.
        private static void ImportIcons(string dir)
        {
            foreach (var path in Directory.GetFiles(dir, "*.png"))
            {
                ImportAsSprite(path.Replace('\\', '/'), 100f, 0f);
            }
        }

        // A design-space radius, in the 2x texture: doubled, plus the 1px flat pad the
        // texture leaves outside the curve.
        private static float TextureBorder(float designRadius)
        {
            return designRadius * 2f + 2f;
        }

        private static void ImportAsSprite(string path, float pixelsPerUnit, float border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new System.IO.FileNotFoundException($"UI Kit sprite missing: {path}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = Vector4.one * border;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{k_SpritesDir}/{name}.png");
            if (sprite == null)
            {
                throw new System.IO.FileNotFoundException(
                    $"UI Kit sprite missing (or not imported as a Sprite): {k_SpritesDir}/{name}.png");
            }

            return sprite;
        }

        // Loads a screen-owned icon sprite by asset path (imported via ImportIcons).
        private static Sprite LoadIcon(string dir, string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{name}.png");
            if (sprite == null)
            {
                throw new System.IO.FileNotFoundException(
                    $"UI Kit sprite missing (or not imported as a Sprite): {dir}/{name}.png");
            }

            return sprite;
        }

        // ---- Fonts ---------------------------------------------------------------------------

        // Noto Sans KR Regular is the primary face (its Latin covers the en locale); JP rides as
        // a TMP fallback, Medium backs the 500 weight (Typography.Button) and Bold the 600/700
        // weights. Dynamic
        // population keeps the assets small — glyphs rasterize on demand from the bundled OTFs.
        private static TMP_FontAsset BakeFontAssets()
        {
            Directory.CreateDirectory(k_FontAssetsDir);
            var main = CreateFontAsset("NotoSansKR-Regular.otf", "NotoSansKR-SDF");
            var medium = CreateFontAsset("NotoSansKR-Medium.otf", "NotoSansKR-Medium-SDF");
            var bold = CreateFontAsset("NotoSansKR-Bold.otf", "NotoSansKR-Bold-SDF");
            var jp = CreateFontAsset("NotoSansJP-Regular.otf", "NotoSansJP-SDF");
            var jpMedium = CreateFontAsset("NotoSansJP-Medium.otf", "NotoSansJP-Medium-SDF");

            main.fallbackFontAssetTable = new List<TMP_FontAsset> { jp };
            medium.fallbackFontAssetTable = new List<TMP_FontAsset> { jpMedium };
            var weights = main.fontWeightTable;
            weights[5].regularTypeface = medium;
            weights[6].regularTypeface = bold; // SemiBold renders Bold: no 600 cut in noto-cjk
            weights[7].regularTypeface = bold;
            EditorUtility.SetDirty(main);
            EditorUtility.SetDirty(medium);
            return main;
        }

        private static TMP_FontAsset CreateFontAsset(string fontFile, string assetName)
        {
            // Recreate from the OTF every bake, so swapping a font file actually lands.
            // Recreating at the same path keeps the asset's GUID, but the sub-asset IDs
            // inside it change — safe because BakeAll rebakes the prefabs (the only
            // referrers) right after; run the whole bake, not pieces.
            string path = $"{k_FontAssetsDir}/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{k_FontsDir}/{fontFile}");
            if (font == null)
            {
                throw new System.IO.FileNotFoundException(
                    $"UI Kit font missing: {k_FontsDir}/{fontFile}");
            }

            var asset = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = assetName + " Atlas";
            asset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            return asset;
        }

        // ---- Common prefabs --------------------------------------------------------------------

        private static void BakeCommonPrefabs(TMP_FontAsset font)
        {
            SavePrefab(
                BuildCloseButton(),
                k_Root + "/Common/CloseButton/Resources/UIKit/Prefabs/CloseButton.prefab");
            SavePrefab(
                BuildFailureToast(font),
                k_Root + "/Common/FailureToast/Resources/UIKit/Prefabs/FailureToast.prefab");
            SavePrefab(
                BuildActionButton(font),
                k_Root + "/Common/ActionButton/Resources/UIKit/Prefabs/AxylUIActionButton.prefab");
            SavePrefab(
                BuildInputGroup(font),
                k_Root + "/Common/InputGroup/Resources/UIKit/Prefabs/AxylUIInputGroup.prefab");
        }

        private static void SavePrefab(GameObject go, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // A bare light 18px X in a 48px transparent hit area, top-right: no white
        // circle or panel; a translucent dark round backdrop appears only on Hover/Pressed.
        private static GameObject BuildCloseButton()
        {
            var go = new GameObject("CloseButton", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);
            rect.sizeDelta = new Vector2(AxylUITheme.CloseHitSize, AxylUITheme.CloseHitSize);

            var hit = go.AddComponent<Image>();
            hit.color = Color.clear;

            // The state backdrop: a black circle whose alpha the Button tint drives — invisible
            // at rest, translucent dark while hovered or pressed.
            var stateBg = NewImage("StateBg", rect, LoadSprite("uikit_circle"), Image.Type.Simple);
            ((RectTransform)stateBg.transform).sizeDelta =
                new Vector2(AxylUITheme.CloseHitSize - 8f, AxylUITheme.CloseHitSize - 8f);
            stateBg.color = Color.black;

            // The X: two rotated bars sized so their span reads as the 18px icon. The bars are
            // wired so CloseButton.Configure can retint them for a light card.
            float barLength = AxylUITheme.CloseIconSize * 1.3334f;
            var bars = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var bar = NewImage("Bar", rect, null, Image.Type.Simple);
                var barRect = (RectTransform)bar.transform;
                barRect.sizeDelta = new Vector2(2f, barLength);
                barRect.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
                bar.color = AxylUITheme.TextOnDark;
                bars[i] = bar;
            }

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = stateBg;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, AxylUITheme.CloseHoverBgAlpha);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f); // Focus = Default
            colors.pressedColor = new Color(1f, 1f, 1f, AxylUITheme.ClosePressedBgAlpha);
            colors.fadeDuration = AxylUITheme.StateTransitionSeconds;
            button.colors = colors;

            var close = go.AddComponent<CloseButton>();
            Wire(close, ("m_button", button));
            WireArray(close, "m_bars", bars);
            return go;
        }

        // Dark elevated surface, Radius.16, status glyph + Body SM label.
        private static GameObject BuildFailureToast(TMP_FontAsset font)
        {
            var go = new GameObject("FailureToast", typeof(RectTransform), typeof(CanvasGroup));
            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false; // retries stay possible while the toast shows

            var bg = go.AddComponent<Image>();
            bg.sprite = LoadSprite("uikit_rounded_16");
            bg.type = Image.Type.Sliced;
            bg.color = AxylUITheme.SurfaceDarkElevated;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(
                (int)AxylUITheme.ToastPadding.x, (int)AxylUITheme.ToastPadding.x,
                (int)AxylUITheme.ToastPadding.y, (int)AxylUITheme.ToastPadding.y);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<LayoutElement>().minHeight = AxylUITheme.ToastMinHeight;

            var iconBg = NewImage(
                "AlertIcon", (RectTransform)go.transform, LoadSprite("uikit_circle"), Image.Type.Simple);
            iconBg.color = new Color(
                AxylUITheme.SemanticDown.r, AxylUITheme.SemanticDown.g, AxylUITheme.SemanticDown.b, 0.18f);
            var iconLayout = iconBg.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 16f;
            iconLayout.preferredHeight = 16f;
            var mark = NewText("Mark", (RectTransform)iconBg.transform, font, 11f, AxylUITheme.SemanticDown);
            mark.fontWeight = FontWeight.Bold;
            mark.alignment = TextAlignmentOptions.Center;
            mark.text = "!";
            Stretch((RectTransform)mark.transform, 0f);

            var label = NewText(
                "Text", (RectTransform)go.transform, font, AxylUITheme.ToastFontSize, AxylUITheme.TextOnDark);
            label.lineSpacing = 0f;
            label.textWrappingMode = TextWrappingModes.Normal;

            var toast = go.AddComponent<FailureToast>();
            Wire(toast,
                ("m_group", (Object)group), ("m_label", label),
                ("m_iconBg", iconBg), ("m_mark", mark));
            return go;
        }

        // The shared pill action with a centered label; AxylUIActionButton.Configure applies
        // the Primary / Destructive look.
        private static GameObject BuildActionButton(TMP_FontAsset font)
        {
            var go = new GameObject("AxylUIActionButton", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(AxylUITheme.ButtonWidthMobile, AxylUITheme.ActionButtonHeight);

            var border = go.AddComponent<Image>();
            border.sprite = LoadSprite("uikit_pill");
            border.type = Image.Type.Sliced;
            border.color = AxylUITheme.Border;

            var fill = NewImage("Fill", rect, LoadSprite("uikit_pill_fill"), Image.Type.Sliced);
            Stretch((RectTransform)fill.transform, AxylUITheme.ButtonBorderWidth);
            fill.color = AxylUITheme.Surface;

            var label = NewText("Label", rect, font, AxylUITheme.ButtonFontSize, AxylUITheme.TextPrimary);
            label.fontWeight = FontWeight.Medium;
            label.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)label.transform, 0f);

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = border;

            var component = go.AddComponent<AxylUIActionButton>();
            Wire(component,
                ("m_borderImage", border), ("m_fillImage", fill),
                ("m_label", label), ("m_button", button));
            return go;
        }

        // One labeled input of a form: label over a 48px field (Input-radius border + 1px-inset
        // fill, leading icon, the TMP input, a visibility toggle for secure fields) with an
        // error line underneath. AxylUIInputGroup.Configure fills the copy and decides
        // secure/plain; the toggle starts hidden.
        private static GameObject BuildInputGroup(TMP_FontAsset font)
        {
            var go = new GameObject("AxylUIInputGroup", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(AxylUITheme.FormWidth, 0f);

            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = AxylUITheme.InputErrorGap;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var label = NewText("Label", rect, font,
                AxylUITheme.InputLabelFontSize, AxylUITheme.TextBody);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // The field: border + fill layers (same trick as the pill), content row inside.
            var fieldGo = new GameObject("Field", typeof(RectTransform));
            var fieldRect = (RectTransform)fieldGo.transform;
            fieldRect.SetParent(rect, false);
            fieldGo.AddComponent<LayoutElement>().preferredHeight = AxylUITheme.InputHeight;

            var border = fieldGo.AddComponent<Image>();
            border.sprite = LoadSprite("uikit_rounded_12");
            border.type = Image.Type.Sliced;
            border.color = AxylUITheme.Border;

            var fill = NewImage("Fill", fieldRect, LoadSprite("uikit_rounded_12"), Image.Type.Sliced);
            Stretch((RectTransform)fill.transform, AxylUITheme.ButtonBorderWidth);
            fill.color = AxylUITheme.Surface;

            var row = new GameObject("Content", typeof(RectTransform));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(fieldRect, false);
            Stretch(rowRect, 0f);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            // Input row padding, asymmetric by design: 14 left (beside the glyph), 16
            // right (beside the toggle). Fixed sides — the RTL swap reverses child order
            // only, so in RTL the 14 sits beside the toggle.
            rowLayout.padding = new RectOffset(14, 16, 0, 0);
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var icon = NewImage("Icon", rowRect, null, Image.Type.Simple);
            icon.color = AxylUITheme.TextMuted;
            icon.preserveAspect = true;
            var iconElement = icon.gameObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = AxylUITheme.ButtonIconSize;
            iconElement.preferredHeight = AxylUITheme.ButtonIconSize;

            // The TMP input: viewport + text + placeholder, all stretched; the transparent
            // image is the input's interaction target.
            var inputGo = new GameObject("InputField", typeof(RectTransform));
            var inputRect = (RectTransform)inputGo.transform;
            inputRect.SetParent(rowRect, false);
            var inputElement = inputGo.AddComponent<LayoutElement>();
            inputElement.flexibleWidth = 1f;
            inputElement.preferredHeight = AxylUITheme.InputHeight;
            var inputHit = inputGo.AddComponent<Image>();
            inputHit.color = Color.clear;

            var viewportGo = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            var viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(inputRect, false);
            Stretch(viewport, 0f);

            var placeholder = NewText("Placeholder", viewport, font,
                AxylUITheme.InputFontSize, AxylUITheme.TextMuted);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch((RectTransform)placeholder.transform, 0f);

            var text = NewText("Text", viewport, font,
                AxylUITheme.InputFontSize, AxylUITheme.TextPrimary);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch((RectTransform)text.transform, 0f);

            var input = inputGo.AddComponent<TMP_InputField>();
            input.targetGraphic = inputHit;
            input.textViewport = viewport;
            input.textComponent = text;
            // The typed value must read back exactly as typed (no "<b>" turning bold).
            // AxylUIInputGroup sets these again at runtime; baked too so the prefab is right.
            input.richText = false;
            text.parseCtrlCharacters = false;
            input.placeholder = placeholder;

            // The visibility toggle (secure fields only): an icon-only button.
            var toggleGo = new GameObject("VisibilityToggle", typeof(RectTransform));
            var toggleRect = (RectTransform)toggleGo.transform;
            toggleRect.SetParent(rowRect, false);
            // The hit area is the full 48×48 touch minimum (the field is 48 tall); the
            // glyph inside stays 20px.
            var toggleElement = toggleGo.AddComponent<LayoutElement>();
            toggleElement.preferredWidth = AxylUITheme.InputToggleHitSize;
            toggleElement.preferredHeight = AxylUITheme.InputToggleHitSize;
            var toggleHit = toggleGo.AddComponent<Image>();
            toggleHit.color = Color.clear;
            var toggleIcon = NewImage("Icon", toggleRect, null, Image.Type.Simple);
            toggleIcon.color = AxylUITheme.TextMuted;
            toggleIcon.preserveAspect = true;
            ((RectTransform)toggleIcon.transform).sizeDelta =
                new Vector2(AxylUITheme.ButtonIconSize, AxylUITheme.ButtonIconSize);
            var toggleButton = toggleGo.AddComponent<Button>();
            toggleButton.transition = Selectable.Transition.ColorTint;
            toggleButton.targetGraphic = toggleIcon;
            var colors = toggleButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = AxylUITheme.IconButtonHoverTint;
            colors.pressedColor = AxylUITheme.IconButtonPressedTint;
            colors.selectedColor = Color.white; // Focus = Default
            colors.fadeDuration = AxylUITheme.StateTransitionSeconds;
            toggleButton.colors = colors;
            toggleGo.SetActive(false);

            var error = NewText("Error", rect, font,
                AxylUITheme.InputErrorFontSize, AxylUITheme.SemanticDown);
            error.alignment = TextAlignmentOptions.MidlineLeft;
            error.textWrappingMode = TextWrappingModes.Normal;
            error.gameObject.SetActive(false);

            var group = go.AddComponent<AxylUIInputGroup>();
            Wire(group,
                ("m_label", label), ("m_borderImage", border), ("m_fillImage", fill),
                ("m_icon", icon), ("m_input", input), ("m_placeholder", placeholder),
                ("m_toggleButton", toggleButton), ("m_toggleIcon", toggleIcon),
                ("m_error", error));
            return go;
        }

        // ---- Helpers -------------------------------------------------------------------------

        private static Image NewImage(string name, RectTransform parent, Sprite sprite, Image.Type type)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI NewText(
            string name, RectTransform parent, TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        // Fills a component's private [SerializeField] references the way the inspector would.
        private static void Wire(Component component, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(component);
            foreach (var (field, value) in refs)
            {
                so.FindProperty(field).objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Fills a component's private [SerializeField] array the way the inspector would.
        private static void WireArray(Component component, string field, Object[] values)
        {
            var so = new SerializedObject(component);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
