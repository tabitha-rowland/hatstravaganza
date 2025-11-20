using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;

namespace Hatstravaganza
{
    public class HatRenderer
    {
        private IModHelper helper;
        private IMonitor monitor;
        private Texture2D hatTexture;

        // Dictionary to store offsets per NPC
        private Dictionary<string, NPCHatOffsets> npcOffsets;

        // Default offsets for NPCs not in the file
        private NPCHatOffsets defaultOffsets;

        public HatRenderer(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;

            // Initialize defaults
            defaultOffsets = new NPCHatOffsets();
            npcOffsets = new Dictionary<string, NPCHatOffsets>();

            LoadHatSprite();
            LoadHatOffsets();
        }

        private void LoadHatSprite()
        {
            try
            {
                hatTexture = helper.ModContent.Load<Texture2D>("assets/pumpkin-hat.png");

                if (hatTexture == null)
                {
                    throw new System.Exception("Hat texture loaded but is null");
                }

                monitor.Log("Hat texture loaded successfully", LogLevel.Debug);
            }
            catch (System.Exception ex)
            {
                monitor.Log($"Failed to load hat texture: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadHatOffsets()
        {
            try
            {
                // Path to the JSON file
                string filePath = Path.Combine(helper.DirectoryPath, "hat-offsets.json");

                if (!File.Exists(filePath))
                {
                    monitor.Log("hat-offsets.json not found, using default offsets", LogLevel.Warn);
                    return;
                }

                // Read and parse JSON
                string json = File.ReadAllText(filePath);
                npcOffsets = JsonConvert.DeserializeObject<Dictionary<string, NPCHatOffsets>>(json);

                if (npcOffsets == null)
                {
                    npcOffsets = new Dictionary<string, NPCHatOffsets>();
                }

                monitor.Log($"Loaded hat offsets for {npcOffsets.Count} NPCs", LogLevel.Info);

                // Log each NPC for debugging
                foreach (var npcName in npcOffsets.Keys)
                {
                    monitor.Log($"  - {npcName}", LogLevel.Debug);
                }
            }
            catch (System.Exception ex)
            {
                monitor.Log($"Failed to load hat offsets: {ex.Message}", LogLevel.Error);
                npcOffsets = new Dictionary<string, NPCHatOffsets>();
            }
        }

        /// <summary>
        /// Get the hat offset for a specific NPC and direction
        /// </summary>
        private HatOffset GetOffsetForNPC(string npcName, int direction)
        {
            // Check if we have custom offsets for this NPC
            if (npcOffsets.ContainsKey(npcName))
            {
                return npcOffsets[npcName].GetOffsetForDirection(direction);
            }

            // Fall back to default offsets
            return defaultOffsets.GetOffsetForDirection(direction);
        }

        public void DrawHatOnNPC(NPC npc, SpriteBatch spriteBatch)
        {
            // Safety check - don't try to draw if texture failed to load
            if (hatTexture == null)
                return;

            // Get which direction the NPC is facing (0=up, 1=right, 2=down, 3=left)
            int direction = npc.FacingDirection;

            // Calculate which frame of our sprite sheet to use
            Rectangle sourceRect = new Rectangle(direction * 16, 0, 16, 16);

            // Get NPC's position on screen
            Vector2 npcPosition = npc.getLocalPosition(Game1.viewport);

            // Get the custom offset for this NPC and direction
            HatOffset offset = GetOffsetForNPC(npc.Name, direction);
            Vector2 offsetVector = offset.ToVector2();

            // Scale the offset by the game's zoom level (4x)
            offsetVector *= 4f;

            // Position hat on top of NPC's head using custom offset
            Vector2 hatPosition = new Vector2(
                npcPosition.X + offsetVector.X,
                npcPosition.Y + offsetVector.Y
            );

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