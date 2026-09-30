# Password Change — credential form

The password-change form in the Common Popup frame: the title, three masked
password inputs — current, new, and confirm, each with a show/hide toggle and
named by its placeholder (no label row) with its own error line — and the
confirm action right under them. Shown as a popup over the dimmed game or as a
full-screen panel; the game's own background art stays with the app.

## Common widgets this screen uses

- `Common/ActionButton` — the shared Primary pill action (the confirm button),
  disabled until the form is client-valid
- `Common/InputGroup` — the shared field (leading glyph, secure-value
  visibility toggle; the label row is left out here — the placeholder names
  the field — and each field shows its own error line)
- `Common/CloseButton` — the popup's close control (tinted dark for the
  light card; a full-screen form shows none)
- `Common/FailureToast` — the result (Success) and transient-error channel
- `Common/AxylUITheme` · `Common/AxylUIRuntimeAssets` · `Common/AxylUIScreen` ·
  `Common/AxylUIInput` — tokens, runtime assets, screen plumbing

Copy this folder together with `Common/`, `Fonts/` and the root `UIKit.asmdef`,
`.meta` files included — see [Install](../../../README.md#install).

## Usage

```csharp
PasswordChangeScreen screen = null;
screen = PasswordChangeScreen.Show(new PasswordChangeOptions
{
    // The screen goes busy on submit by itself; the app releases it when its
    // call returns. Close and Esc stay live during the call, so the callback
    // checks the screen is still there before touching it.
    OnSubmit = (current, next) => ChangePassword(current, next, ok =>
    {
        if (screen == null) return;   // closed while the call was in flight
        screen.SetBusy(false);
        if (ok)
        {
            screen.ShowToast("Your password has been changed.",
                AxylUIToastVariant.Success);
        }
        else
        {
            screen.ShowFormError("The current password is incorrect.");
        }
    }),
    OnClose = () => { /* app navigation */ },
    NewPasswordValidator = value => value.Contains(" ") ? "No spaces." : null,
});
screen.ShowToast("A temporary error occurred. Try again shortly."); // transient
screen.Dismiss();
```

The Kit never calls the SDK, and it judges only what a client can know:
three filled fields, the 8–20 character length rule the username login also
applies (a value past 20 shows the length error instead of being cut; the
masked field itself stops accepting input at 128 characters), a matching confirmation, and the
app's own policy validator — and raises only submissions that pass. Every
error shows under the input it concerns, which paints its error border: a
missing value or a too-short (or too-long) new password under that field, a policy failure
under the new password, the mismatch under the confirmation, and the wrong
current password reported via `ShowFormError` under the current password. A
field's error appears once editing leaves it or a submit is attempted, and
clears as the value is corrected; editing the current password clears the
auth failure. No error is ever a toast: a transient failure goes on the
shared toast, and success as a Success toast. The app can also report its
own verdict about a field with `SetFieldError` — it shows the same way and
clears when that field is edited. The screen reader gets each field's error
as its description, after the field's name. Screens do not stack: `Dismiss()`
this form before showing another Kit screen — each screen traps focus and
reads Esc for itself.

No field is focused when the form opens, so the soft keyboard stays down
until the player taps a field; keyboard and controller input starts at the
close button (with `Popup = false` there is no close button, and input starts
at the current-password field — still without raising the keyboard). The
keyboard path runs close → current → new → confirm →
confirm action → the three show/hide toggles. A form taller than the screen
(Landscape, usually) scrolls as
a whole — inputs and confirm action together — under the fixed close button,
and while the keyboard covers the focused field the content rides above it and
settles back when the keyboard hides.

## Customizing

The screen is meant to be edited in place — copy it into your project and
change the code directly (the classes are `sealed`; subclassing is not the
extension path). The usual changes, and where they live:

The reference design in numbers: a max-720px panel around a 480px form (32px
padding on PC, 16/24px on mobile), 48px inputs with a 20px leading glyph,
and a 44px confirm action. The form keeps one column and its input order in
every layout.

| Change | Where |
| --- | --- |
| Colors, sizes, spacing, state values | `Common/AxylUITheme.cs` — one value sheet for every screen |
| Copy (title, placeholders, errors) | `PasswordChangeOptions` — pass localized strings in |
| Password policy | `PasswordChangeOptions.NewPasswordValidator` (+ `SetFieldError` for server verdicts) |
| What the field states look like | `Common/InputGroup/AxylUIInputGroup.Refresh()` — resting / error |
| Widget structure (field anatomy) | change `Editor/AxylUIBaker.cs` and rebake — a bake regenerates the prefabs from that code, so edits made to a prefab in the editor are lost on the next bake |
| Screen layout (backdrop, panel, order) | `PasswordChangeScreen.Build()` — numbered steps, one method per region |
| Field glyph (lock) | `PasswordChange/Sprites/Resources/UIKit/PasswordChangeIcons/lock.png` — drawn for this Kit; a white shape, tinted at runtime |

## Known limitations

- The confirm action shows no spinner while busy; it disables (with the
  fields) until the app answers.
