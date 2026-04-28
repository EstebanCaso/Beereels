# Beereels UI — Editor Setup Guide

The C# scripts in `Assets/UI/` are complete. The remaining work is **Unity Editor wiring** (scenes, prefabs, build settings). Follow these steps in order.

## 0. Prerequisites

- Unity **6000.1.5f1** open on this project.
- Quest Link / Air Link connected if you want to test hand tracking in the Editor.
- Confirm `Assets/UI/` contains: `OVRHandPointer.cs`, `UIPointerEventSystem.cs`, `MainMenuController.cs`, `PauseController.cs`, `HUDController.cs`. Wait for Unity to finish compiling — Console must show no errors.

## 1. Build the `HandPointer` prefab

This is the visual + interaction pointer attached to each hand.

1. In Hierarchy: `Create Empty` → name it `HandPointer`.
2. Add child `Create Empty` → name it `Line`. Add component `Line Renderer`. Settings:
   - Width: `0.005`
   - Material: a simple unlit material (color `#22D3EE` cyan, alpha 0.8). Create one as `Assets/UI/Materials/PointerLine.mat` if needed.
   - Use World Space: ✓
   - Positions: 2 (will be overwritten at runtime)
3. Add child `3D Object → Sphere` → name it `Reticle`. Scale `(0.01, 0.01, 0.01)`. Remove its `SphereCollider`. Material: same cyan unlit.
4. On the root `HandPointer`, add component `OVR Hand Pointer`. In the inspector:
   - `Aim Line` → drag the `Line` child's `LineRenderer`.
   - `Reticle` → drag the `Reticle` child's `Transform`.
   - Leave `Hand` empty for now (assigned per-hand below).
5. Drag `HandPointer` from Hierarchy into `Assets/UI/Prefabs/HandPointer.prefab` (create the `Prefabs` folder if missing). Delete the scene instance.

## 2. UI panel base prefabs (visual style: phone-screen vibe)

Common settings for **every world-space Canvas** below:
- `Render Mode`: World Space
- `Event Camera`: leave empty (we drive events with `OVRHandPointer`, no camera ray needed)
- Canvas `Scaler`: `Constant Pixel Size`, `Reference Pixels Per Unit` 100
- Add component `Graphic Raycaster` (Unity adds it automatically; verify it's enabled)
- RectTransform width × height: ~`800 × 600`, then set the GameObject's transform `Scale` to `(0.0015, 0.0015, 0.0015)` so the panel is ~1.2 m wide

For all text:
- Font asset: `Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset`
- For titles: enable `Outline` on the TMP material, color `#22D3EE` (cyan), thickness ~0.2

For all panels:
- Background `Image`: rounded sprite (Unity's built-in `UISprite` works as fallback). Color `#0E0E14` alpha `235/255`.
- Accent color (button fill): `#FF2D7B` (neon pink)
- Text color: `#F5F5FA` (off-white)
- Add an `Audio Source` to the panel root with `Play On Awake` off. Drag `Assets/VRTemplateAssets/Audio/Button_22_click.wav` into a public field, then on each Button's `OnClick` add a second event that calls `AudioSource.PlayOneShot(clickClip)`.

### 2a. `MainMenuCanvas.prefab`
Hierarchy:
```
MainMenuCanvas (Canvas + GraphicRaycaster + MainMenuController + AudioSource)
├── Background (Image)
├── Title (TMP) — "BEEREELS" — large, cyan outline
├── Tagline (TMP) — "Don't crash. Watch the reels."
├── StartButton (Button + Image + child TMP "Start")
└── QuitButton  (Button + Image + child TMP "Quit")
```
Wire:
- `StartButton.onClick` → `MainMenuController.OnStart`
- `QuitButton.onClick` → `MainMenuController.OnQuit`

Save as `Assets/UI/Prefabs/MainMenuCanvas.prefab`.

### 2b. `HUDCanvas.prefab`
Smaller, no background (or very subtle). Just a single TMP showing score.
```
HUDCanvas (Canvas + GraphicRaycaster + HUDController)
└── Score (TMP) — large, anchored center
```
- Set Canvas world scale `(0.001, 0.001, 0.001)` so the readout is small.
- On `HUDController`, leave `speedText` and `car` empty unless you want speed too.

Save as `Assets/UI/Prefabs/HUDCanvas.prefab`.

### 2c. `PausePanel.prefab`
```
PausePanel (Canvas + GraphicRaycaster + AudioSource)
├── Background (Image)
├── Title (TMP) — "Paused"
├── ResumeButton    → PauseController.Resume()
├── RestartButton   → PauseController.RestartRun()
└── MainMenuButton  → PauseController.BackToMainMenu()
```

The `PauseController` itself is **not** on this prefab — it lives on a separate scene object that references this panel. (See step 4.)

Save as `Assets/UI/Prefabs/PausePanel.prefab`.

### 2d. `GameOverPanel.prefab`
```
GameOverPanel (Canvas + GraphicRaycaster + GameOverUI + AudioSource)
├── Background (Image)
├── Title (TMP) — "Game Over"
├── ScoreText (TMP)        ← assign to GameOverUI.scoreText
├── TimeText  (TMP)        ← assign to GameOverUI.timeText
├── RestartButton   → GameManager.Instance.RestartRun()  (use a runtime ref, see note)
└── MainMenuButton  → GameManager.Instance.LoadMainMenu()
```
Wire `GameOverUI.gameOverPanel` → drag the root Canvas object.

**Note on the buttons:** Unity's UnityEvent inspector can't bind to `GameManager.Instance.X` because Instance is static. Easiest workaround: on the panel root, add a tiny passthrough MonoBehaviour OR wire the buttons to `PauseController.Instance.RestartRun` / `BackToMainMenu` (since those wrap the same calls, and PauseController exists in the scene). That's what the panel buttons should call.

Save as `Assets/UI/Prefabs/GameOverPanel.prefab`.

### 2e. `WristPauseButton.prefab`
A tiny world-space canvas that lives on the player's left wrist.
```
WristPauseButton (Canvas + GraphicRaycaster)
└── Btn (Button + Image, e.g. ≡ icon)
     └── Label (TMP) — "Pause"
```
- Canvas scale `(0.0005, 0.0005, 0.0005)` (≈4 cm wide).
- `Btn.onClick` → `PauseController.Instance.Toggle` (set in scene at step 4 since we need the scene's PauseController instance).

Save as `Assets/UI/Prefabs/WristPauseButton.prefab`.

## 3. Build the `MainMenu.unity` scene

1. `File → New Scene` → `Basic (URP)` template → save as `Assets/Scenes/MainMenu.unity`.
2. Delete the default Camera (we use OVRCameraRig).
3. Drag `Assets/Oculus/.../OVRCameraRig.prefab` (or whatever rig SampleScene uses — copy from there) into the scene at origin. **Important:** match the same rig SampleScene uses so hand tracking is identical.
4. Under `LeftHandAnchor` and `RightHandAnchor` (or wherever the OVRHandPrefabs sit), drop one `HandPointer.prefab` instance per hand. On each, set `Hand` to that side's `OVRHand`.
5. `Create Empty` → name `EventSystem` → add component `EventSystem` → add component `UIPointerEventSystem`. (Unity auto-adds a `StandaloneInputModule`; the bootstrap disables it.)
6. Drop `MainMenuCanvas.prefab` ~1.5 m in front of the rig at eye height. Rotate so it faces the rig.
7. Optional: drag `Assets/car-1.prefab` into the scene as decoration (rotate slightly so it looks dynamic).
8. Save.

## 4. Modify `SampleScene.unity`

Open `Assets/Scenes/SampleScene.unity`.

1. Under `LeftHandAnchor` / `RightHandAnchor`: add one `HandPointer.prefab` per hand. Assign `Hand` field per side.
2. `Create Empty` → `EventSystem` → add `EventSystem` + `UIPointerEventSystem`.
3. Under `OVRCameraRig/CenterEyeAnchor`: drop `HUDCanvas.prefab`. Position it `(0, -0.15, 0.6)` local (slightly below center, 60 cm forward). Rotate to face the camera.
4. Under `OVRCameraRig` (or as a scene root): drop `PausePanel.prefab` ~1.5 m forward at eye height. Disable it (its `SetActive(false)` happens in `Start`, but disable in scene too so it never flashes on load).
5. `Create Empty` → name `PauseController` → add component `PauseController`. Drag the `PausePanel` instance into its `Pause Panel` field. Optionally drag the `HUDCanvas` instance into `Hud To Hide`.
6. Wire the three buttons inside `PausePanel`:
   - Resume → `PauseController.Instance` (drag the scene PauseController GameObject) → `Resume()`
   - Restart → same → `RestartRun()`
   - Main Menu → same → `BackToMainMenu()`
7. Under `LeftHandAnchor`: drop `WristPauseButton.prefab`. Position it on the back of the wrist, facing outward (e.g., local `(0, 0.04, -0.05)`, rotate so palm-up reveals it). Wire its button's `onClick` → `PauseController.Toggle`.
8. As a scene root: drop `GameOverPanel.prefab` at the same position as the pause panel. Disable in scene. On its `GameOverUI` component, wire `gameOverPanel` (self), `scoreText`, `timeText`.
9. **Wire `GameManager.onGameOver`:** select the `GameManager` GameObject. In the Inspector under `GameManager → On Game Over ()`, click `+`, drag the `GameOverPanel` instance (or the `GameOverUI` script reference), pick `GameOverUI.ShowGameOver`.
10. Wire the GameOver panel's two buttons → `PauseController.Instance.RestartRun` / `.BackToMainMenu`.
11. Save.

## 5. Build settings

`File → Build Profiles → Scene List`:
1. Add `Assets/Scenes/MainMenu.unity` and **drag it to index 0**.
2. Confirm `Assets/Scenes/SampleScene.unity` is at index 1.
3. Remove `BasicScene` if it shows up — it's unused.

## 6. Smoke test (Editor)

1. Open `MainMenu.unity`. Press Play. Verify: no Console errors, the menu canvas is visible from the rig's POV.
2. Without hand tracking you can't pinch-click — temporarily add `Standalone Input Module` to the EventSystem and click with the mouse if you want a flat-mode smoke test.
3. With Quest Link, verify the cyan ray + reticle land on the buttons and pinching the index finger triggers Start.

## 7. End-to-end (Quest)

1. Build & deploy to headset.
2. Launch → MainMenu loads. Pinch Start → SampleScene with HUD ticking up.
3. Pinch the wrist button → game freezes, PausePanel appears. Resume / Restart / Main Menu all work.
4. Drive into an obstacle → GameOverPanel appears with score+time. Restart / Main Menu work.

## Desktop preview (no VR headset)

You can preview the UI flow in the Unity Editor's Game view without a Quest using two helpers:

- `Assets/UI/EditorMousePointer.cs` — mouse-driven counterpart of `OVRHandPointer`. Aims with the mouse, clicks with left mouse button. Auto-disables itself if it detects a tracked OVR hand, so you can leave it in the scene permanently.
- `Assets/UI/DesktopDebugInput.cs` — keyboard shortcuts so you can test flow without driving:
  - `P` toggle pause
  - `G` force Game Over (to preview that panel)
  - `R` restart run
  - `M` back to main menu

### Setup (do this in both `MainMenu.unity` and `SampleScene.unity`)

1. `Create Empty` → name it `DesktopPreview`.
2. Add component `Editor Mouse Pointer`.
   - Leave `Reference Camera` empty (it falls back to `Camera.main`, which is the OVR center-eye camera).
   - Optionally drag a child `LineRenderer` and a small sphere into `Aim Line` and `Reticle` for visual feedback (same as `HandPointer.prefab`).
3. In `SampleScene` only, also add component `Desktop Debug Input` to the same GameObject.
4. Make sure the OVRCameraRig's center-eye camera has the **MainCamera** tag (it usually does by default). Without `Camera.main`, the mouse pointer can't compute a ray.

### How to preview

1. Open `MainMenu.unity` → Press Play. Move the mouse over **Start** / **Quit** in Game view, click. Verify Start loads `SampleScene` and Quit exits Play mode.
2. In `SampleScene`, after Play:
   - HUD should show score ticking up.
   - Press `P` → pause panel appears (you can also click its buttons with the mouse). Press `P` again to resume.
   - Press `G` → game-over panel appears with score + time. Click Restart or Main Menu, or press `R` / `M`.
3. Without VR you can't grip the steering wheel, so the car won't actually drive — that's fine for UI preview. The road still scrolls because `CarController.Update` accelerates from `baseSpeed = 15f` regardless of input.

### Build to PC (optional, for sharing)

If you want a non-VR build to share for visual feedback:
1. `File → Build Profiles` → switch active platform to `Windows, Mac, Linux`.
2. Make sure XR Plug-in Management is **disabled** for Standalone (Edit → Project Settings → XR Plug-in Management → uncheck the Standalone tab plugins). The OVR scripts will no-op without VR initialized.
3. Build & run the .exe — same mouse + keyboard controls work.

## Troubleshooting

- **Pinch click misses the button:** The Graphic must have `Raycast Target` checked and `depth >= 0`. If hovering shows the reticle on the panel but click does nothing, check that the EventSystem GameObject has `UIPointerEventSystem` (which disables StandaloneInputModule — otherwise it competes for events).
- **GameOver buttons do nothing:** UnityEvent fields can't reference `GameManager.Instance.X` directly (static). Wire them to the scene's `PauseController` (which has wrappers `RestartRun` and `BackToMainMenu`) — see step 4.10.
- **HUD doesn't update during pause:** Expected. `Time.timeScale = 0` doesn't stop `Update`, but the GameManager's score-accumulating code is gated on `IsGameOver` only. If you want the score frozen visually too, the current code already does that — it just stops increasing because Time.deltaTime becomes 0.
- **Ray clips into the wheel/car:** Increase `OVRHandPointer.maxRayDistance` only if menus are far. For close UI (HUD, wrist), consider a separate near-pointer pass — out of v1 scope.
