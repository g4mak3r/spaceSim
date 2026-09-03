# SpaceSim

A small Unity space-simulation prototype focused on procedural star-system generation, first-person ship navigation, diegetic HUD elements, and lightweight custom gravity.

> **Project status:** playable prototype / portfolio project. The repository is structured so a fresh clone can be opened directly in the pinned Unity version without committing generated Unity caches or IDE files.

## Highlights

- Deterministic streamed star-system generation around the player
- Generated stars, suns, and planets with lightweight kinematic orbits
- Constant-cost local space-dust field with particle recycling (no visible spawn boundary)
- Inertial ship flight with RIFTING cruise, hold-to-boost VECTOR thrust, reverse flight, and charged WARP
- Velocity-preserving turns, lateral drift, mode-dependent FOV, and restrained particle-streak feedback
- Diegetic navigation HUD with nearest-system direction and distance
- Procedural galactic star background
- Unity Input System integration
- URP rendering pipeline

## Requirements

- **Unity 6.0.64f1**
- Windows, macOS, or Linux for editing
- Git (optional, for cloning)

The exact editor version is pinned in `ProjectSettings/ProjectVersion.txt`.

## Run from source

1. Clone or download this repository.
2. Open Unity Hub.
3. Add the repository root as an existing project.
4. Open it with **Unity 6.0.64f1**.
5. On a fresh clone, the project automatically opens `Assets/Scenes/space.unity` when Unity starts with an empty scene.
6. Press **Play**.

The bootstrap never replaces an already-open saved scene. You can also open the gameplay scene manually through **SpaceSim > Open Main Scene**.

The gameplay scene is the only enabled scene in Build Settings.

## Controls

| Input | Action |
| --- | --- |
| `W` / Up Arrow | Throttle forward |
| Hold `Left Shift` / `Right Shift` | VECTOR boost (higher thrust, capped around 135 unit/s) |
| Hold `Space` while nearly stopped | Charge WARP (3 → 2 → 1); keep holding to remain in WARP |
| `S` / Down Arrow | Brake; once nearly stopped, reverse thrust |
| Mouse | Pitch / yaw with rotational inertia |
| `Esc` | Toggle cursor lock |

RIFTING is the normal heavy cruise mode. Releasing `W` preserves momentum with only minimal passive drag. Holding Shift temporarily engages VECTOR boost. WARP can only be charged at near-zero speed; releasing Space disengages it and begins deceleration, while `S` performs a stronger abort/brake and transitions into reverse once the ship is nearly stopped.

## Build

A small editor build helper is included:

**Unity menu:** `SpaceSim > Build > Windows x86_64`

It creates:

```text
Builds/Windows/SpaceSim.exe
```

`Builds/` is intentionally excluded from Git. For a public playable download, package the generated Windows build and attach it to a GitHub Release instead of committing binaries to the repository.

The same build can be invoked from Unity batch mode with:

```text
-executeMethod SpaceSim.Editor.BuildProject.BuildWindows
```

## Project structure

```text
Assets/
├── Editor/          # Editor-only build tooling
├── Materials/
├── Prefabs/         # Planet, sun, and star-system prefabs
├── Scenes/          # Main playable scene
├── Scripts/
│   ├── Core/        # High-level simulation orchestration
│   ├── Environment/ # Procedural background visuals
│   ├── Physics/     # Gravity and particle behavior
│   ├── Player/      # Ship input, movement, camera feedback
│   ├── Systems/     # Generated star-system behavior
│   ├── UI/          # HUD and terminal effects
│   └── Utils/       # Small reusable utilities
├── Settings/        # URP / renderer assets
└── Sprites/
```

## Architecture notes

The project deliberately stays lightweight. Gameplay remains component-oriented and Unity-native rather than introducing service containers or multiple assemblies for a prototype of this size.

Key boundaries are:

- `GalaxyManager` streams a bounded number of deterministic systems around the player and unloads distant systems.
- `StarSystem` owns deterministic system content, cheap kinematic planet orbits, and its proximity trigger.
- `ShipController` reads player input; `ShipMotor` owns ship movement state.
- `ShipHUD` consumes simulation/player state and handles presentation only.
- `SpaceDustField` keeps two periodic world-space particle layers around the player; particles remain stationary at rest and wrap only at distant field boundaries.
- `GravityBody` remains available for isolated physics experiments, but generated systems intentionally use cheap kinematic orbits.

## Repository hygiene

Only source assets and Unity project settings belong in Git. Generated folders such as `Library`, `Temp`, `Logs`, `UserSettings`, IDE project files, and local builds are excluded through `.gitignore`.

Unity `.meta` files **must remain committed** because their GUIDs preserve scene and prefab references.
