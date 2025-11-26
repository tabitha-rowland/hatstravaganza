using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Characters;

namespace Hatstravaganza
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private HatRenderer hatRenderer; // Instance of HatRenderer to manage hat drawing
        private DialogueManager dialogueManager;

        private HatManager hatManager;


        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>

        public override void Entry(IModHelper helper)
        {
            this.Monitor.Log("Hatstravaganza mod loaded!", LogLevel.Info);

            // Create managers
            hatRenderer = new HatRenderer(helper, this.Monitor);
            dialogueManager = new DialogueManager(helper, this.Monitor);
            hatManager = new HatManager(helper, this.Monitor);  // Added helper parameter

            // Load custom hat data
            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            // Subscribe to hat gift confirmation
            dialogueManager.OnHatGiftConfirmed += OnHatGiftConfirmed;



            // Register event handlers
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saved += this.OnSaved;

            // Give everyone hats
            helper.ConsoleCommands.Add("hat_all", "Give all NPCs a hat", GiveAllNPCsHatsCommand);

            // Adjust hats
            helper.ConsoleCommands.Add("hat_remove", "Remove hat from an NPC\n\nUsage: hat_remove <npc_name>", RemoveHatCommand);
            helper.ConsoleCommands.Add("hat_clear", "Remove all hats from all NPCs", ClearAllHatsCommand);

            helper.ConsoleCommands.Add("hat_adjust", "Adjust hat offset in real-time\n\nUsage: hat_adjust <npc> <direction> <x|y> <amount>", HatAdjustCommand);
            helper.ConsoleCommands.Add("hat_save", "Save current offsets to JSON file", HatSaveCommand);
            helper.ConsoleCommands.Add("hat_show", "Show current offsets for an NPC\n\nUsage: hat_show <npc_name>", HatShowCommand);

        }
        private void OnSaved(object sender, SavedEventArgs e)
        {
            // Save hat data when game saves
            hatManager.SaveNPCHats();
            this.Monitor.Log("Hat data saved with game save", LogLevel.Info);
        }
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.Monitor.Log("Save loaded! Loading hat data and giving player pumpkin hat...", LogLevel.Info);

            // Load existing hat data
            hatManager.LoadNPCHats();

            // Give player pumpkin hat token
            StardewValley.Object pumpkinHatToken = new StardewValley.Object("305", 1);
            Game1.player.addItemToInventory(pumpkinHatToken);

            this.Monitor.Log("Pumpkin hat token (Void Egg) added to inventory!", LogLevel.Info);
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Don't process clicks if a menu is already open
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

                        this.Monitor.Log($"Player holding: {heldItem.DisplayName} (ID: {heldItem.ItemId})", LogLevel.Debug);

                        // Check if it's our pumpkin hat token (Void Egg ID = 305)
                        if (heldItem.ItemId == "305")
                        {
                            this.Monitor.Log($"Player is gifting Pumpkin Hat to {npc.Name}", LogLevel.Info);

                            // Show confirmation dialogue
                            dialogueManager.ShowHatGiftConfirmation(npc, "Pumpkin Hat");

                            // Suppress normal gift behavior
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
            // Only draw if world is ready
            if (!Context.IsWorldReady)
                return;

            // Check if we're in a cutscene/event
            if (Game1.CurrentEvent != null)
            {
                // During events, only draw on event actors
                foreach (NPC npc in Game1.CurrentEvent.actors)
                {
                    if (hatManager.NPCHasHat(npc.Name))
                    {
                        hatRenderer.DrawHatOnNPC(npc, e.SpriteBatch);
                    }
                }
            }
            else
            {
                // Normal gameplay - draw on location characters
                foreach (NPC npc in Game1.currentLocation.characters)
                {
                    if (hatManager.NPCHasHat(npc.Name))
                    {
                        hatRenderer.DrawHatOnNPC(npc, e.SpriteBatch);
                    }
                }
            }
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Add custom hat data
            if (e.NameWithoutLocale.IsEquivalentTo("Data/hats"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    // Add pumpkin hat (using ID 9999 to avoid conflicts)
                    data["9999"] = "Pumpkin Hat/A festive pumpkin hat/true/false/Hatstravaganza/Hatstravaganza.PumpkinHat";

                    this.Monitor.Log("Added Pumpkin Hat to game data", LogLevel.Debug);
                });
            }

            // Add custom hat texture
            if (e.NameWithoutLocale.IsEquivalentTo("Characters/Farmer/hats"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsImage();
                    var pumpkinHatTexture = Helper.ModContent.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/pumpkin-hat.png");

                    // Patch it into the hat spritesheet at position for hat ID 9999
                    // This is complex - for now let's use a simpler approach

                    this.Monitor.Log("Would patch hat texture here", LogLevel.Debug);
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

    }
}