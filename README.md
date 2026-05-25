# JumperGame

JumperGame is a 2D vertical arcade platformer built with Unity.
The player climbs by bouncing on platforms, avoiding hazards, collecting coins, and using shop buffs.

## Gameplay Features

- Core movement: move, jump, wrap, shoot projectile.
- Procedural world: vertical platform spawning.
- Platform variants: static, moving, spring, breakable.
- Economy: inventory, shop, item use flow.
- Buffs: Rocket Boost, High Jump, Double Coin.
- Progress: score tracking, history, leaderboard UI.
- UX flow: menu, level select, pause, game over, settings.
- Audio: BGM and SFX hooks.

## Scenes

- `Assets/Scenes/MenuScene.unity`
- `Assets/Scenes/GameScene.unity`
- `Assets/Scenes/AudioScene.unity`

## Team and Responsibilities

- **Leader - Trần Nguyễn Tuấn Anh** (`dopaemon`, `polarisdp@gmail.com`)
  - Core gameplay loop, rules, and tuning.
  - PlayerController, projectile combat, camera/score.

- **Phan Văn Nhật Trường** (`Girrint`, `truongphan172@gmail.com`)
  - Menu/UI systems, scene transitions, level select.
  - About/History/Settings and gameplay canvas flow.

- **Lê Thị Mai Anh** (`maianhtl`, `lethimaianhdl2019@gmail.com`)
  - Platform/world generation and spawn balancing.
  - Object pooling and world interaction effects.

- **Nguyễn Minh Anh** (`MinhAnhhhhhh`, `nguyenminanh5@gmail.com`)
  - Inventory/shop/item systems and buff runtime logic.
  - Audio manager integration and QA balancing pass.

## Tech Stack

- Unity (URP 2D), C#, Physics2D, Unity UI/TMP
- ScriptableObject, Singleton, Object Pooling

## Run

1. Open project in Unity Hub.
2. Open `Assets/Scenes/MenuScene.unity`.
3. Press Play.

## Build

1. Open `File -> Build Settings`.
2. Add scenes: MenuScene, GameScene, AudioScene.
3. Build for PC/WebGL as required.
