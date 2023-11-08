using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Items;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using System.Linq;
using DaggerfallWorkshop.Game.Weather;

namespace OverhauledOverworldTravel
{
    public partial class OOTMain
    {
        // Base odds to change weather based on season and current type of weather: The 4 Rows represent seasons, the currently 11 Columns represent this mod's defined weather types.
        public static byte[,] baseChangeOdds = new byte[,]
        {
            {35, 55, 65, 90, 75, 35, 75, 45, 85, 60, 90},
            {20, 35, 45, 65, 90, 70, 90, 55, 90, 70, 90},
            {60, 45, 55, 65, 90, 50, 45, 75, 65, 90, 65},
            {45, 40, 45, 90, 75, 55, 30, 65, 75, 90, 50}
        };

        // Creates new instance of "DaggerfallDateTime" object, which is then set to the provided time in seconds, finally returning what season it currently is, based on that input time.
        public static DaggerfallDateTime.Seasons GetSeasonFromDFSeconds(ulong dateTimeInSeconds)
        {
            DaggerfallDateTime dfDateTime = new DaggerfallDateTime();
            dfDateTime.FromSeconds(dateTimeInSeconds);

            return dfDateTime.SeasonValue;
        }

        public static OOTWeatherType RollForWeatherChange(OOTWeatherType weather, ClimateType climate, DaggerfallDateTime.Seasons season, ref int unchangedCounter)
        {
            byte seasonIndex = SeasonToNumber(season);
            byte weatherIndex = (byte)weather;
            byte baseOdds = baseChangeOdds[seasonIndex, weatherIndex];

            if (Dice100.SuccessRoll(baseOdds + (unchangedCounter * UnityEngine.Random.Range(5, 21))))
            {
                unchangedCounter = 0;

                if (CoinFlip())
                {
                    return ImproveWeather(weather);
                }
                else
                {
                    return WorsenWeather(weather, climate, season);
                }
            }

            ++unchangedCounter;
            return weather;
        }

        public static OOTWeatherType ImproveWeather(OOTWeatherType weather)
        {
            switch (weather)
            {
                case OOTWeatherType.Sunny:
                case OOTWeatherType.Cloudy:
                default: return OOTWeatherType.Sunny;
                case OOTWeatherType.Overcast: return OOTWeatherType.Cloudy;
                case OOTWeatherType.Fog:
                case OOTWeatherType.Rain:
                case OOTWeatherType.Snow: if (CoinFlip()) { return OOTWeatherType.Overcast; } else { return OOTWeatherType.Cloudy; }
                case OOTWeatherType.Sandstorm: if (CoinFlip()) { return OOTWeatherType.Sunny; } else { return OOTWeatherType.Cloudy; }
                case OOTWeatherType.Thunderstorm:
                case OOTWeatherType.Hail: if (CoinFlip()) { return OOTWeatherType.Rain; } else { return OOTWeatherType.Overcast; }
                case OOTWeatherType.Typhoon: if (Dice100.SuccessRoll(20)) { return OOTWeatherType.Overcast; } else if (CoinFlip()) { return OOTWeatherType.Thunderstorm; } else { return OOTWeatherType.Rain; }
                case OOTWeatherType.Blizzard: if (Dice100.SuccessRoll(20)) { return OOTWeatherType.Hail; } else if (CoinFlip()) { return OOTWeatherType.Overcast; } else { return OOTWeatherType.Snow; }
            }
        }

        public static OOTWeatherType WorsenWeather(OOTWeatherType weather, ClimateType climate, DaggerfallDateTime.Seasons season)
        {
            OOTWeatherType[] wTypes = { OOTWeatherType.Fog, OOTWeatherType.Sandstorm, OOTWeatherType.Rain, OOTWeatherType.Snow, OOTWeatherType.Thunderstorm, OOTWeatherType.Hail, OOTWeatherType.Typhoon, OOTWeatherType.Blizzard };
            byte[] odds = { 0, 0, 0, 0, 0, 0, 0, 0};
            byte roll = (byte)UnityEngine.Random.Range(0, 101);

            switch (season)
            {
                case DaggerfallDateTime.Seasons.Spring:
                default:
                    if (IsDesert(climate)) { odds = new byte[] { 15, 5, 50, 0, 25, 0, 5, 0 }; }
                    else if (IsWarmClimate(climate)) { odds = new byte[] { 10, 0, 55, 0, 20, 0, 15, 0 }; }
                    else if (IsColdClimate(climate)) { odds = new byte[] { 20, 0, 45, 5, 20, 10, 0, 0 }; }
                    else if (IsHauntedClimate(climate)) { odds = new byte[] { 40, 0, 25, 0, 30, 5, 0, 0 }; }
                    else { odds = new byte[] { 20, 0, 50, 0, 20, 5, 5, 0 }; } break;
                case DaggerfallDateTime.Seasons.Summer:
                    if (IsDesert(climate)) { odds = new byte[] { 0, 20, 40, 0, 30, 0, 10, 0 }; }
                    else if (IsWarmClimate(climate)) { odds = new byte[] { 5, 0, 45, 0, 30, 0, 20, 0 }; }
                    else if (IsColdClimate(climate)) { odds = new byte[] { 10, 0, 50, 0, 25, 15, 0, 0 }; }
                    else if (IsHauntedClimate(climate)) { odds = new byte[] { 25, 0, 25, 0, 40, 10, 0, 0 }; }
                    else { odds = new byte[] { 10, 0, 40, 0, 30, 10, 10, 0 }; } break;
                case DaggerfallDateTime.Seasons.Fall:
                    if (IsDesert(climate)) { odds = new byte[] { 0, 15, 45, 5, 25, 5, 5, 0 }; }
                    else if (IsWarmClimate(climate)) { odds = new byte[] { 5, 0, 55, 0, 25, 5, 10, 0 }; }
                    else if (IsColdClimate(climate)) { odds = new byte[] { 5, 0, 25, 35, 15, 15, 0, 5 }; }
                    else if (IsHauntedClimate(climate)) { odds = new byte[] { 20, 0, 15, 15, 25, 15, 0, 10 }; }
                    else { odds = new byte[] { 10, 0, 35, 20, 15, 15, 0, 5 }; } break;
                case DaggerfallDateTime.Seasons.Winter:
                    if (IsDesert(climate)) { odds = new byte[] { 10, 5, 35, 10, 25, 15, 0, 0 }; }
                    else if (IsWarmClimate(climate)) { odds = new byte[] { 10, 0, 45, 0, 30, 10, 5, 0 }; }
                    else if (IsColdClimate(climate)) { odds = new byte[] { 10, 0, 10, 40, 5, 20, 0, 15 }; }
                    else if (IsHauntedClimate(climate)) { odds = new byte[] { 25, 0, 5, 20, 15, 15, 0, 20 }; }
                    else { odds = new byte[] { 15, 0, 20, 30, 10, 15, 0, 10 }; } break;
            }

            switch (weather)
            {
                case OOTWeatherType.Sunny:
                default:
                    if (IsDesert(climate)) { if (roll <= odds[1]) { return OOTWeatherType.Sandstorm; } else { return OOTWeatherType.Cloudy; } }
                    else { return OOTWeatherType.Cloudy; }
                case OOTWeatherType.Cloudy:
                    if (IsDesert(climate)) { if (roll <= odds[1]) { return OOTWeatherType.Sandstorm; } else { return OOTWeatherType.Overcast; } }
                    else { if (Dice100.SuccessRoll(80)) { return OOTWeatherType.Overcast; } else { return RollWorsenedWeather(roll, odds, wTypes, weather); } }
                case OOTWeatherType.Overcast:
                    if (IsDesert(climate)) { if (roll <= odds[1]) { return OOTWeatherType.Sandstorm; } else { return RollWorsenedWeather(roll, odds, wTypes, weather); } }
                    else { return RollWorsenedWeather(roll, odds, wTypes, weather); }
                case OOTWeatherType.Rain:
                case OOTWeatherType.Snow:
                case OOTWeatherType.Fog:
                case OOTWeatherType.Sandstorm:
                    return RollWorsenedWeather(roll, odds, wTypes, weather);
                case OOTWeatherType.Thunderstorm:
                    if (Dice100.SuccessRoll(odds[5] * 2)) { return OOTWeatherType.Hail; } else if (Dice100.SuccessRoll(odds[6] * 2)) { return OOTWeatherType.Typhoon; } else { return OOTWeatherType.Thunderstorm; }
                case OOTWeatherType.Hail:
                    if (Dice100.SuccessRoll(odds[6] * 2)) { return OOTWeatherType.Typhoon; } else if (Dice100.SuccessRoll(odds[7] * 2)) { return OOTWeatherType.Blizzard; } else { return OOTWeatherType.Thunderstorm; }
                case OOTWeatherType.Typhoon:
                    if (Dice100.SuccessRoll(odds[6] * 2)) { return OOTWeatherType.Typhoon; } else if (Dice100.SuccessRoll(odds[4] * 3)) { return OOTWeatherType.Thunderstorm; } else { return OOTWeatherType.Rain; }
                case OOTWeatherType.Blizzard:
                    if (Dice100.SuccessRoll(odds[7] * 2)) { return OOTWeatherType.Blizzard; } else if (Dice100.SuccessRoll(odds[5] * 2)) { return OOTWeatherType.Hail; } else { return OOTWeatherType.Snow; }
            }
        }

        public static OOTWeatherType RollWorsenedWeather(byte roll, byte[] odds, OOTWeatherType[] wTypes, OOTWeatherType currWeather)
        {
            byte cumulativeProbability = 0;

            for (int i = 0; i < wTypes.Length; i++)
            {
                cumulativeProbability += odds[i];
                if (roll <= cumulativeProbability)
                {
                    return wTypes[i];
                }
            }
            return currWeather;
        }

        public static OOTWeatherType ConformWeatherToClimate(OOTWeatherType weather, ClimateType climate)
        {
            if (IsDesert(climate))
            {
                if (weather == OOTWeatherType.Snow) { return OOTWeatherType.Rain; }
                else if (weather == OOTWeatherType.Hail) { return OOTWeatherType.Thunderstorm; }
                else if (weather == OOTWeatherType.Blizzard) { return OOTWeatherType.Typhoon; }
            }
            else if (IsWarmClimate(climate))
            {
                if (weather == OOTWeatherType.Snow) { return OOTWeatherType.Rain; }
                else if (weather == OOTWeatherType.Hail) { return OOTWeatherType.Thunderstorm; }
                else if (weather == OOTWeatherType.Blizzard) { return OOTWeatherType.Typhoon; }
                else if (weather == OOTWeatherType.Sandstorm) { return OOTWeatherType.Fog; }
            }
            else if (IsColdClimate(climate) || IsHauntedClimate(climate))
            {
                if (weather == OOTWeatherType.Sandstorm) { return OOTWeatherType.Fog; }
            }
            else
            {
                if (weather == OOTWeatherType.Sandstorm) { return OOTWeatherType.Fog; }
            }
            return weather;
        }

        // Converts current map weather to an existing vanilla equivalent, then sets that as the current weather in-game.
        public static void SetRealWeather(OOTWeatherType weather)
        {
            WeatherManager weatherManager = GameManager.Instance.WeatherManager;

            switch (weather)
            {
                case OOTWeatherType.Sunny:
                default: weatherManager.SetWeather(WeatherType.Sunny); break;
                case OOTWeatherType.Cloudy: weatherManager.SetWeather(WeatherType.Cloudy); break;
                case OOTWeatherType.Overcast: weatherManager.SetWeather(WeatherType.Overcast); break;
                case OOTWeatherType.Sandstorm:
                case OOTWeatherType.Fog: weatherManager.SetWeather(WeatherType.Fog); break;
                case OOTWeatherType.Rain:
                case OOTWeatherType.Hail: weatherManager.SetWeather(WeatherType.Rain); break;
                case OOTWeatherType.Thunderstorm:
                case OOTWeatherType.Typhoon: weatherManager.SetWeather(WeatherType.Thunder); break;
                case OOTWeatherType.Snow:
                case OOTWeatherType.Blizzard: weatherManager.SetWeather(WeatherType.Snow); break;
            }
        }

        public static bool IsDesert(ClimateType climate)
        {
            if (climate == ClimateType.Desert_South || climate == ClimateType.Hot_Desert_South_East) { return true; }
            else { return false; }
        }

        public static bool IsWarmClimate(ClimateType climate)
        {
            if (climate == ClimateType.Rainforest || climate == ClimateType.Swamp || climate == ClimateType.SubTropical) { return true; }
            else { return false; }
        }

        public static bool IsColdClimate(ClimateType climate)
        {
            if (climate == ClimateType.Mountains || climate == ClimateType.Woodland_Hills) { return true; }
            else { return false; }
        }

        public static bool IsHauntedClimate(ClimateType climate)
        {
            if (climate == ClimateType.Haunted_Woodland) { return true; }
            else { return false; }
        }

        public static byte SeasonToNumber(DaggerfallDateTime.Seasons season)
        {
            switch (season)
            {
                case DaggerfallDateTime.Seasons.Spring:
                default:
                    return 0;
                case DaggerfallDateTime.Seasons.Summer: return 1;
                case DaggerfallDateTime.Seasons.Fall: return 2;
                case DaggerfallDateTime.Seasons.Winter: return 3;
            }
        }

        public static OOTWeatherType DetermineCurrentVanillaWeather()
        {
            WeatherManager weatherManager = GameManager.Instance.WeatherManager;

            if (weatherManager.IsSnowing)
                return OOTWeatherType.Snow;
            else if (weatherManager.IsStorming)
                return OOTWeatherType.Thunderstorm;
            else if (weatherManager.IsRaining)
                return OOTWeatherType.Rain;
            else if (weatherManager.IsOvercast && weatherManager.currentOutdoorFogSettings.density == weatherManager.HeavyFogSettings.density)
                return OOTWeatherType.Fog;
            else if (weatherManager.IsOvercast)
                return OOTWeatherType.Overcast;
            else
                return OOTWeatherType.Sunny;
        }

        public enum OOTWeatherType
        {
            Sunny,
            Cloudy,
            Overcast,
            Sandstorm,
            Fog,
            Rain,
            Snow,
            Thunderstorm,
            Hail,
            Typhoon,
            Blizzard,
        }

        /// <summary>
        /// All the vanilla Daggerfall climate types, assigned their associated byte data value.
        /// </summary>
        public enum ClimateType
        {
            Ocean_Water = 223,
            Desert_South = 224,
            Hot_Desert_South_East = 225,
            Mountains = 226,
            Rainforest = 227,
            Swamp = 228,
            SubTropical = 229,
            Woodland_Hills = 230,
            Temperate_Woodland = 231,
            Haunted_Woodland = 232,
        }
    }
}