// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UIKIT_ACCESSIBILITY
using UnityEngine.Accessibility;
#endif

namespace Hive.Axyl.UIKit
{
    /// <summary>One control the screen reader can reach: its name, role and target.</summary>
    public readonly struct AxylUIAccessibleControl
    {
        public AxylUIAccessibleControl(
            Selectable target, string label, bool isTextField = false, string description = null)
        {
            Target = target;
            Label = label;
            IsTextField = isTextField;
            Description = description;
        }

        /// <summary>The control; its rect is the node's frame, its click/focus the action.</summary>
        public Selectable Target { get; }

        /// <summary>The accessibility name, already localized by the app — the full text
        /// (never abbreviated), and never repeating a decorative icon.</summary>
        public string Label { get; }

        /// <summary>True for an input; false for a button-like control.</summary>
        public bool IsTextField { get; }

        /// <summary>Extra text the screen reader reads after the name — an input's current
        /// error message, so the fault is announced with the field it belongs to. Null when
        /// there is nothing to add.</summary>
        public string Description { get; }
    }

    /// <summary>
    /// Publishes a screen's controls to the platform screen reader: every control gets a
    /// name (the app's localized string — "Close",
    /// "Show password", the provider's full label), a role, and its on-screen frame, and
    /// activating a node presses the button or focuses the input. Uses Unity's
    /// <c>UnityEngine.Accessibility</c> hierarchy (iOS VoiceOver, Android TalkBack), which
    /// needs the built-in Accessibility module enabled in the project
    /// (<c>com.unity.modules.accessibility</c> in the package manifest); without it, or
    /// where a player build has no assistive support, the component is inert.
    /// </summary>
    public sealed class AxylUIAccessibility : MonoBehaviour
    {
        private readonly List<AxylUIAccessibleControl> m_controls = new List<AxylUIAccessibleControl>();
#if UIKIT_ACCESSIBILITY
        private readonly List<AccessibilityNode> m_nodes = new List<AccessibilityNode>();
        private readonly List<Selectable> m_nodeTargets = new List<Selectable>();
        private AccessibilityHierarchy m_hierarchy;

        // The Kit screens alive right now, most recent last. The app's own hierarchy is
        // saved once, when the first Kit screen publishes, and restored when the last one
        // goes — a per-screen "previous" would hand a dismissed screen's hierarchy back
        // when the app opens screens back to back in one frame.
        private static readonly List<AxylUIAccessibility> s_live = new List<AxylUIAccessibility>();
        private static AccessibilityHierarchy s_appHierarchy;
#endif

        /// <summary>Puts the publisher on the screen's canvas with <paramref name="controls"/>.
        /// Call again after a rebuild with the new controls.</summary>
        public static AxylUIAccessibility Install(
            GameObject canvasGo, IReadOnlyList<AxylUIAccessibleControl> controls)
        {
            var publisher = canvasGo.GetComponent<AxylUIAccessibility>()
                ?? canvasGo.AddComponent<AxylUIAccessibility>();
            publisher.SetControls(controls);
            return publisher;
        }

        public void SetControls(IReadOnlyList<AxylUIAccessibleControl> controls)
        {
            m_controls.Clear();
            foreach (var control in controls)
            {
                if (control.Target == null)
                {
                    continue;
                }

                m_controls.Add(control);
            }

            Publish();
        }

#if UIKIT_ACCESSIBILITY
        // A screen reader switched on while the screen is up publishes it right away.
        private void OnEnable() => AssistiveSupport.screenReaderStatusChanged += OnScreenReaderChanged;

        private void OnDisable() => AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderChanged;

        private void OnScreenReaderChanged(bool enabled)
        {
            if (enabled)
            {
                Publish();
            }
        }
#endif

        private void OnDestroy()
        {
#if UIKIT_ACCESSIBILITY
            s_live.Remove(this);
            if (m_hierarchy != null && AssistiveSupport.activeHierarchy == m_hierarchy)
            {
                AssistiveSupport.activeHierarchy = s_live.Count > 0
                    ? s_live[s_live.Count - 1].m_hierarchy
                    : s_appHierarchy;
            }
#endif
        }

#if UIKIT_ACCESSIBILITY
        // A node keeps the frame its getter returned when it was added; Unity re-asks only
        // on a rotation or a window resize. The Kit's controls also move with a scroll and
        // the keyboard lift, so re-ask the moment any frame no longer matches — this also
        // picks up the screen scaler, which applies a frame after Show() published.
        private void LateUpdate()
        {
            if (m_hierarchy == null || AssistiveSupport.activeHierarchy != m_hierarchy)
            {
                return;
            }

            for (int i = 0; i < m_nodes.Count; i++)
            {
                var target = m_nodeTargets[i];
                if (target != null
                    && m_nodes[i].frame != ScreenFrame((RectTransform)target.transform))
                {
                    m_hierarchy.RefreshNodeFrames();
                    return;
                }
            }
        }
#endif

        private void Publish()
        {
#if UIKIT_ACCESSIBILITY
            if (!AssistiveSupport.isScreenReaderEnabled)
            {
                return;
            }

            // The same controls again (a toggle relabeled, an error appearing on a field):
            // renaming the standing nodes keeps the screen reader's focus, where rebuilding
            // the hierarchy would drop it.
            if (m_hierarchy != null && SameTargets())
            {
                for (int i = 0; i < m_controls.Count; i++)
                {
                    m_nodes[i].label = m_controls[i].Label;
                    m_nodes[i].hint = m_controls[i].Description ?? string.Empty;
                }

                if (AssistiveSupport.activeHierarchy != m_hierarchy)
                {
                    // Unity nulls the active hierarchy when the screen reader goes off, so a
                    // reader switched off and back on needs the hierarchy re-attached — this
                    // path would otherwise leave the screen invisible to it.
                    AssistiveSupport.activeHierarchy = m_hierarchy;
                }
                else
                {
                    AssistiveSupport.notificationDispatcher.SendLayoutChanged();
                }

                return;
            }

            if (m_hierarchy == null)
            {
                if (s_live.Count == 0)
                {
                    s_appHierarchy = AssistiveSupport.activeHierarchy;
                }

                s_live.Add(this);
                m_hierarchy = new AccessibilityHierarchy();
            }
            else
            {
                m_hierarchy.Clear();
            }

            m_nodes.Clear();
            m_nodeTargets.Clear();
            foreach (var control in m_controls)
            {
                var target = control.Target;
                var node = m_hierarchy.AddNode(control.Label);
                node.role = control.IsTextField ? AccessibilityRole.TextField : AccessibilityRole.Button;
                node.hint = control.Description ?? string.Empty;
                node.frameGetter = () =>
                    target != null ? ScreenFrame((RectTransform)target.transform) : Rect.zero;
                m_nodes.Add(node);
                m_nodeTargets.Add(target);
                node.invoked += () =>
                {
                    if (target == null || !target.IsInteractable())
                    {
                        return false;
                    }

                    EventSystem.current?.SetSelectedGameObject(target.gameObject);
                    if (target is Button button)
                    {
                        button.onClick.Invoke();
                    }
                    else if (target is TMP_InputField input)
                    {
                        input.ActivateInputField();
                    }

                    return true;
                };
            }

            AssistiveSupport.activeHierarchy = m_hierarchy;
#endif
        }

#if UIKIT_ACCESSIBILITY
        private bool SameTargets()
        {
            if (m_nodeTargets.Count != m_controls.Count)
            {
                return false;
            }

            for (int i = 0; i < m_controls.Count; i++)
            {
                if (m_nodeTargets[i] != m_controls[i].Target)
                {
                    return false;
                }
            }

            return true;
        }
#endif

        // The control's rect in screen pixels with the origin at the top-left, as the
        // accessibility hierarchy expects. The corner buffer is shared — LateUpdate calls
        // this per node per frame while a reader is on, and a fresh array each call would
        // be steady allocation for nothing (main thread only, consumed before return).
        private static readonly Vector3[] s_corners = new Vector3[4];

        private static Rect ScreenFrame(RectTransform rect)
        {
            var corners = s_corners;
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null ? canvas.worldCamera : null;
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
        }
    }
}
