using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Reflection;

namespace CustomBackpack
{
    public static class PatchHandler
    {
        public static void SafePatchAll(Harmony harmony, IMonitor monitor)
        {
            Patch(harmony, monitor, typeof(GameLocation), "performAction", "GameLocation_performAction_Prefix");
            Patch(harmony, monitor, typeof(GameLocation), "answerDialogueAction", "GameLocation_answerDialogueAction_Prefix");
            Patch(harmony, monitor, typeof(Farmer), "shiftToolbar", "Farmer_shiftToolbar_Prefix");
            Patch(harmony, monitor, typeof(InventoryMenu), "draw", "InventoryMenu_draw_Prefix", "InventoryMenu_draw_Postfix");
        }

        private static void Patch(
            Harmony harmony,
            IMonitor monitor,
            Type type,
            string methodName,
            string prefix = null,
            string postfix = null
        )
        {
            try
            {
                var original = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == methodName)
                    .OrderByDescending(m => m.GetParameters().Length)
                    .FirstOrDefault();

                if (original == null)
                    return;

                harmony.Patch(
                    original,
                    prefix != null ? new HarmonyMethod(typeof(ObjectPatches), prefix) : null,
                    postfix != null ? new HarmonyMethod(typeof(ObjectPatches), postfix) : null
                );
            }
            catch (Exception ex)
            {
                monitor.Log($"Patch failed: {type.Name}.{methodName}\n{ex}", LogLevel.Error);
            }
        }
    }
}
