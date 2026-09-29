# Login — provider select screen

The login provider-select screen: backdrop, title, provider buttons, close
button, failure toast.

## Common widgets this screen uses

- `Common/FailureToast` — shared failure toast
- `Common/CloseButton` — bare 18px light X in a 48px hit area
- `Common/ProviderIcons` + `Common/ProviderMarks` — the icon slot and the
  drop-in icon files (the bundled guest glyph included)
- `Common/AxylUITheme` · `Common/AxylUIRuntimeAssets` — tokens and runtime assets
- `Common/AxylUIScreen` · `Common/AxylUIInput` · `Common/AxylUIFocusTrap` ·
  `Common/AxylUIScrollView` · `Common/AxylUIAccessibility` ·
  `Common/AxylUIDirection` — layout, input, focus, scrolling, screen reader
  and reading-direction plumbing every screen shares

Copy this folder together with `Common/`.

## Usage

```csharp
var screen = LoginScreen.Show(new LoginScreenOptions
{
    Providers = LoginScreenOptions.DefaultProviders(), // or the app's own list
    OnProviderSelected = id => { /* run the matching login recipe */ },
    OnClose = () => { /* app navigation */ },
});
screen.ShowFailure("Sign-in could not be completed. Please try again later.");
screen.Dismiss();
```

The Kit never calls the SDK: it raises the selected provider id and renders
what the app passes in. Error copy is the app's mapping. Screens do not
stack: `Dismiss()` this screen before showing another Kit screen — each
screen traps focus and reads Esc for itself.

The screen opens with the close button selected but no focus ring showing
(after a Tab in the session the ring shows right away; a touch device never
shows rings) — a confirm pressed right away closes the screen; keyboard and
controller input starts there and runs close → the providers in order. A list taller
than the screen scrolls, and it opens at the top.

## Customizing

The screen is meant to be edited in place — copy it into your project and
change the copy directly (the classes are `sealed`; subclassing is not the
extension path). The usual changes, and where they live:

The reference design in numbers: 305×48 (PC) / 260×44 (mobile) buttons with
16px padding (the shared `Button.HorizontalPadding`), a 10px icon–text gap
and a 14/500 label at line height 1.4 (two lines at most; a longer label is a
copy defect to fix, not something the screen handles); Focus paints no tint
on the button itself — keyboard or controller focus shows the shared ring
outside it; the backdrop and Esc raise OnClose just like the close button;
Landscape goes 2-col only from five providers when 528px of content width
fits.

| Change | Where |
| --- | --- |
| Colors, sizes, spacing, state values | `Common/AxylUITheme.cs` — one value sheet for every screen |
| Title / button copy | `LoginScreenOptions` (`Title`, each provider's `Label`) — pass localized strings in |
| What Hover / Pressed / Focus look like | `LoginOptionButton.Refresh()` — the one place states are decided |
| Widget structure (button anatomy, toast) | change `Editor/AxylUIBaker.cs` and rebake — a bake regenerates the prefabs from that code, so edits made to a prefab in the editor are lost on the next bake |
| Screen layout (backdrop, column, grid) | `LoginScreen.Build()` — numbered steps, one method per region |
| Provider icons | `Common/ProviderMarks/Resources/UIKit/ProviderMarks/*.png` — shared with the account screen; download the official asset (sources below) and drop it onto that path |
| Fonts | `Fonts/` — swap the OTFs and rebake (the bake recreates the TMP assets from the OTFs, so a TMP asset swapped by hand is overwritten by the next full bake) |

## Provider icons

The Kit bundles no brand assets — each provider's icon is the app's input.
Download the official asset from the provider's brand guide below and use
it exactly as delivered — shape, proportions, colors and clear space are the
provider's terms, and several guides forbid altering them (screens display
the icon at 20×20). Hand it to the Kit either
way: pass the Sprite on the option (`LoginProviderOption` /
`AccountProviderLink`, the `icon` argument) — the route this Kit's sample
app uses — or drop the file at
`Common/ProviderMarks/Resources/UIKit/ProviderMarks/<providerId>.png`, imported
with Texture Type "Sprite (2D and UI)" (a 3D-mode project imports a new PNG as
a plain texture, which loads as null and falls back to the letter chip). The
login screen and the account popup read the same icon per provider id. A
provider with no delivered asset renders the letter-chip placeholder —
never substitute an unofficial icon; ask the app developer for the asset
instead. Guest is the one icon the Kit ships (its own default, not a
brand); Username and Custom have no official source, so the app supplies
those icons.

When dropping an asset in, record alongside it: the provider id, the file
path, the source and asset version, whether the source is managed as SVG
(preferred where the provider offers one), and whether it is usable on
light and dark grounds.

| File | Official source |
| --- | --- |
| `google.png` | [Google Identity branding guidelines](https://developers.google.com/identity/branding-guidelines) |
| `apple.png` | [Apple Design Resources](https://developer.apple.com/design/resources/) |
| `googlePlayGames.png` | [Google Play Games Services branding](https://developer.android.com/games/pgs/branding) |
| `steam.png` | [Steamworks branding](https://partner.steamgames.com/doc/marketing/branding) |
| `x.png` | [X Brand Toolkit](https://about.x.com/en/who-we-are/brand-toolkit) |
| `guest.png` | Drawn for this Kit (bundled) |

