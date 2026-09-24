# LoLItems addon roadmap

Running list of items discussed for the LoLItems addon, and the design
decisions made for each. Update this file as items get built or the plan
changes.

## Standing decisions (apply to every item below)

- **No mana.** Any passive that reads/converts mana is dropped or reworked
  around health/flat values instead.
- **Armor / magic resist → one armor stat.** RoR2 only has a single armor
  stat. Anything that grants or reduces "magic resist" in the source item
  just uses armor instead.
- **Ability haste → cooldown reduction** (`cooldownReductionAdd` /
  `*CooldownMultAdd`).
- **Lethality / armor penetration → flat bonus damage.** RoR2 doesn't
  expose a way to ignore/reduce a target's armor mid-calculation, so these
  get folded into a flat AD bump instead of true penetration.
- **AD/AP split**: undecided — possible via tagging which skill slot
  (Primary vs. Secondary/Utility/Special) is currently firing and routing
  damage bonuses through an AD-only or AP-only bucket accordingly. Real
  infrastructure work (not per-item), and imperfect for delayed
  detonations/summons/pets. Not started; current items below use "AP"
  interchangeably with flat damage until/unless we build this.
- **Omnivamp / lifesteal → one unified "vamp" stat**, healing off any
  damage instance with `procCoefficient > 0`. This already covers most
  ability damage too, not just basic attacks, so there's no need to split
  physical/spell vamp.

## Items

| Item | Status | Notes |
|---|---|---|
| Overlord's Bloodmail | ✅ Done | `OverlordsBloodmail.cs`. Tyranny (AD from bonus health) + Retribution (AD from missing health, approximated off last frame's total damage). |
| The Collector | ✅ Done | `TheCollector.cs`. Sub-5% execute + gold on kill. See PR notes. |
| Abyssal Mask | Not started | Aura debuff (enemies within range take +12% damage). Lethality/haste/health as usual. |
| Hubris | Not started | Reuses Mejai's Soulstealer's stack-on-kill pattern. Open question: does a takedown need to credit only the killing blow (like Mejai's) or anyone who hit the target in the last 3s? |
| Riftmaker | Not started | AP → flat damage. Void Corruption = ramping in-combat damage buff. Void Infusion reuses Bloodmail's bonus-health formula. Omnivamp via the shared vamp approach above. |
| Jak'Sho, The Protean | Not started | Armor/magic resist both → armor. Passive needs a "bonus armor" concept mirroring Bloodmail's bonus-health trick, using `baseArmor` as the innate value. |
| Banshee's Veil | Not started | AP → flat damage. Spell-shield passive: block next hit taken once 40s pass without being hit, resetting whenever hit — a real timer/negation feature, not a stat. |
| Navori Flickerblade | Not started | Attack speed/crit/move speed trivial. Passive (basic attack cuts remaining ability cooldowns by 15%) needs new infra: reach into `GenericSkill`'s recharge timer per hit, and distinguish basic attack from ability hits. First item needing this. |
| Rabadon's Deathcap | Already exists (`Rabadons.cs`) | Existing version is flat +30% damage, no base AP stat line. Decide: leave as-is, or add a flat damage stat to match the screenshot's base +130 AP. |
| Guardian Angel | Not started | Revive-on-lethal, modeled on vanilla RoR2's "Dio's Best Friend" (survive once per cooldown + heal) rather than a true 4s invulnerable/untargetable/can't-act freeze, which needs real character-state-machine work. No mana clause (dropped per standing decision) — substitute "restore mana" with refreshing skill cooldowns. |
| Eclipse | Not started | Simplified per your note: "attack twice within a window → shield + bonus damage," dropping the per-cast-instance-type stacking and ICD nuance from the real item. |
| Fimbulwinter | Not started | No mana (dropped per standing decision) — Awe (mana→health) and Everlasting's mana-scaled shield both need reworking to flat values. Melee-only clause needs a hardcoded per-survivor melee/ranged list (RoR2 doesn't expose this as a flag). |
| Sunfire Aegis | Not started | New "pulse while in combat" infra: periodic AoE tick centered on self, gated by recent damage dealt/taken. Minion/monster/champion damage tiers collapse to one flat multiplier. Shared infra with Unending Despair. |
| Unending Despair | Not started | Same pulse infra as Sunfire Aegis, but AoE damage + self-heal off damage dealt (reuses the vamp math). |
| Death's Dance | Not started | Reuses Liandry's existing custom `DotController` pattern for "stored pain released as true damage over 3s." Champion/monster split maps cleanly to `CharacterBody.isPlayerControlled`. Defy is a straightforward kill-within-window check. |
| Lord Dominik's Regards | Not started | Armor pen → flat AD. Scaling-vs-target's-bonus-health reuses Bloodmail's formula, just computed against the victim instead of the attacker. |

## Build order reasoning

Doing The Collector first since it needs no new shared infrastructure — good
sanity check before investing in the heavier systems (pulse-AoE, stored-damage
DoT, skill-slot tagging for AD/AP) that several of the later items depend on.
