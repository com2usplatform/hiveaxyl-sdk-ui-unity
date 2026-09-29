# Account — link & logout popup

The account settings popup: a centered card over the dimmed game with the
read-only account info (nickname, server, CS Code with one-tap copy), the
provider link states, and the delete-account / log-out actions.

## Common widgets this screen uses

- `Common/ActionButton` — the shared Primary / Destructive pill action (delete / log out)
- `Common/CloseButton` — bare 18px X in a 48px hit area (tinted dark for the
  light card)
- `Common/FailureToast` — the popup's feedback toast (copy feedback, link and
  unlink refusals)
- `Common/ProviderMarks` · `Common/ProviderIcons` — the shared provider icon
  slot; the login screen and this popup render the same file per provider id
  (the assets are the app's input — sources and the drop-in checklist live in
  the login screen README's "Provider icons" section)
- `Common/AxylUITheme` · `Common/AxylUIRuntimeAssets` · `Common/AxylUIScreen` ·
  `Common/AxylUIInput` — tokens, runtime assets, screen plumbing

Copy this folder together with `Common/`.

## Usage

```csharp
var screen = AccountScreen.Show(new AccountScreenOptions
{
    Nickname = "Player One",
    Server = "KR232",
    CsCode = 10160539418,
    // The app's list: its localized labels and the player's real link states.
    // DefaultProviders() is the reference set only — nothing connected, labels
    // showing as their string keys.
    Providers = new List<AccountProviderLink>
    {
        new("google", "Google", "G", connected: true),
        new("apple", "Apple", "A"),
    },
    OnProviderSelected = id => { /* run the matching link/login flow */ },
    OnCsCodeCopied = code => { /* show the app's "copied" toast */ },
    OnDeleteAccount = () => { /* open the app's confirmation flow */ },
    OnLogout = () => { /* log out, then close */ },
    OnClose = () => { /* app navigation */ },
});
screen.ShowToast("This provider cannot be unlinked.");  // player-facing feedback
screen.ShowToast("CS Code copied.", AxylUIToastVariant.Success); // copy feedback
screen.Dismiss();
```

The popup opens at the top of its content with the close button selected but
no focus ring showing (after a Tab in the session the ring shows right away;
a touch device never shows rings) — a confirm pressed right away closes
the popup; keyboard and controller input starts there and runs
close → copy → provider rows (a row the app disabled is left out) → delete
account → log out. Content taller than the screen scrolls between the fixed
header and the fixed actions. Screens do not stack: `Dismiss()` this popup
before showing another Kit screen — each screen traps focus and reads Esc
for itself.

The Kit never calls the SDK: it renders the given values and link states and
raises events. The copy button is the one built-in behavior — it puts the CS
Code on the system clipboard, then raises `OnCsCodeCopied` so the app can show
its own feedback toast. Deleting never happens in the Kit; `OnDeleteAccount`
is where the app opens its confirmation flow.

## Customizing

The screen is meant to be edited in place — copy it into your project and
change the code directly (the classes are `sealed`; subclassing is not the
extension path). The usual changes, and where they live:

The reference design in numbers: a max-960px card (32px padding on PC, 16px
on mobile), a 48px header, 44px provider rows in a grid with 8px gaps —
every provider visible, the grid itself never scrolling — and 44px bottom
actions. PC and Mobile Landscape share the Wide variant (info and actions in
one row, the seven providers in two columns by four rows with the last row's
second cell empty); Mobile Portrait stacks the account info vertically and
gives each provider a full-width row of its own — one column, seven rows —
while the actions stay side by side. Connected rows show a Primary emphasis
and a check; disconnected rows show a plus; a disabled row mutes its colors
but keeps its state icon (linked or not is never told by color alone), and is
left out of the keyboard order; Focus paints nothing on the row itself —
keyboard or controller focus shows the shared ring outside it. Rows share
the 16px `Button.HorizontalPadding` with the login buttons. The logout /
delete confirm card has no header or close control and does not close on a
backdrop tap: cancel, confirm, or Esc. A rotation rebuilds the popup and the
card goes with it; the popup stays open.

| Change | Where |
| --- | --- |
| Colors, sizes, spacing, state values | `Common/AxylUITheme.cs` — one value sheet for every screen |
| Copy (title, captions, labels) | `AccountScreenOptions` — pass localized strings in |
| What Connected / Hover / Pressed / Disabled look like | `AccountLinkRow.Refresh()` / `Common/ActionButton/AxylUIActionButton.Refresh()` — the one place per widget |
| Widget structure (row anatomy, action button) | change `Editor/AxylUIBaker.cs` and rebake — a bake regenerates the prefabs from that code, so edits made to a prefab in the editor are lost on the next bake |
| Screen layout (card, header, grid, actions) | `AccountScreen.Build()` — numbered steps, one method per region |
| Provider icons | `Common/ProviderMarks/Resources/UIKit/ProviderMarks/*.png` — shared with the login screen; app-supplied (official sources in the login README) |
| Header / state glyphs (settings, copy, plus, check) | `Account/Sprites/Resources/UIKit/AccountIcons/*.png` — drawn for this Kit; white shapes, tinted at runtime |

## Known limitations

- The column count follows the layout variant, not the measured label width:
  a wide screen keeps two columns even where a long label has to wrap.
- Row and action sizes stay fixed; height-constrained landscape screens rely
  on the popup's own margins, and spacing does not compress.
