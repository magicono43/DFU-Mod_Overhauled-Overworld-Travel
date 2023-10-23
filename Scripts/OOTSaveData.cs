using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;

namespace OverhauledOverworldTravel
{
    [FullSerializer.fsObject("v1")]
    public class OOTSaveData : IHasModSaveData
    {
        public int[] ExploredPixelsSaved;

        public Type SaveDataType
        {
            get { return typeof(OOTSaveData); }
        }

        public object NewSaveData()
        {
            OOTSaveData emptyData = new OOTSaveData();
            emptyData.ExploredPixelsSaved = new int[1000 * 500];

            // Fill "fog of war" tracking array with empty values initially
            for (int i = 0; i < emptyData.ExploredPixelsSaved.Length; i++)
            {
                emptyData.ExploredPixelsSaved[i] = 0; // Not sure if this filling with "0"s is entirely necessary, but doing it anyway, just in case.
            }

            return emptyData;
        }

        public object GetSaveData()
        {
            OOTSaveData data = new OOTSaveData();
            int[,] arrayData2D = (int[,])OOTMain.Instance.ExploredPixelValues.Clone();
            int[] arrayData1D = new int[1000 * 500];

            // Convert 2D array to 1D array to be used as save data
            int index = 0;
            for (int x = 0; x < 1000; x++)
            {
                for (int y = 0; y < 500; y++)
                {
                    arrayData1D[index] = arrayData2D[x, y];
                    index++;
                }
            }

            data.ExploredPixelsSaved = (int[])arrayData1D.Clone(); // DFU's save-load framework does not support multi-dimensional arrays, so have to convert to a 1D array.
            return data;
        }

        public void RestoreSaveData(object dataIn)
        {
            OOTSaveData data = (OOTSaveData)dataIn;
            int[] arrayData1D = (int[])data.ExploredPixelsSaved.Clone();
            int[,] arrayData2D = new int[1000, 500];

            // Convert 1D array to 2D array to be used as data for the "ExploredPixelValues" variable in the OOTMain class
            int index = 0;
            for (int x = 0; x < 1000; x++)
            {
                for (int y = 0; y < 500; y++)
                {
                    arrayData2D[x, y] = arrayData1D[index];
                    index++;
                }
            }

            OOTMain.Instance.ExploredPixelValues = (int[,])arrayData2D.Clone(); // DFU's save-load framework does not support multi-dimensional arrays, so have to revert a 1D array to a 2D array.
        }
    }
}
