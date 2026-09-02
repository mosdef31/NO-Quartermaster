# Quartermaster

A convoy menu mod for Nuclear Option. Add your own options to the convoy purchase
screen, build them in game or write them by hand, and share them with your server.

[![Release](https://img.shields.io/github/v/release/mosdef31/NO-Quartermaster)](https://github.com/mosdef31/NO-Quartermaster/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/mosdef31/NO-Quartermaster/total)](https://github.com/mosdef31/NO-Quartermaster/releases)
[![Game](https://img.shields.io/badge/Nuclear%20Option-0.34.2-blue)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![Licence](https://img.shields.io/badge/licence-MIT-green)](LICENSE)
[![Issues](https://img.shields.io/github/issues/mosdef31/NO-Quartermaster)](https://github.com/mosdef31/NO-Quartermaster/issues)

[Download](https://github.com/mosdef31/NO-Quartermaster/releases/latest) |
[Changelog](CHANGELOG.md) |
[Credits](ATTRIBUTION.md) |
[Report a bug](https://github.com/mosdef31/NO-Quartermaster/issues)

## What it is

The convoy purchase screen offers a handful of fixed bundles. Quartermaster lets you
add your own, out of any ground vehicle in the game.

### In the game

- **A convoy editor.** A window listing every ground vehicle, with a search box and
  plus and minus buttons. Build a list, name it, save it.
- **A buy menu that scrolls.** The stock panel was sized for four options and clips
  anything past that. It scrolls now, however many you add.

### In the file

- **Lists you write by hand.** `quartermaster.json` sits beside the mod and holds
  every list. The editor writes the same file, so the two never disagree.
- **Sections.** Group your lists under names of your own.
- **Button icons.** Point a list at an image, or leave it out and it borrows the
  icon of its first unit.

The mod ships with one list to try it out: a **Rocket Artillery Battery** of four
MSV MLRS launchers, two munitions trucks and two MRAPs.

## Installing

Needs BepInEx. Put the `Quartermaster` folder in `BepInEx/plugins`. The mod writes a
starter `quartermaster.json` beside its DLL the first time you run the game.

## The editor

Tick **ShowEditor** in the mod's settings to open it. It has three columns:

- **Ground vehicles**, everything you can put in a convoy, with a search box. The
  plus button adds one to the list you are editing.
- **The list you are editing**, with its name, its section, its icon and its
  cooldown, and a row per unit with `-5`, `-1`, `+1` and `+5`.
- **Your lists**, to pick one to edit, add a new one, or delete one.

**Save and apply** writes the file and reloads it, so what you just built is in the
buy menu straight away. **Reload from file** throws away anything unsaved and reads
the file again.

Save between missions rather than during one. Saving changes the order of the buy
menu, and in multiplayer that order is what a purchase is sent as.

## Editing the file directly

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

**If you make a mistake in a list, the mod says which line it is on**, skips that one
list and reads all the others. Look for `Quartermaster` in `BepInEx/LogOutput.log`.

### Sections

Lists can be grouped under names of your own:

```json
{
  "convoys": [],
  "sections": [
    {
      "name": "armour",
      "convoys": [
        {
          "name": "Tank Company",
          "units": [ { "id": "MBT1", "count": 6 } ]
        }
      ]
    }
  ]
}
```

A section is a grouping, not a scope. Every list in every section is offered in the
game exactly as a top level one is, and the order of the file is the order of the
buttons. A file with only `convoys` and no `sections` still works.

## Button icons

Put the image in the mod's own folder, or in a folder inside it, and name it from
there:

```json
"icon": "icons/battery.png"
```

- PNG and JPG both work. Any size; it is scaled to the button.
- Forward slashes are easiest. If you write backslashes, **double them**,
  `"icons\\battery.png"`, because a single one means something else in this kind of
  file.
- A full path starting with a drive letter is refused, so that a list you post still
  works for whoever downloads it.
- If the image cannot be used, the button falls back to the icon of the first unit in
  the list, and the log says why.

Leave `icon` out entirely and you get that fallback, which is usually the right
answer: it always exists and it shows what is in the bundle.

## Settings

They are in `BepInEx/config/com.quartermaster.cfg`, and you can also edit them in
Configuration Manager if you have it.

| Setting | Default | Does |
|---|---|---|
| `Enabled` | `true` | Turn the mod off without removing it. Off means nothing is added and no file is read or written |
| `ShowEditor` | `false` | Show the in game convoy editor |
| `Diagnostics` | `false` | Extra log lines describing what was read, what each list parsed to and what each icon loaded as |

Problems are logged whichever way `Diagnostics` is set. It only adds the commentary
around them.

## Multiplayer

Everyone on a server needs the same file. Buying an option sends its **position** in
the list, not its name, so a player whose list differs from the server's would buy
whatever is in that position instead.

The mod checks for this. The host publishes a fingerprint of its lists, and a player
whose lists do not match has their custom options turned off for that mission, with a
line in the log saying so. You lose the extra options for that game rather than
buying something nobody picked. Copy the host's `quartermaster.json` to match.

Icons are not read from your folder in multiplayer, so nobody needs the same images
as anybody else. Buttons use the icon of the list's first unit instead.

## What it does not do

Buying a convoy does not spawn anything that drives. It adds the units to what your
faction is allowed to field, which is what the stock convoy options do too.

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

The editor lists all of them, so you do not have to work from this table.

## AI use

I use an AI agent to help with coding, refactoring, asset modification, and
authoring long bodies of text and lore.

It raises the quality ceiling beyond what my own skills currently guarantee, while I
learn and develop them. Every decision, every number, and everything that ships is
mine.

## About this source

`src/` is the mod's C# with the comments stripped. It is there to read, not to build:
there is no project file and no game assemblies, so it will not compile as it stands.

See [ATTRIBUTION.md](ATTRIBUTION.md) for anything in here that is not my own work.
