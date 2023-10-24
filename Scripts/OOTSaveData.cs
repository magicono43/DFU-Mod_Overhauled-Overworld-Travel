using DaggerfallWorkshop.Game.Utility.ModSupport;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OverhauledOverworldTravel
{
    [FullSerializer.fsObject("v1")]
    public class OOTSaveData : IHasModSaveData
    {
        public string ExploredPixelsCompressedSaved;

        public Type SaveDataType
        {
            get { return typeof(OOTSaveData); }
        }

        public object NewSaveData()
        {
            OOTSaveData emptyData = new OOTSaveData();
            emptyData.ExploredPixelsCompressedSaved = "a500000x0"; // Text representation of a 0'ed out array with 500,000 values, or "empty."

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

            // Start converting 1D array data into "compressed" string to be used in save-data.
            List<string> compressedData = new List<string>();
            int currentCount = 0;
            int currentValue = -1; // Initialize to a value not in your data set

            foreach (int value in arrayData1D)
            {
                if (value != currentValue)
                {
                    // A new value is encountered, add the marker
                    if (currentCount > 0)
                    {
                        compressedData.Add($"a{currentCount}x{currentValue}");
                    }

                    // Update current value and count
                    currentValue = value;
                    currentCount = 1;
                }
                else
                {
                    // Value is the same as the previous, increment the count
                    currentCount++;
                }
            }

            // Add the last marker
            if (currentCount > 0)
            {
                compressedData.Add($"a{currentCount}x{currentValue}");
            }

            // Join the compressed data into a single string
            string compressedString = string.Join("", compressedData);

            data.ExploredPixelsCompressedSaved = compressedString; // DFU's save-load framework does not support multi-dimensional arrays, so have to convert to a 1D array.
            return data;
        }

        public void RestoreSaveData(object dataIn)
        {
            OOTSaveData data = (OOTSaveData)dataIn;
            string compressedString = data.ExploredPixelsCompressedSaved;
            List<int> dataArray = new List<int>();

            // Translate/Decompress save-data from a string to a 1D array to be used again by the game as a proper variable.
            int i = 0;
            while (i < compressedString.Length)
            {
                // Check for the "a" marker
                if (compressedString[i] == 'a')
                {
                    // Find the end of the counting value
                    int j = i + 1;
                    while (j < compressedString.Length && compressedString[j] != 'x')
                    {
                        j++;
                    }

                    // Extract the counting value
                    if (j < compressedString.Length)
                    {
                        int count = int.Parse(compressedString.Substring(i + 1, j - i - 1));

                        // Find the end of the value
                        int k = j + 1;
                        while (k < compressedString.Length && compressedString[k] != 'a')
                        {
                            k++;
                        }

                        // Extract the value
                        if (k <= compressedString.Length)
                        {
                            int value = int.Parse(compressedString.Substring(j + 1, k - j - 1));

                            // Add the value to the array count times
                            for (int l = 0; l < count; l++)
                            {
                                dataArray.Add(value);
                            }

                            i = k;
                        }
                    }
                }
                else
                {
                    // If no marker is found, something might be wrong with the input string, handle this error condition appropriately
                    Debug.LogError("Something Terrible Happened Loading OOT's Save Data, Missed The Letter (a)!");
                    break;
                }
            }

            // Convert the List<int> to a 1D array
            int[] arrayData1D = dataArray.ToArray();

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
