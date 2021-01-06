using Terraria;
using Terraria.ModLoader;

namespace ChestMover.Buffs
{
    public class MovingDebuff : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Heavy");
            Description.SetDefault("Your carrying something heavy!");
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            canBeCleared = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {

        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.slow = true;
        }
    }
}