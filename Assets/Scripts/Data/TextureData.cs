using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu]
    public class TextureData : UpdatableData
    {
        public Color[] baseColors;
        [Range(0f, 1f)] public float[] BaseStartHeights;
        float savedMinHeight;
        float savedMaxHeight;
        public void ApplyToMaterial(Material material)
        {
            // Very important that these strings match shader variables
            material.SetInt("baseColorCount", baseColors.Length);
            material.SetColorArray("baseColors", baseColors);
            material.SetFloatArray("baseStartHeights", BaseStartHeights);

            UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
        }

        public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
        {
            savedMaxHeight = maxHeight;
            savedMinHeight = minHeight;
            material.SetFloat("minHeight", minHeight);
            material.SetFloat("maxHeight", maxHeight);
        }
    }
}
