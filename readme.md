# ⚔️ Terminal Blaster

## Description
A fast-paced old-school style , **terminal-based arcade shooter** built in **.NET 10.0**, featuring ASCII enemies, wave-based progression, running inside console!

### Why created a game in console ? 
- Why not right, wanted to learn some of the cool CLI libraries but none suited what I am trying to do here.

### What worked and what didn't
- Colors were causing too much flikering so dropped that idea
- Played with timers but game doesn't do well with timing in console
- Wanted to keep data structure simple list has to be used to maintain number of bullets hitting enemies
- if you want to upgrade players or power ups might have to change that data structure as well

### Vibe coding
- AI fumbles with new ideas, as code gets longer it reacts poorly
- Used it to generate fun ASCII art saved a ton of time there for sure
- Helped in collission detection 
- Generation of this readme
---

## 🚀 Features

- 🎮 **Keyboard-controlled gameplay** — Move, shoot, and dodge enemies
- 💣 **Wave system** — Enemies respawn stronger after each round
- 💥 **Dynamic bullet logic** — Player and enemy bullets move independently
- 🧠 **Progressive difficulty** — Bullet speed increases each wave
- 🖥️ **Cross-platform** — Runs on Windows, macOS, and Linux
- ⚡ **.net runtime required**

---
Future Enhancements

Feel free to fork or add to it and post screenshots :). Some ideas in case you want to brush up your coding or vibe coding

- 🔊 Add sound effects (Console.Beep() or external library)
- ⚡ Power-ups and weapon upgrades
- 💾 Save/load high scores to-from server
- 🧩 Add color themes (retro green, matrix mode, etc.)

---

## 📋 Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later  
- A terminal environment (Command Prompt, PowerShell, or macOS/Linux terminal)

---
🤝 Contributing

Do the ususal :) 

- Fork this repository
- Create your feature branch 
- Commit your changes
- Push to your branch and open a Pull Request

---

## 🛠️ Installation & Run

```bash
# Clone the repository
git clone https://github.com/blackarck/TerminalBlaster.git
cd TerminalBlaster

# Build the project
dotnet build

# Run it
dotnet run

```
---
### 🔄 Status

- 🧪 Playable and improving
- 💬 Contributions, ideas, and bugs welcome!
---
### 📜 License

This project is licensed under the MIT License — see the LICENSE
 file for details.