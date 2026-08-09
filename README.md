# 🎮✨ A Kid's Dream — Turn-based Tactics (Stardance / Hack Club Ready)

<p align="center">
  <img alt="A Kid's Dream demo" src=".github/ASSETS/demo.gif" style="max-width:640px; width:100%; border-radius:12px;">
</p>

<div align="center">

[![Godot](https://img.shields.io/badge/Godot-4.7-blue?style=for-the-badge)](https://godotengine.org) 
[![C#](https://img.shields.io/badge/C%23-12.0-purple?style=for-the-badge)](https://learn.microsoft.com/en-us/dotnet/csharp/) 
[![License](https://img.shields.io/badge/License-Non--Commercial-red?style=for-the-badge)](LICENSE) 
[![Version](https://img.shields.io/badge/Version-0.1.0--pre--alpha.1-orange?style=for-the-badge)](https://github.com/MrCode200/AKidsDream) 
[![Hackatime](https://hackatime-badge.hackclub.com/U0BMQSUV1DG/AKidsDream)](https://hackatime.hackclub.com/)

</div>

---

## ✨ One-line pitch

A Kid's Dream is a tiny, charming turn-based tactics game where kids use toys and imagination to battle across a colorful checkerboard battlefield — playful, tactical, and delightful. 🎲🧸🌈

---

## 🚀 Play the build

We attach exported builds and web exports to the GitHub Release. To run locally:

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

## 🎞️ Demo & Screenshots

Drop a short GIF (320–640px) at `.github/ASSETS/demo.gif` and screenshots to `.github/ASSETS/screenshots/`. They appear on the repo and release.

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

Short loop: select a unit → move → use ability → end turn. Win by eliminating all enemy units. 🏆

---

## 🛠️ Developer console commands

- `unit.create <name> <x> <y>` — spawn a unit at (x,y)
- `unit.remove <id>` — remove a unit by ID
- `unit.list` — list all units on the board

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

## 📦 Shipping checklist (Stardance / Hack Club friendly)

Before submitting, complete this checklist:

- [ ] One-sentence pitch + short description
- [ ] 10–20s demo GIF at `.github/ASSETS/demo.gif` (recommended: 480×270 or 640×360)
- [ ] 3–5 screenshots in `.github/ASSETS/screenshots/`
- [ ] Exported builds (Windows/macOS/Linux/Web) attached to a GitHub Release
- [ ] Short "How to play" instructions and controls
- [ ] LICENSE and asset attributions verified
- [ ] Release notes and version number updated

Suggested Stardance submission blurb:

> 🎲 A Kid's Dream — a tiny, charming turn-based tactics game where kids use toys and imagination to battle on a checkerboard battlefield. Play in your browser or download builds from the release. Made with Godot 4. 💫

---

## 🙌 Contributing

Contributions welcome! Open issues or PRs. Use conventional commits and include testing notes in PR descriptions. Please add a short demo or screenshots for any visual/UI changes.

---

## 📜 License

This project uses the AKidsDream Non-Commercial License. See `LICENSE` for full details. Contact the maintainer for commercial licensing inquiries.

---

## ✨ Credits

- Author: MrCode200
- Built with Godot 4 and C#

---

<p align="center">Made with ❤️ using Godot 4 &amp; C# — ready for Stardance! ✨🎉</p>
