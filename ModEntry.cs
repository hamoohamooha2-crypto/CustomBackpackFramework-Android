using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace CustomBackpack
{
    public partial class ModEntry : Mod
    {
        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;
        public static ModEntry context;

        public static string dictPath = "platinummyr.CustomBackpackFramework/dictionary";
        public static Dictionary<int, BackPackData> dataDict = new();

        public static Texture2D scrollTexture;
        public static Texture2D handleTexture;

        public static PerScreen<bool> scrolling = new();
        public static PerScreen<int> oldScrollValue = new();
        public static PerScreen<int> scrollChange = new();
        public static PerScreen<Rectangle> scrollArea = new();

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            if (!Config.ModEnabled)
                return;

            context = this;
            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.SaveLoaded += (_, _) => LoadDict();
            helper.Events.GameLoop.DayStarted += (_, _) => LoadDict();
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.Content.AssetRequested += Content_AssetRequested;

            helper.ConsoleCommands.Add(
                "custombackpack",
                "custombackpack <slots>",
                SetSlots
            );

            var harmony = new Harmony(ModManifest.UniqueID);
            PatchHandler.SafePatchAll(harmony, Monitor);

            scrollTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            scrollTexture.SetData(new[] { Config.BackgroundColor });

            handleTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            handleTexture.SetData(new[] { Config.HandleColor });
        }

        private void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (scrolling.Value &&
                (Game1.activeClickableMenu == null ||
                 Game1.input.GetMouseState().LeftButton != ButtonState.Pressed))
            {
                scrolling.Value = false;
            }

            int newValue = Game1.input.GetMouseState().ScrollWheelValue;
            scrollChange.Value =
                oldScrollValue.Value > newValue ? 1 :
                oldScrollValue.Value < newValue ? -1 : 0;

            oldScrollValue.Value = newValue;
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu != null &&
                e.Button == SButton.MouseLeft &&
                scrollArea.Value.Contains(Game1.getMouseX(), Game1.getMouseY()))
            {
                scrolling.Value = true;
            }
        }

        private void SetSlots(string _, string[] args)
        {
            if (args.Length == 1 && int.TryParse(args[0], out int slots))
                SetPlayerSlots(slots);
        }

        public void SetPlayerSlots(int slots)
        {
            if (Game1.player == null)
                return;

            Game1.player.MaxItems = slots;
            while (Game1.player.Items.Count < slots)
                Game1.player.Items.Add(null);
        }

        private void LoadDict()
        {
            try
            {
                dataDict = Game1.content.Load<Dictionary<int, BackPackData>>(dictPath) ?? new();
                foreach (var k in dataDict.Keys.ToArray())
                    dataDict[k].texture = SHelper.GameContent.Load<Texture2D>(dataDict[k].texturePath);
            }
            catch { }
        }

        private void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(dictPath))
            {
                e.LoadFrom(
                    () => new Dictionary<int, BackPackData>(),
                    StardewModdingAPI.Events.AssetLoadPriority.Exclusive
                );
            }
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu"
            );

            if (gmcm == null)
                return;

            gmcm.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("GMCM_ModEnabled"),
                getValue: () => Config.ModEnabled,
                setValue: value => Config.ModEnabled = value
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("GMCM_MinHandleHeight"),
                getValue: () => Config.MinHandleHeight,
                setValue: value => Config.MinHandleHeight = value
            );
        }
    }
}
