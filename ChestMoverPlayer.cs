using ChestMover.Buffs;
using ChestMover.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.ModContent;

namespace ChestMover
{
    public class ChestMoverPlayer : ModPlayer
    {
        public static int[] itemList = new int[40];
        public static int[] itemStackList = new int[40];
        public static short ChestType = 0;
        public static string ChestName;

        private int inventorySpot;
        private bool firstTick;

        public bool GotChest;

        public override void PreUpdate()
        {
            Vector2 mousePos = Main.MouseWorld;
            float dist = Vector2.Distance(player.Center, mousePos);

            //Picking up the chest
            if (!GotChest && Main.mouseLeft && Main.tile[(int)mousePos.X / 16, (int)mousePos.Y / 16].type == TileID.Containers && dist / 16f <= 4 && player.inventory[player.selectedItem].type == 0 && Main.netMode != NetmodeID.Server)
            {
                foreach (Chest chest in Main.chest)
                {
                    Vector2 ChestPos = Vector2.Zero;

                    if (chest != null)
                    {
                        ChestPos = new Vector2(chest.x, chest.y);
                    }

                    if (ChestPos != Vector2.Zero && Main.tile[chest.x, chest.y].type == TileID.Containers && checkForChest2x2(ChestPos, new Vector2((int)mousePos.X / 16, (int)mousePos.Y / 16)) && !Chest.isLocked(chest.x, chest.y))
                    {
                        itemList = new int[40];
                        itemStackList = new int[40]; //Reset the lists so things can save properly (For some reason the itemStackList needs to do this when saving is involved)

                        //Load the list of items (Issues with MP)
                        for (int i = 0; i < 40; i++)
                        {
                            itemList[i] = chest.item[i].type;
                            itemStackList[i] = chest.item[i].stack;
                        }

                        //Saving chest details
                        GotChest = true;
                        ChestType = Main.tile[chest.x, chest.y].frameX;
                        ChestName = chest.name;
                        inventorySpot = player.selectedItem;
                        firstTick = false;

                        //Delete everything in the chest
                        foreach (Item item in chest.item)
                        {
                            item.TurnToAir();
                        }

                        //Destroy the chest without it dropping the item - Move to a separate method to clean up the code a bit TODO
                        Chest.DestroyChest((int)ChestPos.X, (int)ChestPos.Y);

                        Main.tile[(int)ChestPos.X, (int)ChestPos.Y].ClearTile();
                        Main.tile[(int)ChestPos.X, (int)ChestPos.Y].active(false);
                        NetMessage.SendData(MessageID.TileChange, -1, -1, null, 2, (float)ChestPos.X, (float)ChestPos.Y, 0f, 0, 0, 0);

                        Main.tile[(int)ChestPos.X + 1, (int)ChestPos.Y + 1].ClearTile();
                        Main.tile[(int)ChestPos.X + 1, (int)ChestPos.Y + 1].active(false);
                        NetMessage.SendData(MessageID.TileChange, -1, -1, null, 2, (float)ChestPos.X + 1, (float)ChestPos.Y + 1, 0f, 0, 0, 0);

                        Main.tile[(int)ChestPos.X + 1, (int)ChestPos.Y].ClearTile();
                        Main.tile[(int)ChestPos.X + 1, (int)ChestPos.Y].active(false);
                        NetMessage.SendData(MessageID.TileChange, -1, -1, null, 2, (float)ChestPos.X + 1, (float)ChestPos.Y, 0f, 0, 0, 0);

                        Main.tile[(int)ChestPos.X, (int)ChestPos.Y + 1].ClearTile();
                        Main.tile[(int)ChestPos.X, (int)ChestPos.Y + 1].active(false);
                        NetMessage.SendData(MessageID.TileChange, -1, -1, null, 2, (float)ChestPos.X, (float)ChestPos.Y + 1, 0f, 0, 0, 0);

                        WorldGen.SquareTileFrame((int)ChestPos.X, (int)ChestPos.Y, true);
                        WorldGen.SquareTileFrame((int)ChestPos.X + 1, (int)ChestPos.Y + 1, true);
                        WorldGen.SquareTileFrame((int)ChestPos.X + 1, (int)ChestPos.Y, true);
                        WorldGen.SquareTileFrame((int)ChestPos.X, (int)ChestPos.Y + 1, true);

                        ////net stuff
                        //int number = Chest.FindChest((int)ChestPos.X, (int)ChestPos.Y);
                        //NetMessage.SendData(34, -1, -1, null, 101, (float)ChestPos.X, (float)ChestPos.Y, 0f, number, Main.tile[(int)ChestPos.X, (int)ChestPos.Y].type, 0);
                        //NetMessage.SendTileSquare(-1, (int)ChestPos.X, (int)ChestPos.Y, 3, TileChangeType.None);

                        //Create pickup sound
                        Main.PlaySound(SoundID.DoorOpen, (int)player.Center.X, (int)player.Center.Y);

                        break;
                    }
                }
            }

        }

        public override void PostUpdate()
        {
            Vector2 mousePos = Main.MouseWorld;
            float dist = Vector2.Distance(player.Center, mousePos);

            if (GotChest && Main.mouseRight && Main.mouseRightRelease && dist / 16f <= 4 && Main.netMode != NetmodeID.Server) //Issues with mp
            {
                Vector2 NewChestPos = new Vector2((int)mousePos.X / 16, (int)mousePos.Y / 16);

                if (CheckChestPlacing((int)NewChestPos.X, (int)NewChestPos.Y))
                {
                    PlaceChest((int)NewChestPos.X, (int)NewChestPos.Y);
                    //WorldGen.SquareTileFrame((int)NewChestPos.X, (int)NewChestPos.Y, true);

                    GotChest = false;

                    //Create dropoff sound
                    Main.PlaySound(SoundID.DoorClosed, (int)player.Center.X, (int)player.Center.Y);
                }
                else Main.PlaySound(SoundID.Duck, (int)player.Center.X, (int)player.Center.Y);
            }

            if (GotChest && dist / 16f <= 4)
            {

                TileObject tileObject = default(TileObject);
                TileObject.CanPlace(Player.tileTargetX, Player.tileTargetY, 21, (ChestType / 36), 1, out tileObject, true);

                if (!firstTick)
                {
                    player.inventory[inventorySpot].SetDefaults(ModContent.ItemType<Box>());
                    firstTick = true;
                }
            }

            if (GotChest) player.AddBuff(ModContent.BuffType<MovingDebuff>(), 10);


            //Remove the item once the player gets done
            if (!GotChest)
            {
                foreach (Item slot in player.inventory)
                {
                    if (slot.type == ModContent.ItemType<Box>()) slot.SetDefaults(0);
                }

            }

        }

        public override void SetControls() //When the player is holding a chest make it so the player can only move and not use anything
        {
            if (GotChest)
            {
                player.controlUseItem = false;
                player.noBuilding = true;
                player.controlUseTile = false;

                player.controlMap = false;
                player.controlUseItem = false;
                player.controlThrow = false;
            }
        }

        #region Saving/Loading
        public override void Load(TagCompound tag)
        {
            itemList = tag.GetIntArray(nameof(itemList));
            itemStackList = tag.GetIntArray(nameof(itemStackList));
            ChestType = tag.GetAsShort(nameof(ChestType));
            ChestName = tag.GetString(nameof(ChestName));
            inventorySpot = tag.GetAsInt(nameof(inventorySpot));
            firstTick = tag.GetBool(nameof(firstTick));
            GotChest = tag.GetBool(nameof(GotChest));
        }

        public override TagCompound Save()
        {
            return new TagCompound
            {
                [nameof(itemList)] = itemList,
                [nameof(itemStackList)] = itemStackList,
                [nameof(ChestType)] = ChestType,
                [nameof(ChestName)] = ChestName,
                [nameof(inventorySpot)] = inventorySpot,
                [nameof(firstTick)] = firstTick,
                [nameof(GotChest)] = GotChest
            };
        }
        #endregion

        #region Drawing
        private SpriteEffects effect;
        private int offset;
        public static readonly PlayerLayer ChestInvis = new PlayerLayer("ExtraExplosives", "ChestInvis", PlayerLayer.MiscEffectsFront,
            delegate (PlayerDrawInfo info)
            {
                Player drawPlayer = info.drawPlayer;
                Vector2 mousePos = Main.MouseWorld;

                ChestMoverPlayer mp = drawPlayer.CM();
                Texture2D Chest = GetTexture("ChestMover/Images/ChestImagesInvis");
                float dist = Vector2.Distance(drawPlayer.Center, mousePos);

                if (dist / 16f <= 4)
                {
                    int drawX = (int)(mousePos.X - Main.screenPosition.X + 42);
                    int drawY = (int)(mousePos.Y - Main.screenPosition.Y + 8);

                    DrawData dataTopInvis = new DrawData(Chest, new Vector2(drawX, drawY), new Rectangle(ChestType, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                    DrawData dataTopRightInvis = new DrawData(Chest, new Vector2(drawX + 16, drawY), new Rectangle(ChestType + 18, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                    DrawData dataBottomInvis = new DrawData(Chest, new Vector2(drawX, drawY + 16), new Rectangle(ChestType, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                    DrawData dataBottomRightInvis = new DrawData(Chest, new Vector2(drawX + 16, drawY + 16), new Rectangle(ChestType + 18, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);

                    Main.playerDrawData.Add(dataTopInvis);
                    Main.playerDrawData.Add(dataTopRightInvis);
                    Main.playerDrawData.Add(dataBottomInvis);
                    Main.playerDrawData.Add(dataBottomRightInvis);
                }

            });
        public static readonly PlayerLayer ChestHold = new PlayerLayer("ExtraExplosives", "ChestHold", PlayerLayer.MiscEffectsFront,
            delegate (PlayerDrawInfo info)
            {
                Player drawPlayer = info.drawPlayer;

                Mod mod = ModLoader.GetMod("ChestMover");
                ChestMoverPlayer mp = drawPlayer.CM();
                Texture2D Chest = GetTexture("ChestMover/Images/ChestImages");


                if (drawPlayer.direction > 0 && mp.GotChest)
                {
                    mp.offset = 0;
                }
                else
                {
                    mp.offset = -44;
                }

                int drawX = (int)(info.position.X + drawPlayer.width / 2f - Main.screenPosition.X + 5);
                int drawY = (int)(info.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y - 20);

                DrawData dataTop = new DrawData(Chest, new Vector2(drawX + mp.offset, drawY), new Rectangle(ChestType, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                DrawData dataTopRight = new DrawData(Chest, new Vector2(drawX + mp.offset + 16, drawY), new Rectangle(ChestType + 18, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                DrawData dataBottom = new DrawData(Chest, new Vector2(drawX + mp.offset, drawY + 16), new Rectangle(ChestType, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                DrawData dataBottomRight = new DrawData(Chest, new Vector2(drawX + mp.offset + 16, drawY + 16), new Rectangle(ChestType + 18, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);

                Main.playerDrawData.Add(dataTop);
                Main.playerDrawData.Add(dataTopRight);
                Main.playerDrawData.Add(dataBottom);
                Main.playerDrawData.Add(dataBottomRight);

            });
        public override void ModifyDrawLayers(List<Terraria.ModLoader.PlayerLayer> layers)
        {
            if (GotChest && !player.dead)
            {
                ChestHold.visible = true;
                ChestInvis.visible = true;
                layers.Insert(0, ChestHold);
                layers.Add(ChestHold);
                layers.Add(ChestInvis);
            }
        }
        #endregion

        #region Methods
        public static bool checkForChest2x2(Vector2 Chestpos, Vector2 mousepos)
        {
            if (Chestpos == mousepos ||
               Chestpos == new Vector2(mousepos.X - 1, mousepos.Y) ||
               Chestpos == new Vector2(mousepos.X, mousepos.Y - 1) ||
               Chestpos == new Vector2(mousepos.X - 1, mousepos.Y - 1))
            {
                return true;
            }

            return false;
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
            TileObject tileObject = default(TileObject);
            if (TileObject.CanPlace(x, y, (int)type, style, 1, out tileObject, false, false))
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
            }
            else
            {
                num = -1;
            }
            if (num != -1 && Main.netMode == 1 && type == 21)
            {
                NetMessage.SendData(34, -1, -1, null, 0, (float)x, (float)y, (float)style, 0, 0, 0);
            }
            if (num != -1 && Main.netMode == 1 && type == 467)
            {
                NetMessage.SendData(34, -1, -1, null, 4, (float)x, (float)y, (float)style, 0, 0, 0);
            }
            if (num != 1 && Main.netMode == 1 && type >= 470 && TileID.Sets.BasicChest[type])
            {
                NetMessage.SendData(34, -1, -1, null, 100, (float)x, (float)y, (float)style, 0, type, 0);
            }
            return num;
        }

        public static int CreateChest(int X, int Y, int id = -1)
        {

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
            Main.chest[num] = new Chest(false);
            Main.chest[num].x = X;
            Main.chest[num].y = Y;
            for (int i = 0; i < 40; i++) //Initiate the new slots
            {
                Main.chest[num].item[i] = new Item();
            }

            //Add the items to the chest from the old
            for (int i = 0; i < 40; i++)
            {
                Main.chest[num].item[i].SetDefaults(itemList[i]);
                Main.chest[num].item[i].stack = itemStackList[i];

            }

            //Set the chest to the chest type that was broken
            Main.tile[X, Y].frameX = ChestType;
            Main.tile[X + 1, Y + 1].frameX = (short)(ChestType + 18);
            Main.tile[X + 1, Y].frameX = (short)(ChestType + 18);
            Main.tile[X, Y + 1].frameX = ChestType;

            Main.chest[num].name = ChestName;

            return num;
        }
        #endregion
    }

    public static class ClassExtensions
    {
        public static ChestMoverPlayer CM(this Player player) => (ChestMoverPlayer)player.GetModPlayer<ChestMoverPlayer>();
    }
}