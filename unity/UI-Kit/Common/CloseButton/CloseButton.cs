// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The shared close control, authored as CloseButton.prefab: a bare 18px light X in a
    /// 48px transparent hit area, anchored top-right; the round dark backdrop appears only on
    /// Hover / Pressed. The transparent hit padding keeps the sprite size and the interaction
    /// area separate.
    /// </summary>
    public sealed class CloseButton : MonoBehaviour
    {
        [SerializeField] private Button m_button;
        [SerializeField] private Image[] m_bars;

        /// <summary>Wires the click. Position and visuals are the prefab's.</summary>
        /// <param name="onClick">Raised when the button is selected.</param>
        /// <param name="iconColor">Optional X color override: the prefab's light X suits the
        /// overlay dim; pass a dark color when the button sits on a light card.</param>
        /// <param name="rightToLeft">Moves the button to the header's top-left corner, where
        /// a right-to-left layout puts its trailing end.</param>
        public void Configure(Action onClick, Color? iconColor = null, bool rightToLeft = false)
        {
            AxylUICursor.Attach(m_button);
            AxylUIFocusRing.Attach(m_button, AxylUITheme.CloseHitSize / 2f);
            if (rightToLeft)
            {
                var rect = (RectTransform)transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(-rect.anchoredPosition.x, rect.anchoredPosition.y);
            }

            if (onClick != null)
            {
                m_button.onClick.AddListener(() => onClick());
            }

            if (iconColor.HasValue)
            {
                foreach (var bar in m_bars)
                {
                    bar.color = iconColor.Value;
                }
            }
        }
    }
}
