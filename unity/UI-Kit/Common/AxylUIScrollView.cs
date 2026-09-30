// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The Kit's one vertical scroll view: the <c>Scroll View › Viewport › Content</c>
    /// structure, built around a column that turned out taller than the room it has. The
    /// focused control is kept in view as keyboard or controller navigation moves through the
    /// content (see <see cref="AxylUIFocusScroll"/>).
    /// </summary>
    public static class AxylUIScrollView
    {
        /// <summary>
        /// Moves <paramref name="content"/> into a new scroll view that takes its place among
        /// its siblings. The content keeps its width and hangs from the viewport's top edge;
        /// its height follows its children. The caller sizes the returned scroll view (a
        /// <see cref="LayoutElement"/> inside a layout group, or anchors on a free root).
        /// </summary>
        public static RectTransform Wrap(RectTransform content)
        {
            var parent = (RectTransform)content.parent;
            int index = content.GetSiblingIndex();
            float height = content.rect.height;

            var go = new GameObject("ScrollView", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(index);

            // The invisible image catches drags in the gaps between the content's controls.
            var backdrop = go.AddComponent<Image>();
            backdrop.color = Color.clear;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(rect, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            content.SetParent(viewport, false);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
            if (content.GetComponent<ContentSizeFitter>() == null)
            {
                content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
            }

            var scroll = go.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = AxylUITheme.ScrollWheelSensitivity;
            go.AddComponent<AxylUIFocusScroll>();
            return rect;
        }
    }

    /// <summary>
    /// Keeps the focused control visible inside its scroll view: when the selection moves to
    /// a descendant of the content that sits above or below the viewport, the content scrolls
    /// just far enough to show it, so navigation never lands on something off screen.
    /// </summary>
    internal sealed class AxylUIFocusScroll : MonoBehaviour
    {
        private ScrollRect m_scroll;
        private GameObject m_lastSelected;

        private void Awake()
        {
            m_scroll = GetComponent<ScrollRect>();
        }

        // LateUpdate: the frame's navigation and layout have settled by then.
        private void LateUpdate()
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == m_lastSelected)
            {
                return;
            }

            m_lastSelected = selected;
            if (selected != null && selected.transform.IsChildOf(m_scroll.content))
            {
                ScrollIntoView((RectTransform)selected.transform);
            }
        }

        private void ScrollIntoView(RectTransform target)
        {
            var viewport = m_scroll.viewport;
            var content = m_scroll.content;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            var view = viewport.rect;

            // Positive: the target pokes out above the viewport; negative: below it.
            float overflow = 0f;
            if (bounds.max.y > view.yMax)
            {
                overflow = bounds.max.y - view.yMax;
            }
            else if (bounds.min.y < view.yMin)
            {
                overflow = bounds.min.y - view.yMin;
            }

            if (Mathf.Approximately(overflow, 0f))
            {
                return;
            }

            // The content hangs from the viewport's top: scrolling down moves it up
            // (a larger anchoredPosition.y), clamped to the scrollable range.
            float range = Mathf.Max(0f, content.rect.height - view.height);
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y - overflow, 0f, range);
            content.anchoredPosition = position;
        }
    }
}
