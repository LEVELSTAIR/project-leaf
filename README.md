# Project Leaf 🌱

**Open-source arcade horror garden game**
Built with **Unity 6.3 (LTS)** | Steam-enabled | Community-driven

---

## 1. What this project is

**Project Leaf** is a first-person, low-poly game where players grow plants in a calm daytime garden and survive horror events at night.

The core loop is **solo-first**, with a **shared multiplayer space (EDEN)** powered by Steam.

This is an **open-source project by the Levelstair community**.
The goal is learning, collaboration, and shipping something real — not a tech demo.

> **Want to contribute?** Read our [CONTRIBUTING.md](./CONTRIBUTING.md) for how to work on issues, commits, and pull requests the right way.

---

## 2. Core gameplay concept (short & precise)

* Players own a **personal garden** (solo, offline-capable)
* A shared starter plant grows with the player
* Daytime = calm, growth-focused
* Nighttime = horror phase (enemies, corrupted plants)
* Plants can become large enough to host **tree houses**
* Players can **sell unique plants** to other players
* **EDEN** is a shared Steam multiplayer space where:

  * Each player places **one plant**
  * Players walk around, explore, and view others’ plants
  * No plant growth or combat happens here

---

## 3. What multiplayer is (and is NOT)

### Multiplayer IS:

* Steam-authenticated
* Lobby-based
* Used only in **EDEN**
* Peer-hosted (no dedicated servers)
* Cosmetic + social only

### Multiplayer is NOT:

* MMO
* Persistent world simulation
* Used for plant growth
* Required for solo gameplay

If you try to add real-time multiplayer everywhere, your PR will be rejected.

---

## 4. Tech stack (locked decisions)

These choices are final unless the core maintainers say otherwise.

* **Unity 6.3 LTS**
* **URP** (no HDRP)
* **UI Toolkit** (no uGUI for screens)
* **Steamworks.NET** (Steam integration)
* **Unity Netcode for GameObjects (NGO)** for EDEN only
* **GitHub** for version control & PRs

---

## 5. Project structure (DO NOT IGNORE)

Every folder below has its **own Assembly Definition**.

```text
Assets/
 ├─ Leaf.Core
 │   └─ Plant data, growth logic, save systems
 │
 ├─ Leaf.Gameplay
 │   └─ Garden, night cycle, enemies, interactions
 │
 ├─ Leaf.Networking
 │   └─ Netcode logic (EDEN only)
 │
 ├─ Leaf.Steam
 │   └─ Steamworks adapter & services
 │
 ├─ Leaf.UI
 │   └─ UI Toolkit screens & controllers
 │
 └─ Leaf.Shared
     └─ Interfaces, utilities, common types
```

### Dependency rules (strict)

* `UI` ❌ cannot reference Steam directly
* `Core` ❌ cannot reference Networking
* `Steam` ❌ cannot reference UI
* Everything ✅ can reference `Shared`

Break these rules and your PR will be closed.

---

## 6. Steam integration (important)

Steam is used for:

* Player identity (SteamID)
* Lobbies (EDEN)
* Achievements
* Friends & invites

Steam is **NOT** used for:

* Game logic
* Save files
* Economy calculations

### Offline mode exists

The game must run:

* In Unity Editor
* Without Steam running

We use service interfaces like:

* `ISocialService`
* `IMultiplayerService`

This allows Steam and Offline implementations to coexist.

---

## 7. Achievements philosophy

Achievements are:

* Simple
* Event-based
* Triggered locally

Examples:

* First plant growth
* First night survived
* First EDEN visit

Achievements are **not analytics** and **not progression gates**.

---

## 8. What contributors are expected to do

You are welcome if you:

* Follow the architecture
* Keep PRs focused
* Write readable code
* Respect boundaries

You are **not** expected to:

* Be a Unity expert
* Know Steamworks deeply
* Implement massive systems alone

Small, clean contributions > big messy ones.

---

## 9. Git workflow (non-negotiable)

* `main` → stable, playable
* `dev` → active development
* Feature branches → PR → review

PR rules:

* One system per PR
* Must build without Steam running
* No new packages without discussion
* No refactors “just because”

---

## 10. Current development stage

**Early foundation phase**

Implemented / in progress:

* Project structure
* Core plant data model
* Day / night cycle prototype

Not started yet:

* EDEN multiplayer
* Selling plants
* Steam achievements
* Monetization cosmetics

If you’re unsure what to work on, ask before coding.

---

## 11. Philosophy (read this once)

This project values:

* Learning over ego
* Shipping over perfection
* Discipline over hype

Open source does not mean chaotic.
Fun does not mean sloppy.

---

## 12. Levelstair

Project Leaf is built by the **Levelstair community**
Sri Lanka’s open game development ecosystem.

This repo is a **training ground** and a **real product**.

Treat it like both.