using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public struct MapData
    {
        public readonly float[,] HeightMap;
        public MapData(float[,] heightMap)
        {
            this.HeightMap = heightMap;
        }
    }
}
