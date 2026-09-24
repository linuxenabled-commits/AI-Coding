using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using UnityEngine.Networking;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class OverlordsBloodmail
    {
        public static ItemDef myItemDef;

        public static ConfigEntry<float> BonusHealth { get; set; }
        public static ConfigEntry<float> BonusDamage { get; set; }
        public static ConfigEntry<float> TyrannyPercent { get; set; }
        public static ConfigEntry<float> RetributionMaxPercent { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];

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
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "OverlordsBloodmail");
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "OverlordsBloodmail",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "OverlordsBloodmail",
                "Rarity",
                "Tier3Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "OverlordsBloodmail",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            BonusHealth = LoLItems.MyConfig.Bind(
                "OverlordsBloodmail",
                "Health Per Stack",
                550f,
                "Flat max health granted per stack."
            );

            BonusDamage = LoLItems.MyConfig.Bind(
                "OverlordsBloodmail",
                "Damage Per Stack",
                30f,
                "Flat attack damage granted per stack."
            );

            TyrannyPercent = LoLItems.MyConfig.Bind(
                "OverlordsBloodmail",
                "Tyranny Percent",
                2.5f,
                "Percentage of bonus health (health above your survivor's innate base) converted into attack damage, per stack."
            );

            RetributionMaxPercent = LoLItems.MyConfig.Bind(
                "OverlordsBloodmail",
                "Retribution Max Percent",
                12f,
                "Maximum percentage of your attack damage granted as bonus attack damage when at 0 health, scaling down to 0% at full health, per stack."
            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "OverlordsBloodmail";
            myItemDef.nameToken = "OverlordsBloodmail";
            myItemDef.pickupToken = "OverlordsBloodmailItem";
            myItemDef.descriptionToken = "OverlordsBloodmailDesc";
            myItemDef.loreToken = "OverlordsBloodmailLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = LegacyResourcesAPI.Load<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value));
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.LoadCustomIcon("OverlordsBloodmail.png");
            // No custom pickup model yet, falls back to the base game's placeholder model.
            // Swap this for a real prefab from Assets/prefabs once one exists.
            myItemDef.pickupModelReference = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Mystery.PickupMystery_prefab);
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage ];
        }

        private static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int inventoryCount = characterBody?.inventory?.GetItemCountEffective(myItemDef.itemIndex) ?? 0;
            if (inventoryCount <= 0)
                return;

            args.baseHealthAdd += BonusHealth.Value * inventoryCount;
            args.baseDamageAdd += BonusDamage.Value * inventoryCount + GetTyrannyDamage(characterBody, inventoryCount) + GetRetributionDamage(characterBody, inventoryCount);
        }

        // Tyranny: bonus health is everything above the survivor's innate (level 1, no items) max health.
        private static float GetTyrannyDamage(CharacterBody characterBody, int inventoryCount)
        {
            float bonusHealth = Mathf.Max(0f, characterBody.maxHealth - characterBody.baseMaxHealth);
            return bonusHealth * (TyrannyPercent.Value / 100f) * inventoryCount;
        }

        // Retribution: scales from 0% at full health to RetributionMaxPercent at 0 health.
        // "Total attack damage from other sources" is approximated using last frame's already-computed
        // damage stat, since the game doesn't expose a clean way to isolate that mid-recalculation.
        private static float GetRetributionDamage(CharacterBody characterBody, int inventoryCount)
        {
            if (!characterBody.healthComponent || characterBody.maxHealth <= 0f)
                return 0f;

            float missingHealthFraction = 1f - Mathf.Clamp01(characterBody.healthComponent.health / characterBody.maxHealth);
            return characterBody.damage * missingHealthFraction * (RetributionMaxPercent.Value / 100f) * inventoryCount;
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");

            string customDescription = "";
            CharacterBody body = masterRef.GetBody();
            int inventoryCount = masterRef.inventory?.GetItemCountEffective(myItemDef.itemIndex) ?? 0;

            if (body && inventoryCount > 0)
            {
                float tyrannyDamage = GetTyrannyDamage(body, inventoryCount);
                float retributionDamage = GetRetributionDamage(body, inventoryCount);

                customDescription += "<br><br>Tyranny bonus damage: " + string.Format("{0:#,##0.##}", tyrannyDamage);
                customDescription += "<br>Retribution bonus damage: " + string.Format("{0:#,##0.##}", retributionDamage);
            }

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("OverlordsBloodmail", "Overlord's Bloodmail");

            // Short description
            LanguageAPI.Add("OverlordsBloodmailItem", "Gain attack damage from bonus health, and more damage the lower your health is.");

            // Long description
            LanguageAPI.Add("OverlordsBloodmailDesc",
                "Adds <style=cIsDamage>" + BonusDamage.Value + "</style> <style=cStack>(+" + BonusDamage.Value + ")</style> attack damage and <style=cIsHealth>" + BonusHealth.Value + "</style> <style=cStack>(+" + BonusHealth.Value + ")</style> max health." +
                "<br><br><style=cIsUtility>Tyranny</style>: Gain bonus attack damage equal to <style=cIsDamage>" + TyrannyPercent.Value + "%</style> of your bonus health." +
                "<br><style=cIsUtility>Retribution</style>: Gain <style=cIsDamage>0-" + RetributionMaxPercent.Value + "%</style> bonus attack damage based on missing health."
            );

            // Lore
            LanguageAPI.Add("OverlordsBloodmailLore", "\"Only between life and death did he find a way to settle the score.\"");
        }
    }
}
