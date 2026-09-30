// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>The shared toast's status variants (Semantic color on the leading icon only,
    /// never the toast background).</summary>
    public enum AxylUIToastVariant
    {
        /// <summary>Plain status text, no leading icon.</summary>
        Default,

        /// <summary>A completed action; SemanticUp check icon.</summary>
        Success,

        /// <summary>A failed action; SemanticDown alert icon.</summary>
        Error,
    }

    /// <summary>A toast in flight, as <see cref="FailureToast.Capture"/> reports it.</summary>
    public readonly struct AxylUIToastState
    {
        public AxylUIToastState(string message, AxylUIToastVariant variant, float remainingSeconds)
        {
            Message = message;
            Variant = variant;
            RemainingSeconds = remainingSeconds;
        }

        public string Message { get; }

        public AxylUIToastVariant Variant { get; }

        public float RemainingSeconds { get; }
    }

    /// <summary>
    /// The shared toast, authored as FailureToast.prefab. It lives on its own overlay
    /// layer so showing it never moves the screen's layout. One instance per screen; a new
    /// message replaces the current one and restarts the timer. The message text comes from
    /// the app — the Kit never invents copy (error wording is the app's mapping).
    /// </summary>
    public sealed class FailureToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup m_group;
        [SerializeField] private TMP_Text m_label;
        [SerializeField] private Image m_iconBg;
        [SerializeField] private TMP_Text m_mark;

        private RectTransform m_rect;
        private float m_shownY;
        private float m_hiddenY;
        private Coroutine m_running;
        private string m_message;
        private AxylUIToastVariant m_variant;
        // Unscaled time the hold ends; NaN while the enter fade has not measured the hold
        // yet, and unset (null) while no toast shows.
        private float? m_holdUntil;

        /// <summary>Anchors a prefab instance per the screen layout.</summary>
        /// <param name="topEdge">True for the Landscape top-center placement.</param>
        /// <param name="wide">True on PC, where the toast may grow to 560 instead of 360.</param>
        /// <param name="availableWidth">The safe-area width in the layout's reference units
        /// (<see cref="AxylUIScreen.SafeSize"/>); the parent rect still holds raw pixels at
        /// build time, before the scaler has applied its scale.</param>
        /// <param name="rightToLeft">Mirrors the icon + text row and the text alignment for a
        /// right-to-left language.</param>
        public void Configure(bool topEdge, bool wide, float availableWidth, bool rightToLeft = false)
        {
            m_rect = (RectTransform)transform;
            AxylUIDirection.Apply(GetComponent<HorizontalLayoutGroup>(), rightToLeft);
            AxylUIDirection.Apply(m_label, rightToLeft);
            float edge = topEdge ? AxylUITheme.ToastEdgeOffsetTop : AxylUITheme.ToastEdgeOffsetBottom;
            m_rect.anchorMin = m_rect.anchorMax = topEdge ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            m_rect.pivot = topEdge ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            m_shownY = topEdge ? -edge : edge;
            m_hiddenY = m_shownY + (topEdge ? 10f : -10f);
            m_rect.anchoredPosition = new Vector2(0f, m_hiddenY);

            float width = Mathf.Min(
                wide ? AxylUITheme.ToastMaxWidthPc : AxylUITheme.ToastMaxWidth,
                availableWidth - AxylUITheme.ToastSideMargin * 2f);
            m_rect.sizeDelta = new Vector2(width, 0f);
            m_group.alpha = 0f;
        }

        /// <summary>Shows <paramref name="message"/>, replacing any current toast.</summary>
        public void Show(string message, AxylUIToastVariant variant = AxylUIToastVariant.Error)
        {
            Begin(message, variant);
            m_holdUntil = float.NaN;
            m_running = StartCoroutine(Run(enter: true, hold: null));
        }

        /// <summary>The toast in flight — its message, variant and remaining hold — or null
        /// when none shows or it is already leaving. A screen rebuilding itself for a new
        /// screen size hands this to <see cref="Restore"/> on its new toast, so a rotation
        /// never swallows a message the player was reading.</summary>
        public AxylUIToastState? Capture()
        {
            if (m_holdUntil == null)
            {
                return null;
            }

            // Still entering: the whole hold is ahead, sized by the line count as Run will.
            float remaining = float.IsNaN(m_holdUntil.Value)
                ? HoldSeconds()
                : m_holdUntil.Value - Time.unscaledTime;
            if (remaining <= 0f)
            {
                return null;
            }

            return new AxylUIToastState(m_message, m_variant, remaining);
        }

        /// <summary>Shows a captured toast where it left off: already visible, holding for
        /// the remaining seconds, then leaving as usual.</summary>
        public void Restore(AxylUIToastState state)
        {
            Begin(state.Message, state.Variant);
            m_group.alpha = 1f;
            m_rect.anchoredPosition = new Vector2(0f, m_shownY);
            m_running = StartCoroutine(Run(enter: false, hold: state.RemainingSeconds));
        }

        private void Begin(string message, AxylUIToastVariant variant)
        {
            m_message = message;
            m_variant = variant;
            m_label.text = message;
            ApplyVariant(variant);
            if (m_running != null)
            {
                StopCoroutine(m_running);
            }
        }

        private void ApplyVariant(AxylUIToastVariant variant)
        {
            m_iconBg.gameObject.SetActive(variant != AxylUIToastVariant.Default);
            var semantic = variant == AxylUIToastVariant.Success
                ? AxylUITheme.SemanticUp
                : AxylUITheme.SemanticDown;
            m_iconBg.color = new Color(semantic.r, semantic.g, semantic.b, 0.18f);
            m_mark.color = semantic;
            m_mark.text = variant == AxylUIToastVariant.Success ? "✓" : "!";
        }

        // A toast's life: enter (unless restored mid-hold), hold, leave. The hold is
        // measured after the enter fade, when layout has settled and the line count is
        // known — two-line messages hold longer.
        private IEnumerator Run(bool enter, float? hold)
        {
            if (enter)
            {
                yield return Fade(visible: true);
            }

            hold ??= HoldSeconds();

            m_holdUntil = Time.unscaledTime + hold.Value;
            while (Time.unscaledTime < m_holdUntil.Value)
            {
                yield return null;
            }

            m_holdUntil = null;
            yield return Fade(visible: false);
            m_running = null;
        }

        // Two-line messages hold longer; the line count is read from the current layout.
        private float HoldSeconds()
        {
            bool twoLines = m_label.textInfo != null && m_label.textInfo.lineCount >= 2;
            return twoLines ? AxylUITheme.ToastShowSecondsTwoLines : AxylUITheme.ToastShowSeconds;
        }

        private IEnumerator Fade(bool visible)
        {
            float from = m_group.alpha;
            float to = visible ? 1f : 0f;
            float fromY = m_rect.anchoredPosition.y;
            float toY = visible ? m_shownY : m_hiddenY;
            for (float t = 0f; t < AxylUITheme.ToastEnterExitSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / AxylUITheme.ToastEnterExitSeconds;
                m_group.alpha = Mathf.Lerp(from, to, k);
                m_rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(fromY, toY, k));
                yield return null;
            }

            m_group.alpha = to;
            m_rect.anchoredPosition = new Vector2(0f, toY);
        }
    }
}
