// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The focus indicator for keyboard and controller navigation: the focused control
    /// shows a ring just outside its edge, and the ring goes with the focus.
    /// A control focused by a pointer click shows none (the pointer is the indicator), and a
    /// touch-only device shows none at all — only a selection that arrived through
    /// navigation (arrow keys, a gamepad, Tab) lights it. The ring is a separate child
    /// outside the control's rect, so it changes no layout size, radius or text position.
    /// </summary>
    public sealed class AxylUIFocusRing : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        // Whether the player has driven the focus from a keyboard or controller yet.
        // Opening a screen places the focus on its first control so the keyboard path has
        // somewhere to start, and that selection arrives as plain event data — the same
        // shape Tab produces. Without this latch the ring lit on a control nobody had
        // navigated to, the moment a screen opened. It stays on for the session once the
        // player does navigate, which is what keeps the ring visible for someone working
        // from the keyboard.
        private static bool s_navigationSeen;

        private GameObject m_ring;

        /// <summary>Records that the focus moved by keyboard or controller, so the rings
        /// light from here on. Called by <see cref="AxylUIInput.MoveFocus"/>; an arrow or
        /// gamepad move reports itself through its own event data.</summary>
        public static void NoteNavigation() => s_navigationSeen = true;

        /// <summary>Gives <paramref name="control"/> a focus ring shaped to its corner
        /// radius (a pill passes half its height; a circle its radius). The ring wraps
        /// <paramref name="host"/> when given — a control whose visible box is an ancestor,
        /// like the text field inside an input group — and the control itself otherwise.</summary>
        public static AxylUIFocusRing Attach(
            Selectable control, float cornerRadius, RectTransform host = null)
        {
            if (control == null)
            {
                return null;
            }

            var ring = control.GetComponent<AxylUIFocusRing>()
                ?? control.gameObject.AddComponent<AxylUIFocusRing>();
            ring.Build(cornerRadius, host != null ? host : (RectTransform)control.transform);
            return ring;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (m_ring == null)
            {
                return;
            }

            // A click (or tap) carries pointer data and shows no ring — the pointer is the
            // indicator. An arrow or gamepad move carries axis data and always shows one.
            // Everything else arrives as plain data: both Tab and the focus a screen places
            // when it opens look identical here, so the ring waits for NoteNavigation to
            // say the player has actually used the keyboard.
            bool pointer = eventData is PointerEventData;
            bool navigated = eventData is AxisEventData
                || (!pointer && !Application.isMobilePlatform && s_navigationSeen);
            m_ring.SetActive(navigated);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (m_ring != null)
            {
                m_ring.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (m_ring != null)
            {
                m_ring.SetActive(false);
            }
        }

        // The ring may live under a host that outlives the control (an input group's box
        // around its text field); it goes with the control either way.
        private void OnDestroy()
        {
            if (m_ring != null)
            {
                Destroy(m_ring);
            }
        }

        // Builds the ring once and reshapes it afterwards, so a widget that sizes itself in
        // two steps (Configure, then the grid's final row height) does not churn objects.
        private void Build(float cornerRadius, RectTransform host)
        {
            float offset = AxylUITheme.FocusRingOffset;
            var sprite = AxylUIRuntimeAssets.RoundedRing(
                cornerRadius + offset, AxylUITheme.FocusRingWidth);
            if (m_ring != null)
            {
                m_ring.GetComponent<Image>().sprite = sprite;
                return;
            }

            m_ring = new GameObject("FocusRing", typeof(RectTransform));
            var rect = (RectTransform)m_ring.transform;
            rect.SetParent(host, false);
            rect.SetAsLastSibling(); // above the control's own graphics
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-offset, -offset);
            rect.offsetMax = new Vector2(offset, offset);
            m_ring.AddComponent<LayoutElement>().ignoreLayout = true;

            var image = m_ring.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = AxylUITheme.FocusRingColor;
            image.raycastTarget = false;
            m_ring.SetActive(false);
        }
    }
}
