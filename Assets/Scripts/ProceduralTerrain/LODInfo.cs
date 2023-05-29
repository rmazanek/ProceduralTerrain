using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain
{
    [System.Serializable]
    public struct LODInfo
    {
        [Range(0, MeshSettings.NumSupportedLODs-1)]
        public int LOD;
        public float VisibleDistanceThreshold;
        public float SqrVisibleDistanceThreshold
        {
            get 
            {
                return VisibleDistanceThreshold * VisibleDistanceThreshold;
            }
        }
    }
}