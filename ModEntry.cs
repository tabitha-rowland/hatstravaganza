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
using System.Reflection;
using System.Linq;


namespace Hatstravaganza
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private HatRenderer hatRenderer; // Instance of HatRenderer to manage hat drawing
        private DialogueManager dialogueManager;

        private bool waitingForHatMail;
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
            // helper.Events.Player.MailReceived += OnMailReceived;


            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;

            helper.Events.World.ObjectListChanged += OnObjectListChanged;

            //debug stuff
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.ConsoleCommands.Add("hat_dumpmail", "Dump player's mailReceived to log", DumpMailCommand);



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

        //debug for mail
        private void OnUpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            // Only run when world ready and once per second-ish to avoid spam
            if (!Context.IsWorldReady)
                return;

            // throttle: run every 60 ticks (~1 second)
            if (e.Ticks % 60 != 0)
                return;

            // sanity
            if (Game1.player == null)
                return;

            const string mailId = "Hatstravaganza.HatBoxMail";
            const string givenFlag = "Hatstravaganza.HatBoxGiven";

            // If player has read the mail but we haven't given the box yet
            if (Game1.player.mailReceived.Contains(mailId) &&
                !Game1.player.mailReceived.Contains(givenFlag))
            {
                Monitor.Log("Detected Hat Box mail in mailReceived — giving Hat Box now.", LogLevel.Info);

                GivePlayerHatBox();

                // mark so we don't give it again
                Game1.player.mailReceived.Add(givenFlag);
            }
        }


        private void DumpMailCommand(string cmd, string[] args)
        {
            if (Game1.player == null)
            {
                Monitor.Log("player is null", LogLevel.Info);
                return;
            }

            Monitor.Log("=== player.mailReceived ===", LogLevel.Info);
            foreach (var id in Game1.player.mailReceived)
                Monitor.Log($"  {id}", LogLevel.Info);
        }

        private void OnSaved(object sender, SavedEventArgs e)
        {
            // Save hat data when game saves
            hatManager.SaveNPCHats();
            this.Monitor.Log("Hat data saved with game save", LogLevel.Info);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // Check if a chest menu just closed (for refilling)
            if (e.OldMenu is StardewValley.Menus.ItemGrabMenu itemGrabMenu && e.NewMenu == null)
            {
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
            if (!Game1.player.mailReceived.Contains("Hatstravaganza.HatBoxReceived"))
            {
                // Add to mailbox
                Game1.player.mailbox.Add("Hatstravaganza.HatBoxMail");
                waitingForHatMail = true;
                this.Monitor.Log("Hat Box mail added to mailbox!", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log("Player already has Hat Box", LogLevel.Debug);
            }
        }


        private void GivePlayerHatBox()
        {
            this.Monitor.Log("Creating Hat Box chest...", LogLevel.Debug);

            Chest hatBox = new Chest(true); // Player chest
            hatBox.Name = "Hat Box";
            hatBox.displayName = "Hat Box"; // Also set display name
            hatBox.playerChest.Value = true;
            hatBox.fridge.Value = false;
            hatBox.Type = "interactive";
            hatBox.specialChestType.Value = Chest.SpecialChestTypes.None;



            // Try adding as Item instead of Object
            bool added = Game1.player.addItemToInventoryBool(hatBox);

            if (added)
            {
                Game1.addHUDMessage(new HUDMessage("Received: Hat Box", 2));
                this.Monitor.Log("Hat Box added to inventory!", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log("Failed to add Hat Box to inventory - inventory full?", LogLevel.Warn);
                // Drop it at player's feet instead
                Game1.createItemDebris(hatBox, Game1.player.Position, -1);
            }
        }
        private void FillHatBox(Chest chest)
        {
            var hatItemIds = hatRenderer.GetAllHatItemIds();

            foreach (var itemId in hatItemIds)
            {
                int currentCount = 0;

                foreach (var item in chest.Items)
                {
                    if (item is StardewValley.Object obj && obj.ItemId == itemId)
                        currentCount += obj.Stack;
                }

                int hatsToAdd = 36 - currentCount;
                if (hatsToAdd > 0)
                {
                    Monitor.Log($"Adding {hatsToAdd} of {itemId}", LogLevel.Debug);
                    StardewValley.Object hatItem = new StardewValley.Object(itemId, hatsToAdd);

                    chest.Items.Add(hatItem);
                }
            }

            Monitor.Log($"Filled Hat Box with {hatItemIds.Count} hat types", LogLevel.Debug);
        }

        private void OnObjectListChanged(object sender, StardewModdingAPI.Events.ObjectListChangedEventArgs e)
        {
            // Only react in player's current location
            if (!Context.IsWorldReady || e.Location != Game1.player.currentLocation)
                return;

            foreach (var added in e.Added)
            {
                if (added.Value is Chest chest)
                {
                    // Detect your Hat Box by name
                    if (chest.Name == "Hat Box")
                    {
                        Monitor.Log("Detected newly placed Hat Box — filling now.", LogLevel.Debug);

                        // Fill the chest AFTER it is placed, not before
                        FillHatBox(chest);
                    }
                }
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
            // Register Hat Box mail
            if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    if (!data.ContainsKey("Hatstravaganza.HatBoxMail"))
                    {
                        data["Hatstravaganza.HatBoxMail"] =
                            "Dear @,^" +
                            "Thanks for downloading Hatstravaganza! " +
                            "A Hat Box that'll never run out of hats has been added to your inventory. " +
                            "Remember that you can always add your own custom hat art- checkout the readme! ^" +
                            "   -Tab^";

                        this.Monitor.Log("Registered Hat Box mail", LogLevel.Debug);
                    }
                });
            }
            // Patch item spritesheet to add custom hat icons
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/springobjects"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsImage();
                    var hatItemIds = hatRenderer.GetAllHatItemIds();

                    this.Monitor.Log($"Patching {hatItemIds.Count} hat icons into spritesheet", LogLevel.Debug);

                    // CALCULATE MAX HEIGHT NEEDED FIRST
                    int maxId = 0;
                    foreach (var itemId in hatItemIds)
                    {
                        if (int.TryParse(itemId, out int id) && id > maxId)
                            maxId = id;
                    }

                    // EXTEND ONCE before patching anything
                    if (maxId > 0)
                    {
                        int maxY = (maxId / 24) * 16 + 16;
                        if (maxY > editor.Data.Height)
                        {
                            this.Monitor.Log($"Extending spritesheet from {editor.Data.Height} to {maxY}", LogLevel.Debug);
                            editor.ExtendImage(minWidth: editor.Data.Width, minHeight: maxY);
                        }
                    }

                    // NOW patch all icons
                    foreach (var itemId in hatItemIds)
                    {
                        if (int.TryParse(itemId, out int id))
                        {
                            int x = (id % 24) * 16;
                            int y = (id / 24) * 16;

                            Texture2D icon = hatRenderer.GetItemIcon(itemId);

                            if (icon != null)
                            {
                                editor.PatchImage(icon, null, new Rectangle(x, y, 16, 16));
                                this.Monitor.Log($"Patched icon for item {itemId} at ({x}, {y})", LogLevel.Debug);
                            }
                        }
                    }
                });
            }
            // Register custom hat items
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.Objects.ObjectData>().Data;
                    var hatItemIds = hatRenderer.GetAllHatItemIds();

                    foreach (string itemId in hatItemIds)
                    {
                        if (!data.ContainsKey(itemId))
                        {
                            string hatName = hatRenderer.GetHatNameFromItemId(itemId);

                            // Create ObjectData for this hat
                            data[itemId] = new StardewValley.GameData.Objects.ObjectData
                            {
                                Name = hatName,
                                DisplayName = hatName,
                                Description = $"A stylish hat from Hatstravaganza.",
                                Type = "Basic",
                                Category = -999, // Miscellaneous
                                Price = 50,
                                Texture = null, // Uses default spritesheet
                                SpriteIndex = int.Parse(itemId)
                            };

                            this.Monitor.Log($"Registered {hatName} as item {itemId}", LogLevel.Debug);
                        }
                    }

                    this.Monitor.Log($"Registered {hatItemIds.Count} hat items in Data/Objects", LogLevel.Info);
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

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // Did the player open our letter?
            if (Game1.player.mailReceived.Contains("Hatstravaganza.HatBoxMail") &&
                !Game1.player.mailReceived.Contains("Hatstravaganza.HatBoxGiven"))
            {
                Monitor.Log("Hat Box mail detected as read — giving Hat Box!", LogLevel.Info);

                GivePlayerHatBox();

                // Prevent duplicates
                Game1.player.mailReceived.Add("Hatstravaganza.HatBoxGiven");
            }
        }




    }
}