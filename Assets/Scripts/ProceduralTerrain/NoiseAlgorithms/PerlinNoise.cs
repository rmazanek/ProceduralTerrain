using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu(menuName = "Noise/Perlin")]
    public class PerlinNoise : NoiseAlgorithm
    {
        public override float GenerateNoise(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }
    }
}