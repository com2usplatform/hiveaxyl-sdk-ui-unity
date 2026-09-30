// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;
using UnityEngine.EventSystems;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The pointer cursor over interactive controls on PC: hovering a button, row, link or
    /// toggle shows a pointing hand; leaving it restores the system arrow. Unity
    /// has no runtime access to the platform's own hand cursor (<see cref="Cursor.SetCursor"/>
    /// takes a texture or null), so the hand is the Kit's cursor asset under
    /// Common/Cursor/Resources/UIKit/Cursor/hand.png — replace the file to restyle it.
    /// Nothing happens on a touch platform, which has no cursor.
    /// </summary>
    public sealed class AxylUICursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // 32×32, imported as a Cursor texture by AxylUIBaker; the hotspot is the fingertip.
        private const string k_HandPath = "UIKit/Cursor/hand";
        private static readonly Vector2 s_handHotspot = new Vector2(13f, 6f);

        private static Texture2D s_hand;
        private static bool s_handLooked;
        private static int s_hovering;

        /// <summary>Gives <paramref name="control"/> the pointer cursor on hover.</summary>
        public static void Attach(Component control)
        {
            if (control == null || Application.isMobilePlatform
                || control.GetComponent<AxylUICursor>() != null)
            {
                return;
            }

            control.gameObject.AddComponent<AxylUICursor>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var hand = Hand();
            if (hand == null)
            {
                return;
            }

            s_hovering++;
            Cursor.SetCursor(hand, s_handHotspot, CursorMode.Auto);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Leave();
        }

        private void OnDisable()
        {
            Leave(); // a control destroyed under the pointer must not leave the hand behind
        }

        private void Leave()
        {
            if (s_hovering > 0 && --s_hovering == 0)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        // The cursor texture, looked up once; a Kit copied without it keeps the system arrow.
        private static Texture2D Hand()
        {
            if (!s_handLooked)
            {
                s_handLooked = true;
                s_hand = Resources.Load<Texture2D>(k_HandPath);
                if (s_hand == null)
                {
                    Debug.LogWarning(
                        $"[UIKit] Cursor texture missing at Resources/{k_HandPath}; " +
                        "keeping the system arrow over controls.");
                }
            }

            return s_hand;
        }
    }
}
