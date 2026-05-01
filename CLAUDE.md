# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Beereels** — a Unity VR drive-and-don't-die game for Meta Quest. The player drives a car while being tempted to look at their phone (reels) and drink beer. Looking at the phone gives points, looking at the road costs points, and hitting an obstacle ends the run.

- Unity editor version: **6000.1.5f1** (locked — see `ProjectSettings/ProjectVersion.txt`).
- Render pipeline: URP (`com.unity.render-pipelines.universal` 17.1.0).
- XR stack: **Meta XR SDK** (`com.meta.xr.sdk.all` 201.0.0) on top of OpenXR + `com.unity.xr.androidxr-openxr`. `com.unity.xr.interaction.toolkit` is installed but the gameplay scripts use **OVR APIs (`OVRHand`, `OVRCameraRig`) directly**, not XRI — match that style when adding to existing systems.
- Build target: **Android** (Quest). The single scene listed in `EditorBuildSettings.asset` is `Assets/Scenes/SampleScene.unity`; `BasicScene.unity` also exists but is not in the build list.

## Working in this repo

There is no CLI build/test tooling — everything goes through the Unity Editor.

- Open the project: launch Unity Hub, add `repo/` as a project, open with editor `6000.1.5f1` exactly. A different patch version will silently re-import and may modify `Packages/packages-lock.json` / `ProjectSettings/`.
- Play the game: open `Assets/Scenes/SampleScene.unity` and press Play (Quest Link / Air Link required for hand tracking + the steering wheel — desktop play won't drive the car).
- Build for Quest: `File → Build Settings → Android → Build`. Output is gitignored under `/Build/` or `/Builds/`.
- There is no test suite, no linter, and no CI. Don't invent commands for them.

## Repository layout

Project-authored gameplay code is a **flat set of MonoBehaviours at `Assets/*.cs`** (7 files total). Everything else under `Assets/` is third-party Asset Store content (BOXOPHOBIC, BrokenVector, Farland Skies, Nicrom, Pack_Pickup, PolyRonin, Oculus, InteractionSDK, XR, XRI, VRTemplateAssets, TextMesh Pro, Plugins, Samples). **Do not edit anything inside those vendor folders** — treat them as read-only dependencies.

Prefabs in `Assets/`: `car-1/2/3.prefab`, `Obstacle.prefab`, `RoadSegment.prefab`. Gameplay scripts reference these prefabs via Inspector wiring on scene objects, not via `Resources.Load` — so renaming/moving a prefab requires re-wiring in the scene.

## Gameplay architecture

The whole game runs on a single trick: **the car never moves; the world moves toward the car.** Understanding this is required before changing any movement, spawning, or collision code.

- `CarController` (on the car) reads `_currentSpeed` and pushes the `RoadGenerator` transform along `Vector3.back` each frame. Steering pushes the same transform along `Vector3.right`. The car's own transform is essentially static.
- `RoadGenerator` spawns `RoadSegment` prefabs at fixed local Z offsets (`segmentLength * index`) parented under itself, then recycles oldest segments out of a `Queue<GameObject>`. Because the parent transform is what moves, children are positioned in **local** space — see the comment at `RoadGenerator.cs:63`.
- Obstacles are spawned by `RoadGenerator.SpawnObstacle` and **re-parented to the RoadSystem (not the segment)** so they travel with the world. `ObstacleAutoDestroy` finds `OVRCameraRig` and self-destructs once world-Z drops below `player.z - destroyDistance`. If you change parenting or add a different player rig, fix both ends.
- `Obstacle.OnTriggerEnter` calls `GameManager.Instance.TriggerGameOver()` when the collider tagged `Player` enters. The car's collider must keep the `Player` tag.

### Game state — `GameManager`

- Singleton via `GameManager.Instance` (set in `Awake`, duplicates self-destroy). Other scripts call into it directly; do not look it up with `FindObjectOfType` for state reads.
- Score model: continuous `pointsPerSecondAlive`, bonus `pointsPerReelSecond` while `_isLookingAtPhone`, penalty `penaltyPerRoadSecond` while `_isLookingAtRoad`, discrete `AddBeerPoints()` on beer sip. Score floored at 0.
- `TriggerGameOver()` sets `Time.timeScale = 0f` and fires the `onGameOver` `UnityEvent`. **`CarController` and any other gameplay script must early-out on `IsGameOver`** (CarController already does — copy that pattern).
- `RestartGame()` reloads the active scene by name; `GameOverUI.RestartGame()` does the same thing — they're duplicated, restart-from-UI doesn't go through the GameManager.

### Look detection (referenced but not implemented)

`GameManager.SetLookingAtPhone` / `SetLookingAtRoad` are public hooks for a gaze-detection system that doesn't exist in `Assets/*.cs` yet. If you add it, drive those setters from whatever gaze/raycast component you build — don't add gaze logic into GameManager itself.

### Steering wheel — `SteeringWheel`

- Uses **Meta hand tracking** (`OVRHand`), not controllers and not XRI. Grab is detected by (a) hand position inside `grabRadius` of the wheel in the wheel's local XY plane and within `depthTolerance` on Z, AND (b) index-finger `GetFingerPinchStrength > pinchThreshold`.
- Two-handed grab measures angle between hands; one-handed grab measures angle from wheel center to that hand. When grab count changes, `_grabReferenceAngle` and `_wheelAngleOnGrab` are rebased to avoid wheel snap — preserve that rebase if you refactor `CalculateWheelRotation`.
- When released, the wheel returns to center with a critically-damped spring (`returnSpeed`, `returnDamping`).
- Output to the car: `_currentWheelAngle / wheelMaxRotation * maxSteerAngleOut → CarController.SetSteerAngle`. `CarController` then `Mathf.Lerp`s its own `_currentSteerAngle` toward that target — so there are **two layers of smoothing** between hand motion and lateral world translation. Tune one at a time.

### UI

`GameOverUI` is the only UI script. It hides its panel on `Start`, subscribes to `GameManager.Instance.onGameOver` (so GameManager must exist before GameOverUI's `Start` runs — guarantee this with script execution order or scene-object ordering), and uses **TextMeshPro** for labels.

## Conventions to match

- Inspector-driven wiring: most cross-script references are public/`SerializeField` fields set in the scene, not found at runtime. Prefer adding a public field over a `FindObjectOfType` call. The existing `FindObjectOfType` calls in `CarController.Start` and `ObstacleAutoDestroy.Start` are the exception, not the pattern to extend.
- Comments and inspector labels in this codebase are in **Spanish** (e.g. `[Header("Velocidad")]`, `// Movemos la calle hacia atr�s`). Match that when editing existing files; new files can follow whatever language the user prefers.
- The `.cs` files contain mojibake (e.g. `atr�s` instead of `atrás`) because they were saved without a UTF-8 BOM. Don't "fix" those characters in unrelated lines — only touch what your task requires, or you'll produce a diff full of encoding-only changes.
- Every `.cs`, prefab, mat, and asset has a paired `.meta` file. **Never delete a `.meta` without deleting its asset, and never add an asset without letting Unity generate the `.meta`** (open the project in the editor once after adding files via tooling).
