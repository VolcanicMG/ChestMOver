using System.IO;
using ChestMover.Core.Networking;
using Terraria.ModLoader;

namespace ChestMover
{
	public class ChestMover : Mod
    {
		public override void Load() => MultiplayerSystem.Load(this);

		public override void Unload() => MultiplayerSystem.Unload();

		public override void HandlePacket(BinaryReader reader, int sender) => MultiplayerSystem.HandlePacket(reader, sender);
	}
}