# 🎮✨ A Kid's Dream — Turn-based Tactics (Stardance / Hack Club Ready)

<p align="center">
  <img alt="A Kid's Dream demo" src=".github/ASSETS/demo.gif" style="max-width:640px; width:100%; border-radius:12px;">
</p>

<div align="center">

[![Godot](https://img.shields.io/badge/Godot-4.7-478CBF?logo=godotengine&logoColor=white)](https://godotengine.org)
[![C%23](https://img.shields.io/badge/C%23-12.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-Non--Commercial-EA4335?&logo=creativecommons&logoColor=white)](LICENSE)
[![Version](https://img.shields.io/badge/Version-0.1.0--pre--alpha.1-F59E0B?&logo=git&logoColor=white)](https://github.com/MrCode200/AKidsDream)
[![Hackatime](https://hackatime-badge.hackclub.com/U0BMQSUV1DG/AKidsDream)](https://hackatime.hackclub.com/)

</div>

---

## 📖 Table of Contents

- [Why “A Kid’s Dream”?](#-why-a-kids-dream)
- [Play the Build](#-play-the-build)
- [Features](#-features)
- [How to Play](#-how-to-play-quick)
- [Developer Console Commands](#️-developer-console-commands)
- [Project Structure](#-project-structure)
- [Development Quick Start](#️-development-quick-start)
- [Contributing](#-contributing)
- [License](#-license)
- [AI Usage](#-ai-usage)
- [Credits](#-credits)

---
## 🤔 Why “A Kid’s Dream”?

When I was a child, I created a small board game using paper and scissors. It was simple, but incredibly fun to play.

**A Kid’s Dream** is my attempt to recreate that experience — not exactly as it was, but from the pieces that remain in my memory. It is a game inspired by childhood imagination, handmade rules, and the joy of turning simple ideas into an adventure.


## 🚀 Play the build

> [!TIP]
> To spawn units for testing, see the [Developer Console Commands](#️-developer-console-commands) section.

We attach exported builds to the GitHub Release, these can be run without needing to install godot. 

To run locally with the project files:

1. Install Godot 4.7.x.
2. Clone this repository:

   git clone https://github.com/MrCode200/AKidsDream.git
   cd AKidsDream

3. Open the project in Godot. If you have the matching .NET SDK installed, the C# assemblies will be restored/compiled automatically.
4. Press Play to run the game, or use the Editor export templates to create platform-specific builds.

Minimum tested toolchain:
- Godot 4.7
- .NET SDK compatible with the project's C# version

---

## 🌟 Features

- 🎯 Turn-based tactical combat on a tile grid
- 🧩 Modular unit system (stats, abilities, components)
- 🎨 Cute visuals & playful animations
- 🕹️ Intuitive controls + developer console for debugging
- 🤖 AI controllers and team relations
- 💾 Save/load and Serilog structured logging

---

## 🎮 How to play (quick)

- Left click — select units and interact with tiles
- End Turn — finish your turn
- `~` (tilde) — open developer console for debug/test commands

Short loop: select a unit → move → use ability → end turn. Win by (... maybe you can tell me how to win ?:0 (looking for suggestions :)))

---

## 🛠️ Developer console commands

- `unit_create <name> <player_id> <team_id> <x> <y>` — spawn a unit at (x,y)
> [!NOTE]
> (currently only 1 1, or 2 2 work for player_id and team_id. ex: unit_create 1 1 2 4)
---

## 📁 Project structure

```
AKidsDream/
├── Core/           # Controllers, Globals (EventBus), Managers
├── Entities/       # Board and Unit implementations
├── UI/             # User interface scenes and scripts
├── Common/         # Shared utilities (logging, state machines)
└── Utilities/      # Save/load system, console commands, tooling
```

---

## ⚙️ Development quick start

1. Fork the repo and create a branch: `git checkout -b feature/your-feature`
2. Work in Godot and run locally
3. Commit with conventional commits: `feat(scope): short description` ✍️
4. Push and open a Pull Request with testing notes ✅

See `GUIDELINES.md` for coding and logging conventions.

---

## 🙌 Contributing

Contributions welcome! Open issues or PRs. Use conventional commits and include testing notes in PR descriptions.

---

## 📜 License

This project uses the AKidsDream Non-Commercial License. See `LICENSE` for full details. Contact the maintainer for commercial licensing inquiries.

---
## 🤖 AI Usage

AI tools were used only as development assistance. They helped with debugging, troubleshooting, and providing suggestions for the design and implementation of individual systems.

AI was also used to help write and improve documentation. All final implementation decisions, code integration, and project direction were reviewed and made by the developer.

---

## ✨ Credits

- Author: MrCode200
- Thanks to the helpful members of the Godot Discord community who helped me solve problems during development
- Built with Godot 4 and C#

---

<p align="center">Made with ❤️ using Godot 4 &amp; C# — ready for Stardance! ✨🎉</p>
