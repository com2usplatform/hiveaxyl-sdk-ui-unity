# Hive Axyl Reference UI Kit

> **Official repository** of Com2uS Platform.
> Canonical URL: <https://github.com/com2usplatform/hiveaxyl-sdk-ui-unity>

| | |
| --- | --- |
| Product | Hive Axyl |
| Domain | sdk |
| Version | see [`versions.yaml`](versions.yaml) and [`CHANGELOG.md`](CHANGELOG.md) |
| Lifecycle | Active |
| Related | [hiveaxyl-sdk-unity](https://github.com/com2usplatform/hiveaxyl-sdk-unity): the SDK; [hiveaxyl-sdk-example-unity](https://github.com/com2usplatform/hiveaxyl-sdk-example-unity): Recipes and usage examples for the SDK |

Reference UI implementations for the Hive Axyl SDK: screen-unit UI
resources a game copies into its own project and edits in place.

## Install

Copy the screen folders you need, together with `Common/`, into your
project:

- Unity — copy `unity/UI-Kit/` (or just the screen folders + `Common/` +
  `Fonts/` + the root `UIKit.asmdef`) into `Assets/`, `.meta` files included:
  the widget prefabs reference scripts, sprites and fonts by the GUIDs those
  files carry. `UIKit.asmdef` is not optional — `UIKIT_ACCESSIBILITY` is
  declared by its `versionDefines` alone, so without it screen reader support
  turns itself off silently. The Kit is auto-referenced: game code in plain
  `Assets/` scripts calls `LoginScreen` directly, and a game that organizes
  its own assemblies references `Hive.Axyl.UIKit` by name.
- Unreal — not yet available; when it ships, `unreal/UI-Kit/` copies into
  `Content/`.

Each screen folder's `README.md` lists the `Common/` widgets it uses.

## Trying the screens without an app

`unity/Sandbox/` is a self-contained Unity project that opens all four
screens on mock data — no SDK, no server. See its `README.md`; setup is one
copy script and one editor menu item.

## Supported engine versions

See [`versions.yaml`](versions.yaml) — the engine versions this Kit is
verified against.

## Design tokens

`AxylUITheme` is the Kit's token sheet — every color, size, spacing and state
value the screens read lives there, so restyling starts in that one file. The
reusable widgets ship as prefabs, and the bundled Noto Sans fonts (below) are
the Kit's type family.

Tokens without a `Baked` note in `AxylUITheme.cs` are read at runtime and take
effect on the next play. The tokens marked `Baked` are written into the
prefabs and sprites, and changing one of those needs a rebake before it fully
shows (`ButtonBorderWidth`, `InputHeight`, the close-button and toast metrics
among them; some are read at runtime as well, and those uses drift from the
baked ones until the rebake). A radius token is the case to watch: the PNGs
under `Common/Sprites` are drawn at those radii, so moving one means redrawing
the sprite as well.

Regenerate with `Axyl/UI Kit/Bake Assets` (`Editor/AxylUIBaker.cs`) after
changing a baked token, a widget's structure, a sprite or a font.

## Adding a screen

A screen is one self-contained folder beside `Common/`: its screen class, its
widget prefabs, its sprites, and one bake step — a partial of `AxylUIBaker`
in `Editor/AxylUIBaker.<Screen>.cs` whose `[AxylUIBakeStep]` method builds
the screen's prefabs with the shared helpers. The baker discovers steps by
attribute, so `AxylUIBaker.cs` itself never changes when a screen is added or
removed. The step lives under `Editor/` rather than in the screen folder, so
dropping a screen means deleting both — leave the step behind and it still
references the screen's types and the editor assembly stops compiling.

Rebake screen work with `Axyl/UI Kit/Bake Screen Assets`: it runs the steps
against the committed fonts and Common prefabs and touches nothing shared.
`Bake Assets` (the full bake, fonts and Common prefabs included) is for
changes to `Common/` itself.

## Model types

Screens take their data through plain option models and raise user actions
as events. Field types line up with the Hive Axyl SDK where the meaning is
the same — provider ids are strings, player-scoped numbers are 64-bit
(`long`) — so a value coming out of an SDK recipe drops into the screen
options; picking which values to pass stays app code.

Provider ids are the one value that needs a mapping. The Kit's defaults are
its own lowerCamel ids (`apple`, `googlePlayGames`, `custom`), and the same
string names the icon file under
`Common/ProviderMarks/Resources/UIKit/ProviderMarks/`. The SDK's `Provider`
enum goes on the wire as `SIGNIN_APPLE`, `GOOGLE_PLAY_GAMES`,
`CUSTOM_PROVIDER`, so handing a wire value straight to the screen finds no
icon file and the provider falls back to its letter chip. Either translate
the SDK value to the Kit's id when filling the options, or rename the icon
files to the ids the app passes — the Kit only requires that the id it is
given matches the file name.

## Languages and reading direction

Every string a screen shows comes in through its options, already
localized by the app; the Kit holds no language-specific copy. Each text
option defaults to its string key — the Kit's reference keys, collected in
`AxylUIStringKeys` — so a string the app has not passed shows up as its
key, never as English. The accessibility names the screen reader announces
(`CloseLabel`, `PasswordShowLabel` / `PasswordHideLabel`, `CopyLabel`, a
provider's `AccessibilityName`) come in the same way; announcing them needs
Unity's built-in Accessibility module enabled in the project
(`com.unity.modules.accessibility` in `Packages/manifest.json`). For a
right-to-left language — Arabic, Hebrew, Persian — set the options'
`RightToLeft` (the app judges it from its current language, e.g.
`AxylUIDirection.IsRightToLeft("ar")`): text aligns right and every
horizontal row — icon + label, header, actions, the grid rows — runs
right-to-left, while paddings, sizes and brand marks stay as they are. The
value the player types into an input keeps its glyph order and only aligns
right (ids, e-mail addresses and passwords are mostly Latin, and TMP's RTL
rendering reverses characters blindly while the input's caret does not
follow), as does the CS Code the Kit formats itself — so Hebrew input, like
Arabic shaping, is the app's own step.
TextMeshPro reverses the glyph run but does not shape Arabic or Persian
letters and has no bidirectional handling (a Latin brand name
inside an Arabic string is reversed too), and the bundled Noto Sans KR /
JP fonts carry no Arabic or Hebrew glyphs: an app shipping those languages
adds a font with the glyphs to the TextMeshPro fallback chain and its
shaping / bidi step, as it would for any TextMeshPro UI. To see the mirrored
layout, set `RightToLeft` on the screen's options — the Sandbox scene does
not expose a toggle for it.

## Reference rules

The Kit is pure UI: it renders what the app passes in and raises user
input as events. SDK calls stay with the app.

Screens do not stack: show one Kit screen at a time, and `Dismiss()` the
open one before showing another. Each screen traps focus and reads Esc for
itself, so two at once fight over both.

- May reference: `UnityEngine.UI`, `Unity.TextMeshPro` and `Unity.InputSystem`.
  `UIKit.asmdef` names the Input System unconditionally — Unity skips a
  reference a project does not have — while the code behind it compiles in
  only where the package is installed (a `versionDefines` check) and that
  backend is enabled, so Esc-to-close works on either input backend and a
  project that removed the package with Active Input Handling left on Both
  still compiles.
- Must NOT reference: `Hive.Axyl.*` (the SDK), sample Recipes, or sample
  scripts. `UIKit.asmdef` enforces the code boundary; asset references
  (sprites, fonts) are reviewed separately.

## License

Licensed under the [Apache License 2.0](LICENSE). The bundled Noto Sans
fonts (`unity/UI-Kit/Fonts/`) are under the SIL Open Font License 1.1
(`Fonts/OFL.txt`); see [NOTICE](NOTICE). The Kit bundles no third-party
brand marks.

## Support

GitHub Issues are not used as a support channel for this repository. Please
use the channels below for questions, bug reports, feature requests, and
customer inquiries.

| Purpose | Channel |
| --- | --- |
| Questions, bug reports, feature requests | <cs-platform@com2us.com> |
| Security vulnerabilities | Private report only, see [`SECURITY.md`](SECURITY.md) |

GitHub is not an official customer support channel, and we do not commit to
response or resolution timelines here. External pull requests are not
accepted; see [`CONTRIBUTING.md`](CONTRIBUTING.md).
