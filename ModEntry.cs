using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using xTile.Dimensions;

namespace CustomBackpack
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static IModHelper H;
        internal static IMonitor M;
        internal static ModConfig Config;
        internal static Dictionary<int, BackpackData> Data = new();

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            H = helper;
            M = Monitor;
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void OnAssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("platinummyr.CustomBackpackFramework/dictionary"))
                e.LoadFrom(() => new Dictionary<int, BackpackData>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
        }

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var api = H.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api == null)
                return;

            api.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () => H.WriteConfig(Config)
            );

            api.AddBoolOption(
                ModManifest,
                () => H.Translation.Get("gmcm.modenabled.name").ToString(),
                () => H.Translation.Get("gmcm.modenabled.desc").ToString(),
                () => Config.ModEnabled,
                v => Config.ModEnabled = v
            );

            api.AddNumberOption(
                ModManifest,
                () => H.Translation.Get("gmcm.minhandleheight.name").ToString(),
                () => H.Translation.Get("gmcm.minhandleheight.desc").ToString(),
                () => Config.MinHandleHeight,
                v => Config.MinHandleHeight = v
            );
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction))]
        private static class PerformActionPatch
        {
            private static bool Prefix(string fullActionString, Farmer who, Location tileLocation, ref bool __result)
            {
                if (!Config.ModEnabled || string.IsNullOrEmpty(fullActionString))
                    return true;

                if (!fullActionString.StartsWith("BuyBackpack"))
                    return true;

                var keys = Data.Keys.OrderBy(p => p).ToList();
                foreach (var size in keys)
                {
                    if (Game1.player.MaxItems < size)
                    {
                        Game1.currentLocation.createQuestionDialogue(
                            Instance.Helper.Translation.Get("backpack.question").ToString().Replace("{{slots}}", size.ToString()),
                            new[]
                            {
                                new Response("Yes", Instance.Helper.Translation.Get("backpack.buy").ToString()),
                                new Response("No", Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"))
                            },
                            "BackpackPurchase"
                        );
                        __result = true;
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.answerDialogueAction))]
        private static class AnswerDialoguePatch
        {
            private static bool Prefix(string questionAndAnswer, string[] questionParams, ref bool __result)
            {
                if (!Config.ModEnabled || questionAndAnswer != "BackpackPurchase_Yes")
                    return true;

                var keys = Data.Keys.OrderBy(p => p).ToList();
                foreach (var size in keys)
                {
                    if (Game1.player.MaxItems < size)
                    {
                        var data = Data[size];
                        if (Game1.player.Money < data.Cost)
                        {
                            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney2"));
                            __result = true;
                            return false;
                        }

                        Game1.player.Money -= data.Cost;
                        Game1.player.increaseBackpackSize(size);
                        Game1.player.holdUpItemThenMessage(new SpecialItem(99, data.Name), true);

                        __result = true;
                        return false;
                    }
                }

                return true;
            }
        }
    }

    public class BackpackData
    {
        public int Cost;
        public string Name;
    }

    public class ModConfig
    {
        public bool ModEnabled = true;
        public int MinHandleHeight = 32;
    }
}
