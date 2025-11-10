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
            hatManager = new HatManager(this.Monitor);  

            // Subscribe to hat gift confirmation
            dialogueManager.OnHatGiftConfirmed += OnHatGiftConfirmed;

            // Register event handlers
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.Monitor.Log("Save loaded! Giving player pumpkin hat item...", LogLevel.Info);

            // Create a Void Egg as placeholder (it's purple/spooky like a pumpkin!)
            // Item ID "305" = Void Egg
            StardewValley.Object pumpkinHatToken = new StardewValley.Object("305", 1);

            // Override the display name so you know it's the "hat"
            pumpkinHatToken.displayName = "Pumpkin Hat (temp)";

            // Add to player inventory
            Game1.player.addItemToInventory(pumpkinHatToken);

            this.Monitor.Log("Pumpkin hat token added to inventory!", LogLevel.Info);
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
                        string itemName = Game1.player.ActiveObject.Name;

                        // Check if item name contains "Hat" (simple check for now)
                        if (itemName.Contains("Hat"))
                        {
                            this.Monitor.Log($"Player is gifting hat: {itemName} to {npc.Name}", LogLevel.Info);

                            // Show confirmation dialogue
                            dialogueManager.ShowHatGiftConfirmation(npc, itemName);

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
            foreach (NPC npc in Game1.currentLocation.characters)
            {
                // Check if this NPC has a hat
                if (hatManager.NPCHasHat(npc.Name))
                {
                    hatRenderer.DrawHatOnNPC(npc, e.SpriteBatch);
                }
            }
        }


    }
}