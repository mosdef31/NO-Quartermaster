# Changelog

## 1.4.0

- **A vehicle row adds from anywhere along it.** The whole row is the button, from its
  icon across to its price.
- **Vehicles wear their own icons.** The `+` is replaced by the icon the game
  draws for that unit, so the catalogue reads the way the rest of the game does.
  A unit the game has no icon for keeps the `+`.
- **Your lists show the icon they will have in the buy menu.** It is the icon you
  named, or the icon of the first unit in the list if you named none, which is
  what the buy menu has always done, and until now there was nowhere to see it.
- **Hover a vehicle to see what it is armed with.** In either panel of the editor,
  resting the cursor on a vehicle shows its weapons, how many stations carry each
  one, and how many rounds each of those holds.
- **The vehicle list fits.** The price and the role used to slide off the right
  edge into a sideways scrollbar on any long vehicle name. The column is wider,
  every cell is measured, and nothing scrolls sideways any more.
- **Switch a list off without deleting it.** Press `on` beside a list to take it
  out of the buy menu and leave it in your file. It keeps its units, its icon and
  its place, and a list that is off does not count towards the multiplayer check.
- **Import and export the whole file.** Export writes a timestamped copy beside
  `quartermaster.json` and puts the lot on your clipboard. Import adds what is on
  the clipboard to what you have; Replace swaps everything for it, and takes a
  copy first.
- **A soft warning on an expensive list.** A bundle over $200m says so in one
  line. It is a warning and nothing else: the list still saves, still appears and
  can still be bought. Set your own figure with `BudgetWarningAbove`, or `0` to
  turn it off.
- **The buy menu grows to fit its options.** It measured the room it had once,
  against furniture that may not even have been on screen, and then kept that
  number for the rest of the mission. It now re-measures as the menu changes.
- **The buy list scrolls smoothly.** The wheel used to jump it.
- **Section names are headings.** They were printed underneath the lists they
  belonged to, which read as a label on the wrong list. They are now above them.
- **Bundle prices include ammunition, at last.** The cost strip had been quoting
  hull prices only. It asked each vehicle what its ammunition was worth at a
  moment when the vehicle had no weapons on it yet, so the answer came back as
  nothing every time.
- **The vehicle list is never empty any more.** Open the editor before the game
  has finished loading its units and the list came up blank and stayed blank for
  the rest of the session, because the empty answer was cached. It asks again
  every frame until there is something to show, and says on screen why it is
  waiting.
- **Your lists fit their column.** The right-hand column had the same fault the
  vehicle list had: a long name pushed the row wider than the panel and slid the
  name behind a sideways scrollbar. Every cell is measured now.
- **A bigger window, with the empty strip at the bottom gone.** The three columns
  used to stop short of the footer and leave a band of dead space under the saving
  note. The footer is measured, so the columns run right down to it.
- **The armament tooltip appears under the cursor.** It was drawn well above and
  to the left of the pointer, and it was larger than it needed to be. It is
  smaller, shorter, and directly below the cursor.
- **`Left Alt` + `` ` `` opens the editor**, replacing `F7`.
- **The editor no longer comes back open.** Quitting with the window up saved it
  as open, so the next launch drew it over the main menu. Every session now starts
  with it hidden.
- **`BuyListHeight`** sets how tall the convoy list in the buy menu may grow, for
  anyone who wants it larger than the room the menu measures.

## 1.3.0

- **A rebuilt editor.** Dark, readable, and drawn at a size that matches your
  screen instead of a fixed number of pixels, so it is no longer half the size on
  a 4K panel. The window is larger, it can be resized by its bottom right corner,
  and no panel draws over another at any size.
- **A key opens it.** `F7` by default, and rebindable in the settings.
- **The unit count is bigger and clearer**, so the number you are adjusting is the
  one your eye lands on.
- **Live cost while you edit.** The strip under the units shows what the bundle
  costs, worked out exactly the way the buy menu works it out, ammunition
  included. It turns red when the bundle costs more than you can spend.
- **Faction targeting has a control.** The `factions` field has always been in the
  file and there was no way to set it without editing JSON. Now every faction the
  game knows about is a toggle.
- **Filter the catalogue by role.** Tanks, artillery, SAMs, trucks and the rest,
  by the game's own categories.
- **Unit ids that resolve to nothing are visible.** They used to appear only in
  the log and as a question mark on one row. They are now red, with the reason
  beside them, and counted along the top of the window.
- **Clone a list**, to start a variant without rebuilding it.
- **Reorder lists** with the arrows, which is the order the buttons appear in.
- **Share one list as a paste.** Copy puts it on your clipboard as a single line;
  Paste adds it to your file. It never overwrites a list you already have.
- **The multiplayer fingerprint is on screen**, along the top of the editor, with
  whether you match the host. Two players can compare it before a mission instead
  of finding out during one.
- **Five starter lists instead of one**, across three sections: artillery, armour,
  air defence, recon and supply.
- **The category buttons fit their own words.** TRUCK and IR SAM were cut short to
  RUCK and IR S. Every filter button is now measured against the longest label in
  the set and they are all the same width, so the row reads as a grid.
- **Selecting a button no longer changes its size.** A pressed filter used to
  switch to a larger, bolder typeface in a cell of the same width, which is what
  clipped IR SAM the moment you chose it. Selection is the colour and the border
  now, and nothing moves.
- **The vehicle list is one weight throughout.** Every other row looked heavier
  than its neighbours. Nothing was sized differently; the stripe behind the text
  was light enough to change how the letters resolved. The stripe is fainter and
  the rows are an equal height.
- **Scrollbars match the window.** They were the stock grey Unity bars with an
  arrow at each end. They are now a dark gutter with a thumb that lights up under
  the pointer.
- **Text lines up.** Names, roles and prices in a row share a centre line instead
  of hanging from the top of their own boxes, and prices are right aligned so the
  column can be read down.
- **The multiplayer line only speaks when it has something to say.** It used to
  read "not checked yet" for an entire single player session. Matched and
  mismatched still announce themselves, and the fingerprint is always shown.

## 1.2.2

- **Deleting a list now actually deletes it.** "Delete selected" only ever changed
  the copy in memory, so the list stayed in `quartermaster.json` and its option
  stayed in the donate menu until you noticed a one line status asking you to press
  Save. Delete now removes the list, writes the file and refreshes the buy menu in
  one press. If the write is refused, the list comes back and you are told why.
- **The donate menu no longer draws over the panel below it.** The menu measured
  the free space above the contribute slider correctly and then threw the reading
  away, because it was smaller than the box the list was drawn in. That box is the
  overlap, so the list kept covering the slider no matter how many entries you had.
  The measurement is trusted now, and the list scrolls inside it.

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
