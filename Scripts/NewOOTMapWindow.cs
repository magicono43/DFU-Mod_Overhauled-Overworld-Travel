using UnityEngine;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using LockedLootContainers;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using OverhauledOverworldTravel;
using System;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallWorkshop.Utility.AssetInjection;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Implements Overhauled Overworld Travel's World Map Interface Window.
    /// </summary>
    public class NewOOTMapWindow : DaggerfallPopupWindow
    {
        PlayerEntity player;

        PlayerEntity Player
        {
            get { return (player != null) ? player : player = GameManager.Instance.PlayerEntity; }
        }

        #region Testing Properties

        public static Color32 redColor = new Color32(255, 0, 0, 255);
        public static Color32 blueColor = new Color32(0, 0, 255, 255);
        public static Color32 blackColor = new Color32(0, 0, 0, 255);
        public static Color32 whiteColor = new Color32(255, 255, 255, 255);

        public static Rect butt1 = new Rect(0, 0, 0, 0);
        public static Rect butt2 = new Rect(0, 0, 0, 0);
        public static Rect butt3 = new Rect(0, 0, 0, 0);

        #endregion

        #region Constructors

        public NewOOTMapWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
        }

        #endregion

        #region UI Textures

        Texture2D baseTexture;
        Texture2D backgroundTexture;
        Texture2D heightMapTexture;

        Panel worldMapPanel;

        Panel locationDotOverlayPanel;
        Texture2D locationDotTexture;
        Color32[] locationDotPixelBuffer;

        Panel travelPathOverlayPanel;
        Texture2D travelPathTexture;
        Color32[] travelPathPixelBuffer;

        Panel mouseCursorHitboxOverlayPanel;
        Texture2D mouseCursorHitboxTexture;
        Color32[] mouseCursorHitboxPixelBuffer;

        TextLabel regionLabel;
        TextLabel firstDebugLabel;
        TextLabel secondDebugLabel;
        TextLabel thirdDebugLabel;

        #endregion

        int currentZoom = 1;
        Vector2 zoomPosition = Vector2.zero;
        Vector2 zoomOffset = Vector2.zero;
        Vector2 lastMousePos = Vector2.zero;

        Dictionary<string, Vector2> offsetLookup = new Dictionary<string, Vector2>();

        public static Vector2 GetWorldMapPanelSize()
        {
            return new Vector2(1000, 500);
            /*
            int screenWidth = DaggerfallUnity.Settings.ResolutionWidth;
            int screenHeight = DaggerfallUnity.Settings.ResolutionHeight;

            if (screenWidth >= 1600 && screenHeight >= 800) { return new Vector2(1600, 800); }
            else if (screenWidth >= 1280 && screenHeight >= 640) { return new Vector2(1280, 640); }
            else if (screenWidth >= 960 && screenHeight >= 480) { return new Vector2(960, 480); }
            else if (screenWidth >= 640 && screenHeight >= 320) { return new Vector2(640, 320); }
            else { return new Vector2(320, 160); }
            */
        }

        protected override void Setup()
        {
            base.Setup();

            // Load textures
            LoadTextures();

            // Populate the offset dict
            PopulateRegionOffsetDict();

            ParentPanel.BackgroundColor = Color.black;
            //ParentPanel.BackgroundTexture = backgroundTexture;

            // This makes the background "transparent" instead of a blank black screen when opening this window.
            //ParentPanel.BackgroundColor = ScreenDimColor;

            // Setup native panel background
            NativePanel.BackgroundColor = ScreenDimColor;
            //NativePanel.BackgroundTexture = baseTexture;

            worldMapPanel = new Panel();
            worldMapPanel.HorizontalAlignment = HorizontalAlignment.Center;
            worldMapPanel.VerticalAlignment = VerticalAlignment.Middle;
            worldMapPanel.Size = new Vector2(1000, 500);
            worldMapPanel.AutoSize = AutoSizeModes.None;
            worldMapPanel.BackgroundColor = ScreenDimColor;
            worldMapPanel.BackgroundTexture = heightMapTexture;
            //worldMapPanel.ToolTip = defaultToolTip;
            //worldMapPanel.ToolTipText = "This Is The World Map";
            worldMapPanel.OnMouseClick += ClickHandler;
            if (ParentPanel != null)
                ParentPanel.Components.Add(worldMapPanel);

            Rect rectWorldMap = worldMapPanel.Rectangle;

            // Add region/location label
            regionLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.LargeFont, new Vector2(0, 2), string.Empty, worldMapPanel);
            regionLabel.HorizontalAlignment = HorizontalAlignment.Center;
            regionLabel.TextScale = 2.7f;

            // Add debug display labels
            firstDebugLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.LargeFont, new Vector2(0, 2), string.Empty, worldMapPanel);
            firstDebugLabel.HorizontalAlignment = HorizontalAlignment.Left;
            firstDebugLabel.TextScale = 2.0f;

            secondDebugLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.LargeFont, new Vector2(0, 25), string.Empty, worldMapPanel);
            secondDebugLabel.HorizontalAlignment = HorizontalAlignment.Left;
            secondDebugLabel.TextScale = 2.0f;

            thirdDebugLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.LargeFont, new Vector2(0, 48), string.Empty, worldMapPanel);
            thirdDebugLabel.HorizontalAlignment = HorizontalAlignment.Left;
            thirdDebugLabel.TextScale = 2.0f;

            // Overlay for the map locations panel
            locationDotOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel);
            locationDotOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            locationDotOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;
            //locationDotOverlayPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            locationDotPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            locationDotTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            locationDotTexture.filterMode = FilterMode.Point;

            // Overlay for the player travel path panel
            travelPathOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel); // May have to make the Parent panel this panel's parent similar to the worldMapPanel, will see.
            travelPathOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            travelPathOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;
            //travelPathOverlayPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            // Setup pixel buffer and texture for player travel path
            travelPathPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            travelPathTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            travelPathTexture.filterMode = FilterMode.Point;

            // Overlay for the mouse cursor hitbox panel
            mouseCursorHitboxOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel); // May have to make the Parent panel this panel's parent similar to the worldMapPanel, will see.
            mouseCursorHitboxOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mouseCursorHitboxOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;
            //travelPathOverlayPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            // Setup pixel buffer and texture for mouse cursor hitbox area
            mouseCursorHitboxPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            mouseCursorHitboxTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            mouseCursorHitboxTexture.filterMode = FilterMode.Point;

            Panel chestPictureBox = DaggerfallUI.AddPanel(new Rect(113, 64, 30, 22), NativePanel);
            chestPictureBox.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes
            chestPictureBox.ToolTip = defaultToolTip;
            chestPictureBox.ToolTipText = "The Chest Looks";

            // Zoom Out Button
            Button zoomOutButton = DaggerfallUI.AddButton(new Rect(0, 0, 0, 0), worldMapPanel);
            zoomOutButton.Hotkey = new HotkeySequence(KeyCode.Semicolon, HotkeySequence.KeyModifiers.None);

            // Exit Button
            Button exitButton = DaggerfallUI.AddButton(new Rect(139, 122, 43, 15), NativePanel);
            exitButton.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes
            exitButton.OnMouseClick += ExitButton_OnMouseClick;
            exitButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);

            //SetupChestChoiceButtons();
        }

        protected virtual void LoadTextures()
        {
            baseTexture = OOTMain.Instance.PrimaryWorldMapTexture;
            backgroundTexture = OOTMain.Instance.BackgroundMapFillerTexture;
            heightMapTexture = OOTMain.Instance.WorldHeightMapTexture;
        }

        protected void SetupChestChoiceButtons()
        {
            // Inspect Chest button
            Button inspectChestButton = DaggerfallUI.AddButton(new Rect(144, 70, 33, 16), NativePanel);
            inspectChestButton.ToolTip = defaultToolTip;
            inspectChestButton.ToolTipText = "Inspect Chest";
            //inspectChestButton.OnMouseClick += InspectChestButton_OnMouseClick;
            inspectChestButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);

            // Attempt Lockpick button
            Button attemptLockpickButton = DaggerfallUI.AddButton(new Rect(144, 92, 33, 16), NativePanel);
            attemptLockpickButton.ToolTip = defaultToolTip;
            attemptLockpickButton.ToolTipText = "Attempt Lockpick";
            //attemptLockpickButton.OnMouseClick += AttemptLockpickButton_OnMouseClick;
            attemptLockpickButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);

            // Exit button
            Button exitButton = DaggerfallUI.AddButton(new Rect(142, 114, 36, 17), NativePanel);
            exitButton.OnMouseClick += ExitButton_OnMouseClick;
            exitButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.ButtonClick);
        }

        public override void Update()
        {
            base.Update();

            // Input handling
            HotkeySequence.KeyModifiers keyModifiers = HotkeySequence.GetKeyboardKeyModifiers();
            Vector2 currentMousePos = Vector2.zero;
            Rect mainMapRect = Rect.zero;
            if (worldMapPanel != null) { currentMousePos = new Vector2(worldMapPanel.ScaledMousePosition.x, worldMapPanel.ScaledMousePosition.y); mainMapRect = worldMapPanel.Rectangle; }

            if (currentMousePos.x != lastMousePos.x || currentMousePos.y != lastMousePos.y)
            {
                lastMousePos.x = currentMousePos.x;
                lastMousePos.y = currentMousePos.y;

                // Ensure cursor is inside map texture
                if (currentMousePos.x < 0 || currentMousePos.x > mainMapRect.width || currentMousePos.y < 0 || currentMousePos.y > mainMapRect.height)
                    return;

                int flippedY = (int)(mainMapRect.height - currentMousePos.y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
                int pixelBufferPos = (int)(flippedY * mainMapRect.width + currentMousePos.x);

                if (currentZoom > 1)
                {
                    int usedPosX = (int)((currentMousePos.x + (zoomOffset.x * currentZoom)) / currentZoom);
                    int usedPosY = (int)((currentMousePos.y + (zoomOffset.y * currentZoom)) / currentZoom);
                    flippedY = (int)(mainMapRect.height - usedPosY - 1);
                    pixelBufferPos = (int)(flippedY * mainMapRect.width + usedPosX);
                }

                UpdateMouseOverLocationLabel(currentMousePos);
                TestWhereMouseCursorHitboxIsLocated(pixelBufferPos);
            }

            if (InputManager.Instance.GetMouseButtonUp(1))
            {
                // Ensure clicks are inside map texture
                if (currentMousePos.x < 0 || currentMousePos.x > mainMapRect.width || currentMousePos.y < 0 || currentMousePos.y > mainMapRect.height)
                    return;

                zoomPosition = currentMousePos;

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) // Zoom out to mouse position
                {
                    ZoomMapTexture(false, false);
                }
                else // Zoom to mouse position
                {
                    ZoomMapTexture(false, true);
                }
            }
            else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentZoom > 1)
            {
                // Ensure clicks are inside map texture
                if (currentMousePos.x < 0 || currentMousePos.x > mainMapRect.width || currentMousePos.y < 0 || currentMousePos.y > mainMapRect.height)
                    return;

                // Scrolling while zoomed in
                zoomPosition = currentMousePos;
                ZoomMapTexture(true, false);
            }
        }

        // Will update the text label showing the location name (if any) currently moused over on the map
        void UpdateMouseOverLocationLabel(Vector2 currentMousePos)
        {
            Vector2 testingZoomMousePos = Vector2.zero;
            testingZoomMousePos.x = zoomOffset.x;
            testingZoomMousePos.y = zoomOffset.y;

            Vector2 testingScaledMousePos = new Vector2(currentMousePos.x, currentMousePos.y);
            Vector2 testingMousePos = new Vector2(testingZoomMousePos.x, testingZoomMousePos.y);

            firstDebugLabel.Text = string.Format("ScaledMousePos: ({0}, {1})", testingScaledMousePos.x, testingScaledMousePos.y);
            secondDebugLabel.Text = string.Format("ZoomedMousePos: ({0}, {1})", testingMousePos.x, testingMousePos.y);
            thirdDebugLabel.Text = string.Format("Magnification: {0}x", currentZoom);

            int usedPosX = (int)((currentMousePos.x + (zoomOffset.x * currentZoom)) / currentZoom);
            int usedPosY = (int)((currentMousePos.y + (zoomOffset.y * currentZoom)) / currentZoom);

            ContentReader.MapSummary mapSummary;
            if (DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX, usedPosY, out mapSummary)) { }
            else if (DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX, usedPosY - 1, out mapSummary)) { }
            else if (DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX + 1, usedPosY - 1, out mapSummary)) { }
            else { DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX + 1, usedPosY, out mapSummary); }

            string regionName = DaggerfallUnity.ContentReader.MapFileReader.GetRegionName(mapSummary.RegionIndex);
            string locationName = GetLocationNameInCurrentRegion(mapSummary.RegionIndex, mapSummary.MapIndex);
            int mapPixelID = mapSummary.ID;
            if (locationName == string.Empty) // Keep label from showing up if no valid location is moused over.
            {
                regionLabel.Text = string.Empty;
            }
            else
            {
                regionLabel.Text = string.Format("{0} : {1} ({2})", regionName, locationName, mapPixelID);
            }
        }

        // Gets name of location in currently moused over region - tries world data replacement then falls back to MAPS.BSA
        string GetLocationNameInCurrentRegion(int regionIndex, int locationIndex)
        {
            // Must have a region open
            if (regionIndex == -1)
                return string.Empty;

            // Just a work around for now, since "invalid space" gets a 0 and 0 value rather than -1 and -1 like I expected. So this specific location in Alik'r Desert is ignored for now.
            if (regionIndex == 0 && locationIndex == 0)
                return string.Empty;

            // Get location name from world data replacement if available or fall back to MAPS.BSA cached names
            DFLocation location;
            if (WorldDataReplacement.GetDFLocationReplacementData(regionIndex, locationIndex, out location))
            {
                return location.Name;
            }
            else
            {
                DFRegion dfRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegion(regionIndex);
                string locationName = dfRegion.MapNames[locationIndex];

                return locationName;
            }
        }

        // Handle clicks on the world map panel
        void ClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            Rect mainMapRect = sender.Rectangle;

            // Ensure clicks are inside region texture
            if (position.x < 0 || position.x > mainMapRect.width || position.y < 0 || position.y > mainMapRect.height)
                return;

            // Play distinct sound just for testing right now.
            DaggerfallUI.Instance.PlayOneShot(DaggerfallUI.Instance.GetAudioClip(SoundClips.PageTurn));

            Vector2 clickedPos = sender.ScaledMousePosition;
            Debug.LogFormat("Clicked This Spot: x:{0} y:{1}", clickedPos.x, clickedPos.y);

            int flippedY = (int)(mainMapRect.height - clickedPos.y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
            int pixelBufferPos = (int)(flippedY * mainMapRect.width + clickedPos.x);

            if (currentZoom > 1)
            {
                int usedPosX = (int)((clickedPos.x + (zoomOffset.x * currentZoom)) / currentZoom);
                int usedPosY = (int)((clickedPos.y + (zoomOffset.y * currentZoom)) / currentZoom);
                flippedY = (int)(mainMapRect.height - usedPosY - 1);
                pixelBufferPos = (int)(flippedY * mainMapRect.width + usedPosX);
            }

            TestPlacingDaggerfallLocationDots();

            TestWherePixelBufferIsLocated(pixelBufferPos);

            /*EndPos = GetClickMPCoords();

            destinationPosition = ConvertDFPosToExactPixelPos(EndPos);
            currentTravelLinePositionsList = FindPixelsBetweenPlayerAndDest();
            if (currentTravelLinePositionsList.Count > 0)
            {
                nextPlayerPosition.Y = currentTravelLinePositionsList[0].Y;
                nextPlayerPosition.X = currentTravelLinePositionsList[0].X;
                dateTimeInSeconds += 25;
                mapTimeHasChanged = true;
            }

            UpdatePlayerTravelDotsTexture();
            UpdateMapLocationDotsTexture();

            if (popUp == null)
            {
                popUp = (DaggerfallTravelPopUp)UIWindowFactory.GetInstanceWithArgs(UIWindowType.TravelPopUp, new object[] { uiManager, uiManager.TopWindow, this });
            }*/
        }

        void ZoomMapTexture(bool scrolling, bool zoomIn) // Attempt to get some form of zooming to work on the world map.
        {
            int originalZoom = currentZoom;
            int zoomFactor = 1;

            if (scrolling)
            {
                zoomFactor = currentZoom;
            }
            else
            {
                worldMapPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                locationDotOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                travelPathOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                mouseCursorHitboxOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;

                if (zoomIn)
                {
                    if (currentZoom == 1) { zoomFactor = 2; currentZoom = 2; }
                    else if (currentZoom == 2) { zoomFactor = 4; currentZoom = 4; }
                    else if (currentZoom == 4) { zoomFactor = 5; currentZoom = 5; }
                    else if (currentZoom == 5) { zoomFactor = 10; currentZoom = 10; }
                    else if (currentZoom == 10) { zoomFactor = 10; currentZoom = 10; }
                }
                else
                {
                    if (currentZoom == 1) { currentZoom = 1; zoomOffset = Vector2.zero; return; }
                    else if (currentZoom == 2) { currentZoom = 1; zoomOffset = Vector2.zero; return; }
                    else if (currentZoom == 4) { zoomFactor = 2; currentZoom = 2; }
                    else if (currentZoom == 5) { zoomFactor = 4; currentZoom = 4; }
                    else if (currentZoom == 10) { zoomFactor = 5; currentZoom = 5; }
                }
            }

            bool zoomChanged = (originalZoom != currentZoom);

            if (!scrolling)
            {
                if (zoomChanged && zoomIn)
                {
                    DaggerfallUI.Instance.PlayOneShot(DaggerfallUI.Instance.GetAudioClip(SoundClips.GoldPieces));
                }
                else if (zoomChanged)
                {
                    DaggerfallUI.Instance.PlayOneShot(DaggerfallUI.Instance.GetAudioClip(SoundClips.PlayerDoorBash));
                }
            }

            // Center cropped portion over mouse using classic dimensions
            int width = 1000;
            int height = 500;
            int zoomWidth = width / (zoomFactor * 2);
            int zoomHeight = height / (zoomFactor * 2);
            int startX = (int)zoomPosition.x - zoomWidth;
            int startY = (int)(height + (-zoomPosition.y - zoomHeight));

            // Clamp to edges
            if (startX < 0)
                startX = 0;
            else if (startX + width / zoomFactor >= width)
                startX = width - width / zoomFactor;
            if (startY < 0)
                startY = 0;
            else if (startY + height / zoomFactor >= height)
                startY = height - height / zoomFactor;

            zoomOffset = new Vector2(startX, Mathf.Abs(startY - height) - (height / zoomFactor));

            // Set cropped area in location dots panel - always at classic dimensions
            Rect worldMapNewRect = new Rect(startX, startY, width / zoomFactor, height / zoomFactor);
            worldMapPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            worldMapPanel.BackgroundCroppedRect = worldMapNewRect;
            locationDotOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            locationDotOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
            travelPathOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            travelPathOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
            mouseCursorHitboxOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            mouseCursorHitboxOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
        }

        void TestPlacingDaggerfallLocationDots()
        {
            Array.Clear(locationDotPixelBuffer, 0, locationDotPixelBuffer.Length);

            /*Vector2 origin = offsetLookup["FMAP0I59.IMG"];
            int regionIndex = 59;
            int originX = (int)origin.x;
            int originY = (int)origin.y;*/
            int width = 1000;
            int height = 500;

            // Plot locations to color array
            float scale = 1.0f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = (int)((((height - y - 1) * width) + x) * scale);
                    if (offset >= (height * width))
                        continue;

                    if (DaggerfallUnity.Instance.ContentReader.HasLocation(x, y))
                    {
                        locationDotPixelBuffer[offset] = redColor;
                    }
                }
            }

            // Maybe tomorrow, try to get the location dots to have the "proper" color based on the location type, then may try to get color on the map texture, will see.

            /*foreach (KeyValuePair<string, Vector2> entry in offsetLookup)
            {
                // Get map and dimensions
                string mapName = entry.Key;
                Vector2 origin = entry.Value;
                int regionIndex = GetRegionIndexByMapName(mapName);
                int originX = (int)origin.x;
                int originY = (int)origin.y;
                int width = 320;
                int height = 160;

                // Plot locations to color array
                float scale = 1.0f;
                scale = GetRegionMapScale(regionIndex);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = (int)((((height - y - 1) * width) + x) * scale);
                        //if (offset >= (width * height))
                            //continue;

                        ContentReader.MapSummary summary;
                        if (DaggerfallUnity.Instance.ContentReader.HasLocation(originX + x, originY + y, out summary))
                        {
                            locationDotPixelBuffer[offset] = redColor;
                        }
                    }
                }
            }*/

            // Apply updated color array to texture
            locationDotTexture.SetPixels32(locationDotPixelBuffer);
            locationDotTexture.Apply();

            // Present texture
            locationDotOverlayPanel.BackgroundTexture = locationDotTexture;

            // Plot locations to color array
            /*for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.ContentReader.HasLocation(x, y, out summary))
                    {
                        int flippedY = (int)(height - y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
                        int index = (int)(flippedY * width + x);
                        locationDotPixelBuffer[index * 16] = redColor;
                    }
                }
            }*/
        }

        void TestWherePixelBufferIsLocated(int clickedSpot)
        {
            Vector2 mapDimensions = GetWorldMapPanelSize();
            int width = (int)mapDimensions.x;
            int height = (int)mapDimensions.y;

            Array.Clear(travelPathPixelBuffer, 0, travelPathPixelBuffer.Length);

            DrawClickedSpotCursor(clickedSpot, width, ref travelPathPixelBuffer);

            /*int spacing = 5;

            // Plot locations to color array
            Array.Clear(travelPathPixelBuffer, 0, travelPathPixelBuffer.Length);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x % spacing == 0 || y % spacing == 0)
                    {
                        int index = y * width + x;
                        travelPathPixelBuffer[index] = whiteColor;
                    }
                }
            }*/

            // Apply updated color array to texture
            travelPathTexture.SetPixels32(travelPathPixelBuffer);
            travelPathTexture.Apply();

            // Present texture
            travelPathOverlayPanel.BackgroundTexture = travelPathTexture;
        }

        void DrawClickedSpotCursor(int pixelPos, int width, ref Color32[] pixelBuffer) // Right now assuming 4x4 "pixel" size or 16 area, will need to consider other resolution values later.
        {
            for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos + i] = whiteColor; }
            for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos - width + i] = whiteColor; }

            for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos + (width * i)] = whiteColor; }
            for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos + (width * i) - 1] = whiteColor; }
        }

        void TestWhereMouseCursorHitboxIsLocated(int hitboxCenter)
        {
            Vector2 mapDimensions = GetWorldMapPanelSize();
            int width = (int)mapDimensions.x;
            int height = (int)mapDimensions.y;

            Array.Clear(mouseCursorHitboxPixelBuffer, 0, mouseCursorHitboxPixelBuffer.Length);

            DrawMouseCursorHitboxArea(hitboxCenter, width, ref mouseCursorHitboxPixelBuffer);

            // Apply updated color array to texture
            mouseCursorHitboxTexture.SetPixels32(mouseCursorHitboxPixelBuffer);
            mouseCursorHitboxTexture.Apply();

            // Present texture
            mouseCursorHitboxOverlayPanel.BackgroundTexture = mouseCursorHitboxTexture;
        }

        void DrawMouseCursorHitboxArea(int pixelPos, int width, ref Color32[] pixelBuffer) // Right now assuming 2x2 "pixel" size or 4 area, will need to consider other resolution values later.
        {
            pixelBuffer[pixelPos] = whiteColor;
            pixelBuffer[pixelPos + 1] = whiteColor;
            pixelBuffer[pixelPos + width] = whiteColor;
            pixelBuffer[pixelPos + width + 1] = whiteColor;
        }

        int GetRegionIndexByMapName(string mapName)
        {
            switch (mapName)
            {
                case "FMAPAI00.IMG":
                case "FMAPBI00.IMG": return 0; // Alik'r Desert
                case "FMAPAI01.IMG":
                case "FMAPBI01.IMG":
                case "FMAPCI01.IMG":
                case "FMAPDI01.IMG": return 1; // Dragontail Mountains
                case "FMAP0I05.IMG": return 5; // Dwynnen
                case "FMAP0I09.IMG": return 9; // Isle of Balfiera
                case "FMAP0I11.IMG": return 11; // Dak'fron
                case "FMAPAI16.IMG":
                case "FMAPBI16.IMG":
                case "FMAPCI16.IMG":
                case "FMAPDI16.IMG": return 16; // Wrothgarian Mountains
                case "FMAP0I17.IMG": return 17; // Daggerfall
                case "FMAP0I18.IMG": return 18; // Glenpoint
                case "FMAP0I19.IMG": return 19; // Betony
                case "FMAP0I20.IMG": return 20; // Sentinel
                case "FMAP0I21.IMG": return 21; // Anticlere
                case "FMAP0I22.IMG": return 22; // Lainlyn
                case "FMAP0I23.IMG": return 23; // Wayrest
                case "FMAP0I26.IMG": return 26; // Orsinium Area
                case "FMAP0I32.IMG": return 32; // Northmoor
                case "FMAP0I33.IMG": return 33; // Menevia
                case "FMAP0I34.IMG": return 34; // Alcaire
                case "FMAP0I35.IMG": return 35; // Koegria
                case "FMAP0I36.IMG": return 36; // Bhoriane
                case "FMAP0I37.IMG": return 37; // Kambria
                case "FMAP0I38.IMG": return 38; // Phrygias
                case "FMAP0I39.IMG": return 39; // Urvaius
                case "FMAP0I40.IMG": return 40; // Ykalon
                case "FMAP0I41.IMG": return 41; // Daenia
                case "FMAP0I42.IMG": return 42; // Shalgora
                case "FMAP0I43.IMG": return 43; // Abibon-Gora
                case "FMAP0I44.IMG": return 44; // Kairou
                case "FMAP0I45.IMG": return 45; // Pothago
                case "FMAP0I46.IMG": return 46; // Myrkwasa
                case "FMAP0I47.IMG": return 47; // Ayasofya
                case "FMAP0I48.IMG": return 48; // Tigonus
                case "FMAP0I49.IMG": return 49; // Kozanset
                case "FMAP0I50.IMG": return 50; // Satakalaam
                case "FMAP0I51.IMG": return 51; // Totambu
                case "FMAP0I52.IMG": return 52; // Mournoth
                case "FMAP0I53.IMG": return 53; // Ephesus
                case "FMAP0I54.IMG": return 54; // Santaki
                case "FMAP0I55.IMG": return 55; // Antiphyllos
                case "FMAP0I56.IMG": return 56; // Bergama
                case "FMAP0I57.IMG": return 57; // Gavaudon
                case "FMAP0I58.IMG": return 58; // Tulune
                case "FMAP0I59.IMG": return 59; // Glenumbra Moors
                case "FMAP0I60.IMG": return 60; // Ilessan Hills
                case "FMAP0I61.IMG": return 61; // Cybiades
                default: return -1;
            }
        }

        // Gets scale of region map
        protected virtual float GetRegionMapScale(int region)
        {
            if (region == 19) // Betony Region Index Value
                return 4f;
            else
                return 1;
        }

        // Populates offset dictionary for aligning top-left of map to map pixel coordinates.
        // Most maps have a 1:1 pixel ratio with map cells. A couple of maps have a larger scale.
        void PopulateRegionOffsetDict()
        {
            offsetLookup = new Dictionary<string, Vector2>();
            offsetLookup.Add("FMAPAI00.IMG", new Vector2(212, 340));
            offsetLookup.Add("FMAPBI00.IMG", new Vector2(322, 340));
            offsetLookup.Add("FMAPAI01.IMG", new Vector2(583, 279));
            offsetLookup.Add("FMAPBI01.IMG", new Vector2(680, 279));
            offsetLookup.Add("FMAPCI01.IMG", new Vector2(583, 340));
            offsetLookup.Add("FMAPDI01.IMG", new Vector2(680, 340));
            offsetLookup.Add("FMAP0I05.IMG", new Vector2(381, 4));
            offsetLookup.Add("FMAP0I09.IMG", new Vector2(525, 114));
            offsetLookup.Add("FMAP0I11.IMG", new Vector2(437, 340));
            offsetLookup.Add("FMAPAI16.IMG", new Vector2(578, 0));
            offsetLookup.Add("FMAPBI16.IMG", new Vector2(680, 0));
            offsetLookup.Add("FMAPCI16.IMG", new Vector2(578, 52));
            offsetLookup.Add("FMAPDI16.IMG", new Vector2(680, 52));
            offsetLookup.Add("FMAP0I17.IMG", new Vector2(39, 106));
            offsetLookup.Add("FMAP0I18.IMG", new Vector2(20, 29));
            offsetLookup.Add("FMAP0I19.IMG", new Vector2(80, 123));     // Betony scale different
            offsetLookup.Add("FMAP0I20.IMG", new Vector2(217, 293));
            offsetLookup.Add("FMAP0I21.IMG", new Vector2(263, 79));
            offsetLookup.Add("FMAP0I22.IMG", new Vector2(548, 219));
            offsetLookup.Add("FMAP0I23.IMG", new Vector2(680, 146));
            offsetLookup.Add("FMAP0I26.IMG", new Vector2(680, 80));
            offsetLookup.Add("FMAP0I32.IMG", new Vector2(41, 0));
            offsetLookup.Add("FMAP0I33.IMG", new Vector2(660, 101));
            offsetLookup.Add("FMAP0I34.IMG", new Vector2(578, 40));
            offsetLookup.Add("FMAP0I35.IMG", new Vector2(525, 3));
            offsetLookup.Add("FMAP0I36.IMG", new Vector2(440, 40));
            offsetLookup.Add("FMAP0I37.IMG", new Vector2(448, 0));
            offsetLookup.Add("FMAP0I38.IMG", new Vector2(366, 0));
            offsetLookup.Add("FMAP0I39.IMG", new Vector2(300, 8));
            offsetLookup.Add("FMAP0I40.IMG", new Vector2(202, 0));
            offsetLookup.Add("FMAP0I41.IMG", new Vector2(223, 6));
            offsetLookup.Add("FMAP0I42.IMG", new Vector2(148, 76));
            offsetLookup.Add("FMAP0I43.IMG", new Vector2(15, 340));
            offsetLookup.Add("FMAP0I44.IMG", new Vector2(61, 340));
            offsetLookup.Add("FMAP0I45.IMG", new Vector2(86, 338));
            offsetLookup.Add("FMAP0I46.IMG", new Vector2(132, 340));
            offsetLookup.Add("FMAP0I47.IMG", new Vector2(344, 309));
            offsetLookup.Add("FMAP0I48.IMG", new Vector2(381, 251));
            offsetLookup.Add("FMAP0I49.IMG", new Vector2(553, 255));
            offsetLookup.Add("FMAP0I50.IMG", new Vector2(661, 217));
            offsetLookup.Add("FMAP0I51.IMG", new Vector2(672, 275));
            offsetLookup.Add("FMAP0I52.IMG", new Vector2(680, 256));
            offsetLookup.Add("FMAP0I53.IMG", new Vector2(680, 340));
            offsetLookup.Add("FMAP0I54.IMG", new Vector2(491, 340));
            offsetLookup.Add("FMAP0I55.IMG", new Vector2(293, 340));
            offsetLookup.Add("FMAP0I56.IMG", new Vector2(263, 340));
            offsetLookup.Add("FMAP0I57.IMG", new Vector2(680, 157));
            offsetLookup.Add("FMAP0I58.IMG", new Vector2(17, 53));
            offsetLookup.Add("FMAP0I59.IMG", new Vector2(0, 0));        // Glenumbra Moors correct at 0,0
            offsetLookup.Add("FMAP0I60.IMG", new Vector2(107, 11));
            offsetLookup.Add("FMAP0I61.IMG", new Vector2(255, 275));    // Cybiades
        }

        void UpdatePlayerTravelDotsTexture()
        {
            /*DFPosition playerWorldPos = TravelTimeCalculator.GetPlayerTravelPosition();
            if (previousPlayerPosition.Y == playerWorldPos.Y && previousPlayerPosition.X == playerWorldPos.X)
            {
                previousPlayerPosition = ConvertDFPosToExactPixelPos(previousPlayerPosition);
                nextPlayerPosition.Y = previousPlayerPosition.Y;
                nextPlayerPosition.X = previousPlayerPosition.X;
                destinationPosition.Y = previousPlayerPosition.Y;
                destinationPosition.X = previousPlayerPosition.X;
                playerPosAlreadyConverted = true;
            }

            // Get map and dimensions
            string mapName = selectedRegionMapNames[mapIndex];
            Vector2 origin = offsetLookup[mapName];
            int originX = (int)origin.x;
            int originY = (int)origin.y;
            int width = (int)regionTextureOverlayPanelRect.width;
            int height = (int)regionTextureOverlayPanelRect.height;
            scale = GetRegionMapScale(selectedRegion);
            Array.Clear(travelPathPixelBuffer, 0, travelPathPixelBuffer.Length);

            int widthMulti5 = width * 5;

            /*int dottedLineCounter = 0;
            foreach (DFPosition pixelPos in currentTravelLinePositionsList)
            {
                dottedLineCounter++;
                if (dottedLineCounter % 4 == 0)
                {
                    DrawPathLine(pixelPos, widthMulti5, blueColor, ref travelPathPixelBuffer);
                }
            }*/

            /*if ((previousPlayerPosition.Y != destinationPosition.Y) || (previousPlayerPosition.X != destinationPosition.X))
            {
                int dottedLineCounter = 0;
                foreach (DFPosition pixelPos in followingTravelLinePositionsList)
                {
                    dottedLineCounter++;
                    if (dottedLineCounter >= 0 && dottedLineCounter <= 4)
                    {
                        DrawPathLine(pixelPos, widthMulti5, blueColor, ref travelPathPixelBuffer);
                    }
                    else if (dottedLineCounter >= 12)
                    {
                        dottedLineCounter = 0;
                    }
                }

                // Draw Classic Fallout system "Destination Crosshair" 15x15, where the player last clicked
                DrawDestinationCrosshair(destinationPosition, widthMulti5, redColor, ref travelPathPixelBuffer);
            }

            // Draw "Player Position Crosshair" where the player is meant to currently be
            DrawPlayerPosition(previousPlayerPosition, widthMulti5, whiteColor, ref travelPathPixelBuffer);

            // Apply updated color array to texture
            travelPathTexture.SetPixels32(travelPathPixelBuffer);
            travelPathTexture.Apply();

            // Present texture
            travelPathOverlayPanel.BackgroundTexture = travelPathTexture;*/
        }

        /*
        private void InspectChestButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            CloseWindow();
            if (chest.HasBeenInspected) { } // Do nothing, will likely change this eventually, so reinspection/rerolling for inspection results is possible at some cost or something.
            else
            {
                LockedLootContainersMain.ApplyInspectionCosts();
                chest.RecentInspectValues = LockedLootContainersMain.GetInspectionValues(chest);
                chest.HasBeenInspected = true;
            }
            InspectionInfoWindow inspectionInfoWindow = new InspectionInfoWindow(DaggerfallUI.UIManager, chest);
            DaggerfallUI.UIManager.PushWindow(inspectionInfoWindow);
        }

        private void AttemptLockpickButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            CloseWindow();
            if (chest != null)
            {
                DaggerfallAudioSource dfAudioSource = chest.GetComponent<DaggerfallAudioSource>();
                ItemCollection closedChestLoot = chest.AttachedLoot;
                Transform closedChestTransform = chest.transform;
                Vector3 pos = chest.transform.position;

                LockedLootContainersMain.IsThisACrime(ChestInteractionType.Lockpick);

                if (chest.IsLockJammed)
                {
                    DaggerfallUI.AddHUDText(LockedLootContainersMain.GetLockAlreadyJammedText(), 2f);
                    if (dfAudioSource != null && !dfAudioSource.IsPlaying())
                        dfAudioSource.AudioSource.PlayOneShot(LockedLootContainersMain.GetLockAlreadyJammedClip(), UnityEngine.Random.Range(0.9f, 1.42f) * DaggerfallUnity.Settings.SoundVolume);
                }
                else if (LockedLootContainersMain.LockPickChance(chest))
                {
                    chest.PicksAttempted++;
                    LockedLootContainersMain.ApplyLockPickAttemptCosts();

                    DaggerfallLoot openChestLoot = null;
                    if (LockedLootContainersMain.ChestGraphicType == 0) // Use sprite based graphics for chests
                    {
                        int spriteID = closedChestLoot.Count <= 0 ? LockedLootContainersMain.OpenEmptyChestSpriteID : LockedLootContainersMain.OpenFullChestSpriteID;
                        openChestLoot = GameObjectHelper.CreateLootContainer(LootContainerTypes.Nothing, InventoryContainerImages.Chest, pos, closedChestTransform.parent, spriteID, 0, DaggerfallUnity.NextUID, null, false);
                        openChestLoot.gameObject.name = GameObjectHelper.GetGoFlatName(spriteID, 0);
                        openChestLoot.Items.TransferAll(closedChestLoot); // Transfers items from closed chest's items to the new open chest's item collection.
                        GameObject.Destroy(openChestLoot.GetComponent<SerializableLootContainer>());
                    }
                    else // Use 3D models for chests
                    {
                        GameObject usedModelPrefab = null;
                        int modelID = 0;
                        if (closedChestLoot.Count <= 0) { usedModelPrefab = (LockedLootContainersMain.ChestGraphicType == 1) ? LockedLootContainersMain.Instance.LowPolyOpenEmptyChestPrefab : LockedLootContainersMain.Instance.HighPolyOpenEmptyChestPrefab; modelID = LockedLootContainersMain.OpenEmptyChestModelID; }
                        else { usedModelPrefab = (LockedLootContainersMain.ChestGraphicType == 1) ? LockedLootContainersMain.Instance.LowPolyOpenFullChestPrefab : LockedLootContainersMain.Instance.HighPolyOpenFullChestPrefab; modelID = LockedLootContainersMain.OpenFullChestModelID; }
                        GameObject chestGo = GameObjectHelper.InstantiatePrefab(usedModelPrefab, GameObjectHelper.GetGoModelName((uint)modelID), closedChestTransform.parent, pos);
                        chestGo.transform.rotation = chest.gameObject.transform.rotation;
                        Collider col = chestGo.AddComponent<BoxCollider>();
                        openChestLoot = chestGo.AddComponent<DaggerfallLoot>();
                        LockedLootContainersMain.ToggleChestShadowsOrCollision(chestGo);
                        if (openChestLoot)
                        {
                            openChestLoot.ContainerType = LootContainerTypes.Nothing;
                            openChestLoot.ContainerImage = InventoryContainerImages.Chest;
                            openChestLoot.LoadID = DaggerfallUnity.NextUID;
                            openChestLoot.TextureRecord = modelID;
                            openChestLoot.Items.TransferAll(closedChestLoot); // Transfers items from closed chest's items to the new open chest's item collection.
                        }
                    }

                    // Show success and play unlock sound
                    DaggerfallUI.AddHUDText(LockedLootContainersMain.GetLockPickSuccessText(), 3f);
                    if (dfAudioSource != null)
                        AudioSource.PlayClipAtPoint(LockedLootContainersMain.GetLockpickSuccessClip(), chest.gameObject.transform.position, UnityEngine.Random.Range(1.5f, 2.31f) * DaggerfallUnity.Settings.SoundVolume);

                    UnityEngine.Object.Destroy(LockedLootContainersMain.ChestObjRef); // Remove closed chest from scene.
                    LockedLootContainersMain.ChestObjRef = null;
                }
                else
                {
                    chest.PicksAttempted++; // Increase picks attempted counter by 1 on the chest.
                    LockedLootContainersMain.ApplyLockPickAttemptCosts();
                    int mechDamDealt = LockedLootContainersMain.DetermineDamageToLockMechanism(chest);

                    if (LockedLootContainersMain.DoesLockJam(chest, mechDamDealt))
                    {
                        DaggerfallUI.AddHUDText(LockedLootContainersMain.GetJammedLockText(), 3f);
                        if (dfAudioSource != null)
                            AudioSource.PlayClipAtPoint(LockedLootContainersMain.GetLockpickJammedClip(), chest.gameObject.transform.position, UnityEngine.Random.Range(8.2f, 9.71f) * DaggerfallUnity.Settings.SoundVolume);
                    }
                    else
                    {
                        DaggerfallUI.AddHUDText(LockedLootContainersMain.GetLockPickAttemptText(), 2f);
                        if (dfAudioSource != null && !dfAudioSource.IsPlaying())
                            dfAudioSource.AudioSource.PlayOneShot(LockedLootContainersMain.GetLockpickAttemptClip(), UnityEngine.Random.Range(1.2f, 1.91f) * DaggerfallUnity.Settings.SoundVolume);
                    }
                }
            }
            else
            {
                DaggerfallUI.AddHUDText("ERROR: Chest Was Found As Null.", 3f);
            }
        }*/

        private void ExitButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            CloseWindow();
        }
    }
}
