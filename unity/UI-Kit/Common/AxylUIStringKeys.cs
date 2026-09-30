// Copyright (c) Com2uS Platform Corp. All rights reserved.

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The string keys the Kit's screens are written against — the keys an app's translation
    /// table carries for them. The Kit itself holds no language-specific copy: every option
    /// that shows text defaults to its key, so a string the app forgot to pass shows up as
    /// the key on screen instead of as English. Pass <c>Localize(key)</c> results into the
    /// screen options.
    /// </summary>
    public static class AxylUIStringKeys
    {
        // ---- Shared ----
        public const string Close = "common.close";
        public const string PasswordShow = "auth.password.show";
        public const string PasswordHide = "auth.password.hide";
        public const string NetworkError = "auth.network.error";

        // ---- Login (provider select) ----
        public const string Login = "auth.login";
        public const string LoginError = "auth.loginError";

        /// <summary>The label key of one provider button: <c>auth.provider.[providerId]</c>.</summary>
        public static string Provider(string providerId) => "auth.provider." + providerId;

        // ---- Username login ----
        public const string UsernameLoginTitle = "auth.login.title";
        public const string UsernameLabel = "auth.username.label";
        public const string UsernamePlaceholder = "auth.username.placeholder";
        public const string PasswordLabel = "auth.password.label";
        public const string PasswordPlaceholder = "auth.password.placeholder";
        public const string LoginButton = "auth.login.button";
        public const string CreateAccountPrompt = "auth.createAccount.prompt";
        public const string CreateAccount = "auth.createAccount";
        public const string UsernameError = "auth.username.error";
        public const string PasswordError = "auth.password.error";
        public const string UsernameLoginError = "auth.login.error";

        // ---- Password change ----
        public const string PasswordChangeTitle = "auth.passwordChange.title";
        public const string CurrentPasswordPlaceholder = "auth.passwordChange.currentPassword.placeholder";
        public const string CurrentPasswordRequired = "auth.passwordChange.currentPassword.required";
        public const string NewPasswordPlaceholder = "auth.passwordChange.newPassword.placeholder";
        public const string NewPasswordRequired = "auth.passwordChange.newPassword.required";
        public const string NewPasswordInvalid = "auth.passwordChange.newPassword.invalid";
        public const string ConfirmPasswordPlaceholder = "auth.passwordChange.confirmPassword.placeholder";
        public const string ConfirmPasswordRequired = "auth.passwordChange.confirmPassword.required";
        public const string ConfirmPasswordMismatch = "auth.passwordChange.confirmPassword.mismatch";
        public const string PasswordChangeSubmit = "auth.passwordChange.submit";
        public const string PasswordChangeError = "auth.passwordChange.error";
        public const string PasswordChangeSuccess = "auth.passwordChange.success";
        public const string PasswordChangeAuthFailed = "auth.passwordChange.formError.authFailed";
        public const string PasswordChangeNetworkError = "auth.passwordChange.networkError";

        // ---- Account (reference keys; a project may rename them to its own scheme) ----
        public const string AccountTitle = "account.title";
        public const string AccountNickname = "account.nickname";
        public const string AccountServer = "account.server";
        public const string AccountCsCode = "account.csCode";
        public const string AccountCsCodeCopy = "account.csCode.copy";
        public const string AccountCsCodeCopied = "account.csCode.copied";
        public const string AccountLinkTitle = "account.link.title";
        public const string AccountDelete = "account.delete";
        public const string AccountLogout = "account.logout";
        public const string AccountLogoutConfirmTitle = "account.logout.confirm.title";
        public const string AccountLogoutConfirm = "account.logout.confirm";
        public const string AccountLogoutCancel = "account.logout.cancel";

        /// <summary>The account row label key of one provider: <c>account.provider.[providerId]</c>.</summary>
        public static string AccountProvider(string providerId) => "account.provider." + providerId;
    }
}
