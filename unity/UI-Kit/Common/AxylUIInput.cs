// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The Kit's tiny input shim: screens poll it instead of talking to an input backend
    /// directly, so the same screen code compiles against whichever backend the project
    /// enables (legacy Input Manager, the Input System package, or both).
    /// </summary>
    internal static class AxylUIInput
    {
        /// <summary>True on the frame Esc is pressed. On Android the hardware back button
        /// arrives as Escape, so a screen's Esc-to-close doubles as back-to-close.</summary>
        public static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM && UIKIT_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                return true;
            }
#endif
            return false;
        }

        /// <summary>True on the frame Tab is pressed; <paramref name="backward"/> reports a
        /// held Shift (Shift+Tab walks the focus order the other way).</summary>
        public static bool TabPressed(out bool backward)
        {
            backward = false;
#if ENABLE_INPUT_SYSTEM && UIKIT_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                backward = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                backward = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                return true;
            }
#endif
            return false;
        }

        /// <summary>True on the frame Enter (Return or the keypad Enter) is pressed while a
        /// text field holds the focus — the keyboard way to fire a form's primary action, the
        /// way Tab walks its fields. Enter on a focused button or toggle is left to uGUI's own
        /// Submit event, so one key press never fires two actions.</summary>
        public static bool SubmitPressed()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null || selected.GetComponent<TMPro.TMP_InputField>() == null)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM && UIKIT_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                return true;
            }
#endif
            return false;
        }

        /// <summary>
        /// Moves keyboard focus along <paramref name="order"/> — the screen's Tab order.
        /// Steps from the currently selected entry (or its child, so an input's caret still
        /// counts as the input), skipping entries that are gone or not selectable; lands on
        /// the first entry when nothing on the list holds focus yet.
        /// </summary>
        public static void MoveFocus(
            System.Collections.Generic.IReadOnlyList<UnityEngine.UI.Selectable> order,
            bool backward)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null || order == null || order.Count == 0)
            {
                return;
            }

            int current = -1;
            var selected = eventSystem.currentSelectedGameObject;
            for (int i = 0; i < order.Count && current < 0; i++)
            {
                if (order[i] != null && selected != null
                    && selected.transform.IsChildOf(order[i].transform))
                {
                    current = i;
                }
            }

            int step = backward ? -1 : 1;
            for (int hop = 1; hop <= order.Count; hop++)
            {
                int index = current < 0
                    ? (backward ? order.Count - hop : hop - 1)
                    : (current + step * hop + order.Count * hop) % order.Count;
                var next = order[index];
                if (next != null && next.isActiveAndEnabled && next.IsInteractable())
                {
                    // Tab reaches the ring as plain event data, the same shape a screen's
                    // opening focus has, so say outright that this one is the player
                    // navigating: from here the rings light.
                    AxylUIFocusRing.NoteNavigation();
                    eventSystem.SetSelectedGameObject(next.gameObject);
                    return;
                }
            }
        }
    }
}
