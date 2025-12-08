
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace Hatstravaganza
{
    /// <summary>
    /// Analyzes NPC sprites to calculate hat positioning offsets
    /// </summary>
    public class SpriteAnalyzer
    {
        private IModHelper helper;
        private IMonitor monitor;

        public SpriteAnalyzer(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }

        /// <summary>
        /// Analyze all NPCs and generate offset data
        /// </summary>
        public Dictionary<string, NPCHatOffsets> AnalyzeAllNPCs()
        {
            var allOffsets = new Dictionary<string, NPCHatOffsets>();

            string[] npcNames = new string[]
            {
                "Abigail", "Alex", "Caroline", "Clint", "Demetrius", "Dwarf", "Elliott",
                "Emily", "Evelyn", "George", "Gus", "Haley", "Harvey", "Jas", "Jodi",
                "Kent", "Krobus", "Leah", "Lewis", "Linus", "Marnie", "Maru", "Pam",
                "Penny", "Pierre", "Robin", "Sam", "Sandy", "Sebastian", "Shane",
                "Vincent", "Willy", "Wizard"
            };

            foreach (string npcName in npcNames)
            {
                try
                {
                    NPCHatOffsets offsets = AnalyzeNPC(npcName);
                    if (offsets != null)
                    {
                        allOffsets[npcName] = offsets;
                        monitor.Log($"Analyzed {npcName}", LogLevel.Debug);
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed to analyze {npcName}: {ex.Message}", LogLevel.Warn);
                }
            }

            return allOffsets;
        }

        /// <summary>
        /// Analyze a single NPC's sprite to find head position
        /// </summary>
        private NPCHatOffsets AnalyzeNPC(string npcName)
        {
            try
            {
                // Load the NPC's sprite sheet
                Texture2D spriteSheet = helper.GameContent.Load<Texture2D>($"Characters/{npcName}");

                if (spriteSheet == null)
                {
                    monitor.Log($"Could not load sprite for {npcName}", LogLevel.Warn);
                    return null;
                }

                // Get pixel data
                Color[] pixels = new Color[spriteSheet.Width * spriteSheet.Height];
                spriteSheet.GetData(pixels);

                NPCHatOffsets offsets = new NPCHatOffsets();

                // Analyze each direction
                // Down = frame 0, Up = frame 1, Left = frame 2, Right = frame 3
                offsets.Down = AnalyzeDirection(pixels, spriteSheet.Width, 0);
                offsets.Up = AnalyzeDirection(pixels, spriteSheet.Width, 1);
                offsets.Left = AnalyzeDirection(pixels, spriteSheet.Width, 2);
                offsets.Right = AnalyzeDirection(pixels, spriteSheet.Width, 2); //use same as left

                return offsets;
            }
            catch (Exception ex)
            {
                monitor.Log($"Error analyzing {npcName}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Analyze a specific direction sprite to find head position
        /// </summary>
        private HatOffset AnalyzeDirection(Color[] pixels, int spriteWidth, int direction)
        {
            // NPC sprites are 16x32 pixels
            // They're arranged in rows, with each row being a different animation
            // We want the standing frame (frame 0) for each direction

            int frameX = direction * 16;  // Each frame is 16 pixels wide
            int frameY = 0;                // First row is standing frames

            // Find the topmost non-transparent pixel (top of head)
            int topOfHead = FindTopOfHead(pixels, spriteWidth, frameX, frameY);

            // Hat should sit just above the head
            // Calculate offset from top of 32-pixel sprite
            int yOffset = topOfHead - 32;  // Negative value (hat goes above sprite origin)

            //0 is down, 1 is up, 2 is left, 3 is right

            // Adjust based on direction (back of head is different)
            if (direction == 0) // down
            {
                yOffset += 2;
            }

            else if (direction == 1) // Up/back view
            {
                yOffset += 2; // Slightly lower for back view
            }

            else if (direction == 2 || direction == 3) // left and right
            {
                yOffset += 3;
            }
            else
            {

            }





            return new HatOffset(0, yOffset);
        }

        /// <summary>
        /// Find the Y position of the top of the NPC's head
        /// </summary>
        private int FindTopOfHead(Color[] pixels, int spriteWidth, int frameX, int frameY)
        {
            // Scan from top to bottom to find first non-transparent pixel
            for (int y = 0; y < 32; y++)
            {
                for (int x = frameX; x < frameX + 16; x++)
                {
                    int index = (frameY + y) * spriteWidth + x;

                    if (index < pixels.Length && pixels[index].A > 0)
                    {
                        // Found first non-transparent pixel
                        return y;
                    }
                }
            }

            // Default if no pixels found
            return 8;
        }
    }
}