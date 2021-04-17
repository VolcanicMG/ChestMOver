using ChestMover.Buffs;
using ChestMover.Common.Packets;
using ChestMover.Core.Networking;
using ChestMover.Items;
using ChestMover.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public Item[] chestItems;
        public short ChestType = 0;
        public string ChestName;

        private int inventorySpot;
        private bool firstTick;

        public bool GotChest;

        public Vector2 mousePos;
        public float dist;

        public override void PreUpdate()
        {
			if (Main.netMode == NetmodeID.Server)
			{
				return;
			}

			if (player.whoAmI == Main.myPlayer && (player.inventory[player.selectedItem].type == 0 || player.inventory[player.selectedItem].type == ModContent.ItemType<Box>()))
            {
                mousePos = Main.MouseWorld;
                dist = Vector2.Distance(player.Center, mousePos);
            }

			var mouseTilePos = mousePos.ToTileCoordinates16();

            //Picking up the chest
            if (!GotChest && Main.mouseLeft && Main.tile[mouseTilePos.X, mouseTilePos.Y]?.type == TileID.Containers && dist / 16f <= 4 && player.inventory[player.selectedItem].type == ItemID.None && player.whoAmI == Main.myPlayer)
            {
                foreach (Chest chest in Main.chest)
                {
					if (chest == null || (chest.x == 0 && chest.y == 0))
					{
						continue;
					}

                    Point16 chestPos = new Point16(chest.x, chest.y);
					var tile = Main.tile[chestPos.X, chestPos.Y];

					if (tile.type == TileID.Containers && ChestUtils.CheckForChest2x2(chestPos, mousePos.ToTileCoordinates16()) && !Chest.isLocked(chest.x, chest.y))
                    {
						if (Main.netMode == NetmodeID.MultiplayerClient)
						{
							MultiplayerSystem.SendPacket(new AttemptChestPickupPacket(chestPos));
							break;
						}

						PickupChest(chest);

                        break;
                    }
                }
            }
        }

        public override void PostUpdate()
		{
			if (Main.netMode == NetmodeID.Server || player.whoAmI != Main.myPlayer)
			{
				return;
			}

			if (GotChest && Main.mouseRight && Main.mouseRightRelease && dist / 16f <= 4) //Issues with mp
			{
				Point16 newPos = mousePos.ToTileCoordinates16();

				if (ChestUtils.CheckChestPlacing(newPos.X, newPos.Y))
				{
					PlaceChest(newPos);
				}
				else
				{
					Main.PlaySound(SoundID.Duck, (int)player.Center.X, (int)player.Center.Y);
				}
			}

			if (GotChest && dist / 16f <= 4) {

				TileObject tileObject = default(TileObject);
				TileObject.CanPlace(Player.tileTargetX, Player.tileTargetY, 21, (ChestType / 36), 1, out tileObject, true);

				if (!firstTick) {
					player.inventory[inventorySpot].SetDefaults(ModContent.ItemType<Box>());
					firstTick = true;
				}
			}

			if (GotChest) player.AddBuff(ModContent.BuffType<MovingDebuff>(), 10);


			//Remove the item once the player gets done
			if (!GotChest) {
				foreach (Item slot in player.inventory) {
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
			if (tag.ContainsKey(nameof(chestItems))) {
				chestItems = tag.GetList<TagCompound>(nameof(chestItems)).Select(t => ItemIO.Load(t)).ToArray();
			}

            ChestType = tag.GetAsShort(nameof(ChestType));
            ChestName = tag.GetString(nameof(ChestName));
            inventorySpot = tag.GetAsInt(nameof(inventorySpot));
            firstTick = tag.GetBool(nameof(firstTick));
            GotChest = tag.GetBool(nameof(GotChest));
        }

        public override TagCompound Save()
        {
            var tag = new TagCompound
            {
                [nameof(ChestType)] = ChestType,
                [nameof(ChestName)] = ChestName,
                [nameof(inventorySpot)] = inventorySpot,
                [nameof(firstTick)] = firstTick,
                [nameof(GotChest)] = GotChest
            };

			if (chestItems != null) {
				tag[nameof(chestItems)] = chestItems.Select(i => ItemIO.Save(i)).ToList();
			}

			return tag;
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

                if (drawPlayer.CM().dist / 16f <= 4 && !Main.dedServ && !Main.gameMenu)
                {
                    int drawX = (int)(mousePos.X - Main.screenPosition.X + 42);
                    int drawY = (int)(mousePos.Y - Main.screenPosition.Y + 8);

                    DrawData dataTopInvis = new DrawData(Chest, new Vector2(drawX, drawY), new Rectangle(drawPlayer.CM().ChestType, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                    DrawData dataTopRightInvis = new DrawData(Chest, new Vector2(drawX + 16, drawY), new Rectangle(drawPlayer.CM().ChestType + 18, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                    DrawData dataBottomInvis = new DrawData(Chest, new Vector2(drawX, drawY + 16), new Rectangle(drawPlayer.CM().ChestType, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                    DrawData dataBottomRightInvis = new DrawData(Chest, new Vector2(drawX + 16, drawY + 16), new Rectangle(drawPlayer.CM().ChestType + 18, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);

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

                DrawData dataTop = new DrawData(Chest, new Vector2(drawX + mp.offset, drawY), new Rectangle(drawPlayer.CM().ChestType, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                DrawData dataTopRight = new DrawData(Chest, new Vector2(drawX + mp.offset + 16, drawY), new Rectangle(drawPlayer.CM().ChestType + 18, 0, 16, 16), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                DrawData dataBottom = new DrawData(Chest, new Vector2(drawX + mp.offset, drawY + 16), new Rectangle(drawPlayer.CM().ChestType, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);
                DrawData dataBottomRight = new DrawData(Chest, new Vector2(drawX + mp.offset + 16, drawY + 16), new Rectangle(drawPlayer.CM().ChestType + 18, 18, 16, 18), new Microsoft.Xna.Framework.Color(255, 255, 255), 0f, new Vector2(0, 0), 1f, mp.effect, 0);

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

		public void SetHeldChest(short? chestType)
		{
			GotChest = chestType.HasValue;
			ChestType = chestType ?? -1;
		}

		//For local/server use
		public void PickupChest(Chest chest) => PickupChestInternal(Main.tile[chest.x, chest.y].frameX, chest.name, chest.item, chest);

		//For multiplayer clients that do not own this player
		public void PickupChest(short chestType) => PickupChestInternal(chestType);

		//For multiplayer clients that own this player
		public void PickupChest(short chestType, string chestName, Item[] items) => PickupChestInternal(chestType, chestName, items);

		public void PlaceChest(Point16 chestPos)
		{
			GotChest = false;

			if (Main.myPlayer == player.whoAmI)
			{
				int chestId = ChestUtils.PlaceChest(chestPos.X, chestPos.Y);

				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					MultiplayerSystem.SendPacket(new SetChestContentsPacket(chestId, chestItems));
					MultiplayerSystem.SendPacket(new VisualChestPlacePacket(chestPos));
				}

				chestItems = null;
			}

			//Create dropoff sound
			if (!Main.dedServ)
			{
				Main.PlaySound(SoundID.DoorClosed, player.Center);
			}
		}

		private void PickupChestInternal(short chestType, string chestName = null, Item[] items = null, Chest chest = null)
		{
			if (items != null && player.whoAmI == Main.myPlayer)
			{
				Array.Resize(ref chestItems, items.Length);
				Array.Copy(items, chestItems, items.Length);
			}

			if (chest != null)
			{
				//Delete everything in the chest
				for (int i = 0; i < chest.item.Length; i++) {
					chest.item[i] = new Item();
				}

				//Destroy the chest
				ChestUtils.RemoveChest(chest);
			}

			//Saving chest details

			SetHeldChest(chestType);

			if (player.whoAmI == Main.myPlayer)
			{
				ChestName = chestName;
				inventorySpot = player.selectedItem;
				firstTick = false;
			}

			//Create pickup sound
			if (!Main.dedServ)
			{
				Main.PlaySound(SoundID.DoorOpen, (int)player.Center.X, (int)player.Center.Y);
			}
		}
    }

    public static class ClassExtensions
    {
        public static ChestMoverPlayer CM(this Player player) => (ChestMoverPlayer)player.GetModPlayer<ChestMoverPlayer>();
    }
}