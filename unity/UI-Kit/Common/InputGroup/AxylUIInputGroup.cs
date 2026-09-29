// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// One labeled input of a form, authored as AxylUIInputGroup.prefab (repeated UI ships as
    /// a prefab): a label over a 48px field — leading icon, the text itself, and (for a secure
    /// field) a visibility toggle — with an error line underneath. Shared by every form
    /// screen; the group renders and reports, while validation rules and error copy come from
    /// the screen.
    /// </summary>
    /// <remarks>
    /// <para><b>How to customize.</b> Structure and resting look live in the prefab; every
    /// color, size and duration reads from <see cref="AxylUITheme"/>. The state look itself is
    /// decided in one place, <see cref="Refresh"/>: the resting and error borders. Focus
    /// changes neither — the resting style holds while the field is edited, and the error
    /// border holds through focus; keyboard focus shows the shared <see cref="AxylUIFocusRing"/>
    /// around the box.</para>
    /// <para>A secure field masks its value by default; the trailing toggle shows and hides it
    /// without losing the value.</para>
    /// </remarks>
    public sealed class AxylUIInputGroup : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_label;
        [SerializeField] private Image m_borderImage;
        [SerializeField] private Image m_fillImage;
        [SerializeField] private Image m_icon;
        [SerializeField] private TMP_InputField m_input;
        [SerializeField] private TMP_Text m_placeholder;
        [SerializeField] private Button m_toggleButton;
        [SerializeField] private Image m_toggleIcon;
        [SerializeField] private TMP_Text m_error;

        private Sprite m_showIcon;
        private Sprite m_hideIcon;
        private bool m_revealed;
        private bool m_hasError;

        /// <summary>The current field value, as typed.</summary>
        public string Text => m_input.text;

        /// <summary>Replaces the field's text — a screen rebuilding itself for a new layout
        /// carries typed input over with this. Runs the edited callback, so validation
        /// re-judges the restored value.</summary>
        public void SetText(string value) => m_input.text = value ?? string.Empty;

        /// <summary>The field itself, for the screen's keyboard Tab order.</summary>
        public Selectable Field => m_input;

        /// <summary>The visibility toggle, for the Tab order; hidden on a plain field.</summary>
        public Selectable Toggle => m_toggleButton;

        /// <summary>Raised when editing ends (the field loses focus or the value is
        /// submitted) — the moment deferred validation is meant to show its error.</summary>
        public event Action EndEdited;

        /// <summary>True while a secure value is shown in the clear.</summary>
        public bool Revealed => m_revealed;

        /// <summary>Raised when the visibility toggle flips — the toggle's accessibility
        /// name ("show" or "hide" the password) follows the new state.</summary>
        public event Action RevealChanged;

        /// <summary>Fills a prefab instance with one field's content and wires the callbacks.</summary>
        /// <param name="label">The field label, already localized by the app; null or empty
        /// leaves the label row out (a form whose fields carry only placeholders).</param>
        /// <param name="placeholder">The empty-field hint, already localized by the app.</param>
        /// <param name="icon">The 20×20 leading glyph; tinted, purely decorative.</param>
        /// <param name="secure">True masks the value and shows the visibility toggle.</param>
        /// <param name="maxLength">A visible field refuses input past this length; a masked
        /// field accepts it whole (a cut the player cannot see) and leaves the report to the
        /// screen's length rule. 0 for no limit.</param>
        /// <param name="onChanged">Raised with the new value on every edit.</param>
        /// <param name="rightToLeft">Mirrors the field for a right-to-left language: the
        /// leading glyph and the toggle swap sides, the text aligns right.</param>
        public void Configure(
            string label, string placeholder, Sprite icon, bool secure, int maxLength,
            Action<string> onChanged, bool rightToLeft = false)
        {
            AxylUIDirection.Apply(m_input.GetComponentInParent<HorizontalLayoutGroup>(), rightToLeft);
            AxylUIDirection.Apply(m_label, rightToLeft);
            AxylUIDirection.Apply(m_placeholder, rightToLeft);
            AxylUIDirection.Apply(m_error, rightToLeft);

            // The typed value (an id, an e-mail, a password) is mostly Latin: keep its glyph
            // order — TMP's RTL flag reverses characters blindly and the input's caret and
            // selection do not follow it — and only move the text to the right edge. A game
            // taking Hebrew input adds its own bidi step, as the root README's language
            // section describes.
            if (rightToLeft)
            {
                m_input.textComponent.horizontalAlignment = HorizontalAlignmentOptions.Right;
            }

            bool hasLabel = !string.IsNullOrEmpty(label);
            gameObject.name = $"Input_{(hasLabel ? label : placeholder)}";
            m_label.text = hasLabel ? label : string.Empty;
            m_label.gameObject.SetActive(hasLabel);
            m_placeholder.text = placeholder;
            m_icon.sprite = icon;

            // A masked value is never cut silently: the player cannot see the cut, so a
            // pasted 24-character password would quietly become its first 20 characters.
            // The field takes the long value and the screen's length rule reports it.
            m_input.characterLimit = secure && maxLength > 0
                ? Mathf.Max(maxLength, 128)
                : maxLength;
            m_input.contentType = secure
                ? TMP_InputField.ContentType.Password
                : TMP_InputField.ContentType.Standard;

            // The typed value must read back exactly as typed: with rich text on, a value
            // like "Pa<b>ss" renders as markup in the id field and in a revealed password.
            m_input.richText = false;
            m_input.textComponent.parseCtrlCharacters = false;

            AxylUIFocusRing.Attach(
                m_input, AxylUITheme.InputRadius, (RectTransform)m_borderImage.transform);
            m_toggleButton.gameObject.SetActive(secure);
            if (secure)
            {
                AxylUICursor.Attach(m_toggleButton);
                AxylUIFocusRing.Attach(m_toggleButton, AxylUITheme.InputToggleHitSize / 2f);
                m_showIcon = Resources.Load<Sprite>("UIKit/InputIcons/eye");
                m_hideIcon = Resources.Load<Sprite>("UIKit/InputIcons/eye_off");
                m_toggleIcon.sprite = m_showIcon;
                m_toggleButton.onClick.AddListener(ToggleReveal);
            }

            if (onChanged != null)
            {
                m_input.onValueChanged.AddListener(value => onChanged(value));
            }

            m_input.onEndEdit.AddListener(_ => EndEdited?.Invoke());
            SetError(null);
        }

        /// <summary>The error message shown under the field, or null when there is none —
        /// what the screen hands the screen reader as the field's description.</summary>
        public string Error => m_hasError && m_error.text.Length > 0 ? m_error.text : null;

        /// <summary>Shows <paramref name="message"/> under the field and paints the error
        /// border; null clears both.</summary>
        public void SetError(string message)
        {
            m_hasError = !string.IsNullOrEmpty(message);
            m_error.text = m_hasError ? message : string.Empty;
            m_error.gameObject.SetActive(m_hasError);
            Refresh();
        }

        /// <summary>Blocks edits while a request is in flight.</summary>
        public void SetInteractable(bool interactable)
        {
            m_input.interactable = interactable;
            m_toggleButton.interactable = interactable;
        }

        // Shows or hides a secure value in place: the value survives the switch. There is
        // no caret to keep — pressing the eye already ended the edit, since the input
        // module deselects the field before the toggle's click lands.
        private void ToggleReveal()
        {
            m_revealed = !m_revealed;
            m_toggleIcon.sprite = m_revealed ? m_hideIcon : m_showIcon;
            RevealChanged?.Invoke();

            m_input.contentType = m_revealed
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            m_input.ForceLabelUpdate();
        }

        // The one place the field states are decided:
        //   Resting — Surface Card fill, Border border; Focus keeps this same style (the
        //             keyboard focus ring is a separate child, see AxylUIFocusRing)
        //   Error   — danger border (held through focus), message shown underneath
        private void Refresh()
        {
            m_borderImage.color = m_hasError ? AxylUITheme.SemanticDown : AxylUITheme.Border;
            m_fillImage.color = AxylUITheme.Surface;
            m_error.color = AxylUITheme.SemanticDown;
        }
    }
}
