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
            hatRenderer = new HatRenderer(helper);
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
            helper.Events.GameLoop.Saved += this.OnSaved;  // Add this
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

            // Get all NPCs in current location
            this.Monitor.Log($"Checking {Game1.currentLocation.characters.Count} NPCs in {Game1.currentLocation.Name}", LogLevel.Debug);

            foreach (NPC npc in Game1.currentLocation.characters)
            {
                this.Monitor.Log($"Checking NPC: {npc.Name}", LogLevel.Debug);

                // Check if this NPC has a hat
                if (hatManager.NPCHasHat(npc.Name))
                {
                    this.Monitor.Log($"{npc.Name} HAS a hat! Drawing it...", LogLevel.Info);
                    hatRenderer.DrawHatOnNPC(npc, e.SpriteBatch);
                }
                else
                {
                    this.Monitor.Log($"{npc.Name} does NOT have a hat", LogLevel.Debug);
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


    }
}