# Slam Disarm - development notes

Everything about how the mod is built and tested: versions, the game APIs used and what is verified vs. assumed, how a slam is decided, test status. The player-facing description and the Mod Menu reference are in [`README.md`](README.md); the spec is [`CLAUDE.md`](CLAUDE.md).

## Status

| Step | State |
|---|---|
| 1. Project, pure logic + unit tests, game glue, Mod Menu, smoke scenario | built 2026-11-22 on Windows. **Unit tests: 90/90 pass** (`dotnet test`; written first: 63 of 90 failed on the stubs, the 27 option-spec tests passed, then all green after the logic). **Release build: 0 warnings, 0 errors** against the Windows install's `ThunderRoad.dll`. |
| 2. Headless runs | **2/2 pass** on Linux (2026-11-22, `artifacts/harness/20261122-210619`): `0-smoke.txt` (renamed from `smoke.txt` so it runs first; load lines, 16 options with defaults, live toggles, "Turned off" line, no exceptions) and `1-grab.txt` (a real grab of the NPC's forearm starts the watch, letting go stops it, Forearm Grab Counts off starts nothing). No code change was needed. |
| 3. Real slam in the game | **Passes headless** (2026-11-23, v0.1.1, `harness/2-slam.txt`, suite 3/3 in `artifacts/harness/20261123-140340`, the slam scenario alone 3/3 more): two slams of the held hand onto a block, `World` at 2.59-2.68 m/s, disarm, no regrab. Needed a fix in the mod: the game raises no collision events for a standing NPC's hand (see "Harness"). **Unit tests: 96/96.** Headset check owed. |

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
| Contacts | `CollisionHandler.OnCollisionStartEvent` (`CollisionEvent(CollisionInstance)`), `RagdollPart.collisionHandler`, `Item.collisionHandlers`, `CollisionHandler.active`, `Ragdoll.state` | raised from `OnCollisionEnter` → `InvokeCollisionStart`, only while `active` (see below) |
| What was hit | `CollisionInstance.targetColliderGroup` (null for colliders without a group), `.collisionHandler.isRagdollPart`, `.collisionHandler.item`, `Item.isTelekinesisGrabbed`, `Item.holder` | |
| Slam speed | `CollisionInstance.impactVelocity` | `CollisionHandler.CalculateImpactVelocity` at the contact point |
| Disarm | `RagdollHand.TryRelease()`, `EventManager.InvokeDisarm(creature, hand, handle, hit)` | the game's hand-hit disarm and DMCS do the same; `SkillCombatFocus.OnDisarm` gives the player focus for any NPC disarm |
| Pain voice | `BrainData.GetModule<BrainModuleSpeak>(false).Play("hit", true)` | every human voice in the 1.3.2 catalog (`Voices/*.json`) has the dialog `hit` |
| Haptic pulse | `RagdollHand.HapticTick(intensity)` | only acts on a player hand (`playerHand.controlHand.HapticShort`) |
| Hand Damage | `Creature.Damage(float, DamageType.Blunt)` | builds a `CollisionInstance` with no body part and push level 0. The game's grab disarm needs push level ≥ 2 and its hand-hit disarm push level == 2 (`BrainModuleEquipment`, defaults in `Brains/*.json`), so neither fires; this mod listens to no hit event, so it cannot loop |
| Cleanup | `EventManager.onCreatureDespawn`, `onCreatureKill`, `onLevelUnload` | |

### Engine behaviour that shapes the code (read, not seen in game)

- **A standing NPC's hand raises no contacts, grabbed or not.** `Ragdoll.RefreshPartJointAndCollision` turns every part's `CollisionHandler.active` off unless the ragdoll is Inert, Destabilized or Frozen; a held `HandleRagdoll` turns it back on only for the parts in its `activateCollisionOnParts`, and the wrist handle (`HumanWrist`) lists Head, Neck, Torso, LeftArm and RightArm, not the hands (measured headless 2026-11-23). So the mod turns the watched NPC hand's events on while it watches the grab and gives back only what it changed (`Logic/HandEvents`). The forearm's events are on already. On top of that, a standing ragdoll nobody grabbed raises nothing at all (`IsStandingUngrabbedRagdoll`); holding only its weapon leaves the weapon's own contacts as the slams.
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

Debug Logging lines (`[SlamDisarm]` prefix): `GRAB | <npc> | watching ...` / `stopped watching`, `SLAM | <npc> | World at 4.2 m/s (<npc> LeftHand 'Palm' -> world '<collider>') | 1/2` (in brackets: which collider met which), `SKIP | <npc> | ... | TooSlow (count 0/2)`, `DISARM | <npc> | Right hand dropped '<item>' | no regrab for 5.0 s`, `REGRAB | ...`.

## Performance

Per frame: one read of each player hand's grab (no allocation; a struct and a few reference checks) and, only while NPCs are locked, a check of their two hands. Listeners exist only while a grab counts; every contact decision is a few comparisons. Log strings are only built with Debug Logging on. Not measured in the game.

## Harness

| File | Checks |
|---|---|
| `0-smoke.txt` | load lines, 16 options with categories and defaults, live toggles, "Turned off" line, no exceptions |
| `1-grab.txt` | `player-grab-npc LeftArm section=Lower` on a HumanDummy holding a sword: `GRAB ... watching Left hand with 'SwordShortCommon'`, `stopped watching` on release; with Forearm Grab Counts off no watch |
| `2-slam.txt` | the player grabs the NPC's sword forearm, lifts it up and out, slams the hand down onto a block twice: `SLAM ... World`, `DISARM`, the sword stays out of the hand for No Regrab Time; filmed |
| `probe/*.txt` | not run by `tools/harness.sh`: the measurements behind the slam scenario (where the bodies are, a sideways slam, straight-down slams) |

BasHarness got the steps for this on 2026-11-22: `player-grab-npc` (the controller reaches for the handle, then `RagdollHand.Grab`), `player-slam` (controller moved at a set speed, real physics), `place-block` (a static box with the ground's layer and physics material, so it counts as World). On 2026-11-23: `player-eyes` (head at the creature's eye height), `place-block from=npc-hand` and its refusal of a block inside a body, and a per-frame `player-slam` trace in Player.log; later that day the trace got the held body's speed, the grip's joints, the NPC hand's collision state and every raw physics contact during the slam.

**How the slam scenario came to pass** (2026-11-22 and -23, about 40 runs in `artifacts/harness/20261122-20*` and `20261123-*`, probes in `harness/probe/`):

- Blocks need a physics material the game knows (`CollisionHandler.OnCollisionEnter` logs `Target physics material not found` and raises no event). `place-block` copies the ground's.
- A block that appears inside a body flings it out. The first 0.6 m block sat inside both men's legs and the sword and tore the grab loose. `place-block` now refuses that and names each body.
- The mock headset sat 0.16 m above the creature's eyes, and the game measures a grab's reach from the head. `player-eyes` moves the head to the eyes; since then the grab holds through every slam.
- The arm cannot be pulled far down from where it hangs. His hand hangs in front of his thigh: a block under it sits inside the leg, and below that the shoulder's reach ends after 0.24 m (2.5 m/s at best). The scenario lifts the arm 0.3 m and moves it 0.2 m out first; then the hand falls 0.24 m onto the block at about 2.6 m/s. The game's grip joint pulls the 12 kg forearm with 5000 N/m, at most 3000 N (`Handle.SetJointConfig`, `CreatureData` defaults).
- **The bug in the mod:** the palm hit the block at 2.7 m/s (raw contact) and the mod saw nothing, because the game keeps a standing NPC's hand's collision events off (see "Engine behaviour"). Same scenario before and after the fix: `artifacts/harness/20261123-140247` (no `SLAM`, the sword stays in the hand, filmed) and `20261123-140157` (two `SLAM ... World`, `DISARM`, filmed).

Open for the headset: in the harness the arm meets the block at 2.6-2.7 m/s with the controller moving at 6 m/s, just above Min Slam Speed (2.5). Whether real slams land well above it is not known yet; the `SLAM`/`SKIP` lines with Debug Logging show every contact's speed.

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
