using Terraria;
using Terraria.ModLoader;

namespace ChestMover.Items
{
    public class Box : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Chest");
            Tooltip.SetDefault("");
        }

        public override void SetDefaults()
        {
            item.width = 25;    //sprite width
            item.height = 26;   //sprite height
            item.maxStack = 1;   //This defines the items max stack
            item.consumable = false;  //Tells the game that this should be used up once fired
            item.rare = 1;   //The color the title of your item when hovering over it ingame
                             //item.useTime = 20;	 //How fast the item is used.
            item.value = Item.buyPrice(0, 0, 0, 0);   //How much the item is worth, in copper coins, when you sell it to a merchant. It costs 1/5th of this to buy it back from them. An easy way to remember the value is platinum, gold, silver, copper or PPGGSSCC (so this item price is 3 silver)\
            //item.axe = 1;
            item.createTile = 0;
        }

    }
}