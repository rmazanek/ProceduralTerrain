using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu(menuName = "Noise/UnityEngine.Random")]
    public class RandomNoise : NoiseAlgorithm
    {
        public override float GenerateNoise(float x, float y)
        {
            return UnityEngine.Random.Range(0f,1f);
        }
    }
}
