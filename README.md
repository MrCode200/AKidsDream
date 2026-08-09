# 🎮 A Kid's Dream

> A Kid's Dream is a small, charming turn-based tactics game built with Godot 4 and C#. Shipping-ready for Stardance / Hack Club.

<p align="center">
  <img alt="A Kid's Dream demo" src=".github/ASSETS/demo.gif" style="max-width:640px; width:100%;">
</p>

---

## One-line pitch

A Kid's Dream is a tiny turn-based tactical game where kids use toys and imagination to battle across a checkerboard battlefield.

---

## Play the build

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

## Demo / Screenshots

Add a short GIF (320–640px wide) to `.github/ASSETS/demo.gif` and screenshots to `.github/ASSETS/screenshots/`. These are used on the project page and release notes for Stardance submissions.

---

## Features

- Turn-based tactical combat on a tile grid
- Modular unit system (stats, abilities, components)
- Intuitive controls and developer console for debugging
- AI controllers and team relations
- Save/load system and Serilog structured logging

---

## How to play

- Left click — select units and interact with tiles
- End Turn — finish your turn
- `~` (tilde) — open developer console for debugging and test commands

Short loop: select a unit → move → use ability → end turn. Win by eliminating all enemy units.

---

## Console commands (developer)

- `unit.create <name> <x> <y>` — spawn a unit at (x,y)
- `unit.remove <id>` — remove a unit by ID
- `unit.list` — list all units on the board

---

## Project structure

```
AKidsDream/
├── Core/           # Controllers, Globals (EventBus), Managers
├── Entities/       # Board and Unit implementations
├── UI/             # User interface scenes and scripts
├── Common/         # Shared utilities (logging, state machines)
└── Utilities/      # Save/load system, console commands, tooling
```

---

## Development

Quick start:

1. Fork the repo and create a branch: `git checkout -b feature/your-feature`
2. Work in Godot and run locally
3. Commit with conventional commits: `feat(scope): short description`
4. Push and open a Pull Request describing your change and how to test it

See `GUIDELINES.md` for coding and logging conventions.

---

## Shipping checklist (for Stardance / Hack Club)

Before submitting, complete this checklist:

- [ ] One-sentence pitch and short description
- [ ] 10–20s demo GIF at `.github/ASSETS/demo.gif`
- [ ] 3–5 screenshots in `.github/ASSETS/screenshots/`
- [ ] Exported builds (Windows/macOS/Linux/Web) attached to a GitHub Release
- [ ] Short "How to play" instructions and controls
- [ ] LICENSE and asset attributions verified
- [ ] Release notes and version number updated

Suggested Stardance submission blurb:

> A Kid's Dream — a tiny, charming turn-based tactics game where kids use toys and imagination to battle on a checkerboard battlefield. Play in your browser or download builds from the release. Made with Godot 4.

---

## Contributing

Contributions welcome! Please open issues or PRs. Use conventional commits and include testing notes in PR descriptions.

---

## License

This project uses the AKidsDream Non-Commercial License. See `LICENSE` for full details. Contact the maintainer for commercial licensing.

---

## Credits

- Author: MrCode200
- Built with Godot 4 and C#

---

<p align="center">Made with ❤️ using Godot 4 &amp; C# — ready for Stardance!</p>
