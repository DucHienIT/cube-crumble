# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

A Unity 6 (`6000.3.9f1`) project containing **Cube Burst** (working title) — a genre clone of the iOS tap-puzzle "Cube Crumble": tap cubes in an isometric polycube, cubes crumble into colored balls that fall into color-matched containers, with a shared-slot overflow fail condition and a countdown timer.

**The full game design spec is in [docs/cube-crumble-game-spec.md](docs/cube-crumble-game-spec.md)** (in Vietnamese). The spec's section 5 proposes a web/Phaser stack — superseded; only the game rules, systems, and data formats apply. The game is implemented and playable: open `Assets/_Project/Scenes/Main.unity` and press Play.

## Code map (`Assets/_Project/`)

Scene-authored + prefab architecture: `Main.unity` contains the real persistent objects — Main Camera, `GameBootstrap` (applies config + camera only, creates nothing), `EventSystem` (InputSystemUIInputModule), `AudioManager`, `UICanvas` (Canvas + `UIController`), and `GameManager` (serialized refs to the UIController and the five level prefabs). Per-level content is instantiated from **prefabs in `Prefabs/`**: `Backdrop`, `CubeShape` (refs `Cube` + `DebrisPiece` prefabs), `SharedSlot`, `ContainerRow` (refs `DebrisPiece`), `Ball`, `Cube`, `DebrisPiece`. Component tunables (ball scale, trail time/width, collider size, tray/row root positions, sorting orders) live on the prefabs — edit them in the Inspector. All sprites/meshes/audio are still generated procedurally (zero image/sound assets), so components assign those procedural assets to their prefab-provided components at runtime in `Init`/`Launch`/`Start`; deep visual child hierarchies (UI screens, basin sprites, sockets, outline hulls) are still built in code because runtime-generated sprites can't be serialized into assets. **Designer-facing tunables are NOT hardcoded**: they live in two ScriptableObject assets under `Resources/Config/` (see `Scripts/Config/` below) wired to `GameBootstrap`'s serialized fields. When adding a new gameplay/feel/color constant, put it in the config or on a prefab, not a `const`.

**Serialization gotcha:** a MonoBehaviour used on a prefab or scene object must live in a `.cs` file named exactly after the class (`DebrisPiece` is in its own `DebrisPiece.cs` for this reason — when it lived inside `DebrisBurst.cs`, prefab script refs silently saved as `{fileID: 0}` and loaded as null). Classes only ever `AddComponent`ed at runtime (`BallTag`, `ContainerSlotView`) are exempt.

- `Scripts/Config/` — ScriptableObject configs, each with a static `Active` accessor (serialized reference from `GameBootstrap` → `Resources.Load("Config/…")` fallback → `CreateInstance` code defaults, so the game runs even without the assets): `GameConfig` (frame rate, camera fit, total levels, star time thresholds, drag threshold/sensitivity, ball flight duration/stagger/jitter, tray-transfer delays, result-popup delay, music volume), `PaletteConfig` (every color incl. the `GameColor`-indexed `gameColors` array). Assets: `Resources/Config/GameConfig.asset`, `Resources/Config/PaletteConfig.asset` (`[CreateAssetMenu]` under *Cube Burst/* for extras). Note: `SpriteFactory`/`CubeMeshFactory` bake colors into cached sprites/materials at startup — palette edits apply on next Play, not live.

- `Scripts/Core/` — pure C# logic, no UnityEngine dependency (except none): `GameColor`, `LevelData` (JSON model), `CubeShapeModel` (voxel grid + exposure rule), `ContainerModels` (4 active slots + queue, per-ball reservation), `SharedSlotModel`, `GameSession` (orchestrates a level; views drive it via `TapCube`/`BallArrived` and listen to events).
- `Scripts/Gameplay/` — MonoBehaviour views styled after the original Cube Crumble reference art (screenshot-matched): `GameBootstrap` (config + camera apply, `DefaultExecutionOrder(-100)`), `GameManager` (scene object holding the prefab refs; level load — `Instantiate(prefab)` + `Init(session)` per view; input: tap = press+release without moving, horizontal drag spins the shape; `Physics.Raycast` picking; session↔view wiring), `CubeShapeView`/`CubeView` (3D cube meshes under an isometric rotation, drag-to-rotate with 90° yaw snap, plus the white+blue **silhouette outline**: enlarged cube clones pushed to fixed world-z planes behind the shape — the ortho camera keeps their footprint identical so their union hugs the outline; repositioned in `ApplyRotation`), `BallView` (bezier arc flight, 3D sphere with matcap-baked lighting), `SharedSlotView` (U-shaped basin sprite + polka interior + dashed limit; landed balls become **physics balls** — SphereCollider + Rigidbody frozen on z, piling up in the basin between static BoxColliders), `ContainerRowView` (active containers = pill sprites with socket holes + 3D fill balls; upcoming queue shown as pill rows below, round-robin per column), `BackdropView` (striped bg, bottom panel, side pillars), `DebrisBurst` (tumbling mini cubes, shrink-out instead of alpha fade — the unlit materials are opaque).
- `Scripts/Systems/` — `SpriteFactory` (2D sprites via SDF-ish pixel generation: UI sprites plus the reference-art set — BigRounded/Stripes/PolkaPanel/BasinRim/Dashes/Pill/Socket/Stopwatch), `CubeMeshFactory` (shared unit-cube + matcap-UV sphere meshes; flat faces with thin near-black seams; `MaterialsForTint`/`BallMaterial`/`SolidMaterial` for arbitrary colors), `UIFactory` (uGUI by code, legacy `Text` + `LegacyRuntime.ttf`, **no TMP**), `AudioManager` (all clips synthesized), `SaveSystem` (PlayerPrefs, prefix `CubeBurst_`), `Palette` (static facade over `PaletteConfig` — call sites stay `Palette.Background`/`Palette.Of(color)`; **display hues follow the reference art, not the enum names** — e.g. `GameColor.Orange` renders dark gray, `Blue` renders cyan; level JSON stores indices so the enum can't change).
- `Scripts/UI/` — plain-class screens toggled by `UIController`: MainMenu, LevelSelect, HUD, PausePopup, ResultPopup.
- `Scripts/Editor/LevelTools.cs` — menu `Tools/Cube Burst/Validate Levels`.
- `Prefabs/` — the seven gameplay prefabs (see architecture note above); authored via Unity MCP, editable in the Inspector.
- `Resources/Levels/level_NNN.json` — 30 generated levels (see below).
- `Scenes/Main.unity` — the only scene; first in Build Settings. Contains the persistent objects listed above — when adding a manager/system, put it in the scene or a prefab, don't spawn it from code.

### Key game rules as implemented

- Cube exposure is **orientation-dependent**: the player drags horizontally to spin the shape, which snaps to one of four 90° yaw steps (`CubeShapeModel.Orientation`, 0..3). A cube is tappable unless all three camera-facing neighbours are occupied; the facing axes per orientation are `(+x,+y,+z)`, `(+x,+y,-z)`, `(-x,+y,-z)`, `(-x,+y,+z)` (`CubeShapeModel.FacingSigns`). The view must keep matching: cubes at local `(x, y, -z)` (grid z mirrored) under root rotation `Euler(-33, 45 + 90k, 0)` with the fixed ortho camera — change the mapping and the exposure rule no longer matches what the player sees. Orientation 0 is the authored view; level solvability is guaranteed there (rotation only ever adds options, so levels stay completable). `CubeView.SetDepthOrder` is a painter-order fallback recomputed per orientation; balls/debris spawn at `z = CubeShapeView.FrontZ` so they never fail the depth test against cube meshes.
- Every cube yields **9 balls**; every container has capacity 3 → **3 containers per cube** (the container queue is built so each cube's 3 same-color containers are consecutive in removal order). Balls reserve container space at spawn (`InFlight`), so simultaneous flights never overfill.
- Balls with no open matching container land in the shared tray (**capacity 45** — a forgiving danger meter). The HUD's big gray counter shows `trayCount / capacity`, **not** delivery progress. Landing a ball that pushes the tray *over* capacity = instant loss (`GameStatus.LostOverflow`). When a new container slides in, matching tray balls automatically transfer out (`GameSession.DrainSharedSlot`) — combined with the queue being a valid removal order, playing in queue order never overflows, so levels stay completable.
- Win = all balls delivered (total container capacity == total balls, enforced when generating levels); stars by remaining time (≥50% → 3, ≥25% → 2, else 1; thresholds live in `GameConfig`).
- Flying balls (`BallView`) carry a `TrailRenderer` comet trail (killed via `DetachTrail` when a ball parks in the tray). Completing a container plays a ghost fly-off; the incoming container + queue shift are **held `GhostFlyTime` (~0.34s)** so they don't move before the old one clears (`ContainerRowView._justCompleted` → `SetContainer(model, delay)`).

### Fixed GUIDs — never regenerate

- `GameBootstrap.cs`: `cb00b007a11ce0de000000000000cb01` (referenced by Main.unity)
- `Main.unity`: `cb005cee0000000000000000000000b2` (referenced by EditorBuildSettings.asset)
- `GameConfig.cs`: `c377b41122c353cf7bd59fa5bd56a918` (referenced by GameConfig.asset)
- `PaletteConfig.cs`: `362cd7c97c220adf72b06823bade7358` (referenced by PaletteConfig.asset)
- `GameConfig.asset`: `43d335f756a4b99106720ea6238acbee` (referenced by Main.unity)
- `PaletteConfig.asset`: `d4695687e0d952ed68571c832094bf6b` (referenced by Main.unity)

## Level generation

Levels are generated by Python (mirrors the C# exposure rule exactly): `generate_levels.py` + `validate_levels.py` (kept in the Claude session scratchpad; regenerate by re-writing them from this description if needed). Construction guarantees solvability: the container queue **is** a valid exposure-respecting removal order, so playing in queue order never touches the shared tray. Difficulty ramp: 7→36 cubes, 3→6 colors, ~5.2→3.2 s/cube time budget.

The shipped JSONs were then transformed (one-off script) to the current ball economy: each cube `ballCount` 3→**9**, each queued container expanded into **3 consecutive copies** (same color/capacity, preserving removal order → still solvable in queue order), and `sharedSlotCapacity`→**45**. Invariant enforced: `sum(cube.ballCount) == sum(container.capacity)` per level (so every ball has a home and the level is winnable). A backup of the pre-transform levels is in the session scratchpad (`levels_backup/`). If regenerating from scratch, bake these numbers into the generator directly rather than transforming afterward.

## Working with the project

- Open with **Unity Hub**, editor version `6000.3.9f1` exactly.
- **Unity MCP** (`com.coplaydev.unity-mcp`) is installed — when the Unity Editor is running, prefer MCP tools for scene manipulation, running tests, and reading console errors. When not connected, author files directly and write `.meta` files by hand (new files need a `.meta` with a random GUID; folders too).
- Compile check without Unity (works even while the editor is open) — build a response file with refs from `Assembly-CSharp.csproj` HintPaths **plus all `Library/ScriptAssemblies/*.dll`** (package assemblies incl. InputSystem and the DOTween module extensions in `Assembly-CSharp-firstpass`; exclude `Assembly-CSharp.dll` itself), then:
  ```
  <UNITY>/NetCoreRuntime/dotnet.exe <UNITY>/DotNetSdkRoslyn/csc.dll @refs.rsp
  ```
  with `-nostdlib -target:library` and the `Assets/_Project/Scripts` sources (Editor pass adds `-define:UNITY_EDITOR -r:UnityEditor.dll`). HintPaths alone miss the package DLLs. Note: passing refs on the command line overflows Windows' arg limit — always use an `@response` file.
- Input: **new Input System only** (`activeInputHandler: 1`) — use `Pointer.current`, and `InputSystemUIInputModule` on the EventSystem (plain `StandaloneInputModule` will throw).
- Tests use the **Unity Test Framework** — run via the editor Test Runner or Unity MCP.

## Key assets and packages

- **Rendering:** URP 17.3 configured for 2D (`Assets/Settings/Renderer2D.asset`). Runtime-created SpriteRenderers use the built-in unlit sprite material — no 2D lights needed.
- **Tweening:** DOTween Pro (`Assets/Plugins/Demigiant/`) — used for punch/scale/fade polish; ball flight is a hand-rolled bezier in `BallView.Update` (pauses with `Time.timeScale`). `GameManager.CleanupLevel` calls `DOTween.KillAll()` — keep persistent tweens out of gameplay code.
- **UI art:** Layer Lab **GUI Pro-CasualGame** pack (`Assets/Layer Lab/`) exists but is *not* used — game UI is procedural (`SpriteFactory`/`UIFactory`) so the game has zero asset dependencies. Swap in Layer Lab sprites later for a nicer look if desired.
- **Toon shading:** Toony Colors Pro 2 (`Assets/JMO Assets/`) — unused by the game.
- **Scenes:** `Assets/_Project/Scenes/Main.unity` is the game. `Assets/Scenes/SampleScene.unity` is the template default (kept as scene-YAML reference); `Assets/_Recovery/0.unity` is a recovery artifact, not a real scene.
