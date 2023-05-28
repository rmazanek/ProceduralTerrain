using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain
{
    public static class FalloffGenerator
    {
        public static float[,] GenerateFalloffMap(int size, float slope, float offset)
        {
            float[,] map = new float[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    float x = i / (float)size * 2 - 1;
                    float y = j / (float)size * 2 - 1;

                    float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    map[i, j] = FalloffModifier(value, slope, offset);
                }
            }
            return map;
        }

        static float FalloffModifier(float value, float slope, float offset)
        {
            return Mathf.Pow(value, slope) / (Mathf.Pow(value, slope) + Mathf.Pow(offset - offset * value, slope));
        }
    }
}