using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Hatstravaganza
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private HatRenderer hatRenderer; // Instance of HatRenderer to manage hat drawing

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>

        public override void Entry(IModHelper helper)
        {
                this.Monitor.Log("Hatstravaganza mod loaded!", LogLevel.Info);

            this.hatRenderer = new HatRenderer(helper);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
        }

        ///once word is rendered, do ...
        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // Only draw if world is ready
            if (!Context.IsWorldReady)
                return;

            // Find Leah in the current location
            NPC leah = Game1.currentLocation.getCharacterFromName("Leah");

            // If Leah is in this location, draw her hat
            if (leah != null)
            {
                hatRenderer.DrawHatOnNPC(leah, e.SpriteBatch);
            }
        }
    }
}