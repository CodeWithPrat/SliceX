using SliceX.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SliceX.Utilities
{
    public static class ProfileManager
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static List<string> defaultProfiles = new List<string>
        {
            "default", "3dlnk_Acrylic", "HT", "PMMA"
        };

        public static List<string> GetAvailableProfiles()
        {
            return defaultProfiles;
        }

        public static void SaveProfile(PrinterSettings settings, string filePath)
        {
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(filePath, json);
        }

        public static PrinterSettings? LoadProfile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PrinterSettings>(json, options);
        }

        public static void CreateProfile(string profileName)
        {
            if (!defaultProfiles.Contains(profileName))
            {
                defaultProfiles.Add(profileName);
            }
        }

        public static void DeleteProfile(string profileName)
        {
            if (defaultProfiles.Contains(profileName) && profileName != "default")
            {
                defaultProfiles.Remove(profileName);
            }
        }
    }
}