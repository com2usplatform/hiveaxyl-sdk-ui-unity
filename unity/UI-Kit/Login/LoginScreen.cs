// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>One entry of the provider list. The app decides which providers appear and in
    /// which order; the Kit only renders them.</summary>
    public sealed class LoginProviderOption
    {
        public LoginProviderOption(string id, string label, string chipText,
            Color? chipFrom = null, Color? chipTo = null, Sprite icon = null,
            string accessibilityName = null)
        {
            Id = id;
            Label = label;
            ChipText = chipText;
            ChipFrom = chipFrom;
            ChipTo = chipTo;
            Icon = icon;
            AccessibilityName = string.IsNullOrEmpty(accessibilityName) ? label : accessibilityName;
        }

        /// <summary>Raised through <see cref="LoginScreenOptions.OnProviderSelected"/> as-is.</summary>
        public string Id { get; }

        /// <summary>The button text. Localized copy comes from the app.</summary>
        public string Label { get; }

        /// <summary>Fallback chip letters — a placeholder, not a stand-in mark, drawn only when
        /// the Kit has no mark for <see cref="Id"/> (substitute icons are not allowed).</summary>
        public string ChipText { get; }

        /// <summary>Optional fallback-chip gradient start color.</summary>
        public Color? ChipFrom { get; }

        /// <summary>Optional fallback-chip gradient end color.</summary>
        public Color? ChipTo { get; }

        /// <summary>The app-supplied icon — the official brand asset the app obtained under
        /// the provider's own guideline. Null falls back to the drop-in file and then the
        /// letter-chip placeholder; the Kit itself bundles no brand assets.</summary>
        public Sprite Icon { get; }

        /// <summary>The name a screen reader announces for the button: the full provider
        /// label by default, never abbreviated; the mark itself is not read.</summary>
        public string AccessibilityName { get; }
    }

    /// <summary>Everything the app passes in. The Kit is pure UI: it raises events and renders
    /// what it is given — SDK calls stay with the app or the sample Recipe.</summary>
    public sealed class LoginScreenOptions
    {
        /// <summary>The providers to list, in display order. Required, at least one.</summary>
        public IReadOnlyList<LoginProviderOption> Providers { get; set; }

        /// <summary>Called with the provider id when a button is clicked.</summary>
        public Action<string> OnProviderSelected { get; set; }

        /// <summary>Called when the overlay asks to close: the close button, the backdrop, or
        /// Esc. Navigation is the app's.</summary>
        public Action OnClose { get; set; }

        /// <summary>The screen title — the app's string for
        /// <see cref="AxylUIStringKeys.Login"/>. The Kit holds no copy of its own: an unset
        /// string shows its key.</summary>
        public string Title { get; set; } = AxylUIStringKeys.Login;

        /// <summary>The close button's accessibility name (<see cref="AxylUIStringKeys.Close"/>).</summary>
        public string CloseLabel { get; set; } = AxylUIStringKeys.Close;

        /// <summary>True for a right-to-left language (Arabic, Hebrew, Persian — see
        /// <see cref="AxylUIDirection.IsRightToLeft"/>): text aligns right and every
        /// horizontal row runs right-to-left; brand marks are never mirrored.</summary>
        public bool RightToLeft { get; set; }

        /// <summary>Set to pin a layout; leave null to pick one from the display.</summary>
        public AxylUIScreenLayout? ForcedLayout { get; set; }

        /// <summary>The reference provider set, in the reference order, each labeled with
        /// its string key (<see cref="AxylUIStringKeys.Provider"/>) — the app replaces the
        /// labels with its localized strings. Steam and Custom carry gradients for the
        /// fallback chip; app-supplied marks are used when present. Guest closes the list
        /// with the Kit's own icon; whether to offer it is the app's call.</summary>
        public static List<LoginProviderOption> DefaultProviders()
        {
            Color steamFrom = Hex("#66C0F4"), steamTo = Hex("#1B8FBF");
            Color customFrom = Hex("#A855F7"), customTo = Hex("#6366F1");
            return new List<LoginProviderOption>
            {
                new("google", AxylUIStringKeys.Provider("google"), "G"),
                new("apple", AxylUIStringKeys.Provider("apple"), "A"),
                new("googlePlayGames", AxylUIStringKeys.Provider("googlePlayGames"), "GP"),
                new("steam", AxylUIStringKeys.Provider("steam"), "S", steamFrom, steamTo),
                new("x", AxylUIStringKeys.Provider("x"), "X"),
                new("username", AxylUIStringKeys.Provider("username"), "U"),
                new("custom", AxylUIStringKeys.Provider("custom"), "C", customFrom, customTo),
                new("guest", AxylUIStringKeys.Provider("guest"), "Gu"),
            };
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }

    /// <summary>
    /// The login provider-select screen: backdrop, title, provider buttons, close
    /// button and the shared failure toast. The reusable widgets are prefabs that
    /// this screen instantiates and fills; the screen-level composition — canvas, backdrop,
    /// layout containers — stays code so the three layout variants remain one code path. The
    /// content container is transparent: the app's own screen shows through the dim.
    /// </summary>
    /// <remarks>
    /// <para><b>How to customize.</b> Colors, sizes and spacing all live in
    /// <see cref="AxylUITheme"/> — change them there so every screen moves together. The widget
    /// look lives in the prefabs under Resources/UIKit/Prefabs (rebake with the AxylUIBaker after
    /// a structural change). Copy (title, button labels, error messages) comes in through
    /// <see cref="LoginScreenOptions"/>. The screen itself is built in the numbered steps of
    /// <see cref="Build"/>; each step is one visual region, so a layout change is usually one
    /// method.</para>
    /// </remarks>
    public sealed class LoginScreen : MonoBehaviour
    {
        private FailureToast m_toast;
        private TapToClose m_backdrop;
        private LoginScreenOptions m_options;
        private readonly List<Selectable> m_tabOrder = new List<Selectable>();
        private GameObject m_previousSelection;
        private readonly AxylUIScreenSizeWatch m_sizeWatch = new AxylUIScreenSizeWatch();

        /// <summary>Creates the screen on its own overlay canvas and shows it.</summary>
        public static LoginScreen Show(LoginScreenOptions options)
        {
            if (options?.Providers == null || options.Providers.Count == 0
                || options.Providers.Count > AxylUITheme.MaxProviders)
            {
                throw new ArgumentException(
                    $"LoginScreenOptions.Providers must hold 1 to {AxylUITheme.MaxProviders} providers.");
            }

            var layout = options.ForcedLayout ?? AxylUIScreen.DetectLayout();
            var screen = AxylUIScreen.CreateCanvas("AxylUILoginScreen", layout)
                .AddComponent<LoginScreen>();
            screen.m_options = options;
            // The caller's selection, remembered once: Dismiss returns focus there. (A
            // rebuild for a new screen size must not overwrite it — the selection is then
            // inside this screen.)
            screen.m_previousSelection = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            screen.Build(layout);
            return screen;
        }

        /// <summary>Shows the shared failure toast. Message wording is the app's.</summary>
        public void ShowFailure(string message)
        {
            m_toast.Show(message);
        }

        /// <summary>Removes the screen and returns focus to whatever held it before the overlay
        /// opened. The backdrop and everything on it go together.</summary>
        public void Dismiss()
        {
            GetComponent<AxylUIFocusTrap>()?.Release(); // let focus leave before the frame ends
            var eventSystem = EventSystem.current;
            if (eventSystem != null && m_previousSelection != null)
            {
                eventSystem.SetSelectedGameObject(m_previousSelection);
            }

            Destroy(gameObject);
        }

        // Esc closes the overlay like the close button does. Closing itself is the
        // app's move — the Kit only raises the request. Tab / Shift+Tab walk the close
        // button first, then the providers in order.
        private void Update()
        {
            if (AxylUIInput.EscapePressed())
            {
                m_options.OnClose?.Invoke();
            }

            if (AxylUIInput.TabPressed(out bool backward))
            {
                AxylUIInput.MoveFocus(m_tabOrder, backward);
            }

            if (m_sizeWatch.Changed())
            {
                RebuildForScreenChange();
            }
        }

        // A rotation or a window resize invalidates the layout choice, the scaler
        // reference, and every width computed at build time, so the screen rebuilds in
        // place — the component and the app's callbacks survive.
        private void RebuildForScreenChange()
        {
            float? scrollPosition = AxylUIScreen.ScrollPosition(gameObject);
            int selectedIndex = SelectedTabIndex();
            var toast = m_toast != null ? m_toast.Capture() : null;

            var layout = m_options.ForcedLayout ?? AxylUIScreen.DetectLayout();
            AxylUIScreen.ApplyScaler(gameObject, layout);
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            m_tabOrder.Clear();
            Build(layout);
            if (selectedIndex >= 0 && selectedIndex < m_tabOrder.Count)
            {
                EventSystem.current?.SetSelectedGameObject(m_tabOrder[selectedIndex].gameObject);
            }

            if (toast != null)
            {
                m_toast.Restore(toast.Value); // a message mid-read survives the rotation
            }

            AxylUIScreen.RestoreScrollPosition(gameObject, scrollPosition);
        }

        // Which control of the keyboard path holds the focus (-1: none of them) — the same
        // control is selected again after a rebuild.
        private int SelectedTabIndex()
        {
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            for (int i = 0; i < m_tabOrder.Count; i++)
            {
                if (m_tabOrder[i] != null && m_tabOrder[i].gameObject == selected)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Build(AxylUIScreenLayout layout)
        {
            var canvas = (RectTransform)transform;
            bool grid = UseTwoColumns(layout);

            // Step 1 — backdrop: dims the app behind, blocks its input, and closes the overlay
            // when clicked, same as Esc and the close button. It stays on the canvas itself
            // (edge to edge, under the notch too); everything else sits on the safe-area root,
            // created after the backdrop so it draws above the dim (uGUI renders siblings
            // in order).
            BuildBackdrop(canvas);

            var root = AxylUIScreen.ContentRoot(gameObject);

            // Step 2 — the transparent content column: title above the provider group.
            var content = BuildContentColumn(root, layout, grid);

            // Step 3 — title.
            BuildTitle(content);

            // Step 4 — provider buttons: one column, or two columns filled row by row. Each
            // grid row is its own horizontal group, so only the buttons sharing a row share
            // its height (a two-line label grows its row alone), and an odd last button sits
            // in the first column with the second left empty.
            var group = BuildProviderGroup(content, layout, grid);
            var providerButtons = new List<Selectable>(m_options.Providers.Count);
            RectTransform row = null;
            for (int i = 0; i < m_options.Providers.Count; i++)
            {
                if (grid && i % 2 == 0)
                {
                    row = BuildGridRow(group);
                }

                var button = AddProviderButton(grid ? row : group, m_options.Providers[i], layout);
                providerButtons.Add(button.GetComponent<Button>());
            }

            // Step 5 — close control, top-right of the overlay layer; the first stop of the
            // keyboard path, ahead of the providers.
            var close = InstantiateWidget<CloseButton>("CloseButton", root);
            close.Configure(() => m_options.OnClose?.Invoke(), rightToLeft: m_options.RightToLeft);
            m_tabOrder.Add(close.GetComponent<Button>());
            m_tabOrder.AddRange(providerButtons);

            // Focus stays inside the overlay (a modal keeps focus until it closes), and
            // the screen reader gets every control by its full name — the close button
            // first, then the providers.
            AxylUIFocusTrap.Install(gameObject, m_tabOrder);
            var accessible = new List<AxylUIAccessibleControl>(m_tabOrder.Count)
            {
                new AxylUIAccessibleControl(m_tabOrder[0], m_options.CloseLabel),
            };
            for (int i = 0; i < providerButtons.Count; i++)
            {
                accessible.Add(new AxylUIAccessibleControl(
                    providerButtons[i], m_options.Providers[i].AccessibilityName));
            }

            AxylUIAccessibility.Install(gameObject, accessible);

            // Step 6 — the shared failure toast on its own layer above everything else,
            // hidden until ShowFailure.
            m_toast = InstantiateWidget<FailureToast>("FailureToast", AxylUIScreen.ToastLayer(gameObject));
            m_toast.Configure(
                topEdge: layout == AxylUIScreenLayout.Landscape,
                wide: layout == AxylUIScreenLayout.Pc,
                availableWidth: AxylUIScreen.SafeSize(layout).x,
                rightToLeft: m_options.RightToLeft);

            // Step 7 — content taller than the screen scrolls: the column moves into a
            // clipped scroll view that keeps the layout's top margin. The backdrop forwards
            // drags and wheel scrolls to it.
            m_backdrop.Scroll = WrapInScrollIfNeeded(root, content, layout);

            // The close button is selected on open, with no ring shown: the screen
            // appearing is not a choice the player has made yet. The focus trap starts
            // the keyboard path at that button, the first stop of the navigation order,
            // and a rebuild (a rotation) puts the selection back on the button the
            // player had. Show remembered the app's own selection for Dismiss to restore.
        }

        // Content exceeding the available height (minus the layout's top margin, kept above
        // and below) scrolls vertically; shorter content stays centered with no scroll view.
        private static ScrollRect WrapInScrollIfNeeded(
            RectTransform root, RectTransform content, AxylUIScreenLayout layout)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float margin = layout switch
            {
                AxylUIScreenLayout.Pc => AxylUITheme.ScrollTopMarginPc,
                AxylUIScreenLayout.Portrait => AxylUITheme.ScrollTopMarginPortrait,
                _ => AxylUITheme.ScrollTopMarginLandscape,
            };
            if (content.rect.height <= AxylUIScreen.SafeSize(layout).y - margin * 2f)
            {
                return null;
            }

            // The scroll view fills the root inside the margins; the column moves inside as
            // the scrolled child.
            var rect = AxylUIScrollView.Wrap(content);
            // The column's own image catches taps and drags now; leaving this one on would
            // swallow taps on the dim beside the column only when the list scrolls.
            rect.GetComponent<Image>().raycastTarget = false;
            Stretch(rect);
            rect.offsetMin = new Vector2(0f, margin);
            rect.offsetMax = new Vector2(0f, -margin);
            return rect.GetComponent<ScrollRect>();
        }

        // Two columns need five providers or more AND room for them: two buttons plus the
        // column gap inside the side margins. Otherwise everything stacks.
        private bool UseTwoColumns(AxylUIScreenLayout layout)
        {
            if (layout != AxylUIScreenLayout.Landscape
                || m_options.Providers.Count < AxylUITheme.GridThresholdProviders)
            {
                return false;
            }

            float available = AxylUIScreen.SafeSize(layout).x - AxylUITheme.SideMarginMobile * 2f;
            return available >= AxylUITheme.TwoColumnMinWidth;
        }

        private void BuildBackdrop(RectTransform root)
        {
            var go = new GameObject("Backdrop", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            Stretch(rect);

            // The Image blocks raycasts to the app behind (Overlay Dim). A tap asks the
            // app to close, like Esc — a tap only: a swipe that starts here hands its drag
            // (and a wheel its scroll) to the list's ScrollRect, so the dim beside a
            // scrolling column scrolls like the column and a swipe never closes the screen.
            go.AddComponent<Image>().color = AxylUITheme.OverlayDim;
            m_backdrop = go.AddComponent<TapToClose>();
            m_backdrop.OnTap = () => m_options.OnClose?.Invoke();
        }

        // The backdrop's pointer behavior. Not a Selectable: focus never lands here.
        private sealed class TapToClose : MonoBehaviour, IPointerClickHandler,
            IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
            IScrollHandler
        {
            public Action OnTap;
            public ScrollRect Scroll; // null while the list does not scroll

            public void OnPointerClick(PointerEventData e)
            {
                // Left button only, as a Button would: uGUI raises click events for the
                // right and middle buttons too.
                if (e.button == PointerEventData.InputButton.Left && !e.dragging)
                {
                    OnTap?.Invoke();
                }
            }

            public void OnInitializePotentialDrag(PointerEventData e) =>
                Scroll?.OnInitializePotentialDrag(e);

            public void OnBeginDrag(PointerEventData e) => Scroll?.OnBeginDrag(e);

            public void OnDrag(PointerEventData e) => Scroll?.OnDrag(e);

            public void OnEndDrag(PointerEventData e) => Scroll?.OnEndDrag(e);

            public void OnScroll(PointerEventData e) => Scroll?.OnScroll(e);
        }

        private static RectTransform BuildContentColumn(
            RectTransform root, AxylUIScreenLayout layout, bool grid)
        {
            var go = new GameObject("Content", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            rect.sizeDelta = new Vector2(
                grid ? AxylUITheme.TwoColumnMinWidth : ButtonWidth(layout), 0f);

            // A tap in the gaps between the buttons (or on the title) stays inside the
            // column instead of falling through to the backdrop and closing the screen;
            // the dim beside the column still closes. Drags pass up to a wrapping scroll.
            go.AddComponent<Image>().color = Color.clear;

            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = layout switch
            {
                AxylUIScreenLayout.Pc => AxylUITheme.TitleToGroupGapPc,
                AxylUIScreenLayout.Portrait => AxylUITheme.TitleToGroupGapPortrait,
                _ => AxylUITheme.TitleToGroupGapLandscape,
            };
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private void BuildTitle(RectTransform content)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(content, false);

            // Title MD 18/600 in every layout; light on the dim, no card behind
            // it; wraps up to two lines.
            var title = go.AddComponent<TextMeshProUGUI>();
            title.font = AxylUIRuntimeAssets.Font();
            title.fontSize = AxylUITheme.TitleFontSize;
            title.fontWeight = FontWeight.SemiBold;
            title.color = AxylUITheme.TextOnDark;
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.maxVisibleLines = 2;
            AxylUIDirection.Apply(title, m_options.RightToLeft);
            title.text = m_options.Title;
            title.raycastTarget = false;
        }

        private RectTransform BuildProviderGroup(
            RectTransform content, AxylUIScreenLayout layout, bool grid)
        {
            var go = new GameObject("Providers", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(content, false);

            // One column of buttons, or one column of two-button rows: the same vertical
            // stack either way, the rows spaced by the grid's row gap.
            var rows = go.AddComponent<VerticalLayoutGroup>();
            rows.spacing = grid ? AxylUITheme.GridRowGap : AxylUITheme.ButtonRowGap;
            rows.childAlignment = grid ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
            rows.childControlWidth = false;
            rows.childControlHeight = false;
            rows.childForceExpandWidth = false;
            rows.childForceExpandHeight = false;
            return rect;
        }

        // One row of the two-column layout: two buttons side by side at the column gap,
        // the row as tall as its taller button. Reading direction decides which side the
        // first button takes.
        private RectTransform BuildGridRow(RectTransform group)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(group, false);
            // Always both columns wide, so an odd last button keeps its first-column slot
            // (the left one, or the right one when reading right-to-left).
            rect.sizeDelta = new Vector2(AxylUITheme.TwoColumnMinWidth, 0f);
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = AxylUITheme.GridColumnGap;
            row.childAlignment = TextAnchor.UpperLeft; // Apply mirrors it for RTL
            AxylUIDirection.Apply(row, m_options.RightToLeft);
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private LoginOptionButton AddProviderButton(
            RectTransform group, LoginProviderOption provider, AxylUIScreenLayout layout)
        {
            var id = provider.Id;

            // The app-supplied icon first, then the drop-in file. A provider with neither
            // keeps the prefab's letter-chip placeholder — over the app's gradient when given.
            var icon = provider.Icon != null ? provider.Icon : ProviderIcons.TryGet(id);
            var glyph = (string)null;
            if (icon == null)
            {
                glyph = provider.ChipText;
                if (provider.ChipFrom.HasValue && provider.ChipTo.HasValue)
                {
                    icon = AxylUIRuntimeAssets.Gradient(provider.ChipFrom.Value, provider.ChipTo.Value);
                }
            }

            var button = InstantiateWidget<LoginOptionButton>("LoginOptionButton", group);
            button.Configure(
                provider.Label, icon, Color.white, glyph,
                ButtonWidth(layout), ButtonHeight(layout),
                () => m_options.OnProviderSelected?.Invoke(id), m_options.RightToLeft);
            return button;
        }

        // The widget prefabs live in Resources so this static-entry screen can load them
        // without a scene reference.
        private static T InstantiateWidget<T>(string name, RectTransform parent)
            where T : Component
        {
            var prefab = Resources.Load<GameObject>("UIKit/Prefabs/" + name);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"UI Kit prefab missing at Resources/UIKit/Prefabs/{name} — was the Kit " +
                    "copied whole? Rebake with AxylUIBaker if prefabs were removed.");
            }

            return Instantiate(prefab, parent, false).GetComponent<T>();
        }

        private static float ButtonWidth(AxylUIScreenLayout layout) =>
            layout == AxylUIScreenLayout.Pc ? AxylUITheme.ButtonWidthPc : AxylUITheme.ButtonWidthMobile;

        private static float ButtonHeight(AxylUIScreenLayout layout) =>
            layout == AxylUIScreenLayout.Pc ? AxylUITheme.ButtonHeightPc : AxylUITheme.ButtonHeightMobile;

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
