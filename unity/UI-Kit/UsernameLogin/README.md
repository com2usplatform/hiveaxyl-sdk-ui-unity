# Username Login — credential form

The username login form in the Common Popup frame: the title in the popup's
header, then the "Don't have an account? Create a new account" row, the username and password
inputs (password masked, with a show/hide toggle), the form error, and the
login action in the popup's fixed Actions area. Shown as a popup over the
dimmed game or as a full-screen panel — the game's own background art and
decoration stay with the app.

## Common widgets this screen uses

- `Common/ActionButton` — the shared Primary pill action (the login button),
  disabled until the form validates
- `Common/InputGroup` — the shared labeled field (label, leading glyph,
  secure-value visibility toggle, error line)
- `Common/CloseButton` — the popup's close control (tinted dark for the
  light card; a full-screen form shows none)
- `Common/FailureToast` — the network/server-error channel
- `Common/AxylUITheme` · `Common/AxylUIRuntimeAssets` · `Common/AxylUIScreen` ·
  `Common/AxylUIInput` — tokens, runtime assets, screen plumbing

Copy this folder together with `Common/`, `Fonts/` and the root `UIKit.asmdef`,
`.meta` files included — see [Install](../../../README.md#install).

## Usage

```csharp
UsernameLoginScreen screen = null;
screen = UsernameLoginScreen.Show(new UsernameLoginOptions
{
    // The screen goes busy on submit by itself; the app releases it when its
    // call returns. Close and Esc stay live during the call, so the callback
    // checks the screen is still there before touching it.
    OnLoginRequested = (username, password) => SignIn(username, password, ok =>
    {
        if (screen == null) return;   // closed while the call was in flight
        screen.SetBusy(false);
        if (!ok) screen.ShowFormError("Username or password is incorrect.");
    }),
    OnCreateAccount = () => { /* open the app's sign-up URL or flow */ },
    OnClose = () => { /* app navigation */ },
});
screen.ShowToast("A network error occurred. Try again shortly."); // transport failure
screen.Dismiss();
```

The Kit never calls the SDK: it enforces the given input rules, raises only
valid submissions, and reports everything else as events. Where an error is
shown follows its kind — a broken or missing input under its own field, an
auth failure inside the form under the password input (no field turns red), a
network/server failure on the shared toast. Any edit clears the form error;
the typed username survives a failure. Screens do not stack: `Dismiss()` this
form before showing another Kit screen — each screen traps focus and reads
Esc for itself.

No field is focused when the form opens, so the soft keyboard stays down
until the player taps a field; keyboard and controller input starts at the
close button (with `Popup = false` there is no close button, and input starts
at the first item — the create-account link, or the username field when the
link is hidden). The keyboard path runs close → create-account link → username →
password → show/hide → login. A form taller than the screen (Landscape,
usually) scrolls on its own while the close button and the login action stay
fixed, and while the keyboard covers the focused field the content rides above
it and settles back when the keyboard hides.

## Input rules

The reference defaults are 4–20 characters for the username and 8–20 for the
password, enforced live: the login action enables only while both fields
pass. A non-empty field that breaks its rules shows its error underneath as it
is typed; an empty field shows its error once editing leaves it or a submit is
attempted (Enter).
The app layers its own auth policy on top through `UsernameValidator` /
`PasswordValidator` — return the error copy to show, or null to pass.

## Customizing

The screen is meant to be edited in place — copy it into your project and
change the code directly (the classes are `sealed`; subclassing is not the
extension path). The usual changes, and where they live:

The reference design in numbers: a max-720px popup around a 480px form, with
the content padding — 32px on PC, 16px in Mobile Portrait, 24px in Mobile
Landscape — as the popup's one padding (the form has no panel surface of its
own; on a 375px phone that leaves a 311px form), 48px inputs with a 20px
leading glyph, a 44px login action, and a create-account link hit area of
44px on PC and 48×48px on mobile. The form keeps one column in every layout; the prompt and
the link share a row while both fit and stack vertically when a translation
is too long for it.

| Change | Where |
| --- | --- |
| Colors, sizes, spacing, state values | `Common/AxylUITheme.cs` — one value sheet for every screen |
| Copy (title, labels, placeholders, prompt and link, errors) | `UsernameLoginOptions` — pass localized strings in |
| Input rules | `UsernameLoginOptions` lengths + validators |
| What the field states look like | `Common/InputGroup/AxylUIInputGroup.Refresh()` — resting / error |
| Widget structure (field anatomy) | change `Editor/AxylUIBaker.cs` and rebake — a bake regenerates the prefabs from that code, so edits made to a prefab in the editor are lost on the next bake |
| Screen layout (backdrop, panel, order) | `UsernameLoginScreen.Build()` — numbered steps, one method per region |
| Field glyphs (user, lock) | `UsernameLogin/Sprites/Resources/UIKit/UsernameIcons/*.png` — drawn for this Kit; white shapes, tinted at runtime (the eye toggle glyphs live with `Common/InputGroup` and are drawn for this Kit as well) |
