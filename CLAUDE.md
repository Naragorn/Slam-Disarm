# Slam Disarm (Blade & Sorcery)

Implementation spec for this repo. Standalone mod, independent of DMCS and Heel of Achilles (AchillesBAS).

Blade & Sorcery (VR) mod built against the game's own `ThunderRoad.dll` (Unity + C#, `ThunderRoad` namespace), same setup as AchillesBAS.

> **Important for the implementer:** API names below (`RagdollHand.grabbedHandle`, `HandleRagdoll`, `CollisionHandler.OnCollisionStartEvent`, `EventManager.InvokeDisarm`, etc.) were read in the decompiled `ThunderRoad.dll` 1.3.2 (`DEVELOPMENT.md`, "API check"). **Verify every API against the current game version before relying on it.** If an API differs, adapt — don't invent.

---

## The mechanic (Nadi, 2026-11-22)

"ich kann die waffe an der waffenhand des gegners grabben und dann gegen objekte oder wände hauen. dann lässt dieser die fallen." — grab the enemy's weapon hand, slam it against objects or walls, and the enemy drops the weapon. Everything adjustable in the Mod Menu.

**Trigger**
- The player's hand holds an NPC's **weapon hand** (the hand holds an item), or its **forearm** (Forearm Grab Counts, default on), or the NPC's **weapon itself** while the NPC still holds it (Weapon Grab Counts, default on).
- While the grab lasts, each contact of the NPC's hand, forearm or held weapon with the **world** (walls, floor, rocks) or, with Objects Count (default on), a **loose object** is a slam candidate.
- Never a slam: any creature's body (the NPC itself, the player, other NPCs), items someone holds (that is a parry).
- The contact must be at least **Min Slam Speed** (default 2.5 m/s). Contacts closer than **Slam Cooldown** (0.3 s) to the last counted slam are the same slam.

**Effect**
- **Slams To Disarm** (default 2) counted slams, each within **Slam Window** (default 3 s) of the last one, loosen the grip; the last needed slam rolls **Disarm Chance** (default 100 %). A failed roll keeps the count full; the next slam rolls again.
- A counted slam that does not disarm yet: the NPC's pain voice (Pain Sound) and a pulse in the player's grabbing hand (Haptic Pulse), each switchable. Optional **Hand Damage** per counted slam (default 0).
- Disarm: the slammed hand lets go (the game's disarm event is raised, so the game's own disarm rewards apply); shields only with Shields Can Be Slammed (default off); the other hand too with Drop Both Hands (default off). Then **No Regrab Time** (default 5 s): any weapon that hand picks up or draws is released again.
- Player vs NPC only.

## Mod Menu (required)

- Every option in the Mod Menu, category "Slam Disarm" starting with "Slam Disarm Enabled", plus "Debug Logging" in category "Debug". No magic numbers in code.
- Option names unique per mod (the game saves by name); an enum default must be the first entry; float value lists come from `Logic/OptionSpecs` (decimal builder, the default bit-identical to one entry).
- Live: a changed value applies at once. Turning the mod off mid-fight stops listening, lifts every no-regrab lock and forgets every slam count.
- Persistence and reset: the game's own (saved by option name + value index; the mod page's reset button).

## Code rules

- Decisions in pure code under `src/SlamDisarm/Logic/` (no Unity, no game types), unit-tested for every flow incl. refusal paths, test first. Game glue outside `Logic/` stays thin.
- Per frame only the poll of the player's two hands; no allocation per frame; collision listeners only while a grab counts.
- New functions ≤ 100 lines, files ≤ 1000 lines.
- `README.md` is for players only (what it does, install, Mod Menu, compat; plain words, no engine terms). Everything technical goes in `DEVELOPMENT.md`.

## Build order

1. Project setup, pure logic with unit tests, game glue, Mod Menu, smoke scenario. **Done 2026-11-22** (see `DEVELOPMENT.md`, "Status").
2. Headless runs on Linux (`tools/harness.sh`): smoke, grab and a real slam. **Done 2026-11-23**, 3/3 pass (`harness/2-slam.txt` needed v0.1.1: the mod turns a standing NPC's hand collision events on while it watches the grab; DEVELOPMENT.md, "Harness").
3. Headset check: real grab, real slam, Min Slam Speed tuning, feel of the feedback. Owed.
4. Harness steps for a real slam (see `DEVELOPMENT.md`, "Harness") if the headset check leaves questions.

**Deploy (rule, user 2026-11-18 for AchillesBAS, same here): always both installs.** `tools/deploy.sh` (Linux) builds Release, refuses uncommitted `src` changes, copies DLL + manifest into the Linux and the Windows Mods folder and checks the md5. Not while a harness run or the game uses the Linux install.
