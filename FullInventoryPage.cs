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
    internal class FullInventoryPage : InventoryPage
    {
        private FieldInfo trashRotField = AccessTools.Field(typeof(InventoryPage), "trashCanLidRotation");
        private FieldInfo hoverTextField = AccessTools.Field(typeof(InventoryPage), "hoverText");
        private FieldInfo hoverAmountField = AccessTools.Field(typeof(InventoryPage), "hoverAmount");
        private FieldInfo hoverTitleField = AccessTools.Field(typeof(InventoryPage), "hoverTitle");
        private FieldInfo hoveredItemField = AccessTools.Field(typeof(InventoryPage), "hoveredItem");

        public FullInventoryPage(InventoryMenu instance, int x, int y, int width, int height) : base(x, y, width, height)
        {
            this.inventory = new FullInventoryMenu(instance);
            this.equipmentIcons?.Clear();
            
            if (this.junimoNoteIcon != null)
                this.junimoNoteIcon.bounds = Rectangle.Empty;
                
            if (this.portrait != null)
                this.portrait.bounds = Rectangle.Empty;

            this.exitFunction = delegate ()
            {
                ModEntry.scrolled = ModEntry.oldScrolled;
                Game1.activeClickableMenu = ModEntry.lastMenu.Value;
            };
        }

        public override void draw(SpriteBatch b)
        {
            
            this.xPositionOnScreen = this.inventory.xPositionOnScreen;
            this.yPositionOnScreen = this.inventory.yPositionOnScreen - 36;
            this.width = this.inventory.width;
            this.height = this.inventory.height - 136;
            
            this.inventory.draw(b);

            float trashCanLidRotation = (float)trashRotField.GetValue(this);
            string hoverText = (string)hoverTextField.GetValue(this);
            int hoverAmount = (int)hoverAmountField.GetValue(this);
            string hoverTitle = (string)hoverTitleField.GetValue(this);
            Item hoveredItem = (Item)hoveredItemField.GetValue(this);

            if (this.organizeButton != null)
            {
                this.organizeButton.bounds.X = this.xPositionOnScreen + this.width + 64;
                this.organizeButton.draw(b);
            }

            this.trashCan.bounds.X = this.xPositionOnScreen + this.width + 64;
            this.trashCan.bounds.Y = this.organizeButton.bounds.Y + 256;
            this.trashCan.draw(b);

            b.Draw(Game1.mouseCursors, new Vector2(this.trashCan.bounds.X + 60, this.trashCan.bounds.Y + 40), new Rectangle(564 + Game1.player.trashCanLevel * 18, 129, 18, 10), Color.White, trashCanLidRotation, new Vector2(16f, 10f), 4f, SpriteEffects.None, 0.86f);

            if (Game1.player.CursorSlotItem != null)
            {
                Game1.player.CursorSlotItem.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
            }

            if (!string.IsNullOrEmpty(hoverText))
            {
                if (hoverAmount > 0)
                {
                    IClickableMenu.drawToolTip(b, hoverText, hoverTitle, null, true, -1, 0, null, -1, null, hoverAmount);
                }
                else
                {
                    
                    IClickableMenu.drawToolTip(b, hoverText, hoverTitle, hoveredItem, Game1.player.CursorSlotItem != null, -1, 0, null, -1, null, -1);
                }
            }

            
            this.drawMouse(b);
        }
    }
}
