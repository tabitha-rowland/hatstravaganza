using System;
using StardewModdingAPI;
using StardewValley;


namespace Hatstravaganza
{
    /// Handles all dialogue interactions for hat gifting

    public class DialogueManager
    {
        private IModHelper helper;
        private IMonitor monitor;

        // Callback for when player confirms hat gift
        public event Action<NPC, string> OnHatGiftConfirmed;

        public DialogueManager(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }

        /// Show confirmation dialogue for gifting a hat to an NPC
        public void ShowHatGiftConfirmation(NPC npc, string hatName)
        {
            monitor.Log($"Showing gift confirmation for {hatName} to {npc.Name}", LogLevel.Debug);

            // Create yes/no responses
            Response[] responses = new Response[]
            {
                new Response("Yes", $"Give {hatName} to {npc.Name}"),
                new Response("No", "Keep it")
            };

            // Show the question dialogue
            Game1.currentLocation.createQuestionDialogue(
                $"Give {hatName} hat to {npc.Name}?",
                responses,
                (who, answer) => HandleHatGiftResponse(npc, hatName, answer)
            );
        }

        /// Handle the player's response to the gift confirmation
        private void HandleHatGiftResponse(NPC npc, string hatName, string answer)
        {
            monitor.Log($"Player answered: {answer}", LogLevel.Debug);

            if (answer == "Yes" && npc.Name != "Willy" && npc.Name != "Wizard")
            {
                monitor.Log($"Player confirmed gifting {hatName} to {npc.Name}", LogLevel.Info);

                // Show NPC receiving hat dialogue
                ShowNPCReceivedHatDialogue(npc, hatName);

                // Trigger the confirmed event
                OnHatGiftConfirmed?.Invoke(npc, hatName);
            }
            else if (npc.Name == "Willy" || npc.Name == "Wizard")
            {
                monitor.Log($"Gifting hats to {npc.Name} is not supported because they already have a built-in hat.", LogLevel.Info);
                // Show a message that this NPC cannot receive hats
                ShowNPCDialogue(npc, "Oh.. thanks... but I already have a hat.");
                // ShowNPCDialogue(npc, "Ummm no. In case you haven't noticed, I'm weird. I'm a weirdo. I don't fit it, and I don't... wanna fit in. Have you ever seen me without this stupid hat on? That's weird. So, no.");

            }
            else
            {
                monitor.Log($"Player cancelled gifting {hatName}", LogLevel.Debug);
                // Do nothing - player keeps the item
            }
        }

        /// Show dialogue when NPC receives a hat
        private void ShowNPCReceivedHatDialogue(NPC npc, string hatName)
        {
            // Create a custom dialogue for the NPC
            string dialogueText = $"Oh, a {hatName}! Thanks!$h";

            Dialogue dialogue = new Dialogue(npc, null, dialogueText);
            npc.CurrentDialogue.Push(dialogue);

            // Show the dialogue
            Game1.drawDialogue(npc);

            monitor.Log($"{npc.Name} received the hat!", LogLevel.Debug);
        }

        /// Show generic NPC dialogue (for future use)
        public void ShowNPCDialogue(NPC npc, string message)
        {
            Dialogue dialogue = new Dialogue(npc, null, message);
            npc.CurrentDialogue.Push(dialogue);
            Game1.drawDialogue(npc);
        }
    }
}