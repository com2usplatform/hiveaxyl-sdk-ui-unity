# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-09-29

First release of the Hive Axyl Reference UI Kit for Unity — screen-unit UI
resources a game copies into its own project and edits in place.

### Added

- Login screen (Unity uGUI): the provider-select screen, rendering the one to
  eight provider buttons the app passes in, in that order, with Default /
  Hover / Pressed states, a close control, and a shared failure toast. It
  lays out for PC, Mobile Portrait and Mobile Landscape, goes two columns
  where the width allows, and scrolls when the content outgrows the screen.
  The close button, the backdrop and Esc all raise the close event —
  navigation stays with the app.
- Account screen (Unity uGUI): the account link-and-logout popup — a centered
  card showing the nickname, server and CS Code with one-tap copy, every
  provider's link state in a grid, and the delete-account and log-out actions.
  Log-out asks first on a confirm card; delete account raises
  `OnDeleteAccount` at once — the app runs its own confirmation, and can reuse
  the Kit's card through `ShowConfirm`. A rotation dismisses the card with the
  rebuild and leaves the popup open. All values, link states and strings come from the app, every
  action is raised as an event, and a shared toast overlays the card for the
  messages the player must see.
- Username login screen (Unity uGUI): a credential form in the Common Popup
  frame — username and password inputs with live length rules and
  app-supplied validators, a masked password with a show/hide toggle, an
  optional create-account row that hands navigation to the app, and a login
  action that enables only while the form validates and goes busy until the
  app answers. A broken field shows its error under the input as it is typed
  and a missing one once editing leaves it, an auth failure shows inside the
  form, and network errors go on the shared toast. Shown as a popup over the
  dimmed game or full screen.
- Password-change screen (Unity uGUI): a form in the Common Popup frame of
  three masked password inputs — current, new and confirm, each with a
  show/hide toggle — and a confirm action that enables only while the fields
  are filled, the new password meets the 8–20 character rule, the confirmation
  matches and the app's policy validator passes. Every error shows under the
  input it concerns with that field outlined, appearing once editing leaves
  the field and clearing as it is corrected, and the screen reader reads it as
  the field's description. The result and transient errors go on the shared
  toast.
- One design-token sheet: `AxylUITheme` holds every color, size, spacing, and
  state value the screens read, so restyling the Kit starts in one file. Most
  tokens apply at runtime; the tokens marked `Baked` are built into the
  prefabs and sprites and need `Axyl/UI Kit/Bake Assets` before they show.
- Screens pick their layout by breakpoint order — platform first, then the
  safe-area viewport in density-independent pixels: on mobile a landscape
  viewport under 480 px tall takes the landscape layout whatever its width,
  and 640 px of width separates the PC layout from the single column. They
  size their content against the device safe area (notch, home indicator,
  display cutouts) and rebuild in place when the size or orientation changes,
  carrying over typed input, the focused control and the scroll position,
  while the content slides up to clear the soft keyboard and back when it
  hides. The same helpers (`AxylUIScreen`) are public, so an app's own chrome
  can follow the pattern.
- One shared scroll view (`AxylUIScrollView`) for content taller than its
  room, built as Scroll View › Viewport › Content; keyboard or controller
  navigation that focuses a control outside the visible part scrolls it into
  view.
- Right-to-left languages: every screen takes a `RightToLeft` option (the
  app judges it from its language with `AxylUIDirection.IsRightToLeft`), and
  text aligns right while icon + label rows, headers, action rows and the
  grid rows run right-to-left; paddings, sizes and brand marks stay as they
  are. The value the player types into an input and the CS Code the Kit
  formats itself keep their glyph order and only align right.
- The toast lives on its own layer above everything else on the screen —
  an open confirmation card included — and a message mid-read survives a
  rotation with its remaining time.
- Reusable widgets ship as prefabs (CloseButton, the FailureToast status
  toast with Default / Success / Error variants, the AxylUIActionButton pill
  action with Primary, Secondary and Destructive styles, and the
  AxylUIInputGroup form field with an optional label row, an optional
  secure-value visibility toggle and an error border) built on 9-slice
  sprites, with Noto Sans KR (Regular, Medium, Bold) and Noto Sans JP
  (Regular, Medium) bundled under the SIL Open Font License. An editor menu
  (Axyl → UI Kit → Bake Assets) regenerates the baked assets after a
  structural edit.
- A provider icon slot instead of bundled brand assets: the app downloads
  each provider's official asset from its brand guide (sources linked in the
  login screen README) and drops a PNG named after the provider id — no code
  changes. Guest ships with the Kit's own default icon, and a provider with
  no delivered asset renders a letter-chip placeholder, never a substitute
  icon.
- Localization-ready: the Kit holds no language-specific copy and renders
  the strings the app passes in — every text option defaults to its string
  key (`AxylUIStringKeys`, the Kit's reference keys), so a string the app has
  not passed shows up as its key rather than as English; the bundled fonts
  cover the Korean, English, and Japanese demo locales out of the box.
- Keyboard, controller and screen reader: a screen opens with the close
  button selected but no focus ring showing (after a Tab in the session the
  ring shows right away — never on a touch device), and the first Tab moves
  on with the ring visible. Focus stays inside the
  open screen, and the focused control shows a ring outside its edge when the
  focus arrived by keyboard or controller — never for a pointer click, and
  not on a touch-only device. Every control carries an accessibility name the
  app passes in, announced by the platform screen reader when the project
  enables Unity's Accessibility module, and on PC the pointer turns into a
  hand over any interactive control.
- Model types line up with the Hive Axyl SDK where meanings coincide —
  provider ids are strings, player-scoped numbers are 64-bit — so values
  coming out of the SDK drop into the screen options. Provider ids are the
  one exception: the SDK's wire values differ from the Kit's icon-file ids,
  so they pass through the mapping the README describes.
