// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>One provider row of the account popup. The app decides which providers appear,
    /// their order, and each link state; the Kit only renders them.</summary>
    public sealed class AccountProviderLink
    {
        public AccountProviderLink(string id, string label, string chipText,
            bool connected = false, bool interactable = true, Sprite icon = null)
        {
            Id = id;
            Label = label;
            ChipText = chipText;
            Connected = connected;
            Interactable = interactable;
            Icon = icon;
        }

        /// <summary>Raised through <see cref="AccountScreenOptions.OnProviderSelected"/> as-is.
        /// Uses the same ids as the login screen, so both screens load the same mark file.</summary>
        public string Id { get; }

        /// <summary>The row text. Localized copy comes from the app.</summary>
        public string Label { get; }

        /// <summary>Fallback chip letters, drawn only when the Kit has no mark for
        /// <see cref="Id"/> (substitute icons are not allowed).</summary>
        public string ChipText { get; }

        /// <summary>True renders the linked state: Primary emphasis and a check, no plus.</summary>
        public bool Connected { get; }

        /// <summary>False renders the Disabled state; the row cannot be selected.</summary>
        public bool Interactable { get; }

        /// <summary>The app-supplied icon — the official brand asset the app obtained under
        /// the provider's own guideline. Null falls back to the drop-in file and then the
        /// letter-chip placeholder; the Kit itself bundles no brand assets.</summary>
        public Sprite Icon { get; }
    }

    /// <summary>Everything the app passes in. The Kit is pure UI: it renders the given account
    /// info and link states and raises events — SDK calls, the delete-confirmation flow, and
    /// the copied-feedback toast stay with the app.</summary>
    public sealed class AccountScreenOptions
    {
        /// <summary>The providers to list, in display order. Required, at least one; the
        /// reference layout is seven of them — two columns by four rows on PC and Mobile
        /// Landscape, with the last row's second cell left empty, and one column by seven
        /// rows in Mobile Portrait (Guest is not a link row).</summary>
        public IReadOnlyList<AccountProviderLink> Providers { get; set; }

        /// <summary>Read-only account fields. The Kit displays them as given.</summary>
        public string Nickname { get; set; }

        public string Server { get; set; }

        /// <summary>The player's customer-support code. Typed to match the SDK's player id
        /// (a 64-bit number), so the value a login recipe hands back drops in directly.</summary>
        public long CsCode { get; set; }

        /// <summary>Called with the provider id when a row is selected — for a disconnected
        /// provider that means "start the login/link flow"; for a connected one the behavior
        /// follows the app's own link policy.</summary>
        public Action<string> OnProviderSelected { get; set; }

        /// <summary>Called after the copy button has put <see cref="CsCode"/> on the clipboard
        /// — show the app's "copied" toast here.</summary>
        public Action<long> OnCsCodeCopied { get; set; }

        /// <summary>Called when the delete-account action is selected. The Kit never deletes
        /// anything — open the app's confirmation flow here.</summary>
        public Action OnDeleteAccount { get; set; }

        /// <summary>Called when the logout action is selected.</summary>
        public Action OnLogout { get; set; }

        /// <summary>Called when the popup asks to close: the close button or Esc (the dim
        /// backdrop blocks input but does not close). Navigation is the app's.</summary>
        public Action OnClose { get; set; }

        // Display strings — the app's localized strings for the keys in AxylUIStringKeys.
        // The Kit holds no copy of its own: an unset string shows its key.
        public string Title { get; set; } = AxylUIStringKeys.AccountTitle;

        public string NicknameLabel { get; set; } = AxylUIStringKeys.AccountNickname;

        public string ServerLabel { get; set; } = AxylUIStringKeys.AccountServer;

        public string CsCodeLabel { get; set; } = AxylUIStringKeys.AccountCsCode;

        public string LinkSectionTitle { get; set; } = AxylUIStringKeys.AccountLinkTitle;

        public string DeleteAccountLabel { get; set; } = AxylUIStringKeys.AccountDelete;

        public string LogoutLabel { get; set; } = AxylUIStringKeys.AccountLogout;

        // The logout-confirmation popup the logout action opens; OnLogout is raised only
        // after the player confirms there.
        public string LogoutConfirmMessage { get; set; } = AxylUIStringKeys.AccountLogoutConfirmTitle;

        public string LogoutCancelLabel { get; set; } = AxylUIStringKeys.AccountLogoutCancel;

        public string LogoutConfirmLabel { get; set; } = AxylUIStringKeys.AccountLogoutConfirm;

        // Accessibility names — read by the screen reader, never drawn.
        public string CloseLabel { get; set; } = AxylUIStringKeys.Close;

        public string CopyLabel { get; set; } = AxylUIStringKeys.AccountCsCodeCopy;

        /// <summary>Set to pin a layout; leave null to pick one from the display.</summary>
        public AxylUIScreenLayout? ForcedLayout { get; set; }

        /// <summary>True for a right-to-left language (Arabic, Hebrew, Persian — see
        /// <see cref="AxylUIDirection.IsRightToLeft"/>): text aligns right and every
        /// horizontal row runs right-to-left; brand marks are never mirrored.</summary>
        public bool RightToLeft { get; set; }

        /// <summary>The reference provider set, in the reference order. Labels default to
        /// their string keys (never English) and nothing is connected — the app passes its
        /// own list with localized labels and the player's real link states.</summary>
        public static List<AccountProviderLink> DefaultProviders()
        {
            return new List<AccountProviderLink>
            {
                new("google", AxylUIStringKeys.AccountProvider("google"), "G"),
                new("apple", AxylUIStringKeys.AccountProvider("apple"), "A"),
                new("googlePlayGames", AxylUIStringKeys.AccountProvider("googlePlayGames"), "GP"),
                new("steam", AxylUIStringKeys.AccountProvider("steam"), "S"),
                new("x", AxylUIStringKeys.AccountProvider("x"), "X"),
                new("username", AxylUIStringKeys.AccountProvider("username"), "U"),
                new("custom", AxylUIStringKeys.AccountProvider("custom"), "C"),
            };
        }
    }

    /// <summary>
    /// The account link-and-logout popup: a centered card over the dimmed game, with a header
    /// (settings glyph, title, close), the read-only account info (nickname / server / CS Code
    /// with a copy button), the provider-link grid — every provider visible, the grid itself
    /// never scrolling — and the two bottom actions (delete account, log out). The
    /// reusable widgets are prefabs that this screen instantiates and fills; the screen-level
    /// composition stays code so the layout variants remain one code path.
    /// </summary>
    /// <remarks>
    /// <para><b>Layout variants.</b> PC and Mobile Landscape share the Wide variant (account
    /// info in one row); Mobile Portrait stacks the info vertically. The actions sit side by
    /// side in every variant, and the provider grid runs two columns by four rows on the Wide
    /// variants and one column by seven rows in Portrait. Content taller than the screen
    /// scrolls between the fixed header and actions.</para>
    /// <para><b>How to customize.</b> Colors, sizes and spacing all live in
    /// <see cref="AxylUITheme"/>. The widget look lives in the prefabs under
    /// Resources/UIKit/Prefabs (rebake with the AxylUIBaker after a structural change). Copy
    /// comes in through <see cref="AccountScreenOptions"/>. The screen itself is built in the
    /// numbered steps of <see cref="Build"/>; each step is one visual region.</para>
    /// </remarks>
    public sealed class AccountScreen : MonoBehaviour
    {
        private AccountScreenOptions m_options;
        private readonly AxylUIScreenSizeWatch m_sizeWatch = new AxylUIScreenSizeWatch();
        private readonly List<AccountLinkRow> m_rows = new List<AccountLinkRow>();
        private readonly List<Selectable> m_tabOrder = new List<Selectable>();
        private readonly List<AxylUIAccessibleControl> m_accessible = new List<AxylUIAccessibleControl>();
        private AxylUIFocusTrap m_focusTrap;
        private IReadOnlyList<Selectable> m_currentOrder;
        private GameObject m_confirm;
        private FailureToast m_toast;
        private RectTransform m_toastLayer;
        private GameObject m_previousSelection;

        /// <summary>Creates the popup on its own overlay canvas and shows it.</summary>
        public static AccountScreen Show(AccountScreenOptions options)
        {
            if (options?.Providers == null || options.Providers.Count == 0)
            {
                throw new ArgumentException("AccountScreenOptions.Providers must not be empty.");
            }

            var layout = options.ForcedLayout ?? AxylUIScreen.DetectLayout();
            var screen = AxylUIScreen.CreateCanvas("AxylUIAccountScreen", layout)
                .AddComponent<AccountScreen>();
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

        /// <summary>Shows the popup's feedback toast — the channel for copy feedback (a
        /// <see cref="AxylUIToastVariant.Success"/>) and for link/unlink refusals the app
        /// wants the player to see. Message wording is the app's.</summary>
        public void ShowToast(string message, AxylUIToastVariant variant = AxylUIToastVariant.Error)
        {
            m_toast.Show(message, variant);
        }

        /// <summary>Removes the popup and returns focus to whatever held it before the overlay
        /// opened. The backdrop and everything on it go together.</summary>
        public void Dismiss()
        {
            m_focusTrap?.Release(); // let focus leave before the frame ends
            var eventSystem = EventSystem.current;
            if (eventSystem != null && m_previousSelection != null)
            {
                eventSystem.SetSelectedGameObject(m_previousSelection);
            }

            Destroy(gameObject);
        }

        // Esc closes the topmost layer: an open confirmation card first (as its cancel
        // would), otherwise the popup like the close button does. Closing the popup itself
        // is the app's move — the Kit only raises the request.
        private void Update()
        {
            if (AxylUIInput.EscapePressed())
            {
                if (m_confirm != null)
                {
                    CloseConfirm();
                }
                else
                {
                    m_options.OnClose?.Invoke();
                }
            }

            if (AxylUIInput.TabPressed(out bool backward))
            {
                AxylUIInput.MoveFocus(m_currentOrder, backward); // the card's ring while it is up
            }

            if (m_sizeWatch.Changed())
            {
                RebuildForScreenChange();
            }
        }

        // A rotation or a window resize invalidates the layout choice, the scaler
        // reference, and every width computed at build time, so the screen rebuilds in
        // place — the component and the app's callbacks survive (an open
        // confirmation card is dropped; the popup itself stays).
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

            m_rows.Clear();
            m_tabOrder.Clear();
            m_accessible.Clear();
            m_confirm = null; // the card, if any, went with the old tree
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
        // control is selected again after a rebuild. The index covers every tab stop, not
        // the provider rows alone, so a rotation while the close, copy, delete or log-out
        // control held the focus restores that control rather than dropping to a row.
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

        private static bool IsWide(AxylUIScreenLayout layout) =>
            layout != AxylUIScreenLayout.Portrait;

        private static float PopupPadding(AxylUIScreenLayout layout) =>
            layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.PopupPaddingPc
                : AxylUITheme.PopupPaddingMobile;

        private static float SectionGap(AxylUIScreenLayout layout) =>
            layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.PopupSectionGapPc
                : AxylUITheme.PopupSectionGapMobile;

        private void Build(AxylUIScreenLayout layout)
        {
            var canvas = (RectTransform)transform;
            var size = AxylUIScreen.SafeSize(layout);
            bool wide = IsWide(layout);

            // Step 1 — backdrop: dims the app behind and blocks its input; it does not close
            // the popup (only the close button and Esc do). It stays on the canvas itself
            // (edge to edge, under the notch too); everything else sits on the safe-area root,
            // created after the backdrop so it draws above the dim (uGUI renders siblings
            // in order).
            BuildBackdrop(canvas);

            var root = AxylUIScreen.ContentRoot(gameObject);

            // Step 2 — the popup card, centered; height follows its content.
            float sideMargin = layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.SideMarginPc
                : AxylUITheme.SideMarginMobile;
            float popupWidth = Mathf.Min(
                AxylUITheme.PopupMaxWidth, size.x - sideMargin * 2f);
            var popup = BuildPopupCard(root, popupWidth, layout);
            float contentWidth = popupWidth - PopupPadding(layout) * 2f;

            // Step 3 — header: settings glyph + title; the close control anchors itself to the
            // card's top-right corner, lifted out of the card's vertical flow.
            BuildHeader(popup);
            var close = InstantiateWidget<CloseButton>("CloseButton", popup);
            close.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            close.Configure(
                () => m_options.OnClose?.Invoke(), AxylUITheme.TextPrimary, m_options.RightToLeft);
            var closeButton = close.GetComponent<Button>();
            m_tabOrder.Add(closeButton);
            m_accessible.Add(new AxylUIAccessibleControl(closeButton, m_options.CloseLabel));

            // Step 4 — the content container: everything between the fixed header and the
            // fixed actions. When the popup outgrows the screen this part alone scrolls.
            var content = BuildContent(popup, layout);

            // Step 5 — account info: one row of three fields (Wide), or a vertical stack
            // (Portrait). The CS Code field carries the copy button.
            BuildAccountInfo(content, wide);

            // Step 6 — the link section: title + the provider grid (2 columns × 4 rows Wide,
            // 1 column × 7 rows Portrait), all rows visible; the grid itself never scrolls.
            BuildLinkSection(content, contentWidth, layout);

            // Step 7 — bottom actions: delete account (destructive) and log out (primary),
            // side by side at equal sizes in every variant.
            BuildActions(popup, contentWidth);

            // Step 8 — content taller than the screen scrolls between the fixed header and
            // actions; a popup that fits keeps its plain layout with no scroll view.
            LimitToScreenIfNeeded(root, popup, content, layout);

            // Focus stays inside the popup (a modal keeps focus until it closes):
            // close → copy → provider rows → delete → log out. The screen reader gets
            // the same controls by name.
            m_currentOrder = m_tabOrder;
            m_focusTrap = AxylUIFocusTrap.Install(gameObject, m_tabOrder);
            AxylUIAccessibility.Install(gameObject, m_accessible);

            // Step 9 — the shared feedback toast on its own layer above everything else —
            // an open confirmation card included — so it never moves the layout.
            m_toastLayer = AxylUIScreen.ToastLayer(gameObject);
            m_toast = InstantiateWidget<FailureToast>("FailureToast", m_toastLayer);
            m_toast.Configure(
                topEdge: layout == AxylUIScreenLayout.Landscape,
                wide: layout == AxylUIScreenLayout.Pc,
                availableWidth: size.x,
                rightToLeft: m_options.RightToLeft);

            // The close button is selected on open (no ring shown), so the content opens
            // at its top: selecting a row would scroll it into view, which on a short
            // screen would mean opening part-way down with the account info cut off. The
            // focus trap starts the keyboard path at the close button, the first stop of
            // the navigation order, and a rebuild (a
            // rotation) puts the selection back on the row the player had. Show remembered
            // the app's own selection for Dismiss to restore.
        }

        private void BuildBackdrop(RectTransform root)
        {
            var go = new GameObject("Backdrop", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            Stretch(rect, 0f);

            // The dim blocks raycasts to the app behind (a raycast-target Image absorbs
            // them); it does not close on tap — only the close button and Esc close,
            // so a mis-tap outside can't discard a form mid-edit.
            go.AddComponent<Image>().color = AxylUITheme.OverlayDim;
        }

        private RectTransform BuildPopupCard(
            RectTransform root, float width, AxylUIScreenLayout layout)
        {
            var go = new GameObject("AccountPopup", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            rect.sizeDelta = new Vector2(width, 0f);

            // Common Popup surface: a 1px border layer with the card fill just inside it.
            var card = go.AddComponent<Image>();
            card.sprite = AxylUIRuntimeAssets.RoundedRect(AxylUITheme.CommonPopupRadius);
            card.type = Image.Type.Sliced;
            card.color = AxylUITheme.Border;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            var fillRect = (RectTransform)fillGo.transform;
            fillRect.SetParent(rect, false);
            float inset = AxylUITheme.CommonPopupBorderWidth;
            Stretch(fillRect, inset);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = AxylUIRuntimeAssets.RoundedRect(AxylUITheme.CommonPopupRadius - inset);
            fill.type = Image.Type.Sliced;
            fill.color = AxylUITheme.Surface;
            fill.raycastTarget = false;
            fillGo.AddComponent<LayoutElement>().ignoreLayout = true;

            float pad = PopupPadding(layout);
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            column.spacing = SectionGap(layout);
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private RectTransform BuildContent(RectTransform popup, AxylUIScreenLayout layout)
        {
            var go = new GameObject("Content", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(popup, false);
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = SectionGap(layout);
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return rect;
        }

        // The popup may not outgrow the safe area: when it would, the content section gets a
        // scroll view sized to what remains beside the fixed header and actions.
        private static void LimitToScreenIfNeeded(
            RectTransform root, RectTransform popup, RectTransform content,
            AxylUIScreenLayout layout)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(popup);
            float margin = layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.SideMarginPc
                : AxylUITheme.SideMarginMobile;
            float available = AxylUIScreen.SafeSize(layout).y - margin * 2f;
            float overflow = popup.rect.height - available;
            if (overflow <= 0f)
            {
                return;
            }

            // The scroll view takes the content's place in the popup column at the reduced
            // height; the content column moves inside as the scrolled child.
            float visible = Mathf.Max(0f, content.rect.height - overflow);
            AxylUIScrollView.Wrap(content).gameObject
                .AddComponent<LayoutElement>().preferredHeight = visible;
        }

        private void BuildHeader(RectTransform popup)
        {
            var go = new GameObject("Header", typeof(RectTransform));
            go.transform.SetParent(popup, false);
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = AxylUITheme.ButtonIconTextGap;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            // Keep the header clear of the close button's corner — the close sits at the
            // trailing edge (right in LTR, left in RTL), and a long title would run under it.
            int reserve = Mathf.CeilToInt(AxylUITheme.CloseHitSize);
            row.padding = m_options.RightToLeft
                ? new RectOffset(reserve, 0, 0, 0)
                : new RectOffset(0, reserve, 0, 0);
            AxylUIDirection.Apply(row, m_options.RightToLeft);
            go.AddComponent<LayoutElement>().preferredHeight = AxylUITheme.HeaderHeight;

            var icon = NewImage("SettingsIcon", (RectTransform)go.transform,
                AccountIcon("settings"), AxylUITheme.TextPrimary);
            var iconElement = icon.gameObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = AxylUITheme.HeaderIconSize;
            iconElement.preferredHeight = AxylUITheme.HeaderIconSize;

            var title = NewText("Title", (RectTransform)go.transform,
                AxylUITheme.TitleFontSize, AxylUITheme.TextPrimary);
            title.fontWeight = FontWeight.SemiBold;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            AxylUIDirection.Apply(title, m_options.RightToLeft); // re-mirror after the vertical change
            title.text = m_options.Title;
        }

        private void BuildAccountInfo(RectTransform popup, bool wide)
        {
            var go = new GameObject("AccountInfo", typeof(RectTransform));
            go.transform.SetParent(popup, false);

            HorizontalOrVerticalLayoutGroup group = wide
                ? go.AddComponent<HorizontalLayoutGroup>()
                : go.AddComponent<VerticalLayoutGroup>();
            group.spacing = AxylUITheme.GridColumnGap;
            group.childAlignment = TextAnchor.UpperLeft;
            AxylUIDirection.Apply(group as HorizontalLayoutGroup, m_options.RightToLeft);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            BuildInfoField(go, m_options.NicknameLabel, m_options.Nickname, flexible: 1f, copy: false);
            BuildInfoField(go, m_options.ServerLabel, m_options.Server, flexible: 1f, copy: false);
            // The CS Code field gets the widest share when space runs short.
            BuildInfoField(go, m_options.CsCodeLabel,
                m_options.CsCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                flexible: 2f, copy: true);
        }

        // One read-only field: caption over a soft value card; the CS Code card carries the
        // copy button on its right edge.
        private void BuildInfoField(GameObject parent, string caption, string value,
            float flexible, bool copy)
        {
            var go = new GameObject($"Info_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            go.AddComponent<LayoutElement>().flexibleWidth = flexible;

            var captionText = NewText("Caption", (RectTransform)go.transform,
                AxylUITheme.InfoCaptionFontSize, AxylUITheme.TextBody); // AA contrast on Surface
            captionText.text = caption;

            var cardGo = new GameObject("Value", typeof(RectTransform));
            cardGo.transform.SetParent(go.transform, false);
            var card = cardGo.AddComponent<Image>();
            card.sprite = AxylUIRuntimeAssets.RoundedRect(AxylUITheme.ChipRadius);
            card.type = Image.Type.Sliced;
            card.color = AxylUITheme.SurfaceSoft;
            var cardRow = cardGo.AddComponent<HorizontalLayoutGroup>();
            cardRow.padding = new RectOffset(12, 8, 8, 8);
            cardRow.spacing = 8f;
            cardRow.childAlignment = TextAnchor.MiddleLeft;
            AxylUIDirection.Apply(cardRow, m_options.RightToLeft);
            cardRow.childControlWidth = true;
            cardRow.childControlHeight = true;
            cardRow.childForceExpandWidth = false;
            cardRow.childForceExpandHeight = false;

            var valueText = NewText("Text", (RectTransform)cardGo.transform,
                AxylUITheme.InfoValueFontSize, AxylUITheme.TextPrimary);
            valueText.fontWeight = FontWeight.SemiBold;
            // Player-set values (the nickname) must not carry markup — "<size=400>" as a
            // nickname would render as formatting and break the card's layout.
            valueText.richText = false;
            valueText.text = value;
            valueText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            if (copy)
            {
                // The Kit formats this value itself (the CS Code digits): keep its glyph
                // order in an RTL UI so the shown number matches what the copy button puts
                // on the clipboard. The right alignment from NewText stays.
                valueText.isRightToLeftText = false;
                BuildCopyButton((RectTransform)cardGo.transform);
            }
        }

        // Copies the CS Code to the system clipboard, then hands the app the value so it can
        // show its own "copied" toast. An icon button: a 20px glyph inside a 48×48 hit area
        // that overhangs the value card (the layout footprint stays the glyph's).
        private void BuildCopyButton(RectTransform parent)
        {
            var go = new GameObject("CopyButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<LayoutElement>();
            Square(layout, AxylUITheme.ButtonIconSize);

            var hitGo = new GameObject("Hit", typeof(RectTransform));
            var hitRect = (RectTransform)hitGo.transform;
            hitRect.SetParent(go.transform, false);
            hitRect.sizeDelta = new Vector2(AxylUITheme.CloseHitSize, AxylUITheme.CloseHitSize);
            var hit = hitGo.AddComponent<Image>();
            hit.color = Color.clear;

            var icon = NewImage("Icon", hitRect, AccountIcon("copy"), AxylUITheme.TextMuted);
            ((RectTransform)icon.transform).sizeDelta =
                new Vector2(AxylUITheme.ButtonIconSize, AxylUITheme.ButtonIconSize);

            var button = hitGo.AddComponent<Button>();
            AxylUICursor.Attach(button);
            AxylUIFocusRing.Attach(button, AxylUITheme.CloseHitSize / 2f);
            m_tabOrder.Add(button);
            m_accessible.Add(new AxylUIAccessibleControl(button, m_options.CopyLabel));
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = icon;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = AxylUITheme.IconButtonHoverTint;
            colors.pressedColor = AxylUITheme.IconButtonPressedTint;
            colors.selectedColor = Color.white; // Focus = Default
            colors.fadeDuration = AxylUITheme.StateTransitionSeconds;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer =
                    m_options.CsCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
                m_options.OnCsCodeCopied?.Invoke(m_options.CsCode);
            });

            static void Square(LayoutElement element, float size)
            {
                element.preferredWidth = size;
                element.preferredHeight = size;
                element.minWidth = size;
                element.minHeight = size;
            }
        }

        private void BuildLinkSection(
            RectTransform popup, float contentWidth, AxylUIScreenLayout layout)
        {
            var go = new GameObject("AccountLink", typeof(RectTransform));
            go.transform.SetParent(popup, false);
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = 8f;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var title = NewText("SectionTitle", (RectTransform)go.transform,
                AxylUITheme.SectionTitleFontSize, AxylUITheme.TextBody);
            title.fontWeight = FontWeight.SemiBold;
            title.text = m_options.LinkSectionTitle;

            var gridGo = new GameObject("ProviderGrid", typeof(RectTransform));
            gridGo.transform.SetParent(go.transform, false);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            // Portrait gives each provider a row of its own (seven rows of one); the wide
            // variants pair them (four rows of two, the last row's second cell empty). The
            // gaps hold at 8px everywhere — a screen that runs short scrolls its content
            // rather than tightening the grid.
            int columns = layout == AxylUIScreenLayout.Portrait ? 1 : 2;
            float cellWidth = columns == 1
                ? contentWidth
                : (contentWidth - AxylUITheme.GridColumnGap) / 2f;
            grid.cellSize = new Vector2(cellWidth, AxylUITheme.AccountRowHeight);
            grid.spacing = new Vector2(AxylUITheme.GridColumnGap, AxylUITheme.GridRowGap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            AxylUIDirection.Apply(grid, m_options.RightToLeft);

            foreach (var provider in m_options.Providers)
            {
                var id = provider.Id;
                var row = InstantiateWidget<AccountLinkRow>(
                    "AccountLinkRow", (RectTransform)gridGo.transform);
                row.Configure(
                    provider.Label,
                    provider.Icon != null ? provider.Icon : ProviderIcons.TryGet(id),
                    provider.ChipText, provider.Connected, provider.Interactable,
                    () => m_options.OnProviderSelected?.Invoke(id), m_options.RightToLeft);
                m_rows.Add(row);
                var rowButton = row.GetComponent<Button>();
                if (provider.Interactable)
                {
                    // A disabled row stays disabled for the screen's whole life (the app
                    // re-opens with new options to change it), so it stays out of the
                    // navigation order — uGUI's Explicit navigation would land on it.
                    m_tabOrder.Add(rowButton);
                }

                m_accessible.Add(new AxylUIAccessibleControl(rowButton, provider.Label));
            }

            // Every row takes the height the tallest label needs (a wrapped "Play Games" on
            // a narrow Portrait screen); 44px when every label fits on one line.
            float rowHeight = AxylUITheme.AccountRowHeight;
            foreach (var row in m_rows)
            {
                rowHeight = Mathf.Max(rowHeight, row.PreferredHeight(cellWidth));
            }

            grid.cellSize = new Vector2(cellWidth, rowHeight);
            foreach (var row in m_rows)
            {
                row.SetHeight(rowHeight);
            }
        }

        private void BuildActions(RectTransform popup, float contentWidth)
        {
            var go = new GameObject("Actions", typeof(RectTransform));
            go.transform.SetParent(popup, false);

            var group = go.AddComponent<HorizontalLayoutGroup>();
            group.spacing = AxylUITheme.GridColumnGap;
            group.childAlignment = TextAnchor.UpperCenter;
            AxylUIDirection.Apply(group, m_options.RightToLeft);
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            float width = (contentWidth - AxylUITheme.GridColumnGap) / 2f;

            // Delete account leads (left), log out follows — equal sizes. Logging out asks
            // first; OnLogout is raised only from the confirmation's confirm action.
            var delete = AddAction(go, m_options.DeleteAccountLabel, AxylUIActionStyle.Destructive,
                width, () => m_options.OnDeleteAccount?.Invoke());
            var logout = AddAction(go, m_options.LogoutLabel, AxylUIActionStyle.Primary,
                width, ShowLogoutConfirm);
            m_tabOrder.Add(delete);
            m_tabOrder.Add(logout);
            m_accessible.Add(new AxylUIAccessibleControl(delete, m_options.DeleteAccountLabel));
            m_accessible.Add(new AxylUIAccessibleControl(logout, m_options.LogoutLabel));
        }

        private void ShowLogoutConfirm()
        {
            ShowConfirm(m_options.LogoutConfirmMessage, m_options.LogoutCancelLabel,
                m_options.LogoutConfirmLabel, () => m_options.OnLogout?.Invoke());
        }

        /// <summary>
        /// Shows a confirmation card over the popup: its own dim, a small Common Popup card
        /// with the message and a cancel / confirm pair. Cancel (and Esc) returns to
        /// the account popup; confirm runs <paramref name="onConfirm"/>. The logout action
        /// uses it, and the app can call it for confirmations of its own (delete account) so
        /// the flow keeps the Kit's look. <paramref name="destructive"/> styles the confirm
        /// action as Destructive instead of Primary.
        /// </summary>
        public void ShowConfirm(
            string message, string cancelLabel, string confirmLabel, Action onConfirm,
            bool destructive = false)
        {
            if (m_confirm != null)
            {
                CloseConfirm(); // one card at a time
            }

            var root = (RectTransform)transform;
            var dimGo = new GameObject("Confirm", typeof(RectTransform));
            m_confirm = dimGo;
            var dim = (RectTransform)dimGo.transform;
            dim.SetParent(root, false);
            if (m_toastLayer != null)
            {
                dim.SetSiblingIndex(m_toastLayer.GetSiblingIndex()); // the toast stays on top
            }

            Stretch(dim, 0f);
            // The dim (a raycast target) blocks input to the popup behind it and does nothing
            // itself — the confirmation card closes only through its two actions (and Esc,
            // as cancel); a tap beside the card is neither.
            dimGo.AddComponent<Image>().color = AxylUITheme.OverlayDim;

            // The card keeps the popup margins on a narrow screen instead of its full width.
            float cardWidth = Mathf.Min(
                AxylUITheme.LogoutConfirmWidth,
                AxylUIScreen.SafeSize(m_options.ForcedLayout ?? AxylUIScreen.DetectLayout()).x
                    - AxylUITheme.SideMarginMobile * 2f);
            var cardGo = new GameObject("Card", typeof(RectTransform));
            var card = (RectTransform)cardGo.transform;
            card.SetParent(dim, false);
            card.sizeDelta = new Vector2(cardWidth, 0f);
            var border = cardGo.AddComponent<Image>();
            border.sprite = AxylUIRuntimeAssets.RoundedRect(AxylUITheme.CommonPopupRadius);
            border.type = Image.Type.Sliced;
            border.color = AxylUITheme.Border;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            var fillRect = (RectTransform)fillGo.transform;
            fillRect.SetParent(card, false);
            float inset = AxylUITheme.CommonPopupBorderWidth;
            Stretch(fillRect, inset);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = AxylUIRuntimeAssets.RoundedRect(AxylUITheme.CommonPopupRadius - inset);
            fill.type = Image.Type.Sliced;
            fill.color = AxylUITheme.Surface;
            fill.raycastTarget = false;
            fillGo.AddComponent<LayoutElement>().ignoreLayout = true;

            float pad = AxylUITheme.PopupPaddingPc;
            var column = cardGo.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            column.spacing = AxylUITheme.PopupSectionGapPc;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            cardGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var messageText = NewText("Message", card,
                AxylUITheme.InfoValueFontSize, AxylUITheme.TextPrimary);
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Overflow;
            messageText.text = message;

            var actionsGo = new GameObject("Actions", typeof(RectTransform));
            actionsGo.transform.SetParent(card, false);
            var actions = actionsGo.AddComponent<HorizontalLayoutGroup>();
            actions.spacing = AxylUITheme.GridColumnGap;
            actions.childAlignment = TextAnchor.UpperCenter;
            AxylUIDirection.Apply(actions, m_options.RightToLeft);
            actions.childControlWidth = false;
            actions.childControlHeight = false;
            actions.childForceExpandWidth = false;
            actions.childForceExpandHeight = false;

            float width = (cardWidth - pad * 2f - AxylUITheme.GridColumnGap) / 2f;
            var cancel = AddAction(actionsGo, cancelLabel, AxylUIActionStyle.Secondary,
                width, CloseConfirm);
            var confirm = AddAction(actionsGo, confirmLabel,
                destructive ? AxylUIActionStyle.Destructive : AxylUIActionStyle.Primary,
                width, () =>
                {
                    CloseConfirm();
                    onConfirm?.Invoke();
                });

            // While the card is up, focus and the screen reader see only its two actions;
            // cancel lands the focus first.
            m_currentOrder = new List<Selectable> { cancel, confirm };
            m_focusTrap?.SetOrder(m_currentOrder);
            AxylUIAccessibility.Install(gameObject, new List<AxylUIAccessibleControl>
            {
                new AxylUIAccessibleControl(cancel, cancelLabel),
                new AxylUIAccessibleControl(confirm, confirmLabel),
            });
            EventSystem.current?.SetSelectedGameObject(cancel.gameObject);
        }

        // The one way the confirmation card goes away — cancel, confirm, Esc —
        // handing focus and the screen reader back to the popup's own controls.
        private void CloseConfirm()
        {
            if (m_confirm == null)
            {
                return;
            }

            Destroy(m_confirm);
            m_confirm = null;
            m_currentOrder = m_tabOrder;
            m_focusTrap?.SetOrder(m_tabOrder);
            AxylUIAccessibility.Install(gameObject, m_accessible);
        }

        private Button AddAction(
            GameObject parent, string label, AxylUIActionStyle style, float width, Action onClick)
        {
            var action = InstantiateWidget<AxylUIActionButton>(
                "AxylUIActionButton", (RectTransform)parent.transform);
            ((RectTransform)action.transform).sizeDelta =
                new Vector2(width, AxylUITheme.ActionButtonHeight);
            action.Configure(label, style, onClick, m_options.RightToLeft);
            return action.GetComponent<Button>();
        }

        // ---- Helpers ---------------------------------------------------------------------

        private static Sprite AccountIcon(string name)
        {
            return Resources.Load<Sprite>("UIKit/AccountIcons/" + name);
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

        private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        // Every text starts left-aligned and follows the reading direction; a caller that
        // centers its text afterwards keeps the direction flag.
        private TextMeshProUGUI NewText(
            string name, RectTransform parent, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = AxylUIRuntimeAssets.Font();
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            AxylUIDirection.Apply(tmp, m_options.RightToLeft);
            return tmp;
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
