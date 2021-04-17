using System.IO;
using ChestMover.Core.Networking;
using Terraria;
using Terraria.ModLoader.IO;

namespace ChestMover.Common.Packets
{
	/// <summary>
	/// Sent by the server to players who's recent attempts to pickup a chest have been confirmed to be a success. <br/>
	/// Contains the full list of items, intended only to be received by the player who picked up the chest.
	/// </summary>
	public class ChestPickupConfirmationPacket : NetPacket
	{
		public ChestPickupConfirmationPacket(Chest chest)
		{
			Writer.Write(Main.tile[chest.x, chest.y].frameX);
			Writer.Write(chest.name);
			Writer.Write((short)chest.item.Length);

			for (int i = 0; i < chest.item.Length; i++)
			{
				ItemIO.Send(chest.item[i], Writer, writeStack: true);
			}
		}

		public override void Read(BinaryReader reader, int sender)
		{
			short chestType = reader.ReadInt16();
			string chestName = reader.ReadString();
			short numItems = reader.ReadInt16();

			var items = new Item[numItems];

			for (int i = 0; i < numItems; i++)
			{
				items[i] = ItemIO.Receive(reader, readStack: true);
			}

			Main.LocalPlayer.GetModPlayer<ChestMoverPlayer>().PickupChest(chestType, chestName, items);
		}
	}
}
