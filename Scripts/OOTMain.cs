// Project:         Overhauled Overworld Travel mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2023 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    8/3/2023, 8:40 PM
// Last Edit:		10/22/2023, 9:50 PM
// Version:			1.00
// Special Thanks:  
// Modifier:

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System;
using Wenzil.Console;

namespace OverhauledOverworldTravel
{
    public partial class OOTMain : MonoBehaviour
    {
        public static OOTMain Instance;
        public static OOTSaveData ModSaveData = new OOTSaveData();

        static Mod mod;

        // Options
        //public static bool TogglePotionsGlassBottles { get; set; }

        // Mod Compatibility Check Values
        public static bool ClimatesAndCaloriesCheck { get; set; }

        // Global Variables
        int[,] exploredPixelArray = new int[1000, 500];
        public int[,] ExploredPixelValues { get { return exploredPixelArray; } set { exploredPixelArray = value; } }
        public static PlayerEntity Player { get { return GameManager.Instance.PlayerEntity; } }

        // Mod Textures || GUI
        public Texture2D PrimaryWorldMapTexture;
        public Texture2D BackgroundMapFillerTexture;
        public Texture2D WorldHeightMapTexture;
        public Texture2D RegionBordersMapTexture;
        public Texture2D RegionBitmapColorTexture;
        public Texture2D UnexploredMapOverlayTexture;

        public Texture2D BorderToggleButtonTexture;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<OOTMain>(); // Add script to the scene.

            mod.LoadSettingsCallback = LoadSettings; // To enable use of the "live settings changes" feature in-game.

            mod.IsReady = true;
        }

        private void Start()
        {
            Debug.Log("Begin mod init: Overhauled Overworld Travel");

            Instance = this;

            mod.SaveDataInterface = ModSaveData;

            mod.LoadSettings();

            ModCompatibilityChecking();
            
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelMap, typeof(OOTMapWindow));

            // Load Resources
            LoadTextures();
            //LoadAudio();

            RegisterOOTCommands();

            // Fill "fog of war" tracking array with empty values initially
            for (int x = 0; x < 1000; x++)
            {
                for (int y = 0; y < 500; y++)
                {
                    exploredPixelArray[x, y] = 0; // Set the initial value for each pixel
                }
            }

            Debug.Log("Finished mod init: Overhauled Overworld Travel");
        }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            //TogglePotionsGlassBottles = mod.GetSettings().GetValue<bool>("ToggleInteractables", "PotionsPoisonsAlcoholGlass-Bottles");
        }

        private void ModCompatibilityChecking()
        {
            /*Mod climatesAndCalories = ModManager.Instance.GetMod("Climates & Calories");
            ClimatesAndCaloriesCheck = climatesAndCalories != null ? true : false;*/
        }

        private void LoadTextures() // Example taken from Penwick Papers Mod
        {
            ModManager modManager = ModManager.Instance;
            bool success = true;

            success &= modManager.TryGetAsset("320x160_World_Map_Base", false, out PrimaryWorldMapTexture);
            success &= modManager.TryGetAsset("320x200_Background_Filler", false, out BackgroundMapFillerTexture);
            success &= modManager.TryGetAsset("1000x500_World_Height-Map", false, out WorldHeightMapTexture);
            success &= modManager.TryGetAsset("1000x500_Region_Borders_Map", false, out RegionBordersMapTexture);
            success &= modManager.TryGetAsset("1000x500_Region_Bitmap_Colors", false, out RegionBitmapColorTexture);
            success &= modManager.TryGetAsset("1000x500_Unexplored_Map_Overlay", false, out UnexploredMapOverlayTexture);
            success &= modManager.TryGetAsset("Concept_Borders_Toggle_Icon_1", false, out BorderToggleButtonTexture);

            if (!success)
                throw new Exception("Overhauled Overworld Travel: Missing texture asset");
        }

        public static void RegisterOOTCommands()
        {
            Debug.Log("[OverhauledOverworldTravel] Trying to register console commands.");
            try
            {
                ConsoleCommandsDatabase.RegisterCommand(ShowOOTWorldMapWindow.command, ShowOOTWorldMapWindow.description, ShowOOTWorldMapWindow.usage, ShowOOTWorldMapWindow.Execute);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Error Registering OverhauledOverworldTravel Console commands: {0}", e.Message));
            }
        }

        private static class ShowOOTWorldMapWindow
        {
            public static readonly string command = "showmap";
            public static readonly string description = "Shows the custom Overhauled Overworld Travel Map Window.)";
            public static readonly string usage = "showmap";

            public static string Execute(params string[] args)
            {
                NewOOTMapWindow ootWorldMapWindow;

                ootWorldMapWindow = new NewOOTMapWindow(DaggerfallUI.UIManager);
                DaggerfallUI.UIManager.PushWindow(ootWorldMapWindow);
                return "Complete";
            }
        }
    }
}
