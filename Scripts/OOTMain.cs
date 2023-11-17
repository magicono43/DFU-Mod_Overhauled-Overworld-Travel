// Project:         Overhauled Overworld Travel mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2023 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    8/3/2023, 8:40 PM
// Last Edit:		11/17/2023, 6:50 PM
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
using DaggerfallWorkshop;
using DaggerfallConnect.Utility;
using System.Collections.Generic;

namespace OverhauledOverworldTravel
{
    public partial class OOTMain : MonoBehaviour
    {
        public static OOTMain Instance;
        public static OOTSaveData ModSaveData = new OOTSaveData();

        static Mod mod;

        // Options
        public static int ViewRadiusValue { get; set; }

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

            go.AddComponent<OOTWanderingEncounterAI>();

            mod.LoadSettingsCallback = LoadSettings; // To enable use of the "live settings changes" feature in-game.

            mod.IsReady = true;
        }

        public string timerText = "00:00.00";
        public string storedTime = "00:00.00";
        public string facingAngle = "0\u00B0";
        private float elapsedTime = 0;

        void Update()
        {
            elapsedTime += Time.deltaTime;
            UpdatePlayerFacingAngle();
        }

        string UpdateTimerText()
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            float milliseconds = (elapsedTime % 1) * 100;
            return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }

        public void ResetTimer()
        {
            elapsedTime = 0;
            UpdateTimerText();
        }

        public void UpdatePlayerFacingAngle()
        {
            float rotation = GameManager.Instance.MainCamera.transform.eulerAngles.y;
            facingAngle = rotation + "\u00B0";
        }

        void OnGUI()
        {
            if (Event.current.type.Equals(EventType.Repaint))
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                style.fontSize = 40;
                string text = UpdateTimerText();
                GUI.Label(new Rect(735, 80, 500, 24), text, style);
                GUI.Label(new Rect(735, 122, 500, 24), storedTime, style);
                GUI.Label(new Rect(735, 164, 500, 24), facingAngle, style);
            }
        }

        private void Start()
        {
            Debug.Log("Begin mod init: Overhauled Overworld Travel");

            Instance = this;

            DaggerfallWorkshop.PlayerGPS.OnMapPixelChanged += WhenMapPixelChanges; // This is just for testing.

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

        // This is just for testing.
        public void WhenMapPixelChanges(DaggerfallConnect.Utility.DFPosition mapPixel)
        {
            storedTime = UpdateTimerText();
            ResetTimer();
        }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            ViewRadiusValue = mod.GetSettings().GetValue<int>("Testing", "ViewRangeRadius");
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

        public static bool CoinFlip()
        {
            if (UnityEngine.Random.Range(0, 1 + 1) == 0)
                return false;
            else
                return true;
        }

        public static void CreateWanderingEncounterObjectAI(DFPosition dfPos)
        {
            ulong loadID = DaggerfallUnity.NextUID;
            string encounterName = "OOT_Wandering_Encounter-" + loadID;
            GameObject go = new GameObject(encounterName);

            if (Instance != null)
                go.transform.parent = Instance.transform;
            else
                return;

            OOTWanderingEncounterAI encounterObj = go.AddComponent<OOTWanderingEncounterAI>();
            encounterObj.LoadID = loadID;
            encounterObj.EncounterName = encounterName;
            encounterObj.PreviousEncounterPosition = dfPos;
            encounterObj.EncounterDestination = GetEncounterDestinationPos(dfPos);

            NewOOTMapWindow.Instance.WanderingEncountersList.Add(go);
        }

        public static DFPosition GetEncounterDestinationPos(DFPosition startPos)
        {
            int startX = startPos.X;
            int startY = startPos.Y;
            //int radius = OOTMain.ViewRadiusValue; // For testing
            int radius = 40;
            int width = 1000;
            int height = 500;

            byte[,] mapCanvas2D = new byte[width, height];
            // Determine what pixels within the area of a square around the player's current position are "valid" for wandering encounters to be created on.
            for (int y = startY - radius; y <= startY + radius; y++)
            {
                for (int x = startX - radius; x <= startX + radius; x++)
                {
                    // Ensure the x and y coordinates are within the pixel buffer bounds
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        if ((ClimateType)NewOOTMapWindow.Instance.UsableClimateMapValues[x, y] != ClimateType.Ocean_Water)
                        {
                            mapCanvas2D[x, y] = 1;
                        }
                    }
                }
            }

            List<DFPosition> validPosList = new List<DFPosition>();
            // Fill a list with all the valid DFPositions, that being any marked with a "1"
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (mapCanvas2D[x, y] == 1)
                    {
                        DFPosition dfPos = new DFPosition(x, y);
                        validPosList.Add(dfPos);
                    }
                }
            }

            int randIndex = UnityEngine.Random.Range(0, validPosList.Count);
            return new DFPosition(validPosList[randIndex].X, validPosList[randIndex].Y);
        }
    }
}
