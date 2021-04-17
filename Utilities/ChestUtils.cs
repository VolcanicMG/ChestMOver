using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace ChestMover.Utilities
{
	public static class ChestUtils
	{
		public static bool CheckForChest2x2(Point16 chestPos, Point16 mousePos)
		{
			return chestPos == mousePos
				|| chestPos == new Point16(mousePos.X - 1, mousePos.Y)
				|| chestPos == new Point16(mousePos.X, mousePos.Y - 1)
				|| chestPos == new Point16(mousePos.X - 1, mousePos.Y - 1);
		}

		public static void RemoveChest(Chest chest)
		{
			for (int i = 0; i < 2; i++)
			{
				for (int y = 0; y < 2; y++)
				{
					for (int x = 0; x < 2; x++)
					{
						if (i == 0)
						{
							Main.tile[chest.x + x, chest.y + y].ClearTile();
						}
						else
						{
							WorldGen.SquareTileFrame(chest.x + x, chest.y + y);
						}
					}
				}
			}

			for (int i = 0; i < Main.maxChests; i++)
			{
				if (Main.chest[i] == chest) {
					Main.chest[i] = null;

					if (!Main.dedServ)
					{
						if (Main.player[Main.myPlayer].chest == i)
							Main.player[Main.myPlayer].chest = -1;

						Recipe.FindRecipes();
					}
				}
			}

			if (Main.netMode == NetmodeID.Server)
			{
				NetMessage.SendTileSquare(-1, chest.x, chest.y, 2);
			}
		}

		public static bool CheckChestPlacing(int i, int j)
        {
            if (Main.tile[i, j].type == 3 || Main.tile[i, j].type == 73) //check for grass
            {
                WorldGen.KillTile(i, j);
            }

            if (Main.tile[i + 1, j].type == 3 || Main.tile[i + 1, j].type == 73)
            {
                WorldGen.KillTile(i + 1, j);
            }

            if (!WorldGen.TileEmpty(i, j + 1) && //Check the bottom one from the pos
                !WorldGen.TileEmpty(i + 1, j + 1) && //Check the bottom one from the pos but right one
                Main.tile[i, j].active() == false &&
                Main.tile[i + 1, j].active() == false &&
                Main.tile[i, j + 1].halfBrick() == false && //Make sure the player doesn't place on slabs
                Main.tile[i + 1, j + 1].halfBrick() == false &&
                WorldGen.TileEmpty(i, j - 1) && //Check the top one from the pos
                WorldGen.TileEmpty(i + 1, j - 1) &&
                Main.tileSolid[Main.tile[i, j + 1].type] &&
                Main.tileSolid[Main.tile[i + 1, j + 1].type])
            {
                return true;
            }

            return false;
        }

        public static int PlaceChest(int x, int y, ushort type = 21, bool notNearOtherChests = false, int style = 0)
        {
            int num = -1;

			if (TileObject.CanPlace(x, y, type, style, 1, out TileObject tileObject, false, false))
			{
				bool flag = true;

				if (notNearOtherChests && Chest.NearOtherChests(x - 1, y - 1))
				{
					flag = false;
				}

				if (flag)
				{
					TileObject.Place(tileObject);
					num = CreateChest(tileObject.xCoord, tileObject.yCoord, -1);
				}
			} else {
				num = -1;
			}

			if (num != -1 && Main.netMode == 1 && type == 21)
            {
                NetMessage.SendData(MessageID.ChestUpdates, -1, -1, null, 0, x, y, style, 0, 0, 0);
            }

            if (num != -1 && Main.netMode == 1 && type == 467)
            {
                NetMessage.SendData(MessageID.ChestUpdates, -1, -1, null, 4, x, y, style, 0, 0, 0);
            }

            if (num != 1 && Main.netMode == 1 && type >= 470 && TileID.Sets.BasicChest[type])
            {
                NetMessage.SendData(MessageID.ChestUpdates, -1, -1, null, 100, x, y, style, 0, type, 0);
            }

            return num;
        }

        public static int CreateChest(int X, int Y, int id = -1)
        {
            Player player = Main.player[Main.myPlayer];

            int num = id;

            if (num == -1)
            {
                num = Chest.FindEmptyChest(X, Y, 21, 0, 1);

                if (num == -1)
                {
                    return -1;
                }

                if (Main.netMode == 1)
                {
                    return num;
                }
            }

			var chest = Main.chest[num];

            chest = new Chest(false);
            chest.x = X;
            chest.y = Y;

            for (int i = 0; i < 40; i++) //Initiate the new slots
            {
                chest.item[i] = new Item();
            }

			var chestMoverPlayer = player.GetModPlayer<ChestMoverPlayer>();

			//Add the items to the chest from the old
			Array.Resize(ref chest.item, chestMoverPlayer.chestItems.Length);
			Array.Copy(chestMoverPlayer.chestItems, chest.item, chestMoverPlayer.chestItems.Length);

			//Set the chest to the chest type that was broken
			Main.tile[X, Y].frameX = chestMoverPlayer.ChestType;
            Main.tile[X + 1, Y + 1].frameX = (short)(chestMoverPlayer.ChestType + 18);
            Main.tile[X + 1, Y].frameX = (short)(chestMoverPlayer.ChestType + 18);
            Main.tile[X, Y + 1].frameX = chestMoverPlayer.ChestType;

            chest.name = chestMoverPlayer.ChestName;

            //netstuff
            NetMessage.SendData(MessageID.ChestUpdates, -1, -1, null, 0, X, Y, (float)chestMoverPlayer.ChestType / 16, 0, 0, 0);
            NetMessage.SendData(MessageID.ChestUpdates, -1, -1, null, 100, X, Y, (float)chestMoverPlayer.ChestType / 16, 0, 21, 0);

            Chest.UpdateChestFrames();

            return num;
        }
	}
}
