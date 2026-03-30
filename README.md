# CasualHunter — Monster Hunter: World Overlay

Real-time transparent overlay for Monster Hunter: World (Iceborne) that reads game memory to display monster stats, player damage, and hidden game mechanics.

**MHW overlay** | **MHW HP overlay** | **Monster Hunter World damage meter** | **MHW DPS meter** | **Alatreon elemental threshold tracker**

---

## What It Does

CasualHunter reads Monster Hunter: World's process memory in real-time and renders useful information as a transparent, click-through overlay on top of the game.

- **Monster HP** — Live health bar with current/max values and percentage
- **Rage Timer** — Shows when the monster is enraged with a countdown timer
- **Part Tracker** — All breakable parts with individual HP and break counts
- **Damage Board** — Player damage leaderboard with totals and percentages
- **Alatreon Elemental Threshold** — Tracks the hidden elemental damage countdown that determines whether Escaton Judgment is survivable
- **Status Effects** — Monster ailment buildup (paralysis, stun, blast, etc.)

## Screenshots

*Coming soon*

## Installation

1. Download the latest release from [Releases](https://github.com/quardianwolf/CasualHunter/releases)
2. Extract the zip
3. Right-click `CasualHunter.exe` → **Run as Administrator**
4. Launch Monster Hunter: World and enter a quest

> **Note:** Administrator privileges are required to read game process memory. The app only reads memory — it does not write or modify anything.

## Controls

| Key | Action |
|-----|--------|
| `Insert` | Toggle edit mode — drag widgets to reposition, show/hide toggles |
| `ESC` | Shows hint to press Insert |

## How It Works

CasualHunter uses AOB (Array of Bytes) pattern scanning to locate game data structures in memory, then follows multi-level pointer chains to extract live values every frame. Memory offsets were developed by reverse engineering existing open-source overlay tools and binary analysis of the game executable.

No DLL injection. No game file modification. Read-only memory access.

## Requirements

- Windows 10/11 (64-bit)
- Monster Hunter: World with Iceborne
- Self-contained build — no .NET runtime needed

## Building from Source

```bash
dotnet build mhw-stat/MHWStatOverlay.slnx

# Self-contained release
dotnet publish mhw-stat/MHWStatOverlay/MHWStatOverlay.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Settings

Settings are saved to `%APPDATA%/CasualHunter/settings.json` and include:

- Widget visibility and positions
- Widget widths
- Overlay opacity
- Polling rate

## Language Support

Auto-detects system language. Currently supports:
- English
- Turkish (Türkçe)

## License

MIT

## Disclaimer

This tool is not affiliated with or endorsed by Capcom. Use at your own risk. CasualHunter only reads game memory and does not modify game files or inject code.
