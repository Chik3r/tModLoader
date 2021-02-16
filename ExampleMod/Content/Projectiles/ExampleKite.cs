using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExampleMod.Content.Projectiles
{
	public class ExampleKite : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_766";

		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 4;
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
		}

		public override void SetDefaults() {
			Projectile.width = 4;
			Projectile.height = 4;
			//aiStyle = 160;
			Projectile.penetrate = -1;
			Projectile.extraUpdates = 60;
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			// TODO: Is this the player hand?
			Vector2 playerHand = player.RotatedRelativePoint(player.MountedCenter);
			Projectile.timeLeft = 60;
			//player.itemAnimation = 2;

			bool killKite = false;
			// If the player is frozen, stoned, webbed or can't use items, kill the kite
			if (player.CCed || player.noItems)
				killKite = true;
			// If the selected item doesn't shoot the kite, kill the kite
			else if (player.inventory[player.selectedItem].shoot != Projectile.type)
				killKite = true;
			// If the player is in a pulley, kill the kite
			else if (player.pulley)
				killKite = true;
			// If the player is dead, kill the kite
			else if (player.dead)
				killKite = true;

			// If the kite is more than 2000 pixels away, kill the kite
			if (!killKite)
				killKite = (player.Center - Projectile.Center).Length() > 2000f;

			if (killKite) {
				Projectile.Kill();
				return;
			}

			// Set the limits for the kite length
			float minKiteLength = 4f;
			float maxKiteLength = 500f;
			float defaultKiteLength = maxKiteLength / 2f;
			if (Projectile.owner == Main.myPlayer && Projectile.extraUpdates == 0) {
				// Store the old ai[0]
				float oldAi = Projectile.ai[0];

				// If ai[0] is 0, set it to 
				if (Projectile.ai[0] == 0f)
					Projectile.ai[0] = defaultKiteLength;

				float updatedAi = Projectile.ai[0];
				if (Main.mouseRight)
					updatedAi -= 5f;

				if (Main.mouseLeft)
					updatedAi += 5f;

				Projectile.ai[0] = MathHelper.Clamp(updatedAi, minKiteLength, maxKiteLength);
				if (oldAi != updatedAi)
					Projectile.netUpdate = true;
			}

			if (Projectile.numUpdates == 1)
				Projectile.extraUpdates = 0;

			float visualWind = 0f;
			if (WorldGen.InAPlaceWithWind(Projectile.position, Projectile.width, Projectile.height))
				visualWind = Main.WindForVisuals;

			float windYSpeed = Utils.GetLerpValue(0.2f, 0.5f, Math.Abs(visualWind), true) * 0.5f;

			// TODO: Discover this magic
			Vector2 targetPosition = Projectile.Center + new Vector2(visualWind, (float)Math.Sin(Main.GlobalTimeWrappedHourly) + Main.cloudAlpha * 5f) * 25f;
			Vector2 targetVelocity = targetPosition - Projectile.Center;
			targetVelocity = targetVelocity.SafeNormalize(Vector2.Zero) * (3f + Main.cloudAlpha * 7f);
			if (windYSpeed == 0f)
				targetVelocity = Projectile.velocity;

			float distanceToTarget = Projectile.Distance(targetPosition);
			float lerpValue = Utils.GetLerpValue(5f, 10f, distanceToTarget, true);
			float oldY = Projectile.velocity.Y;
			if (distanceToTarget > 10f)
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetVelocity, 0.075f * lerpValue);

			Projectile.velocity.Y = oldY;
			Projectile.velocity.Y -= windYSpeed;
			Projectile.velocity.Y += 0.02f + windYSpeed * 0.25f;
			Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y, -2f, 2f);
			if (Projectile.Center.Y + Projectile.velocity.Y < targetPosition.Y)
				Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, Projectile.velocity.Y + windYSpeed + 0.01f, 0.75f);

			Projectile.velocity.X *= 0.98f;
			float distanceToPlayerHand = Projectile.Distance(playerHand);
			if (distanceToPlayerHand > Projectile.ai[0]) {
				Vector2 directionToPlayerHand = Projectile.DirectionTo(playerHand);
				float scaleFactor = distanceToPlayerHand - Projectile.ai[0];
				Projectile.Center += directionToPlayerHand * scaleFactor;
				bool dotted = Vector2.Dot(directionToPlayerHand, Vector2.UnitY) < 0.8f || windYSpeed > 0f;

				Projectile.velocity.Y += directionToPlayerHand.Y * 0.05f;

				if (dotted)
					Projectile.velocity.Y -= 0.15f;

				Projectile.velocity.X += directionToPlayerHand.X * 0.2f;
				if (Projectile.ai[0] == minKiteLength && Projectile.owner == Main.myPlayer) {
					Projectile.Kill();
					return;
				}
			}

			Projectile.timeLeft = 2;
			Vector2 handOffset = Projectile.Center - playerHand;
			int dir = (handOffset.X > 0f) ? 1 : -1;
			if (Math.Abs(handOffset.X) > Math.Abs(handOffset.Y) / 2f)
				player.ChangeDir(dir);

			Vector2 normalizedDirectionToHand = Projectile.DirectionTo(playerHand).SafeNormalize(Vector2.Zero);
			if (windYSpeed == 0f && Projectile.velocity.Y > -0.02f) {
				Projectile.rotation *= 0.95f;
			} else {
				float newRotation = (-normalizedDirectionToHand).ToRotation() + (float)Math.PI / 4f;
				if (Projectile.spriteDirection == -1)
					newRotation -= (float)Math.PI / 2f * player.direction;

				Projectile.rotation = newRotation + Projectile.velocity.X * 0.05f;
			}


			float velLength = Projectile.velocity.Length();
			// Change the frame depending on the length of the velocity
			// TODO: Frame changing code
			//Projectile.frame = 0;
			if (velLength < 3f)
				Projectile.frame = 0;
			else if (velLength < 5f)
				Projectile.frame = 1;
			else if (velLength < 7f)
				Projectile.frame = 2;
			else
				Projectile.frame = 3;

			Projectile.spriteDirection = player.direction;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) => false;

		public override void PostDraw(SpriteBatch spriteBatch, Color lightColor) {
			int extraFrames = 15;
			float someExtraRotation = 0f;
			int numberTrailingExtras = 10;
			int num4 = 5;
			float distanceBetweenExtras = 10f;
			float someRotation = 0f;
			int someDrawOffsetOrSomething = -14;
			int someYDrawOffset = -2;
			int startCustomDistance = -1;
			int customDistance = -1;
			int someWindThing = 8;
			int someRectangleThing = 3;
			int timesToDrawVertically = 1;
			int moreDrawOffsets = 0;
			int moreYDrawOffsets = 0;
			bool someExtraDrawFlag = true;
			bool decreaseSomeRectangle = false;

			SpriteEffects effects = Projectile.spriteDirection != -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Texture2D mainTexture = ModContent.GetTexture(Texture).Value;
			Rectangle mainRectangle = mainTexture.Frame(Main.projFrames[Projectile.type], 1, Projectile.frame);
			Vector2 mainOrigin = mainRectangle.Size() / 2f;
			Vector2 mainPosition = Projectile.Center - Main.screenPosition;

			Color color = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
			Color alpha = Projectile.GetAlpha(color);

			Texture2D fishingLineTexture = TextureAssets.FishingLine.Value;
			Rectangle fishingLineRectangle = fishingLineTexture.Frame();
			Vector2 fishingLineOrigin = new Vector2(fishingLineRectangle.Width / 2, 2f);

			Texture2D extraTexture = TextureAssets.Extra[103].Value;
			Rectangle extraRectangle = extraTexture.Frame(extraFrames);
			
			int extraWidth = extraRectangle.Width;
			extraRectangle.Width -= 2;
			// TODO: Find name
			Vector2 origin3 = extraRectangle.Size() / 2f;
			extraRectangle.X = extraWidth * (extraFrames - 1);

			Vector2 playerArmPosition = Main.GetPlayerArmPosition(Projectile);
			Vector2 projCenter = Projectile.Center;

			float scaleFactor = 12f;

			Vector2 playerArmPos = playerArmPosition;
			Vector2 distanceToArm = projCenter - playerArmPos;
			Vector2 projVelocity = Projectile.velocity;
			if (Math.Abs(projVelocity.X) > Math.Abs(projVelocity.Y))
				Utils.Swap(ref projVelocity.X, ref projVelocity.Y);

			float distanceArmLength = distanceToArm.Length();
			// TODO: Find name
			// Some kind of limit?
			float num17 = 16f;
			float num18 = 80f;
			bool someDrawCondition = true;
			if (distanceArmLength == 0f) {
				someDrawCondition = false;
			}
			else {
				distanceToArm *= 12f / distanceArmLength;
				playerArmPos -= distanceToArm;
				distanceToArm = projCenter - playerArmPos;
			}

			while (someDrawCondition) {
				float sourceRectCustomHeight = 12f;
				distanceArmLength = distanceToArm.Length();
				float distanceToArmLength = distanceArmLength;

				if (float.IsNaN(distanceArmLength) || distanceArmLength == 0f) {
					someDrawCondition = false;
					continue;
				}

				if (distanceArmLength < 20f) {
					sourceRectCustomHeight = distanceArmLength - 8f;
					someDrawCondition = false;
				}

				distanceArmLength = 12f / distanceArmLength;
				distanceToArm *= distanceArmLength;
				playerArmPos += distanceToArm;
				distanceToArm = projCenter - playerArmPos;

				if (distanceToArmLength > 12f) {
					// TODO: Find name
					float num22 = 0.3f;
					float num23 = Math.Abs(projVelocity.X) + Math.Abs(projVelocity.Y);
					if (num23 > num17)
						num23 = num17;

					num23 = 1f - num23 / num17;
					num22 *= num23;
					num23 = distanceToArmLength / num18;
					if (num23 > 1f)
						num23 = 1f;

					num22 *= num23;
					if (num22 < 0f)
						num22 = 0f;

					num23 = 1f;
					num22 *= num23;

					if (distanceToArm.Y > 0f) {
						distanceToArm.Y *= 1f + num22;
						distanceToArm.X *= 1f - num22;
					}
					else {
						num23 = Math.Abs(projVelocity.X) / 3f;
						if (num23 > 1f)
							num23 = 1f;

						num23 -= 0.5f;
						num22 *= num23;
						if (num22 > 0f)
							num22 *= 2f;

						distanceToArm.Y *= 1f + num22;
						distanceToArm.X *= 1f - num22;
					}
				}

				float rotation = distanceToArm.ToRotation() - (float)Math.PI / 2f;
				if (!someDrawCondition)
					fishingLineRectangle.Height = (int)sourceRectCustomHeight;

				Color centerColor = Lighting.GetColor(projCenter.ToTileCoordinates());
				Main.EntitySpriteDraw(fishingLineTexture, playerArmPos - Main.screenPosition, fishingLineRectangle, centerColor, rotation,
					fishingLineOrigin, 1f, SpriteEffects.None, 0);
			}

			Vector2 halfProjSize = Projectile.Size / 2f;
			float absoluteWind = Math.Abs(Main.WindForVisuals);
			float lerpedSpeedWind = MathHelper.Lerp(0.5f, 1f, absoluteWind);
			float absoluteWindCopy = absoluteWind;

			if (distanceToArm.Y >= -0.02f && distanceToArm.Y < 1f)
				absoluteWindCopy = Utils.GetLerpValue(0.2f, 0.5f, absoluteWind, true);

			// TODO: find name
			int num27 = num4;
			int numTrailing = numberTrailingExtras + 1;

			// TODO: Is this needed?
			for (int i = 0; i < timesToDrawVertically; i++) {
				extraRectangle.X = extraWidth * (extraFrames - 1);
				List<Vector2> posList = new List<Vector2>();

				// TODO: Find name for this
				// Maybe some pos thing
				Vector2 value6 = new Vector2(lerpedSpeedWind * someWindThing * Projectile.spriteDirection,
					(float)Math.Sin(Main.timeForVisualEffects / 300f * 6.2831854820251465 * absoluteWindCopy) * 2f);

				float xDrawOffset = someDrawOffsetOrSomething + moreDrawOffsets;
				float yDrawOffset = someYDrawOffset + moreYDrawOffsets;

				//switch (i) {
				//	case 1:
				//		value6 = new Vector2(lerpedSpeedWind * someWindThing * Projectile.spriteDirection,
				//			(float)Math.Sin(Main.timeForVisualEffects / 300f * 6.2831854820251465 * absoluteWindCopy + 0.5f) * 2f);
				//		xDrawOffset -= 8f;
				//		yDrawOffset -= 8f;
				//		break;
				//		// TODO: Are these really needed?
				//		//case 2:
				//		//	break;
				//		//case 3:
				//		//	break;
				//}

				Vector2 value7 = Projectile.Center +
				                 new Vector2(((float)mainRectangle.Width * 0.5f + xDrawOffset) * (float)Projectile.spriteDirection,
					                 yDrawOffset).RotatedBy(Projectile.rotation + someRotation);
				posList.Add(value7);

				int num31 = num27;
				int num32 = 1;
				while (num31 < numTrailing * num27) {
					//if (startCustomDistance != )

					Vector2 oldPos = Projectile.oldPos[num31];
					if (oldPos.X == 0f && oldPos.Y == 0f) {
						posList.Add(value7);
					}
					else {
						oldPos += halfProjSize +
						          new Vector2((mainRectangle.Width * 0.5f + (float)xDrawOffset) * (float)Projectile.oldSpriteDirection[num31],
							          yDrawOffset).RotatedBy(Projectile.oldRot[num31] + someRotation);
						//value8 += value5 +
						//          new Vector2(((float)rectangle.Width * 0.5f + num29) * (float)proj.oldSpriteDirection[num31], num30)
						//	          .RotatedBy(proj.oldRot[num31] + someRotation);
						oldPos += value6 * (num32 + 1);

						Vector2 difference = value7 - oldPos;
						float diffLength = difference.Length();

						if (diffLength > distanceBetweenExtras)
							diffLength *= distanceBetweenExtras / diffLength;

						oldPos = value7 - difference;
						posList.Add(oldPos);
						value7 = oldPos;
					}

					num31 += num27;
					num32++;
				}

				if (someExtraDrawFlag) {
					Rectangle fishLineRect = fishingLineTexture.Frame();
					for (int j = posList.Count - 2; j >= 0; j--) {
						Vector2 pos = posList[j];
						Vector2 nextPos = posList[j + 1] - pos;
						float nextPosLength = nextPos.Length();

						if (!(nextPosLength < 2f)) {
							float rotation = nextPos.ToRotation() - (float)Math.PI / 2f;
							Main.EntitySpriteDraw(fishingLineTexture, pos - Main.screenPosition, fishLineRect, alpha, rotation,
								fishingLineOrigin, new Vector2(1f, nextPosLength / fishLineRect.Height), SpriteEffects.None, 0);
						}
					}
				}

				for (int num36 = posList.Count - 2; num36 >= 0; num36--) {
					Vector2 value11 = posList[num36];
					Vector2 value12 = posList[num36 + 1];
					Vector2 v2 = value12 - value11;
					v2.Length();
					float rotation3 = v2.ToRotation() - (float)Math.PI / 2f + someExtraRotation;
					Main.EntitySpriteDraw(extraTexture, value12 - Main.screenPosition, extraRectangle, alpha, rotation3, origin3, Projectile.scale, effects, 0);
					extraRectangle.X -= extraWidth;
					if (extraRectangle.X < 0) {
						int num37 = someRectangleThing;
						if (decreaseSomeRectangle)
							num37--;

						extraRectangle.X = num37 * extraWidth;
					}
				}
			}

			Main.EntitySpriteDraw(mainTexture, mainPosition, mainRectangle, alpha, Projectile.rotation + someRotation, mainOrigin, Projectile.scale, effects, 0);

			return;
		}
	}
}
