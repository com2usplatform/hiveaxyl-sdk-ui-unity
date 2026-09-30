// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Hive.Axyl.UIKit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Hive.Axyl.UIKit.Sandbox.Tests
{
    /// <summary>
    /// Each Kit screen opens, lives a few frames, and closes without an exception, driven the
    /// way the sandbox drives it: options filled with plain demo values and no SDK anywhere.
    /// </summary>
    /// <remarks>
    /// This is the Kit's whole contract with an app that has nothing else running yet — Show,
    /// render, Dismiss. A regression here (a missing prefab, a null on an empty option, a
    /// throw in a layout pass) is exactly what a designer would hit on the sandbox's first
    /// play, so it is pinned headlessly where it runs on every checkout.
    /// </remarks>
    public sealed class ScreenSmokeTests
    {
        private GameObject m_eventSystem;

        [SetUp]
        public void SetUp()
        {
            if (EventSystem.current == null)
            {
                m_eventSystem = new GameObject(
                    "EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // The ring's "has the player navigated yet" latch is static and outlives a
            // screen, which is what keeps rings visible for someone working from the
            // keyboard. Clear it so a test that navigates cannot decide the outcome of a
            // test that runs after it.
            ResetFocusRingLatch();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_eventSystem != null)
            {
                Object.Destroy(m_eventSystem);
            }
        }

        [UnityTest]
        public IEnumerator Login_opens_and_closes()
        {
            var screen = LoginScreen.Show(new LoginScreenOptions
            {
                Title = "Sign in",
                CloseLabel = "Close",
                Providers = new List<LoginProviderOption>
                {
                    new LoginProviderOption("google", "Sign in with Google", "G"),
                    new LoginProviderOption("guest", "Play as guest", "?"),
                },
                OnProviderSelected = _ => { },
                OnClose = () => { },
            });

            yield return null;
            yield return null;
            AssertBuilt(screen);

            screen.ShowFailure("A demo failure message.");
            yield return null;

            screen.Dismiss();
            yield return null;
            AssertDismissed(screen);
        }

        [UnityTest]
        public IEnumerator Account_opens_and_closes()
        {
            var screen = AccountScreen.Show(new AccountScreenOptions
            {
                Title = "Account",
                Nickname = "Player One",
                Server = "KR232",
                CsCode = 50000004919,
                Providers = new List<AccountProviderLink>
                {
                    new AccountProviderLink("google", "Google", "G", connected: true),
                    new AccountProviderLink("apple", "Apple", "A"),
                },
                OnProviderSelected = _ => { },
                OnClose = () => { },
            });

            yield return null;
            yield return null;
            AssertBuilt(screen);

            screen.ShowConfirm("Log out of this account?", "Cancel", "Log out", () => { });
            yield return null;

            screen.Dismiss();
            yield return null;
            AssertDismissed(screen);
        }

        [UnityTest]
        public IEnumerator Username_login_opens_and_closes()
        {
            var screen = UsernameLoginScreen.Show(new UsernameLoginOptions
            {
                Title = "Username sign-in",
                OnLoginRequested = (_, _) => { },
                OnCreateAccount = () => { },
                OnClose = () => { },
            });

            yield return null;
            yield return null;
            AssertBuilt(screen);

            screen.SetBusy(true);
            yield return null;
            screen.SetBusy(false);
            screen.ShowFormError("The username or password is incorrect.");
            yield return null;

            screen.Dismiss();
            yield return null;
            AssertDismissed(screen);
        }

        [UnityTest]
        public IEnumerator Password_change_opens_and_closes()
        {
            var screen = PasswordChangeScreen.Show(new PasswordChangeOptions
            {
                Title = "Change password",
                OnSubmit = (_, _) => { },
                OnClose = () => { },
            });

            yield return null;
            yield return null;
            AssertBuilt(screen);

            screen.ShowToast("The password was changed.", AxylUIToastVariant.Success);
            yield return null;

            screen.Dismiss();
            yield return null;
            AssertDismissed(screen);
        }

        // The app's own gate has to hold on the keyboard path too. A screen raises its
        // submit from two places — the action button and Enter in a text field — and only
        // the button consults `interactable`, so a form the app disabled through
        // SetSubmitEnabled(false) would otherwise go out the moment the player pressed
        // Enter on valid input. Both paths funnel through the screen's private submit, so
        // the test drives that directly: the legacy Input Manager this project uses cannot
        // have a key press injected, and reaching the funnel is the point either way.
        [UnityTest]
        public IEnumerator Username_login_submit_obeys_the_app_gate()
        {
            int raised = 0;
            var screen = UsernameLoginScreen.Show(new UsernameLoginOptions
            {
                Title = "Sign in",
                CloseLabel = "Close",
                OnLoginRequested = (_, _) => raised++,
                OnClose = () => { },
            });

            yield return null;
            FillCredentials(screen, "player-one", "correct-horse");
            yield return null;

            screen.SetSubmitEnabled(false);
            Submit(screen);
            yield return null;
            Assert.That(
                raised, Is.Zero,
                "a disabled form must not submit, whichever path reaches the submit");

            // The other half, so the assertion above cannot pass because the submit never
            // works at all: re-enabled, the same call goes through.
            screen.SetSubmitEnabled(true);
            Submit(screen);
            yield return null;
            Assert.That(raised, Is.EqualTo(1), "an enabled form with valid input submits");

            screen.Dismiss();
            yield return null;
            AssertDismissed(screen);
        }

        // A max length of 0 means "no limit" — the reading the input field's own character
        // limit uses. Taken literally it inverts: every value is longer than 0, so an app
        // that turned the length rule off would see its form reject everything it was given.
        [UnityTest]
        public IEnumerator Username_login_accepts_any_length_when_the_max_is_zero()
        {
            int raised = 0;
            var screen = UsernameLoginScreen.Show(new UsernameLoginOptions
            {
                Title = "Sign in",
                CloseLabel = "Close",
                UsernameMaxLength = 0,
                PasswordMaxLength = 0,
                OnLoginRequested = (_, _) => raised++,
                OnClose = () => { },
            });

            yield return null;
            FillCredentials(screen, "a-username-well-past-twenty", "a-password-well-past-twenty");
            yield return null;

            Submit(screen);
            yield return null;
            Assert.That(
                raised, Is.EqualTo(1),
                "no limit must accept a value, not reject every one");

            screen.Dismiss();
            yield return null;
            AssertDismissed(screen);
        }

        // Opening a screen puts the focus on its first control so the keyboard path has
        // somewhere to start, but nobody has navigated yet — showing a ring there tells the
        // player the close button is where they are, before they touched anything. The ring
        // belongs to the keyboard, so it waits for a keyboard move.
        [UnityTest]
        public IEnumerator A_screen_opens_with_no_focus_ring()
        {
            var screen = LoginScreen.Show(new LoginScreenOptions
            {
                Title = "Sign in",
                CloseLabel = "Close",
                Providers = new List<LoginProviderOption>
                {
                    new LoginProviderOption("google", "Sign in with Google", "G"),
                },
                OnProviderSelected = _ => { },
                OnClose = () => { },
            });

            // Two frames: the focus trap places its selection in LateUpdate, and the ring
            // would light on the select event that follows.
            yield return null;
            yield return null;
            AssertBuilt(screen);

            Assert.That(
                EventSystem.current.currentSelectedGameObject, Is.Not.Null,
                "the keyboard path still starts somewhere — only the ring is withheld");
            Assert.That(
                ActiveRings(screen.gameObject), Is.Zero,
                "no ring before the player has navigated");

            // The other half, so the assertion above cannot pass because rings never show:
            // once a keyboard move is recorded, re-selecting the same control lights it.
            var focused = EventSystem.current.currentSelectedGameObject;
            AxylUIFocusRing.NoteNavigation();
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            EventSystem.current.SetSelectedGameObject(focused);
            yield return null;

            Assert.That(
                ActiveRings(screen.gameObject), Is.EqualTo(1),
                "a control focused from the keyboard shows its ring");

            screen.Dismiss();
            yield return null;
            AssertDismissed(screen);
        }

        // Show has to leave a screen standing in the scene, not just return without
        // throwing: a Build that silently produced nothing would otherwise read as a pass.
        private static void AssertBuilt(MonoBehaviour screen)
        {
            Assert.That(screen, Is.Not.Null, "Show returned nothing");
            Assert.That(
                screen.gameObject.activeInHierarchy, Is.True,
                "the screen is not in the scene");
            Assert.That(
                screen.transform.childCount, Is.GreaterThan(0),
                "the screen built no content");
        }

        // Dismiss has to take the screen with it. Unity destroys at the end of the frame,
        // so the caller yields once before this. Without the check, turning Dismiss into a
        // no-op leaves every one of these tests green.
        private static void AssertDismissed(MonoBehaviour screen)
        {
            Assert.That(screen == null, Is.True, "Dismiss left the screen alive");
        }

        private static void ResetFocusRingLatch()
        {
            var field = typeof(AxylUIFocusRing).GetField(
                "s_navigationSeen", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "AxylUIFocusRing.s_navigationSeen is gone");
            field.SetValue(null, false);
        }

        private static int ActiveRings(GameObject screen)
        {
            int active = 0;
            foreach (var t in screen.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t.name == "FocusRing" && t.gameObject.activeInHierarchy)
                {
                    active++;
                }
            }

            return active;
        }

        // The screens keep their fields and their submit private — the Kit's surface is
        // Show / Dismiss and the option callbacks — so the test reaches them by name.
        // A rename fails here loudly rather than quietly stopping to test anything.
        private static void FillCredentials(
            UsernameLoginScreen screen, string username, string password)
        {
            SetField(screen, "m_username", username);
            SetField(screen, "m_password", password);
        }

        private static void SetField(UsernameLoginScreen screen, string field, string value)
        {
            var info = typeof(UsernameLoginScreen).GetField(
                field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, $"UsernameLoginScreen.{field} is gone");
            var group = info.GetValue(screen) as AxylUIInputGroup;
            Assert.That(group, Is.Not.Null, $"UsernameLoginScreen.{field} holds no input group");
            group.SetText(value);
        }

        private static void Submit(UsernameLoginScreen screen)
        {
            var method = typeof(UsernameLoginScreen).GetMethod(
                "SubmitLogin", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "UsernameLoginScreen.SubmitLogin is gone");
            method.Invoke(screen, null);
        }
    }
}
