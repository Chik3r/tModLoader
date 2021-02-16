using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ID;

namespace ExampleMod.Content.Items
{
	public class ExampleKite : ModItem
	{
		public override string Texture => "Terraria/Images/Item_4367";

		public override void SetStaticDefaults() {
			ItemID.Sets.IsAKite[Item.type] = true;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 28;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.shoot = ModContent.ProjectileType<Projectiles.ExampleKite>();
			Item.shootSpeed = 2f;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.maxStack = 1;
			Item.SetShopValues(ItemRarityColor.Blue1, Item.buyPrice(0, 2));

			// The above can be shortened to:
			// Item.DefaultTokite(ModContent.ProjectileType<Projectiles.ExampleKite>());
		}

		public override bool CanUseItem(Player player) {
			return !Main.projectile.Any(x => x.active && x.type == ModContent.ProjectileType<Projectiles.ExampleKite>());
		}

		//public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage,
		//	ref float knockBack) {
		//	if (Main.projectile.Any(x => x.active && x.type == ModContent.ProjectileType<Projectiles.ExampleKite>())) {
		//		return false;
		//	}

		//	return true;
		//}
	}
}
