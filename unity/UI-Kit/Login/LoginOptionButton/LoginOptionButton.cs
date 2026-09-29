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
    /// One provider row of the login screen, authored as LoginOptionButton.prefab (repeated
    /// UI ships as a prefab). Owns the Default / Hover / Pressed visuals (tints from
    /// <see cref="AxylUITheme"/>); raises the click through the standard
    /// <see cref="Button"/> and knows nothing about providers or the SDK.
    /// </summary>
    /// <remarks>
    /// <para><b>How to customize.</b> Structure and resting look live in the prefab; every
    /// color, size and duration reads from <see cref="AxylUITheme"/>. The state look itself is
    /// decided in one place, <see cref="Refresh"/>: to change what Hover or Pressed looks like,
    /// edit that method.</para>
    /// <para>Hover applies to pointer input only. Focus tints nothing on the button itself —
    /// keyboard/controller focus shows the shared <see cref="AxylUIFocusRing"/> outside it;
    /// navigation works through the stock <see cref="Button"/>. Disabled and Loading are out
    /// of the Reference UI's scope.</para>
    /// </remarks>
    public sealed class LoginOptionButton :
        MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        // The pill sprites are baked at AxylUITheme.BakedPillRadius (the PC button height); a
        // different button height rescales the 9-slice through pixelsPerUnitMultiplier so the
        // ends stay true semicircles — see Configure.
        [SerializeField] private Image m_borderImage;
        [SerializeField] private Image m_fillImage;
        [SerializeField] private Image m_icon;
        [SerializeField] private TMP_Text m_glyph;
        [SerializeField] private TMP_Text m_label;
        [SerializeField] private Button m_button;

        private RectTransform m_rect;
        private Coroutine m_transition;

        private bool m_hovered;
        private bool m_pressed;

        /// <summary>Fills a prefab instance with one provider's content and wires the click.</summary>
        /// <param name="label">The button text, already localized by the app.</param>
        /// <param name="iconSprite">The 20×20 mark; null keeps the prefab's fallback chip.</param>
        /// <param name="iconTint">Tint for the icon sprite; use white for full-color marks.</param>
        /// <param name="iconFallbackText">Letters drawn over the chip when there is no mark.</param>
        /// <param name="width">Button width (305 PC / 260 mobile).</param>
        /// <param name="height">Button height (48 PC / 44 mobile).</param>
        /// <param name="onClick">Raised on click, tap, or Enter/Space while focused.</param>
        /// <param name="rightToLeft">Puts the mark on the right and the label, right-aligned,
        /// on its left for a right-to-left language; the mark itself is never mirrored.</param>
        public void Configure(
            string label, Sprite iconSprite, Color iconTint, string iconFallbackText,
            float width, float height, Action onClick, bool rightToLeft = false)
        {
            gameObject.name = $"LoginOption_{label}";
            AxylUICursor.Attach(m_button);
            AxylUIDirection.Apply(GetComponentInChildren<HorizontalLayoutGroup>(), rightToLeft);
            AxylUIDirection.Apply(m_label, rightToLeft);
            m_label.text = label;
            AxylUITypography.SetLineHeight(m_label, AxylUITheme.LoginButtonLineHeight);

            // A label too long for one line wraps (up to two lines) and the button keeps its
            // width, growing only in height; icon and text stay vertically centered.
            float labelWidth = width - AxylUITheme.ButtonPaddingH * 2f
                - AxylUITheme.ButtonIconSize - AxylUITheme.ButtonIconTextGap;
            // Two lines at most: a longer label is invalid input and shows its first two.
            m_label.maxVisibleLines = 2;
            float oneLine = m_label.GetPreferredValues("A", labelWidth, 0f).y;
            float needed = m_label.GetPreferredValues(label, labelWidth, 0f).y;
            if (needed > oneLine + 0.5f)
            {
                height += Mathf.Min(needed - oneLine, oneLine);
            }

            m_rect.sizeDelta = new Vector2(width, height);
            AxylUIFocusRing.Attach(m_button, height / 2f); // the pill's final radius

            // Rescale the baked pill to this height's radius. The border sprite carries
            // radius+1px of flat pad; the fill sits 1px inside it.
            float baked = AxylUITheme.BakedPillRadius;
            float radius = height / 2f;
            m_borderImage.pixelsPerUnitMultiplier = (baked + 1f) / (radius + 1f);
            // Exact only while ButtonBorderWidth == 1 (the fill inset cancels against the pad).
            m_fillImage.pixelsPerUnitMultiplier = baked / radius;

            if (iconSprite != null)
            {
                m_icon.sprite = iconSprite;
                // Bitmap marks render whole (aspect kept); the chip sprite is 9-sliced.
                m_icon.type = iconSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                m_icon.color = iconTint;
            }

            bool glyph = !string.IsNullOrEmpty(iconFallbackText);
            m_glyph.gameObject.SetActive(glyph);
            if (glyph)
            {
                m_glyph.text = iconFallbackText;
                m_glyph.fontSize = iconFallbackText.Length > 1 ? 8f : 10f;
                m_glyph.color = iconSprite != null ? Color.white : AxylUITheme.TextMuted;
            }

            if (onClick != null)
            {
                m_button.onClick.AddListener(() => onClick());
            }
        }

        private void Awake()
        {
            m_rect = (RectTransform)transform;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_hovered = true;
            Refresh();
        }

        // Also ends Pressed when a touch is cancelled or released outside the button.
        public void OnPointerExit(PointerEventData eventData)
        {
            m_hovered = false;
            m_pressed = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_pressed = true;
            Refresh();
        }

        // A mouse release lands back on Hover (the pointer is still over the button); a touch
        // release is followed by PointerExit, which clears Hover down to Default — the
        // intended state flow for PC and Mobile.
        public void OnPointerUp(PointerEventData eventData)
        {
            m_pressed = false;
            Refresh();
        }

        // The one place the states are decided:
        //   Default  — Surface fill, Border border
        //   Hover    — StateHoverBg fill, Primary border, 100ms          (pointer only)
        //   Pressed  — StatePressedBg fill, Primary border, 80ms (no scale: the size
        //              never changes with state)
        //   Focus    — identical to Default (the keyboard focus ring is a separate child,
        //              see AxylUIFocusRing)
        private void Refresh()
        {
            Color fill = AxylUITheme.Surface;
            Color border = AxylUITheme.Border;
            float seconds = AxylUITheme.StateTransitionSeconds;

            if (m_hovered)
            {
                fill = AxylUITheme.StateHoverBg;
                border = AxylUITheme.Primary;
            }

            if (m_pressed)
            {
                fill = AxylUITheme.StatePressedBg;
                border = AxylUITheme.Primary;
                seconds = AxylUITheme.StatePressedTransitionSeconds;
            }

            if (m_transition != null)
            {
                StopCoroutine(m_transition);
            }

            if (isActiveAndEnabled)
            {
                m_transition = StartCoroutine(TransitionTo(fill, border, seconds));
            }
        }

        private IEnumerator TransitionTo(Color fill, Color border, float seconds)
        {
            Color fill0 = m_fillImage.color;
            Color border0 = m_borderImage.color;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                m_fillImage.color = Color.Lerp(fill0, fill, k);
                m_borderImage.color = Color.Lerp(border0, border, k);
                yield return null;
            }

            m_fillImage.color = fill;
            m_borderImage.color = border;
            m_transition = null;
        }
    }
}
