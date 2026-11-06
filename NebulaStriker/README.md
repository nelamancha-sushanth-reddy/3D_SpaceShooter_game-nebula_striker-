# NEBULA STRIKER — 3D Space Shooter (Unity)

A complete 3D rail-style space shooter: fly a starfighter through a procedurally rendered nebula,
blast enemy drones, gunships and kamikaze mines, collect power-ups, and take down a Dreadnought
boss every 5th wave.

Everything — ships, nebula sky, particles, UI, sound effects and music — is generated **from code
and custom shaders**, so the project has no external asset dependencies and opens cleanly.

---

## 1. Opening the project (Unity Hub)

1. Unzip `NebulaStriker.zip` somewhere with a short path (e.g. `D:\Unity\NebulaStriker`).
2. Open **Unity Hub → Projects → Add → Add project from disk** and select the `NebulaStriker` folder
   (the one containing `Assets`, `Packages`, `ProjectSettings`).
3. Open it with **Unity 2022.3 LTS** (made for 2022.3.62f1) or **Unity 6 (6000.x)**.
   If Hub says the version differs, choose your installed version and accept the upgrade.
4. First import takes a few minutes. The editor script automatically creates
   `Assets/Scenes/Main.unity` and adds it to Build Settings.
5. Press **Play** (or use the menu **Nebula Striker → Play Game**).

> Render pipeline: **Built-in**. Do not convert to URP/HDRP – the custom shaders are written for Built-in.

### Troubleshooting
| Symptom | Fix |
|---|---|
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class…` | Edit → Project Settings → Player → Other Settings → **Active Input Handling = Both** (or *Input Manager (Old)*), then restart Unity. The editor script also offers to do this. |
| Pink/magenta objects | Make sure the project is on the Built-in pipeline (no URP asset under Project Settings → Graphics). |
| Scene missing | Menu **Nebula Striker → Rebuild Main Scene**. |
| Game runs in any scene | Intentional – `GameBootstrap` spawns the `GameManager` automatically. |

---

## 2. Controls

| Action | Keys |
|---|---|
| Fly | **W A S D** / Arrow keys |
| Fire twin lasers | **Space** / Left mouse / J |
| Nova Bomb (screen clear) | **B** / Right mouse / L |
| Barrel roll (dodge, brief invulnerability) | **Shift** / K |
| Pause / Resume | **Esc** / P |
| Quit to menu (while paused) | **Q** |
| Start / Retry | **Enter** |

---

## 3. Gameplay features

- **Realistic animated nebula** – raymarch-free procedural skybox shader: domain-warped fractal noise
  gas clouds, dark dust lanes, glowing cores, galactic band and 3 layers of twinkling stars.
  The palette cross-fades to a new "sector" every 5 waves (Violet Veil → Emerald Drift → Crimson Forge → Glacier Rift).
- **Parallax depth** – hyperspace star streaks, drifting space dust, distant ringed gas giant and moon.
- **Player starfighter** – inertial movement, banking/pitching, converging twin lasers with
  aiming reticles, engine exhaust particles, barrel-roll dodge, i-frames with blink.
- **Enemies**
  - *Drone* – weaving saucer, often in V-formations.
  - *Gunship* – holds range, strafes, fires aimed plasma.
  - *Kamikaze mine* – homing spiked mine.
  - *Asteroids* – tumbling procedurally deformed rocks.
  - *Dreadnought boss* (every 5th wave) – radial plasma fans, aimed bursts, summons escorts, enrages at 50% HP.
- **Power-ups** – Hull repair, Rapid fire (spread shot), Shield bubble, extra Nova Bomb; magnetised to the player.
- **Juice** – explosions (fireball + sparks + flash + debris), hit flashes, trauma-based screen shake,
  damage vignette, wave banners, pickup toasts.
- **Procedural audio** – lasers, explosions, shields, bombs, UI blips and a looping ambient synth track,
  all synthesised at runtime.
- **Progression** – escalating waves and difficulty scaling, score, persistent high score (PlayerPrefs),
  menu / pause / game-over flow.

---

## 4. Project structure

```
NebulaStriker/
├── Assets/
│   ├── Editor/NebulaProjectSetup.cs      Auto-creates Main scene, menu items, input check
│   ├── Resources/Shaders/
│   │   ├── NebulaSkybox.shader           Procedural nebula + stars sky
│   │   ├── ShipRim.shader                Hull shading (diffuse + spec + rim + emission + hit flash)
│   │   └── Additive.shader               Lasers, glows, shields, particles
│   └── Scripts/
│       ├── Core/        GameManager (state machine, score), WaveSpawner, GameBootstrap
│       ├── Player/      PlayerController (movement/roll/shield), PlayerWeapons (lasers/bomb)
│       ├── Enemies/     EnemyBase (abstract) → Drone, Fighter, Kamikaze, Asteroid, Boss
│       ├── Combat/      Health (shared HP component), Projectile (sphere-cast bullets)
│       ├── Pickups/     PowerUp
│       ├── Builders/    ShipFactory (procedural models), MaterialLibrary (material cache)
│       ├── Effects/     FX, NebulaController, SpaceEnvironment, Spinner, FlashFX, Debris, FXRunner
│       ├── CameraSystem/ChaseCamera (follow + shake)
│       ├── Audio/       Sfx (procedural synth)
│       └── UI/          UIManager (uGUI built in code)
├── Packages/manifest.json
├── ProjectSettings/ProjectVersion.txt     (Unity regenerates the remaining settings)
└── Docs/Game_Design_Document.md
```

---

## 5. Building an executable
File → Build Settings → (Main scene is already listed) → choose PC/Mac/Linux → **Build**.
