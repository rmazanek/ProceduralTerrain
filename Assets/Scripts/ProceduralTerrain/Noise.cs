using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain 
{
    public static class Noise
    {
        public enum NormalizeMode { Local, Global };
        private static float maxLocalNoiseHeight = float.MinValue;
        private static float minLocalNoiseHeight = float.MaxValue;
        public static float[,] GenerateNoiseMap(NoiseAlgorithm noiseAlgorithm, int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode, float normalizationFactor)
        {
            float[,] noiseMap = new float[mapWidth, mapHeight];

            Vector2[] octaveOffsets = new Vector2[octaves];
            
            float maxPossibleHeight = 0f;
            maxPossibleHeight = GetMaxPossibleHeightForGlobalNormalizationMode(octaves, persistence);

            octaveOffsets = GenerateOctaveOffsets(octaves, offset, seed);

            scale = ClampScale(scale);
            InitializeMaxAndMinHeight();

            // To zoom into center rather than top right when setting scale
            float halfWidth = mapWidth / 2f;
            float halfHeight = mapHeight / 2f;

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                        float noiseValue = noiseAlgorithm.GenerateNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += noiseValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    TrackMaxAndMinNoiseHeight(noiseHeight);
                    noiseMap[x, y] = noiseHeight;
                }
            }
            noiseMap = NormalizedNoiseMap(noiseMap, normalizeMode, normalizationFactor, maxPossibleHeight);

            return noiseMap;
        }

        private static float GetMaxPossibleHeightForGlobalNormalizationMode(int octaves, float persistence)
        {
            float maxPossibleHeight = 0f;
            float amplitude = 1f;
            
            //Original max height estimate
            //for (int i = 0; i < octaves; i++)
            //{
            //    maxPossibleHeight += amplitude;
            //    amplitude *= persistence;
            //}

            // New max height est
            maxPossibleHeight = amplitude * (1 / (1 - Mathf.Pow(persistence, 1.5f) / 2f));
            
            return maxPossibleHeight;
        }

        private static float ClampScale(float scale)
        {
            if  (scale <= 0) 
            {
                return 0.0001f;
            }
            else
            {
                return scale;
            }
        }
        private static void InitializeMaxAndMinHeight()
        {
            maxLocalNoiseHeight = float.MinValue;
            minLocalNoiseHeight = float.MaxValue;
        }
        // To establish bounds for later normalization of map height
        public static void TrackMaxAndMinNoiseHeight(float height)
        {
            if (height > maxLocalNoiseHeight)
            {
                maxLocalNoiseHeight = height;
            }
            else if (height < minLocalNoiseHeight)
            {
                minLocalNoiseHeight = height;
            }
        }
        public static float[,] NormalizedNoiseMap(float [,] noiseMap, NormalizeMode normalizeMode, float normalizationFactor, float maxPossibleNoiseHeight)
        {
            for (int y = 0; y < noiseMap.GetLength(1); y++)
            {
                for (int x = 0; x < noiseMap.GetLength(0); x++)
                {
                    if (normalizeMode == NormalizeMode.Local) {
                        noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                    }
                    else
                    {
                        float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleNoiseHeight / normalizationFactor);
                        noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0f, int.MaxValue);
                    }
                }
            }

            return noiseMap;
        }
        public static Vector2[] GenerateOctaveOffsets(int octaveCount, Vector2 manualOffset, int randSeed)
        {
            System.Random prng = new System.Random(randSeed);
            Vector2[] octaveOffsets = new Vector2[octaveCount];

            for (int i = 0; i < octaveCount; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + manualOffset.x;
                float offsetY = prng.Next(-100000, 100000) - manualOffset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            return octaveOffsets;
        }
    }
}
