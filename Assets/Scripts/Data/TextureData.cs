using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProceduralTerrain
{
    [CreateAssetMenu]
    public class TextureData : UpdatableData
    {
        const int textureSize = 512;
        const TextureFormat textureFormat = TextureFormat.RGB565;
        public TerrainLayer[] TerrainLayers;
        
        float savedMinHeight;
        float savedMaxHeight;
        public void ApplyToMaterial(Material material)
        {
            // Very important that these strings match shader variables
            material.SetInt("terrainLayerCount", TerrainLayers.Length);
            material.SetColorArray("baseColors", TerrainLayers.Select(x => x.Tint).ToArray());
            material.SetFloatArray("baseStartHeights", TerrainLayers.Select(x => x.StartHeight).ToArray());
            material.SetFloatArray("baseBlends", TerrainLayers.Select(x => x.BlendStrength).ToArray());
            material.SetFloatArray("baseColorStrength", TerrainLayers.Select(x => x.TintStrength).ToArray());
            material.SetFloatArray("baseTextureScales", TerrainLayers.Select(x => x.TextureScale).ToArray());
            Texture2DArray texturesArray = GenerateTextureArray(TerrainLayers.Select(x => x.Texture).ToArray());
            material.SetTexture("baseTextures", texturesArray);

            UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
        }
        public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
        {
            savedMaxHeight = maxHeight;
            savedMinHeight = minHeight;
            material.SetFloat("minHeight", minHeight);
            material.SetFloat("maxHeight", maxHeight);
        }
        private Texture2DArray GenerateTextureArray(Texture2D[] textures)
        {
            Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);

            for (int i = 0; i < textures.Length; i++)
            {
                textureArray.SetPixels(textures[i].GetPixels(), i);
            }
            textureArray.Apply();

            return textureArray;
        }
    }
}
