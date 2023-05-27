using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu(menuName = "Noise/CosSin")]
    public class CosSinNoise : NoiseAlgorithm
    {
        public override float GenerateNoise(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float phi = Mathf.Atan2(y, x);
            return Mathf.PerlinNoise(r, phi);
        }
    }
}
