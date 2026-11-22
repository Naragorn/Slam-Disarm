# Slam Disarm

A Blade & Sorcery mod. Grab an enemy's weapon hand and slam it into a wall, the floor or a crate until they let go of their weapon.

## How it works

- Grab their wrist or forearm while they hold a weapon. Grabbing their weapon itself works too.
- Slam their hand (or the weapon) hard into a wall, the floor or a loose object.
- Until they let go, each slam makes them cry out and your hand buzzes: the grip is loosening.
- After two slams (you can change this) they drop the weapon.
- For a few seconds after that they cannot pick up or draw a weapon.
- Hitting yourself or another enemy with their hand does not count. Neither does hitting a weapon someone holds.
- Shields stay in their hand unless you allow it.

## Install

Copy the `SlamDisarm` folder into
`Blade & Sorcery/BladeAndSorcery_Data/StreamingAssets/Mods`, or install it with your mod manager.

## Mod Menu

Open the book, go to **Mods**, pick **Slam Disarm**. Changes apply at once.

**Slam Disarm**

| Setting | Default | Range | What it does |
|---|---|---|---|
| Slam Disarm Enabled | on | | Turns the mod on or off |
| Min Slam Speed | 2.5 m/s | 0.5–8 m/s | How hard a slam must be |
| Slams To Disarm | 2 | 1–5 | Slams until they let go |
| Slam Window | 3 s | 1–10 s | Time for the next slam before the count starts over |
| Slam Cooldown | 0.3 s | 0.1–1 s | Bumps closer together count as one slam |
| Disarm Chance | 100 % | 0–100 % | Chance the last slam works; if not, the next one tries again |
| Objects Count | on | | Loose objects count, not only walls and the floor |
| Forearm Grab Counts | on | | Holding their forearm works too |
| Weapon Grab Counts | on | | Holding their weapon works too |
| Shields Can Be Slammed | off | | Shields can be slammed loose too |
| Drop Both Hands | off | | They drop what is in the other hand as well |
| No Regrab Time | 5 s | 0–30 s | How long they cannot pick up a weapon again |
| Pain Sound | on | | They cry out on each slam |
| Haptic Pulse | on | | Your hand buzzes on each slam |
| Hand Damage | 0 | 0–20 | Damage each slam deals |

**Debug**

| Setting | Default | What it does |
|---|---|---|
| Debug Logging | off | Writes every slam and disarm to the game log, for bug reports |

## Works with

- **Disarm Squeeze** and other disarm mods should work side by side: whoever makes the enemy let go first wins. Not tested together yet.
- A slam disarm counts as a disarm for the game, so skills that reward disarming should reward it too.

## Known limits

- Not tested in the game yet. The default slam strength is a first guess.
