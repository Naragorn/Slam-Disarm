# Slam Disarm - development notes

Everything about how the mod is built and tested: versions, the game APIs used and what is verified vs. assumed, how a slam is decided, test status. The player-facing description and the Mod Menu reference are in [`README.md`](README.md); the spec is [`CLAUDE.md`](CLAUDE.md).

## Status

| Step | State |
|---|---|
| 1. Project, pure logic + unit tests, game glue, Mod Menu, smoke scenario | built 2026-11-22 on Windows. **Unit tests: 90/90 pass** (`dotnet test`; written first: 63 of 90 failed on the stubs, the 27 option-spec tests passed, then all green after the logic). **Release build: 0 warnings, 0 errors** against the Windows install's `ThunderRoad.dll`. |
| 2. Headless runs | **2/2 pass** on Linux (2026-11-22, `artifacts/harness/20261122-210619`): `0-smoke.txt` (renamed from `smoke.txt` so it runs first; load lines, 16 options with defaults, live toggles, "Turned off" line, no exceptions) and `1-grab.txt` (a real grab of the NPC's forearm starts the watch, letting go stops it, Forearm Grab Counts off starts nothing). No code change was needed. |
| 3. Real slam in the game | **Not passing headless yet** (`harness/owed/1-slam.txt`, see "Harness"). 2026-11-23: the grab now holds through the slam; the NPC's hand meets the block too slowly (1-2 m/s, the game's floor is 2 m/s). Headset check owed. |

The headless runs are the only in-game evidence so far (no headset). Every other "verified" below means read in the decompiled DLL and compiled against it.

## Versions

| What | Version | Source |
|---|---|---|
| Game (Windows install) | same `ThunderRoad.dll` as AchillesBAS reads: 1.3.2 | AchillesBAS `DEVELOPMENT.md`; decompiled `C:\Steam\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\Managed\ThunderRoad.dll` with ILSpy (ilspycmd 9.1) on 2026-11-22 |
| Manifest `GameVersion` | `1.0.0.0` | as AchillesBAS (`ModManager` requires an exact match) |
| Target framework | .NET Framework 4.7.2, C# 9 | as AchillesBAS |
| Tests | net8.0, xunit 2.9.2 | |

The decompiled source is not part of this repo.

## API check

### Verified in `ThunderRoad.dll` 1.3.2 (read and compiled against)

| Used for | API | Notes |
|---|---|---|
| Entry point | `ThunderScript` (`ScriptLoaded`, `ScriptEnable`, `ScriptDisable`, `ScriptUnload`, `ScriptUpdate`) | as AchillesBAS |
| Mod Menu | `ModOption`, `ModOptionCategory`, `ModOptionOrder`, `ModOptionSlider`, `ModOptionFloat` | as AchillesBAS |
| Player's hands | `Player.currentCreature.handLeft/handRight` (`RagdollHand`), `RagdollHand.grabbedHandle` | |
| Grab of a body part | `HandleRagdoll : Handle`, `.ragdollPart` | `HandleRagdoll` grabbing sets `ragdollPart.isGrabbed` and `ragdoll.isGrabbed` and treats `LeftHand/LeftArm` (and right) as grabbable hand/arm parts |
| NPC hand from the part | `RagdollHand : RagdollPart`; `Creature.GetHand(Side)`, `RagdollHand.lowerArmPart` (serialized) | a grabbed arm part counts as the forearm when it is its hand's `lowerArmPart` |
| Grab of the weapon | `Handle.item`, `Item.handlers` (list of `RagdollHand`), `RagdollHand.creature`, `Creature.isPlayer/isKilled` | |
| Shield test | `item.data.moduleAI.primaryClass == ItemModuleAI.WeaponClass.Shield` | as the game's `BrainModuleEquipment.OnDamage` and DMCS |
| Contacts | `CollisionHandler.OnCollisionStartEvent` (`CollisionEvent(CollisionInstance)`), `RagdollPart.collisionHandler`, `Item.collisionHandlers` | raised from `OnCollisionEnter` → `InvokeCollisionStart` |
| What was hit | `CollisionInstance.targetColliderGroup` (null for colliders without a group), `.collisionHandler.isRagdollPart`, `.collisionHandler.item`, `Item.isTelekinesisGrabbed`, `Item.holder` | |
| Slam speed | `CollisionInstance.impactVelocity` | `CollisionHandler.CalculateImpactVelocity` at the contact point |
| Disarm | `RagdollHand.TryRelease()`, `EventManager.InvokeDisarm(creature, hand, handle, hit)` | the game's hand-hit disarm and DMCS do the same; `SkillCombatFocus.OnDisarm` gives the player focus for any NPC disarm |
| Pain voice | `BrainData.GetModule<BrainModuleSpeak>(false).Play("hit", true)` | every human voice in the 1.3.2 catalog (`Voices/*.json`) has the dialog `hit` |
| Haptic pulse | `RagdollHand.HapticTick(intensity)` | only acts on a player hand (`playerHand.controlHand.HapticShort`) |
| Hand Damage | `Creature.Damage(float, DamageType.Blunt)` | builds a `CollisionInstance` with no body part and push level 0. The game's grab disarm needs push level ≥ 2 and its hand-hit disarm push level == 2 (`BrainModuleEquipment`, defaults in `Brains/*.json`), so neither fires; this mod listens to no hit event, so it cannot loop |
| Cleanup | `EventManager.onCreatureDespawn`, `onCreatureKill`, `onLevelUnload` | |

### Engine behaviour that shapes the code (read, not seen in game)

- **A standing NPC that nobody grabbed raises no ragdoll contacts** (`CollisionHandler.IsStandingUngrabbedRagdoll`). Holding its hand or forearm marks the ragdoll grabbed, so the hand's and forearm's contacts come through. Holding only its weapon does not: then the weapon's own contacts are the slams.
- **Ragdoll parts ignore items on the moving/dropped item layers** (`IsRagdollAndHitByExcludedLayers`: MovingItem, DroppedItem, ItemAndRagdollOnly, PlayerHandAndFoot). So a hand slammed onto a dropped item raises nothing on the hand's side; the weapon slammed onto it does. Derived, not seen: a hand slam onto a loose object may only count when the object sits on another layer.
- **Item against item, only the faster side reports** (`OnCollisionEnter`: the event is dropped when the target item moves faster). A slammed weapon is the faster side against a resting object.
- **Slow ragdoll contacts against the world raise nothing** (below `ragdoll.collisionMinVelocity`). Min Slam Speed sits on top of that.

### Assumed - must be confirmed in the game

| Assumption | Why it matters | How to check |
|---|---|---|
| `impactVelocity.magnitude` of a hard slam is well above 2.5 m/s and a light bump below | Min Slam Speed's default | Debug Logging: `SLAM`/`SKIP` lines show the speed of every contact |
| ~~Holding an NPC's wrist uses a handle on the hand or forearm part~~ **Seen 2026-11-22 (harness):** the hand part has no handle; the arm handle `HandleArmLeft`/`HandleArmRight` sits on the forearm (`LeftArm`/`RightArm`, section Lower), and grabbing it logs `GRAB ... watching Left hand with 'SwordShortCommon' (3 bodies)` | Which grabs count | done |
| Walls and floors have no `ColliderGroup` | They classify as World | `SLAM ... World` lines when slamming into a wall |
| The NPC's AI does not drop or swap the weapon by itself when its wrist is held | Otherwise the watch ends before a slam | `GRAB ... stopped watching` right after the grab |
| Slam Cooldown 0.3 s merges the several colliders of one slam into one | Otherwise one slam counts twice | two `SLAM` lines for one slam |

## How a slam is decided

Pure logic in `src/SlamDisarm/Logic`, unit-tested without the game.

**Does a grab count** (`GrabRules.Decide`, per player hand, every frame; the first failing check is the reason):
1. `ModOff`
2. `NotGrabbing` - the hand holds nothing (or a lever, door, ...)
3. Body part: `NotNpc` (the player's own body), `NpcDead`, `NotHandOrForearm`, `ForearmGrabOff`; item: `WeaponGrabOff`, `NotHeldByNpc`, `NpcDead`
4. `HandEmpty` - the NPC hand holds nothing
5. `Shield` - it holds a shield and Shields Can Be Slammed is off

While a grab counts, the hand's watch listens to the NPC hand's, forearm's and held weapon's contacts. When both player hands hold the same NPC hand and weapon (one on the wrist, one on the blade), only one listens, so a contact is decided once.

**What a contact does** (`SlamRules.Decide`, per NPC series):
1. Target: `Creature` (any body part) and `HeldObject` (an item a hand, telekinesis or a holder holds) never count; `LooseObject` only with Objects Count; `World` = a collider without a collider group.
2. `TooSlow` - below Min Slam Speed (does not start the cooldown).
3. `Cooldown` - within Slam Cooldown of the last counted slam (changes nothing).
4. Window: more than Slam Window after the last counted slam, the count starts over.
5. Count + 1 (capped at Slams To Disarm, at least 1). Below it: `Counted` (pain voice, haptic pulse, Hand Damage).
6. At it: roll Disarm Chance. Hit: `Disarm`, the series resets. Miss: `Resisted` (feedback, the count stays full, the next slam rolls again).

**Disarm**: the slammed hand lets go (`TryRelease` + `InvokeDisarm`); with Drop Both Hands the other hand too, unless it holds a shield and Shields Can Be Slammed is off (`DisarmRules.HandsToRelease`). Each hand that let go is locked for No Regrab Time (`NoRegrabRules`): every frame a locked hand that holds something lets go of it again. 0 s = no lock.

Debug Logging lines (`[SlamDisarm]` prefix): `GRAB | <npc> | watching ...` / `stopped watching`, `SLAM | <npc> | World at 4.2 m/s | 1/2`, `SKIP | <npc> | ... | TooSlow (count 0/2)`, `DISARM | <npc> | Right hand dropped '<item>' | no regrab for 5.0 s`, `REGRAB | ...`.

## Performance

Per frame: one read of each player hand's grab (no allocation; a struct and a few reference checks) and, only while NPCs are locked, a check of their two hands. Listeners exist only while a grab counts; every contact decision is a few comparisons. Log strings are only built with Debug Logging on. Not measured in the game.

## Harness

| File | Checks |
|---|---|
| `0-smoke.txt` | load lines, 16 options with categories and defaults, live toggles, "Turned off" line, no exceptions |
| `1-grab.txt` | `player-grab-npc LeftArm section=Lower` on a HumanDummy holding a sword: `GRAB ... watching Left hand with 'SwordShortCommon'`, `stopped watching` on release; with Forearm Grab Counts off no watch |
| `owed/1-slam.txt` | not run by `tools/harness.sh`; fails, see below |
| `probe/*.txt` | not run by `tools/harness.sh`: the measurements behind the slam scenario (where the bodies are, a sideways slam, straight-down slams) |

BasHarness got the steps for this on 2026-11-22: `player-grab-npc` (the controller reaches for the handle, then `RagdollHand.Grab`), `player-slam` (controller moved at a set speed, real physics), `place-block` (a static box with the ground's layer and physics material, so it counts as World). On 2026-11-23: `player-eyes` (head at the creature's eye height), `place-block from=npc-hand` and its refusal of a block inside a body, and a per-frame `player-slam` trace in Player.log. Run `tools/harness.sh harness/owed/1-slam.txt` by hand.

**Why the slam does not pass yet** (first 20 runs 2026-11-22, `artifacts/harness/20261122-20*`; 12 more 2026-11-23, `artifacts/harness/20261123-09*`, probes in `harness/probe/`):

- Fixed: blocks without a physics material the game knows (`CollisionHandler.OnCollisionEnter` logs `Target physics material not found` and raises no event). `place-block` copies the ground's material.
- Fixed, and the first runs' explanation was wrong: the grab was not lost to the reach rule. It was lost the moment the block appeared. The 0.6 m block sat inside both men's legs, both bodies and the NPC's sword, and the physics flung them apart. `place-block` now refuses a block inside any body and names each one with its place.
- Fixed: the mock headset sits 0.16 m above the player creature's eyes (head 1.75 m, eyes 1.59 m above the feet), because the harness marks the calibration done without measuring anyone. The game measures a grab's reach from the head (the shoulder centre is the head: `Morphology.armsToEyesHeight` is never set and stays 0). `player-eyes` moves the head to the eyes. Since then the grab holds through every slam (grip at most 0.96 m from the head, reach 1.22 m).
- Open: the NPC's hand reaches the block, but too slowly. Moving the controller 0.6 m down at 12 m/s brings the NPC's forearm down only 0.12-0.24 m, at 2-3 m/s at best and about 1-2 m/s where it meets a block it can reach. The game raises no collision event for a ragdoll part that meets the world slower than `Ragdoll.collisionMinVelocity` (2 m/s, read in the decompiled DLL), and the mod counts only from Min Slam Speed (2.5 m/s). So no `World` contact has been logged yet; every logged contact is `Creature`.
- Seen, not explained: while the player holds the NPC's forearm, the player's own hand stays at the NPC's chest (1.06 m, in the film and in the trace) and does not follow the controller. The NPC's arm seems to move only through its IK target, which `HandleRagdoll.OnGrab` parents to the player's controller grip. Whether a real player's hand drags the arm harder in the headset is not known.

Next step: find out why the player's hand stays put while it holds an NPC (the grab's joint, `HandleRagdoll.RefreshJointAndCollision`). If the harness grab differs from a real one, fix the harness; if not, the headset will show the same slow arm, and then Min Slam Speed and the game's 2 m/s floor are a question for the mod. In the headset the reach rule is the same: a player slamming a wrist onto a low table must step in or crouch.

## Build

```sh
dotnet test                                    # unit tests (no game needed)
dotnet build src/SlamDisarm -c Release         # mod in dist/SlamDisarm
tools/harness.sh                               # Linux: headless runs via BasHarness (harness/*.txt)
tools/deploy.sh                                # Linux: both installs, md5 checked
# other install path:  -p:BasDir="/path/to/Blade & Sorcery"
```

The csproj finds the game at `C:\Steam\steamapps\common\Blade & Sorcery` on Windows and `~/.local/share/Steam/steamapps/common/Blade & Sorcery` on Linux.

Log: `Player.log` of the game; all lines start with `[SlamDisarm]`.
