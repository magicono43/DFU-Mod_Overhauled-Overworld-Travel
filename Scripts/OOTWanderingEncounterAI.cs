using UnityEngine;
using System.Collections.Generic;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace OverhauledOverworldTravel
{
    public class OOTWanderingEncounterAI : MonoBehaviour
    {
        #region Fields

        ulong loadID = 0;

        string encounterName = "";

        // Tomorrow, will see about working on these wandering encounters more. Assigning "enemies" to them, logic to pursue the player, actually encountering them when collided with on the map, etc.

        DFPosition previousEncounterPosition = new DFPosition();
        DFPosition nextEncounterPosition = new DFPosition();
        DFPosition encounterDestination = new DFPosition();

        List<DFPosition> currentTravelLinePositionsList = new List<DFPosition>();

        int currentPixelTravelTime = 0;
        int nextPixelTravelTime = 0;

        bool destinationReached = false;

        #endregion

        #region Properties

        public ulong LoadID
        {
            get { return loadID; }
            set { loadID = value; }
        }

        public string EncounterName
        {
            get { return encounterName; }
            set { encounterName = value; }
        }

        public DFPosition PreviousEncounterPosition
        {
            get { return previousEncounterPosition; }
            set { previousEncounterPosition = value; }
        }

        public DFPosition EncounterDestination
        {
            get { return encounterDestination; }
            set { encounterDestination = value; }
        }

        public bool DestinationReached
        {
            get { return destinationReached; }
            set { destinationReached = value; }
        }

        #endregion

        void Start()
        {
            currentTravelLinePositionsList = FindPixelsBetweenStartAndDest();

            if (currentTravelLinePositionsList.Count > 0)
            {
                nextEncounterPosition.Y = currentTravelLinePositionsList[0].Y;
                nextEncounterPosition.X = currentTravelLinePositionsList[0].X;
            }
        }

        void Update()
        {
            if (destinationReached == true)
                return;

            if (NewOOTMapWindow.isPlayerTraveling == true || NewOOTMapWindow.isPlayerWaiting == true)
            {
                if ((previousEncounterPosition.Y == encounterDestination.Y) || (previousEncounterPosition.X == encounterDestination.X))
                {
                    destinationReached = true;
                    return;
                }

                if (currentPixelTravelTime == 0) { currentPixelTravelTime = CalculatePixelTravelTime(); }

                if (nextPixelTravelTime == 0) { nextPixelTravelTime = CalculatePixelTravelTime(); }

                --currentPixelTravelTime;

                if (currentPixelTravelTime <= 0)
                {
                    currentPixelTravelTime = nextPixelTravelTime;
                    nextPixelTravelTime = 0;

                    previousEncounterPosition.Y = nextEncounterPosition.Y;
                    previousEncounterPosition.X = nextEncounterPosition.X;
                    if (currentTravelLinePositionsList.Count > 0)
                    {
                        currentTravelLinePositionsList.RemoveAt(0);

                        if (currentTravelLinePositionsList.Count > 0)
                        {
                            nextEncounterPosition.Y = currentTravelLinePositionsList[0].Y;
                            nextEncounterPosition.X = currentTravelLinePositionsList[0].X;
                        }
                        else
                        {
                            nextEncounterPosition.Y = encounterDestination.Y;
                            nextEncounterPosition.X = encounterDestination.X;
                            previousEncounterPosition.Y = nextEncounterPosition.Y;
                            previousEncounterPosition.X = nextEncounterPosition.X;
                        }
                    }
                }
            }
        }

        public List<DFPosition> FindPixelsBetweenStartAndDest()
        {
            int encounterXMapPixel = previousEncounterPosition.X;
            int encounterYMapPixel = previousEncounterPosition.Y;
            int endPosXMapPixel = encounterDestination.X;
            int endPosYMapPixel = encounterDestination.Y;

            // Do rest of distance calculation and populating list with pixel values in-between playerPos and destinationPos
            List<DFPosition> pixelsList = new List<DFPosition>();
            int distanceXMapPixels = endPosXMapPixel - encounterXMapPixel;
            int distanceYMapPixels = endPosYMapPixel - encounterYMapPixel;
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
                    encounterXMapPixel += xPixelMovementDirection;
                    shorterOfXandYDistanceIncrementer += distanceYMapPixelsAbs;

                    if (shorterOfXandYDistanceIncrementer > distanceXMapPixelsAbs)
                    {
                        shorterOfXandYDistanceIncrementer -= distanceXMapPixelsAbs;
                        encounterYMapPixel += yPixelMovementDirection;
                    }
                }
                else
                {
                    encounterYMapPixel += yPixelMovementDirection;
                    shorterOfXandYDistanceIncrementer += distanceXMapPixelsAbs;

                    if (shorterOfXandYDistanceIncrementer > distanceYMapPixelsAbs)
                    {
                        shorterOfXandYDistanceIncrementer -= distanceYMapPixelsAbs;
                        encounterXMapPixel += xPixelMovementDirection;
                    }
                }

                pixelPos.Y = encounterYMapPixel;
                pixelPos.X = encounterXMapPixel;

                pixelsList.Add(pixelPos);

                ++numberOfMovements;
            }

            return pixelsList;
        }

        int CalculatePixelTravelTime()
        {
            return 35;
        }
    }
}