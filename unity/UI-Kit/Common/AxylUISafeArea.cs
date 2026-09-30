// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;
using UnityEngine.EventSystems;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// Pins its RectTransform to the device safe area: anchors follow
    /// <see cref="Screen.safeArea"/> as fractions of the screen, re-applied whenever the
    /// area changes (a rotation, a resize, an inset change). Screens parent their content
    /// here so nothing interactive lands under a notch, a cutout, or the home indicator.
    /// </summary>
    /// <remarks>
    /// The same root keeps the focused input clear of the soft keyboard: while the keyboard
    /// is up and covers the selected field, the whole content slides up by the overlap and
    /// slides back when the keyboard hides, so the selected input stays visible.
    /// <see cref="Screen.safeArea"/> knows nothing of the
    /// keyboard, so this is measured from <see cref="TouchScreenKeyboard.area"/>; on
    /// platforms without a soft keyboard the shift is always zero.
    /// </remarks>
    internal sealed class AxylUISafeArea : MonoBehaviour
    {
        private Rect m_applied;
        private Vector2Int m_screen;
        private float m_keyboardShift;
        private int m_settleFrames;

        private void Update()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != m_applied || screen != m_screen)
            {
                m_applied = Screen.safeArea;
                m_screen = screen;
                m_keyboardShift = 0f;
                // The layout for the new screen rebuilds over the next frames. Measuring a
                // field that still sits where the old orientation put it — against a
                // keyboard that has not resized yet either — asks for a lift the new screen
                // never needed, so let both settle before measuring again.
                m_settleFrames = 2;
                Apply((RectTransform)transform);
            }
        }

        // LateUpdate: selection and layout for the frame are final; the shift is measured
        // against where the field actually is.
        private void LateUpdate()
        {
            if (m_settleFrames > 0)
            {
                m_settleFrames--;
                return;
            }

            float shift = KeyboardShift();
            if (!Mathf.Approximately(shift, m_keyboardShift))
            {
                m_keyboardShift = shift;
                Apply((RectTransform)transform, shift);
            }
        }

        /// <summary>Anchors <paramref name="rect"/> to the current safe area, lifted by
        /// <paramref name="shift"/> canvas units (zero: flush with the area).</summary>
        public static void Apply(RectTransform rect, float shift = 0f)
        {
            var safe = Screen.safeArea;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = new Vector2(0f, shift);
            rect.offsetMax = new Vector2(0f, shift);
        }

        // How far up the content must sit for the selected field to clear the keyboard.
        // Measured fresh every frame against where the field rests with no lift applied, so
        // the answer follows the current screen, keyboard and layout — a rotation with the
        // keyboard up recomputes instead of keeping a lift that belonged to the old
        // orientation. Zero once the keyboard is gone, nothing inside is selected, or the
        // field already clears the keyboard on its own.
        private float KeyboardShift()
        {
            if (!TouchScreenKeyboard.visible)
            {
                return 0f;
            }

            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null || !selected.transform.IsChildOf(transform))
            {
                return 0f;
            }

            var keyboard = TouchScreenKeyboard.area;
            // The keyboard sits against the bottom of the screen and covers its own height,
            // so its top edge is that height above the bottom — the same origin the field is
            // measured in below. (Subtracting the height from the screen instead reads the
            // edge from the top and asks for a lift of most of a screen.)
            float keyboardTopPx = keyboard.height;
            if (keyboardTopPx <= 0f || keyboardTopPx >= Screen.height)
            {
                return m_keyboardShift; // no usable keyboard rect (some platforms report none)
            }

            // The soft keyboard spans the width of the screen, so a rect that no longer does
            // is the one the previous orientation left behind: a landscape keyboard measured
            // against a portrait screen reads as covering far more than it does. Hold the
            // current lift until the keyboard reports itself on this screen.
            if (Mathf.Abs(keyboard.width - Screen.width) > 1f)
            {
                return m_keyboardShift;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.scaleFactor <= 0f)
            {
                return 0f;
            }

            var corners = new Vector3[4];
            ((RectTransform)selected.transform).GetWorldCorners(corners);
            float fieldBottomPx = RectTransformUtility
                .WorldToScreenPoint(canvas.worldCamera, corners[0]).y;
            // The lift already applied is part of that measurement; take it back out so the
            // covered amount is an absolute quantity and not a delta on top of itself. It is
            // read off the root rather than remembered, so a lift that something else reset
            // is not subtracted a second time.
            float appliedShift = ((RectTransform)transform).offsetMin.y;
            float restingBottomPx = fieldBottomPx - appliedShift * canvas.scaleFactor;
            float coveredPx = keyboardTopPx + AxylUITheme.SideMarginMobile * canvas.scaleFactor
                - restingBottomPx;
            if (coveredPx <= 0f)
            {
                return 0f;
            }

            // A stale keyboard or screen rect (both change a frame apart on a rotation) can
            // ask for more than the screen is tall; the content never leaves the safe area.
            float maxShift = Screen.safeArea.height / canvas.scaleFactor;
            return Mathf.Min(coveredPx / canvas.scaleFactor, maxShift);
        }
    }
}
