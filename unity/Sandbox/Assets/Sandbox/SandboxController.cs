// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using Hive.Axyl.UIKit;
using UnityEngine;
using UnityEngine.UI;

namespace Hive.Axyl.UIKit.Sandbox
{
    /// <summary>
    /// A UI-only exercise bench for the Kit's four screens: every value a screen shows is a
    /// mock defined here, and every event a screen raises is answered here — no SDK, no
    /// server, no network. Open the scene, press play, and open a screen from the menu.
    /// </summary>
    /// <remarks>
    /// The mock account's password is <c>password1</c>: the credential forms accept it and
    /// answer anything else with their form error, so both outcomes are one keystroke apart.
    /// A short artificial delay stands in for the round trip, which keeps the busy state
    /// visible long enough to check. Link marks on the account popup toggle in memory and
    /// survive reopening the popup, not the play session.
    /// </remarks>
    public sealed class SandboxController : MonoBehaviour
    {
        private const string k_MockPassword = "password1";
        private const float k_MockDelaySeconds = 0.8f;

        // The account popup's mock state: which providers show as linked.
        private readonly HashSet<string> m_linked = new HashSet<string> { "google" };

        // The demo language the Kit screens open in. The Kit renders whatever strings the
        // app passes, so switching here is exactly what a localized app does: pass the other
        // set. The bench chrome itself stays English.
        private bool m_korean;

        private Text m_status;
        private Text m_languageLabel;

        private void Awake()
        {
            BuildMenu();
            SetStatus("Pick a screen. Mock password: " + k_MockPassword);
        }

        // One demo string: the set the current language picks.
        private string T(string english, string korean) => m_korean ? korean : english;

        // ---- Screens, wired to mocks ----

        private void OpenLogin()
        {
            LoginScreen screen = null;
            screen = LoginScreen.Show(new LoginScreenOptions
            {
                Title = T("Sign in", "로그인"),
                CloseLabel = T("Close", "닫기"),
                Providers = Providers(),
                OnProviderSelected = id =>
                {
                    // No sign-in exists here; the tap demonstrates the failure toast.
                    SetStatus($"Login: '{id}' selected.");
                    screen.ShowFailure(T(
                        $"Mock: '{id}' has nothing to sign in to.",
                        $"모의: '{id}'는 이 샌드박스에서 연결할 로그인이 없습니다."));
                },
                OnClose = () =>
                {
                    screen.Dismiss();
                    SetStatus("Login closed.");
                },
            });
        }

        private void OpenUsernameLogin()
        {
            UsernameLoginScreen screen = null;
            screen = UsernameLoginScreen.Show(new UsernameLoginOptions
            {
                Title = T("Username sign-in", "Username 로그인"),
                UsernameLabel = T("Username", "Username"),
                UsernamePlaceholder = T("Enter your username.", "username을 입력해 주세요."),
                PasswordLabel = T("Password", "비밀번호"),
                PasswordPlaceholder = T("Enter your password.", "비밀번호를 입력해 주세요."),
                LoginButtonLabel = T("Sign in", "로그인"),
                CreateAccountPrompt = T("Don't have an account?", "계정이 없으신가요?"),
                CreateAccountLabel = T("Create a new account", "새 계정 만들기"),
                UsernameErrorText = T(
                    "Check the username: 4-20 characters.",
                    "Username 입력값을 확인해 주세요. (4~20자)"),
                PasswordErrorText = T(
                    "Check the password: 8-20 characters.",
                    "비밀번호 입력값을 확인해 주세요. (8~20자)"),
                CloseLabel = T("Close", "닫기"),
                PasswordShowLabel = T("Show password", "비밀번호 표시"),
                PasswordHideLabel = T("Hide password", "비밀번호 숨기기"),
                OnCreateAccount = () => SetStatus("Username login: create-account selected."),
                OnLoginRequested = (username, password) => StartCoroutine(
                    MockRoundTrip(() =>
                    {
                        if (screen == null)
                        {
                            return; // closed during the mock round trip
                        }

                        if (password != k_MockPassword)
                        {
                            screen.SetBusy(false);
                            screen.ShowFormError(T(
                                "The username or password is incorrect.",
                                "username 또는 비밀번호가 올바르지 않습니다."));
                            return;
                        }

                        screen.SetBusy(false);
                        screen.ShowToast(
                            T($"Signed in as '{username}'.", $"'{username}'(으)로 로그인했습니다."),
                            AxylUIToastVariant.Success);
                        SetStatus($"Username login: '{username}' accepted.");
                    })),
                OnClose = () =>
                {
                    screen.Dismiss();
                    SetStatus("Username login closed.");
                },
            });
        }

        private void OpenPasswordChange()
        {
            PasswordChangeScreen screen = null;
            screen = PasswordChangeScreen.Show(new PasswordChangeOptions
            {
                Title = T("Change password", "비밀번호 변경"),
                CurrentPlaceholder = T("Current password", "현재 비밀번호"),
                NewPlaceholder = T("New password", "새 비밀번호"),
                ConfirmPlaceholder = T("Confirm new password", "새 비밀번호 확인"),
                SubmitLabel = T("Change", "변경"),
                CurrentRequiredText = T(
                    "Enter the current password.", "현재 비밀번호를 입력해 주세요."),
                NewRequiredText = T(
                    "Enter a new password.", "새 비밀번호를 입력해 주세요."),
                ConfirmRequiredText = T(
                    "Enter the new password again.", "새 비밀번호를 다시 입력해 주세요."),
                MismatchErrorText = T(
                    "The passwords do not match.", "비밀번호가 일치하지 않습니다."),
                NewInvalidText = T(
                    "Check the new password: 8-20 characters.",
                    "새 비밀번호를 확인해 주세요. (8~20자)"),
                CloseLabel = T("Close", "닫기"),
                PasswordShowLabel = T("Show password", "비밀번호 표시"),
                PasswordHideLabel = T("Hide password", "비밀번호 숨기기"),
                OnSubmit = (current, next) => StartCoroutine(
                    MockRoundTrip(() =>
                    {
                        if (screen == null)
                        {
                            return; // closed during the mock round trip
                        }

                        if (current != k_MockPassword)
                        {
                            screen.SetBusy(false);
                            screen.ShowFormError(T(
                                "The current password is incorrect.",
                                "현재 비밀번호가 올바르지 않습니다."));
                            return;
                        }

                        screen.SetBusy(false);
                        screen.ShowToast(
                            T("The password was changed.", "비밀번호가 변경되었습니다."),
                            AxylUIToastVariant.Success);
                        SetStatus("Password change: accepted.");
                    })),
                OnClose = () =>
                {
                    screen.Dismiss();
                    SetStatus("Password change closed.");
                },
            });
        }

        private void OpenAccount()
        {
            AccountScreen screen = null;
            screen = AccountScreen.Show(new AccountScreenOptions
            {
                Title = T("Account", "계정"),
                NicknameLabel = T("Nickname", "닉네임"),
                ServerLabel = T("Server", "서버"),
                CsCodeLabel = "CS Code",
                LinkSectionTitle = T("Linked accounts", "계정 연동"),
                DeleteAccountLabel = T("Delete account", "계정 삭제"),
                LogoutLabel = T("Log out", "로그아웃"),
                LogoutConfirmMessage = T(
                    "Log out of this account?", "이 계정에서 로그아웃할까요?"),
                LogoutCancelLabel = T("Cancel", "취소"),
                LogoutConfirmLabel = T("Log out", "로그아웃"),
                CloseLabel = T("Close", "닫기"),
                CopyLabel = T("Copy CS Code", "CS Code 복사"),
                Nickname = T("Player One", "마로쿠루"),
                Server = "KR232",
                CsCode = 50000004919,
                Providers = AccountLinks(),
                OnProviderSelected = id =>
                {
                    // Toggle the mark in memory and reopen so the rows re-render — the same
                    // dismiss-and-show a real app uses after its link call returns.
                    if (!m_linked.Remove(id))
                    {
                        m_linked.Add(id);
                    }

                    SetStatus($"Account: '{id}' is now "
                        + (m_linked.Contains(id) ? "linked." : "unlinked."));
                    screen.Dismiss();
                    OpenAccount();
                },
                OnCsCodeCopied = code => screen.ShowToast(T(
                    $"CS Code {code} copied.",
                    $"CS Code {code}를 복사했습니다."), AxylUIToastVariant.Success),
                OnDeleteAccount = () => screen.ShowConfirm(
                    T("Delete this account? This cannot be undone.",
                        "계정을 삭제하시겠습니까? 되돌릴 수 없습니다."),
                    T("Cancel", "취소"), T("Delete", "삭제"),
                    () =>
                    {
                        screen.Dismiss();
                        SetStatus("Account: deleted (mock).");
                    },
                    destructive: true),
                OnLogout = () =>
                {
                    screen.Dismiss();
                    SetStatus("Account: logged out (mock).");
                },
                OnClose = () =>
                {
                    screen.Dismiss();
                    SetStatus("Account closed.");
                },
            });
        }

        // ---- Mock data ----

        // Demo labels in English or Korean (see the T helper): the Kit renders whatever the app passes, and the Kit's own
        // defaults are string keys. Brand icons: none ship (see SandboxProviderIcons) —
        // drop an officially obtained PNG at Resources/SandboxProviderIcons/<id>.png to
        // see a real one; otherwise Guest renders the Kit's own mark and every other
        // provider its letter chip.
        private List<LoginProviderOption> Providers() => new List<LoginProviderOption>
        {
            Option("google", T("Sign in with Google", "Google로 로그인"), "G"),
            Option("apple", T("Sign in with Apple", "Apple로 로그인"), "A"),
            Option("googlePlayGames",
                T("Sign in with Play Games", "Play Games로 로그인"), "GP"),
            Option("steam", T("Sign in with Steam", "Steam으로 로그인"), "S"),
            Option("x", T("Sign in with X", "X로 로그인"), "X"),
            Option("username", T("Sign in with username", "username으로 로그인"), "U"),
            Option("custom", T("Sign in with Custom", "Custom으로 로그인"), "C"),
            Option("guest", T("Play as guest", "게스트로 시작"), "?"),
        };

        private static LoginProviderOption Option(string id, string label, string chip) =>
            new LoginProviderOption(id, label, chip, icon: SandboxProviderIcons.TryGet(id));

        private List<AccountProviderLink> AccountLinks() => new List<AccountProviderLink>
        {
            Link("google", "Google", "G"),
            Link("apple", "Apple", "A"),
            Link("googlePlayGames", "Play Games", "GP"),
            Link("steam", "Steam", "S"),
            Link("x", "X", "X"),
            Link("username", "Username", "U"),
            Link("custom", "Custom", "C"),
        };

        private AccountProviderLink Link(string id, string label, string chip) =>
            new AccountProviderLink(
                id, label, chip, connected: m_linked.Contains(id),
                icon: SandboxProviderIcons.TryGet(id));

        private IEnumerator MockRoundTrip(System.Action onDone)
        {
            // Long enough for the busy state to be seen, short enough to iterate quickly.
            yield return new WaitForSeconds(k_MockDelaySeconds);
            onDone();
        }

        // ---- The bench's own chrome: a plain uGUI menu under the Kit's overlay canvas ----

        private void BuildMenu()
        {
            var canvasGo = new GameObject("SandboxMenu",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // the Kit sorts its canvases above

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var column = new GameObject("Column", typeof(RectTransform)).GetComponent<RectTransform>();
            column.SetParent(canvasGo.transform, false);
            column.anchorMin = new Vector2(0.5f, 0.5f);
            column.anchorMax = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(560f, 0f);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            column.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            AddLabel(column, "Reference UI Kit — sandbox", 40, FontStyle.Bold);
            AddButton(column, "Login", OpenLogin);
            AddButton(column, "Account", OpenAccount);
            AddButton(column, "Username Login", OpenUsernameLogin);
            AddButton(column, "Password Change", OpenPasswordChange);
            m_languageLabel = AddButton(column, "Language: English", ToggleLanguage);
            m_status = AddLabel(column, "", 26, FontStyle.Normal);
        }

        private static Text AddLabel(RectTransform parent, string text, int size, FontStyle style)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            return label;
        }

        private void ToggleLanguage()
        {
            m_korean = !m_korean;
            m_languageLabel.text = m_korean ? "Language: 한국어" : "Language: English";
            SetStatus(m_korean
                ? "Kit screens now open in Korean."
                : "Kit screens now open in English.");
        }

        private static Text AddButton(RectTransform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Button_{text}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 88f;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.35f, 0.9f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var label = AddLabel((RectTransform)go.transform, text, 32, FontStyle.Normal);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return label;
        }

        private void SetStatus(string message)
        {
            if (m_status != null)
            {
                m_status.text = message;
            }

            Debug.Log($"[Sandbox] {message}");
        }
    }
}
