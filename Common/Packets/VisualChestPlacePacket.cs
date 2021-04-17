using System;
using System.IO;
using ChestMover.Core.Networking;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace ChestMover.Common.Packets
{
	public class VisualChestPlacePacket : NetPacket
	{
		public VisualChestPlacePacket(Point16 point)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				throw new InvalidOperationException("This overload is not to be used by servers.");
			}

			Writer.Write(point.X);
			Writer.Write(point.Y);
		}

		public VisualChestPlacePacket(byte playerId, Point16 point)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				throw new InvalidOperationException("This overload is not to be used by clients.");
			}

			Writer.Write(playerId);
			Writer.Write(point.X);
			Writer.Write(point.Y);
		}

		public override void Read(BinaryReader reader, int sender)
		{
			byte playerId = Main.netMode == NetmodeID.MultiplayerClient ? reader.ReadByte() : (byte)sender;
			Point16 chestPos = new Point16(reader.ReadInt16(), reader.ReadInt16());

			var player = Main.player[playerId];

			if (player?.active == true)
			{
				player.GetModPlayer<ChestMoverPlayer>().PlaceChest(chestPos);

				if (Main.netMode == NetmodeID.Server)
				{
					MultiplayerSystem.SendPacket(new VisualChestPlacePacket(playerId, chestPos), ignoreClient: playerId);
				}
			}
		}
	}
}
