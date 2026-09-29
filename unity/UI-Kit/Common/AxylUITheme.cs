// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The design tokens every UI Kit screen reads: the shared design-system values, plus a
    /// section per screen for the metrics that screen sizes for itself — where the two
    /// overlap, the screen-specific value wins. The full sheet lives here even for screens a
    /// copy leaves out, so restyling starts in this one file and screen folders never edit
    /// it. Change values here, never inline in a screen.
    /// <para>
    /// A token marked <c>Baked</c> is read by <c>AxylUIBaker</c>: its value is built into a
    /// prefab or a sprite, so a change fully shows only after <c>Axyl/UI Kit/Bake Assets</c>
    /// runs. Many of them are read at runtime as well — the runtime uses pick the new value
    /// up right away, the baked uses keep the old one until the rebake, and the two drift
    /// apart in between. An unmarked token is runtime-only and takes effect on the next play.
    /// </para>
    /// </summary>
    public static class AxylUITheme
    {
        // ---- Color tokens ----
        public static readonly Color Primary = Hex("#1677FF");  // Baked
        public static readonly Color PrimaryActive = Hex("#003ECC");
        public static readonly Color PrimaryDisabled = Hex("#A8B8CC");
        public static readonly Color Canvas = Hex("#FFFFFF");
        public static readonly Color SurfaceSoft = Hex("#F7F7F7");
        public static readonly Color Surface = Hex("#FFFFFF");        // Baked: Surface Card
        public static readonly Color SurfaceStrong = Hex("#EEF0F3");  // Baked
        public static readonly Color SurfaceDark = Hex("#0A0B0D");
        public static readonly Color SurfaceDarkElevated = Hex("#16181C");  // Baked
        public static readonly Color TextPrimary = Hex("#0A0B0D");  // Baked
        public static readonly Color TextBody = Hex("#5B616E");  // Baked
        public static readonly Color TextMuted = Hex("#7C828A");  // Baked
        public static readonly Color TextDisabled = Hex("#A8ACB3");
        public static readonly Color TextOnPrimary = Hex("#FFFFFF");
        public static readonly Color TextOnDark = Hex("#FFFFFF");  // Baked
        public static readonly Color TextOnDarkSoft = Hex("#A8ACB3");
        public static readonly Color Border = Hex("#DEE1E6");  // Baked
        public static readonly Color BorderSoft = Hex("#EEF0F3");
        public static readonly Color SemanticUp = Hex("#05B169");
        public static readonly Color SemanticDown = Hex("#CF202F");  // Baked

        // ---- Overlay scrim (Overlay Dim: #000000 at 50%) ----
        public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.5f);

        // ---- LoginOptionButton (sized by the login screen itself) ----
        public const float ButtonWidthPc = 305f;  // Baked
        public const float ButtonHeightPc = 48f;  // Baked
        public const float ButtonWidthMobile = 260f;  // Baked
        public const float ButtonHeightMobile = 44f;
        public const float ButtonPaddingH = 16f;      // Baked: Button.HorizontalPadding = Spacing.16
        public const float ButtonIconSize = 20f;  // Baked
        public const float ButtonIconTextGap = 10f;  // Baked
        public const float ButtonBorderWidth = 1f;  // Baked
        public const float ButtonFontSize = 14f;  // Baked: Typography.Button: 14 / 500
        public const float ButtonLineHeight = 1.15f;  // Typography.Button default (single line)
        public const float LoginButtonLineHeight = 1.4f; // login buttons: room for two-line provider labels
        public const float ButtonRadius = 100f;       // Baked (via BakedPillRadius): clamps to height/2 -> pill
        public const float ChipRadius = 4f;           // Baked: fallback letter chip

        // The radius the shared pill sprites are baked at (the PC button height): the baker
        // draws at this radius, and widgets rescale the 9-slice to their own height from it.
        public static readonly float BakedPillRadius = Mathf.Min(ButtonRadius, ButtonHeightPc / 2f);

        // Icon-only buttons (the copy button) darken their glyph tint on Hover/Pressed.
        public static readonly Color IconButtonHoverTint = new Color(0.6f, 0.6f, 0.6f, 1f);  // Baked
        public static readonly Color IconButtonPressedTint = new Color(0.4f, 0.4f, 0.4f, 1f);  // Baked

        // ---- Button states ----
        public static readonly Color StateHoverBg = Hex("#F0F6FF");   // Primary 4%, PC only
        public static readonly Color StatePressedBg = Hex("#EBF3FF"); // Primary 8%
        public const float StatePressedScale = 0.98f;
        public const float StateTransitionSeconds = 0.10f;            // Baked: 100ms ease-out
        public const float StatePressedTransitionSeconds = 0.08f;     // 80ms ease-out
        // Focus indicator: a ring outside the control, drawn only for keyboard and
        // controller focus — never for a pointer click, and never on a touch-only device.
        // It changes no layout size. The reference values are the Primary color, 2px wide,
        // with 2px between the control's edge and the ring's inner edge; a game may restyle
        // all three as long as focus stays distinguishable.
        public static readonly Color FocusRingColor = Primary;
        public const float FocusRingWidth = 2f;
        public const float FocusRingOffset = 2f;

        // ---- Breakpoints (in density-independent px of the safe area) ----
        public const float TabletMinWidth = 640f;           // below: Mobile, single column
        public const float MobileLandscapeMaxHeight = 480f; // mobile landscape rule wins under this
        public const float PcCompactMaxHeight = 600f;       // short PC window: same layout, content scrolls

        // ---- Overlay canvas (the screen's own ScreenSpaceOverlay canvas) ----
        public const int CanvasSortingOrder = 100;     // above the app's own UI; raise on clash
        public static readonly Vector2 ReferenceResolutionPc = new Vector2(1920f, 1080f);
        public static readonly Vector2 ReferenceResolutionPortrait = new Vector2(375f, 812f);
        public static readonly Vector2 ReferenceResolutionLandscape = new Vector2(812f, 375f);
        public const float ResizeSettleSeconds = 0.25f;  // debounce window for a resize in progress

        // ---- Layout spacing (values sit on the 4px scale) ----
        public const float ButtonRowGap = 8f;
        public const float TitleToGroupGapPc = 32f;
        public const float TitleToGroupGapPortrait = 24f;
        public const float TitleToGroupGapLandscape = 20f;
        // Distance one mouse-wheel notch scrolls. uGUI defaults this to 1, which moves the
        // content a few units per notch — a wheel that reads as broken on a desktop.
        public const float ScrollWheelSensitivity = 24f;
        // Top (and bottom) margin kept when content taller than the safe area scrolls.
        public const float ScrollTopMarginPc = 32f;
        public const float ScrollTopMarginPortrait = 24f;
        public const float ScrollTopMarginLandscape = 16f;
        public const float SideMarginPc = 32f;
        public const float SideMarginMobile = 16f;
        public const float GridColumnGap = 8f;
        public const float GridRowGap = 8f;
        public const float TwoColumnMinWidth = 528f;   // 260×2 + 8
        public const int GridThresholdProviders = 5;   // 5+ providers may go 2-col
        public const int MaxProviders = 8;             // upper bound the login layout supports

        // ---- Title (Title MD: 18/600; max two lines) ----
        public const float TitleFontSize = 18f;

        // ---- CloseButton (bare 18px light X, 48px touch target, no white circle) ----
        public const float CloseHitSize = 48f;  // Baked
        public const float CloseIconSize = 18f;  // Baked
        // Hover/Pressed draw a translucent dark backdrop behind the X. Note the visibility
        // limit: over the 50% Overlay Dim, a dark backdrop reads only when the scene behind
        // is reasonably light.
        public const float CloseHoverBgAlpha = 0.18f;  // Baked
        public const float ClosePressedBgAlpha = 0.30f;  // Baked

        // ---- Action button (the shared Primary / Destructive pill action) ----
        public const float ActionButtonHeight = ButtonHeightMobile;  // Baked

        // ---- Common Popup (Surface Card on the Overlay Dim; Radius.24 + 1px Border) ----
        public const float CommonPopupRadius = 24f;    // Baked: drawn in uikit_rounded_24(+_fill)
        public const float CommonPopupBorderWidth = 1f;  // Baked

        // ---- Account popup (sized by the account screen itself) ----
        public const float PopupMaxWidth = 960f;
        public const float PopupPaddingPc = 32f;
        public const float PopupPaddingMobile = 16f;
        public const float PopupSectionGapPc = 24f;
        public const float PopupSectionGapMobile = 16f;
        public const float HeaderHeight = 48f;
        public const float HeaderIconSize = 20f;
        public const float LogoutConfirmWidth = 360f;  // the logout-confirmation card
        // AccountLinkRow reuses the shared button tokens: 44px rows in every layout.
        public const float AccountRowHeight = ButtonHeightMobile;  // Baked
        public const float AccountStateIconSize = 16f; // Baked: right-side chevron / check
        public const float InfoCaptionFontSize = 13f;   // Caption 13/400
        public const float InfoValueFontSize = 14f;
        public const float SectionTitleFontSize = 14f;

        // ---- Username login (sized by the username-login screen itself) ----
        public const float FormWidth = 480f;            // Baked: PC · Landscape; Portrait stretches
        public const float FormPanelMaxWidth = 720f;
        public const float FormPanelPaddingPc = 32f;
        public const float FormPanelPaddingPortrait = 16f;
        public const float FormPanelPaddingLandscape = 24f;
        public const float InputHeight = 48f;  // Baked
        public const float InputRadius = 12f;           // Baked: drawn in uikit_rounded_12
        public const float InputGroupGapPc = 16f;
        public const float InputGroupGapMobile = 12f;
        public const float InputLabelFontSize = 14f;  // Baked
        public const float InputFontSize = 14f;  // Baked
        public const float InputErrorFontSize = 14f;    // Baked: Body SM — never Caption
        public const float InputErrorGap = 8f;          // Baked: Spacing.8
        public const float InputToggleHitSize = 48f;    // Baked: visibility toggle: 48×48 hit, 20px glyph
        public const float LinkFontSize = 14f;
        public const float LinkHitHeight = 44f;         // minimum link touch target (PC)
        public const float LinkHitSizeMobile = 48f;     // Mobile: 48×48 minimum, both axes

        // ---- Toast (own overlay layer; Radius.16, Body SM 14, dark elevated surface) ----
        public const float ToastMaxWidth = 360f;        // Mobile: min(360, width - 32)
        public const float ToastMaxWidthPc = 560f;      // PC: min(560, width - 32)
        public const float ToastSideMargin = 16f;
        // Baked: floored at the Kit's shared 48px control height (inputs, hit targets),
        // so a one-line toast never renders thinner than the controls around it.
        public const float ToastMinHeight = 48f;
        public const float ToastEdgeOffsetBottom = 20f; // PC · Portrait
        public const float ToastEdgeOffsetTop = 16f;    // Landscape
        public const float ToastFontSize = 14f;  // Baked
        public const float ToastRadius = 16f;  // Baked
        // x horizontal, y vertical
        public static readonly Vector2 ToastPadding = new Vector2(16f, 12f);  // Baked
        public const float ToastEnterExitSeconds = 0.2f;
        public const float ToastShowSeconds = 4f;
        public const float ToastShowSecondsTwoLines = 5f;

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
