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

        // Dictionary to map item IDs to hat names
        private Dictionary<string, string> itemIdToHatName;

        // Default offsets for NPCs not in the file
        private NPCHatOffsets defaultOffsets;

        // Dictionary to cache hat textures
        private Dictionary<string, Texture2D> hatTextures;

        // Dictionary to cache item icon textures
        private Dictionary<string, Texture2D> itemIcons;

        // Path to offsets file
        private string offsetsFilePath;

        public HatRenderer(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;


            // monitor.Log("Initializing HatRenderer...", LogLevel.Info);

            // Initialize defaults
            defaultOffsets = new NPCHatOffsets();
            npcOffsets = new Dictionary<string, NPCHatOffsets>();
            hatTextures = new Dictionary<string, Texture2D>();  // Initialize hat dictionary
            itemIdToHatName = new Dictionary<string, string>();
            itemIcons = new Dictionary<string, Texture2D>();

            offsetsFilePath = Path.Combine(helper.DirectoryPath, "hat-offsets.json");

            // LoadHatRegistry();   // Load first
            LoadHatSprites();    // Then load textures
            LoadHatOffsets();
            // monitor.Log($"Hat offsets have been called and run", LogLevel.Info);

        }

        /// <summary>
        /// Get hat name from item ID
        /// </summary>
        public string GetHatNameFromItemId(string itemId)
        {
            if (itemIdToHatName.ContainsKey(itemId))
            {
                return itemIdToHatName[itemId];
            }
            return null;
        }


        private void LoadHatSprites()
        {
            try
            {
                monitor.Log("Auto-discovering hat sprites in assets folder...", LogLevel.Info);

                string assetsPath = Path.Combine(helper.DirectoryPath, "assets");

                if (!Directory.Exists(assetsPath))
                {
                    Directory.CreateDirectory(assetsPath);
                    return;
                }

                string[] pngFiles = Directory.GetFiles(assetsPath, "*.png");
                string[] pngItemFiles = Directory.GetFiles(assetsPath, "*-item.png");

                foreach (string png in pngFiles)
                {
                    foreach (string itemPng in pngItemFiles)
                    {
                        if (Path.GetFileNameWithoutExtension(png) + "-item" == Path.GetFileNameWithoutExtension(itemPng))
                        {
                            // monitor.Log($"Removing item texture {Path.GetFileName(itemPng)} from hat list", LogLevel.Info);
                            List<string> tempList = new List<string>(pngFiles);
                            tempList.Remove(itemPng);
                            pngFiles = tempList.ToArray();
                        }
                    }
                }

                monitor.Log($"Found {pngFiles.Length} PNG files", LogLevel.Info);


                itemIdToHatName.Clear();
                int nextId = 950;

                foreach (string filePath in pngFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string hatName = FormatHatName(fileName);

                        string relativePath = $"assets/{Path.GetFileName(filePath)}";
                        Texture2D texture = helper.ModContent.Load<Texture2D>(relativePath);

                        if (texture != null)
                        {
                            hatTextures[hatName] = texture;

                            string itemId = nextId.ToString();
                            itemIdToHatName[itemId] = hatName;

                            // Extract item icon
                            // In HatRenderer LoadHatSprites, after extracting icon:
                            Texture2D icon = ExtractItemIcon(texture);
                            if (icon != null)
                            {
                                itemIcons[itemId] = icon;
                                // monitor.Log($"  Created 16×16 icon for {hatName}", LogLevel.Debug);
                            }
                            else
                            {
                                monitor.Log($"  Failed to create icon for {hatName}", LogLevel.Warn);
                            }

                            //monitor.Log($"  ✓ {hatName} → Item ID {itemId}", LogLevel.Info);
                            nextId++;
                        }
                    }
                    catch (Exception ex)
                    {
                        monitor.Log($"  ✗ Failed: {Path.GetFileName(filePath)} - {ex.Message}", LogLevel.Warn);
                    }
                }

                // monitor.Log($"Loaded {hatTextures.Count} hats with icons", LogLevel.Info);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error loading hats: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Get item icon texture for a hat
        /// </summary>
        public Texture2D GetItemIcon(string itemId)
        {
            if (itemIcons.ContainsKey(itemId))
            {
                return itemIcons[itemId];
            }
            return null;
        }


        private Texture2D ExtractItemIcon(Texture2D hatSprite)
        {
            try
            {
                // Extract first 16x16 frame (down-facing)
                Color[] pixels = new Color[16 * 16];
                hatSprite.GetData(0, new Rectangle(0, 0, 16, 16), pixels, 0, pixels.Length);

                // Create new 16x16 texture
                Texture2D icon = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
                icon.SetData(pixels);

                return icon;
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to extract icon: {ex.Message}", LogLevel.Error);
                return null;
            }
        }


        /// <summary>
        /// Convert filename to display name
        /// Examples: "pumpkin-hat" -> "Pumpkin Hat", "santa_hat" -> "Santa Hat"
        /// </summary>
        private string FormatHatName(string fileName)
        {
            // Replace hyphens and underscores with spaces
            string formatted = fileName.Replace("-", " ").Replace("_", " ");

            // Capitalize each word
            var words = formatted.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            return string.Join(" ", words);
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

                // monitor.Log($"Loaded hat offsets for {npcOffsets.Count} NPCs", LogLevel.Info);

                // Log each NPC for debugging
                // foreach (var npcName in npcOffsets.Keys)
                // {
                //     monitor.Log($"  - {npcName}", LogLevel.Debug);
                // }
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

        public void DrawHatOnNPC(NPC npc, string hatName, SpriteBatch spriteBatch)
        {

            // Get the correct texture for this hat
            if (!hatTextures.ContainsKey(hatName))
            {
                monitor.Log($"No texture found for hat: {hatName}", LogLevel.Warn);
                return;
            }

            Texture2D hatTexture = hatTextures[hatName];

            if (hatTexture == null)
                return;

            int direction = npc.FacingDirection;

            // Remap Stardew directions to your sprite sheet order
            int spriteFrame = direction switch
            {
                2 => 0,  // Down -> frame 0
                0 => 1,  // Up -> frame 1
                3 => 2,  // Left -> frame 2
                1 => 3,  // Right -> frame 3
                _ => 0   // Default to down
            };

            Rectangle sourceRect = new Rectangle(spriteFrame * 16, 0, 16, 16);


            Vector2 npcPosition = npc.getLocalPosition(Game1.viewport);

            // Add animation offsets
            npcPosition.Y += npc.yJumpOffset;

            // Add walking bob animation

            if (npc.Sprite != null && npc.Sprite.currentFrame % 2 == 1 && npc.Name != "George") // Exclude George from bobbing because of wheelchair
            {
                npcPosition.Y -= -4;
            }

            HatOffset offset = GetOffsetForNPC(npc.Name, direction);

            float hatScale = 3f;
            float npcSpriteWidth = 64f;
            float hatWidth = 16f * hatScale;
            float centerOffset = (npcSpriteWidth - hatWidth) / 2f;


            Vector2 hatPosition = new Vector2(
           npcPosition.X + centerOffset + (offset.X * 4f),
           npcPosition.Y + (offset.Y * 4f) + 10  // Add extra +10 for testing
            );





            float layerDepth = (npc.Position.Y + 128f) / 10000f;

            spriteBatch.Draw(
                hatTexture,
                hatPosition,
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                hatScale,
                SpriteEffects.None,
                layerDepth
            );
        }
        /// <summary>
        /// Get all registered hat item IDs
        /// </summary>
        public List<string> GetAllHatItemIds()
        {
            return new List<string>(itemIdToHatName.Keys);
        }
    }
}