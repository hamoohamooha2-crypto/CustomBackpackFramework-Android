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
using xTile.Dimensions;

namespace CustomBackpack
{
    public partial class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static IModHelper H;
        internal static IMonitor M;
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
                () => H.Translation.Get("GMCM_Option_ModEnabled_Name").ToString(),
                () => H.Translation.Get("GMCM_Option_ModEnabled_Desc").ToString(),
                () => Config.ModEnabled,
                v => Config.ModEnabled = v
            );

            api.AddNumberOption(
                ModManifest,
                () => H.Translation.Get("GMCM_Option_MinHandleHeight_Name").ToString(),
                () => H.Translation.Get("GMCM_Option_MinHandleHeight_Desc").ToString(),
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

                foreach (var size in Data.Keys.OrderBy(p => p))
                {
                    if (Game1.player.MaxItems < size)
                    {
                        Game1.currentLocation.createQuestionDialogue(
                            H.Translation.Get("farmer-bought-x").ToString(),
                            new[]
                            {
                                new Response("Yes", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")),
                                new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No"))
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

                foreach (var size in Data.Keys.OrderBy(p => p))
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
}
