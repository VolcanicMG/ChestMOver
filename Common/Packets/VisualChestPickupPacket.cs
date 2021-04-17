using System.IO;
using ChestMover.Core.Networking;
using Terraria;

namespace ChestMover.Common.Packets
{
	public class VisualChestPickupPacket : NetPacket
	{
		public VisualChestPickupPacket(byte playerId, Chest chest)
		{
			Writer.Write(playerId);
			Writer.Write(Main.tile[chest.x, chest.y].frameX);
		}

		public override void Read(BinaryReader reader, int sender)
		{
			byte playerId = reader.ReadByte();
			short chestType = reader.ReadInt16();

			var player = Main.player[playerId];

			if (player?.active == true)
			{
				player.GetModPlayer<ChestMoverPlayer>().PickupChest(chestType);
			}
		}
	}
}
