// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// Keeps keyboard and controller focus inside an open screen, the way a modal should:
    /// uGUI navigation is wired as an explicit ring over the screen's controls (so arrow
    /// keys and a gamepad wrap around instead of wandering into the app's UI behind the
    /// dim), and a selection that leaves the screen — cleared by a tap on empty space, or
    /// moved by the app — comes back to the last control that held it.
    /// </summary>
    /// <remarks>Install once per screen with <see cref="Install"/>; a screen that rebuilds
    /// itself calls <see cref="SetOrder"/> again with the new controls, and one that opens a
    /// nested card swaps the ring in while the card is up.</remarks>
    public sealed class AxylUIFocusTrap : MonoBehaviour
    {
        private IReadOnlyList<Selectable> m_order;
        private GameObject m_lastInside;

        /// <summary>Puts the trap on the screen's canvas over <paramref name="order"/>.</summary>
        public static AxylUIFocusTrap Install(GameObject canvasGo, IReadOnlyList<Selectable> order)
        {
            var trap = canvasGo.GetComponent<AxylUIFocusTrap>() ?? canvasGo.AddComponent<AxylUIFocusTrap>();
            trap.SetOrder(order);
            return trap;
        }

        /// <summary>The controls focus may rest on, in keyboard order. Explicit navigation is
        /// wired along this list as a ring; the same list object may be mutated by the
        /// screen and passed again after a rebuild.</summary>
        public void SetOrder(IReadOnlyList<Selectable> order)
        {
            m_order = order;
            m_lastInside = null;
            WireRing();
        }

        /// <summary>Lets focus leave — call before handing the selection back to the app on
        /// Dismiss, since the screen is destroyed only at the end of the frame.</summary>
        public void Release()
        {
            enabled = false;
        }

        // The ring: each control's up/left go to the previous, down/right to the next,
        // wrapping at both ends. Explicit navigation lands on a wired control whether or
        // not it is interactable — uGUI's Navigate() checks IsActive() only — so a screen
        // leaves a control that stays disabled for its whole life out of the order
        // (Account's disabled rows), while one that gets enabled later (a form's submit)
        // stays wired and reachable.
        private void WireRing()
        {
            if (m_order == null)
            {
                return;
            }

            int count = m_order.Count;
            for (int i = 0; i < count; i++)
            {
                var current = m_order[i];
                if (current == null)
                {
                    continue;
                }

                var previous = m_order[(i - 1 + count) % count];
                var next = m_order[(i + 1) % count];
                var navigation = current.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = previous;
                navigation.selectOnLeft = previous;
                navigation.selectOnDown = next;
                navigation.selectOnRight = next;
                navigation.wrapAround = true;
                current.navigation = navigation;
            }
        }

        // LateUpdate: whatever moved the selection this frame has finished; a selection
        // outside the screen (or none) is pulled back in.
        private void LateUpdate()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || m_order == null || m_order.Count == 0)
            {
                return;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
            {
                m_lastInside = selected;
                return;
            }

            var target = m_lastInside != null && m_lastInside.activeInHierarchy
                ? m_lastInside
                : FirstInteractable();
            if (target != null)
            {
                SelectWithoutActivating(eventSystem, target);
            }
        }

        // Pulling the selection back is bookkeeping, not the player choosing the control:
        // selecting an input field the normal way would also activate it (the soft keyboard
        // rises, PC selects the text), so the trap selects without activation. A tap, click
        // or Tab onto the field still activates it as always.
        // Shared with the form screens: restoring focus after a rebuild must not activate
        // a field the player was not editing.
        internal static void SelectWithoutActivating(EventSystem eventSystem, GameObject target)
        {
            var input = target.GetComponent<TMP_InputField>();
            if (input == null)
            {
                eventSystem.SetSelectedGameObject(target);
                return;
            }

            bool activate = input.shouldActivateOnSelect;
            input.shouldActivateOnSelect = false;
            eventSystem.SetSelectedGameObject(target);
            input.shouldActivateOnSelect = activate;
        }

        private GameObject FirstInteractable()
        {
            for (int i = 0; i < m_order.Count; i++)
            {
                var control = m_order[i];
                if (control != null && control.isActiveAndEnabled && control.IsInteractable())
                {
                    return control.gameObject;
                }
            }

            return null;
        }
    }
}
