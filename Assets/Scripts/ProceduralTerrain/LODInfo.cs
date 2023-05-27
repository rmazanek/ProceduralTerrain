using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain
{
    [System.Serializable]
    public struct LODInfo
    {
        public int LOD;
        public float VisibleDistanceThreshold;
        public bool UseForCollider;
    }
}