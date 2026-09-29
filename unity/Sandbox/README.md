# UI Kit Sandbox

A Unity project for exercising the Kit's four screens with nothing behind
them: every value a screen shows is a mock, every event it raises is answered
locally, and no SDK, server or network is involved. Use it to check layout,
states, rotation and copy fit without setting up anything else.

## Run

1. `./sync-kit.sh` — copies `../UI-Kit` into `Assets/UI-Kit` (gitignored; the
   Kit's source of truth stays in `unity/UI-Kit`). Re-run after changing the
   Kit.
2. Open this folder as a Unity project (the version `../../versions.yaml`
   names). On a fresh checkout run `Axyl → UI Kit Sandbox → Build Scene`
   once: it imports the TMP Essential Resources the Kit's text needs
   (gitignored) and rewrites the scene. The rewrite is not byte-stable, so
   discard a scene diff you did not mean to make.
3. Open `Assets/Sandbox/SandboxScene.unity` and press play. Pick a screen
   from the menu — the menu is the sandbox's own plain-uGUI chrome; the
   Kit's look starts inside the screens it opens.

## The mocks

- The account's password is `password1`: the username sign-in and the
  password change accept exactly that value and answer anything else with
  their form error, so both outcomes are one keystroke apart. A 0.8s
  artificial delay stands in for the round trip, long enough to see the
  busy state.
- On the account popup, selecting a row toggles its link mark in memory and
  the popup reopens on the new state; copy shows the toast; delete and log
  out confirm on the Kit's card and close.
- On the login screen, any provider tap shows the failure toast naming the
  provider — there is nothing to sign in to here.
- All strings are English demo copy passed in the way an app passes its
  localized strings (the Kit's own defaults are string keys).
- Brand icons: none ship — this repository carries no provider's mark, so
  every provider renders the way a real app looks before its assets arrive:
  Guest through the one mark the Kit itself ships, everything else as the
  letter chip. To see real icons, obtain each official asset under its
  provider's brand guideline and drop it at
  `Assets/Sandbox/Resources/SandboxProviderIcons/<providerId>.png`, imported
  as Sprite (2D and UI) — the Sandbox passes it in on the screen options,
  the way an app hands the Kit the assets it obtained.

## Layout of this folder

- `Assets/Sandbox/SandboxController.cs` — the menu and every mock.
- `Assets/Sandbox/Editor/SandboxSceneBuilder.cs` — rebuilds the committed
  scene (`Axyl → UI Kit Sandbox → Build Scene`) after changing what it must
  contain, and builds a macOS player into the gitignored `Build/macOS`
  (`Axyl → UI Kit Sandbox → Build macOS Player`) for a look outside the
  editor.
- `Assets/Sandbox/Tests/PlayMode/` — a headless smoke: each screen opens,
  lives a few frames, and closes without an exception.
- `Assets/UI-Kit/` — the copy `sync-kit.sh` makes; never edited here. The
  baker menu is available from this project, but it writes into this copy,
  which is gitignored and the next `sync-kit.sh` deletes. Rebake from a
  project that opens `unity/UI-Kit` itself, then sync.
