using GameClient.Files;
using GameClient.Misc;
using Shared;
using System;
using Steamworks;

namespace GameClient.Core.Preferences
{
    /// <summary>
    /// Manages user colony identity to ensure colonies are tied to usernames rather than device-specific IDs
    /// </summary>
    public static class UserColonyIdentityManager
    {
        /// <summary>
        /// Generates a consistent UID based on username and Steam ID
        /// This ensures the same username + Steam ID combination always gets the same UID across different machines
        /// </summary>
        /// <param name="username">The username to generate UID for</param>
        /// <returns>A consistent UID based on the username and Steam ID</returns>
        public static string GenerateConsistentUID(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            // Get Steam ID if available
            string steamIdString = GetSteamIdString();
            
            // If Steam ID is available, use username + Steam ID combination
            if (!string.IsNullOrEmpty(steamIdString))
            {
                string combinedIdentifier = $"{username.ToLowerInvariant()}_{steamIdString}";
                string combinedHash = Hasher.GetHashFromString(combinedIdentifier);
                return combinedHash.Substring(0, 16);
            }
            else
            {
                // Fall back to the old method (time-based UID) if Steam ID is not available
                TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
                return Hasher.GetHashFromString(timeSpan.TotalMilliseconds.ToString()).Substring(0, 16);
            }
        }

        /// <summary>
        /// Gets the Steam ID as a string, or a fallback identifier if Steam is not available
        /// </summary>
        /// <returns>Steam ID string or fallback identifier</returns>
        private static string GetSteamIdString()
        {
            try
            {
                // Try to get Steam ID if Steam is initialized and available
                if (SteamAPI.IsSteamRunning())
                {
                    CSteamID steamId = SteamUser.GetSteamID();
                    if (steamId.IsValid())
                    {
                        return steamId.m_SteamID.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - fall back to machine identifier
                Printer.Warning($"Failed to get Steam ID: {ex.Message}");
            }

            // Fallback to empty string if Steam ID is not available
            // This means only username will be used for UID generation
            return string.Empty;
        }

        /// <summary>
        /// Updates the login data file to use username-based UID if needed
        /// </summary>
        /// <param name="file">The login data file to update</param>
        /// <returns>True if the UID was updated, false if no change was needed</returns>
        public static bool UpdateUIDIfNeeded(LoginDataFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Username))
                return false;

            string expectedUID = GenerateConsistentUID(file.Username);
            
            if (file.UID != expectedUID)
            {
                file.UID = expectedUID;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Migrates an existing user to use username-based UID
        /// This is called when a username is assigned to ensure consistency
        /// </summary>
        /// <param name="username">The username to migrate to</param>
        public static void MigrateToUsernameBasedUID(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            LoginDataFile file = UserLoginHandler.LoadLoginData();
            string newUID = GenerateConsistentUID(username);
            
            // Only update if the UID is different
            if (file.UID != newUID)
            {
                file.UID = newUID;
                UserLoginHandler.SaveLoginData(file);
            }
        }
    }
}
