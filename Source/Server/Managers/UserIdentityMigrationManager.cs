using GameServer.Core;
using GameServer.Misc;
using Shared;
using Shared.Files;
using System;
using System.IO;
using System.Linq;
using TCPNetwork.Server;

namespace GameServer.Managers
{
    /// <summary>
    /// Manages server-side user identity migration to support username-based colony identification
    /// </summary>
    public static class UserIdentityMigrationManager
    {
        /// <summary>
        /// Generates a consistent UID based on username (same logic as client)
        /// </summary>
        /// <param name="username">The username to generate UID for</param>
        /// <returns>A consistent UID based on the username</returns>
        public static string GenerateConsistentUID(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            // Generate a consistent hash based on the username
            // This ensures the same username always produces the same UID
            string usernameHash = Hasher.GetHashFromString(username.ToLowerInvariant());
            
            // Take first 16 characters to match existing UID format
            return usernameHash.Substring(0, 16);
        }

        /// <summary>
        /// Checks if a user file exists for the username-based UID
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns>True if a user file exists for the username-based UID</returns>
        public static bool DoesUsernameBasedUserFileExist(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            string expectedUID = GenerateConsistentUID(username);
            string userFilePath = Path.Combine(Master.UsersPath, expectedUID + UserManagerH.fileExtension);
            return File.Exists(userFilePath);
        }

        /// <summary>
        /// Attempts to find an existing user file that matches the username but has a different UID
        /// This helps identify users who might be logging in from a new device
        /// </summary>
        /// <param name="username">The username to search for</param>
        /// <param name="currentUID">The current UID being used</param>
        /// <returns>The existing UserFile if found, null otherwise</returns>
        public static UserFile FindExistingUserFileByUsername(string username, string currentUID)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var allUserFiles = UserManagerH.GetAllUserFiles();
            
            // Look for a user file with the same username but different UID
            return allUserFiles.FirstOrDefault(user => 
                user.Label.Equals(username, StringComparison.OrdinalIgnoreCase) && 
                user.Uid != currentUID);
        }

        /// <summary>
        /// Migrates user data from old UID to new username-based UID
        /// This includes user file, settlements, sites, and saves
        /// </summary>
        /// <param name="oldUID">The old UID to migrate from</param>
        /// <param name="newUID">The new username-based UID to migrate to</param>
        /// <param name="username">The username for logging purposes</param>
        public static void MigrateUserData(string oldUID, string newUID, string username)
        {
            if (string.IsNullOrWhiteSpace(oldUID) || string.IsNullOrWhiteSpace(newUID) || oldUID == newUID)
                return;

            try
            {
                // Migrate user file
                MigrateUserFile(oldUID, newUID);
                
                // Migrate settlements
                MigrateSettlements(oldUID, newUID);
                
                // Migrate sites
                MigrateSites(oldUID, newUID);
                
                // Migrate save file
                MigrateSaveFile(oldUID, newUID);
                
                Printer.Warning($"Successfully migrated user data for '{username}' from UID '{oldUID}' to '{newUID}'");
            }
            catch (Exception ex)
            {
                Printer.Error($"Failed to migrate user data for '{username}': {ex.Message}");
            }
        }

        private static void MigrateUserFile(string oldUID, string newUID)
        {
            string oldPath = Path.Combine(Master.UsersPath, oldUID + UserManagerH.fileExtension);
            string newPath = Path.Combine(Master.UsersPath, newUID + UserManagerH.fileExtension);
            
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                UserFile userFile = Serializer.SerializeFromFile<UserFile>(oldPath);
                userFile.Uid = newUID; // Update the UID in the file
                Serializer.SerializeToFile(newPath, userFile);
                File.Delete(oldPath);
            }
        }

        private static void MigrateSettlements(string oldUID, string newUID)
        {
            var settlements = SettlementManager.GetAllSettlementsFromUsername(oldUID);
            foreach (var settlement in settlements)
            {
                settlement.UID = newUID;
                string filePath = Path.Combine(Master.SettlementsPath, settlement.Tile + SettlementManager.fileExtension);
                Serializer.SerializeToFile(filePath, settlement);
            }
        }

        private static void MigrateSites(string oldUID, string newUID)
        {
            var sites = SiteManagerHelper.GetAllSitesFromUID(oldUID);
            foreach (var site in sites)
            {
                site.UID = newUID;
                string filePath = Path.Combine(Master.SitesPath, site.Tile + SiteManagerHelper.fileExtension);
                Serializer.SerializeToFile(filePath, site);
            }
        }

        private static void MigrateSaveFile(string oldUID, string newUID)
        {
            string oldPath = Path.Combine(Master.SavesPath, oldUID + SaveManager.fileExtension);
            string newPath = Path.Combine(Master.SavesPath, newUID + SaveManager.fileExtension);
            
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                File.Move(oldPath, newPath);
            }
        }
    }
}
