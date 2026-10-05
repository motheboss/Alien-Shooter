# Alien Shooter – Unity 2D (MVP + every extension)

Works with Unity 2021.3 LTS, 2022 LTS, and Unity 6 (2D or 2D URP/Built-in template). Works with the old
Input Manager, the new Input System, or both. **No external assets**: sprites, sounds, music, particles
and UI are all generated in code.

## Quick start (recommended: one click)

1. Create a new project from the **2D (Built-in Render Pipeline)** template.
2. Copy the `Assets` folder from this package into your project's `Assets` folder (merge).
3. Wait for compilation, then choose **Tools > Alien Shooter > Build Game Scene**.
   This generates the sprite PNGs, the four prefabs, the scene `Assets/Scenes/AlienShooter.unity`,
   and adds it to Build Settings.
4. Press **Play**. Press **Enter**/**Space** or click **PLAY**.

Controls: `A`/`D` or `←`/`→` move, `Space` (or left mouse) shoots, `Enter`/`Space` start/restart, `Esc` back to menu from game over.

> If you use URP 2D and the sprites look black, add a **Global Light 2D** (GameObject > Light > Global Light 2D)
> or assign the *Sprite-Unlit-Default* material to the prefabs' SpriteRenderers.

## Folder structure

```
Assets/
├── Editor/
│   └── AlienShooterSceneBuilder.cs   // Tools > Alien Shooter > Build Game Scene
├── Scripts/
│   ├── GameManager.cs        // state (Menu/Playing/GameOver), score, wave, high score
│   ├── PlayerController.cs   // movement, shooting, health, power-ups
│   ├── Bullet.cs
│   ├── Enemy.cs              // Normal / Fast / Tank / ZigZag
│   ├── EnemySpawner.cs       // wave system + power-up drops
│   ├── PowerUp.cs            // Rapid fire / Triple shot / Heal
│   ├── UIManager.cs          // builds HUD, menu, game-over UI at runtime
│   ├── AudioManager.cs       // synthesised SFX + music
│   ├── ParticleEffects.cs    // explosion / impact / pickup bursts
│   ├── ScrollingBackground.cs// parallax stars + nebula
│   ├── CameraShake.cs
│   ├── SpriteFactory.cs      // square / circle / triangle sprites
│   ├── GameInput.cs          // input wrapper (old, new, or both)
│   └── GameUtil.cs           // screen bounds, find helper
├── Sprites/    (created by builder: Square.png, Circle.png, Triangle.png)
├── Prefabs/    (created by builder: Player, Bullet, Enemy, PowerUp)
└── Scenes/     (created by builder: AlienShooter.unity)
```

## Scene hierarchy (what the builder creates)

```
Main Camera        Camera (Orthographic, size 5, Solid Color), AudioListener, CameraShake
Background         ScrollingBackground
Player             (Player prefab)  pos (0, -3.8, 0)
  └─ FirePoint     local pos (0, 0.6, 0)
EnemySpawner       EnemySpawner     pos (0, 6, 0)  (red gizmo bar marks the spawn line)
GameSystems        GameManager, AudioManager, UIManager
```
`UIManager` creates `Canvas` (Screen Space Overlay, Scale With Screen Size 1920×1080) and an `EventSystem` at runtime:

```
Canvas
├── HUD      Score, Best, Wave, HullLabel, HealthPips, PowerUps, WaveBanner, Toast
├── MenuPanel        Title, Subtitle, MenuBest, PlayButton, QuitButton, Controls
└── GameOverPanel    GameOverTitle, Reason, FinalScore, FinalWave, FinalBest, NewRecord, RestartButton, MenuButton, Hint
```

## Inspector reference (manual setup, if you don't use the builder)

No custom **Tags** or **Layers** are required; scripts identify objects by component. Leave everything on `Default`.
All sprite fields may be left empty on runtime-created objects; scripts fall back to generated shapes.

| GameObject | Components | Key settings |
|---|---|---|
| **Main Camera** | Camera, AudioListener, CameraShake | Tag `MainCamera`; Projection **Orthographic**; Size **5**; Clear Flags **Solid Color**; BG `#050514`; Position (0,0,-10) |
| **Player** (prefab) | SpriteRenderer (Triangle.png, color cyan, order 10), Rigidbody2D, BoxCollider2D, PlayerController | RB2D **Kinematic**, Gravity 0; collider **Is Trigger**, size 0.8×0.8; assign **Bullet Prefab** and **Fire Point** (child at 0,0.6) |
| **Bullet** (prefab) | SpriteRenderer (Square.png, yellow), Rigidbody2D, BoxCollider2D, Bullet | Scale (0.14, 0.45, 1); RB2D **Kinematic**; collider **Is Trigger**, size 1×1 |
| **Enemy** (prefab) | SpriteRenderer (Circle.png), Rigidbody2D, CircleCollider2D, Enemy | RB2D **Dynamic**, Gravity Scale **0**, Freeze Rotation; collider **Is Trigger**, radius 0.5 |
| **PowerUp** (prefab) | SpriteRenderer (Circle.png), Rigidbody2D, CircleCollider2D, PowerUp | Scale 0.55; RB2D **Dynamic**, Gravity 0; collider **Is Trigger**, radius 0.6 |
| **EnemySpawner** | EnemySpawner | Assign **Enemy Prefab** and **Power Up Prefab**; position (0, 6, 0) |
| **GameSystems** | GameManager, AudioManager, UIManager | GameManager: assign **Player** (scene instance) and **Spawner** |
| **Background** | ScrollingBackground | – |

Prefab creation by hand: build the object in the scene with the components above, drag it into `Assets/Prefabs`, delete it from the scene.
Add the scene to **File > Build Settings** if you want to build.

## Tuning (all in the inspector)

- **EnemySpawner**: enemies per wave, spawn interval, speed growth, delays between waves.
- **GameManager**: `Instant Game Over On Breach` (untick to make breaches cost 1 hull instead), wave clear bonus.
- **PlayerController**: move speed, fire cooldown, max health, invulnerability, power-up duration.
- **AudioManager**: volumes, music on/off, optional override clips.
- High score is stored in PlayerPrefs under `AlienShooter.HighScore` (Edit > Clear All PlayerPrefs to reset).

## Possible Extensions – all implemented

| Extension | Where |
|---|---|
| Wave system with rising difficulty, wave banner, clear bonus | `EnemySpawner`, `GameManager`, `UIManager` |
| Enemy types: Normal, Fast, Tank (multi-hit), ZigZag | `Enemy`, `EnemySpawner.PickType` |
| Particles: enemy death, bullet impact, pickups, player death | `ParticleEffects` |
| Sound effects: shoot, hit, explosion, damage, pickup, wave, game over, UI + looping music | `AudioManager` |
| Start menu, game over/restart screen with score | `UIManager`, `GameManager` |
| High score with PlayerPrefs, "NEW HIGH SCORE!" | `GameManager`, `UIManager` |
| Polished HUD: score, best, wave, hull pips, power-up timers, toasts | `UIManager` |
| Scrolling parallax background | `ScrollingBackground` |
| Player health + invulnerability blink | `PlayerController` |
| Power-ups: rapid fire, triple shot, heal | `PowerUp`, `PlayerController` |
| Screen shake | `CameraShake` |

## Troubleshooting

- **Sprites invisible / black**: see the URP note above.
- **Nothing shoots**: make sure the Player prefab's *Bullet Prefab* field is assigned (the builder does this).
- **Input errors**: none expected; `GameInput` compiles for old, new, or both input systems.
