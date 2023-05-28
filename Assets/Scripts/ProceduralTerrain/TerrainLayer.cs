using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [System.Serializable]
    public class TerrainLayer
    {
        public Texture2D Texture;
        public Color Tint;
        [Range(0f, 1f)] public float TintStrength;
        [Range(0f, 1f)] public float StartHeight;
        [Range(0f, 1f)] public float BlendStrength;
        public float TextureScale;
    }
}
