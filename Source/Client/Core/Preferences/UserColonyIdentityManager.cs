using GameClient.Files;
using Shared;
using System;

namespace GameClient.Core.Preferences
{
    /// <summary>
    /// Manages user colony identity to ensure colonies are tied to usernames rather than device-specific IDs
    /// </summary>
    public static class UserColonyIdentityManager
    {
        /// <summary>
        /// Generates a consistent UID based on username
        /// This ensures the same username always gets the same UID across different machines
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
