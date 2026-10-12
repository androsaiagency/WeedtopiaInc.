using System;
using System.Linq;

namespace Weedtopia.Domain
{
    /// <summary>Profile rules shared by tutorial, settings and save validation.</summary>
    public static class PlayerProfile
    {
        public const string DefaultAvatar = "player_male_01";
        public static readonly string[] Avatars = { DefaultAvatar, "player_male_02", "player_male_03", "player_male_04", "player_male_05", "player_female_01", "player_female_02", "player_female_03", "player_female_04", "player_female_05" };
        // Only historical save loading accepts resident portraits. New profile choices use their own catalog.
        public static string MigrateAvatar(string value)
        {
            if (string.IsNullOrEmpty(value)) return DefaultAvatar;
            switch (value)
            {
                case "rocko": return DefaultAvatar;
                case "mila": return "player_female_01";
                case "jaro": return "player_male_02";
                case "nia": return "player_female_03";
                case "city_host": return "player_female_02";
                case "gardener": return "player_male_03";
                case "baker": return "player_female_04";
                case "driver": return "player_male_04";
                default: return value;
            }
        }
        public static bool ValidName(string value)
        {
            value = (value ?? "").Trim();
            return value.Length >= 2 && value.Length <= 24 && !value.Any(c => char.IsControl(c) || c == '<' || c == '>');
        }
        public static bool ValidAvatar(string value) => Avatars.Contains(value);
    }
}
