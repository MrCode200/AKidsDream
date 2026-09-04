# A Kid's Dream — Turn-based Tactics War Game

A Kid's Dream is a turn‑based tactical war game inspired by a paper-and-scissors board game I made as a kid. Command modular units with unique abilities over a tiled battlefield and see how battles play out.

<p align="center">
  <a
    href="https://github.com/MrCode200/AKidsDream/raw/refs/heads/main/Git/Assets/Preview.mp4"
    download="AKidsDream-Preview.mp4"
  >
    <img
      src="Git/Assets/Preview.png"
      alt="Download gameplay preview"
      width="720"
    />
  </a>
</p>

## 📖 Table of Contents

- [Why “A Kid’s Dream”?](#-why-a-kids-dream)
- [Play the Build](#-play-the-build)
- [Features](#-features)
- [Roadmap](#-roadmap)
- [How to Play](#-how-to-play-quick)
- [Developer Console Commands](#️-developer-console-commands)
- [Documentation](#-documentation)
- [How It Works](#-how-it-works)
- [License](#-license)
- [AI Usage](#-ai-usage)
- [Credits](#-credits)

## 🤔 Why “A Kid’s Dream”?

When I was a child, I created a small board game using paper and scissors. It was simple, but incredibly fun to play.
**A Kid’s Dream** is my attempt to recreate that experience, not exactly as it was, but with the pieces that remained in my memory. 

## 🚀 Play the build

Tip: use the developer console to spawn units for testing (see Developer console commands).

I attach exported builds on the Releases page, those run without Godot.

To run locally:

Install Godot 4.7.x.
Clone the repo: git clone https://github.com/MrCode200/AKidsDream.git cd AKidsDream
Install .NET 10.0: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
Open the project in Godot and press Play.

## 🌟 Features

- Turn-based tactical combat on a tile grid
- Modular unit system (stats, abilities, components)
- Save/load and Serilog structured logging

## Roadmap

- Enemy AI logic
- More unique units
- Mana system and upgrades
- Online matchmaking

## 🎮 How to play (quick)

`Left click` -- select units and interact with tiles \
`Right click` -- Deletes selected tile (for multi selected tiles) \
`Hover` -- To see ability Descriptions hover of the respected Button \
`End Turn` -- finish your turn \
`~` (tilde) -- open the developer console for debug/test commands \
**Typical turn**: select a unit → move → use an ability → end turn. \
***Win conditions are not yet implemented, any (any) suggestions are really WELCOME:)***

## 🛠️ Developer console commands

> unit_create <name> <player_id> <.x> <.y> — spawn a unit at (x,y)

**NOTE:** \
Available units: Soldier, TestUnit. Currently player_id accept 1 or 2 only (example: unit_create Soldier 1 2 4).

## 📃 Documentation
To read the documentation visit:
- Project layout and roadmap on [Miro](https://miro.com/app/board/uXjVH4avfyE=/?share_link_id=420032532025)
- Ask targeted questions via [Devin](https://app.devin.ai/org/navidyaghmaei/wiki/MrCode200/AKidsDream/page/1?branch=main) 
## 🔧 How it works

The project uses a modular, data-driven architecture:

Units are composed from components. \
Abilities are composed from Effects. \
Payloads & States let abilities communicate and chain behavior. \
Abilities execute two main ways:

1. **Sequential:** run the effect on selected tiles one at a time.
2. **Batch:** apply the effect to all selected tiles together. 

Each ability can include multiple effects. Effects (damage, move-self, etc.) 
can include triggers such as animation frame or timed moments. 
This way of implementing enables easy and modular way to create abilities very fast.


## 📜 License

This project uses the AKidsDream Non-Commercial License. See LICENSE for details. Contact the maintainer for commercial licensing inquiries.

## 🤖 AI Usage

AI was used as a development assitante, in the areas for debugging, suggestion for creation of individual systems, writing documentation for the code (docstrings).

## ✨ Credits

- Author: MrCode200
- Thanks to the Godot Discord community for help during development
- Built with Godot 4 and C#
