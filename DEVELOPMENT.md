# Slam Disarm - development notes

Everything about how the mod is built and tested: versions, the game APIs used and what is verified vs. assumed, how a slam is decided, test status. The player-facing description and the Mod Menu reference are in [`README.md`](README.md); the spec is [`CLAUDE.md`](CLAUDE.md).

## Status

| Step | State |
|---|---|
| 1. Project, pure logic + unit tests, game glue, Mod Menu, smoke scenario | built 2026-11-22 on Windows. **Unit tests: 90/90 pass** (`dotnet test`; written first: 63 of 90 failed on the stubs, the 27 option-spec tests passed, then all green after the logic). **Release build: 0 warnings, 0 errors** against the Windows install's `ThunderRoad.dll`. |
| 2. Headless smoke run | `harness/smoke.txt` written (load lines, 16 options with defaults, live toggles, "Turned off" log line, no exceptions). **Not run**: BasHarness runs on Linux + Proton only, and this was built on Windows. |
| 3. Real slam in the game | **Not run**: no harness step can grab an NPC's hand and slam it (see "Harness"). Needs the headset (or the harness steps below). |

Nothing in this repo has run inside the game yet. Every "verified" below means read in the decompiled DLL and compiled against it.

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
| Holding an NPC's wrist uses a handle on the hand or forearm part (not the upper arm) | Which grabs count | `GRAB` line on grab; nothing logged means the grab did not count |
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

`harness/smoke.txt` uses only existing BasHarness steps. A real slam test needs steps BasHarness does not have (BasHarness README, 2026-11-22):

1. `player-grab-npc-part <RagdollPart.Type> [hand=right]` - the player's hand grabs a handle of the spawned NPC's part (`RagdollHand.Grab(HandleRagdoll)`), so the ragdoll counts as grabbed.
2. A solid object next to the NPC's hand: either a step that spawns a static box collider (no `ColliderGroup`, = World) at an offset from the NPC, or an NPC spawned next to an Arena wall.
3. A controller move that drives the held hand through that object at a set speed (like `player-push`, towards a point instead of the NPC), repeated for Slams To Disarm.

Then: `expect-npc holding False` after the second slam, `expect-log SLAM`, a regrab check (`npc-grab` during No Regrab Time → released). Without those steps the slam itself is a headset check.

## Build

```sh
dotnet test                                    # unit tests (no game needed)
dotnet build src/SlamDisarm -c Release         # mod in dist/SlamDisarm
tools/harness.sh harness/smoke.txt             # Linux: headless smoke run via BasHarness
tools/deploy.sh                                # Linux: both installs, md5 checked
# other install path:  -p:BasDir="/path/to/Blade & Sorcery"
```

The csproj finds the game at `C:\Steam\steamapps\common\Blade & Sorcery` on Windows and `~/.local/share/Steam/steamapps/common/Blade & Sorcery` on Linux.

Log: `Player.log` of the game; all lines start with `[SlamDisarm]`.
