# Platforming QoL Mod

A Hollow Knight mod focused on improving precision platforming with optional time control and hitbox visualization tools.

## 🔧 Features

- **Freeze on Damage**: Briefly freezes time after taking damage.
  - Can be configured to trigger on **hazard objects only** or **any hits** (`Freeze Mode` setting).
  - Configurable freeze duration (`Freeze Duration` setting, 0–2 seconds).
- **Speedup After Freeze**: Speeds up time for a short period after freeze.
  - Configurable multiplier (`Speed Multiplier` setting, 1x–5x).
  - Skip time duration depends on **Skip Type** (`Respawn` = 1.8s, `Death` = 5s).
- **Hitbox Viewer**: Toggle display of all active hitboxes (`Show Hitboxes` setting).
- **Coroutine-Safe Logic**: Ensures only one time effect runs at a time.
- **Global Settings Save/Load**: Automatically saves your mod settings to a JSON file and restores them on game start.

## 📥 Installation

1. Install Modding API if not already installed.
2. Place the compiled `.dll` into your `Hollow Knight/Mods` folder.
3. Launch the game and configure via **Mods > Platforming QoL Mod** in the pause menu.

## 🛠 Settings

| Setting                  | Description                                           | Range / Options                |
|---------------------------|-------------------------------------------------------|--------------------------------|
| Freeze Duration           | Time (in seconds) to freeze after taking damage      | 0.0 – 2.0                      |
| Speed Multiplier          | Timescale applied after freeze                        | 1.0 – 5.0                      |
| Show Hitboxes             | Display hitboxes during slowmo                        | On / Off                        |
| Freeze Mode               | Determines which hits trigger the freeze             | "Hazard objects only" / "Any hits" |
| Skip Type                 | Duration skip after freeze                             | "Respawn" (1.8s) / "Death" (5s) |
