using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;

namespace Hatstravaganza
{
    /// <summary>
    /// Manages which NPCs are wearing which hats and handles save/load
    /// </summary>
    public class HatManager
    {
        private IModHelper helper;
        private IMonitor monitor;
        
        // Dictionary: NPC name -> hat name
        private Dictionary<string, string> npcHats;
        
        // Save file name
        private const string SaveFileName = "npc-hats.json";
        
        public HatManager(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
            npcHats = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Assign a hat to an NPC and save immediately
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
        /// Remove hat from an NPC and save
        /// </summary>
        public void RemoveHatFromNPC(string npcName)
        {
            if (npcHats.ContainsKey(npcName))
            {
                npcHats.Remove(npcName);
                monitor.Log($"Removed hat from {npcName}", LogLevel.Debug);
            }
        }
        
        /// <summary>
        /// Save NPC hat states to JSON file
        /// </summary>
        public void SaveNPCHats()
        {
            try
            {
                // Get the save folder path specific to this save file
                string saveFolderPath = GetSaveFolder();
                
                if (saveFolderPath == null)
                {
                    monitor.Log("Cannot save - no save file loaded", LogLevel.Warn);
                    return;
                }
                
                // Create save folder if it doesn't exist
                Directory.CreateDirectory(saveFolderPath);
                
                // Full path to save file
                string filePath = Path.Combine(saveFolderPath, SaveFileName);
                
                // Serialize dictionary to JSON
                string json = JsonConvert.SerializeObject(npcHats, Formatting.Indented);
                
                // Write to file
                File.WriteAllText(filePath, json);
                
                monitor.Log($"Saved {npcHats.Count} NPC hat states to {filePath}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error saving NPC hats: {ex.Message}", LogLevel.Error);
            }
        }
        
        /// <summary>
        /// Load NPC hat states from JSON file
        /// </summary>
        public void LoadNPCHats()
        {
            try
            {
                // Get the save folder path
                string saveFolderPath = GetSaveFolder();
                
                if (saveFolderPath == null)
                {
                    monitor.Log("Cannot load - no save file loaded", LogLevel.Warn);
                    return;
                }
                
                // Full path to save file
                string filePath = Path.Combine(saveFolderPath, SaveFileName);
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    monitor.Log("No saved hat data found - starting fresh", LogLevel.Info);
                    npcHats = new Dictionary<string, string>();
                    return;
                }
                
                // Read file
                string json = File.ReadAllText(filePath);
                
                // Deserialize JSON to dictionary
                npcHats = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                
                if (npcHats == null)
                    npcHats = new Dictionary<string, string>();
                
                monitor.Log($"Loaded {npcHats.Count} NPC hat states from {filePath}", LogLevel.Info);
                
                // Log each loaded hat for debugging
                foreach (var kvp in npcHats)
                {
                    monitor.Log($"  {kvp.Key} is wearing {kvp.Value}", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error loading NPC hats: {ex.Message}", LogLevel.Error);
                npcHats = new Dictionary<string, string>();
            }
        }
        
        /// <summary>
        /// Get the save folder path for the current save file
        /// </summary>
        private string GetSaveFolder()
        {
            // Make sure a save is loaded
            if (!Context.IsWorldReady)
                return null;
            
            // Get the unique save ID
            string saveId = Constants.SaveFolderName;
            
            // Create path in mod's data folder, organized by save
            string modDataPath = Path.Combine(helper.DirectoryPath, "data", saveId);
            
            return modDataPath;
        }
        
        /// <summary>
        /// Clear all hat data (useful for testing)
        /// </summary>
        public void ClearAllHats()
        {
            npcHats.Clear();
            SaveNPCHats();
            monitor.Log("Cleared all NPC hats", LogLevel.Info);
        }
        
        /// <summary>
        /// Get all NPCs currently wearing hats
        /// </summary>
        public Dictionary<string, string> GetAllHattedNPCs()
        {
            return new Dictionary<string, string>(npcHats);
        }
    }
}