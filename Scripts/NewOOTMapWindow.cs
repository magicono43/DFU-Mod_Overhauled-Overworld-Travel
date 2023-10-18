using UnityEngine;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Entity;
using OverhauledOverworldTravel;
using System;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallWorkshop.Utility.AssetInjection;
using System.IO;
using DaggerfallConnect.Arena2;
using System.Linq;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.Utility;

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
        public static Color32 dimRedColor = new Color32(255, 0, 0, 85);
        public static Color32 dimBlackColor = new Color32(0, 0, 0, 62); // Likely make a setting for this, and many of the color values honestly, later on.
        public static Color32 emptyColor = new Color32(0, 0, 0, 0);

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
        Texture2D regionBordersTexture;
        Texture2D regionBitmapColorsTexture;
        Texture2D unexploredAreasMapTexture;

        Texture2D borderButtonTexture;

        Panel worldMapPanel;

        Panel regionBordersOverlayPanel;

        Panel leftButtonsPanel;
        Panel rightButtonsPanel;

        Panel locationDotOverlayPanel;
        Texture2D locationDotTexture;
        Color32[] locationDotPixelBuffer;

        Panel travelPathOverlayPanel;
        Texture2D travelPathTexture;
        Color32[] travelPathPixelBuffer;

        Panel mouseCursorHitboxOverlayPanel;
        Texture2D mouseCursorHitboxTexture;
        Color32[] mouseCursorHitboxPixelBuffer;

        Panel fogOfWarOverlayPanel;
        Texture2D fogOfWarTexture;
        Color32[] fogOfWarPixelBuffer;

        Panel searchHighlightOverlayPanel;
        Texture2D searchHighlightTexture;
        Color32[] searchHighlightPixelBuffer;

        Color32[] regionColorsBitmap;
        Color32[] unexploredAreasColorMap;

        TextLabel regionSelectInfoLabel;

        TextLabel regionLabel;
        TextLabel firstDebugLabel;
        TextLabel secondDebugLabel;
        TextLabel thirdDebugLabel;

        #endregion

        int currentZoom = 1;
        Vector2 zoomPosition = Vector2.zero;
        Vector2 zoomOffset = Vector2.zero;
        Vector2 lastMousePos = Vector2.zero;
        Vector2 lastClickedPos = Vector2.zero;

        bool autoCenterViewOnPlayer = false;

        Color32[] locationPixelColors;

        Dictionary<string, Vector2> offsetLookup = new Dictionary<string, Vector2>();

        int[,] exploredPixelArray = new int[1000, 500];

        bool regionSelectionMode = false;

        WorldTime worldTimer;
        DaggerfallDateTime dateTime;
        string clockDisplayString = "";
        ulong initialDateTimeInSeconds = 0;
        ulong dateTimeInSeconds = 0;
        public static bool mapTimeHasChanged = false;
        protected Button mapClockButton;
        protected Rect mapClockRect = new Rect(0, 0, 90, 11);
        protected TextLabel mapClockText;

        protected Button stopTravelButton;
        protected Rect stopTravelRect = new Rect(0, 0, 45, 11);

        protected Button startFastTravelButton;
        protected Rect startFastTravelRect = new Rect(0, 0, 45, 11);

        Color32 textShadowColor = new Color32(0, 0, 0, 255);
        Vector2 textShadowPosition = new Vector2(0.60f, 0.60f);

        public PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

        public static DFPosition previousPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
        public static DFPosition nextPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
        public static DFPosition destinationPosition = TravelTimeCalculator.GetPlayerTravelPosition();

        public static List<DFPosition> currentTravelLinePositionsList = new List<DFPosition>();
        public static List<DFPosition> followingTravelLinePositionsList = new List<DFPosition>();

        public static int previousRegionIndex = -1;
        public static int currentRegionIndex = -1;

        public static bool isPlayerTraveling = false;

        int travelDelayTimer = 0;

        DFPosition endPos = new DFPosition(109, 158);
        public DFPosition EndPos { get { return endPos; } protected internal set { endPos = value; } }

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

            // Set location pixel colors and identify flash color from palette file
            DFPalette colors = new DFPalette();
            if (!colors.Load(Path.Combine(DaggerfallUnity.Instance.Arena2Path, "FMAP_PAL.COL")))
                throw new Exception("DaggerfallTravelMap: Could not load color palette.");

            // Populate Location Pixel Color Values
            locationPixelColors = new Color32[]
            {
                new Color32(colors.GetRed(237), colors.GetGreen(237), colors.GetBlue(237), 255),  //dunglab (R215, G119, B39)
                new Color32(colors.GetRed(240), colors.GetGreen(240), colors.GetBlue(240), 255),  //dungkeep (R191, G87, B27)
                new Color32(colors.GetRed(243), colors.GetGreen(243), colors.GetBlue(243), 255),  //dungruin (R171, G51, B15)
                new Color32(colors.GetRed(246), colors.GetGreen(246), colors.GetBlue(246), 255),  //graveyards (R147, G15, B7)
                new Color32(colors.GetRed(0), colors.GetGreen(0), colors.GetBlue(0), 255),        //coven (R15, G15, B15)
                new Color32(colors.GetRed(53), colors.GetGreen(53), colors.GetBlue(53), 255),     //farms (R165, G100, B70)
                new Color32(colors.GetRed(51), colors.GetGreen(51), colors.GetBlue(51), 255),     //wealthy (R193, G133, B100)
                new Color32(colors.GetRed(55), colors.GetGreen(55), colors.GetBlue(55), 255),     //poor (R140, G86, B55)
                new Color32(colors.GetRed(96), colors.GetGreen(96), colors.GetBlue(96), 255),     //temple (R176, G205, B255)
                new Color32(colors.GetRed(101), colors.GetGreen(101), colors.GetBlue(101), 255),  //cult (R68, G124, B192)
                new Color32(colors.GetRed(39), colors.GetGreen(39), colors.GetBlue(39), 255),     //tavern (R126, G81, B89)
                new Color32(colors.GetRed(33), colors.GetGreen(33), colors.GetBlue(33), 255),     //city (R220, G177, B177)
                new Color32(colors.GetRed(35), colors.GetGreen(35), colors.GetBlue(35), 255),     //hamlet (R188, G138, B138)
                new Color32(colors.GetRed(37), colors.GetGreen(37), colors.GetBlue(37), 255),     //village (R155, G105, B106)
            };

            // Fill "fog of war" tracking array with empty values initially
            for (int x = 0; x < 1000; x++)
            {
                for (int y = 0; y < 500; y++)
                {
                    exploredPixelArray[x, y] = 0; // Set the initial value for each pixel
                }
            }

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

            // Overlay for the map region borders panel
            regionBordersOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel);
            regionBordersOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            regionBordersOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;
            regionBordersOverlayPanel.Size = new Vector2(1000, 500);
            regionBordersOverlayPanel.AutoSize = AutoSizeModes.None;
            regionBordersOverlayPanel.BackgroundColor = ScreenDimColor;
            regionBordersOverlayPanel.BackgroundTexture = regionBordersTexture;
            regionBordersOverlayPanel.Enabled = false;

            // Overlay for the player travel path panel
            travelPathOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel); // May have to make the Parent panel this panel's parent similar to the worldMapPanel, will see.
            travelPathOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            travelPathOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;
            //travelPathOverlayPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            // Setup pixel buffer and texture for player travel path
            travelPathPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            travelPathTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            travelPathTexture.filterMode = FilterMode.Point;

            // Overlay for the map locations panel
            locationDotOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel);
            locationDotOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            locationDotOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;
            //locationDotOverlayPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            locationDotPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            locationDotTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            locationDotTexture.filterMode = FilterMode.Point;

            // Overlay for the mouse cursor hitbox panel
            mouseCursorHitboxOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel);
            mouseCursorHitboxOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mouseCursorHitboxOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;

            // Setup pixel buffer and texture for mouse cursor hitbox area
            mouseCursorHitboxPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            mouseCursorHitboxTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            mouseCursorHitboxTexture.filterMode = FilterMode.Point;

            // Overlay for the fog of war panel
            fogOfWarOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel);
            fogOfWarOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            fogOfWarOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;

            // Setup pixel buffer and texture for map fog of war
            fogOfWarPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            fogOfWarTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            fogOfWarTexture.filterMode = FilterMode.Point;

            // Overlay for the search highlight panel
            searchHighlightOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel);
            searchHighlightOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            searchHighlightOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;

            // Setup pixel buffer and texture for search highlight area
            searchHighlightPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            searchHighlightTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            searchHighlightTexture.filterMode = FilterMode.Point;

            // Panel housing the button bar on the left of the screen
            leftButtonsPanel = DaggerfallUI.AddPanel(new Rect(0, 125, 105, 265), worldMapPanel);
            leftButtonsPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            // Testing First UI Button in left panel
            Button testingUIButton1 = CreateGenericTextButton(new Rect(2, 3, 100, 50), leftButtonsPanel, (int)SoundClips.AnimalPig, "Borders", 3.5f);
            testingUIButton1.OnMouseClick += ToggleRegionBorders_OnMouseClick;

            // Testing Second UI Button in left panel
            Button testingUIButton2 = CreateGenericTextButton(new Rect(2, 55, 100, 50), leftButtonsPanel, (int)SoundClips.AnimalCow, "Locations", 3.5f);
            testingUIButton2.OnMouseClick += ToggleLocationDots_OnMouseClick;

            // Testing Third UI Button in left panel
            Button testingUIButton3 = CreateGenericTextButton(new Rect(2, 107, 100, 50), leftButtonsPanel, (int)SoundClips.AnimalCat, "Zoom In +", 3.0f);
            testingUIButton3.OnMouseClick += ZoomInView_OnMouseClick;

            // Testing Forth UI Button in left panel
            Button testingUIButton4 = CreateGenericTextButton(new Rect(2, 159, 100, 50), leftButtonsPanel, (int)SoundClips.AnimalHorse, "Zoom Out -", 3.0f);
            testingUIButton4.OnMouseClick += ZoomOutView_OnMouseClick;

            // Testing Fifth UI Button in left panel
            Button testingUIButton5 = CreateGenericTextButton(new Rect(2, 211, 100, 50), leftButtonsPanel, (int)SoundClips.AnimalDog, "EXIT", 4.0f);
            testingUIButton5.OnMouseClick += ExitMapWindow_OnMouseClick;

            // Panel housing the button bar on the right of the screen
            rightButtonsPanel = DaggerfallUI.AddPanel(new Rect(895, 125, 105, 317), worldMapPanel);
            rightButtonsPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            // Testing First UI Button in right panel
            Button testingUIButton6 = CreateGenericTextButton(new Rect(2, 3, 100, 50), rightButtonsPanel, (int)SoundClips.MakeItem, "Center View", 3.0f);
            testingUIButton6.OnMouseClick += CenterMapView_OnMouseClick;

            // Testing Second UI Button in right panel
            Button testingUIButton7 = CreateGenericTextButton(new Rect(2, 55, 100, 50), rightButtonsPanel, (int)SoundClips.ActivateRatchet, "Center On Player", 3.0f);
            testingUIButton7.OnMouseClick += CenterOnPlayer_OnMouseClick;

            // Testing Third UI Button in right panel
            Button testingUIButton8 = CreateGenericTextButton(new Rect(2, 107, 100, 50), rightButtonsPanel, (int)SoundClips.EnemyBearAttack, "Center On Destination", 3.0f);
            testingUIButton8.OnMouseClick += CenterOnDestination_OnMouseClick;

            // Testing Forth UI Button in right panel
            Button testingUIButton9 = CreateGenericTextButton(new Rect(2, 159, 100, 50), rightButtonsPanel, (int)SoundClips.EnemyWraithAttack, "Fog of War", 3.0f);
            testingUIButton9.OnMouseClick += ToggleFogOfWar_OnMouseClick;

            // Testing Fifth UI Button in right panel
            Button testingUIButton10 = CreateGenericTextButton(new Rect(2, 211, 100, 50), rightButtonsPanel, (int)SoundClips.EnemyScorpionAttack, "Auto Center On Player", 2.7f);
            testingUIButton10.OnMouseClick += ToggleAutoCenterOnPlayer_OnMouseClick;

            // Testing Sixth UI Button in right panel
            Button testingUIButton11 = CreateGenericTextButton(new Rect(2, 263, 100, 50), rightButtonsPanel, (int)SoundClips.EnemyDaedraLordBark, "Region Select Mode", 2.7f);
            testingUIButton11.OnMouseClick += StartRegionSelectionMode_OnMouseClick;
            regionSelectInfoLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.LargeFont, new Vector2(0, 35), "Click On A Region You Want To Search Within", worldMapPanel);
            regionSelectInfoLabel.HorizontalAlignment = HorizontalAlignment.Center;
            regionSelectInfoLabel.TextScale = 2.0f;
            regionSelectInfoLabel.Enabled = false;

            // Tomorrow maybe I'll see about somehow getting the "explored/found" locations be a thing that only shows them when they are already known, rather than always like atm, will see.
            // There really are a good few ways I can approach this. But I also have to keep in mind that keeping the "search" function is also going to be important, so yeah, going to take some thought overall.
            // Should obviously check how the vanilla game/world map keeps track of "hidden" and non-hidden locations and in what data-type it does this.
            // Keep in mind also that I may very likely rework the search function alot. So that the first thing you likely have to specify is the region you want to search in, not only the location name, etc.
            // Might just make it easier on myself and just have the "undiscovered" layer determine what locations actually give the location name when moused over, that way I can just draw all the location
            // dots one time, but still have the same effect in essense. The main issue with that case would be the searching thing, but I suppose I could just do similar and check if the locations with
            // the searched name is in a DFPosition that is undiscovered on the unexploredAreas array, or something like that. Probably would be easier than actually flagging the locations as hidden and such.

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

            // Setup Color array for determining what region the mouse cursor is currently hovering over
            regionColorsBitmap = regionBitmapColorsTexture.GetPixels32();

            // Setup Color array for filling "unexplored" areas of the map with the overlay texture
            unexploredAreasColorMap = unexploredAreasMapTexture.GetPixels32();

            // Zoom Out Button
            Button zoomOutButton = DaggerfallUI.AddButton(new Rect(0, 0, 0, 0), worldMapPanel);
            zoomOutButton.Hotkey = new HotkeySequence(KeyCode.Semicolon, HotkeySequence.KeyModifiers.None);

            // Toggle Left Button Panel
            Button toggleUIButton = DaggerfallUI.AddButton(new Rect(0, 0, 100, 50), worldMapPanel);
            toggleUIButton.BackgroundColor = new Color(0.1f, 0.8f, 0.4f, 0.75f); // For testing purposes
            toggleUIButton.OnMouseClick += ToggleLeftPanel_OnMouseClick;
            toggleUIButton.ClickSound = DaggerfallUI.Instance.GetAudioClip(SoundClips.AmbientCreepyBirdCall);
            TextLabel toggleUIText = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, Vector2.zero, "Toggle UI", toggleUIButton);
            toggleUIText.VerticalAlignment = VerticalAlignment.Middle;
            toggleUIText.HorizontalAlignment = HorizontalAlignment.Center;
            toggleUIText.TextScale = 3.5f;

            // Map Clock Display
            mapClockButton = DaggerfallUI.AddButton(new Rect(350, 10, 260, 35), worldMapPanel);
            mapClockButton.BackgroundColor = new Color(1f, 1f, 1f, 1f); // For testing purposes
            mapClockButton.OnMouseClick += ClockDisplayButton_OnMouseClick;
            mapClockText = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(0, 0), string.Empty, mapClockButton);
            mapClockText.HorizontalAlignment = HorizontalAlignment.Center;
            mapClockText.VerticalAlignment = VerticalAlignment.Bottom;
            mapClockText.ShadowColor = textShadowColor;
            mapClockText.ShadowPosition = textShadowPosition;
            mapClockText.TextScale = 3.00f;
            mapClockText.Text = clockDisplayString;
            mapClockText.TextColor = new Color(0f, 0f, 0f, 1f);
            mapClockButton.Enabled = false; // For testing so I can see stuff better.

            // Stop Travel button
            stopTravelButton = DaggerfallUI.AddButton(new Rect(920, 440, 80, 30), worldMapPanel);
            stopTravelButton.BackgroundColor = new Color(1f, 1f, 1f, 1f); // For testing purposes
            stopTravelButton.OnMouseClick += StopTravelingButton_OnMouseClick;
            TextLabel stopTravelText = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(0, 0), string.Empty, stopTravelButton);
            stopTravelText.HorizontalAlignment = HorizontalAlignment.Center;
            stopTravelText.VerticalAlignment = VerticalAlignment.Bottom;
            stopTravelText.ShadowColor = textShadowColor;
            stopTravelText.ShadowPosition = textShadowPosition;
            stopTravelText.TextScale = 3.00f;
            stopTravelText.Text = "STOP";
            stopTravelText.TextColor = new Color(0f, 0f, 0f, 1f);

            // Start Fast Travel button
            startFastTravelButton = DaggerfallUI.AddButton(new Rect(920, 470, 80, 30), worldMapPanel);
            startFastTravelButton.BackgroundColor = new Color(1f, 1f, 1f, 1f); // For testing purposes
            startFastTravelButton.OnMouseClick += StartFastTravelButton_OnMouseClick;
            TextLabel startFastTravelText = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(0, 0), string.Empty, startFastTravelButton);
            startFastTravelText.HorizontalAlignment = HorizontalAlignment.Center;
            startFastTravelText.VerticalAlignment = VerticalAlignment.Bottom;
            startFastTravelText.ShadowColor = textShadowColor;
            startFastTravelText.ShadowPosition = textShadowPosition;
            startFastTravelText.TextScale = 3.00f;
            startFastTravelText.Text = "TRAVEL";
            startFastTravelText.TextColor = new Color(0f, 0f, 0f, 1f);

            UpdatePlayerTravelDotsTexture();
            TestPlacingDaggerfallLocationDots();
        }

        private Button CreateGenericTextButton(Rect rect, Panel parentPanel, int soundIndex, string text, float textScale)
        {
            Button button = DaggerfallUI.AddButton(rect, parentPanel);
            button.BackgroundColor = new Color(0.1f, 0.5f, 0.8f, 0.75f);
            button.ClickSound = DaggerfallUI.Instance.GetAudioClip((SoundClips)soundIndex);
            TextLabel textLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, Vector2.zero, text, button);
            textLabel.VerticalAlignment = VerticalAlignment.Middle;
            textLabel.HorizontalAlignment = HorizontalAlignment.Center;
            textLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            textLabel.WrapText = true;
            textLabel.WrapWords = true;
            textLabel.MaxWidth = (int)rect.width;
            textLabel.TextScale = textScale;

            return button;
        }

        private void ClockDisplayButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            // Does nothing for now but make a click sound, maybe later it can change the time display setting or something?
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        private void StopTravelingButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            // Stops travel on the map-screen and sets the destination as the last pixel the player was at on the map when it was clicked.
            isPlayerTraveling = false;
            travelDelayTimer = 0;

            currentTravelLinePositionsList = new List<DFPosition>();
            followingTravelLinePositionsList = new List<DFPosition>();
            destinationPosition.Y = previousPlayerPosition.Y;
            destinationPosition.X = previousPlayerPosition.X;
            nextPlayerPosition.Y = previousPlayerPosition.Y;
            nextPlayerPosition.X = previousPlayerPosition.X;

            UpdatePlayerTravelDotsTexture();

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        private void StartFastTravelButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            // Stops travel on the map-screen and performs the necessary actions to bring the player to that current pixel in the game-world, I.E. fast travels to it.
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            CloseWindow();
        }

        protected virtual void LoadTextures()
        {
            baseTexture = OOTMain.Instance.PrimaryWorldMapTexture;
            backgroundTexture = OOTMain.Instance.BackgroundMapFillerTexture;
            heightMapTexture = OOTMain.Instance.WorldHeightMapTexture;
            regionBordersTexture = OOTMain.Instance.RegionBordersMapTexture;
            regionBitmapColorsTexture = OOTMain.Instance.RegionBitmapColorTexture;
            unexploredAreasMapTexture = OOTMain.Instance.UnexploredMapOverlayTexture;

            borderButtonTexture = OOTMain.Instance.BorderToggleButtonTexture;
        }

        public override void OnPush()
        {
            base.OnPush();

            previousPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
            nextPlayerPosition = TravelTimeCalculator.GetPlayerTravelPosition();
            destinationPosition = TravelTimeCalculator.GetPlayerTravelPosition();
            currentTravelLinePositionsList = new List<DFPosition>();
            followingTravelLinePositionsList = new List<DFPosition>();

            regionSelectionMode = false;

            isPlayerTraveling = false;
            mapTimeHasChanged = false;

            worldTimer = GameObject.Find("DaggerfallUnity").GetComponent<WorldTime>();
            dateTime = worldTimer.DaggerfallDateTime.Clone();
            initialDateTimeInSeconds = dateTime.ToSeconds();
            dateTimeInSeconds = dateTime.ToSeconds();
            clockDisplayString = GetTimeMode(dateTime.Hour, dateTime.Minute) + " , " + dateTime.DayName + " the " + (dateTime.Day + 1) + GetSuffix(dateTime.Day + 1);
        }

        public override void OnPop()
        {
            base.OnPop();

            regionSelectionMode = false;

            isPlayerTraveling = false;
            mapTimeHasChanged = false;

            performFastTravel();
        }

        public override void Update()
        {
            base.Update();

            if (mapTimeHasChanged)
            {
                dateTime.FromSeconds(dateTimeInSeconds);
                clockDisplayString = GetTimeMode(dateTime.Hour, dateTime.Minute) + " , " + dateTime.DayName + " the " + (dateTime.Day + 1) + GetSuffix(dateTime.Day + 1);
                mapClockText.Text = clockDisplayString;
                mapTimeHasChanged = false;
            }

            if ((previousPlayerPosition.Y != destinationPosition.Y) || (previousPlayerPosition.X != destinationPosition.X))
            {
                isPlayerTraveling = true;
            }
            else
            {
                isPlayerTraveling = false;
                followingTravelLinePositionsList = new List<DFPosition>();
            }

            if (isPlayerTraveling)
            {
                ++travelDelayTimer;
                dateTimeInSeconds += 50;
                mapTimeHasChanged = true;

                if (travelDelayTimer >= 2)
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
                        followingTravelLinePositionsList.Add(currentTravelLinePositionsList[0]);
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

            if (autoCenterViewOnPlayer && currentZoom > 1)
            {
                ZoomMapTexture(true, false, false, true);
            }    

            if (InputManager.Instance.GetMouseButtonUp(1))
            {
                // Ensure clicks are inside map texture
                if (currentMousePos.x < 0 || currentMousePos.x > mainMapRect.width || currentMousePos.y < 0 || currentMousePos.y > mainMapRect.height)
                    return;

                zoomPosition = currentMousePos;

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) // Zoom out to mouse position
                {
                    ZoomMapTexture(false, false, false);
                }
                else // Zoom to mouse position
                {
                    ZoomMapTexture(false, true, false);
                }
            }
            else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentZoom > 1)
            {
                // Ensure clicks are inside map texture
                if (currentMousePos.x < 0 || currentMousePos.x > mainMapRect.width || currentMousePos.y < 0 || currentMousePos.y > mainMapRect.height)
                    return;

                // Scrolling while zoomed in
                zoomPosition = currentMousePos;
                ZoomMapTexture(true, false, false);
            }

            if (Input.GetKeyUp(KeyCode.RightBracket)) // Just temporary way to toggle region borders, plan to have a better hotkey later, as well as a proper UI button for this.
            {
                if (regionBordersOverlayPanel.Enabled == false)
                    regionBordersOverlayPanel.Enabled = true;
                else
                    regionBordersOverlayPanel.Enabled = false;
            }
        }

        // Handle clicks on the world map panel
        void ClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            Rect mainMapRect = sender.Rectangle;

            // Ensure clicks are inside region texture
            if (position.x < 0 || position.x > mainMapRect.width || position.y < 0 || position.y > mainMapRect.height)
                return;

            // Ignore clicks that are within the screen-space that these buttons occupy while a region is selected. Needed to use "sender.MousePosition" due to the "position" being post-scaling value.
            if ((leftButtonsPanel.Rectangle.Contains(sender.MousePosition) && leftButtonsPanel.Enabled == true) || rightButtonsPanel.Rectangle.Contains(sender.MousePosition) && rightButtonsPanel.Enabled == true)
                return;

            if (regionSelectionMode)
            {
                int usedPosX = (int)((position.x + (zoomOffset.x * currentZoom)) / currentZoom);
                int usedPosY = (int)((position.y + (zoomOffset.y * currentZoom)) / currentZoom);
                int regionColorIndex = -1;
                int flippedY = (int)(500 - usedPosY - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
                int bitMapPos = (int)(flippedY * 1000 + usedPosX);

                if (regionColorsBitmap[bitMapPos].r > 0) { regionColorIndex = regionColorsBitmap[bitMapPos].r - 1; }

                if ((bitMapPos >= 0 || bitMapPos < regionColorsBitmap.Length) && (regionColorIndex >= 0 || regionColorIndex < DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount))
                {
                    string regionName = DaggerfallUnity.ContentReader.MapFileReader.GetRegionName(regionColorIndex);
                    PromptLocationSearch(regionColorIndex, regionName);
                }
                // code here
            }

            //EndPos = new DFPosition((int)position.x, (int)position.y);
            EndPos = new DFPosition((int)((position.x + (zoomOffset.x * currentZoom)) / currentZoom), (int)((position.y + (zoomOffset.y * currentZoom)) / currentZoom));

            destinationPosition = EndPos;
            currentTravelLinePositionsList = FindPixelsBetweenPlayerAndDest();
            if (currentTravelLinePositionsList.Count > 0)
            {
                nextPlayerPosition.Y = currentTravelLinePositionsList[0].Y;
                nextPlayerPosition.X = currentTravelLinePositionsList[0].X;
                dateTimeInSeconds += 25;
                mapTimeHasChanged = true;
            }

            // Play distinct sound just for testing right now.
            DaggerfallUI.Instance.PlayOneShot(DaggerfallUI.Instance.GetAudioClip(SoundClips.PageTurn));

            Vector2 clickedPos = sender.ScaledMousePosition;
            lastClickedPos = clickedPos;
            Debug.LogFormat("Clicked This Spot: x:{0} y:{1}", clickedPos.x, clickedPos.y);

            UpdatePlayerTravelDotsTexture();
        }

        void UpdatePlayerTravelDotsTexture()
        {
            int width = 1000;
            int height = 500;
            Array.Clear(travelPathPixelBuffer, 0, travelPathPixelBuffer.Length);

            // Fills "fog of war" pixel buffer entirely with black, then later "chip away" this color to hopefully reduce overall operations, will see.
            for (int i = 0; i < fogOfWarPixelBuffer.Length; i++)
            {
                fogOfWarPixelBuffer[i] = unexploredAreasColorMap[i];
            }

            // Dim out the previous revealed areas that the player detection radius is currently not in, using a low alpha value black for the "dimming" effect
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (exploredPixelArray[x, y] == 1)
                    {
                        exploredPixelArray[x, y] = 2;
                    }
                }
            }

            /*int dottedLineCounter = 0;
            foreach (DFPosition pixelPos in currentTravelLinePositionsList)
            {
                dottedLineCounter++;
                if (dottedLineCounter % 4 == 0)
                {
                    DrawPathLine(pixelPos, widthMulti5, blueColor, ref travelPathPixelBuffer);
                }
            }*/

            if ((previousPlayerPosition.Y != destinationPosition.Y) || (previousPlayerPosition.X != destinationPosition.X))
            {
                int dottedLineCounter = 0;
                foreach (DFPosition linePos in followingTravelLinePositionsList)
                {
                    dottedLineCounter++;
                    if (dottedLineCounter >= 0 && dottedLineCounter <= 4)
                    {
                        DrawPathLine(linePos, width, height, blackColor, ref travelPathPixelBuffer);
                    }
                    else if (dottedLineCounter >= 12)
                    {
                        dottedLineCounter = 0;
                    }
                }

                // Draw Classic Fallout system "Destination Crosshair" 15x15, where the player last clicked
                if (lastClickedPos != Vector2.zero)
                    DrawDestinationCrosshair(width, height, redColor, ref travelPathPixelBuffer);
            }

            // Draw "Player Position Crosshair" where the player is meant to currently be
            DrawPlayerCrosshair(width, height, whiteColor, ref travelPathPixelBuffer);

            // Reveal/update areas of the map the player has explored and removed the fog of war from
            int playX = previousPlayerPosition.X;
            int playY = previousPlayerPosition.Y;
            int radius = 22;
            // Attempt to map a filled circle around the player, this will be the fog of war "revealing" area
            for (int y = playY - radius; y <= playY + radius; y++)
            {
                for (int x = playX - radius; x <= playX + radius; x++)
                {
                    // Check if the current pixel is within the circle's bounds
                    if (Mathf.Pow(x - playX, 2) + Mathf.Pow(y - playY, 2) <= radius * radius)
                    {
                        // Ensure the x and y coordinates are within the pixel buffer bounds
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            // Set the pixel color to the specified color
                            exploredPixelArray[x, y] = 1;
                        }
                    }
                }
            }

            // Erase "fog of war" texture from the map, depending on where the player currently is and has been
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (exploredPixelArray[x, y] != 0)
                    {
                        int flippedY = (int)(height - y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
                        int pixelPos = (int)(flippedY * width + x);

                        if (exploredPixelArray[x, y] == 2)
                            fogOfWarPixelBuffer[pixelPos] = dimBlackColor;
                        else
                            fogOfWarPixelBuffer[pixelPos] = emptyColor;
                    }
                }
            }

            // Apply updated color array to texture
            travelPathTexture.SetPixels32(travelPathPixelBuffer);
            travelPathTexture.Apply();
            fogOfWarTexture.SetPixels32(fogOfWarPixelBuffer);
            fogOfWarTexture.Apply();

            // Present texture
            travelPathOverlayPanel.BackgroundTexture = travelPathTexture;
            fogOfWarOverlayPanel.BackgroundTexture = fogOfWarTexture;
        }

        public List<DFPosition> FindPixelsBetweenPlayerAndDest()
        {
            int playerXMapPixel = previousPlayerPosition.X;
            int playerYMapPixel = previousPlayerPosition.Y;
            int endPosXMapPixel = endPos.X;
            int endPosYMapPixel = endPos.Y;

            // Do rest of distance calculation and populating list with pixel values in-between playerPos and destinationPos
            List<DFPosition> pixelsList = new List<DFPosition>();
            int distanceXMapPixels = endPosXMapPixel - playerXMapPixel;
            int distanceYMapPixels = endPosYMapPixel - playerYMapPixel;
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

                pixelPos.Y = playerYMapPixel;
                pixelPos.X = playerXMapPixel;

                pixelsList.Add(pixelPos);

                ++numberOfMovements;
            }

            return pixelsList;
        }

        // perform fast travel actions
        private void performFastTravel() // Maybe work on getting this to function again tomorrow or next time, with the more exact screen pixel positions, will see.
        {
            isPlayerTraveling = false;
            travelDelayTimer = 0;

            currentTravelLinePositionsList = new List<DFPosition>();
            followingTravelLinePositionsList = new List<DFPosition>();
            destinationPosition.Y = previousPlayerPosition.Y;
            destinationPosition.X = previousPlayerPosition.X;
            nextPlayerPosition.Y = previousPlayerPosition.Y;
            nextPlayerPosition.X = previousPlayerPosition.X;

            UpdatePlayerTravelDotsTexture();

            DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();

            //RaiseOnPreFastTravelEvent(); // So for these events, I'm not sure how or if you can "piggy-back" off the existing ones from another class/window, really not sure how that might be done, so for now whatever.

            DFPosition fastTravelPos = previousPlayerPosition;

            // Cache scene first, if fast travelling while on ship.
            if (GameManager.Instance.TransportManager.IsOnShip())
                DaggerfallWorkshop.Game.Serialization.SaveLoadManager.CacheScene(GameManager.Instance.StreamingWorld.SceneName);
            GameManager.Instance.StreamingWorld.RestoreWorldCompensationHeight(0);
            GameManager.Instance.StreamingWorld.TeleportToCoordinates((int)fastTravelPos.X, (int)fastTravelPos.Y, StreamingWorld.RepositionMethods.DirectionFromStartMarker);

            GameManager.Instance.PlayerEntity.CurrentHealth = GameManager.Instance.PlayerEntity.MaxHealth;
            GameManager.Instance.PlayerEntity.CurrentFatigue = GameManager.Instance.PlayerEntity.MaxFatigue;
            if (!GameManager.Instance.PlayerEntity.Career.NoRegenSpellPoints)
                GameManager.Instance.PlayerEntity.CurrentMagicka = GameManager.Instance.PlayerEntity.MaxMagicka;

            DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(dateTimeInSeconds - initialDateTimeInSeconds);

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

            GameManager.Instance.PlayerEntity.RaiseSkills();
            DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack();

            //RaiseOnPostFastTravelEvent();
        }

        private string GetSuffix(int day)
        {
            string suffix = "th";
            if (day == 1 || day == 21)
                suffix = "st";
            else if (day == 2 || day == 22)
                suffix = "nd";
            else if (day == 3 || day == 33)
                suffix = "rd";

            return suffix;
        }

        private string GetTimeMode(int hours, int minute)
        {
            string result;

            if (true) // useTwelveHourClock
            {
                if (hours == 0)
                {
                    result = "12";
                }
                else if (hours >= 13)
                    result = "" + (hours - 12);
                else
                    result = "" + hours;
            }
            else
            {
                //result = "" + hours;
            }

            if (minute < 10)
                result += ":0" + minute;
            else
                result += ":" + minute;

            if (true) // useTwelveHourClock
            {
                if (hours >= 12)
                    result += " PM";
                else
                    result += " AM";
            }

            return result;
        }

        // Will update the text label showing the location name (if any) currently moused over on the map
        void UpdateMouseOverLocationLabel(Vector2 currentMousePos)
        {
            Vector2 testingZoomMousePos = Vector2.zero;
            testingZoomMousePos.x = zoomOffset.x;
            testingZoomMousePos.y = zoomOffset.y;

            Vector2 testingScaledMousePos = new Vector2(currentMousePos.x, currentMousePos.y);
            Vector2 testingMousePos = new Vector2(testingZoomMousePos.x, testingZoomMousePos.y);

            //firstDebugLabel.Text = string.Format("ScaledMousePos: ({0}, {1})", testingScaledMousePos.x, testingScaledMousePos.y);
            //secondDebugLabel.Text = string.Format("ZoomedMousePos: ({0}, {1})", testingMousePos.x, testingMousePos.y);
            //thirdDebugLabel.Text = string.Format("Magnification: {0}x", currentZoom);

            int usedPosX = (int)((currentMousePos.x + (zoomOffset.x * currentZoom)) / currentZoom);
            int usedPosY = (int)((currentMousePos.y + (zoomOffset.y * currentZoom)) / currentZoom);

            Vector2 refCoords = Vector2.zero; // For now, use to determine what "explored/unexplored" map pixel is being checked here.

            ContentReader.MapSummary mapSummary;
            if (DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX, usedPosY, out mapSummary)) { refCoords.x = usedPosX; refCoords.y = usedPosY; }
            else if (DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX, usedPosY - 1, out mapSummary)) { refCoords.x = usedPosX; refCoords.y = usedPosY - 1; }
            else if (DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX + 1, usedPosY - 1, out mapSummary)) { refCoords.x = usedPosX + 1; refCoords.y = usedPosY - 1; }
            else { DaggerfallUnity.Instance.ContentReader.HasLocation(usedPosX + 1, usedPosY, out mapSummary); refCoords.x = usedPosX + 1; refCoords.y = usedPosY; }

            string regionName = DaggerfallUnity.ContentReader.MapFileReader.GetRegionName(mapSummary.RegionIndex);

            string locationName = string.Empty;
            if (fogOfWarOverlayPanel.Enabled == true && exploredPixelArray[(int)refCoords.x, (int)refCoords.y] == 0) { }
            else { locationName = GetLocationNameInCurrentRegion(mapSummary.RegionIndex, mapSummary.MapIndex); }

            int mapPixelID = mapSummary.ID;
            int regionColorIndex = -1;
            int flippedY = (int)(500 - usedPosY - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
            int bitMapPos = (int)(flippedY * 1000 + usedPosX);
            if (locationName == string.Empty) // Keep label from showing up if no valid location is moused over. But do show region name if over a valid region Bitmap color value.
            {
                // Read from the "Red" color value for this part of the Bitmap texture color data to determine what region index value is used.
                if (regionColorsBitmap[bitMapPos].r > 0) { regionColorIndex = regionColorsBitmap[bitMapPos].r - 1; }
                else if (regionColorsBitmap[bitMapPos + 1].r > 0) { regionColorIndex = regionColorsBitmap[bitMapPos + 1].r - 1; }
                else if (regionColorsBitmap[bitMapPos + 1000].r > 0) { regionColorIndex = regionColorsBitmap[bitMapPos + 1000].r - 1; }
                else if (regionColorsBitmap[bitMapPos + 1000 + 1].r > 0) { regionColorIndex = regionColorsBitmap[bitMapPos + 1000 + 1].r - 1; }

                // Get region from bitmap, if any
                if ((bitMapPos >= 0 || bitMapPos < regionColorsBitmap.Length) && (regionColorIndex >= 0 || regionColorIndex < DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount))
                {
                    regionName = DaggerfallUnity.ContentReader.MapFileReader.GetRegionName(regionColorIndex);
                    regionLabel.Text = string.Format("{0}", regionName);
                }
                else
                {
                    regionLabel.Text = string.Empty;
                }
            }
            else
            {
                regionLabel.Text = string.Format("{0} : {1} ({2})", regionName, locationName, mapPixelID);
            }

            // Just put this here for now, since it is a bit easier than a new method entirely, but will almost definitely move this later on.
            searchHighlightOverlayPanel.Enabled = false;
            regionSelectInfoLabel.Enabled = false;
            if (regionSelectionMode)
            {
                Array.Clear(searchHighlightPixelBuffer, 0, searchHighlightPixelBuffer.Length);
                searchHighlightOverlayPanel.Enabled = true;
                regionSelectInfoLabel.Enabled = true;

                if (regionColorsBitmap[bitMapPos].r > 0) { regionColorIndex = regionColorsBitmap[bitMapPos].r; }

                if ((bitMapPos >= 0 || bitMapPos < regionColorsBitmap.Length) && (regionColorIndex >= 0 || regionColorIndex < DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount))
                {
                    for (int y = 0; y < 500; y++)
                    {
                        for (int x = 0; x < 1000; x++)
                        {
                            int flipY = (int)(500 - y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
                            int pixelPos = (int)(flipY * 1000 + x);

                            if (regionColorsBitmap[pixelPos].r == regionColorIndex)
                            {
                                searchHighlightPixelBuffer[pixelPos] = dimRedColor;
                            }
                        }
                    }
                }

                // Apply updated color array to texture
                searchHighlightTexture.SetPixels32(searchHighlightPixelBuffer);
                searchHighlightTexture.Apply();

                // Present texture
                searchHighlightOverlayPanel.BackgroundTexture = searchHighlightTexture;
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

        void ZoomMapTexture(bool scrolling, bool zoomIn, bool forceCenter, bool centerPlayer = false, bool centerDest = false) // Attempt to get some form of zooming to work on the world map.
        {
            int zoomPosX = (int)zoomPosition.x;
            int zoomPosY = (int)zoomPosition.y;
            int originalZoom = currentZoom;
            int zoomFactor = 1;

            if (scrolling)
            {
                zoomFactor = currentZoom;
                if (centerPlayer) { zoomPosX = previousPlayerPosition.X; zoomPosY = previousPlayerPosition.Y; }
                else if (centerDest) { zoomPosX = destinationPosition.X; zoomPosY = destinationPosition.Y; }
            }
            else
            {
                worldMapPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                regionBordersOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                locationDotOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                travelPathOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                mouseCursorHitboxOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                fogOfWarOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                searchHighlightOverlayPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;

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
            int startX = forceCenter ? 500 - zoomWidth : zoomPosX - zoomWidth;
            int startY = forceCenter ? 250 - zoomHeight : height + (-zoomPosY - zoomHeight);

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
            regionBordersOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            regionBordersOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
            locationDotOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            locationDotOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
            travelPathOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            travelPathOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
            mouseCursorHitboxOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            mouseCursorHitboxOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
            fogOfWarOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            fogOfWarOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
            searchHighlightOverlayPanel.BackgroundTextureLayout = BackgroundLayout.Cropped;
            searchHighlightOverlayPanel.BackgroundCroppedRect = worldMapNewRect;
        }

        void TestPlacingDaggerfallLocationDots()
        {
            Array.Clear(locationDotPixelBuffer, 0, locationDotPixelBuffer.Length);

            int width = 1000;
            int height = 500;

            // Plot locations to color array
            /*float scale = 1.0f;
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
            }*/

            // Plot locations to color array
            float scale = 1.0f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = (int)((((height - y - 1) * width) + x) * scale);
                    if (offset >= (width * height))
                        continue;

                    ContentReader.MapSummary summary;
                    if (DaggerfallUnity.Instance.ContentReader.HasLocation(x, y, out summary))
                    {
                        int index = GetPixelColorIndex(summary.LocationType);
                        if (index == -1)
                            continue;
                        else
                        {
                            locationDotPixelBuffer[offset] = locationPixelColors[index];
                        }
                    }
                }
            }

            // Apply updated color array to texture
            locationDotTexture.SetPixels32(locationDotPixelBuffer);
            locationDotTexture.Apply();

            // Present texture
            locationDotOverlayPanel.BackgroundTexture = locationDotTexture;
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

        public void PromptLocationSearch(int regionIndex, string regionName)
        {
            // Open location search pop-up
            string searchPopUpText = "Enter Name Of Location Within, " + regionName + " : ";
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            if (regionIndex >= 0 || regionIndex < DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount)
            {
                // Check if region clicked has any locations that the player knows of, I.E. the fog of war is not hiding them, if it is enabled atleast.
                if (fogOfWarOverlayPanel.Enabled == true && CheckRegionForKnownLocations(regionIndex))
                {
                    DaggerfallInputMessageBox searchPopUp = new DaggerfallInputMessageBox(uiManager, null, 31, searchPopUpText, true, this);
                    searchPopUp.TextPanelDistanceY = 5;
                    searchPopUp.TextBox.WidthOverride = 308;
                    searchPopUp.TextBox.MaxCharacters = 32;
                    searchPopUp.OnGotUserInput += HandleSearchLocationEvent;
                    searchPopUp.Show();
                }
                else if (CheckRegionForKnownLocations(regionIndex))
                {
                    DaggerfallInputMessageBox searchPopUp = new DaggerfallInputMessageBox(uiManager, null, 31, searchPopUpText, true, this);
                    searchPopUp.TextPanelDistanceY = 5;
                    searchPopUp.TextBox.WidthOverride = 308;
                    searchPopUp.TextBox.MaxCharacters = 32;
                    searchPopUp.OnGotUserInput += HandleSearchLocationEvent;
                    searchPopUp.Show();
                }
                else
                {
                    DaggerfallMessageBox invalidRegionPopup = new DaggerfallMessageBox(uiManager, this);
                    invalidRegionPopup.SetText("You Don't Know Any Locations In That Region, Or They Don't Exist.");
                    invalidRegionPopup.Show();
                    invalidRegionPopup.ClickAnywhereToClose = true;
                }
            }
            else
            {
                DaggerfallMessageBox invalidRegionPopup = new DaggerfallMessageBox(uiManager, this);
                invalidRegionPopup.SetText("Error: That Region Does Not Exist");
                invalidRegionPopup.Show();
                invalidRegionPopup.ClickAnywhereToClose = true;
            }
        }

        // Handles events from Find Location pop-up.
        public void HandleSearchLocationEvent(DaggerfallInputMessageBox inputMessageBox, string locationName)
        {
            // Tomorrow likely try to get this search prompt part working, and filtering out locations that are "hidden" by fog of war if it is active, stuff like that, clean-up and testing as well, etc.
            List<DistanceMatch> matching;
            if (FindLocation(locationName, out matching))
            {
                if (matching.Count == 1)
                { //place flashing crosshair over location
                    locationSelected = true;
                    findingLocation = true;
                    StartIdentify();
                    UpdateCrosshair();
                }
                else
                {
                    ShowLocationPicker(matching.ConvertAll(match => match.text).ToArray(), false);
                }
            }
            else
            {
                TextFile.Token[] textTokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(13);
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetTextTokens(textTokens);
                messageBox.ClickAnywhereToClose = true;
                uiManager.PushWindow(messageBox);
                return;
            }
        }

        public bool CheckRegionForKnownLocations(int regionIndex)
        {
            List<int> validLocationsList = new List<int>();
            DFRegion regionData = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegion(regionIndex);
            if (regionData.LocationCount <= 0) { return false; }
            if (regionIndex == 31) { return false; } // Index for "High Rock sea coast" or the "region" that holds the location of the two player boats, as well as the Mantellan Crux story dungeon.

            // Collect all valid locations within this region
            for (int i = 0; i < regionData.LocationCount; i++)
            {
                // Later on will likely want to add more logic depending on various settings/factors. Such as having the fog of war off, but also still have "undiscovered" locations like covens and such, etc.
                if (fogOfWarOverlayPanel.Enabled == true)
                {
                    DFPosition locPos = MapsFile.LongitudeLatitudeToMapPixel(regionData.MapTable[i].Longitude, regionData.MapTable[i].Latitude);

                    if (exploredPixelArray[locPos.X, locPos.Y] == 0) { continue; }
                }
                validLocationsList.Add(i);
            }

            if (validLocationsList.Count > 0) { return true; }
            else { return false; }
        }

        public void DrawPlayerCrosshair(int width, int height, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            int flippedY = (int)(height - previousPlayerPosition.Y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
            int pixelPos = (int)(flippedY * width + previousPlayerPosition.X);

            for (int i = -2; i < 3; i++)
            {
                if (pixelPos + i < 0 || pixelPos + i > pixelBuffer.Length)
                    continue;
                else
                    pixelBuffer[pixelPos + i] = pathColor;
            }
            //for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos - width + i] = pathColor; }

            for (int i = -2; i < 3; i++)
            {
                if (pixelPos + (width * i) < 0 || pixelPos + (width * i) > pixelBuffer.Length)
                    continue;
                else
                    pixelBuffer[pixelPos + (width * i)] = pathColor;
            }
            //for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos + (width * i) - 1] = pathColor; }
        }

        public void DrawPathLine(DFPosition linePos, int width, int height, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            int flippedY = (int)(height - linePos.Y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
            int pixelPos = (int)(flippedY * width + linePos.X);

            for (int i = -1; i < 1; i++)
            {
                if (pixelPos + width + i < 0 || pixelPos + width + i > pixelBuffer.Length)
                    continue;
                else
                    pixelBuffer[pixelPos + width + i] = pathColor;
            }
            for (int i = -1; i < 1; i++)
            {
                if (pixelPos + i < 0 || pixelPos + i > pixelBuffer.Length)
                    continue;
                else
                    pixelBuffer[pixelPos + i] = pathColor;
            }
            //for (int i = -1; i < 2; i++) { pixelBuffer[offset - width + i] = pathColor; }
        }

        public void DrawDestinationCrosshair(int width, int height, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            int flippedY = (int)(height - destinationPosition.Y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
            int pixelPos = (int)(flippedY * width + destinationPosition.X);

            for (int i = -2; i < 3; i++)
            {
                if (pixelPos + i < 0 || pixelPos + i > pixelBuffer.Length)
                    continue;
                else
                    pixelBuffer[pixelPos + i] = pathColor;
            }
            //for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos - width + i] = pathColor; }

            for (int i = -2; i < 3; i++)
            {
                if (pixelPos + (width * i) < 0 || pixelPos + (width * i) > pixelBuffer.Length)
                    continue;
                else
                    pixelBuffer[pixelPos + (width * i)] = pathColor;
            }
            //for (int i = -3; i < 3; i++) { pixelBuffer[pixelPos + (width * i) - 1] = pathColor; }
        }

        public void DrawFogOfWar(DFPosition fogPos, int width, int height, Color32 pathColor, ref Color32[] pixelBuffer)
        {
            int flippedY = (int)(height - fogPos.Y - 1); // To compensate for the pixelBuffer index starting at the opposite part of the screen as the (0, 0) origin for the screen.
            int pixelPos = (int)(flippedY * width + fogPos.X);

            pixelBuffer[pixelPos] = pathColor;
        }

        void DrawMouseCursorHitboxArea(int pixelPos, int width, ref Color32[] pixelBuffer) // Right now assuming 2x2 "pixel" size or 4 area, will need to consider other resolution values later.
        {
            /*
            pixelBuffer[pixelPos] = whiteColor;
            pixelBuffer[pixelPos + 1] = whiteColor;
            pixelBuffer[pixelPos + width] = whiteColor;
            pixelBuffer[pixelPos + width + 1] = whiteColor;
            */
        }

        // Get index to locationPixelColor array or -1 if invalid or filtered
        int GetPixelColorIndex(DFRegion.LocationTypes locationType)
        {
            int index = -1;
            switch (locationType)
            {
                case DFRegion.LocationTypes.DungeonLabyrinth:
                    index = 0;
                    break;
                case DFRegion.LocationTypes.DungeonKeep:
                    index = 1;
                    break;
                case DFRegion.LocationTypes.DungeonRuin:
                    index = 2;
                    break;
                case DFRegion.LocationTypes.Graveyard:
                    index = 3;
                    break;
                case DFRegion.LocationTypes.Coven:
                    index = 4;
                    break;
                case DFRegion.LocationTypes.HomeFarms:
                    index = 5;
                    break;
                case DFRegion.LocationTypes.HomeWealthy:
                    index = 6;
                    break;
                case DFRegion.LocationTypes.HomePoor:
                    index = 7;
                    break;
                case DFRegion.LocationTypes.HomeYourShips:
                    break;
                case DFRegion.LocationTypes.ReligionTemple:
                    index = 8;
                    break;
                case DFRegion.LocationTypes.ReligionCult:
                    index = 9;
                    break;
                case DFRegion.LocationTypes.Tavern:
                    index = 10;
                    break;
                case DFRegion.LocationTypes.TownCity:
                    index = 11;
                    break;
                case DFRegion.LocationTypes.TownHamlet:
                    index = 12;
                    break;
                case DFRegion.LocationTypes.TownVillage:
                    index = 13;
                    break;
                default:
                    break;
            }
            /*if (index < 0)
                return index;
            else if (index < 5 && filterDungeons)
                index = -1;
            else if (index > 4 && index < 8 && filterHomes)
                index = -1;
            else if (index > 7 && index < 10 && filterTemples)
                index = -1;
            else if (index > 9 && index < 14 && filterTowns)
                index = -1;*/
            return index;
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

        private void ToggleLeftPanel_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (leftButtonsPanel.Enabled == true)
                leftButtonsPanel.Enabled = false;
            else
                leftButtonsPanel.Enabled = true;

            if (rightButtonsPanel.Enabled == true)
                rightButtonsPanel.Enabled = false;
            else
                rightButtonsPanel.Enabled = true;
        }

        private void ToggleRegionBorders_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (regionBordersOverlayPanel.Enabled == false)
                regionBordersOverlayPanel.Enabled = true;
            else
                regionBordersOverlayPanel.Enabled = false;
        }

        private void ToggleLocationDots_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (locationDotOverlayPanel.Enabled == false)
                locationDotOverlayPanel.Enabled = true;
            else
                locationDotOverlayPanel.Enabled = false;
        }

        private void ZoomInView_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            ZoomMapTexture(false, true, true);
        }

        private void ZoomOutView_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            ZoomMapTexture(false, false, true);
        }

        private void ExitMapWindow_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            CloseWindow();
        }

        private void CenterMapView_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (currentZoom > 1)
            {
                ZoomMapTexture(true, false, true);
            }
        }

        private void CenterOnPlayer_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (currentZoom > 1)
            {
                ZoomMapTexture(true, false, false, true);
            }
        }

        private void CenterOnDestination_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (currentZoom > 1)
            {
                ZoomMapTexture(true, false, false, false, true);
            }
        }

        private void ToggleFogOfWar_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (fogOfWarOverlayPanel.Enabled == false)
                fogOfWarOverlayPanel.Enabled = true;
            else
                fogOfWarOverlayPanel.Enabled = false;
        }

        private void ToggleAutoCenterOnPlayer_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (autoCenterViewOnPlayer == false)
                autoCenterViewOnPlayer = true;
            else
                autoCenterViewOnPlayer = false;
        }

        private void StartRegionSelectionMode_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (regionSelectionMode == false)
                regionSelectionMode = true;
            else
                regionSelectionMode = false;
        }

        void VariousUsefulNotesAndMethods()
        {
            /*
            // Interkarma's Height Map To Image Extraction Code
            DFPalette colors = new DFPalette();
            if (!colors.Load(Path.Combine(DaggerfallUnity.Instance.Arena2Path, "FMAP_PAL.COL")))
                throw new Exception("DaggerfallTravelMap: Could not load color palette.");
            */

            /*
            // Start Stuff Here
            PakFile climateFile = new PakFile(@"c:\games\daggerfall\arena2\climate.pak");
            WoodsFile woodsFile = new WoodsFile(@"c:\games\daggerfall\arena2\woods.wld", FileUsage.UseMemory, true);
            byte[] climateData = climateFile.Buffer;
            byte[] heightData = woodsFile.Buffer;
            Color32[] combinedColorMap = new Color32[climateData.Length];

            for (int y = 0; y < 500; y++) // Process raw height data into something more visible to humans
            {
                for (int x = 0; x < 1001; x++)
                {
                    //byte h = heightData[y * 1000 + x];
                    byte h = 0; if (x >= 1000) { h = 255; } else { h = heightData[y * 1000 + x]; }
                    byte k = climateData[y * 1001 + x];
                    Color32 c = new Color32(255, 255, 255, 255);
                    int cIn = 0;
                    if (h <= 2) { c = new Color32(colors.GetRed(255), colors.GetGreen(255), colors.GetBlue(255), 255); }
                    else
                    {
                        switch (k)
                        {
                            case 223: // Ocean/Water
                                c = new Color32(colors.GetRed(255), colors.GetGreen(255), colors.GetBlue(255), 255); break;
                            case 224: // Desert South
                                if (h >= 4 && h <= 11) { cIn = 150; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 12 && h <= 16) { cIn = 149; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 17 && h <= 22) { cIn = 148; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 147; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 57) { cIn = 146; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 225: // Hot Desert South-East
                                if (h >= 4 && h <= 11) { cIn = 157; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 12 && h <= 16) { cIn = 156; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 17 && h <= 22) { cIn = 155; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 154; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 57) { cIn = 153; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 226: // Mountains
                                if (h >= 4 && h <= 22) { cIn = 123; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 122; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 34) { cIn = 121; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 35 && h <= 40) { cIn = 120; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 42 && h <= 49) { cIn = 119; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 50 && h <= 57) { cIn = 118; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 59 && h <= 65) { cIn = 117; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 67 && h <= 72) { cIn = 116; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 74 && h <= 80) { cIn = 115; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 82 && h <= 92) { cIn = 114; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 94 && h <= 100) { cIn = 113; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 102 && h <= 109) { cIn = 112; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 227: // Rainforest
                                if (h >= 4 && h <= 16) { cIn = 182; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 17 && h <= 22) { cIn = 184; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 186; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 255) { cIn = 187; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 228: // Swamp
                                if (h >= 4 && h <= 11) { cIn = 138; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 12 && h <= 16) { cIn = 137; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 17 && h <= 22) { cIn = 136; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 135; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 34) { cIn = 134; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 35 && h <= 40) { cIn = 133; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 42 && h <= 57) { cIn = 132; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 255) { cIn = 129; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 229: // Sub Tropical
                                if (h >= 4 && h <= 11) { cIn = 201; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 12 && h <= 16) { cIn = 200; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 17 && h <= 22) { cIn = 199; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 198; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 57) { cIn = 197; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 230: // Woodland Hills
                                if (h >= 4 && h <= 22) { cIn = 171; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 170; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 34) { cIn = 169; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 35 && h <= 40) { cIn = 168; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 42 && h <= 49) { cIn = 167; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 50 && h <= 57) { cIn = 166; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 59 && h <= 65) { cIn = 165; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 67 && h <= 72) { cIn = 164; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 74 && h <= 80) { cIn = 163; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 82 && h <= 92) { cIn = 162; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 94 && h <= 100) { cIn = 161; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 102 && h <= 109) { cIn = 160; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 231: // Temperate Woodland
                                if (h >= 4 && h <= 22) { cIn = 208; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 24 && h <= 27) { cIn = 209; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 34) { cIn = 210; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 35 && h <= 40) { cIn = 211; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 42 && h <= 49) { cIn = 212; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 50 && h <= 57) { cIn = 213; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 59 && h <= 65) { cIn = 214; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 67 && h <= 72) { cIn = 215; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 74 && h <= 80) { cIn = 236; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 82 && h <= 92) { cIn = 235; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 94 && h <= 100) { cIn = 234; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 102 && h <= 109) { cIn = 233; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            case 232: // Haunted Woodland
                                if (h >= 4 && h <= 27) { cIn = 139; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 29 && h <= 34) { cIn = 138; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 35 && h <= 40) { cIn = 137; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 42 && h <= 49) { cIn = 136; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 50 && h <= 57) { cIn = 135; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 59 && h <= 65) { cIn = 134; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 67 && h <= 72) { cIn = 133; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 74 && h <= 80) { cIn = 132; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 82 && h <= 92) { cIn = 131; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else if (h >= 94 && h <= 109) { cIn = 130; c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); }
                                else { c = new Color32(colors.GetRed(cIn), colors.GetGreen(cIn), colors.GetBlue(cIn), 255); } break;
                            default:
                                c = new Color32(255, 255, 255, 255); break;
                        }
                    }
                    combinedColorMap[(499 - y) * 1001 + x] = c; // Texture needs to be flipped vertically to be right way up
                }
            }

            Texture2D combinedTextureMap = new Texture2D(1001, 500);
            combinedTextureMap.SetPixels32(combinedColorMap);
            combinedTextureMap.Apply();
            byte[] climatePngData = combinedTextureMap.EncodeToPNG();
            System.IO.File.WriteAllBytes(@"c:\dfutesting\combinedmapcolor.png", climatePngData);
            // End Stuff Here
            */

            /*
            PakFile climateFile = new PakFile(@"c:\games\daggerfall\arena2\climate.pak");
            byte[] climateData = climateFile.Buffer;
            Color32[] climateColorMap = new Color32[climateData.Length];
            for (int y = 0; y < 500; y++) // Process raw height data into something more visible to humans
            {
                for (int x = 0; x < 1001; x++)
                {
                    byte h = climateData[y * 1001 + x];
                    Color32 c = new Color32(255, 255, 255, 255);
                    if (h == 223)
                        c = new Color32(colors.GetRed(255), colors.GetGreen(255), colors.GetBlue(255), 255);
                    else if (h == 224)
                        c = new Color32(colors.GetRed(153), colors.GetGreen(153), colors.GetBlue(153), 255);
                    else if (h == 225)
                        c = new Color32(colors.GetRed(148), colors.GetGreen(148), colors.GetBlue(148), 255);
                    else if (h == 226)
                        c = new Color32(colors.GetRed(116), colors.GetGreen(116), colors.GetBlue(116), 255);
                    else if (h == 227)
                        c = new Color32(colors.GetRed(186), colors.GetGreen(186), colors.GetBlue(186), 255);
                    else if (h == 228)
                        c = new Color32(colors.GetRed(137), colors.GetGreen(137), colors.GetBlue(137), 255);
                    else if (h == 229)
                        c = new Color32(colors.GetRed(171), colors.GetGreen(171), colors.GetBlue(171), 255);
                    else if (h == 230)
                        c = new Color32(colors.GetRed(200), colors.GetGreen(200), colors.GetBlue(200), 255);
                    else if (h == 231)
                        c = new Color32(colors.GetRed(236), colors.GetGreen(236), colors.GetBlue(236), 255);
                    else if (h == 232)
                        c = new Color32(colors.GetRed(89), colors.GetGreen(89), colors.GetBlue(89), 255);
                    else
                        c = new Color32(h, h, h, 255);
                    climateColorMap[(499 - y) * 1001 + x] = c; // Texture needs to be flipped vertically to be right way up
                }
            }
            Texture2D climateTextureMap = new Texture2D(1001, 500);
            climateTextureMap.SetPixels32(climateColorMap);
            climateTextureMap.Apply();
            byte[] climatePngData = climateTextureMap.EncodeToPNG();
            System.IO.File.WriteAllBytes(@"c:\dfutesting\climatemapcolor.png", climatePngData);

            WoodsFile woodsFile = new WoodsFile(@"c:\games\daggerfall\arena2\woods.wld", FileUsage.UseMemory, true);
            byte[] heightData = woodsFile.Buffer;
            Color32[] colorMap = new Color32[heightData.Length];
            for (int y = 0; y < 500; y++) // Process raw height data into something more visible to humans
            {
                for (int x = 0; x < 1000; x++)
                {
                    byte h = heightData[y * 1000 + x];
                    Color32 c = new Color32(255, 255, 255, 255);
                    if (h <= 2)
                        c = new Color32(colors.GetRed(255), colors.GetGreen(255), colors.GetBlue(255), 255);
                    else if (h >= 4 && h <= 11)
                        c = new Color32(colors.GetRed(182), colors.GetGreen(182), colors.GetBlue(182), 255);
                    else if (h >= 12 && h <= 16)
                        c = new Color32(colors.GetRed(185), colors.GetGreen(185), colors.GetBlue(185), 255);
                    else if (h >= 17 && h <= 22)
                        c = new Color32(colors.GetRed(187), colors.GetGreen(187), colors.GetBlue(187), 255);
                    else if (h >= 24 && h <= 27)
                        c = new Color32(colors.GetRed(188), colors.GetGreen(188), colors.GetBlue(188), 255);
                    else if (h >= 29 && h <= 34)
                        c = new Color32(colors.GetRed(189), colors.GetGreen(189), colors.GetBlue(189), 255);
                    else if (h >= 35 && h <= 40)
                        c = new Color32(colors.GetRed(190), colors.GetGreen(190), colors.GetBlue(190), 255);
                    else if (h >= 42 && h <= 49)
                        c = new Color32(colors.GetRed(191), colors.GetGreen(191), colors.GetBlue(191), 255);
                    else if (h >= 50 && h <= 57)
                        c = new Color32(colors.GetRed(208), colors.GetGreen(208), colors.GetBlue(208), 255);
                    else if (h >= 59 && h <= 65)
                        c = new Color32(colors.GetRed(209), colors.GetGreen(209), colors.GetBlue(209), 255);
                    else if (h >= 67 && h <= 72)
                        c = new Color32(colors.GetRed(210), colors.GetGreen(210), colors.GetBlue(210), 255);
                    else if (h >= 74 && h <= 80)
                        c = new Color32(colors.GetRed(211), colors.GetGreen(211), colors.GetBlue(211), 255);
                    else if (h >= 82 && h <= 92)
                        c = new Color32(colors.GetRed(212), colors.GetGreen(212), colors.GetBlue(212), 255);
                    else if (h >= 94 && h <= 100)
                        c = new Color32(colors.GetRed(213), colors.GetGreen(213), colors.GetBlue(213), 255);
                    else if (h >= 102 && h <= 109)
                        c = new Color32(colors.GetRed(214), colors.GetGreen(214), colors.GetBlue(214), 255);
                    else if (h >= 255)
                        c = new Color32(colors.GetRed(215), colors.GetGreen(215), colors.GetBlue(215), 255);
                    else
                        c = new Color32(h, h, h, 255);
                    colorMap[(499 - y) * 1000 + x] = c; // Texture needs to be flipped vertically to be right way up
                }
            }

            List<Color32> fmapColors = new List<Color32>();
            for (int i = 0; i < 256; i++)
            {
                fmapColors.Add(new Color32(colors.GetRed(i), colors.GetGreen(i), colors.GetBlue(i), 255));
            }

            string colorFilePath = @"c:\dfutesting\fmapColors.txt";

            using (StreamWriter writer = new StreamWriter(colorFilePath))
            {
                writer.WriteLine("Index, Red, Green, Blue");
                for (int k = 0; k < fmapColors.Count; k++)
                {
                    writer.WriteLine($"{k}, {fmapColors[k].r}, {fmapColors[k].g}, {fmapColors[k].b}");
                }
            }

            Console.WriteLine($"Results have been written to {colorFilePath}");

            // Counting Code
            // Use LINQ to count unique values and their occurrences
            var counts = climateData.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            // Count the number of unique values
            int uniqueCount = counts.Count;

            // Calculate the total count of all values
            int totalCount = climateData.Length;

            // Calculate and order the percentage distribution
            var distribution = counts.OrderBy(kvp => kvp.Key)
                                     .Select(kvp => new
                                     {
                                         Value = kvp.Key,
                                         Count = kvp.Value,
                                         Percentage = (double)kvp.Value / totalCount * 100.0
                                     });

            // Specify the path for the output text file
            string filePath = @"c:\dfutesting\output.txt";

            // Write the results to the text file
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Value,Count,Percentage");
                foreach (var item in distribution)
                {
                    writer.WriteLine($"{item.Value}, {item.Count}, {item.Percentage:F2}%");
                }
            }

            Console.WriteLine($"Results have been written to {filePath}");

            // Print the results
            Debug.LogFormat("Number of unique values: {0}", uniqueCount);
            // Counting Code

            Texture2D textureMap = new Texture2D(1000, 500);
            textureMap.SetPixels32(colorMap);
            textureMap.Apply();
            byte[] pngData = textureMap.EncodeToPNG();
            System.IO.File.WriteAllBytes(@"c:\dfutesting\heightmapcolor.png", pngData);
            // Interkarma's Height Map To Image Extraction Code
            */
        }
    }
}
