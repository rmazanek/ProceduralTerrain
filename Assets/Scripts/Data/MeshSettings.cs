using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu]
    public class MeshSettings : UpdatableData
    {
        public const int NumSupportedLODs = 5;
        public const int NumSupportedChunkSizes = 9;
        public const int NumSupportedFlatShadedChunkSizes = 3;
        public static readonly int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
        public float Scale = 2.5f;
        public bool UseFlatShading;
        [Range(0, NumSupportedChunkSizes-1)]
        public int ChunkSizeIndex;
        [Range(0, NumSupportedFlatShadedChunkSizes-1)]
        public int FlatShadedChunkSizeIndex;
        // Number of vertices per line of mesh rendered at highest resolution (LOD = 0). Includes 2 additional verts on each side of line for calculating normals.
        public int NumVertsPerLine
        {
            get 
            {
                return SupportedChunkSizes[(UseFlatShading) ? FlatShadedChunkSizeIndex : ChunkSizeIndex] + 5;
            }
        }
        public float MeshWorldSize 
        {
            get
            {
                return (NumVertsPerLine - 1 - 2) * Scale;
            }
        }
    }
}
