# Changelog

## 1.2.1

- **The donate menu grows with your lists.** Adding options used to push the
  buttons past the bottom of the panel, where they drew over the funds readout.
  The list now takes as much room as the menu has and scrolls once it runs out.
- **Buttons no longer stack.** Opening the donate menu a second time left the
  previous set of buttons in place and built a new set on top of them.

## 1.2.0

- **The buy menu scrolls.** A list longer than the panel used to be clipped, so
  options past the fourth or fifth were drawn where you could not reach them.
- **An in-game editor**, opened with the `ShowEditor` setting. It lists every
  ground vehicle in the game, with a search box, and builds a convoy out of them
  with plus and minus buttons. Lists can be created, renamed, edited and deleted,
  and saving writes the file for you.
- Lists can be grouped into named **sections** in the file.
- **One bad list no longer costs you the others.** The mistake is reported with
  its line number and the rest of the file is still read.
- **A multiplayer check.** If your lists are not the host's, your custom options
  are turned off for that mission and you are told why, so a purchase cannot buy
  a bundle you did not pick. Icons come from the units themselves in multiplayer,
  so no image file has to match between machines.

## 1.1.0

Renamed from **Convoy Lists** to **Quartermaster**. The list file is
`quartermaster.json` now, and the settings file is `com.quartermaster.cfg`. The
mod had not been released under the old name, so nothing carries over and nothing
is lost.

- **Custom options appear.** The old reader accepted the shipped starter file and
  returned nothing from it, silently, so no button was ever added. The file is
  read by this mod's own parser now.
- **A mistake in the file is reported with its line number**, instead of costing
  you every option with no message.
- Lists can name their own **button icon**, as an image file in the mod's folder.
- Two settings: `Enabled` to turn the mod off without removing it, and
  `Diagnostics` for extra log lines when something is not appearing.

## 1.0.0

First release.

- Custom convoy options read from `convoy.json` beside the DLL.
- Priced from the units themselves, per-faction, with a configurable cooldown.
