using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
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
    public partial class ModEntry
    {
        public static ClickableTextureComponent upArrow;
        public static ClickableTextureComponent downArrow;
        public static ClickableTextureComponent expandButton;

        public class ObjectPatches
        {
            public static void InventoryMenu_Postfix(InventoryMenu __instance, int xPosition, int yPosition, bool playerInventory, IList<Item> actualInventory, InventoryMenu.highlightThisItem highlightMethod, int capacity, int rows, int horizontalGap, int verticalGap, bool drawSlots)
            {
                if (!Config.ModEnabled || (__instance.actualInventory != Game1.player.Items) || __instance.capacity >= __instance.actualInventory.Count)
                    return;

                if (capacity != oldCapacity.Value || rows != oldRows.Value)
                {
                    scrolled.Value = 0;
                }
                oldRows.Value = rows;
                oldCapacity.Value = capacity;

                SMonitor.Log($"Created new inventory menu with {__instance.actualInventory.Count} slots", LogLevel.Trace);
                upArrow = new ClickableTextureComponent(new Rectangle(__instance.xPositionOnScreen + 768 + 32 - 50, __instance.yPositionOnScreen - 46, 24, 24), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 12, -1, -1), 0.4f, false)
                {
                    myID = 88,
                    downNeighborID = 90,
                    rightNeighborID = 106,
                    leftNeighborID = -99998
                };
                downArrow = new ClickableTextureComponent(new Rectangle(__instance.xPositionOnScreen + 768 + 32 - 50, __instance.yPositionOnScreen + 192 + 32 - 53, 24, 24), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 11, -1, -1), 0.4f, false)
                {
                    myID = 89,
                    upNeighborID = 90,
                    rightNeighborID = 106,
                    leftNeighborID = -99998
                };
                expandButton = new ClickableTextureComponent(new Rectangle(__instance.xPositionOnScreen + 768 + 32 - 50, __instance.yPositionOnScreen + 192 + 32 - 53, 32, 32), Game1.mouseCursors, OptionsPlusMinus.plusButtonSource, 4f, false)
                {
                    myID = 90,
                    upNeighborID = 88,
                    downNeighborID = 89,
                    rightNeighborID = 106,
                    leftNeighborID = -99998
                };
                __instance.inventory.Clear();
                int offset = __instance.GetOffset();
                for (int i = 0; i < __instance.actualInventory.Count; i++)
                {
                    ClickableComponent item = new ClickableComponent(GetBounds(__instance, i), i.ToString() ?? "")
                    {
                        myID = ((i >= offset && i < offset + __instance.capacity) ? (IDOffset + i - offset) : (-99999)),
                        leftNeighborID = GetLeftNeighbor(__instance, i),
                        rightNeighborID = GetRightNeighbor(__instance, i),
                        downNeighborID = GetDownNeighbor(__instance, i),
                        upNeighborID = GetUpNeighbor(__instance, i),
                        region = 9000,
                        upNeighborImmutable = true,
                        downNeighborImmutable = true,
                        leftNeighborImmutable = true,
                        rightNeighborImmutable = true
                    };
                    __instance.inventory.Add(item);
                }
            }

            public static void InventoryMenu_hover_Prefix(InventoryMenu __instance, int x, int y)
            {
                if (!Config.ModEnabled || (__instance.actualInventory != Game1.player.Items) || __instance.capacity >= Game1.player.MaxItems || !__instance.isWithinBounds(x, y))
                    return;
                OnHover(ref __instance, x, y);
            }

            public static void ShopMenu_performHoverAction_Prefix(ShopMenu __instance, int x, int y)
            {
                if (!Config.ModEnabled || (__instance.inventory.actualInventory != Game1.player.Items) || __instance.inventory.capacity >= Game1.player.MaxItems || !__instance.inventory.isWithinBounds(x, y))
                    return;
                OnHover(ref __instance.inventory, x, y);
            }

            public static bool ShopMenu_receiveScrollWheelAction_Prefix(ShopMenu __instance, int direction)
            {
                if (!Config.ModEnabled || (__instance.inventory.actualInventory != Game1.player.Items) || __instance.inventory.capacity >= Game1.player.MaxItems)
                    return true;
                return !__instance.inventory.isWithinBounds(Game1.getMouseX(), Game1.getMouseY());
            }

            public static bool InventoryMenu_getInventoryPositionOfClick_Prefix(InventoryMenu __instance, int x, int y, ref int __result)
            {
                if (!Config.ModEnabled || (__instance.actualInventory != Game1.player.Items) || __instance.capacity >= __instance.actualInventory.Count)
                    return true;
                if (!IsWithinBounds(__instance, x, y))
                {
                    __result = -1;
                    return false;
                }
                return true;
            }

            public static bool InventoryMenu_leftClick_Prefix(InventoryMenu __instance, int x, int y, Item toPlace, ref Item __result)
            {
                if (!Config.ModEnabled || (__instance.actualInventory != Game1.player.Items) || __instance.capacity >= __instance.actualInventory.Count)
                    return true;
                if (!IsWithinBounds(__instance, x, y))
                {
                    __result = toPlace;
                    return false;
                }
                return true;
            }

            public static bool InventoryMenu_rightClick_Prefix(InventoryMenu __instance, int x, int y, Item toAddTo, ref Item __result)
            {
                if (!Config.ModEnabled || !__instance.playerInventory || __instance.capacity >= __instance.actualInventory.Count)
                    return true;
                if (!__instance.isWithinBounds(x, y))
                {
                    __result = toAddTo;
                    return false;
                }
                return true;
            }

            public static void InventoryMenu_setUpForGamePadMode_Postfix(InventoryMenu __instance)
            {
                if (!Config.ModEnabled || __instance.inventory == null || __instance.capacity >= __instance.actualInventory.Count || __instance.inventory.Count <= 0)
                    return;
                Rectangle bounds = __instance.inventory[scrolled.Value * __instance.capacity / __instance.rows].bounds;
                Game1.setMousePosition(bounds.Right - bounds.Width / 8, bounds.Bottom - bounds.Height / 8);
            }

            public static bool IClickableMenu_applyMovementKey_Prefix(IClickableMenu __instance, int direction)
            {
                if (!Config.ModEnabled || __instance.currentlySnappedComponent == null)
                    return true;
                InventoryMenu val = null;
                if (__instance is InventoryPage) val = ((InventoryPage)__instance).inventory;
                else if (__instance is ItemGrabMenu) val = ((ItemGrabMenu)__instance).inventory;
                else if (__instance is GeodeMenu) val = ((GeodeMenu)__instance).inventory;
                else if (__instance is JunimoNoteMenu) val = ((JunimoNoteMenu)__instance).inventory;
                else
                {
                    foreach (FieldInfo declaredField in AccessTools.GetDeclaredFields(__instance.GetType()))
                    {
                        if (declaredField.FieldType == typeof(InventoryMenu))
                        {
                            InventoryMenu m = (InventoryMenu)declaredField.GetValue(__instance);
                            if (m != null && m.actualInventory == Game1.player.Items) val = m;
                        }
                    }
                }
                if (val == null || !val.Scrolling()) return true;
                int num = val.Columns();
                if (__instance is ItemGrabMenu && val.inventory != null && val.inventory.Count >= ((ItemGrabMenu)__instance).GetColumnCount())
                {
                    for (int i = 0; i < num; i++)
                        val.inventory[i + val.GetOffset()].upNeighborID = (((ItemGrabMenu)__instance).shippingBin ? 12598 : (Math.Min(i, ((ItemGrabMenu)__instance).ItemsToGrabMenu.inventory.Count - 1) + 53910));
                }
                else if (__instance is GeodeMenu && val.inventory != null)
                {
                    for (int j = 0; j < num; j++) val.inventory[j + val.GetOffset()].upNeighborID = 998;
                }
                else if (__instance is JunimoNoteMenu && val.inventory != null)
                {
                    for (int k = 0; k < num; k++) val.inventory[k + val.GetOffset() + (val.rows - 1) * num].downNeighborID = -99998;
                    for (int l = 0; l < val.rows; l++) val.inventory[val.GetOffset() + l * num + num - 1].rightNeighborID = -99998;
                }
                ClickableComponent currentlySnappedComponent = __instance.currentlySnappedComponent;
                if (direction == 0 && (currentlySnappedComponent.myID == 102 || currentlySnappedComponent.myID == 101))
                    __instance.currentlySnappedComponent = __instance.getComponentWithID(IDOffset + num * (val.rows - 1));
                else if (direction == 2 && currentlySnappedComponent.myID >= 12340 && currentlySnappedComponent.myID < 12340 + num)
                    __instance.currentlySnappedComponent = __instance.getComponentWithID(IDOffset + currentlySnappedComponent.myID % 12340);
                else if (direction == 2 && __instance is GeodeMenu && currentlySnappedComponent.myID == 998)
                    __instance.currentlySnappedComponent = __instance.getComponentWithID(IDOffset);
                else if (direction == 2 && __instance is ItemGrabMenu && currentlySnappedComponent.myID >= 53910 + ((ItemGrabMenu)__instance).ItemsToGrabMenu.capacity - ((ItemGrabMenu)__instance).ItemsToGrabMenu.Columns() && currentlySnappedComponent.myID < 53910 + ((ItemGrabMenu)__instance).ItemsToGrabMenu.capacity)
                    __instance.currentlySnappedComponent = __instance.getComponentWithID(IDOffset + (currentlySnappedComponent.myID - 53910) % ((ItemGrabMenu)__instance).ItemsToGrabMenu.Columns());
                else if (direction == 0 && currentlySnappedComponent.myID >= IDOffset && currentlySnappedComponent.myID < IDOffset + num && scrolled.Value > 0)
                {
                    ChangeScroll(val, -1);
                    __instance.currentlySnappedComponent = __instance.getComponentWithID(currentlySnappedComponent.myID);
                }
                else if (direction == 2 && currentlySnappedComponent.myID >= IDOffset + val.capacity - num && currentlySnappedComponent.myID < IDOffset + val.capacity && scrolled.Value < (val.actualInventory.Count / num - val.rows))
                {
                    ChangeScroll(val, 1);
                    __instance.currentlySnappedComponent = __instance.getComponentWithID(currentlySnappedComponent.myID);
                }
                else return true;
                __instance.snapCursorToCurrentSnappedComponent();
                if (__instance.currentlySnappedComponent != currentlySnappedComponent) Game1.playSound("shiny4");
                return false;
            }

            public static bool ItemGrabMenu_customSnapBehavior_Prefix(ItemGrabMenu __instance, int direction, int oldRegion, int oldID)
            {
                if (!Config.ModEnabled || __instance.inventory == null || !__instance.inventory.Scrolling()) return true;
                InventoryMenu inventory = __instance.inventory;
                int num = inventory.Columns();
                if (direction == 2)
                {
                    if (inventory.inventory != null && inventory.inventory.Count >= __instance.GetColumnCount() && __instance.shippingBin)
                    {
                        for (int i = 0; i < num; i++)
                            inventory.inventory[i + inventory.GetOffset()].upNeighborID = (__instance.shippingBin ? 12598 : (Math.Min(i, __instance.ItemsToGrabMenu.inventory.Count - 1) + 53910));
                    }
                    if (oldID >= IDOffset && oldID < IDOffset + inventory.capacity && oldID >= IDOffset + inventory.capacity - num && scrolled.Value < inventory.actualInventory.Count / num - inventory.rows)
                    {
                        ChangeScroll(inventory, 1);
                        __instance.currentlySnappedComponent = __instance.getComponentWithID(oldID);
                    }
                    else if (!__instance.shippingBin && oldID >= 53910)
                    {
                        int num2 = oldID - 53910;
                        if (num2 + __instance.GetColumnCount() <= __instance.ItemsToGrabMenu.inventory.Count - 1)
                        {
                            __instance.currentlySnappedComponent = __instance.getComponentWithID(num2 + __instance.GetColumnCount() + 53910);
                            __instance.snapCursorToCurrentSnappedComponent();
                            return false;
                        }
                    }
                    else __instance.currentlySnappedComponent = __instance.getComponentWithID((oldRegion == 12598) ? IDOffset : (IDOffset + (oldID - 53910) % __instance.GetColumnCount()));
                }
                else if (direction == 0)
                {
                    if (oldID >= IDOffset && oldID < IDOffset + num)
                    {
                        if (scrolled.Value > 0)
                        {
                            ChangeScroll(inventory, -1);
                            __instance.currentlySnappedComponent = __instance.getComponentWithID(oldID);
                        }
                        else if (__instance.shippingBin && Game1.getFarm().lastItemShipped != null)
                        {
                            __instance.currentlySnappedComponent = __instance.getComponentWithID(12598);
                            __instance.currentlySnappedComponent.downNeighborID = oldID;
                        }
                    }
                    else if (oldID >= IDOffset + inventory.Columns()) __instance.currentlySnappedComponent = __instance.getComponentWithID(oldID - inventory.Columns());
                    else return true;
                }
                __instance.snapCursorToCurrentSnappedComponent();
                return false;
            }

            public static void InventoryMenu_draw_Prefix(InventoryMenu __instance, ref object[] __state)
            {
                try
                {
                    if (Config.ModEnabled && __instance.actualInventory == Game1.player.Items && __instance.capacity < __instance.actualInventory.Count)
                    {
                        __state = new object[2] { Game1.player.Items, __instance.inventory };
                        __instance.actualInventory = new List<Item>(__instance.actualInventory.Skip(__instance.capacity / __instance.rows * scrolled.Value).Take(__instance.capacity));
                        __instance.inventory = new List<ClickableComponent>(__instance.inventory.Skip(__instance.capacity / __instance.rows * scrolled.Value).Take(__instance.capacity));
                    }
                }
                catch (Exception value) { SMonitor.Log($"Failed in InventoryMenu_draw_Prefix:\n{value}", LogLevel.Error); }
            }

            public static void InventoryMenu_draw_Postfix(SpriteBatch b, InventoryMenu __instance, ref object[] __state)
            {
                try
                {
                    if (__state != null)
                    {
                        __instance.actualInventory = (IList<Item>)__state[0];
                        __instance.inventory = (List<ClickableComponent>)__state[1];
                        DrawUIElements(b, __instance);
                    }
                }
                catch (Exception value) { SMonitor.Log($"Failed in InventoryMenu_draw_Postfix:\n{value}", LogLevel.Error); }
            }

            public static bool SeedShop_draw_Prefix(SeedShop __instance, SpriteBatch b)
            {
                if (!Config.ModEnabled || !dataDict.Any()) return true;
                IntPtr functionPointer = AccessTools.Method(typeof(GameLocation), "draw", new Type[1] { typeof(SpriteBatch) }).MethodHandle.GetFunctionPointer();
                Action<SpriteBatch> func = (Action<SpriteBatch>)Activator.CreateInstance(typeof(Action<SpriteBatch>), __instance, functionPointer);
                func(b);
                List<int> list = dataDict.Keys.ToList();
                list.Sort();
                foreach (int item in list)
                {
                    if (Game1.player.MaxItems < item)
                    {
                        b.Draw(dataDict[item].texture, Game1.GlobalToLocal(Config.BackpackPosition), dataDict[item].textureRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.1232f);
                        return false;
                    }
                }
                return false;
            }

            public static bool GameLocation_performAction_Prefix(GameLocation __instance, string[] action, Farmer who, Location tileLocation, ref bool __result)
            {
                if (!Config.ModEnabled || !dataDict.Any() || action[0] != "BuyBackpack") return true;
                List<int> list = dataDict.Keys.ToList();
                list.Sort();
                foreach (int item in list)
                {
                    if (Game1.player.MaxItems < item)
                    {
                        __instance.createQuestionDialogue(string.Format(SHelper.Translation.Get("backpack-upgrade-x"), item), new Response[2]
                        {
                            new Response("Purchase", string.Format(SHelper.Translation.Get("buy-backpack-for-x"), dataDict[item].cost)),
                            new Response("Not", Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"))
                        }, "Backpack");
                        __result = true;
                        return false;
                    }
                }
                return false;
            }

            public static bool GameLocation_answerDialogueAction_Prefix(GameLocation __instance, string questionAndAnswer, string[] questionParams, ref bool __result)
            {
                if (!Config.ModEnabled || questionAndAnswer != "Backpack_Purchase" || !dataDict.Any()) return true;
                List<int> list = dataDict.Keys.ToList();
                list.Sort();
                foreach (int item in list)
                {
                    if (Game1.player.MaxItems < item)
                    {
                        if (Game1.player.Money >= dataDict[item].cost)
                        {
                            Game1.player.Money -= dataDict[item].cost;
                            SetPlayerSlots(item);
                            Game1.player.holdUpItemThenMessage(new SpecialItem(99, dataDict[item].name), true);
                            ((Multiplayer)typeof(Game1).GetField("multiplayer", BindingFlags.Static | BindingFlags.NonPublic).GetValue(Game1.game1)).globalChatInfoMessage($"CustomBackpack_{item}", new string[1] { Game1.player.Name });
                        }
                        else Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney2"));
                        __result = true;
                        return false;
                    }
                }
                return false;
            }

            public static bool SpecialItem_displayName_Prefix(SpecialItem __instance, ref string __result)
            {
                if (!Config.ModEnabled || __instance.which.Value != 99 || !dataDict.Any()) return true;
                __result = __instance.Name;
                return false;
            }

            public static bool SpecialItem_getTemporarySpriteForHoldingUp_Prefix(SpecialItem __instance, Vector2 position, ref TemporaryAnimatedSprite __result)
            {
                if (!Config.ModEnabled || __instance.which.Value != 99 || !dataDict.Any()) return true;
                var value = dataDict.FirstOrDefault(p => p.Value.name == __instance.Name).Value;
                if (value == null) return true;
                __result = new TemporaryAnimatedSprite(value.texturePath, value.textureRect, position + new Vector2(16f, 0f), false, 0f, Color.White) { scale = 4f, layerDepth = 1f };
                return false;
            }

            public static bool Farmer_shiftToolbar_Prefix(Farmer __instance, bool right)
            {
                if (!Config.ModEnabled || Config.ShiftRows < 1 || Config.ShiftRows >= __instance.Items.Count / 12 || __instance.Items == null || __instance.Items.Count < 37 || __instance.UsingTool || Game1.dialogueUp || !__instance.CanMove || __instance.Items.HasAny() || Game1.eventUp || Game1.farmEvent != null) return true;
                if (Config.ShiftRows == 1) return false;
                Game1.playSound("shwip");
                if (__instance.CurrentItem != null) __instance.CurrentItem.actionWhenStopBeingHeld(__instance);
                List<Item> list = __instance.Items.Take(Config.ShiftRows * 12).ToList();
                if (right) { List<Item> l2 = list.Take(12).ToList(); list.RemoveRange(0, 12); list.AddRange(l2); }
                else { List<Item> l3 = list.Skip(list.Count - 12).Take(12).ToList(); list.RemoveRange(list.Count - 12, 12); list.InsertRange(0, l3); }
                for (int l = 0; l < list.Count; l++) __instance.Items[l] = list[l];
                __instance.netItemStowed.Set(false);
                if (__instance.CurrentItem != null) __instance.CurrentItem.actionWhenBeingHeld(__instance);
                foreach (var menu in Game1.onScreenMenus) if (menu is Toolbar t) { t.shifted(right); break; }
                return false;
            }
        }
    }
}
