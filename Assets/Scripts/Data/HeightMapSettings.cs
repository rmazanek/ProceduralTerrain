using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu]
    public class HeightMapSettings : UpdatableData
    {
        public NoiseSettings NoiseSettings;
        public bool UseFalloff;
        [Range(-10, 5)] public int FalloffSlope = 3;
        [Range(0.1f, 20f)] public float FalloffOffset = 1.5f;
        public float HeightMultiplier;
        public AnimationCurve HeightCurve;
        public float MinHeight
        {
            get 
            {
                return HeightMultiplier * HeightCurve.Evaluate(0);
            }
        }
        public float MaxHeight
        {
            get 
            {
                return HeightMultiplier * HeightCurve.Evaluate(1);
            }
        }
        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            NoiseSettings.ValidateValues();
            base.OnValidate();
        }
        #endif
    }
}
