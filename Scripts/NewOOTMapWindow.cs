using UnityEngine;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using LockedLootContainers;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using OverhauledOverworldTravel;
using System;

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

        Panel travelPathOverlayPanel;
        Texture2D travelPathTexture;
        Color32[] travelPathPixelBuffer;

        #endregion

        public static Vector2 GetWorldMapPanelSize()
        {
            int screenWidth = DaggerfallUnity.Settings.ResolutionWidth;
            int screenHeight = DaggerfallUnity.Settings.ResolutionHeight;

            if (screenWidth >= 1600 && screenHeight >= 800) { return new Vector2(1600, 800); }
            else if (screenWidth >= 1280 && screenHeight >= 640) { return new Vector2(1280, 640); }
            else if (screenWidth >= 960 && screenHeight >= 480) { return new Vector2(960, 480); }
            else if (screenWidth >= 640 && screenHeight >= 320) { return new Vector2(640, 320); }
            else { return new Vector2(320, 160); }
        }

        protected override void Setup()
        {
            base.Setup();

            // Load textures
            LoadTextures();

            ParentPanel.BackgroundColor = Color.black;
            //ParentPanel.BackgroundTexture = backgroundTexture;

            // This makes the background "transparent" instead of a blank black screen when opening this window.
            //ParentPanel.BackgroundColor = ScreenDimColor;

            // Setup native panel background
            NativePanel.BackgroundColor = ScreenDimColor;
            //NativePanel.BackgroundTexture = baseTexture;

            Panel worldMapPanel = new Panel();
            worldMapPanel.HorizontalAlignment = HorizontalAlignment.Center;
            worldMapPanel.VerticalAlignment = VerticalAlignment.Middle;
            worldMapPanel.Size = GetWorldMapPanelSize();
            worldMapPanel.AutoSize = AutoSizeModes.None;
            worldMapPanel.BackgroundColor = ScreenDimColor;
            worldMapPanel.BackgroundTexture = baseTexture;
            worldMapPanel.ToolTip = defaultToolTip;
            worldMapPanel.ToolTipText = "This Is The World Map";
            worldMapPanel.OnMouseClick += ClickHandler;
            if (ParentPanel != null)
                ParentPanel.Components.Add(worldMapPanel);

            Rect rectWorldMap = worldMapPanel.Rectangle;

            // Overlay for the player travel path panel
            travelPathOverlayPanel = DaggerfallUI.AddPanel(rectWorldMap, worldMapPanel); // May have to make the Parent panel this panel's parent similar to the worldMapPanel, will see.
            travelPathOverlayPanel.HorizontalAlignment = HorizontalAlignment.Center;
            travelPathOverlayPanel.VerticalAlignment = VerticalAlignment.Middle;
            //travelPathOverlayPanel.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes

            // Setup pixel buffer and texture for player travel path
            travelPathPixelBuffer = new Color32[(int)rectWorldMap.width * (int)rectWorldMap.height];
            travelPathTexture = new Texture2D((int)rectWorldMap.width, (int)rectWorldMap.height, TextureFormat.ARGB32, false);
            travelPathTexture.filterMode = FilterMode.Point;

            Panel chestPictureBox = DaggerfallUI.AddPanel(new Rect(113, 64, 30, 22), NativePanel);
            chestPictureBox.BackgroundColor = new Color(0.9f, 0.1f, 0.5f, 0.75f); // For testing purposes
            chestPictureBox.ToolTip = defaultToolTip;
            chestPictureBox.ToolTipText = "The Chest Looks";

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

            TestWherePixelBufferIsLocated(pixelBufferPos);

            // Perhaps tomorrow, see about getting the map locations to render on-screen using pixelBuffer, similar to how Hazelnut did it, but with this different coord. system, will see.

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
