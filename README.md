# 🎮 A Kid's Dream

<div align="center">

A turn-based tactical strategy game built with Godot 4 and C#

[![Godot](https://img.shields.io/badge/Godot-4.7-blue)](https://godotengine.org)
[![C#](https://img.shields.io/badge/C%23-12.0-purple)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-Non--Commercial-red)](LICENSE)
[![Version](https://img.shields.io/badge/Version-0.1.0--pre--alpha.1-orange)](https://github.com/MrCode200/AKidsDream)

</div>

---

## ✨ Features

- **🎯 Turn-Based Tactical Combat** - Strategic gameplay on a tile-based board
- **🤖 Unit System** - Diverse units with unique abilities and stats
- **🎨 Beautiful Visuals** - Checkerboard board with animated units
- **👥 Team Management** - Support for multiple teams with ally/enemy relations
- **🧠 AI Controllers** - Computer-controlled opponents with intelligent decision-making
- **💾 Save/Load System** - Persist your game progress and resume later
- **🎮 Event-Driven Architecture** - Clean, decoupled systems using an EventBus
- **📊 Structured Logging** - Comprehensive logging with Serilog for debugging
- **🔧 Developer Console** - In-game console for debugging and testing

---

## 📸 Screenshots

*Coming soon - Add screenshots of your game here!*
---

## 🎮 How to Play

### Basic Controls

- **Left Click** - Select units, interact with tiles
- **End Turn Button** - Finish your turn and pass to the next player
- **Console** - Press `~` (tilde) to open the developer console

### Gameplay Loop

1. **Select a Unit** - Click on your unit to see available actions
2. **Move** - Click on highlighted tiles to move your unit
3. **Attack** - Select an ability and target enemy units
4. **End Turn** - Click the End Turn button when finished

### Winning

Eliminate all enemy units to win the match!

---

## 🏗️ Architecture

### Core Systems

```
AKidsDream/
├── Core/
│   ├── Controllers/      # Player and AI input handling
│   ├── Globals/          # EventBus and global state
│   └── Managers/         # Game loop, ability visualization
├── Entities/
│   ├── Board/            # Tile-based board system
│   └── Units/            # Unit classes, abilities, components
├── UI/                   # User interface elements
├── Common/               # Shared utilities (logging, state machines)
└── Utilities/            # Save/load system, console commands
```

### Key Components

- **Board** - Manages the tile grid and unit positions
- **Unit** - Base class for all game units with stats and abilities
- **GameManager** - Orchestrates game initialization and state
- **EventBus** - Global event system for decoupled communication
- **GameLogger** - Structured logging with Serilog

### Design Patterns

- **Event-Driven Architecture** - EventBus for loose coupling
- **State Machine** - For unit and game state management
- **Component-Based** - Modular unit abilities and behaviors
- **Factory Pattern** - Controller creation and unit instantiation

---

## 🛠️ Development

### Console Commands

The in-game console supports various commands for debugging:

- `unit.create <name> <x> <y>` - Spawn a unit at position
- `unit.remove <id>` - Remove a unit by ID
- `unit.list` - List all units on the board

### Code Style

Follow the established conventions in `GUIDELINES.md`:

- Use conventional commit messages: `feat(scope): description`
- Log at appropriate levels (Debug, Info, Warning, Error)
- Enrich logs with entity context for better debugging

---

## 📝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Commit your changes** using conventional commits
4. **Push to the branch** (`git push origin feature/amazing-feature`)
5. **Open a Pull Request**

### Development Guidelines

- Read `GUIDELINES.md` for commit message format and logging standards
- Add structured logging for new features
- Keep code clean and well-documented

---

## 📄 License

This project is licensed under the **AKidsDream Non-Commercial License**.

**Summary:**
- ✅ Free for personal, educational, and research use
- ✅ Free to modify and redistribute (with attribution)
- ❌ Commercial use requires explicit permission

See [LICENSE](LICENSE) for full details.

---


<div align="center">

**Made with ❤️ using Godot 4 & C#**

[⬆ Back to Top](#-a-kids-dream)

</div>
