using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu]
    public class TerrainData : UpdatableData
    {
        public float UniformScale = 2.5f;
        public bool UseFlatShading;
        public bool UseFalloff;
        [Range(-10, 5)] public int FalloffSlope = 3;
        [Range(0.1f, 20f)] public float FalloffOffset = 1.5f;
        public float MeshHeightMultiplier;
        public AnimationCurve MeshHeightCurve;
        public float MinHeight
        {
            get 
            {
                return UniformScale * MeshHeightMultiplier * MeshHeightCurve.Evaluate(0);
            }
        }
        public float MaxHeight
        {
            get 
            {
                return UniformScale * MeshHeightMultiplier * MeshHeightCurve.Evaluate(1);
            }
        }
    }
}
