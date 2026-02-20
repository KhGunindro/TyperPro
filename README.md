# TyperPro
**The Ultimate Typing Speed Analyst**
*Developed for Cybrella 2026 – NIELIT Tech Event*

[![Built with Avalonia](https://img.shields.io/badge/Built%20with-Avalonia%20UI-blue)](https://avaloniaui.net/)
[![Framework](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Event](https://img.shields.io/badge/Event-Cybrella%202026-orange)](https://nielit.gov.in/)

TyperPro is a high-performance desktop application designed to measure, analyze, and improve typing proficiency. Built using **C#**, **Avalonia UI**, and the **MVVM** architectural pattern, it provides a seamless experience for participants at Cybrella 2026.

---

## Key Features

- **Real-time Analytics:** Watch your WPM and accuracy update dynamically as you type
- **3 Difficulty Rounds:** Easy, Medium, and Hard — 3 sub-rounds each
- **60-Second Timed Tests:** With a 5-second countdown before each round
- **Visual Feedback:** Instant UI highlighting for correct and incorrect keystrokes
- **Session Summary:** Detailed per-round and overall performance charts
- **Sound Feedback:** Keyboard click sounds powered by OpenAL
- **Score Persistence:** Results saved locally via SQLite
- **MVVM Architecture:** Clean separation of concerns for high maintainability

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | [Avalonia UI](https://avaloniaui.net/) |
| Language | C# / .NET 9.0 |
| Pattern | Model-View-ViewModel (MVVM) |
| MVVM Library | CommunityToolkit.Mvvm |
| Database | SQLite via Microsoft.Data.Sqlite |
| Audio | OpenAL via OpenTK |
| Typing Font | JetBrains Mono |

---

## How to Run

### Option A — Download the Release (Recommended)

1. Go to the [Releases](../../releases) page
2. Download `TyperPro.exe` from the latest release
3. Install the [OpenAL runtime](https://www.openal.org/downloads/) if you don't have it *(required for sound)*
4. Run `TyperPro.exe` — no installer needed

---

### Option B — Build from Source

**Prerequisites**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- Windows / macOS / Linux

**Steps**
```bash
# Clone the repository
git clone https://github.com/your-username/TyperPro.git
cd TyperPro

# Run in development
dotnet run

# Or build a release executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:UseAppHost=true
```

The published exe will be at:
```
bin/Release/net9.0/win-x64/publish/TyperPro.exe
```

---

## Project Structure
```
TyperPro/
├── Models/          # Data models (RoundResult, RoundSummary, etc.)
├── Views/           # Avalonia XAML pages and windows
├── Services/        # TypingEngine, Sound, Database services
├── Assets/          # Fonts, icons
└── App.axaml        # Application entry point
```

---

## Author

Developed by **KhGunindro**
NIELIT Imphal · Cybrella 2026

---

*© 2026 TyperPro. Built for Cybrella – NIELIT Tech Event.*
