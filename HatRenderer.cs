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

        // Path to offsets file
        private string offsetsFilePath;

        public HatRenderer(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;

            // Initialize defaults
            defaultOffsets = new NPCHatOffsets();
            npcOffsets = new Dictionary<string, NPCHatOffsets>();

            offsetsFilePath = Path.Combine(helper.DirectoryPath, "hat-offsets.json");

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
                if (!File.Exists(offsetsFilePath))
                {
                    monitor.Log("hat-offsets.json not found, using default offsets", LogLevel.Warn);
                    return;
                }

                // Read and parse JSON
                string json = File.ReadAllText(offsetsFilePath);
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

        /// <summary>
        /// Adjust offset for an NPC in real-time
        /// </summary>
        public bool AdjustOffset(string npcName, string directionStr, string axis, int amount)
        {
            try
            {
                // Ensure NPC exists in dictionary
                if (!npcOffsets.ContainsKey(npcName))
                {
                    monitor.Log($"Creating new offset entry for {npcName}", LogLevel.Debug);
                    npcOffsets[npcName] = new NPCHatOffsets();
                }

                // Get the offset for this direction
                HatOffset offset = null;
                switch (directionStr)
                {
                    case "down":
                        offset = npcOffsets[npcName].Down;
                        break;
                    case "up":
                        offset = npcOffsets[npcName].Up;
                        break;
                    case "left":
                        offset = npcOffsets[npcName].Left;
                        break;
                    case "right":
                        offset = npcOffsets[npcName].Right;
                        break;
                    default:
                        monitor.Log($"Invalid direction: {directionStr}", LogLevel.Error);
                        return false;
                }

                // Adjust the offset
                if (axis == "x")
                {
                    offset.X += amount;
                    monitor.Log($"{npcName} {directionStr} X: {offset.X - amount} → {offset.X}", LogLevel.Info);
                }
                else if (axis == "y")
                {
                    offset.Y += amount;
                    monitor.Log($"{npcName} {directionStr} Y: {offset.Y - amount} → {offset.Y}", LogLevel.Info);
                }
                else
                {
                    monitor.Log($"Invalid axis: {axis} (use 'x' or 'y')", LogLevel.Error);
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                monitor.Log($"Error adjusting offset: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Save current offsets to JSON file
        /// </summary>
        public bool SaveCurrentOffsets()
        {
            try
            {
                string json = JsonConvert.SerializeObject(npcOffsets, Formatting.Indented);
                File.WriteAllText(offsetsFilePath, json);

                monitor.Log($"Saved offsets for {npcOffsets.Count} NPCs to {offsetsFilePath}", LogLevel.Info);
                return true;
            }
            catch (System.Exception ex)
            {
                monitor.Log($"Error saving offsets: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Show current offsets for an NPC
        /// </summary>
        public void ShowOffsets(string npcName)
        {
            if (!npcOffsets.ContainsKey(npcName))
            {
                monitor.Log($"{npcName} has no custom offsets (using defaults)", LogLevel.Info);
                monitor.Log($"  Down: X={defaultOffsets.Down.X}, Y={defaultOffsets.Down.Y}", LogLevel.Info);
                monitor.Log($"  Up: X={defaultOffsets.Up.X}, Y={defaultOffsets.Up.Y}", LogLevel.Info);
                monitor.Log($"  Left: X={defaultOffsets.Left.X}, Y={defaultOffsets.Left.Y}", LogLevel.Info);
                monitor.Log($"  Right: X={defaultOffsets.Right.X}, Y={defaultOffsets.Right.Y}", LogLevel.Info);
                return;
            }

            var offsets = npcOffsets[npcName];
            monitor.Log($"{npcName}'s current offsets:", LogLevel.Info);
            monitor.Log($"  Down: X={offsets.Down.X}, Y={offsets.Down.Y}", LogLevel.Info);
            monitor.Log($"  Up: X={offsets.Up.X}, Y={offsets.Up.Y}", LogLevel.Info);
            monitor.Log($"  Left: X={offsets.Left.X}, Y={offsets.Left.Y}", LogLevel.Info);
            monitor.Log($"  Right: X={offsets.Right.X}, Y={offsets.Right.Y}", LogLevel.Info);
        }

        public void DrawHatOnNPC(NPC npc, SpriteBatch spriteBatch)
        {
            if (hatTexture == null)
                return;

            int direction = npc.FacingDirection;
            Rectangle sourceRect = new Rectangle(direction * 16, 0, 16, 16);
            Vector2 npcPosition = npc.getLocalPosition(Game1.viewport);

            // Add animation offsets
            npcPosition.Y += npc.yJumpOffset;  // Jumping

            // Add walking bob animation
            // NPCs bob when sprite.currentFrame is odd (1, 3, 5...)
            if (npc.Sprite != null && npc.Sprite.currentFrame % 2 == 1)
            {
                npcPosition.Y -= 4; // Move up slightly during walk cycle
            }

            HatOffset offset = GetOffsetForNPC(npc.Name, direction);

            float hatScale = 3f;
            float npcSpriteWidth = 64f;
            float hatWidth = 16f * hatScale;
            float centerOffset = (npcSpriteWidth - hatWidth) / 2f;

            Vector2 hatPosition = new Vector2(
                npcPosition.X + centerOffset + (offset.X * 4f),
                npcPosition.Y + (offset.Y * 4f)
            );

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