using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Characters;
using System.Collections;
using Microsoft.Xna.Framework.Graphics;

namespace Hatstravaganza
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private HatRenderer hatRenderer; // Instance of HatRenderer to manage hat drawing
        private DialogueManager dialogueManager;

        private HatManager hatManager;

        private SpriteAnalyzer spriteAnalyzer;  // Add this

        private Texture2D icon;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        ///         
        public override void Entry(IModHelper helper)
        {
            this.Monitor.Log("Hatstravaganza mod loaded!", LogLevel.Info);

            // Create managers
            hatRenderer = new HatRenderer(helper, this.Monitor);
            dialogueManager = new DialogueManager(helper, this.Monitor);
            hatManager = new HatManager(helper, this.Monitor);
            spriteAnalyzer = new SpriteAnalyzer(helper, this.Monitor);

            // Subscribe to events
            dialogueManager.OnHatGiftConfirmed += OnHatGiftConfirmed;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saved += this.OnSaved;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            // NEW: Detect when menus close to refill Hat Box
            helper.Events.Display.MenuChanged += this.OnMenuChanged;



            // ... console commands
            helper.ConsoleCommands.Add("hat_all", "Give all NPCs a hat", GiveAllNPCsHatsCommand);

            // Adjust hats
            helper.ConsoleCommands.Add("hat_remove", "Remove hat from an NPC\n\nUsage: hat_remove <npc_name>", RemoveHatCommand);
            helper.ConsoleCommands.Add("hat_clear", "Remove all hats from all NPCs", ClearAllHatsCommand);

            helper.ConsoleCommands.Add("hat_adjust", "Adjust hat offset in real-time\n\nUsage: hat_adjust <npc> <direction> <x|y> <amount>", HatAdjustCommand);
            helper.ConsoleCommands.Add("hat_save", "Save current offsets to JSON file", HatSaveCommand);
            helper.ConsoleCommands.Add("hat_show", "Show current offsets for an NPC\n\nUsage: hat_show <npc_name>", HatShowCommand);

            helper.ConsoleCommands.Add("hat_analyze", "Analyze NPC sprites and generate hat offsets", AnalyzeSpritesCommand);

        }
        private void OnSaved(object sender, SavedEventArgs e)
        {
            // Save hat data when game saves
            hatManager.SaveNPCHats();
            this.Monitor.Log("Hat data saved with game save", LogLevel.Info);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // Check if a chest menu just closed
            if (e.OldMenu is StardewValley.Menus.ItemGrabMenu itemGrabMenu && e.NewMenu == null)
            {
                // Check if it was a Hat Box
                if (itemGrabMenu.context is Chest chest && chest.Name == "Hat Box")
                {
                    this.Monitor.Log("Hat Box closed, refilling...", LogLevel.Debug);
                    FillHatBox(chest);
                }
            }
        }
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.Monitor.Log("Save loaded! Loading hat data...", LogLevel.Info);

            // Load existing hat data
            hatManager.LoadNPCHats();

            // Check if player already received Hat Box
            bool hasReceivedBefore = Game1.player.mailReceived.Contains("Hatstravaganza.HatBox");
            this.Monitor.Log($"Player mailReceived check: {hasReceivedBefore}", LogLevel.Info);

            if (!hasReceivedBefore)
            {
                this.Monitor.Log("Attempting to give Hat Box...", LogLevel.Info);
                GivePlayerHatBox();
                Game1.player.mailReceived.Add("Hatstravaganza.HatBox");
                this.Monitor.Log("Hat Box given and mail flag added!", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log("Player already has Hat Box", LogLevel.Info);
            }
        }

        private void GivePlayerHatBox()
        {
            this.Monitor.Log("Creating Hat Box chest...", LogLevel.Debug);

            Chest hatBox = new Chest(true);
            hatBox.Name = "Hat Box";

            // Get all hat item IDs from renderer
            var hatItemIds = hatRenderer.GetAllHatItemIds();

            this.Monitor.Log($"Adding {hatItemIds.Count} hat types to box", LogLevel.Debug);

            // Fill with hats
            foreach (var itemId in hatItemIds)
            {
                // Use the actual item IDs (9000, 9001, etc)
                StardewValley.Object hatItem = new StardewValley.Object(itemId, 36);
                hatBox.Items.Add(hatItem);
            }

            Game1.player.addItemToInventory(hatBox);
            Game1.addHUDMessage(new HUDMessage("Received: Hat Box", 2));

            this.Monitor.Log("Hat Box given!", LogLevel.Info);
            FillHatBox(hatBox);
            this.Monitor.Log("Hat Box filled!", LogLevel.Info);

        }

        private void FillHatBox(Chest chest)
        {
            // Get all registered hats from the renderer
            var hatItemIds = hatRenderer.GetAllHatItemIds();

            foreach (var itemId in hatItemIds)
            {
                // Count how many of this hat are currently in the chest
                int currentCount = 0;

                foreach (var item in chest.Items)
                {
                    if (item != null && item is StardewValley.Object obj && obj.ItemId == itemId)
                    {
                        currentCount += obj.Stack;
                    }
                }

                // Calculate how many to add to reach 36
                int hatsToAdd = 36 - currentCount;

                if (hatsToAdd > 0)
                {
                    this.Monitor.Log($"  Adding {hatsToAdd} of item {itemId} (had {currentCount})", LogLevel.Debug);
                    StardewValley.Object hatItem = new StardewValley.Object(itemId, hatsToAdd);
                    chest.addItem(hatItem);
                }
                this.Monitor.Log($"Filled Hat Box with {hatItemIds.Count} hat types", LogLevel.Debug);
            }
        }




        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu != null)
                return;

            if (e.Button == SButton.MouseLeft || e.Button == SButton.MouseRight)
            {
                if (Game1.player?.ActiveObject != null)
                {
                    Vector2 tile = e.Cursor.GrabTile;
                    NPC npc = Game1.currentLocation?.isCharacterAtTile(tile);

                    if (npc != null)
                    {
                        StardewValley.Object heldItem = Game1.player.ActiveObject;

                        // Check if this item ID is registered as a hat
                        string hatName = hatRenderer.GetHatNameFromItemId(heldItem.ItemId);

                        if (hatName != null)
                        {
                            this.Monitor.Log($"Player is gifting {hatName} to {npc.Name}", LogLevel.Info);
                            dialogueManager.ShowHatGiftConfirmation(npc, hatName);
                            Helper.Input.Suppress(e.Button);
                        }
                    }
                }
            }
        }



        private void OnHatGiftConfirmed(NPC npc, string hatName)
        {
            this.Monitor.Log($"Gift confirmed! {npc.Name} will wear {hatName}", LogLevel.Info);

            // Remove item from player inventory
            Game1.player.reduceActiveItemByOne();

            // Save NPC hat state
            hatManager.GiveHatToNPC(npc.Name, hatName);
            this.Monitor.Log($"{npc.Name} now has {hatName} in hat manager", LogLevel.Debug);
        }



        ///once word is rendered, do ...
        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.CurrentEvent != null)
            {
                foreach (NPC npc in Game1.CurrentEvent.actors)
                {
                    if (hatManager.NPCHasHat(npc.Name))
                    {
                        string hatName = hatManager.GetNPCHat(npc.Name);  // Get which hat they're wearing
                        hatRenderer.DrawHatOnNPC(npc, hatName, e.SpriteBatch);  // Pass hat name
                    }
                }
            }
            else
            {
                foreach (NPC npc in Game1.currentLocation.characters)
                {
                    if (hatManager.NPCHasHat(npc.Name))
                    {
                        string hatName = hatManager.GetNPCHat(npc.Name);  // Get which hat they're wearing
                        hatRenderer.DrawHatOnNPC(npc, hatName, e.SpriteBatch);  // Pass hat name
                    }
                }
            }
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Patch item spritesheet to add custom hat icons
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/springobjects"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsImage();

                    // Get all hat item IDs
                    var hatItemIds = hatRenderer.GetAllHatItemIds();

                    foreach (var itemId in hatItemIds)
                    {
                        // Parse the item ID to get position in spritesheet
                        if (int.TryParse(itemId, out int id))
                        {
                            // Calculate position in 24-wide spritesheet
                            int x = (id % 24) * 16;
                            int y = (id / 24) * 16;

                            // Get the icon from renderer
                            icon = hatRenderer.GetItemIcon(itemId);

                            if (icon != null)
                            {
                                // Patch into spritesheet
                                editor.PatchImage(icon, null, new Rectangle(x, y, 16, 16));
                                this.Monitor.Log($"Patched icon for item {itemId} at ({x}, {y})", LogLevel.Debug);
                            }
                        }
                    }
                });
            }
        }

        private void RemoveHatCommand(string command, string[] args)
        {
            if (args.Length == 0)
            {
                this.Monitor.Log("Usage: hat_remove <npc_name>", LogLevel.Info);
                return;
            }

            string npcName = args[0];

            if (hatManager.NPCHasHat(npcName))
            {
                hatManager.RemoveHatFromNPC(npcName);
                this.Monitor.Log($"Removed hat from {npcName}", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log($"{npcName} doesn't have a hat", LogLevel.Info);
            }
        }

        private void ClearAllHatsCommand(string command, string[] args)
        {
            hatManager.ClearAllHats();
            this.Monitor.Log("Removed all hats from all NPCs", LogLevel.Info);
        }





        // live tune hat placements


        private void HatAdjustCommand(string command, string[] args)
        {
            if (args.Length < 4)
            {
                this.Monitor.Log("Usage: hat_adjust <npc_name> <direction> <x|y> <amount>", LogLevel.Info);
                this.Monitor.Log("Example: hat_adjust Alex down y -2", LogLevel.Info);
                this.Monitor.Log("Directions: up, down, left, right", LogLevel.Info);
                return;
            }

            string npcName = args[0];
            string directionStr = args[1].ToLower();
            string axis = args[2].ToLower();

            if (!int.TryParse(args[3], out int amount))
            {
                this.Monitor.Log("Amount must be a number", LogLevel.Error);
                return;
            }

            // Tell HatRenderer to adjust the offset
            bool success = hatRenderer.AdjustOffset(npcName, directionStr, axis, amount);

            if (success)
            {
                this.Monitor.Log($"Adjusted {npcName}'s {directionStr} {axis} offset by {amount}", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log($"Failed to adjust offset", LogLevel.Error);
            }
        }

        private void HatSaveCommand(string command, string[] args)
        {
            bool success = hatRenderer.SaveCurrentOffsets();

            if (success)
            {
                this.Monitor.Log("Saved current hat offsets to hat-offsets.json", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log("Failed to save offsets", LogLevel.Error);
            }
        }

        private void HatShowCommand(string command, string[] args)
        {
            if (args.Length < 1)
            {
                this.Monitor.Log("Usage: hat_show <npc_name>", LogLevel.Info);
                return;
            }

            string npcName = args[0];
            hatRenderer.ShowOffsets(npcName);
        }

        private void GiveAllNPCsHatsCommand(string command, string[] args)
        {
            // List of all giftable NPCs
            string[] allNPCs = new string[]
            {
        "Abigail", "Alex", "Caroline", "Clint", "Demetrius", "Dwarf", "Elliott",
        "Emily", "Evelyn", "George", "Gus", "Haley", "Harvey", "Jas", "Jodi",
        "Kent", "Krobus", "Leah", "Lewis", "Linus", "Marnie", "Maru", "Pam",
        "Penny", "Pierre", "Robin", "Sam", "Sandy", "Sebastian", "Shane",
        "Vincent", "Willy", "Wizard"
            };

            int count = 0;
            foreach (string npcName in allNPCs)
            {
                hatManager.GiveHatToNPC(npcName, "Pumpkin Hat");
                count++;
            }

            this.Monitor.Log($"Gave hats to {count} NPCs!", LogLevel.Info);
        }


        private void AnalyzeSpritesCommand(string command, string[] args)
        {
            this.Monitor.Log("Analyzing NPC sprites...", LogLevel.Info);

            try
            {
                // Run the analysis
                var offsets = spriteAnalyzer.AnalyzeAllNPCs();

                this.Monitor.Log($"Successfully analyzed {offsets.Count} NPCs", LogLevel.Info);

                // Save to a new file
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(offsets, Newtonsoft.Json.Formatting.Indented);
                string filePath = System.IO.Path.Combine(Helper.DirectoryPath, "hat-offsets-generated.json");
                System.IO.File.WriteAllText(filePath, json);

                this.Monitor.Log($"Saved generated offsets to: hat-offsets-generated.json", LogLevel.Info);
                this.Monitor.Log("Review the file and copy to hat-offsets.json if it looks good!", LogLevel.Info);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error during analysis: {ex.Message}", LogLevel.Error);
            }
        }
    }
}