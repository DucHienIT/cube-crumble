# Cube Crumble

Cube Crumble is a casual 3D puzzle game built with Unity. Rotate the cube structure, break exposed cubes, and route the released colored balls into matching containers before time or shared-slot space runs out.

## Gameplay

- Tap an exposed cube to crumble it and release its balls.
- Drag horizontally to rotate the structure and reveal new cubes.
- Balls automatically enter a matching active container.
- Balls without a matching container wait in the shared slot; overflowing it ends the level.
- Clear every ball before the timer expires to complete the level and earn up to three stars.

The project currently includes 30 levels, level selection, saved progression, pause/restart controls, audio, and responsive mobile UI.

## Requirements

- Unity `2022.3.62f3` (Unity 2022 LTS)
- Git

Use the matching Unity editor version to avoid unnecessary project or package upgrades.

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/DucHienIT/cube-crumble.git
   cd cube-crumble
   ```

2. Open the project from Unity Hub with Unity `2022.3.62f3`.
3. Wait for Unity to restore packages and import assets.
4. Open `Assets/_Project/Scenes/Main.unity` and enter Play mode.

## Project Structure

```text
Assets/_Project/        Game scenes, scripts, prefabs, and project assets
Assets/Resources/       Runtime configuration and level data
Packages/               Unity package manifest and lock file
ProjectSettings/        Unity editor and build configuration
Builds/                 Local game builds
```

Gameplay code is organized under the `CubeBurst` namespaces and separates core game rules, Unity views, systems, configuration, and UI.

## Main Packages

- Universal Render Pipeline (URP) `14.0.11`
- Input System `1.7.0`
- uGUI
- DOTween Pro
- Toony Colors Pro 2
- Unity MCP

## Build

The enabled build scene is `Assets/_Project/Scenes/Main.unity`. In Unity, open **File > Build Settings**, choose the target platform, and create a build.
