// Project:         Overhauled Overworld Travel mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2023 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    8/3/2023, 8:40 PM
// Last Edit:		8/9/2023, 12:30 AM
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

namespace OverhauledOverworldTravel
{
    public partial class OOTMain : MonoBehaviour
    {
        public static OOTMain Instance;
        //public static OOTSaveData ModSaveData = new OOTSaveData();

        static Mod mod;

        // Options
        //public static bool TogglePotionsGlassBottles { get; set; }

        // Mod Compatibility Check Values
        public static bool ClimatesAndCaloriesCheck { get; set; }

        // Global Variables
        public static PlayerEntity Player { get { return GameManager.Instance.PlayerEntity; } }

        // Mod Textures || GUI
        public Texture2D GrabModeChoiceMenuTexture;

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

            //mod.SaveDataInterface = ModSaveData;

            mod.LoadSettings();

            ModCompatibilityChecking();
			
			UIWindowFactory.RegisterCustomUIWindow(UIWindowType.TravelMap, typeof(OOTMapWindow));

            // Load Resources
            LoadTextures();
            //LoadAudio();

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
            /*ModManager modManager = ModManager.Instance;
            bool success = true;

            success &= modManager.TryGetAsset("Grab-Mode_Choice_Menu", false, out GrabModeChoiceMenuTexture);

            if (!success)
                throw new Exception("Overhauled Overworld Travel: Missing texture asset");*/
        }
    }
}
