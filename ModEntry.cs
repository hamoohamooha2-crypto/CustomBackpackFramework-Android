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
        public static Dictionary<int, BackPackData> dataDict = new Dictionary<int, BackPackData>();
        public static int IDOffset = 0;
        public static Texture2D scrollTexture;
        public static Texture2D handleTexture;
        public static PerScreen<IClickableMenu> lastMenu = new PerScreen<IClickableMenu>();
        public static PerScreen<int> pressTime = new PerScreen<int>();
        public static PerScreen<int> oldScrollValue = new PerScreen<int>();
        public static PerScreen<int> oldCapacity = new PerScreen<int>();
        public static PerScreen<int> oldRows = new PerScreen<int>();
        public static PerScreen<int> oldScrolled = new PerScreen<int>();
        public static PerScreen<int> scrolled = new PerScreen<int>();
        public static PerScreen<int> scrollChange = new PerScreen<int>();
        public static PerScreen<int> scrollWidth = new PerScreen<int>(() => 4);
        public static PerScreen<bool> scrolling = new PerScreen<bool>();
        public static PerScreen<Rectangle> scrollArea = new PerScreen<Rectangle>();

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            if (!Config.ModEnabled) return;

            context = this;
            SMonitor = Monitor;
            SHelper = helper;

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
            scrollTexture.SetData(new Color[] { Config.BackgroundColor });
            handleTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            handleTexture.SetData(new Color[] { Config.HandleColor });
        }

        private void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (scrolling.Value && (Game1.activeClickableMenu is null || Game1.input.GetMouseState().LeftButton != ButtonState.Pressed))
                scrolling.Value = false;
            
            var newScrollValue = Game1.input.GetMouseState().ScrollWheelValue;
            scrollChange.Value = (oldScrollValue.Value > newScrollValue) ? 1 : (oldScrollValue.Value < newScrollValue ? -1 : 0);
            oldScrollValue.Value = newScrollValue;
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is not null && e.Button == SButton.MouseLeft && scrollArea.Value.Contains(Game1.getMouseX(), Game1.getMouseY()))
                scrolling.Value = true;
        }

        public override object GetApi() => new CustomBackpackApi();

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

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e) => LoadDict();
        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e) => LoadDict();

        private void LoadDict()
        {
            try
            {
                dataDict = Game1.content.Load<Dictionary<int, BackPackData>>(dictPath);
                if (dataDict == null) return;
                foreach (var key in dataDict.Keys.ToArray())
                    dataDict[key].texture = Helper.GameContent.Load<Texture2D>(dataDict[key].texturePath);
                SHelper.GameContent.InvalidateCache("String/UI");
            }
            catch { }
        }

        private void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(dictPath))
                e.LoadFrom(() => new Dictionary<int, BackPackData>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            else if (e.NameWithoutLocale.IsEquivalentTo("String/UI"))
                e.Edit(EditStrings);
        }

        private void EditStrings(IAssetData obj)
        {
            var editor = obj.AsDictionary<string, string>();
            foreach (var key in dataDict.Keys.ToArray())
                editor.Data[$"Chat_CustomBackpack_{key}"] = string.Format(SHelper.Translation.Get("farmer-bought-x"));
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(mod: ModManifest, reset: () => Config = new ModConfig(), save: () => Helper.WriteConfig(Config));
            configMenu.AddBoolOption(mod: ModManifest, name: () => ModEntry.SHelper.Translation.Get("GMCM_Option_ModEnabled_Name"), getValue: () => Config.ModEnabled, setValue: value => Config.ModEnabled = value);
            configMenu.AddNumberOption(mod: ModManifest, name: () => ModEntry.SHelper.Translation.Get("GMCM_Option_MinHandleHeight_Name"), getValue: () => Config.MinHandleHeight, setValue: value => Config.MinHandleHeight = value);
        }
    }

    public static class PatchHandler
    {
        public static void SafePatchAll(Harmony harmony, Type patchClass, IMonitor monitor)
        {
            PatchSafe(harmony, monitor, patchClass, FindConstructor(typeof(InventoryMenu)), null, "InventoryMenu_Postfix", "InventoryMenu Constructor");
            PatchSafe(harmony, monitor, patchClass, FindMethod(typeof(InventoryMenu), "draw"), "InventoryMenu_draw_Prefix", "InventoryMenu_draw_Postfix", "InventoryMenu Draw");
            PatchSafe(harmony, monitor, patchClass, FindMethod(typeof(InventoryMenu), "rightClick"), "InventoryMenu_rightClick_Prefix", null, "InventoryMenu RightClick");
            PatchSafe(harmony, monitor, patchClass, FindMethod(typeof(InventoryMenu), "leftClick"), "InventoryMenu_leftClick_Prefix", null, "InventoryMenu LeftClick");
            PatchSafe(harmony, monitor, patchClass, FindMethod(typeof(InventoryMenu), "hover"), "InventoryMenu_hover_Prefix", null, "InventoryMenu Hover");
            PatchSafe(harmony, monitor, patchClass, FindMethod(typeof(InventoryMenu), "getInventoryPositionOfClick"), "InventoryMenu_getInventoryPositionOfClick_Prefix", null, "InventoryMenu ClickPos");
            PatchSafe(harmony, monitor, patchClass, FindMethod(typeof(GameLocation), "performAction"), "GameLocation_performAction_Prefix", null, "GameLocation PerformAction");
            PatchSafe(harmony, monitor, patchClass, FindMethod(typeof(GameLocation), "answerDialogueAction"), "GameLocation_answerDialogueAction_Prefix", null, "GameLocation AnswerDialogue");
            PatchSafe(harmony, monitor, patchClass, AccessTools.Method(typeof(Farmer), "shiftToolbar"), "Farmer_shiftToolbar_Prefix", null, "Farmer ShiftToolbar");
            PatchSafe(harmony, monitor, patchClass, AccessTools.Method(typeof(IClickableMenu), "applyMovementKey"), "IClickableMenu_applyMovementKey_Prefix", null, "MovementKey");
            PatchSafe(harmony, monitor, patchClass, AccessTools.Method(typeof(InventoryMenu), "setUpForGamePadMode"), null, "InventoryMenu_setUpForGamePadMode_Postfix", "GamePadMode");
        }

        private static MethodBase FindConstructor(Type type)
        {
            return type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        }

        private static MethodBase FindMethod(Type type, string name)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                       .Where(m => m.Name == name)
                       .OrderByDescending(m => m.GetParameters().Length)
                       .FirstOrDefault();
        }

        private static void PatchSafe(Harmony harmony, IMonitor monitor, Type patchClass, MethodBase original, string prefixName, string postfixName, string debugName)
        {
            if (original == null) return;
            try
            {
                HarmonyMethod prefix = prefixName != null ? new HarmonyMethod(patchClass, prefixName) : null;
                HarmonyMethod postfix = postfixName != null ? new HarmonyMethod(patchClass, postfixName) : null;
                harmony.Patch(original, prefix, postfix);
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to patch {debugName}: {ex.Message}", LogLevel.Error);
            }
        }
    }
}
