using System.Collections.Generic;
using SevenCrowns.Systems;

namespace SevenCrowns.Systems.Cities
{
    /// <summary>
    /// Cross-scene transfer container used when entering the City scene.
    /// Stores wallet amounts, population available, and world date to seed the City HUD/services.
    /// </summary>
    public static class CityEnterTransfer
    {
        private static Dictionary<string, int> s_Wallet;
        private static int s_Population;
        private static bool s_HasPopulation;
        private static WorldDate s_Date;
        private static bool s_HasDate;
        private static string s_CityId;
        private static string s_FactionId;
        private static bool s_HasCity;
        private static string s_CityNameKey;
        private static bool s_HasCityNameKey;

        public static void SetWalletSnapshot(Dictionary<string, int> amounts)
        {
            s_Wallet = amounts != null ? new Dictionary<string, int>(amounts) : null;
        }

        public static void SetPopulation(int available)
        {
            s_Population = available;
            s_HasPopulation = true;
        }

        public static void SetWorldDate(WorldDate date)
        {
            s_Date = date;
            s_HasDate = true;
        }

        public static void SetCityContext(string cityId, string factionId)
        {
            s_CityId = string.IsNullOrWhiteSpace(cityId) ? string.Empty : cityId.Trim();
            s_FactionId = string.IsNullOrWhiteSpace(factionId) ? string.Empty : factionId.Trim();
            s_HasCity = true;
        }

        /// <summary>
        /// Overload including a city name key (for localization). Use when available.
        /// </summary>
        public static void SetCityContext(string cityId, string factionId, string cityNameKey)
        {
            SetCityContext(cityId, factionId);
            SetCityNameKey(cityNameKey);
        }

        /// <summary>
        /// Sets a city name key (string table entry key). Optional.
        /// </summary>
        public static void SetCityNameKey(string cityNameKey)
        {
            s_CityNameKey = string.IsNullOrWhiteSpace(cityNameKey) ? string.Empty : cityNameKey.Trim();
            s_HasCityNameKey = !string.IsNullOrEmpty(s_CityNameKey);
        }

        public static bool TryConsumeWallet(out Dictionary<string, int> amounts)
        {
            amounts = s_Wallet;
            s_Wallet = null;
            return amounts != null;
        }

        public static bool TryConsumePopulation(out int available)
        {
            available = s_Population;
            bool had = s_HasPopulation;
            s_HasPopulation = false;
            return had;
        }

        public static bool TryConsumeWorldDate(out WorldDate date)
        {
            date = s_Date;
            bool had = s_HasDate;
            s_HasDate = false;
            return had;
        }

        public static bool TryConsumeCityContext(out string cityId, out string factionId)
        {
            cityId = s_CityId;
            factionId = s_FactionId;
            bool had = s_HasCity;
            s_HasCity = false;
            s_CityId = null;
            s_FactionId = null;
            return had;
        }

        /// <summary>
        /// Non-destructive peek of the last provided city context.
        /// Returns false when no city context was set.
        /// </summary>
        public static bool TryPeekCityContext(out string cityId, out string factionId)
        {
            cityId = s_CityId;
            factionId = s_FactionId;
            return s_HasCity;
        }

        /// <summary>
        /// Consumes and clears the city name key if present.
        /// </summary>
        public static bool TryConsumeCityNameKey(out string cityNameKey)
        {
            cityNameKey = s_CityNameKey;
            bool had = s_HasCityNameKey;
            s_CityNameKey = null;
            s_HasCityNameKey = false;
            return had;
        }

        /// <summary>
        /// Non-destructive peek of the city name key.
        /// </summary>
        public static bool TryPeekCityNameKey(out string cityNameKey)
        {
            cityNameKey = s_CityNameKey;
            return s_HasCityNameKey && !string.IsNullOrEmpty(cityNameKey);
        }
    }
}
