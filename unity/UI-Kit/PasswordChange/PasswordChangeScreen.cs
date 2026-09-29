// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>The three fields of the password-change form, for addressing an app-supplied
    /// field error.</summary>
    public enum PasswordChangeField
    {
        Current,
        New,
        Confirm,
    }

    /// <summary>Everything the app passes in. The Kit is pure UI: it renders the form, checks
    /// only what a client can know (empty fields, the confirm mismatch, the app's own policy
    /// validator), and raises events — the change call, the error-code-to-copy mapping, and
    /// whether the current password is right stay with the app.</summary>
    public sealed class PasswordChangeOptions
    {
        /// <summary>Called with the current and the new password when the confirm action is
        /// selected. The screen only raises client-valid submissions; the app runs the change
        /// and answers with <see cref="PasswordChangeScreen.SetBusy"/>,
        /// <see cref="PasswordChangeScreen.ShowFormError"/> (auth failure),
        /// <see cref="PasswordChangeScreen.SetFieldError"/> (a server-judged field rule) or
        /// <see cref="PasswordChangeScreen.ShowToast"/> (result / transient error).</summary>
        public Action<string, string> OnSubmit { get; set; }

        /// <summary>Called when the popup asks to close: the close button or Esc (the dim
        /// backdrop blocks input but does not close, so a mis-tap can't discard a form
        /// mid-edit). Navigation is the app's. Unused when <see cref="Popup"/> is false.</summary>
        public Action OnClose { get; set; }

        /// <summary>True (default) presents the form as a popup over the dimmed game, with a
        /// close button; false renders the centered panel alone for a full-screen flow.</summary>
        public bool Popup { get; set; } = true;

        // Display strings — the app's localized strings for the keys in AxylUIStringKeys.
        // The Kit holds no copy of its own: an unset string shows its key. The fields carry
        // no label row: the placeholder names each one.
        public string Title { get; set; } = AxylUIStringKeys.PasswordChangeTitle;

        public string CurrentPlaceholder { get; set; } = AxylUIStringKeys.CurrentPasswordPlaceholder;

        public string NewPlaceholder { get; set; } = AxylUIStringKeys.NewPasswordPlaceholder;

        public string ConfirmPlaceholder { get; set; } = AxylUIStringKeys.ConfirmPasswordPlaceholder;

        public string SubmitLabel { get; set; } = AxylUIStringKeys.PasswordChangeSubmit;

        // Error copy. Each message shows under the input it concerns, which also paints
        // its error border.
        public string CurrentRequiredText { get; set; } = AxylUIStringKeys.CurrentPasswordRequired;

        public string NewRequiredText { get; set; } = AxylUIStringKeys.NewPasswordRequired;

        public string ConfirmRequiredText { get; set; } = AxylUIStringKeys.ConfirmPasswordRequired;

        public string MismatchErrorText { get; set; } = AxylUIStringKeys.ConfirmPasswordMismatch;

        /// <summary>Shown under the new password while it breaks the length rule
        /// (<see cref="PasswordMinLength"/>–<see cref="PasswordMaxLength"/>).</summary>
        public string NewInvalidText { get; set; } = AxylUIStringKeys.NewPasswordInvalid;

        // Accessibility names — read by the screen reader, never drawn.
        public string CloseLabel { get; set; } = AxylUIStringKeys.Close;

        public string PasswordShowLabel { get; set; } = AxylUIStringKeys.PasswordShow;

        public string PasswordHideLabel { get; set; } = AxylUIStringKeys.PasswordHide;

        /// <summary>The password length rule, the same one the username login applies
        /// (8–20 characters by default): a new password outside the range shows
        /// <see cref="NewInvalidText"/> — a masked field is never cut silently, so a too-long
        /// value (a pasted one, say) stays whole and errors instead. Further rules are the
        /// app's policy, through <see cref="NewPasswordValidator"/>.</summary>
        public int PasswordMinLength { get; set; } = 8;

        public int PasswordMaxLength { get; set; } = 20;

        /// <summary>Optional new-password policy rule: return an error message to show under
        /// the new password, or null when the value passes.</summary>
        public Func<string, string> NewPasswordValidator { get; set; }

        /// <summary>True for a right-to-left language (Arabic, Hebrew, Persian — see
        /// <see cref="AxylUIDirection.IsRightToLeft"/>): text aligns right and every
        /// horizontal row runs right-to-left; glyphs are never mirrored.</summary>
        public bool RightToLeft { get; set; }

        /// <summary>Set to pin a layout; leave null to pick one from the display.</summary>
        public AxylUIScreenLayout? ForcedLayout { get; set; }
    }

    /// <summary>
    /// The password-change form, in the Common Popup frame: the title, three masked password
    /// inputs — current, new, and confirm, each with a show/hide toggle and named by its
    /// placeholder, each with its own error line — and the confirm action right under them.
    /// Presented as a popup over the dimmed game or as a full-screen panel.
    /// </summary>
    /// <remarks>
    /// <para><b>Behavior.</b> The confirm action enables only while all three fields are
    /// filled, the confirmation matches, and the app's policy validator (when given) passes.
    /// Every error shows under the input it concerns, which paints its error border: a
    /// missing value or the length rule under that field, a policy failure under the new
    /// password, the mismatch under the confirmation, and the app's auth failure (wrong
    /// current password, reported via <see cref="ShowFormError"/>) under the current
    /// password. A field's error appears once editing leaves it or a submit is attempted,
    /// and clears as the value is corrected; the auth failure clears when the current
    /// password is edited. The result and transient errors go through
    /// <see cref="ShowToast"/>; no error is ever a toast.</para>
    /// <para><b>Height.</b> The popup never outgrows the safe area: when the form is taller,
    /// the form — inputs and confirm action together — scrolls under the fixed close
    /// button.</para>
    /// <para><b>How to customize.</b> Colors, sizes and spacing all live in
    /// <see cref="AxylUITheme"/>. The widget look lives in the prefabs under
    /// Resources/UIKit/Prefabs (rebake with the AxylUIBaker after a structural change). Copy
    /// comes in through <see cref="PasswordChangeOptions"/>. The screen itself is built in the
    /// numbered steps of <see cref="Build"/>; each step is one visual region.</para>
    /// </remarks>
    public sealed class PasswordChangeScreen : MonoBehaviour
    {
        private readonly List<Selectable> m_tabOrder = new List<Selectable>();
        private PasswordChangeOptions m_options;
        private AxylUIInputGroup m_current;
        private AxylUIInputGroup m_new;
        private AxylUIInputGroup m_confirm;
        private AxylUIActionButton m_submitButton;
        private FailureToast m_toast;
        private GameObject m_previousSelection;
        private bool m_busy;
        private readonly AxylUIScreenSizeWatch m_sizeWatch = new AxylUIScreenSizeWatch();
        private bool m_valid;
        private readonly string[] m_announcedErrors = new string[s_fields.Length]; // last announced

        // Error state that outlives a rebuild. A field's own error shows only once the player
        // has left it (or tried to submit); the app's verdicts stay until the field they
        // concern is edited.
        private readonly HashSet<PasswordChangeField> m_touched = new HashSet<PasswordChangeField>();
        private readonly Dictionary<PasswordChangeField, string> m_appErrors =
            new Dictionary<PasswordChangeField, string>();
        private string m_authError;
        private bool m_appEnabled = true;

        private static readonly PasswordChangeField[] s_fields =
        {
            PasswordChangeField.Current, PasswordChangeField.New, PasswordChangeField.Confirm,
        };

        /// <summary>Creates the screen on its own overlay canvas and shows it.</summary>
        public static PasswordChangeScreen Show(PasswordChangeOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var layout = options.ForcedLayout ?? AxylUIScreen.DetectLayout();
            var screen = AxylUIScreen.CreateCanvas("AxylUIPasswordChangeScreen", layout)
                .AddComponent<PasswordChangeScreen>();
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

        /// <summary>Shows the auth-failure message (wrong current password) under the current
        /// password, which paints its error border; null clears it. Editing the current
        /// password clears it too.</summary>
        public void ShowFormError(string message)
        {
            m_authError = string.IsNullOrEmpty(message) ? null : message;
            RefreshErrors();
        }

        /// <summary>Shows the shared toast: the change result (Success) or a transient
        /// error (Error). Message wording is the app's.</summary>
        public void ShowToast(string message, AxylUIToastVariant variant = AxylUIToastVariant.Error)
        {
            m_toast.Show(message, variant);
        }

        /// <summary>Reports an app-judged field error (a policy failure the Kit cannot know):
        /// the message shows under that field, which paints its error border; null clears
        /// it. Editing that field clears it too.</summary>
        public void SetFieldError(PasswordChangeField field, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                m_appErrors.Remove(field);
            }
            else
            {
                m_appErrors[field] = message;
                m_touched.Add(field);
            }

            RefreshErrors();
            Revalidate(); // the app's verdict gates the submit, so the button follows it too
        }

        /// <summary>True while the app's change call is in flight: the confirm action disables
        /// and the fields lock, so a second submission cannot start. A submit sets this
        /// itself; the app clears it when its call returns.</summary>
        public void SetBusy(bool busy)
        {
            m_busy = busy;
            m_current.SetInteractable(!busy);
            m_new.SetInteractable(!busy);
            m_confirm.SetInteractable(!busy);
            Revalidate();
        }

        /// <summary>The app's own say on whether the change may go ahead; false disables the
        /// confirm action on top of the Kit's field rules. Defaults to true.</summary>
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
        // Tab / Shift+Tab walk the form's focus order.
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
                Submit(); // revalidates, and a busy or invalid form ignores it
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
            string current = m_current != null ? m_current.Text : null;
            string fresh = m_new != null ? m_new.Text : null;
            string confirm = m_confirm != null ? m_confirm.Text : null;
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

            // Restoring the text counts as an edit, which would drop the app's verdicts; a
            // rotation should not lose a server message, so they are put back afterwards.
            string authError = m_authError;
            var appErrors = new Dictionary<PasswordChangeField, string>(m_appErrors);

            m_tabOrder.Clear();
            Build(layout);
            m_current.SetText(current);
            m_new.SetText(fresh);
            m_confirm.SetText(confirm);

            m_authError = authError;
            foreach (var pair in appErrors)
            {
                m_appErrors[pair.Key] = pair.Value;
            }

            RefreshErrors();
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

                string label = null;
                string description = null; // an input's current error, read after its name
                bool textField = false;
                foreach (var field in s_fields)
                {
                    var group = Group(field);
                    if (control == group.Field)
                    {
                        label = Placeholder(field);
                        description = group.Error;
                        textField = true;
                    }
                    else if (control == group.Toggle)
                    {
                        label = group.Revealed ? m_options.PasswordHideLabel : m_options.PasswordShowLabel;
                    }
                }

                if (label == null)
                {
                    if (control == m_submitButton.GetComponent<Button>())
                    {
                        label = m_options.SubmitLabel;
                    }
                    else if (control.GetComponent<CloseButton>() != null)
                    {
                        label = m_options.CloseLabel;
                    }
                    else
                    {
                        continue; // a control this map does not know gets no wrong name
                    }
                }

                controls.Add(new AxylUIAccessibleControl(control, label, textField, description));
            }

            AxylUIAccessibility.Install(gameObject, controls);
        }

        // The field's placeholder names it (the form has no label row), so it names the
        // field for the screen reader too — three distinct names, one per field.
        private string Placeholder(PasswordChangeField field) => field switch
        {
            PasswordChangeField.Current => m_options.CurrentPlaceholder,
            PasswordChangeField.New => m_options.NewPlaceholder,
            _ => m_options.ConfirmPlaceholder,
        };

        // Which control of the keyboard path holds the focus (-1: none of them) — the same
        // control is selected again after a rebuild. The index covers every tab stop, so a
        // rotation while the close button or a visibility toggle held the focus restores
        // that control rather than dropping to one of the three inputs.
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

        private static float PanelPadding(AxylUIScreenLayout layout) => layout switch
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

            // Step 2 — the form panel, centered; height follows its content.
            float sideMargin = layout == AxylUIScreenLayout.Pc
                ? AxylUITheme.SideMarginPc
                : AxylUITheme.SideMarginMobile;
            float pad = PanelPadding(layout);
            float panelWidth = layout == AxylUIScreenLayout.Portrait
                ? size.x - sideMargin * 2f
                : Mathf.Min(
                    AxylUITheme.FormWidth + pad * 2f,
                    Mathf.Min(AxylUITheme.FormPanelMaxWidth, size.x - sideMargin * 2f));
            float gap = GroupGap(layout);
            var panel = BuildPanel(root, panelWidth, pad, gap);

            // Step 3 — the popup's close control, top-right of the panel, out of the flow.
            // Closing never submits: the form's values just go with the popup.
            if (m_options.Popup)
            {
                var close = InstantiateWidget<CloseButton>("CloseButton", panel);
                close.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                close.Configure(
                    () => m_options.OnClose?.Invoke(), AxylUITheme.TextPrimary, m_options.RightToLeft);
                m_tabOrder.Add(close.GetComponent<Button>());
            }

            // Step 4 — the form column: title, inputs (each with its error line) and the
            // confirm action.
            // It scrolls as one when the popup is taller than the screen.
            var form = BuildColumn("Form", panel, gap);

            // Step 5 — title, centered over the form.
            var title = NewText("Title", form, AxylUITheme.TitleFontSize, AxylUITheme.TextPrimary);
            title.fontWeight = FontWeight.SemiBold;
            title.alignment = TextAlignmentOptions.Center;
            // Kept clear of the close button's corner on both sides, so a long title wraps
            // under the popup's header instead of behind the X (and stays centered).
            float titleInset = m_options.Popup ? AxylUITheme.CloseHitSize : 0f;
            title.margin = new Vector4(titleInset, 0f, titleInset, 0f);
            title.text = m_options.Title;

            // Step 6 — the three password inputs, in the fixed order: current → new →
            // confirm, each named by its placeholder (no label row). A field's error shows
            // when its editing ends; every edit re-gates the confirm action.
            m_current = BuildPasswordInput(form, PasswordChangeField.Current, m_options.CurrentPlaceholder);
            m_new = BuildPasswordInput(form, PasswordChangeField.New, m_options.NewPlaceholder);
            m_confirm = BuildPasswordInput(form, PasswordChangeField.Confirm, m_options.ConfirmPlaceholder);
            // The navigation baseline runs close, the three fields, then confirm — one
            // field per Tab press. The show/hide toggles stay keyboard-reachable after
            // that path instead of sitting between the fields.
            foreach (var group in new[] { m_current, m_new, m_confirm })
            {
                m_tabOrder.Add(group.Field);
            }

            // Step 7 — the confirm action, under the last input group, enabled only while
            // the form is client-valid. Errors live under their own inputs; there is no
            // shared error area.
            m_submitButton = InstantiateWidget<AxylUIActionButton>("AxylUIActionButton", form);
            var buttonRect = (RectTransform)m_submitButton.transform;
            buttonRect.sizeDelta = new Vector2(
                panelWidth - pad * 2f, AxylUITheme.ActionButtonHeight);
            var buttonElement = m_submitButton.gameObject.AddComponent<LayoutElement>();
            buttonElement.preferredHeight = AxylUITheme.ActionButtonHeight;
            m_submitButton.Configure(
                m_options.SubmitLabel, AxylUIActionStyle.Primary, Submit,
                m_options.RightToLeft);
            m_tabOrder.Add(m_submitButton.GetComponent<Button>());
            foreach (var group in new[] { m_current, m_new, m_confirm })
            {
                m_tabOrder.Add(group.Toggle);
            }

            // Step 8 — a form taller than the screen scrolls as a whole (inputs and confirm
            // action together) under the fixed close button; a popup that fits keeps its
            // plain layout.
            LimitToScreenIfNeeded(panel, form, layout);

            // Focus stays inside the popup (a modal keeps focus until it closes), and
            // the screen reader gets every control by name — each toggle's name follows
            // its field's state.
            AxylUIFocusTrap.Install(gameObject, m_tabOrder);
            foreach (var group in new[] { m_current, m_new, m_confirm })
            {
                group.RevealChanged += PublishAccessibility;
            }

            PublishAccessibility();

            // Step 9 — the shared toast (the change result and transient errors) on its own
            // layer above everything else, so it never moves the form.
            m_toast = InstantiateWidget<FailureToast>("FailureToast", AxylUIScreen.ToastLayer(gameObject));
            m_toast.Configure(
                topEdge: layout == AxylUIScreenLayout.Landscape,
                wide: layout == AxylUIScreenLayout.Pc,
                availableWidth: size.x,
                rightToLeft: m_options.RightToLeft);

            // No field is focused on open: the soft keyboard belongs to the player's first
            // tap, not to the screen appearing. The focus trap starts the keyboard path at
            // the close button — with no popup frame, at the current-password field
            // instead, selected without activating so the keyboard stays down — and a
            // rebuild (a rotation) restores whichever control the player had. Show
            // remembered the app's own selection for Dismiss to restore.
            RefreshErrors();
            Revalidate();
        }

        private AxylUIInputGroup BuildPasswordInput(
            RectTransform form, PasswordChangeField field, string placeholder)
        {
            var group = InstantiateWidget<AxylUIInputGroup>("AxylUIInputGroup", form);
            group.Configure(
                label: null, placeholder, Icon("lock"), secure: true,
                m_options.PasswordMaxLength, _ => OnEdited(field), m_options.RightToLeft);
            group.EndEdited += () =>
            {
                m_touched.Add(field);
                RefreshErrors();
            };
            return group;
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

        private AxylUIInputGroup Group(PasswordChangeField field) => field switch
        {
            PasswordChangeField.Current => m_current,
            PasswordChangeField.New => m_new,
            _ => m_confirm,
        };

        // An edit drops the app's verdicts about that field (the auth failure belongs to the
        // current password), lets a shown error clear as soon as its value passes again, and
        // re-gates the confirm action.
        private void OnEdited(PasswordChangeField field)
        {
            m_appErrors.Remove(field);
            if (field == PasswordChangeField.Current)
            {
                m_authError = null;
            }

            RefreshErrors();
            Revalidate();
        }

        // The client-side gate: three filled fields, a matching confirmation, and the app's
        // policy rule. Whether the current password is actually right is the server's call.
        private void Revalidate()
        {
            m_valid = true;
            foreach (var field in s_fields)
            {
                m_valid &= FieldError(field) == null;
            }

            m_submitButton.SetInteractable(m_valid && m_appEnabled && !m_busy);
        }

        // What is wrong with one field right now, in the order the player reads it: a
        // missing value, then the field's own rule, then the app's verdict about it.
        private string FieldError(PasswordChangeField field)
        {
            var group = Group(field);
            if (group.Text.Length == 0)
            {
                return field switch
                {
                    PasswordChangeField.Current => m_options.CurrentRequiredText,
                    PasswordChangeField.New => m_options.NewRequiredText,
                    _ => m_options.ConfirmRequiredText,
                };
            }

            string ruleError = field switch
            {
                PasswordChangeField.New => m_new.Text.Length < m_options.PasswordMinLength
                    || (m_options.PasswordMaxLength > 0
                        && m_new.Text.Length > m_options.PasswordMaxLength)
                    ? m_options.NewInvalidText
                    : m_options.NewPasswordValidator?.Invoke(m_new.Text),
                PasswordChangeField.Confirm =>
                    m_confirm.Text == m_new.Text ? null : m_options.MismatchErrorText,
                _ => null,
            };
            if (ruleError != null)
            {
                return ruleError;
            }

            return m_appErrors.TryGetValue(field, out var appError) ? appError : null;
        }

        // The one place the error display is decided: each touched field at fault shows its
        // message under itself and paints its border; the current password also carries the
        // app's auth failure when its own value is fine. Untouched fields stay quiet: a
        // half-typed confirmation is not an error yet.
        private void RefreshErrors()
        {
            bool changed = false;
            for (int i = 0; i < s_fields.Length; i++)
            {
                var field = s_fields[i];
                string error = m_touched.Contains(field) ? FieldError(field) : null;
                if (field == PasswordChangeField.Current)
                {
                    error ??= m_authError;
                }

                Group(field).SetError(error);
                changed |= error != m_announcedErrors[i];
                m_announcedErrors[i] = error;
            }

            // An error that appeared or went is part of the field's description: republish
            // only then, so the screen reader is not rebuilt on every keystroke.
            if (changed && m_submitButton != null)
            {
                PublishAccessibility();
            }
        }

        private void Submit()
        {
            // A submit attempt counts as leaving every field: a missing value now shows. The
            // button only enables on a valid form, but revalidate before raising so a stale
            // enable can never submit a broken form.
            foreach (var field in s_fields)
            {
                m_touched.Add(field);
            }

            RefreshErrors();
            Revalidate();
            // The gate repeats what SetInteractable was given rather than trusting the
            // button: Enter reaches here without the button, so a form the app disabled
            // through SetSubmitEnabled would otherwise submit from the keyboard alone.
            if (!m_valid || !m_appEnabled || m_busy)
            {
                return;
            }

            ShowFormError(null);
            SetBusy(true); // the request is in flight from here; the app's answer releases it
            m_options.OnSubmit?.Invoke(m_current.Text, m_new.Text);
        }

        // ---- Helpers ---------------------------------------------------------------------

        private static Sprite Icon(string name)
        {
            return Resources.Load<Sprite>("UIKit/PasswordChangeIcons/" + name);
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
        // would, the form column gets a scroll view sized to what remains inside the fixed
        // popup frame — the Common Popup rule the account screen follows.
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
