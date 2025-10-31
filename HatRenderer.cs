using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace Hatstravaganza
{
    public class HatRenderer
    {
        private IModHelper helper;
        private Texture2D hatTexture;

        public HatRenderer(IModHelper helper)
        {
            this.helper = helper;
            LoadHatSprite();
        }

        private void LoadHatSprite()
        {
            // Load the hat sprite from assets folder
            hatTexture = helper.ModContent.Load<Texture2D>("assets/pumpkin-hat.png");
        }

        public void DrawHatOnNPC(NPC npc, SpriteBatch spriteBatch)
        {
            // Get which direction the NPC is facing (0=up, 1=right, 2=down, 3=left)
            int direction = npc.FacingDirection;

            // Calculate which frame of our sprite sheet to use
            Rectangle sourceRect = new Rectangle(direction * 16, 0, 16, 16);

            // Get NPC's position on screen
            Vector2 npcPosition = npc.getLocalPosition(Game1.viewport);

            // Position hat on top of NPC's head (adjust Y offset as needed)
            Vector2 hatPosition = new Vector2(npcPosition.X, npcPosition.Y - 32);

            // Draw the hat
            spriteBatch.Draw(
                hatTexture,
                hatPosition,
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                4f,  // Scale (Stardew uses 4x zoom)
                SpriteEffects.None,
                0f
            );
        }
    }
}