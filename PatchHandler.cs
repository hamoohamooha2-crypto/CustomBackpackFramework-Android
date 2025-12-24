using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CustomBackpack
{
    public static class PatchHandler
    {
        public static void SafePatchAll(Harmony harmony, Type patchClass)
        {
            var invConstructor = AccessTools.Constructor(typeof(InventoryMenu), new[] { typeof(int), typeof(int), typeof(bool), typeof(IList<Item>), typeof(InventoryMenu.highlightThisItem), typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool) })
                                ?? AccessTools.Constructor(typeof(InventoryMenu), new[] { typeof(int), typeof(int), typeof(bool), typeof(IList<Item>), typeof(InventoryMenu.highlightThisItem), typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool) });
            
            if (invConstructor != null)
                harmony.Patch(invConstructor, postfix: new HarmonyMethod(patchClass, "InventoryMenu_Postfix"));

            var drawMethod = AccessTools.Method(typeof(InventoryMenu), nameof(InventoryMenu.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(int) })
                            ?? AccessTools.Method(typeof(InventoryMenu), nameof(InventoryMenu.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int) });

            if (drawMethod != null)
            {
                harmony.Patch(drawMethod, prefix: new HarmonyMethod(patchClass, "InventoryMenu_draw_Prefix"));
                harmony.Patch(drawMethod, postfix: new HarmonyMethod(patchClass, "InventoryMenu_draw_Postfix"));
            }

            var rightClick = AccessTools.Method(typeof(InventoryMenu), nameof(InventoryMenu.rightClick), new[] { typeof(int), typeof(int), typeof(Item), typeof(bool), typeof(bool) })
                            ?? AccessTools.Method(typeof(InventoryMenu), nameof(InventoryMenu.rightClick), new[] { typeof(int), typeof(int), typeof(Item) });

            if (rightClick != null)
                harmony.Patch(rightClick, prefix: new HarmonyMethod(patchClass, "InventoryMenu_rightClick_Prefix"));

            var performAction = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.performAction), new[] { typeof(string[]), typeof(Farmer), typeof(xTile.Dimensions.Location) })
                               ?? AccessTools.Method(typeof(GameLocation), nameof(GameLocation.performAction), new[] { typeof(string[]), typeof(Farmer), typeof(Location) });

            if (performAction != null)
                harmony.Patch(performAction, prefix: new HarmonyMethod(patchClass, "GameLocation_performAction_Prefix"));

            var answerDialogue = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.answerDialogueAction), new[] { typeof(string), typeof(string[]) });
            if (answerDialogue != null)
                harmony.Patch(answerDialogue, prefix: new HarmonyMethod(patchClass, "GameLocation_answerDialogueAction_Prefix"));

            harmony.Patch(AccessTools.Method(typeof(Farmer), nameof(Farmer.shiftToolbar)), prefix: new HarmonyMethod(patchClass, "Farmer_shiftToolbar_Prefix"));
            harmony.Patch(AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.applyMovementKey)), prefix: new HarmonyMethod(patchClass, "IClickableMenu_applyMovementKey_Prefix"));
        }
    }
}
