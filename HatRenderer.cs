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
            if (hatTexture == null)
                return;

            int direction = npc.FacingDirection;
            Rectangle sourceRect = new Rectangle(direction * 16, 0, 16, 16);
            Vector2 npcPosition = npc.getLocalPosition(Game1.viewport);

            HatOffset offset = GetOffsetForNPC(npc.Name, direction);

            // DEBUG: Log the offset being used
            monitor.Log($"Drawing hat on {npc.Name}, offset: X={offset.X}, Y={offset.Y}", LogLevel.Debug);

            float hatScale = 3f;
            float npcSpriteWidth = 64f;
            float hatWidth = 16f * hatScale;
            float centerOffset = (npcSpriteWidth - hatWidth) / 2f;

            Vector2 hatPosition = new Vector2(
                npcPosition.X + centerOffset + (offset.X * 4f),
                npcPosition.Y + (offset.Y * 4f)
            );

            // DEBUG: Log final position
            monitor.Log($"Hat position: {hatPosition}, NPC position: {npcPosition}", LogLevel.Debug);

            spriteBatch.Draw(
                hatTexture,
                hatPosition,
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                hatScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}

