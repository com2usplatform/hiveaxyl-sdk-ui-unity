// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>Everything the app passes in. The Kit is pure UI: it renders the form, enforces
    /// the given input rules, and raises events — the sign-in call, the error-code-to-copy
    /// mapping, and the create-account destination stay with the app.</summary>
    public sealed class UsernameLoginOptions
    {
        /// <summary>Called with the current username and password when the login action is
        /// selected. The screen only raises valid submissions; the app runs the sign-in and
        /// answers with <see cref="UsernameLoginScreen.SetBusy"/> /
        /// <see cref="UsernameLoginScreen.ShowFormError"/>.</summary>
        public Action<string, string> OnLoginRequested { get; set; }

        /// <summary>Called when the create-account link is selected. The Kit ships no sign-up
        /// screen — open the app's URL or flow here.</summary>
        public Action OnCreateAccount { get; set; }

        /// <summary>Called when the popup asks to close: the close button or Esc (the dim
        /// backdrop blocks input but does not close, so a mis-tap can't discard a form
        /// mid-edit). Navigation is the app's. Unused when <see cref="Popup"/> is false.</summary>
        public Action OnClose { get; set; }

        /// <summary>True (default) presents the form as a popup over the dimmed game, with a
        /// close button; false renders the centered panel alone for a full-screen flow.</summary>
        public bool Popup { get; set; } = true;

        // Display strings — the app's localized strings for the keys in AxylUIStringKeys.
        // The Kit holds no copy of its own: an unset string shows its key.
        public string Title { get; set; } = AxylUIStringKeys.UsernameLoginTitle;

        public string UsernameLabel { get; set; } = AxylUIStringKeys.UsernameLabel;

        public string UsernamePlaceholder { get; set; } = AxylUIStringKeys.UsernamePlaceholder;

        public string PasswordLabel { get; set; } = AxylUIStringKeys.PasswordLabel;

        public string PasswordPlaceholder { get; set; } = AxylUIStringKeys.PasswordPlaceholder;

        public string LoginButtonLabel { get; set; } = AxylUIStringKeys.LoginButton;

        /// <summary>The sentence shown before the create-account link, on the same row under
        /// the title ("Don't have an account?"). Hidden along with the link.</summary>
        public string CreateAccountPrompt { get; set; } = AxylUIStringKeys.CreateAccountPrompt;

        /// <summary>The create-account link text; null or empty hides the link (and its
        /// prompt) — the form then serves as a dedicated flow of its own (say, an app-run
        /// sign-up).</summary>
        public string CreateAccountLabel { get; set; } = AxylUIStringKeys.CreateAccount;

        /// <summary>Shown under the username input when it is left empty or breaks its rules.</summary>
        public string UsernameErrorText { get; set; } = AxylUIStringKeys.UsernameError;

        /// <summary>Shown under the password input when it is left empty or breaks its rules.</summary>
        public string PasswordErrorText { get; set; } = AxylUIStringKeys.PasswordError;

        // Accessibility names — read by the screen reader, never drawn.
        public string CloseLabel { get; set; } = AxylUIStringKeys.Close;

        public string PasswordShowLabel { get; set; } = AxylUIStringKeys.PasswordShow;

        public string PasswordHideLabel { get; set; } = AxylUIStringKeys.PasswordHide;

        // Input rules — reference defaults; the app applies its own auth policy on top with
        // the validators below. A max of 0 means no limit. Lengths count UTF-16 units, the
        // unit the input field's own character limit uses, so a character outside the basic
        // plane (an emoji, some CJK extensions) counts as two.
        public int UsernameMinLength { get; set; } = 4;

        public int UsernameMaxLength { get; set; } = 20;

        public int PasswordMinLength { get; set; } = 8;

        public int PasswordMaxLength { get; set; } = 20;

        /// <summary>Optional extra username rule: return an error message to show under the
        /// field, or null when the value passes. Runs after the length rules.</summary>
        public Func<string, string> UsernameValidator { get; set; }

        /// <summary>Optional extra password rule; same contract as the username validator.</summary>
        public Func<string, string> PasswordValidator { get; set; }

        /// <summary>True for a right-to-left language (Arabic, Hebrew, Persian — see
        /// <see cref="AxylUIDirection.IsRightToLeft"/>): text aligns right and every
        /// horizontal row runs right-to-left; glyphs are never mirrored.</summary>
        public bool RightToLeft { get; set; }

        /// <summary>Set to pin a layout; leave null to pick one from the display.</summary>
        public AxylUIScreenLayout? ForcedLayout { get; set; }
    }

    /// <summary>
    /// The username login form, in the Common Popup frame: the title, the create-account
    /// prompt and link on one row, the username and password inputs (password masked, with a
    /// show/hide toggle), the form error, and the login action in the popup's fixed Actions
    /// area. Presented as a popup over the dimmed game or as a full-screen panel; the game's
    /// own background and decoration stay with the app.
    /// </summary>
    /// <remarks>
    /// <para><b>Behavior.</b> The login action enables only while both fields satisfy the
    /// given rules. An invalid non-empty field shows its error under the input as it is
    /// typed; an empty field shows its error once editing leaves it or a submit is attempted
    /// (Enter). An auth failure is the app's to report back via <see cref="ShowFormError"/>
    /// (shown inside the form under the password input, no input turns red); a network/server
    /// failure via <see cref="ShowToast"/>. Any edit clears the form error. The typed username
    /// survives a failure; whether the password does is the app's policy.</para>
    /// <para><b>Height.</b> The popup never outgrows the safe area: when the form is taller,
    /// the form alone scrolls while the close button and the login action stay fixed.</para>
    /// <para><b>How to customize.</b> Colors, sizes and spacing all live in
    /// <see cref="AxylUITheme"/>. The widget look lives in the prefabs under
    /// Resources/UIKit/Prefabs (rebake with the AxylUIBaker after a structural change). Copy
    /// comes in through <see cref="UsernameLoginOptions"/>. The screen itself is built in the
    /// numbered steps of <see cref="Build"/>; each step is one visual region.</para>
    /// </remarks>
    public sealed class UsernameLoginScreen : MonoBehaviour
    {
        private readonly List<Selectable> m_tabOrder = new List<Selectable>();
        private UsernameLoginOptions m_options;
        private AxylUIInputGroup m_username;
        private AxylUIInputGroup m_password;
        private AxylUIActionButton m_loginButton;
        private Button m_createAccountButton;
        private TMP_Text m_formError;
        private FailureToast m_toast;
        private GameObject m_previousSelection;
        private bool m_busy;
        private readonly AxylUIScreenSizeWatch m_sizeWatch = new AxylUIScreenSizeWatch();
        private bool m_valid;
        private (string, string) m_announcedErrors; // the errors the screen reader last got
        // An empty field shows its error only after the player has left it (or tried to
        // submit) — a form that opens all red says nothing useful.
        private bool m_usernameTouched;
        private bool m_passwordTouched;
        private bool m_appEnabled = true;

        /// <summary>Creates the screen on its own overlay canvas and shows it.</summary>
        public static UsernameLoginScreen Show(UsernameLoginOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var layout = options.ForcedLayout ?? AxylUIScreen.DetectLayout();
            var screen = AxylUIScreen.CreateCanvas("AxylUIUsernameLoginScreen", layout)
                .AddComponent<UsernameLoginScreen>();
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

        /// <summary>Shows the auth-failure message inside the form (under the password input).
        /// No input border turns red — the failure belongs to the pair, not to one field.</summary>
        public void ShowFormError(string message)
        {
            string shown = string.IsNullOrEmpty(message) ? null : message;
            string before = m_formError.gameObject.activeSelf ? m_formError.text : null;
            m_formError.text = shown ?? string.Empty;
            m_formError.gameObject.SetActive(shown != null);
            if (shown != before)
            {
                // The failure reaches the screen reader as the password field's description,
                // the way the password-change form reports its auth failure.
                PublishAccessibility();
            }
        }

        /// <summary>Shows the shared toast — network/server errors by default, or a plain
        /// status note with <see cref="AxylUIToastVariant.Default"/> (say, where create-account
        /// navigation would go). Message wording is the app's.</summary>
        public void ShowToast(string message, AxylUIToastVariant variant = AxylUIToastVariant.Error)
        {
            m_toast.Show(message, variant);
        }

        /// <summary>True while the app's sign-in call is in flight: the login action disables
        /// and the fields lock, so a second submission cannot start. A submit sets this
        /// itself; the app clears it when its call returns.</summary>
        public void SetBusy(bool busy)
        {
            m_busy = busy;
            m_username.SetInteractable(!busy);
            m_password.SetInteractable(!busy);
            Revalidate();
        }

        /// <summary>The app's own say on whether login may go ahead (a server-side lockout,
        /// a policy the Kit cannot see); false disables the login action on top of the
        /// Kit's field rules. Defaults to true.</summary>
        public void SetSubmitEnabled(bool enabled)
        {
            m_appEnabled = enabled;
            Revalidate();
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

        // Esc closes the popup like the close button does; a full-screen form has no close.
        // Tab / Shift+Tab walk the form's focus order, and Enter fires the primary action.
        private void Update()
        {
            if (m_options.Popup && AxylUIInput.EscapePressed())
            {
                m_options.OnClose?.Invoke();
            }

            if (AxylUIInput.TabPressed(out bool backward))
            {
                AxylUIInput.MoveFocus(m_tabOrder, backward);
            }

            if (AxylUIInput.SubmitPressed())
            {
                SubmitLogin(); // revalidates, and a busy or invalid form ignores it
            }

            if (m_sizeWatch.Changed())
            {
                RebuildForScreenChange();
            }
        }

        // A rotation or a window resize invalidates the layout choice, the scaler
        // reference, and every width computed at build time, so the screen rebuilds in
        // place — the component and the app's callbacks survive, and typed
        // input is carried over.
        private void RebuildForScreenChange()
        {
            string username = m_username != null ? m_username.Text : null;
            string password = m_password != null ? m_password.Text : null;
            string formError = m_formError != null && m_formError.gameObject.activeSelf
                ? m_formError.text
                : null;
            int selectedIndex = SelectedTabIndex();
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            bool wasEditing = selected != null
                && selected.TryGetComponent(out TMP_InputField focusedField)
                && focusedField.isFocused;
            bool busy = m_busy;
            float? scrollPosition = AxylUIScreen.ScrollPosition(gameObject);
            var toast = m_toast != null ? m_toast.Capture() : null;

            var layout = m_options.ForcedLayout ?? AxylUIScreen.DetectLayout();
            AxylUIScreen.ApplyScaler(gameObject, layout);
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            m_tabOrder.Clear();
            Build(layout);
            m_username.SetText(username);
            m_password.SetText(password);
            ShowFormError(formError); // SetText counts as an edit, which cleared it
            SetBusy(busy);
            if (selectedIndex >= 0 && selectedIndex < m_tabOrder.Count)
            {
                var target = m_tabOrder[selectedIndex].gameObject;
                if (!wasEditing)
                {
                    // A control that was only selected — not editing — comes back the
                    // same way: a plain select would activate a field and raise the soft
                    // keyboard the player had down.
                    AxylUIFocusTrap.SelectWithoutActivating(EventSystem.current, target);
                }
                else
                {
                    if (m_tabOrder[selectedIndex] is TMP_InputField restored)
                    {
                        // TMP activates the restored field in this frame's LateUpdate:
                        // the select-all flag is parked so the next keystroke cannot
                        // replace the typed value, and the caret is placed before the
                        // activation so the soft keyboard opens with it at the end (a
                        // move after activation does not reach the keyboard's own
                        // selection).
                        bool selectAll = restored.onFocusSelectAll;
                        restored.onFocusSelectAll = false;
                        restored.stringPosition = restored.text.Length;
                        StartCoroutine(RestoreSelectAll(restored, selectAll));
                    }

                    EventSystem.current?.SetSelectedGameObject(target);
                }
            }

            if (toast != null)
            {
                m_toast.Restore(toast.Value); // a message mid-read survives the rotation
            }

            AxylUIScreen.RestoreScrollPosition(gameObject, scrollPosition);
        }

        // The activation consumed the parked flag this frame; the frame after, the field
        // selects-all on focus again, so only the restore skipped it.
        private static System.Collections.IEnumerator RestoreSelectAll(
            TMP_InputField field, bool selectAll)
        {
            yield return null;
            if (field != null)
            {
                field.onFocusSelectAll = selectAll;
            }
        }

        private void PublishAccessibility()
        {
            var controls = new List<AxylUIAccessibleControl>();
            foreach (var control in m_tabOrder)
            {
                if (control == null)
                {
                    continue;
                }

                string label;
                string description = null; // an input's current error, read after its name
                bool textField = false;
                if (control == m_username.Field)
                {
                    label = m_options.UsernameLabel;
                    description = m_username.Error;
                    textField = true;
                }
                else if (control == m_password.Field)
                {
                    label = m_options.PasswordLabel;
                    // The form error (an auth failure) belongs to the pair; the password
                    // field carries it for the screen reader, after its own error.
                    description = m_password.Error
                        ?? (m_formError.gameObject.activeSelf ? m_formError.text : null);
                    textField = true;
                }
                else if (control == m_password.Toggle)
                {
                    label = m_password.Revealed ? m_options.PasswordHideLabel : m_options.PasswordShowLabel;
                }
                else if (control == m_loginButton.GetComponent<Button>())
                {
                    label = m_options.LoginButtonLabel;
                }
                else if (control == m_createAccountButton)
                {
                    label = m_options.CreateAccountLabel;
                }
                else if (control.GetComponent<CloseButton>() != null)
                {
                    label = m_options.CloseLabel;
                }
                else
                {
                    continue; // a control this map does not know gets no wrong name
                }

                controls.Add(new AxylUIAccessibleControl(control, label, textField, description));
            }

            AxylUIAccessibility.Install(gameObject, controls);
        }

        // Which control of the keyboard path holds the focus (-1: none of them) — the same
        // control is selected again after a rebuild. The index covers every tab stop, not
        // the password field alone: a rotation while the username was being typed used to
        // drop the focus to the close button, taking the soft keyboard down with it.
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

        // The form sits directly in the Common Popup's content, with no panel surface of its
        // own, and its content padding (32 / 16 / 24 by layout) replaces the popup's default
        // padding there — one padding, never both.
        private static float ContentPadding(AxylUIScreenLayout layout) => layout switch
        {
            AxylUIScreenLayout.Pc => AxylUITheme.FormPanelPaddingPc,
            AxylUIScreenLayout.Portrait => AxylUITheme.FormPanelPaddingPortrait,
            _ => AxylUITheme.FormPanelPaddingLandscape,
        };

        private static float GroupGap(AxylUIScreenLayout layout) =>
            layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.InputGroupGapPc
                : AxylUITheme.InputGroupGapMobile;

        private void Build(AxylUIScreenLayout layout)
        {
            var canvas = (RectTransform)transform;
            var size = AxylUIScreen.SafeSize(layout);

            // Step 1 — backdrop (popup only): dims the app behind and blocks its input; it
            // does not close the form (only the close button and Esc do). It stays on the
            // canvas itself (edge to edge); everything else sits on the safe-area root,
            // created after the backdrop so it draws above the dim (uGUI renders siblings
            // in order).
            if (m_options.Popup)
            {
                BuildBackdrop(canvas);
            }

            var root = AxylUIScreen.ContentRoot(gameObject);

            // Step 2 — the form panel, centered; height follows its content. The app's own
            // background art shows around it in both modes.
            float sideMargin = layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.SideMarginPc
                : AxylUITheme.SideMarginMobile;
            float pad = ContentPadding(layout);
            float panelWidth = layout == AxylUIScreenLayout.Portrait
                ? size.x - sideMargin * 2f
                : Mathf.Min(
                    AxylUITheme.FormWidth + pad * 2f,
                    Mathf.Min(AxylUITheme.FormPanelMaxWidth, size.x - sideMargin * 2f));
            float gap = GroupGap(layout);
            var panel = BuildPanel(root, panelWidth, pad, gap);

            // Step 3 — the popup's close control, top-right of the panel, out of the flow.
            // It is the first stop of the keyboard path: close → create-account link →
            // username → password → show/hide → login.
            if (m_options.Popup)
            {
                var close = InstantiateWidget<CloseButton>("CloseButton", panel);
                close.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                close.Configure(
                    () => m_options.OnClose?.Invoke(), AxylUITheme.TextPrimary, m_options.RightToLeft);
                m_tabOrder.Add(close.GetComponent<Button>());
            }

            // Step 4 — title: the popup's Header, centered, fixed above the form (it never
            // scrolls with it). Kept clear of the close button's corner on both sides, so a
            // long title wraps beside the X instead of behind it (and stays centered).
            var title = NewText("Title", panel, AxylUITheme.TitleFontSize, AxylUITheme.TextPrimary);
            title.fontWeight = FontWeight.SemiBold;
            title.alignment = TextAlignmentOptions.Center;
            float titleInset = m_options.Popup ? AxylUITheme.CloseHitSize : 0f;
            title.margin = new Vector4(titleInset, 0f, titleInset, 0f);
            title.text = m_options.Title;

            // Step 5 — the form column: everything that scrolls when the popup is taller
            // than the screen. The login action sits outside it, in the fixed Actions area.
            var form = BuildColumn("Form", panel, gap);

            // Step 6 — "Don't have an account? Create a new account": the prompt and the link
            // on one row under the title. An empty link label leaves the row out (the app is
            // running this form as its own flow).
            m_createAccountButton = null;
            if (!string.IsNullOrEmpty(m_options.CreateAccountLabel))
            {
                m_createAccountButton = BuildCreateAccountRow(form, layout, panelWidth - pad * 2f);
                m_tabOrder.Add(m_createAccountButton);
            }

            // Step 7 — the two inputs. Validation runs on every edit; an auth failure shown
            // via ShowFormError clears on the next edit.
            m_username = InstantiateWidget<AxylUIInputGroup>("AxylUIInputGroup", form);
            m_username.Configure(
                m_options.UsernameLabel, m_options.UsernamePlaceholder,
                Icon("user"), secure: false, m_options.UsernameMaxLength, _ => OnEdited(),
                m_options.RightToLeft);
            m_username.EndEdited += () => { m_usernameTouched = true; Revalidate(); };
            m_password = InstantiateWidget<AxylUIInputGroup>("AxylUIInputGroup", form);
            m_password.Configure(
                m_options.PasswordLabel, m_options.PasswordPlaceholder,
                Icon("lock"), secure: true, m_options.PasswordMaxLength, _ => OnEdited(),
                m_options.RightToLeft);
            m_password.EndEdited += () => { m_passwordTouched = true; Revalidate(); };
            m_tabOrder.Add(m_username.Field);
            m_tabOrder.Add(m_password.Field);
            m_tabOrder.Add(m_password.Toggle);

            // Step 8 — the form-level auth error, under the password input, hidden until the
            // app reports a failure.
            m_formError = NewText(
                "FormError", form, AxylUITheme.InputErrorFontSize, AxylUITheme.SemanticDown);
            m_formError.gameObject.SetActive(false);

            // Step 9 — the login action in the popup's Actions area (fixed; it never scrolls
            // with the form), enabled only while both fields pass their rules.
            m_loginButton = InstantiateWidget<AxylUIActionButton>("AxylUIActionButton", panel);
            var buttonRect = (RectTransform)m_loginButton.transform;
            buttonRect.sizeDelta = new Vector2(
                panelWidth - pad * 2f, AxylUITheme.ActionButtonHeight);
            var buttonElement = m_loginButton.gameObject.AddComponent<LayoutElement>();
            buttonElement.preferredHeight = AxylUITheme.ActionButtonHeight;
            m_loginButton.Configure(
                m_options.LoginButtonLabel, AxylUIActionStyle.Primary, SubmitLogin,
                m_options.RightToLeft);
            m_tabOrder.Add(m_loginButton.GetComponent<Button>());

            // Step 10 — a form taller than the screen scrolls between the fixed close
            // button and the fixed login action; a popup that fits keeps its plain layout.
            LimitToScreenIfNeeded(panel, form, layout);

            // Focus stays inside the popup (a modal keeps focus until it closes), and
            // the screen reader gets every control by name — the visibility toggle's
            // name follows its state.
            AxylUIFocusTrap.Install(gameObject, m_tabOrder);
            m_password.RevealChanged += PublishAccessibility;
            PublishAccessibility();

            // Step 11 — the shared toast (network/server errors) on its own layer above
            // everything else, so it never moves the form.
            m_toast = InstantiateWidget<FailureToast>("FailureToast", AxylUIScreen.ToastLayer(gameObject));
            m_toast.Configure(
                topEdge: layout == AxylUIScreenLayout.Landscape,
                wide: layout == AxylUIScreenLayout.Pc,
                availableWidth: size.x,
                rightToLeft: m_options.RightToLeft);

            // No field is focused on open: the soft keyboard belongs to the player's first
            // tap, not to the screen appearing. The focus trap starts the keyboard path at
            // the close button — with no popup frame, at the first item instead: the
            // create-account link, or the username field when the link is hidden — and a
            // rebuild (a rotation) restores whichever control the player had. Show
            // remembered the app's own selection for Dismiss to restore.
            Revalidate();
        }

        private void BuildBackdrop(RectTransform root)
        {
            var go = new GameObject("Backdrop", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            Stretch(rect);

            // The dim blocks raycasts to the app behind (a raycast-target Image absorbs
            // them); it does not close on tap — only the close button and Esc close,
            // so a mis-tap outside can't discard a form mid-edit.
            go.AddComponent<Image>().color = AxylUITheme.OverlayDim;
        }

        private RectTransform BuildPanel(
            RectTransform root, float width, float pad, float gap)
        {
            var go = new GameObject("FormPanel", typeof(RectTransform));
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
            Stretch(fillRect);
            float inset = AxylUITheme.CommonPopupBorderWidth;
            fillRect.offsetMin = new Vector2(inset, inset);
            fillRect.offsetMax = new Vector2(-inset, -inset);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = AxylUIRuntimeAssets.RoundedRect(AxylUITheme.CommonPopupRadius - inset);
            fill.type = Image.Type.Sliced;
            fill.color = AxylUITheme.Surface;
            fill.raycastTarget = false;
            fillGo.AddComponent<LayoutElement>().ignoreLayout = true;

            var column = go.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            column.spacing = gap;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        // A plain vertical column: children stack top-down at the panel's content width.
        private static RectTransform BuildColumn(string name, RectTransform parent, float gap)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.spacing = gap;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return rect;
        }

        // The prompt ("Don't have an account?") and the underlined Primary link, centered on
        // one row while both fit in <paramref name="width"/>; a long translation stacks
        // them, prompt over link, instead of clipping or scrolling sideways. The link's hit
        // area meets the touch minimum: 44px tall on PC, 48×48 on mobile.
        private Button BuildCreateAccountRow(
            RectTransform form, AxylUIScreenLayout layout, float width)
        {
            float hit = layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.LinkHitHeight
                : AxylUITheme.LinkHitSizeMobile;

            var rowGo = new GameObject("CreateAccountRow", typeof(RectTransform));
            rowGo.transform.SetParent(form, false);

            var prompt = NewText("Prompt", (RectTransform)rowGo.transform,
                AxylUITheme.LinkFontSize, AxylUITheme.TextBody);
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.text = m_options.CreateAccountPrompt;

            // Measured before the group is chosen: one row when prompt, gap and link fit the
            // form width, a vertical stack otherwise.
            float gap = AxylUITheme.InputErrorGap;
            float promptWidth = prompt.GetPreferredValues(prompt.text).x;
            float linkWidth = prompt.GetPreferredValues(m_options.CreateAccountLabel).x;
            bool stack = promptWidth + gap + linkWidth > width;
            HorizontalOrVerticalLayoutGroup row = stack
                ? rowGo.AddComponent<VerticalLayoutGroup>()
                : rowGo.AddComponent<HorizontalLayoutGroup>();
            row.spacing = gap;
            row.childAlignment = TextAnchor.MiddleCenter;
            if (row is HorizontalLayoutGroup horizontal)
            {
                AxylUIDirection.Apply(horizontal, m_options.RightToLeft);
            }
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = stack;
            row.childForceExpandHeight = false;

            var go = new GameObject("CreateAccount", typeof(RectTransform));
            go.transform.SetParent(rowGo.transform, false);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = hit;
            element.minWidth = layout == AxylUIScreenLayout.Pc ? 0f : hit;

            var hitImage = go.AddComponent<Image>();
            hitImage.color = Color.clear;

            var label = NewText("Label", (RectTransform)go.transform,
                AxylUITheme.LinkFontSize, AxylUITheme.Primary);
            label.fontStyle = FontStyles.Underline;
            label.alignment = TextAlignmentOptions.Center;
            label.text = m_options.CreateAccountLabel;
            Stretch((RectTransform)label.transform);
            // The hit area is at least as wide as the text (the label is stretched over it).
            element.preferredWidth = label.GetPreferredValues(label.text).x;

            var button = go.AddComponent<Button>();
            AxylUIFocusRing.Attach(button, AxylUITheme.ChipRadius);
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = label;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = AxylUITheme.IconButtonHoverTint;
            colors.pressedColor = AxylUITheme.IconButtonPressedTint;
            colors.selectedColor = Color.white; // Focus = Default
            colors.fadeDuration = AxylUITheme.StateTransitionSeconds;
            button.colors = colors;
            button.onClick.AddListener(() => m_options.OnCreateAccount?.Invoke());
            AxylUICursor.Attach(button);
            return button;
        }

        // Any edit re-runs validation and clears a reported auth failure.
        private void OnEdited()
        {
            ShowFormError(null);
            Revalidate();
        }

        // Per-field errors show while a non-empty value breaks its rules, or while a field the
        // player has left (or tried to submit) is still empty; the login action enables only
        // while both fields pass.
        private void Revalidate()
        {
            string usernameError = FieldError(
                m_username.Text, m_options.UsernameMinLength, m_options.UsernameMaxLength,
                m_options.UsernameValidator, m_options.UsernameErrorText);
            string passwordError = FieldError(
                m_password.Text, m_options.PasswordMinLength, m_options.PasswordMaxLength,
                m_options.PasswordValidator, m_options.PasswordErrorText);
            m_username.SetError(
                m_username.Text.Length > 0 || m_usernameTouched ? usernameError : null);
            m_password.SetError(
                m_password.Text.Length > 0 || m_passwordTouched ? passwordError : null);

            m_valid = usernameError == null && passwordError == null;
            m_loginButton.SetInteractable(m_valid && m_appEnabled && !m_busy);

            // An error that appeared or went is part of the field's description: republish
            // only then, so the screen reader is not rebuilt on every keystroke.
            var shown = (m_username.Error, m_password.Error);
            if (shown != m_announcedErrors)
            {
                m_announcedErrors = shown;
                PublishAccessibility();
            }
        }

        // Empty counts as an error too (both fields are required), whatever the minimum.
        // A max of 0 is "no limit", the same as the input field's own character limit reads
        // it — taken literally it would reject every value instead of accepting every one.
        private static string FieldError(
            string value, int min, int max, Func<string, string> validator, string errorText)
        {
            if (value.Length == 0 || value.Length < min || (max > 0 && value.Length > max))
            {
                return errorText;
            }

            return validator?.Invoke(value);
        }

        private void SubmitLogin()
        {
            // A submit attempt (Enter, or the action) counts as leaving both fields: an empty
            // one now shows its error. The button only enables on a valid form, but
            // revalidate before raising so a stale enable can never submit a broken pair.
            // The gate repeats what SetInteractable was given rather than trusting the
            // button: Enter reaches here without the button, so a form the app disabled
            // through SetSubmitEnabled would otherwise submit from the keyboard alone.
            m_usernameTouched = true;
            m_passwordTouched = true;
            Revalidate();
            if (!m_valid || !m_appEnabled || m_busy)
            {
                return;
            }

            ShowFormError(null);
            SetBusy(true); // the request is in flight from here; the app's answer releases it
            m_options.OnLoginRequested?.Invoke(m_username.Text, m_password.Text);
        }

        // ---- Helpers ---------------------------------------------------------------------

        private static Sprite Icon(string name)
        {
            return Resources.Load<Sprite>("UIKit/UsernameIcons/" + name);
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
            tmp.textWrappingMode = TextWrappingModes.Normal;
            AxylUIDirection.Apply(tmp, m_options.RightToLeft);
            return tmp;
        }


        // The popup may not outgrow the safe area (Landscape is the usual case): when it
        // would, the form column gets a scroll view sized to what remains beside the fixed
        // close button and Actions area — the Common Popup rule the account screen follows.
        private static void LimitToScreenIfNeeded(
            RectTransform panel, RectTransform form, AxylUIScreenLayout layout)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            float margin = layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.SideMarginPc
                : AxylUITheme.SideMarginMobile;
            float overflow = panel.rect.height - (AxylUIScreen.SafeSize(layout).y - margin * 2f);
            if (overflow <= 0f)
            {
                return;
            }

            // The scroll view takes the form's place in the panel column at the reduced
            // height; the form column moves inside as the scrolled child.
            float visible = Mathf.Max(0f, form.rect.height - overflow);
            AxylUIScrollView.Wrap(form).gameObject
                .AddComponent<LayoutElement>().preferredHeight = visible;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
