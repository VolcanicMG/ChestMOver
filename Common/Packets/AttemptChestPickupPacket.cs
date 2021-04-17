using System.IO;
using ChestMover.Core.Networking;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace ChestMover.Common.Packets
{
	/// <summary>
	/// Sent by players that attempt to pickup a chest. <br/>
	/// If successful, the chest's contents are sent to the player who picked it up, and all others players get a short version of that packet, without the items.
	/// </summary>
	public class AttemptChestPickupPacket : NetPacket
	{
		public AttemptChestPickupPacket(Point16 chestPosition)
		{
			Writer.Write(chestPosition.X);
			Writer.Write(chestPosition.Y);
		}

		public override void Read(BinaryReader reader, int sender)
		{
			Point16 chestPos = new Point16(reader.ReadInt16(), reader.ReadInt16());

			if (Main.netMode != NetmodeID.Server)
			{
				return;
			}

			int chestId = Chest.FindChest(chestPos.X, chestPos.Y);

			if (!Main.chest.IndexInRange(chestId))
			{
				return;
			}

			var player = Main.player[sender];

			if (player?.active != true)
			{
				return;
			}

			var chest = Main.chest[chestId];

			player.GetModPlayer<ChestMoverPlayer>().PickupChest(chest);

			MultiplayerSystem.SendPacket(new ChestPickupConfirmationPacket(chest), toClient: sender);
			MultiplayerSystem.SendPacket(new VisualChestPickupPacket((byte)sender, chest), ignoreClient: sender);
		}
	}
}
