using System;
using System.IO;
using ChestMover.Core.Networking;
using Terraria;
using Terraria.ModLoader.IO;

namespace ChestMover.Common.Packets
{
	public class SetChestContentsPacket : NetPacket
	{
		public SetChestContentsPacket(int chestId, Item[] items)
		{
			Writer.Write(chestId);
			Writer.Write((short)items.Length);

			for (int i = 0; i < items.Length; i++) {
				ItemIO.Send(items[i], Writer, writeStack: true);
			}
		}

		public override void Read(BinaryReader reader, int sender)
		{
			int chestId = reader.ReadInt32();
			short numItems = reader.ReadInt16();

			if (!Main.chest.IndexInRange(chestId))
			{
				return;
			}

			var chest = Main.chest[chestId];
			var items = new Item[numItems];

			for (int i = 0; i < numItems; i++) {
				items[i] = ItemIO.Receive(reader, readStack: true);
			}

			Array.Copy(items, chest.item, items.Length);
		}
	}
}
