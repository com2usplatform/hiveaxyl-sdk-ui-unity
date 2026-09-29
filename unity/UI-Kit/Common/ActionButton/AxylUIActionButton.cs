// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>The shared action-button styles.</summary>
    public enum AxylUIActionStyle
    {
        /// <summary>The main action: filled Primary, light label.</summary>
        Primary,

        /// <summary>A supporting action: Surface Strong fill, primary-text label.</summary>
        Secondary,

        /// <summary>A destructive action: outlined in the down/danger color.</summary>
        Destructive,
    }

    /// <summary>
    /// The shared pill action button, authored as AxylUIActionButton.prefab (repeated UI ships
    /// as a prefab). Owns the Primary / Destructive looks and their Hover / Pressed states;
    /// raises the click and knows nothing about the SDK. Screens size an instance and call
    /// <see cref="Configure"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>How to customize.</b> Structure and resting look live in the prefab; every
    /// color, size and duration reads from <see cref="AxylUITheme"/>. The state look itself
    /// is decided in one place, <see cref="Refresh"/>. Focus tints nothing on the button
    /// itself — keyboard or controller focus shows the shared <see cref="AxylUIFocusRing"/>
    /// outside it.</para>
    /// </remarks>
    public sealed class AxylUIActionButton :
        MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image m_borderImage;
        [SerializeField] private Image m_fillImage;
        [SerializeField] private TMP_Text m_label;
        [SerializeField] private Button m_button;

        private RectTransform m_rect;
        private Coroutine m_transition;

        private AxylUIActionStyle m_style;
        private bool m_interactable = true;
        private bool m_hovered;
        private bool m_pressed;

        /// <summary>Fills a prefab instance and wires the click.</summary>
        /// <param name="label">The button text, already localized by the app.</param>
        /// <param name="style">Primary or Destructive.</param>
        /// <param name="onClick">Raised on click, tap, or Enter/Space while focused.</param>
        /// <param name="rightToLeft">Renders the label right-to-left; the centered
        /// alignment stays.</param>
        public void Configure(
            string label, AxylUIActionStyle style, Action onClick, bool rightToLeft = false)
        {
            gameObject.name = $"AxylUIAction_{label}";
            AxylUICursor.Attach(m_button);
            AxylUIFocusRing.Attach(m_button, AxylUITheme.ActionButtonHeight / 2f);
            AxylUIDirection.Apply(m_label, rightToLeft);
            m_label.text = label;
            AxylUITypography.SetLineHeight(m_label, AxylUITheme.ButtonLineHeight);
            m_style = style;

            float baked = AxylUITheme.BakedPillRadius;
            float radius = AxylUITheme.ActionButtonHeight / 2f;
            m_borderImage.pixelsPerUnitMultiplier = (baked + 1f) / (radius + 1f);
            m_fillImage.pixelsPerUnitMultiplier = baked / radius;

            if (onClick != null)
            {
                m_button.onClick.AddListener(() => onClick());
            }

            ApplyState(instant: true);
        }

        /// <summary>Enables or disables the action; disabled renders muted and swallows
        /// clicks — screens use it to gate a submit on validation or an in-flight call.</summary>
        public void SetInteractable(bool interactable)
        {
            m_interactable = interactable;
            m_button.interactable = interactable;
            if (!interactable)
            {
                m_hovered = false;
                m_pressed = false;
            }

            ApplyState(instant: true);
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
        //   Primary      — Primary fill, light label; Hover/Pressed darken to PrimaryActive
        //   Secondary    — Surface Strong fill, primary-text label; Hover/Pressed darken the fill
        //   Destructive  — Surface fill, danger border + label; Hover/Pressed tint the fill
        //   Pressed      — additionally 0.98 scale
        //   Disabled     — muted fill and label, no state reactions
        //   Focus        — identical to the resting state (the keyboard focus ring is a
        //                  separate child, see AxylUIFocusRing)
        private void Refresh(out Color fill, out Color border, out Color labelColor,
            out float scale, out float seconds)
        {
            scale = m_pressed ? AxylUITheme.StatePressedScale : 1f;
            seconds = m_pressed
                ? AxylUITheme.StatePressedTransitionSeconds
                : AxylUITheme.StateTransitionSeconds;

            if (m_style == AxylUIActionStyle.Secondary)
            {
                if (!m_interactable)
                {
                    fill = AxylUITheme.SurfaceStrong;
                    border = fill;
                    labelColor = AxylUITheme.TextDisabled;
                    return;
                }

                fill = m_pressed
                    ? Color.Lerp(AxylUITheme.SurfaceStrong, Color.black, 0.08f)
                    : m_hovered
                        ? Color.Lerp(AxylUITheme.SurfaceStrong, Color.black, 0.04f)
                        : AxylUITheme.SurfaceStrong;
                border = fill;
                labelColor = AxylUITheme.TextPrimary;
                return;
            }

            if (m_style == AxylUIActionStyle.Primary)
            {
                if (!m_interactable)
                {
                    fill = AxylUITheme.PrimaryDisabled;
                    border = fill;
                    labelColor = AxylUITheme.TextOnPrimary;
                    return;
                }

                fill = m_hovered || m_pressed ? AxylUITheme.PrimaryActive : AxylUITheme.Primary;
                border = fill;
                labelColor = AxylUITheme.TextOnPrimary;
                return;
            }

            if (!m_interactable)
            {
                fill = AxylUITheme.Surface;
                border = AxylUITheme.BorderSoft;
                labelColor = AxylUITheme.TextDisabled;
                return;
            }

            var danger = AxylUITheme.SemanticDown;
            fill = m_pressed
                ? Color.Lerp(AxylUITheme.Surface, danger, 0.10f)
                : m_hovered
                    ? Color.Lerp(AxylUITheme.Surface, danger, 0.05f)
                    : AxylUITheme.Surface;
            border = danger;
            labelColor = danger;
        }

        private void ApplyState(bool instant)
        {
            Refresh(out var fill, out var border, out var labelColor, out var scale, out var seconds);
            m_label.color = labelColor;

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
