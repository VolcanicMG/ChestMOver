using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ChestMover.Core.Networking
{
	public static class MultiplayerSystem
	{
		private static List<NetPacket> packets;
		private static Dictionary<Type, NetPacket> packetsByType;

		private static Mod modInstance;

		//Get
		public static NetPacket GetPacket(byte id) => packets[id];

		public static NetPacket GetPacket(Type type) => packetsByType[type];

		public static T GetPacket<T>() where T : NetPacket => ModContent.GetInstance<T>();

		//Send
		public static void SendPacket<T>(T packet, int toClient = -1, int ignoreClient = -1, Func<Player, bool> sendDelegate = null) where T : NetPacket
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
			{
				return;
			}

			ModPacket modPacket = modInstance.GetPacket();

			modPacket.Write((byte)packet.Id);
			packet.WriteAndDispose(modPacket);

			try {
				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					modPacket.Send();
				}
				else if (toClient != -1)
				{
					modPacket.Send(toClient, ignoreClient);
				}
				else
				{
					for (int i = 0; i < Main.player.Length; i++)
					{
						var player = Main.player[i];

						if (i != ignoreClient && Netplay.Clients[i].State >= 10 && (sendDelegate?.Invoke(player) ?? true))
						{
							modPacket.Send(i);
						}
					}
				}
			}
			catch { }
		}

		internal static void Load(Mod mod)
		{
			modInstance = mod;

			packets = new List<NetPacket>();
			packetsByType = new Dictionary<Type, NetPacket>();

			foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(NetPacket))))
			{
				var instance = (NetPacket)FormatterServices.GetUninitializedObject(type);

				instance.Id = packets.Count;
				packetsByType[type] = instance;

				packets.Add(instance);

				ContentInstance.Register(instance);
			}
		}

		internal static void Unload()
		{
			modInstance = null;

			if (packets != null)
			{
				packets.Clear();

				packets = null;
			}

			if (packetsByType != null)
			{
				packetsByType.Clear();

				packetsByType = null;
			}
		}

		internal static void HandlePacket(BinaryReader reader, int sender)
		{
			try
			{
				byte packetId = reader.ReadByte();

				if (packetId > packets.Count)
				{
					return;
				}

				var packet = packets[packetId];

				packet.Read(reader, sender);
			}
			catch
			{
				//TODO: Log
			}
		}
	}
}
