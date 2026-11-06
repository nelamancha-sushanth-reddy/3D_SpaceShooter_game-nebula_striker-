# Nebula Striker — Game Design Document
*GMDD Course Project*

## 1. Overview
| Item | Description |
|---|---|
| Title | Nebula Striker |
| Genre | 3D rail / arcade space shooter (shoot 'em up) |
| Platform | PC (Windows / macOS / Linux), Unity Built-in RP |
| Engine | Unity 2022.3 LTS / Unity 6 |
| Target session | 5–15 minutes, endless waves with high-score chase |
| Camera | Third-person chase camera behind the ship |

**High concept:** Pilot a lone starfighter through a living nebula, surviving escalating waves of alien
craft and destroying a Dreadnought at the end of every sector.

## 2. Core gameplay loop
1. **Fly & aim** – the ship moves on an X/Y combat plane; enemies approach from deep space (+Z).
2. **Shoot** – twin lasers converge on the aiming reticle 45 units ahead.
3. **Survive** – dodge plasma fire and rams using movement and the barrel roll.
4. **Collect** – destroyed enemies may drop power-ups.
5. **Progress** – clear the wave → next, harder wave; every 5th wave a boss fight; sector colours change.
6. **Fail state** – hull reaches 0 → Game Over → retry to beat the high score.

## 3. Mechanics
### Player
| Mechanic | Value / behaviour |
|---|---|
| Hull | 100 HP; 1.1 s invulnerability after a hit (blinking) |
| Movement | 20 u/s with exponential smoothing (inertia), bounded play-field ±15 × ±8.5 |
| Primary fire | Alternating wing cannons, 0.12 s interval, 10 dmg, 115 u/s |
| Barrel roll | 0.45 s roll, lateral dash, invulnerable, 1.4 s cooldown |
| Nova Bomb | Starts with 3 (max 5). Shockwave: 120 dmg to all enemies (150 to boss) and erases enemy shots |

### Enemies
| Enemy | HP (base) | Score | Behaviour |
|---|---|---|---|
| Drone | 30 | 100 | Weaves toward player; spawns solo or in 5-ship V formations |
| Gunship | 60 | 250 | Holds at 45–70 u, tracks player, fires aimed bolts, then breaks off |
| Kamikaze | 20 | 150 | Homing mine with limited turn rate; 30 contact damage |
| Asteroid | 12 × size | 20 × size | Tumbling obstacle, also on the menu screen |
| Dreadnought | 1400 (+60 % per sector) | 5000 × sector | Patterns: radial fan, aimed burst, summon escorts. Enrages < 50 % |

Enemy HP is multiplied by **Difficulty = 1 + 0.12 × (wave − 1)**; spawn quota = 6 + 2 × wave; spawn
interval shrinks with wave number. Gunships appear from wave 2, kamikazes from wave 3.

### Power-ups (drop chance 6–20 % depending on enemy)
| Pickup | Colour | Effect |
|---|---|---|
| Hull Repair | Green | +35 HP |
| Rapid Fire | Gold | 9 s: ~2× fire-rate + extra spread shots |
| Shield | Blue | 8 s: absorbs all damage |
| Nova Bomb | Pink | +1 bomb |

## 4. Level / world design
- **Rail structure:** the illusion of forward flight is created by star streaks, dust and enemies moving toward the camera, while the player stays on the Z = 0 plane. This keeps aiming readable in 3D.
- **Sectors:** four nebula palettes; the sky, ambient light and key light colour cross-fade every 5 waves, giving a sense of travel and marking boss completion.
- **Background:** procedural skybox + gas giant with atmosphere rim glow and rings + moon for scale and parallax.

## 5. Art direction
- Sci-fi neon: dark hulls with strong coloured **rim lighting** so silhouettes read against the bright nebula.
- Colour language: **player = cyan/blue**, **enemies = red / magenta / orange**, **pickups = colour-coded**.
- All geometry is composed from primitives by `ShipFactory`; additive glow materials for engines, lasers, shields.

## 6. Audio
Procedurally synthesised (no files): sweeping square/sine lasers, filtered-noise explosions with
sub thump, arpeggiated pickup chime, tremolo shield, deep bomb sweep, and a 16-second ambient loop
(Am–F–C–G pads, pulse bass, arpeggio).

## 7. UI / UX
- **Title screen** with controls and best score.
- **HUD:** score, wave, best, hull bar (colour shifts red when low), bomb count, power-up timers, roll readiness.
- **Boss bar** during Dreadnought fights; **wave banners**; **pickup toasts**; **damage flash**.
- **Pause** and **Game Over** (score, wave reached, new-record badge).

## 8. Technical design
| Pattern / technique | Where |
|---|---|
| Singleton + finite-state machine | `GameManager` (Menu, Playing, Paused, GameOver) |
| Inheritance / template method | `EnemyBase.Tick()` overridden by each enemy |
| Composition | Shared `Health` component for player, enemies, asteroids (team-based) |
| Factory | `ShipFactory` builds all entities procedurally |
| Flyweight / caching | `MaterialLibrary` shares materials between identical parts |
| Coroutines | Wave sequencing, boss bursts, boss death chain, game-over delay |
| Sphere-cast projectiles | `Projectile` – no tunnelling at high speed, no per-bullet rigidbody |
| Kinematic triggers | Player trigger collider detects rams & pickups |
| Custom shaders (HLSL/CG) | Procedural nebula skybox, rim-lit hull shader, additive glow |
| Runtime particles | Explosions, sparks, engine flames, starfield configured in code |
| Procedural audio | `AudioClip.Create` + DSP (oscillators, envelopes, one-pole filters) |
| Persistence | High score via `PlayerPrefs` |
| Editor tooling | `NebulaProjectSetup` – auto scene creation, menu commands |

## 9. Possible extensions
Object pooling for projectiles, controller support via the new Input System, post-processing bloom,
additional bosses, local leaderboards, ship upgrades between sectors.
