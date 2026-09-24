# LoLItems addon — Overlord's Bloodmail

This folder contains a new item for **Debonairesnake6/LoLItems**
(https://github.com/Debonairesnake6/LoLItems), the Risk of Rain 2 mod that
ports League of Legends items into the game. It's built by hand-copying the
patterns already used by that mod's other items (see `Heartsteel.cs` /
`_BaseItem.cs` in that repo), not generated from scratch.

I don't have push access to that repo (and it isn't yours to push to
directly), so these files live here for you to drop into your local clone of
the mod.

## What's new

- `LolItems/OverlordsBloodmail.cs` — the new item.
  - Base stats: +30 attack damage, +550 max health per stack (both
    configurable in the BepInEx config).
  - **Tyranny**: bonus attack damage equal to 2.5% of your "bonus health"
    (health above your survivor's innate level-1 base, i.e. from levels +
    other items). Configurable percent.
  - **Retribution**: 0–12% bonus attack damage, scaling with missing health
    (0% at full HP, max at 0 HP). This is taken as a fraction of your
    *current* total attack damage as an approximation of "damage from other
    sources" — RoR2's stat system doesn't cleanly expose that distinction
    mid-recalculation, so this is the practical equivalent.
  - Tooltip shows the live bonus damage from each passive, same as how
    Heartsteel shows health gained/damage dealt.
- `LolItems/MyAssets.cs` — added `LoadCustomIcon(fileName)`. The mod's
  existing icons are baked into a Unity `AssetBundle` binary
  (`Assets/icons`), which requires the Unity Editor to rebuild — not
  something buildable outside Unity. This helper instead loads a loose PNG
  straight off disk at runtime via `ImageConversion.LoadImage`, so new items
  don't need a Unity rebuild at all. Falls back to the vanilla "mystery"
  icon with a log warning if the file is missing.

## How to apply this to your mod project

1. Copy `LolItems/OverlordsBloodmail.cs` into your clone's `LolItems/`
   folder.
2. Replace your `LolItems/MyAssets.cs` with the one here (it's the same
   file plus the new `LoadCustomIcon` method — nothing else changed).
3. In `LolItems/LoLItems.cs`, inside `Awake()`, add one line near the other
   `*.Init()` calls:
   ```csharp
   OverlordsBloodmail.Init();
   ```
4. Create a `CustomIcons` folder next to the built mod DLL (i.e. next to
   `LoLItems.dll` in the BepInEx plugins folder, same place `icons` /
   `prefabs` already sit), and drop your icon PNG in there named
   `OverlordsBloodmail.png`. Any icon you extract/crop from the LoL wiki
   works — square, ideally 128x128 or larger.
5. Build as normal. If the PNG isn't found it'll log a warning and fall
   back to the default mystery icon rather than failing to load.

No custom pickup model is set up (it uses the game's default placeholder
model on the ground) since that requires a 3D asset — the visual pickup
icon and in-inventory tooltip icon both use your PNG regardless.

## Adding more items the same way

Overlord's Bloodmail is meant as the template for the rest of your list.
For each new item:
1. Copy `OverlordsBloodmail.cs` as a starting point (or `_BaseItem.cs` from
   the mod repo for a blank slate).
2. Swap in the item's real name/stats/passive math in `CreateItem()`,
   `LoadConfig()`, `RecalculateStatsAPI_GetStatCoefficients()`, and
   `AddTokens()`.
3. Add its `Init()` call in `LoLItems.cs`.
4. Drop its icon PNG in `CustomIcons/`.

Send me the next item's tooltip text (like you did for Bloodmail) and I'll
write the `.cs` file the same way.
