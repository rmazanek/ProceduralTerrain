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
        public static float[,] GenerateNormalizedNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
        {
            settings.Scale = ClampScale(settings.Scale);
            InitializeMaxAndMinHeight();

            Vector2[] octaveOffsets = GenerateOctaveOffsets(settings.Octaves, settings.Offset, settings.Seed, sampleCenter);
            float maxPossibleHeight = GetMaxPossibleHeightForGlobalNormalizationMode(settings.Persistence);
            float[,] noiseMap = GenerateNoiseMap(mapWidth, mapHeight, settings, octaveOffsets);
            noiseMap = NormalizedNoiseMap(noiseMap, settings.NormalizeMode, settings.NormalizationFactor, maxPossibleHeight);

            return noiseMap;
        }
        private static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2[] octaveOffsets)
        {
            float[,] noiseMap = new float[mapWidth, mapHeight];

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

                    for (int i = 0; i < settings.Octaves; i++)
                    {
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.Scale * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.Scale * frequency;

                        float noiseValue = settings.NoiseAlgorithm.GenerateNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += noiseValue * amplitude;

                        amplitude *= settings.Persistence;
                        frequency *= settings.Lacunarity;
                    }

                    TrackMaxAndMinNoiseHeight(noiseHeight);
                    noiseMap[x, y] = noiseHeight;
                }
            }

            return noiseMap;
        }
        private static float GetMaxPossibleHeightForGlobalNormalizationMode(float persistence)
        {
            float maxPossibleHeight = 0f;
            float amplitude = 1f;

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
            if (height < minLocalNoiseHeight)
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
                    if (normalizeMode == NormalizeMode.Local) 
                    {
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
        public static Vector2[] GenerateOctaveOffsets(int octaveCount, Vector2 manualOffset, int randSeed, Vector2 sampleCenter)
        {
            System.Random prng = new System.Random(randSeed);
            Vector2[] octaveOffsets = new Vector2[octaveCount];

            for (int i = 0; i < octaveCount; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + manualOffset.x + sampleCenter.x;
                float offsetY = prng.Next(-100000, 100000) - manualOffset.y - sampleCenter.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            return octaveOffsets;
        }
    }
    [System.Serializable]
    public class NoiseSettings
    {
        public int Seed;
        public Noise.NormalizeMode NormalizeMode;
        [Range(0f, 3f)] public float NormalizationFactor = 1f;
        public float Scale = 50f;
        [Range(0, 10)] public int Octaves = 6;
        [Range(0f, 1f)] public float Persistence = 0.6f;
        [Range(1f, 4f)] public float Lacunarity = 2f;
        public Vector2 Offset;
        public NoiseAlgorithm NoiseAlgorithm;
        public void ValidateValues()
        {
            Scale = Mathf.Max(Scale, 0.01f);
            Octaves = Mathf.Max(Octaves, 1);
            Persistence = Mathf.Clamp01(Persistence);
            Lacunarity = Mathf.Max(Lacunarity, 1f);
        }
    }
}
