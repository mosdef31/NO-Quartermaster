# Quartermaster

**Adds your own options to the convoy purchase menu, read from a text file you can
edit yourself.**

[![Latest release](https://img.shields.io/github/v/release/mosdef31/NO-Quartermaster?style=for-the-badge&label=download&color=2ea043)](https://github.com/mosdef31/NO-Quartermaster/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/mosdef31/NO-Quartermaster/total?style=for-the-badge&color=blue)](https://github.com/mosdef31/NO-Quartermaster/releases)
[![Game version](https://img.shields.io/badge/Nuclear%20Option-0.34%2B-orange?style=for-the-badge)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![Issues](https://img.shields.io/github/issues/mosdef31/NO-Quartermaster?style=for-the-badge&color=orange)](https://github.com/mosdef31/NO-Quartermaster/issues)

📥 **[Download](https://github.com/mosdef31/NO-Quartermaster/releases/latest)** &nbsp;·&nbsp;
📝 **[What's new](./CHANGELOG.md)** &nbsp;·&nbsp;
🐛 **[Report a bug](https://github.com/mosdef31/NO-Quartermaster/issues)**

---

![The convoy purchase menu with a custom list in it](./images/quartermaster.png)

The mod ships with five lists to try it out, in three sections: artillery, armour
and air defence, plus recon and supply. The first of them is a **Rocket Artillery
Battery** of four MSV MLRS launchers, two munitions trucks and two MRAPs.

You can build your own either in the game, in the editor below, or by editing the
text file yourself. Both write the same file.

## Installing

Needs BepInEx. Put the `Quartermaster` folder in `BepInEx/plugins`. The mod
writes a starter `quartermaster.json` beside its DLL the first time you run the
game.

## The in-game editor

Press **F7**, or tick `ShowEditor` in the settings. The window has three columns:
every ground vehicle in the game on the left, the list you are editing in the
middle, and your own lists on the right.

- **Search or filter the catalogue.** The buttons under the search box are the
  game's own vehicle categories: trucks, UGVs, LCVs, AFVs, MBTs, artillery, AAA,
  IR and radar SAMs, and radars.
- **Press `+` beside a vehicle** to put it in the list you are editing, then use
  `-5 -1 +1 +5` on its row to set how many.
- **The cost strip under the units** shows what the bundle costs, worked out the
  same way the buy menu works it out, ammunition included. It turns red when the
  bundle costs more than you have to spend.
- **Offered to** picks which factions get the list. Leave it on Every faction
  unless you have a reason not to; a list only some factions get sits at a
  different position in each faction's menu, which matters in multiplayer.
- **New, Clone, Copy, Paste and Delete** are on the right. Copy puts one list on
  your clipboard as a single line that you can send to somebody else, and Paste
  adds it to your file without overwriting anything you already have.
- **The arrows reorder your lists**, which is the order the buttons appear in.
- **Save and apply** writes the file and refreshes the buy menu in one press.

The window is drawn at a size that suits your screen. If it is too large or too
small for yours, drag its bottom right corner, or set `UiScale` in the settings.

**Save between missions rather than during one.** A purchase crosses the network
as a position in the list, so changing the order mid-mission can buy the wrong
bundle.

## Editing the file by hand

Open `BepInEx/plugins/Quartermaster/quartermaster.json`.

```json
{
  "convoys": [
    {
      "name": "Rocket Artillery Battery",
      "cooldown": 120,
      "factions": [],
      "icon": "icons/battery.png",
      "units": [
        { "id": "Truck2-MLRS", "count": 4 },
        { "id": "Truck2-M",    "count": 2 },
        { "id": "Truck2-MRAP", "count": 2 }
      ]
    }
  ]
}
```

| Field | What it does |
|---|---|
| `name` | The text on the button. Also how the mod knows one list from another, so give each one its own |
| `cooldown` | Seconds before you can buy that option again. The game's own options use 60 |
| `factions` | Which factions get the option, by faction name. Leave it empty for all of them |
| `icon` | Optional. An image for the button, named from the mod's own folder |
| `units` | What is in the bundle |
| `id` | The unit's id, not its display name. `Truck2-MLRS`, not `MSV MLRS` |
| `count` | How many of that unit |

Only `name` and `units` are required. The price is worked out from the units
themselves, so there is nothing to set. Delete `quartermaster.json` and start the
game to get the starter file back.

**If you make a mistake in the file, the mod says which line it is on** and adds
nothing that run. Look for `Quartermaster` in `BepInEx/LogOutput.log`.

## Button icons

Put the image in the mod's own folder, or in a folder inside it, and name it from
there:

```json
"icon": "icons/battery.png"
```

- PNG and JPG both work. Any size; it is scaled to the button.
- Forward slashes are easiest. If you write backslashes, **double them**,
  `"icons\\battery.png"`, because a single one means something else in this kind
  of file.
- A full path starting with a drive letter is refused, so that a list you post
  still works for whoever downloads it.
- If the image cannot be used, the button falls back to the icon of the first
  unit in the list, and the log says why.

Leave `icon` out entirely and you get that fallback, which is usually the right
answer: it always exists and it shows what is in the bundle.

## Settings

They are in `BepInEx/config/com.quartermaster.cfg`, and you can also edit them in
Configuration Manager if you have it.

| Setting | Default | Does |
|---|---|---|
| `Enabled` | `true` | Turn the mod off without removing it. Off means nothing is added and no file is read or written |
| `Diagnostics` | `false` | Extra log lines describing what was read, what each list parsed to and what each icon loaded as |
| `ShowEditor` | `false` | Show the in-game convoy editor. The same thing as the toggle key and the window's own Close button, and all three always agree |
| `ToggleKey` | `F7` | The key that shows and hides the editor. Modifiers are allowed, written like `LeftControl + F7` |
| `UiScale` | `0` | How large the editor is drawn. `0` works it out from your screen height, which is right on almost every machine. `1` is the smallest readable size and `2` suits a 4K panel. Takes effect straight away |

Problems are logged whichever way `Diagnostics` is set. It only adds the
commentary around them.

## Multiplayer

**Everyone on a server uses the host's file.** Copy the host's `quartermaster.json`
into your own `BepInEx/plugins/Quartermaster` folder before you join, or have them
send you their lists with the editor's Copy button and paste them in.

The editor shows a **fingerprint** of your lists along the top of its window. Two
players comparing eight characters before a mission know at once whether their
files agree.

Buying an option sends its **position** in the list, not its name, so a player
whose list differs from the host's would buy whatever sits in that position
instead. The mod will not let that happen: the host publishes a fingerprint of
its list, and a player whose list does not match loses the custom options for the
mission and is told why. You get the stock buy menu rather than the wrong convoy.

## What it does not do

Buying a convoy does not spawn anything that drives. It adds the units to what
your faction is allowed to field, which is what the stock convoy options do too.

## Some ids to start from

| Id | Unit |
|---|---|
| `Truck2-MLRS` | MSV MLRS |
| `HLT-MART` | HLT Mobile Artillery |
| `MBT1` | Spearhead MBT |
| `MBT` | Type-12 MBT |
| `AFV8_IFV` | AFV8 IFV |
| `AFV8_SAM` | AFV8 Mobile Air Defense |
| `Truck2-M` | MSV Munitions |
| `Truck2-MRAP` | MSV MRAP |
| `SPAAG1` | AeroSentry SPAAG |
| `RadarSAM1` | T9K41 Boltstrike |

## AI use

I use an AI agent to help with coding, refactoring, asset modification, and authoring
long bodies of text and lore.

It raises the quality ceiling beyond what my own skills currently guarantee, while I
learn and develop them. Every decision, every number, and everything that ships is mine.
