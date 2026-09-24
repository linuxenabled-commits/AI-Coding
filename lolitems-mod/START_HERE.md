# Handoff note for a local Claude Code session

If you're reading this, you're a fresh Claude Code session running locally
on the user's Windows machine, with no memory of the cloud session that
produced these files. This note is your briefing. Read this whole file
before doing anything.

## What this project is

The user plays modded Risk of Rain 2 with **LoLItems**
(https://github.com/Debonairesnake6/LoLItems), a BepInEx/R2API mod that
ports League of Legends items into the game. We're adding new items to it
that don't exist in the original mod, based on tooltips the user picks from
the LoL item shop and sends over.

A prior cloud session (no local machine access) already:
- Reviewed the mod's existing source and conventions.
- Designed and wrote two new items as C# files, following those conventions
  exactly.
- **Verified the C# actually compiles** by installing a .NET SDK in the
  cloud sandbox and running `dotnet build` against the real project — it
  built clean. The only failures were two Windows-only post-build steps
  (`robocopy`, a bundled Unity weaver `.exe`) that don't exist on Linux but
  will work fine on Windows.
- Could not go further: no access to the user's PC, so no way to actually
  install the build, load the game, or test it.

**Your job starts where that left off**: get these two items actually built
and running in the user's real game, so they can test them. Then keep
building the rest of the list the same way as the user sends more.

## Where the files are

The user should have either:
1. A zip file already downloaded (`lolitems-mod-addon.zip`) containing:
   - `LolItems/OverlordsBloodmail.cs`
   - `LolItems/TheCollector.cs`
   - `LolItems/MyAssets.cs`
   - `README.md`
   - `ITEM_ROADMAP.md`
2. And/or access to the GitHub repo `linuxenabled-commits/AI-Coding`,
   branch `claude/eloquent-goodall-0ndsar`, folder `lolitems-mod/` — same
   files, plus this one.

Ask the user which they have handy, or check both. **Read `README.md` and
`ITEM_ROADMAP.md` from that folder in full before writing or changing
anything** — they contain the actual design decisions (see summary below,
but the roadmap file is the source of truth and may have been updated since
this note was written).

The user also has a local extracted copy of the LoLItems mod **source**
(they downloaded it from GitHub separately, per earlier instructions) —
ask where that folder is if it's not obvious.

## Immediate task: get it building and installed

1. Confirm the user has the LoLItems mod source extracted somewhere (from
   https://github.com/Debonairesnake6/LoLItems, "Download ZIP"). If not,
   help them get it.
2. Copy `OverlordsBloodmail.cs`, `TheCollector.cs`, and `MyAssets.cs` into
   that source's `LolItems/` subfolder (overwrite `MyAssets.cs`).
3. In that source's `LolItems/LoLItems.cs`, inside `Awake()`, make sure
   these two lines are present (add them near the other `*.Init()` calls if
   missing):
   ```csharp
   OverlordsBloodmail.Init();
   TheCollector.Init();
   ```
4. Check whether the user has the .NET SDK installed (`dotnet --version` in
   a terminal). If not, help them install it from
   https://dotnet.microsoft.com/download — this is lighter weight than a
   full Visual Studio install and should be enough, since the repo already
   bundles the Windows-only weaver tool the build needs.
5. From the source's `LolItems/` folder, run:
   ```
   dotnet build LoLItems.csproj -c Release
   ```
   If it fails, read the actual error (don't guess) and fix it. If you get
   stuck on something that looks like a design decision rather than a typo,
   surface it to the user rather than guessing.
6. On success, the DLL is at
   `LolItems/bin/Release/netstandard2.1/LoLItems.dll`.
7. Find where the user's mod manager (r2modman / Thunderstore Mod Manager)
   actually loads plugins from — usually reachable via a "browse profile
   folder" button in the manager's settings for the LoLItems profile. Look
   for the existing `LoLItems.dll` under `BepInEx/plugins/`.
8. Rename the existing `LoLItems.dll` to `LoLItems.dll.bak` (so it's easy to
   revert), then copy the freshly built one into that same folder.
9. In that same folder, create a `CustomIcons` subfolder if it doesn't
   exist. If the user has icon PNGs for `OverlordsBloodmail.png` and
   `TheCollector.png`, put them there (exact filenames matter). If not,
   the items will just use a placeholder icon — not a blocker, just less
   pretty.
10. Have the user launch the game **through their mod manager** (not
    directly via Steam) so BepInEx loads.
11. Suggest enabling the game's dev console (`-console` launch arg in the
    mod manager) so the user can spawn the items directly instead of
    hunting for a drop:
    ```
    give_item overlordsbloodmail 1
    give_item thecollector 1
    ```

If anything in the build or load fails, help the user read the actual
error/log (Visual Studio's Output window, or BepInEx's `LogOutput.log`)
rather than guessing blind.

## Standing design decisions (also in ITEM_ROADMAP.md — check there for the current version)

- No mana on any item — RoR2 doesn't have the resource, so mana-based
  passives get reworked around health/flat values.
- Magic resist and armor both map to RoR2's single armor stat.
- Ability haste → cooldown reduction.
- Lethality / armor penetration → folded into flat bonus damage (no true
  armor-pen exists in RoR2's stat hooks).
- Omnivamp/lifesteal → one unified "vamp" stat, healing off any damage
  instance with `procCoefficient > 0` (covers most ability damage too, not
  just basic attacks).
- AD vs. AP as genuinely separate stats is an open, not-yet-built idea
  (see roadmap for the mechanism it would need). Until built, "AP" items
  just add flat damage like everything else.

## Ongoing work: building more items

`ITEM_ROADMAP.md` lists every item discussed so far, its status, and
item-specific notes. When the user sends a new item's tooltip (a
screenshot or pasted text, same format as the ones already done):

1. Read `OverlordsBloodmail.cs` and `TheCollector.cs` first — new items
   should follow their exact structure and conventions (config entries,
   `ItemDef` setup, `RecalculateStatsAPI` hooks, tooltip display pattern,
   `AddTokens()` style).
2. Think through how the item's real LoL passive maps onto what RoR2
   actually exposes — flag anything that doesn't map cleanly (new stats,
   new mechanics) and explain the tradeoff **before** writing code, the way
   the cloud session did. Don't just silently improvise a mechanic the user
   hasn't seen reasoned through.
3. Write the `.cs` file, register its `Init()` call in `LoLItems.cs`,
   rebuild, and get it installed the same way as above.
4. Update `ITEM_ROADMAP.md`'s status for that item.

Some upcoming items need shared infrastructure that doesn't exist yet
(a periodic "pulse while in combat" AoE system for Sunfire Aegis/Unending
Despair; a stored-damage-released-as-DoT system for Death's Dance, which
can reuse Liandry's existing `DotController` pattern already in the mod).
Build that infra once, not duplicated per item.

## One more thing

Don't assume you have git push access to `Debonairesnake6/LoLItems` (the
original mod) — you don't, and it isn't the user's repo to push to. Changes
belong in the user's own repo/files, applied locally to their copy of the
mod source.
