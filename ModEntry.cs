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
    public partial class ModEntry : Mod
    {
        internal static ModEntry instance;
        internal static IModHelper helperGlobal;
        internal static IMonitor monitorGlobal;

        public static ModConfig Config;

        public static string dictPath = "platinummyr.CustomBackpackFramework/dictionary";
        public static Dictionary<int, BackPackData> dataDict = new();

        public static int IDOffset = 0;
        public static Texture2D scrollTexture;
        public static Texture2D handleTexture;

        public static PerScreen<IClickableMenu> lastMenu = new();
        public static PerScreen<int> pressTime = new();
        public static PerScreen<int> oldScrollValue = new();
        public static PerScreen<int> oldCapacity = new();
        public static PerScreen<int> oldRows = new();
        public static PerScreen<int> oldScrolled = new();
        public static PerScreen<int> scrolled = new();
        public static PerScreen<int> scrollChange = new();
        public static PerScreen<int> scrollWidth = new(() => 4);
        public static PerScreen<bool> scrolling = new();
        public static PerScreen<Rectangle> scrollArea = new();

        public override void Entry(IModHelper helper)
        {
            instance = this;
            helperGlobal = helper;
            monitorGlobal = Monitor;
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.Content.AssetRequested += Content_AssetRequested;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;

            helper.ConsoleCommands.Add("custombackpack", "Usage: custombackpack <slotnumber>", SetSlots);

            var harmony = new Harmony(ModManifest.UniqueID);
            PatchHandler.SafePatchAll(harmony, typeof(ObjectPatches), Monitor);

            scrollTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            scrollTexture.SetData(new[] { Config.BackgroundColor });
            handleTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            handleTexture.SetData(new[] { Config.HandleColor });
        }

        private void SetSlots(string arg1, string[] arg2)
        {
            if (arg2.Length == 1 && int.TryParse(arg2[0], out int slots))
                SetPlayerSlots(slots);
        }

        public void SetPlayerSlots(int slots)
        {
            if (Game1.player == null) return;
            Game1.player.MaxItems = slots;
            while (Game1.player.Items.Count < Game1.player.MaxItems)
                Game1.player.Items.Add(null);
        }

        private void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (scrolling.Value && (Game1.activeClickableMenu == null || Game1.input.GetMouseState().LeftButton != Microsoft.Xna.Framework.Input.ButtonState.Pressed))
                scrolling.Value = false;

            var newScrollValue = Game1.input.GetMouseState().ScrollWheelValue;
            scrollChange.Value = (oldScrollValue.Value > newScrollValue) ? 1 : (oldScrollValue.Value < newScrollValue ? -1 : 0);
            oldScrollValue.Value = newScrollValue;
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu != null && e.Button == SButton.MouseLeft && scrollArea.Value.Contains(Game1.getMouseX(), Game1.getMouseY()))
                scrolling.Value = true;
        }

        private void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(dictPath))
                e.LoadFrom(() => new Dictionary<int, BackPackData>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var api = helperGlobal.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api == null) return;

            api.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () => helperGlobal.WriteConfig(Config)
            );

            api.AddBoolOption(
                ModManifest,
                () => helperGlobal.Translation.Get("GMCM_Option_ModEnabled_Name").ToString(),
                () => helperGlobal.Translation.Get("GMCM_Option_ModEnabled_Desc").ToString(),
                () => Config.ModEnabled,
                value => Config.ModEnabled = value
            );

            api.AddNumberOption(
                ModManifest,
                () => helperGlobal.Translation.Get("GMCM_Option_MinHandleHeight_Name").ToString(),
                () => helperGlobal.Translation.Get("GMCM_Option_MinHandleHeight_Desc").ToString(),
                () => Config.MinHandleHeight,
                value => Config.MinHandleHeight = value
            );
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e) => LoadDict();
        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e) => LoadDict();

        private void LoadDict()
        {
            try
            {
                dataDict = Game1.content.Load<Dictionary<int, BackPackData>>(dictPath);
                if (dataDict == null) return;
                foreach (var key in dataDict.Keys.ToArray())
                    dataDict[key].texture = helperGlobal.GameContent.Load<Texture2D>(dataDict[key].texturePath);
            }
            catch { }
        }
    }
}
