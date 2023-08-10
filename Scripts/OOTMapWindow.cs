using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.Utility;

namespace OverhauledOverworldTravel
{
    public class OOTMapWindow : DaggerfallTravelMapWindow
    {
        private const string MsgResume = "Resume your journey to {0}?";
        private const string MsgFollow = "Do you want to follow this road?";
        private const string MsgTeleportCost = "Teleportation will cost you {0} gold, is that acceptable?";
        private const int notEnoughGoldId = 454;

        // Path type and direction constants from BasicRoadsTexturing.
        //const int path_roads = TravelOptionsMod.path_roads;
        //const int path_tracks = TravelOptionsMod.path_tracks;
        //const int path_rivers = TravelOptionsMod.path_rivers;
        //const int path_streams = TravelOptionsMod.path_streams;

        public static Color32 roadColor = new Color32(60, 60, 60, 255);
        public static Color32 trackColor = new Color32(160, 118, 74, 255);
        public static Color32 redColor = new Color32(255, 0, 0, 255);
        public static Color32 blueColor = new Color32(0, 0, 255, 255);
        public static Color32 blackColor = new Color32(0, 0, 0, 255);
        public static Color32 whiteColor = new Color32(255, 255, 255, 255);

        public Vector2 buttonSize = new Vector2(47, 11);
        public Vector2 portsSize = new Vector2(45, 11);
        public Vector2 streamsSize = new Vector2(57, 11);

        const string roadsOffName = "roadsOff.png";
        const string roadsOnName = "roadsOn.png";
        const string tracksOffName = "tracksOff.png";
        const string tracksOnName = "tracksOn.png";
        Texture2D roadsOffTexture;
        Texture2D roadsOnTexture;
        Texture2D tracksOffTexture;
        Texture2D tracksOnTexture;

        protected Vector2 roadsButtonPos = new Vector2(1, 0);
        protected Vector2 tracksButtonPos = new Vector2(48, 0);

        static Color32 riverColor = new Color32(48, 79, 250, 255);
        static Color32 streamColor = new Color32(48, 120, 230, 255);

        const string riversOffName = "riversOff.png";
        const string riversOnName = "riversOn.png";
        const string streamsOffName = "streamsOff.png";
        const string streamsOnName = "streamsOn.png";
        Texture2D riversOffTexture;
        Texture2D riversOnTexture;
        Texture2D streamsOffTexture;
        Texture2D streamsOnTexture;

        protected Vector2 riversButtonPos = new Vector2(272, 0);
        protected Vector2 streamsButtonPos = new Vector2(215, 0);

        protected Rect pathsOverlayPanelRect = new Rect(0, regionPanelOffset, 320 * 5, 160 * 5);
        protected Panel pathsOverlayPanel;

        protected Button roadsButton;
        protected Button tracksButton;
        protected Button riversButton;
        protected Button streamsButton;

        const string portsOffName = "TOportsOff.png";
        const string portsOnName = "TOportsOn.png";
        Texture2D portsOffTexture;
        Texture2D portsOnTexture;

        protected Vector2 portFilterPos = new Vector2(231, 180);
        protected Vector2 portFilterMoved = new Vector2(231, 173);

        protected Vector2 horizArrowPos = new Vector2(231, 176);
        protected Vector2 horizArrowMoved = new Vector2(231, 184);

        protected Vector2 vertArrowPos = new Vector2(254, 176);
        protected Vector2 vertArrowMoved = new Vector2(254, 184);

        protected Button portsFilterButton;

        protected bool portsFilter = false;

        protected bool onlyLargeDots = false;

        protected bool teleportCharge = false;

        protected int markedLocationId = -1;

        internal static byte[][] pathsData = new byte[4][];
        protected bool[] showPaths = { true, true, false, false };

        internal bool LocationSelected { get { return locationSelected; } }

        internal DaggerfallMessageBox infoBox;

        protected Panel travelPathOverlayPanel;
        public Texture2D travelPathTexture;
        public Color32[] travelPathPixelBuffer;

        public PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

        public static DFPosition previousPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
        public static DFPosition nextPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
        public static DFPosition destinationPosition = TravelTimeCalculator.GetPlayerTravelPosition();

        public static List<DFPosition> currentTravelLinePositionsList = new List<DFPosition>();

        public static bool isPlayerTraveling = false;
        public static bool hasPlayerPositionChanged = false;

        int travelDelayTimer = 0;
        int startFastTravelTimer = 0;

        // Gives index to use with terrainMovementModifiers[]. Indexed by terrain type, starting with Ocean at index 0.
        // Also used for getting climate-related indices for dungeon textures.
        public static byte[] climateIndices = { 0, 0, 0, 1, 2, 3, 4, 5, 5, 5 };

        // Used in calculating travel cost
        int pixelsTraveledOnOcean = 0;

        DFPosition endPos = new DFPosition(109, 158);
        public DFPosition EndPos { get { return endPos; } protected internal set { endPos = value; } }

        // Hidden Map Locations mod data structures.
        protected HashSet<ContentReader.MapSummary> discoveredMapSummaries;
        protected HashSet<DFRegion.LocationTypes> revealedLocationTypes;

        public OOTMapWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
            /*if (TravelOptionsMod.Instance.RoadsIntegration)
            {
                // Try to get path data from BasicRoads mod
                ModManager.Instance.SendModMessage(TravelOptionsMod.ROADS_MODNAME, "getPathData", path_roads,
                    (string message, object data) => { pathsData[path_roads] = (byte[])data; });
                ModManager.Instance.SendModMessage(TravelOptionsMod.ROADS_MODNAME, "getPathData", path_tracks,
                    (string message, object data) => { pathsData[path_tracks] = (byte[])data; });
            }
            if (TravelOptionsMod.Instance.WaterwaysEnabled)
            {
                // Try to get waterways data from BasicRoads mod
                ModManager.Instance.SendModMessage(TravelOptionsMod.ROADS_MODNAME, "getPathData", path_rivers,
                    (string message, object data) => { pathsData[path_rivers] = (byte[])data; });
                ModManager.Instance.SendModMessage(TravelOptionsMod.ROADS_MODNAME, "getPathData", path_streams,
                    (string message, object data) => { pathsData[path_streams] = (byte[])data; });
                showPaths[path_rivers] = true;
                showPaths[path_streams] = true;
            }

            if (TravelOptionsMod.Instance.HiddenMapLocationsEnabled)
            {
                discoveredMapSummaries = new HashSet<ContentReader.MapSummary>();
                revealedLocationTypes = new HashSet<DFRegion.LocationTypes>();

                ModManager.Instance.SendModMessage(TravelOptionsMod.HIDDEN_MAP_LOCATIONS_MODNAME, "getRevealedLocationTypes", null,
                    (string message, object data) => { revealedLocationTypes = (HashSet<DFRegion.LocationTypes>)data; });
            }

            onlyLargeDots = !TravelOptionsMod.Instance.VariableSizeDots; */
        }

        protected override void Setup()
        {
            base.Setup();

            NativePanel.OnMiddleMouseClick += MarkLocationHandler;

            /*if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                // Port towns filter button
                if (!TextureReplacement.TryImportImage(portsOffName, true, out portsOffTexture))
                    return;
                if (!TextureReplacement.TryImportImage(portsOnName, true, out portsOnTexture))
                    return;

                portsFilterButton = new Button();
                portsFilterButton.Position = portFilterPos;
                portsFilterButton.Size = portsSize; //new Vector2(portsOffTexture.width, portsOffTexture.height);
                portsFilterButton.BackgroundTexture = portsOffTexture;
                portsFilterButton.OnMouseClick += PortsFilterButton_OnMouseClick;
                NativePanel.Components.Add(portsFilterButton);
            }*/

            if (true)
            {
                SetupPathButtons();
                UpdatePathButtons();

                /*if (TravelOptionsMod.Instance.WaterwaysEnabled)
                {
                    SetupWaterButtons();
                    UpdateWaterButtons();
                }*/

                // Start adding some basic function buttons here next I work on this, I suppose.

                locationDotsPixelBuffer = new Color32[(int)regionTextureOverlayPanelRect.width * (int)regionTextureOverlayPanelRect.height * 25];
                locationDotsTexture = new Texture2D((int)regionTextureOverlayPanelRect.width * 5, (int)regionTextureOverlayPanelRect.height * 5, TextureFormat.ARGB32, false);

                // Overlay for the player travel path panel
                travelPathOverlayPanel = DaggerfallUI.AddPanel(regionTextureOverlayPanelRect, NativePanel);
                travelPathOverlayPanel.Enabled = false;

                // Setup pixel buffer and texture for player travel path
                travelPathPixelBuffer = new Color32[(int)regionTextureOverlayPanelRect.width * (int)regionTextureOverlayPanelRect.height * 25];
                travelPathTexture = new Texture2D((int)regionTextureOverlayPanelRect.width * 5, (int)regionTextureOverlayPanelRect.height * 5, TextureFormat.ARGB32, false);
                travelPathTexture.filterMode = FilterMode.Point;
            }
        }

        private void PortsFilterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            portsFilter = !portsFilter;
            portsFilterButton.BackgroundTexture = portsFilter ? portsOnTexture : portsOffTexture;

            UpdateMapLocationDotsTexture();
        }

        protected override void SetupArrowButtons()
        {
            base.SetupArrowButtons();

            /*if (TravelOptionsMod.Instance.ShipTravelPortsOnly)
            {
                // Move the port filter button and arrow buttons if needed
                if (verticalArrowButton.Enabled || horizontalArrowButton.Enabled)
                {
                    portsFilterButton.Position = portFilterMoved;
                    horizontalArrowButton.Position = horizArrowMoved;
                    verticalArrowButton.Position = vertArrowMoved;
                }
                else
                {
                    portsFilterButton.Position = portFilterPos;
                    horizontalArrowButton.Position = horizArrowPos;
                    verticalArrowButton.Position = vertArrowPos;
                }
            }*/
        }

        protected void SetupPathButtons()
        {
            // Paths buttons
            if (!TextureReplacement.TryImportImage(roadsOffName, true, out roadsOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(roadsOnName, true, out roadsOnTexture))
                return;
            if (!TextureReplacement.TryImportImage(tracksOffName, true, out tracksOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(tracksOnName, true, out tracksOnTexture))
                return;

            roadsButton = new Button();
            //roadsButton.Tag = path_roads;
            roadsButton.Position = roadsButtonPos;
            roadsButton.Size = buttonSize;  //new Vector2(roadsOnTexture.width, roadsOnTexture.height);
            roadsButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(roadsButton);

            tracksButton = new Button();
            //tracksButton.Tag = path_tracks;
            tracksButton.Position = tracksButtonPos;
            tracksButton.Size = buttonSize; //new Vector2(tracksOnTexture.width, tracksOnTexture.height);
            tracksButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(tracksButton);
        }

        protected void SetupWaterButtons()
        {
            // Water buttons
            if (!TextureReplacement.TryImportImage(riversOffName, true, out riversOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(riversOnName, true, out riversOnTexture))
                return;
            if (!TextureReplacement.TryImportImage(streamsOffName, true, out streamsOffTexture))
                return;
            if (!TextureReplacement.TryImportImage(streamsOnName, true, out streamsOnTexture))
                return;

            riversButton = new Button();
            //riversButton.Tag = path_rivers;
            riversButton.Position = riversButtonPos;
            riversButton.Size = buttonSize;
            riversButton.BackgroundColor = Color.white;
            riversButton.OnMouseClick += PathTypeButton_OnMouseClick;
            NativePanel.Components.Add(riversButton);

            /*if (TravelOptionsMod.Instance.StreamsToggle)
            {
                streamsButton = new Button();
                streamsButton.Tag = path_streams;
                streamsButton.Position = streamsButtonPos;
                streamsButton.Size = streamsSize;
                streamsButton.BackgroundColor = Color.white;
                streamsButton.OnMouseClick += PathTypeButton_OnMouseClick;
                NativePanel.Components.Add(streamsButton);
            }*/
        }


        protected virtual void PathTypeButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            int pathType = (int)sender.Tag;
            /*if (pathType >= path_roads && pathType <= path_streams)
                showPaths[pathType] = !showPaths[pathType];

            if (pathType == path_rivers && !TravelOptionsMod.Instance.StreamsToggle)
                showPaths[path_streams] = showPaths[path_rivers];   // Streams follow rivers unless toggle enabled

            UpdatePathButtons();

            if (TravelOptionsMod.Instance.WaterwaysEnabled)
                UpdateWaterButtons();*/

            UpdateMapLocationDotsTexture();
        }

        private void UpdatePathButtons()
        {
            //roadsButton.BackgroundTexture = showPaths[path_roads] ? roadsOnTexture : roadsOffTexture;
            //tracksButton.BackgroundTexture = showPaths[path_tracks] ? tracksOnTexture : tracksOffTexture;
        }

        private void UpdateWaterButtons()
        {
            /*riversButton.BackgroundColorTexture = showPaths[path_rivers] ? riversOnTexture : riversOffTexture;
            if (TravelOptionsMod.Instance.StreamsToggle)
                streamsButton.BackgroundColorTexture = showPaths[path_streams] ? streamsOnTexture : streamsOffTexture;*/
        }

        public override void OnPush()
        {
            base.OnPush();

            previousPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
            nextPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
            destinationPosition = TravelTimeCalculator.GetPlayerTravelPosition();
            currentTravelLinePositionsList = new List<DFPosition>();
            isPlayerTraveling = false;
            hasPlayerPositionChanged = false;
        }

        public override void Update()
        {
            base.Update();

            if (!RegionSelected)
            {
                isPlayerTraveling = false;
                travelDelayTimer = 0;
                startFastTravelTimer = 0;
            }

            if ((previousPlayerPosition.Y != destinationPosition.Y) || (previousPlayerPosition.X != destinationPosition.X))
            {
                isPlayerTraveling = true;
            }
            else
            {
                isPlayerTraveling = false;
            }

            if (isPlayerTraveling)
            {
                ++travelDelayTimer;
                startFastTravelTimer = 0;

                if (travelDelayTimer >= 25)
                {
                    travelDelayTimer = 0;

                    /*DFPosition worldPos = MapsFile.MapPixelToWorldCoord(nextPlayerPosition.X, nextPlayerPosition.Y);
                    playerGPS.WorldX = worldPos.X;
                    playerGPS.WorldZ = worldPos.Y;
                    playerGPS.UpdateWorldInfo();*/

                    previousPlayerPosition.Y = nextPlayerPosition.Y;
                    previousPlayerPosition.X = nextPlayerPosition.X;
                    if (currentTravelLinePositionsList.Count > 0)
                    {
                        currentTravelLinePositionsList.RemoveAt(0);

                        if (currentTravelLinePositionsList.Count > 0)
                        {
                            nextPlayerPosition.Y = currentTravelLinePositionsList[0].Y;
                            nextPlayerPosition.X = currentTravelLinePositionsList[0].X;
                        }
                        else
                        {
                            nextPlayerPosition.Y = destinationPosition.Y;
                            nextPlayerPosition.X = destinationPosition.X;
                            previousPlayerPosition.Y = nextPlayerPosition.Y;
                            previousPlayerPosition.X = nextPlayerPosition.X;
                        }
                    }

                    UpdatePlayerTravelDotsTexture();
                }
            }
            else // Will eventually want a button of some kind to perform this, but for now just a delay when standing in the same spot for long enough on the map, for testing.
            {
                ++startFastTravelTimer;

                if (startFastTravelTimer >= 800)
                {
                    travelDelayTimer = 0;
                    startFastTravelTimer = 0;

                    DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();
                    performFastTravel();
                }
            }
        }

        // perform fast travel actions
        private void performFastTravel()
        {
            //RaiseOnPreFastTravelEvent(); // So for these events, I'm not sure how or if you can "piggy-back" off the existing ones from another class/window, really not sure how that might be done, so for now whatever.

            // Cache scene first, if fast travelling while on ship.
            if (GameManager.Instance.TransportManager.IsOnShip())
                DaggerfallWorkshop.Game.Serialization.SaveLoadManager.CacheScene(GameManager.Instance.StreamingWorld.SceneName);
            GameManager.Instance.StreamingWorld.RestoreWorldCompensationHeight(0);
            GameManager.Instance.StreamingWorld.TeleportToCoordinates((int)endPos.X, (int)endPos.Y, StreamingWorld.RepositionMethods.DirectionFromStartMarker);

            GameManager.Instance.PlayerEntity.CurrentHealth = GameManager.Instance.PlayerEntity.MaxHealth;
            GameManager.Instance.PlayerEntity.CurrentFatigue = GameManager.Instance.PlayerEntity.MaxFatigue;
            if (!GameManager.Instance.PlayerEntity.Career.NoRegenSpellPoints)
                GameManager.Instance.PlayerEntity.CurrentMagicka = GameManager.Instance.PlayerEntity.MaxMagicka;

            //DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(travelTimeTotalMins * 60);

            // Halt random enemy spawns for next playerEntity update so player isn't bombarded by spawned enemies at the end of a long trip
            GameManager.Instance.PlayerEntity.PreventEnemySpawns = true;

            // Vampires and characters with Damage from Sunlight disadvantage never arrive between 6am and 6pm regardless of travel type
            // Otherwise raise arrival time to just after 7am if cautious travel would arrive at night
            /*if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
            {
                if (DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsDay)
                {
                    DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.RaiseTime(
                        (DaggerfallDateTime.DuskHour - DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.Hour) * 3600);
                }
            }*/
            /*if (speedCautious)
            {
                if ((DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour < 7)
                    || ((DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour == 7) && (DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute < 10)))
                {
                    float raiseTime = (((7 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour) * 3600)
                                        + ((10 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute) * 60)
                                        - DaggerfallUnity.WorldTime.DaggerfallDateTime.Second);
                    DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(raiseTime);
                }
                else if (DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour > 17)
                {
                    float raiseTime = (((31 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour) * 3600)
                    + ((10 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute) * 60)
                    - DaggerfallUnity.WorldTime.DaggerfallDateTime.Second);
                    DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(raiseTime);
                }
            }*/

            DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            this.CloseTravelWindows(true);
            GameManager.Instance.PlayerEntity.RaiseSkills();
            DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack();

            //RaiseOnPostFastTravelEvent();
        }

        // Handle clicks on the main panel
        protected override void ClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            // If allowed handle clicks on map pixels without a location, allowing non-fast travel to any MP coords
            if (RegionSelected && !locationSelected && !MouseOverOtherRegion)
            {
                position.y -= regionPanelOffset;

                // Ensure clicks are inside region texture
                if (position.x < 0 || position.x > regionTextureOverlayPanelRect.width || position.y < 0 || position.y > regionTextureOverlayPanelRect.height)
                    return;

                EndPos = GetClickMPCoords();

                destinationPosition = EndPos;
                currentTravelLinePositionsList = FindPixelsBetweenPlayerAndDest(destinationPosition);
                if (currentTravelLinePositionsList.Count > 0)
                {
                    nextPlayerPosition.Y = currentTravelLinePositionsList[0].Y;
                    nextPlayerPosition.X = currentTravelLinePositionsList[0].X;
                }

                UpdatePlayerTravelDotsTexture();
                UpdateMapLocationDotsTexture();

                if (popUp == null)
                {
                    popUp = (DaggerfallTravelPopUp)UIWindowFactory.GetInstanceWithArgs(UIWindowType.TravelPopUp, new object[] { uiManager, uiManager.TopWindow, this });
                }
                //((TravelOptionsPopUp)popUp).EndPos = GetClickMPCoords();
                //uiManager.PushWindow(popUp);
            }
            else
            {
                base.ClickHandler(sender, position);
            }
        }

        protected void MarkLocationHandler(BaseScreenComponent sender, Vector2 position)
        {
            // If allowed handle clicks on map pixels without a location, allowing non-fast travel to any MP coords
            if (RegionSelected && locationSelected && !MouseOverOtherRegion)
            {
                position.y -= regionPanelOffset;

                // Ensure clicks are inside region texture
                if (position.x < 0 || position.x > regionTextureOverlayPanelRect.width || position.y < 0 || position.y > regionTextureOverlayPanelRect.height)
                    return;

                if (markedLocationId == locationSummary.ID)
                    markedLocationId = -1;
                else
                    markedLocationId = locationSummary.ID;

                UpdateMapLocationDotsTexture();
            }
        }

        protected DFPosition GetClickMPCoords()
        {
            float scale = GetRegionMapScale(selectedRegion);
            Vector2 coordinates = GetCoordinates();
            int x = (int)(coordinates.x / scale);
            int y = (int)(coordinates.y / scale);

            if (selectedRegion == betonyIndex)      // Manually correct Betony offset
            {
                x += 60;
                y += 212;
            }

            if (selectedRegion == 61)               // Fix for Cybiades zoom-in map. Map is more zoomed in than for other regions but the pixel coordinates are not scaled to match.
            {
                int xDiff = x - 440;
                int yDiff = y - 340;
                xDiff /= 4;
                yDiff /= 4;
                x = 440 + xDiff;
                y = 340 + yDiff;
            }

            return new DFPosition(x, y);
        }

        // Updates location dots
        protected override void UpdateMapLocationDotsTexture()
        {
            GetDiscoveredLocationsFromHiddenMapMod();

            if (selectedRegion != 61)
            {
                UpdateMapLocationDotsTextureWithPaths();
                UpdatePlayerTravelDotsTexture();
            }
            else
            {
                base.UpdateMapLocationDotsTexture();
            }
        }

        protected virtual void UpdatePlayerTravelDotsTexture()
        {
            // Get map and dimensions
            string mapName = selectedRegionMapNames[mapIndex];
            Vector2 origin = offsetLookup[mapName];
            int originX = (int)origin.x;
            int originY = (int)origin.y;
            int width = (int)regionTextureOverlayPanelRect.width;
            int height = (int)regionTextureOverlayPanelRect.height;

            // Plot locations to color array
            scale = GetRegionMapScale(selectedRegion);
            Array.Clear(travelPathPixelBuffer, 0, travelPathPixelBuffer.Length);

            int widthMulti5 = width * 5;
            int offsetMulti5 = 0;

            foreach (DFPosition pixelPos in currentTravelLinePositionsList)
            {
                offsetMulti5 = (int)((((height - (pixelPos.Y - originY) - 1) * 5 * widthMulti5) + ((pixelPos.X - originX) * 5)) * scale);
                DrawPathLine(offsetMulti5, widthMulti5, blueColor, ref travelPathPixelBuffer);
            }

            // Draw larger "Player Position Crosshair" where the player is meant to currently be
            offsetMulti5 = (int)((((height - (previousPlayerPosition.Y - originY) - 1) * 5 * widthMulti5) + ((previousPlayerPosition.X - originX) * 5)) * scale);
            DrawPlayerPosition(offsetMulti5, widthMulti5, whiteColor, ref travelPathPixelBuffer);

            if ((previousPlayerPosition.Y != destinationPosition.Y) || (previousPlayerPosition.X != destinationPosition.X))
            {
                // Draw Classic Fallout system "Destination Crosshair" 15x15 sprite-sheet drawn method, where the player last clicked
                for (int i = 0; i < 9; i++)
                {
                    if (i >= 0 && i <= 2) // Top Row
                    {
                        offsetMulti5 = (int)((((height - ((destinationPosition.Y - 1) - originY) - 1) * 5 * widthMulti5) + (((destinationPosition.X + (i - 1)) - originX) * 5)) * scale);
                    }
                    else if (i >= 3 && i <= 5) // Middle Row
                    {
                        offsetMulti5 = (int)((((height - (destinationPosition.Y - originY) - 1) * 5 * widthMulti5) + (((destinationPosition.X + (i - 4)) - originX) * 5)) * scale);
                    }
                    else // Bottom Row
                    {
                        offsetMulti5 = (int)((((height - ((destinationPosition.Y + 1) - originY) - 1) * 5 * widthMulti5) + (((destinationPosition.X + (i - 7)) - originX) * 5)) * scale);
                    }
                    DrawDestinationCrosshair(offsetMulti5, widthMulti5, redColor, ref travelPathPixelBuffer, i);
                }
            }

            // Apply updated color array to texture
            travelPathTexture.SetPixels32(travelPathPixelBuffer);
            travelPathTexture.Apply();

            // Present texture
            travelPathOverlayPanel.BackgroundTexture = travelPathTexture;
        }

        protected virtual void UpdateMapLocationDotsTextureWithPaths()
        {
            // Get map and dimensions
            string mapName = selectedRegionMapNames[mapIndex];
            Vector2 origin = offsetLookup[mapName];
            int originX = (int)origin.x;
            int originY = (int)origin.y;
            int width = (int)regionTextureOverlayPanelRect.width;
            int height = (int)regionTextureOverlayPanelRect.height;

            // Plot locations to color array
            scale = GetRegionMapScale(selectedRegion);
            Array.Clear(locationDotsPixelBuffer, 0, locationDotsPixelBuffer.Length);
            Array.Clear(locationDotsOutlinePixelBuffer, 0, locationDotsOutlinePixelBuffer.Length);

            DFPosition playerPOS = previousPlayerPosition;
            DFPosition clickedPOS = EndPos;
            List<DFPosition> pixelsList = FindPixelsBetweenPlayerAndDest(clickedPOS);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = (int)((((height - y - 1) * width) + x) * scale);
                    if (offset >= (width * height))
                        continue;
                    int sampleRegion = DaggerfallUnity.ContentReader.MapFileReader.GetPoliticIndex(originX + x, originY + y) - 128;

                    int width5 = width * 5;
                    int offset5 = (int)((((height - y - 1) * 5 * width5) + (x * 5)) * scale);

                    int pIdx = originX + x + ((originY + y) * MapsFile.MaxMapPixelX);

                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.ContentReader.HasLocation(originX + x, originY + y, out summary))
                    {
                        if (checkLocationDiscovered(summary))
                        {
                            int index = GetPixelColorIndex(summary.LocationType);
                            if (index != -1)
                            {
                                if (DaggerfallUnity.Settings.TravelMapLocationsOutline)
                                    locationDotsOutlinePixelBuffer[offset] = dotOutlineColor;
                                DrawLocation(offset5, width5, locationPixelColors[index], IsLocationLarge(summary.LocationType), ref locationDotsPixelBuffer, summary.ID == markedLocationId);
                            }
                        }
                    }
                }
            }

            // Apply updated color array to texture
            if (DaggerfallUnity.Settings.TravelMapLocationsOutline)
            {
                locationDotsOutlineTexture.SetPixels32(locationDotsOutlinePixelBuffer);
                locationDotsOutlineTexture.Apply();
            }
            locationDotsTexture.SetPixels32(locationDotsPixelBuffer);
            locationDotsTexture.Apply();

            // Present texture
            if (DaggerfallUnity.Settings.TravelMapLocationsOutline)
                for (int i = 0; i < outlineDisplacements.Length; i++)
                    regionLocationDotsOutlinesOverlayPanel[i].BackgroundTexture = locationDotsOutlineTexture;
            regionLocationDotsOverlayPanel.BackgroundTexture = locationDotsTexture;
        }

        public List<DFPosition> FindPixelsBetweenPlayerAndDest(DFPosition endPos)
        {
            List<DFPosition> pixelsList = new List<DFPosition>();
            DFPosition position = previousPlayerPosition;
            int playerXMapPixel = position.X;
            int playerYMapPixel = position.Y;
            int distanceXMapPixels = endPos.X - playerXMapPixel;
            int distanceYMapPixels = endPos.Y - playerYMapPixel;
            int distanceXMapPixelsAbs = Mathf.Abs(distanceXMapPixels);
            int distanceYMapPixelsAbs = Mathf.Abs(distanceYMapPixels);
            int furthestOfXandYDistance = 0;

            if (distanceXMapPixelsAbs <= distanceYMapPixelsAbs)
                furthestOfXandYDistance = distanceYMapPixelsAbs;
            else
                furthestOfXandYDistance = distanceXMapPixelsAbs;

            int xPixelMovementDirection = (distanceXMapPixels >= 0) ? 1 : -1;
            int yPixelMovementDirection = (distanceYMapPixels >= 0) ? 1 : -1;

            int numberOfMovements = 0;
            int shorterOfXandYDistanceIncrementer = 0;

            MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;
            pixelsTraveledOnOcean = 0;

            while (numberOfMovements < furthestOfXandYDistance)
            {
                DFPosition pixelPos = new DFPosition();

                if (furthestOfXandYDistance == distanceXMapPixelsAbs)
                {
                    playerXMapPixel += xPixelMovementDirection;
                    shorterOfXandYDistanceIncrementer += distanceYMapPixelsAbs;

                    if (shorterOfXandYDistanceIncrementer > distanceXMapPixelsAbs)
                    {
                        shorterOfXandYDistanceIncrementer -= distanceXMapPixelsAbs;
                        playerYMapPixel += yPixelMovementDirection;
                    }
                }
                else
                {
                    playerYMapPixel += yPixelMovementDirection;
                    shorterOfXandYDistanceIncrementer += distanceXMapPixelsAbs;

                    if (shorterOfXandYDistanceIncrementer > distanceYMapPixelsAbs)
                    {
                        shorterOfXandYDistanceIncrementer -= distanceYMapPixelsAbs;
                        playerXMapPixel += xPixelMovementDirection;
                    }
                }

                int terrain = mapsFile.GetClimateIndex(playerXMapPixel, playerYMapPixel);
                if (terrain == (int)MapsFile.Climates.Ocean)
                {
                    ++pixelsTraveledOnOcean;
                }

                pixelPos.Y = playerYMapPixel;
                pixelPos.X = playerXMapPixel;

                pixelsList.Add(pixelPos);

                ++numberOfMovements;
            }

            return pixelsList;
        }

        void DrawLocation(int offset, int width, Color32 color, bool large, ref Color32[] pixelBuffer, bool highlight = false)
        {
            int st = large ? 0 : 1;
            int en = large ? 5 : 4;
            for (int y = st; y < en; y++)
            {
                for (int x = st; x < en; x++)
                {
                    pixelBuffer[offset + (y * width) + x] = color;
                }
            }
            if (highlight)
            {
                for (int y = -2; y < 8; y = y + 8)
                {
                    for (int x = -2; x < 7; x++)
                    {
                        pixelBuffer[offset + (y * width) + x] = blackColor;
                    }
                }
                for (int x = -2; x < 8; x = x + 8)
                {
                    for (int y = -2; y < 7; y++)
                    {
                        pixelBuffer[offset + (y * width) + x] = blackColor;
                    }
                }
            }
        }

        bool IsLocationLarge(DFRegion.LocationTypes locationType)
        {
            return locationType == DFRegion.LocationTypes.TownCity || locationType == DFRegion.LocationTypes.TownHamlet || onlyLargeDots;
        }

        public static void DrawDestinationCrosshair(int offset, int width, Color32 pathColor, ref Color32[] pixelBuffer, int part)
        {
            if (part == 0 || part == 8)
            {
                // Top Left or Bottom Right
                pixelBuffer[offset + (width * 4) + 3] = pathColor;
                pixelBuffer[offset + (width * 4) + 4] = pathColor;
                pixelBuffer[offset + (width * 3) + 4] = pathColor;
                pixelBuffer[offset + width] = pathColor;
                pixelBuffer[offset] = pathColor;
                pixelBuffer[offset + 1] = pathColor;
            }
            else if (part == 1)
            {
                // Top Middle
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 4) + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 3) + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
                for (int i = 1; i < 4; i++) { pixelBuffer[offset + width + i] = pathColor; }
                pixelBuffer[offset + 2] = pathColor;
            }
            else if (part == 2 || part == 6)
            {
                // Top Right or Bottom Left
                pixelBuffer[offset + (width * 4)] = pathColor;
                pixelBuffer[offset + (width * 4) + 1] = pathColor;
                pixelBuffer[offset + (width * 3)] = pathColor;
                pixelBuffer[offset + width + 4] = pathColor;
                pixelBuffer[offset + 3] = pathColor;
                pixelBuffer[offset + 4] = pathColor;
            }
            else if (part == 3)
            {
                // Left
                for (int i = 0; i < 3; i++) { pixelBuffer[offset + (width * 4) + i] = pathColor; }
                for (int i = 0; i < 4; i++) { pixelBuffer[offset + (width * 3) + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
                for (int i = 0; i < 4; i++) { pixelBuffer[offset + width + i] = pathColor; }
                for (int i = 0; i < 3; i++) { pixelBuffer[offset + i] = pathColor; }
            }
            else if (part == 4)
            {
                // Middle
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * i) + 2] = pathColor; }
            }
            else if (part == 5)
            {
                // Right
                for (int i = 2; i < 5; i++) { pixelBuffer[offset + (width * 4) + i] = pathColor; }
                for (int i = 1; i < 5; i++) { pixelBuffer[offset + (width * 3) + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
                for (int i = 1; i < 5; i++) { pixelBuffer[offset + width + i] = pathColor; }
                for (int i = 2; i < 5; i++) { pixelBuffer[offset + i] = pathColor; }
            }
            else if (part == 7)
            {
                // Bottom Middle
                pixelBuffer[offset + (width * 4) + 2] = pathColor;
                for (int i = 1; i < 4; i++) { pixelBuffer[offset + (width * 3) + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + width + i] = pathColor; }
                for (int i = 0; i < 5; i++) { pixelBuffer[offset + i] = pathColor; }
            }
        }

        public static void DrawPathLine(int offset, int width, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            if (offset < 0)
            {
                Debug.LogFormat("Index value is negative: offset = {0}", offset);
                return;
            }

            if (offset >= 1280000)
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array: offset = {0}", offset);
                return;
            }

            if (offset + (width * 2) + 2 >= 1280000) // Seems to happen when clicking outside game window, to say clear the debug-log in the Unity Editor, not certain why or if this would even be an issue in live.
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array, when added to width: offset = {0}", offset + (width * 2) + 2);
                return;
            }

            if (offset + (width * 3) + 3 >= 1280000) // Seems to happen when clicking outside game window, to say clear the debug-log in the Unity Editor, not certain why or if this would even be an issue in live.
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array, when added to width: offset = {0}", offset + (width * 3) + 3);
                return;
            }

            if (offset + (width * 4) + 4 >= 1280000) // Seems to happen when clicking outside game window, to say clear the debug-log in the Unity Editor, not certain why or if this would even be an issue in live.
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array, when added to width: offset = {0}", offset + (width * 4) + 4);
                return;
            }

            for (int i = 1; i < 4; i++) { pixelBuffer[offset + (width * 3) + i] = pathColor; }
            for (int i = 1; i < 4; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
            for (int i = 1; i < 4; i++) { pixelBuffer[offset + (width * 1) + i] = pathColor; }
        }

        public static void DrawPlayerPosition(int offset, int width, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            if (offset < 0)
            {
                Debug.LogFormat("Index value is negative: offset = {0}", offset);
                return;
            }

            if (offset >= 1280000)
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array: offset = {0}", offset);
                return;
            }

            if (offset + (width * 2) + 2 >= 1280000) // Seems to happen when clicking outside game window, to say clear the debug-log in the Unity Editor, not certain why or if this would even be an issue in live.
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array, when added to width: offset = {0}", offset + (width * 2) + 2);
                return;
            }

            if (offset + (width * 3) + 3 >= 1280000) // Seems to happen when clicking outside game window, to say clear the debug-log in the Unity Editor, not certain why or if this would even be an issue in live.
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array, when added to width: offset = {0}", offset + (width * 3) + 3);
                return;
            }

            if (offset + (width * 4) + 4 >= 1280000) // Seems to happen when clicking outside game window, to say clear the debug-log in the Unity Editor, not certain why or if this would even be an issue in live.
            {
                Debug.LogFormat("Offset value is outside bounds of pixelBuffer array, when added to width: offset = {0}", offset + (width * 4) + 4);
                return;
            }

            /*pixelBuffer[offset + (width * 4) + 2] = pathColor;
            for (int i = 1; i < 4; i++) { pixelBuffer[offset + (width * 3) + i] = pathColor; }
            for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
            for (int i = 1; i < 4; i++) { pixelBuffer[offset + width + i] = pathColor; }
            pixelBuffer[offset + 2] = pathColor;*/

            for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * 2) + i] = pathColor; }
            for (int i = 0; i < 5; i++) { pixelBuffer[offset + (width * i) + 2] = pathColor; }
        }

        // Zoom and pan region texture
        protected override void ZoomMapTextures()
        {
            base.ZoomMapTextures();

            // Exit cropped rendering
            if (!RegionSelected || !zoom)
            {
                travelPathOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                UpdateBorder();
                return;
            }

            if (RegionSelected && zoom)
            {
                // Adjust cropped location dots overlay to x5 version
                int width = (int)regionTextureOverlayPanelRect.width;
                int height = (int)regionTextureOverlayPanelRect.height;
                int zoomWidth = width / (zoomfactor * 2);
                int zoomHeight = height / (zoomfactor * 2);
                int startX = (int)zoomPosition.x - zoomWidth;
                int startY = (int)(height + (-zoomPosition.y - zoomHeight)) + regionPanelOffset;
                // Clamp to edges
                if (startX < 0)
                    startX = 0;
                else if (startX + width / zoomfactor >= width)
                    startX = width - width / zoomfactor;
                if (startY < 0)
                    startY = 0;
                else if (startY + height / zoomfactor >= height)
                    startY = height - height / zoomfactor;

                Rect locationDotsNewRect = new Rect(startX * 5, startY * 5, width * 5 / zoomfactor, height * 5 / zoomfactor);
                regionLocationDotsOverlayPanel.BackgroundCroppedRect = locationDotsNewRect;

                travelPathOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
                travelPathOverlayPanel.BackgroundCroppedRect = locationDotsNewRect;

                UpdateBorder();
            }
        }

        protected override void OpenRegionPanel(int region)
        {
            base.OpenRegionPanel(region);

            travelPathOverlayPanel.Enabled = true;
        }

        protected override void CloseRegionPanel()
        {
            base.CloseRegionPanel();

            travelPathOverlayPanel.Enabled = false;
        }

        public void DrawMapSection(int originX, int originY, int width, int height, ref Color32[] pixelBuffer, bool circular = false)
        {
            GetDiscoveredLocationsFromHiddenMapMod();
        
            Array.Clear(pixelBuffer, 0, pixelBuffer.Length);

            for (int y = 0; y < height; y++)
            {
                int mpY = originY + y;
                if (mpY < 0 || mpY >= MapsFile.MaxMapPixelY)
                    continue;

                for (int x = 0; x < width; x++)
                {
                    int mpX = originX + x;
                    if (mpX < 0 || mpX >= MapsFile.MaxMapPixelX)
                        continue;

                    if (circular && height == width && Mathf.Sqrt(Mathf.Pow(Mathf.Abs(x - (width / 2) + 0.5f), 2) + Mathf.Pow(Mathf.Abs(y - (height / 2) + 0.5f), 2)) >= (height + 1.5) / 2) 
                        continue;

                    int offset = ((height - y - 1) * width) + x;
                    if (offset >= (width * height))
                        continue;
                    int width5 = width * 5;
                    int offset5 = ((height - y - 1) * 5 * width5) + (x * 5);

                    int pIdx = mpX + (mpY * MapsFile.MaxMapPixelX);
                    //Debug.LogFormat("Checking paths at x:{0} y:{1}  index:{2}", mpX, mpY, pIdx);
                    /*if (showPaths[path_tracks])
                        DrawPath(offset5, width5, pathsData[path_tracks][pIdx], trackColor, ref pixelBuffer);
                    if (showPaths[path_roads])
                        DrawPath(offset5, width5, pathsData[path_roads][pIdx], roadColor, ref pixelBuffer);*/

                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.Instance.ContentReader.HasLocation(mpX, mpY, out summary))
                    {
                        if (checkLocationDiscovered(summary))
                        {
                            int index = GetPixelColorIndex(summary.LocationType);
                            if (index != -1)
                            {
                                DrawLocation(offset5, width5, locationPixelColors[index], IsLocationLarge(summary.LocationType), ref pixelBuffer, summary.ID == markedLocationId);
                            }
                        }
                    }
                }
            }
        }

        protected override bool checkLocationDiscovered(ContentReader.MapSummary summary)
        {
            // If ports filter is on, only return true if it's a port
            if (portsFilter && !HasPort(summary))
                return false;

            // Hidden Map Locations: Reveal ports if setting enabled.
            /*if (TravelOptionsMod.Instance.HiddenMapLocationsEnabled)
            {
                if (TravelOptionsMod.Instance.HiddenMapLocationsRevealPorts && HasPort(summary))
                    return true;
                else
                    return discoveredMapSummaries.Contains(summary) || revealedLocationTypes.Contains(summary.LocationType);
            }*/

            return base.checkLocationDiscovered(summary);
        }

        protected void GetDiscoveredLocationsFromHiddenMapMod()
        {
            /*if (TravelOptionsMod.Instance.HiddenMapLocationsEnabled)
            {
                ModManager.Instance.SendModMessage(TravelOptionsMod.HIDDEN_MAP_LOCATIONS_MODNAME, "getDiscoveredMapSummaries", null,
                    (string _, object result) => { discoveredMapSummaries = (HashSet<ContentReader.MapSummary>)result; });
            }*/
        }

        public static int MaskMapId(int mapId)
        {
            return mapId & 0x000FFFFF;
        }

        public static bool HasPort(ContentReader.MapSummary mapSummary)
        {
            return HasPort(mapSummary.ID);
        }

        public static bool HasPort(DFRegion.RegionMapTable mapTable)
        {
            return HasPort(mapTable.MapId);
        }

        public static bool HasPort(int mapId)
        {
            return Array.Exists(portLocationIds, n => n == MaskMapId(mapId));
        }

        public static readonly int[] portLocationIds = {
            443401, 280614, 285597, 485856, 86496, 137544, 139547, 143535, 143542, 149513,
            150625, 158629, 162631, 162646, 164648, 166644, 168652, 169640, 170654, 178663,
            182685, 188727, 192653, 195681, 201654, 202646, 203671, 225685, 234712, 22763,
            184263, 189097, 192248, 194242, 194279, 196245, 199102, 199111, 201125, 210132,
            212138, 213207, 226205, 228209, 235146, 236143, 239139, 239144, 239146, 241140,
            91170, 93168, 96150, 96212, 107137, 109167, 325404, 325406, 328409, 341392,
            342399, 343397, 344387, 345378, 345383, 346398, 347375, 348372, 348396, 350370,
            351392, 352369, 353387, 354364, 361382, 364381, 369385, 369388, 370441, 372411,
            372439, 373407, 373415, 373422, 373425, 373427, 373429, 374419, 120375, 121377,
            148460, 148463, 150459, 158499, 168357, 172455, 187406, 192361, 193358, 193366,
            195353, 195361, 197366, 200356, 277751, 278764, 279644, 279697, 279749, 279754,
            279766, 280747, 281656, 281658, 281663, 281699, 281702, 281704, 281741, 281770,
            282712, 282724, 282728, 282731, 282734, 282737, 283687, 283707, 284685, 285682,
            286674, 289737, 292695, 293697, 310763, 311766, 194855, 195860, 223828, 225840,
            229847, 236854, 240841, 242856, 243846, 244859, 247836, 249839, 249861, 249866,
            250875, 255876, 256887, 256900, 257889, 258892, 258907, 261923, 261925, 262907,
            262931, 264900, 264902, 264940, 264942, 265956, 266964, 273975, 5222, 5224,
            11215, 14210, 23240, 35152, 49219, 157795, 181800, 187807, 193793, 210785,
            215821, 216791, 112707, 133701, 133718, 134711, 135713, 135717, 135735, 138745,
            140758, 140760, 148782, 151788, 83668, 125675, 111631, 111645, 112652, 113637,
            113646, 113649, 115622, 118573, 134553, 137558, 137561, 137593, 138583, 139588,
            145609, 146607, 147614, 148589, 151591, 152587, 56637, 35449, 41483, 121473,
            129449, 29347, 40361, 69406, 160305, 451180, 451186, 453173, 455174, 457179,
            458198, 460176, 461173, 463171, 468168, 468188, 473169, 474207, 476162, 476164,
            477177, 478159, 483153, 493144, 495141, 422217, 432218, 433205, 435202, 455199,
            459220, 405246, 405263, 406266, 407241, 408235, 408249, 417227, 393300, 397296,
            403279, 406276, 418291, 364449, 370446, 402451, 276583, 279596, 290582, 294569,
            295564, 296558, 297552, 305534, 308524, 308527, 308530, 309521, 312518, 313516,
            316550, 318514, 334515, 339496, 341496, 346475, 351470, 337704, 263832, 264825,
            269847, 269849, 276835, 277798, 278817, 278843, 279815, 283779, 283782, 287827,
            287829, 289866, 294842, 302839, 306854, 337914, 338912, 341916, 346918, 351919,
            354916, 357915, 357918, 361913, 363915, 364868, 370908, 379876, 379888, 380885,
            381881, 382879, 278962, 281872, 281969, 324981, 469891, 437653, 446471, 472431,
            480415, 217966, 100086, 121067, 123073, 144059, 75104, 77077, 83137, 86218,
            86334, 89333, 343439,
            // Extras allowing travel to:
            205676,     // "Isle of Balfiera", "Blackhead"
            278901,     // "Mournoth", "Zagoparia"
            263119,     // "Betony", "Whitefort"
            148062,     // "Tulune", "The Citadel of Hearthham"
            144059,     // "Tulune", "The Elyzanna Assembly"
            343439,     // "Cybiades", "Ruins of Cosh Hall"
            243846,     // Wayrest	Penwall Derry
            273975,     // Wayrest	Tunmont
            255884,     // Wayrest	Eastwold
            256887,     // Wayrest	Chardale
            262931,     // Wayrest	Longmore Field
            106061,     // Tulune	Midmont
            164072,     // Tulune  Lambrugh
            157092,     // Tulune	Gallocart
            296558,     // Tigonus Antelibuton
            297552,     // Tigonus Wadijerareg
            308527,     // Tigonus Kalureg
            376329,     // Sentinel	Pibuda
            359347,     // Sentinel	Zenuhno
            357347,     // Sentinel	Mji-Ij
            358355,     // Sentinel	Antelajda
            350370,     // Sentinel	Naresa
            347375,     // Sentinel	Jalonia
            343397,     // Sentinel Sentinel
            373407,     // Sentinel	Cudakasa
            373415,     // Sentinel	Bubumbaret
            327404,     // Sentinel	Bubissidata
            283779,     // Satakalaam	Tulajidax
            406266,     // Pothago	Berbajan
            42127,      // Northmoor	Gothcroft
            5229,       // Northmoor	Knightshope
            8219,       // Northmoor	Stokwall
            34162,      // Northmoor	Vanpath
            38152,      // Northmoor	Pencart
            45124,      // Northmoor	Burgcart Heath
            403279,     // Myrkwasa	Elissinia
            285898,     // Mournoth	Wadijilanis
            278962,     // Mournoth	Meseraara
            216791,     // Menevia	Chesterbrugh
            279754,     // Lainlyn	Kalunnunu
            281658,     // Lainlyn	Syrotubu
            286674,     // Lainlyn	Papiladisu
            281699,     // Lainlyn	Syrallao
            281702,     // Lainlyn	Pythohajer
            73079,      // Glenumbra Moors	Tambridge
            77077,      // Glenumbra Moors	Deerpath
            223207,     // Daggerfall	Westhead Moor
            199102,     // Daggerfall	Whitecroft
            201118,     // Daggerfall	Holwych
            213285,     // Daggerfall	Ripmore
            214207,     // Daggerfall	Copperfield Manor
            221160,     // Daggerfall	Fontborne
            225169,     // Daggerfall	Longwich End
            224178,     // Daggerfall	Wilderham
            217198,     // Daggerfall	Midbrugh
            214294,     // Daggerfall	Vanvale
            214297,     // Daggerfall	Blackcart Hollow
            239144,     // Daggerfall	Aldpath Hall
            236143,     // Daggerfall	Grimton
            139588,     // Bhoriane	Wartale
            113637,     // Bhoriane	Fontbridge
            112652,     // Bhoriane	Stokbrone
            255114,     // Betony	Kirkbeth Hamlet
            364449,     // Ayasofya Umbopala
            183401,     // Anticlere	Aldwall Rock
            175401,     // Anticlere	Crossleigh
            187406,     // Anticlere	Cathwold Heath
            195353,     // Anticlere	Vanwood Hollow
            193358,     // Anticlere	Ipspath
            197366,     // Anticlere	Wilderbury Rock
            133718,     // Alcaire	Cathborne
            140760,     // Alcaire	Wargate
            455174,     // Abibon-Gora	Papyrydai
            202333,  // Shalgora Aldbrugh
        };
        
    }
}
