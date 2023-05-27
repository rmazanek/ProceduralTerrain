using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public abstract class NoiseAlgorithm : ScriptableObject
    {
        public abstract float GenerateNoise(float x, float y);
    }
}