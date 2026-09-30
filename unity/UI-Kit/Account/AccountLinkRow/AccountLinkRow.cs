// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// One provider row of the account popup, authored as AccountLinkRow.prefab (repeated UI
    /// ships as a prefab). Shows the provider mark, the label, and the link state on the
    /// right: a plus while disconnected (selecting starts the app's login/link flow), a
    /// check with a Primary-tinted row while connected. The row raises the click and knows
    /// nothing about providers or the SDK.
    /// </summary>
    /// <remarks>
    /// <para><b>How to customize.</b> Structure and resting look live in the prefab; every
    /// color, size and duration reads from <see cref="AxylUITheme"/>. The state look itself is
    /// decided in one place, <see cref="Refresh"/>: to change what Connected, Hover, Pressed
    /// or Disabled looks like, edit that method.</para>
    /// <para>Focus tints nothing on the row itself — keyboard/controller focus shows the
    /// shared <see cref="AxylUIFocusRing"/> outside it; navigation works through the stock
    /// <see cref="Button"/>. The provider mark never changes with the state.</para>
    /// </remarks>
    public sealed class AccountLinkRow :
        MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image m_borderImage;
        [SerializeField] private Image m_fillImage;
        [SerializeField] private Image m_icon;
        [SerializeField] private TMP_Text m_glyph;
        [SerializeField] private TMP_Text m_label;
        [SerializeField] private Image m_plus;
        [SerializeField] private Image m_check;
        [SerializeField] private Button m_button;

        private RectTransform m_rect;
        private Coroutine m_transition;

        private bool m_connected;
        private bool m_interactable;
        private bool m_hovered;
        private bool m_pressed;

        /// <summary>Fills a prefab instance with one provider's content and wires the click.</summary>
        /// <param name="label">The row text, already localized by the app.</param>
        /// <param name="iconSprite">The 20×20 mark; null keeps the prefab's fallback chip.</param>
        /// <param name="iconFallbackText">Letters drawn over the chip when there is no mark.</param>
        /// <param name="connected">True renders the linked state: Primary tint + check.</param>
        /// <param name="interactable">False renders the Disabled state and swallows clicks.</param>
        /// <param name="onClick">Raised on click, tap, or Enter/Space while focused.</param>
        /// <param name="rightToLeft">Runs the row right-to-left — mark, label, state icon —
        /// with the label right-aligned; the mark itself is never mirrored.</param>
        public void Configure(
            string label, Sprite iconSprite, string iconFallbackText,
            bool connected, bool interactable, Action onClick, bool rightToLeft = false)
        {
            gameObject.name = $"AccountLinkRow_{label}";
            AxylUICursor.Attach(m_button);
            AxylUIDirection.Apply(GetComponentInChildren<HorizontalLayoutGroup>(), rightToLeft);
            AxylUIDirection.Apply(m_label, rightToLeft);
            m_label.maxVisibleLines = 2; // not a serialized field: set here, not in the bake
            m_label.text = label;
            AxylUITypography.SetLineHeight(m_label, AxylUITheme.ButtonLineHeight);
            m_connected = connected;
            m_interactable = interactable;
            m_button.interactable = interactable;

            SetHeight(AxylUITheme.AccountRowHeight);

            if (iconSprite != null)
            {
                m_icon.sprite = iconSprite;
                // Bitmap marks render whole (aspect kept); the chip sprite is 9-sliced.
                m_icon.type = iconSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                m_icon.color = Color.white;
            }

            bool glyph = iconSprite == null && !string.IsNullOrEmpty(iconFallbackText);
            m_glyph.gameObject.SetActive(glyph);
            if (glyph)
            {
                m_glyph.text = iconFallbackText;
                m_glyph.fontSize = iconFallbackText.Length > 1 ? 8f : 10f;
            }

            if (onClick != null)
            {
                m_button.onClick.AddListener(() => onClick());
            }

            ApplyState(instant: true);
        }

        /// <summary>The height this row needs at <paramref name="width"/>: the 44px minimum,
        /// or more when the label wraps to its second line, as it can in Portrait. The grid
        /// gives every row the tallest of these.</summary>
        public float PreferredHeight(float width)
        {
            float labelWidth = width - AxylUITheme.ButtonPaddingH * 2f
                - AxylUITheme.ButtonIconSize - AxylUITheme.ButtonIconTextGap * 2f
                - AxylUITheme.AccountStateIconSize;
            float oneLine = m_label.GetPreferredValues("A", labelWidth, 0f).y;
            float needed = m_label.GetPreferredValues(m_label.text, labelWidth, 0f).y;
            float extra = needed > oneLine + 0.5f ? Mathf.Min(needed - oneLine, oneLine) : 0f;
            return AxylUITheme.AccountRowHeight + extra;
        }

        /// <summary>Sets the row height and rescales the baked pill to that height's radius;
        /// the border sprite carries radius+1px of flat pad, the fill sits 1px inside it
        /// (see LoginOptionButton).</summary>
        public void SetHeight(float height)
        {
            float baked = AxylUITheme.BakedPillRadius;
            float radius = height / 2f;
            AxylUIFocusRing.Attach(m_button, radius); // reshaped to each height
            m_borderImage.pixelsPerUnitMultiplier = (baked + 1f) / (radius + 1f);
            m_fillImage.pixelsPerUnitMultiplier = baked / radius;
        }

        private void Awake()
        {
            m_rect = (RectTransform)transform;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_hovered = true;
            ApplyState(instant: false);
        }

        // Also ends Pressed when a touch is cancelled or released outside the row.
        public void OnPointerExit(PointerEventData eventData)
        {
            m_hovered = false;
            m_pressed = false;
            ApplyState(instant: false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_pressed = true;
            ApplyState(instant: false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_pressed = false;
            ApplyState(instant: false);
        }

        // The one place the states are decided:
        //   Disconnected — Surface fill, Border border, plus
        //   Connected    — StateHoverBg fill, Primary border, Primary label, check
        //   Hover        — StateHoverBg fill, Primary border            (pointer only)
        //   Pressed      — StatePressedBg fill, Primary border, 0.98 scale
        //   Disabled     — Surface fill, BorderSoft border, TextDisabled label and icon
        //                  (the state icon stays: linked or not is never color-only)
        //   Focus        — identical to the resting state (the keyboard focus ring is a
        //                  separate child, see AxylUIFocusRing)
        private void Refresh(out Color fill, out Color border, out Color labelColor,
            out float scale, out float seconds)
        {
            fill = AxylUITheme.Surface;
            border = AxylUITheme.Border;
            labelColor = AxylUITheme.TextPrimary;
            scale = 1f;
            seconds = AxylUITheme.StateTransitionSeconds;

            if (!m_interactable)
            {
                border = AxylUITheme.BorderSoft;
                labelColor = AxylUITheme.TextDisabled;
                return;
            }

            if (m_connected)
            {
                fill = AxylUITheme.StateHoverBg;
                border = AxylUITheme.Primary;
                labelColor = AxylUITheme.Primary;
            }

            if (m_hovered)
            {
                fill = AxylUITheme.StateHoverBg;
                border = AxylUITheme.Primary;
            }

            if (m_pressed)
            {
                fill = AxylUITheme.StatePressedBg;
                border = AxylUITheme.Primary;
                scale = AxylUITheme.StatePressedScale;
                seconds = AxylUITheme.StatePressedTransitionSeconds;
            }
        }

        private void ApplyState(bool instant)
        {
            Refresh(out var fill, out var border, out var labelColor, out var scale, out var seconds);

            m_label.color = labelColor;
            m_plus.gameObject.SetActive(!m_connected);
            m_plus.color = m_interactable ? AxylUITheme.TextMuted : AxylUITheme.TextDisabled;
            m_check.gameObject.SetActive(m_connected);
            m_check.color = m_interactable ? AxylUITheme.Primary : AxylUITheme.TextDisabled;

            if (m_transition != null)
            {
                StopCoroutine(m_transition);
                m_transition = null;
            }

            if (instant || !isActiveAndEnabled)
            {
                m_fillImage.color = fill;
                m_borderImage.color = border;
                m_rect.localScale = new Vector3(scale, scale, 1f);
                return;
            }

            m_transition = StartCoroutine(TransitionTo(fill, border, scale, seconds));
        }

        private IEnumerator TransitionTo(Color fill, Color border, float scale, float seconds)
        {
            Color fill0 = m_fillImage.color;
            Color border0 = m_borderImage.color;
            float scale0 = m_rect.localScale.x;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                m_fillImage.color = Color.Lerp(fill0, fill, k);
                m_borderImage.color = Color.Lerp(border0, border, k);
                float s = Mathf.Lerp(scale0, scale, k);
                m_rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            m_fillImage.color = fill;
            m_borderImage.color = border;
            m_rect.localScale = new Vector3(scale, scale, 1f);
            m_transition = null;
        }
    }
}
