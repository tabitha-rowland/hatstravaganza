using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace Hatstravaganza
{
    /// <summary>
    /// Manages which NPCs are wearing which hats
    /// </summary>
    public class HatManager
    {
        private IMonitor monitor;
        
        // Dictionary: NPC name -> hat name
        private Dictionary<string, string> npcHats;
        
        public HatManager(IMonitor monitor)
        {
            this.monitor = monitor;
            npcHats = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Assign a hat to an NPC
        /// </summary>
        public void GiveHatToNPC(string npcName, string hatName)
        {
            if (npcHats.ContainsKey(npcName))
            {
                monitor.Log($"Replacing {npcName}'s hat with {hatName}", LogLevel.Debug);
                npcHats[npcName] = hatName;
            }
            else
            {
                monitor.Log($"Giving {npcName} a {hatName}", LogLevel.Debug);
                npcHats.Add(npcName, hatName);
            }
        }
        
        /// <summary>
        /// Check if an NPC has a hat
        /// </summary>
        public bool NPCHasHat(string npcName)
        {
            return npcHats.ContainsKey(npcName);
        }
        
        /// <summary>
        /// Get the hat name for an NPC
        /// </summary>
        public string GetNPCHat(string npcName)
        {
            if (npcHats.ContainsKey(npcName))
                return npcHats[npcName];
            return null;
        }
        
        /// <summary>
        /// Remove hat from an NPC
        /// </summary>
        public void RemoveHatFromNPC(string npcName)
        {
            if (npcHats.ContainsKey(npcName))
            {
                npcHats.Remove(npcName);
                monitor.Log($"Removed hat from {npcName}", LogLevel.Debug);
            }
        }
    }
}