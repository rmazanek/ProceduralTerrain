using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu]
    public class NoiseData : UpdatableData
    {
        public int Seed;
        public Noise.NormalizeMode NormalizeMode;
        [Range(0f, 3f)] public float NormalizationFactor = 1f;
        public float NoiseScale;
        [Range(0, 10)] public int Octaves;
        [Range(0f, 1f)] public float Persistance;
        [Range(1f, 4f)] public float Lacunarity;
        public Vector2 Offset;
        protected override void OnValidate()
        {
            if (Lacunarity < 1) { Lacunarity = 1; } 
            if (Octaves < 0) { Octaves = 0; }

            base.OnValidate();
        }
    }
}
