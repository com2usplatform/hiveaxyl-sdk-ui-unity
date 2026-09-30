// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>How a Kit screen lays itself out. Picked from the display when not forced.</summary>
    public enum AxylUIScreenLayout
    {
        /// <summary>1920×1080 reference.</summary>
        Pc,

        /// <summary>375×812 reference.</summary>
        Portrait,

        /// <summary>812×375 reference.</summary>
        Landscape,
    }

    /// <summary>
    /// The screen-level plumbing every Kit screen shares: picking a layout from the display
    /// and creating the overlay canvas that screen builds itself on.
    /// </summary>
    public static class AxylUIScreen
    {
        /// <summary>The layout for the live display — see <see cref="LayoutFor"/>.</summary>
        public static AxylUIScreenLayout DetectLayout() => LayoutFor(SafeAreaPixels());

        /// <summary>The device safe area's size in pixels: what the breakpoint reads, and
        /// what a size watch compares.</summary>
        public static Vector2Int SafeAreaPixels()
        {
            var safe = Screen.safeArea;
            return new Vector2Int(Mathf.RoundToInt(safe.width), Mathf.RoundToInt(safe.height));
        }

        /// <summary>The overlay canvas: Scale With Screen Size against the layout's reference
        /// resolution, above the app's own UI.</summary>
        public static GameObject CreateCanvas(string name, AxylUIScreenLayout layout)
        {
            var go = new GameObject(
                name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = AxylUITheme.CanvasSortingOrder;

            ApplyScaler(go, layout);
            return go;
        }

        /// <summary>Applies the layout's scaler settings — at creation, and again when a
        /// screen-size change (a rotation arrives as one) moves the screen to another
        /// layout's reference.</summary>
        public static void ApplyScaler(GameObject canvasGo, AxylUIScreenLayout layout)
        {
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = layout switch
            {
                AxylUIScreenLayout.Pc => AxylUITheme.ReferenceResolutionPc,
                AxylUIScreenLayout.Portrait => AxylUITheme.ReferenceResolutionPortrait,
                _ => AxylUITheme.ReferenceResolutionLandscape,
            };
            scaler.matchWidthOrHeight = layout == AxylUIScreenLayout.Pc ? 0.5f : 0f;
        }

        /// <summary>
        /// The device safe area's size in the layout's reference units — what screens size
        /// their content against, so nothing lands under a notch, a display cutout, or the
        /// home indicator. Computed with the scaler's own math instead of reading a canvas
        /// rect, because the scaler applies its scale a frame after the canvas is created
        /// and a build-time rect read would still see raw pixels.
        /// </summary>
        public static Vector2 SafeSize(AxylUIScreenLayout layout)
        {
            float scale = ScaleFor(layout);
            var safe = Screen.safeArea;
            return new Vector2(safe.width / scale, safe.height / scale);
        }

        // The scale CanvasScaler settles on for the current screen under ApplyScaler's
        // settings (ScaleWithScreenSize's documented log-lerp of the two axis ratios).
        private static float ScaleFor(AxylUIScreenLayout layout)
        {
            Vector2 reference = layout switch
            {
                AxylUIScreenLayout.Pc => AxylUITheme.ReferenceResolutionPc,
                AxylUIScreenLayout.Portrait => AxylUITheme.ReferenceResolutionPortrait,
                _ => AxylUITheme.ReferenceResolutionLandscape,
            };
            float match = layout == AxylUIScreenLayout.Pc ? 0.5f : 0f;
            float logWidth = Mathf.Log(Screen.width / reference.x, 2f);
            float logHeight = Mathf.Log(Screen.height / reference.y, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, match));
        }

        /// <summary>
        /// The full-canvas child a screen builds its content under, pinned to the device
        /// safe area every frame. Decorative fulls — the backdrop dim — stay on the canvas
        /// itself, which may extend under the notch — decoration may sit outside the safe
        /// area, interactive content and text never do.
        /// </summary>
        public static RectTransform ContentRoot(GameObject canvasGo)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvasGo.transform, false);
            go.AddComponent<AxylUISafeArea>();
            AxylUISafeArea.Apply(rect);
            return rect;
        }

        /// <summary>
        /// The layer the screen's toast lives on: pinned to the safe area like
        /// <see cref="ContentRoot"/>, and kept the canvas's last child so the toast draws
        /// above everything else on the screen — an open confirmation card included, since
        /// the toast layer sits above the modal layer. A screen that adds a layer of its own
        /// later inserts it below this one.
        /// </summary>
        public static RectTransform ToastLayer(GameObject canvasGo)
        {
            var go = new GameObject("ToastLayer", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvasGo.transform, false);
            rect.SetAsLastSibling();
            go.AddComponent<AxylUISafeArea>();
            AxylUISafeArea.Apply(rect);
            return rect;
        }

        /// <summary>True when the screen size changed since <paramref name="last"/> held
        /// it — every change, at once. Fine for a light piece of chrome; a screen that
        /// rebuilds itself should poll <see cref="AxylUIScreenSizeWatch"/> instead, which
        /// waits out a resize in progress. The first call only records the size.</summary>
        public static bool ScreenSizeChanged(ref Vector2Int last)
        {
            var now = new Vector2Int(Screen.width, Screen.height);
            if (last == now)
            {
                return false;
            }

            bool first = last == default;
            last = now;
            return !first;
        }

        /// <summary>
        /// The layout a safe area of <paramref name="safeAreaPixels"/> falls in, by the
        /// breakpoint order: platform first, then the viewport. On a mobile platform a
        /// taller-than-wide viewport is Portrait, a landscape one shorter than the
        /// mobile-landscape height is Landscape (whatever its width says), and a landscape
        /// one with room — a tablet — takes the width breakpoint: PC from the tablet width
        /// up, Portrait's single column below it. On PC the width breakpoint alone decides;
        /// a narrow or taller-than-wide window gets the single column. Widths and heights
        /// are read in density-independent pixels so the thresholds mean the same on every
        /// display. A short PC window (under <see cref="AxylUITheme.PcCompactMaxHeight"/>) is
        /// not a layout of its own: it keeps the PC layout, sizes and column count, and only
        /// its content scrolls inside the safe area with the header and actions fixed — what
        /// every screen does whenever its content is taller than the room, so this method
        /// needs no branch for it.
        /// </summary>
        public static AxylUIScreenLayout LayoutFor(Vector2Int safeAreaPixels)
        {
            float dpScale = Screen.dpi > 0f ? Screen.dpi / 160f : 1f;
            float width = safeAreaPixels.x / dpScale;
            float height = safeAreaPixels.y / dpScale;
            if (width < height)
            {
                return AxylUIScreenLayout.Portrait;
            }

            if (Application.isMobilePlatform && height < AxylUITheme.MobileLandscapeMaxHeight)
            {
                return AxylUIScreenLayout.Landscape;
            }

            return width < AxylUITheme.TabletMinWidth
                ? AxylUIScreenLayout.Portrait
                : AxylUIScreenLayout.Pc;
        }

        /// <summary>Where the screen's scroll view stands (Unity's normalized position:
        /// 1 = top, 0 = bottom), or null when nothing scrolls — read before an in-place
        /// rebuild so the position can be carried over like typed input is.</summary>
        public static float? ScrollPosition(GameObject canvasGo)
        {
            var scroll = canvasGo.GetComponentInChildren<ScrollRect>();
            return scroll != null ? scroll.verticalNormalizedPosition : (float?)null;
        }

        /// <summary>Puts the rebuilt screen's scroll view back where
        /// <see cref="ScrollPosition"/> found it; nothing happens when either side has no
        /// scroll view (the new layout may fit without one).</summary>
        public static void RestoreScrollPosition(GameObject canvasGo, float? position)
        {
            var scroll = canvasGo.GetComponentInChildren<ScrollRect>();
            if (scroll == null || position == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases(); // the new content needs its height first
            scroll.verticalNormalizedPosition = position.Value;
        }
    }

    /// <summary>
    /// Tells a screen when to rebuild for a new screen size: a change of layout
    /// (breakpoint) — a rotation — fires at once, while any other size change (a
    /// window being dragged) fires once the size has held still for
    /// <see cref="AxylUITheme.ResizeSettleSeconds"/>, so a resize in progress never tears
    /// the screen down every frame. Screens poll <see cref="Changed"/> from Update.
    /// </summary>
    public sealed class AxylUIScreenSizeWatch
    {
        private Vector2Int m_applied;
        private Vector2Int m_pending;
        private float m_pendingSince;

        /// <summary>True on the frame the screen should rebuild for the current size. The
        /// first call only records the size.</summary>
        public bool Changed()
        {
            var now = AxylUIScreen.SafeAreaPixels();
            if (m_applied == default)
            {
                m_applied = now;
                return false;
            }

            if (now == m_applied)
            {
                m_pending = default;
                return false;
            }

            if (AxylUIScreen.LayoutFor(now) != AxylUIScreen.LayoutFor(m_applied))
            {
                m_applied = now;
                m_pending = default;
                return true;
            }

            if (now != m_pending)
            {
                m_pending = now;
                m_pendingSince = Time.unscaledTime;
                return false;
            }

            if (Time.unscaledTime - m_pendingSince < AxylUITheme.ResizeSettleSeconds)
            {
                return false;
            }

            m_applied = now;
            m_pending = default;
            return true;
        }
    }
}
