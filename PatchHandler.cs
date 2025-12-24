using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using StardewModdingAPI;
using System.Linq;

namespace CustomBackpack
{
    public static class PatchHandler
    {
        public static void SafePatchAll(Harmony harmony, Type patchClass, IMonitor monitor)
        {
            // 1. InventoryMenu Constructor
            var invConstructor = FindConstructor(typeof(InventoryMenu));
            PatchSafe(harmony, monitor, patchClass, invConstructor, null, "InventoryMenu_Postfix", "InventoryMenu Constructor");

            // 2. InventoryMenu.draw
            var drawMethod = FindMethod(typeof(InventoryMenu), "draw");
            PatchSafe(harmony, monitor, patchClass, drawMethod, "InventoryMenu_draw_Prefix", "InventoryMenu_draw_Postfix", "InventoryMenu Draw");

            // 3. InventoryMenu.rightClick
            var rightClick = FindMethod(typeof(InventoryMenu), "rightClick");
            PatchSafe(harmony, monitor, patchClass, rightClick, "InventoryMenu_rightClick_Prefix", null, "InventoryMenu RightClick");

            // 4. GameLocation.performAction
            var performAction = FindMethod(typeof(GameLocation), "performAction");
            PatchSafe(harmony, monitor, patchClass, performAction, "GameLocation_performAction_Prefix", null, "GameLocation PerformAction");

            // 5. GameLocation.answerDialogueAction
            var answerDialogue = FindMethod(typeof(GameLocation), "answerDialogueAction");
            PatchSafe(harmony, monitor, patchClass, answerDialogue, "GameLocation_answerDialogueAction_Prefix", null, "GameLocation AnswerDialogue");

            // 6. Standard Hooks
            PatchSafe(harmony, monitor, patchClass, AccessTools.Method(typeof(Farmer), "shiftToolbar"), "Farmer_shiftToolbar_Prefix", null, "Farmer ShiftToolbar");
            PatchSafe(harmony, monitor, patchClass, AccessTools.Method(typeof(IClickableMenu), "applyMovementKey"), "IClickableMenu_applyMovementKey_Prefix", null, "MovementKey");
        }

        private static MethodBase FindConstructor(Type type)
        {
            // Mobile 1.6 Constructor can vary (10 or 11 params). We take the one with the most parameters.
            return type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        }

        private static MethodBase FindMethod(Type type, string name)
        {
            // Android builds sometimes rename or overload methods. 
            // We look for the method by name and match the most likely candidate for patching.
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                       .Where(m => m.Name == name)
                       .OrderByDescending(m => m.GetParameters().Length)
                       .FirstOrDefault();
        }

        private static void PatchSafe(Harmony harmony, IMonitor monitor, Type patchClass, MethodBase original, string prefixName, string postfixName, string debugName)
        {
            if (original == null)
            {
                monitor.Log($"[Critical] {debugName} not found! This feature will be disabled.", LogLevel.Warn);
                return;
            }

            try
            {
                HarmonyMethod prefix = prefixName != null ? new HarmonyMethod(patchClass, prefixName) : null;
                HarmonyMethod postfix = postfixName != null ? new HarmonyMethod(patchClass, postfixName) : null;
                
                harmony.Patch(original, prefix, postfix);
                monitor.Log($"[Success] {debugName} patched (Params: {original.GetParameters().Length})", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"[Error] Failed to patch {debugName}: {ex.Message}", LogLevel.Error);
            }
        }
    }
}
