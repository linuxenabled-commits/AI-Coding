using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class TheCollector
    {
        public static ItemDef myItemDef;

        public static ConfigEntry<float> BonusDamage { get; set; }
        public static ConfigEntry<float> LethalityDamage { get; set; }
        public static ConfigEntry<float> CritChancePercent { get; set; }
        public static ConfigEntry<float> ExecuteThresholdPercent { get; set; }
        public static ConfigEntry<int> GoldPerKill { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];

        // Guards against the follow-up execute hit re-triggering itself through the same TakeDamage hook.
        private static readonly HashSet<HealthComponent> executingHealthComponents = [];

        internal static void Init()
        {
            LoadConfig();
            if (!Enabled.Value)
            {
                return;
            }

            CreateItem();
            AddTokens();
            ItemDisplayRuleDict displayRules = new(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "TheCollector");
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Rarity",
                "Tier3Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            BonusDamage = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Damage Per Stack",
                50f,
                "Flat attack damage granted per stack."
            );

            LethalityDamage = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Lethality As Damage Per Stack",
                15f,
                "Flat attack damage granted per stack, standing in for the item's lethality (armor penetration has no RoR2 equivalent)."
            );

            CritChancePercent = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Crit Chance Percent Per Stack",
                25f,
                "Critical strike chance granted per stack."
            );

            ExecuteThresholdPercent = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Execute Threshold Percent",
                5f,
                "Enemies dealt damage that would leave them below this percent of their max health are executed instead."
            );

            GoldPerKill = LoLItems.MyConfig.Bind(
                "TheCollector",
                "Gold Per Kill Per Stack",
                25,
                "Bonus gold granted per stack for every kill."
            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "TheCollector";
            myItemDef.nameToken = "TheCollector";
            myItemDef.pickupToken = "TheCollectorItem";
            myItemDef.descriptionToken = "TheCollectorDesc";
            myItemDef.loreToken = "TheCollectorLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = LegacyResourcesAPI.Load<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value));
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.LoadCustomIcon("TheCollector.png");
            // No custom pickup model yet, falls back to the base game's placeholder model.
            myItemDef.pickupModelReference = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Mystery.PickupMystery_prefab);
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage, ItemTag.OnKillEffect ];
        }

        private static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);
                TryExecute(self, damageInfo);
            };

            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, globalEventManager, damageReport) =>
            {
                orig(globalEventManager, damageReport);

                if (!NetworkServer.active)
                    return;

                CharacterMaster attackerMaster = damageReport.attackerMaster;
                int inventoryCount = attackerMaster?.inventory?.GetItemCountEffective(myItemDef.itemIndex) ?? 0;
                if (inventoryCount > 0)
                {
                    attackerMaster.GiveMoney((uint)(GoldPerKill.Value * inventoryCount));
                }
            };
        }

        private static void TryExecute(HealthComponent self, DamageInfo damageInfo)
        {
            if (!self || !self.alive || executingHealthComponents.Contains(self))
                return;

            if (!damageInfo.attacker)
                return;

            CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            int inventoryCount = attackerBody?.inventory?.GetItemCountEffective(myItemDef.itemIndex) ?? 0;
            if (inventoryCount <= 0)
                return;

            if (self.fullHealth <= 0f || self.health / self.fullHealth > ExecuteThresholdPercent.Value / 100f)
                return;

            executingHealthComponents.Add(self);

            // Copy the triggering hit and massively overkill it so armor/reductions can't
            // let the target survive - this is what actually "executes" them.
            DamageInfo executeDamage = damageInfo;
            executeDamage.damage = self.fullHealth * 100f;
            executeDamage.procCoefficient = 0f;
            executeDamage.crit = false;
            self.TakeDamage(executeDamage);

            executingHealthComponents.Remove(self);
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int inventoryCount = characterBody?.inventory?.GetItemCountEffective(myItemDef.itemIndex) ?? 0;
            if (inventoryCount <= 0)
                return;

            args.baseDamageAdd += (BonusDamage.Value + LethalityDamage.Value) * inventoryCount;
            args.critAdd += CritChancePercent.Value * inventoryCount;
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            return (Language.GetString(myItemDef.descriptionToken), "");
        }

        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("TheCollector", "The Collector");

            // Short description
            LanguageAPI.Add("TheCollectorItem", "Gain attack damage and critical strike chance. Executes low health enemies and pays you for every kill.");

            // Long description
            LanguageAPI.Add("TheCollectorDesc",
                "Adds <style=cIsDamage>" + (BonusDamage.Value + LethalityDamage.Value) + "</style> <style=cStack>(+" + (BonusDamage.Value + LethalityDamage.Value) + ")</style> attack damage and <style=cIsDamage>" + CritChancePercent.Value + "%</style> <style=cStack>(+" + CritChancePercent.Value + "%)</style> critical strike chance." +
                "<br><br><style=cIsUtility>Death</style>: Damage that would leave an enemy below <style=cIsHealth>" + ExecuteThresholdPercent.Value + "%</style> health executes them instead." +
                "<br><style=cIsUtility>Taxes</style>: Killing an enemy grants <style=cIsUtility>" + GoldPerKill.Value + "</style> <style=cStack>(+" + GoldPerKill.Value + ")</style> bonus gold."
            );

            // Lore
            LanguageAPI.Add("TheCollectorLore", "\"Everyone has a price.\"");
        }
    }
}
