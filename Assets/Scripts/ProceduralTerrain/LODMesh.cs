using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain
{
    public class LODMesh
    {
        public Mesh Mesh;
        public bool HasRequestedMesh;
        public bool HasMesh;
        public event System.Action UpdateCallback;
        private MapGenerator _mapGenerator;
        int _lod;
        public LODMesh(MapGenerator mapGenerator, int lod)
        {
            this._mapGenerator = mapGenerator;
            this._lod = lod;
        }
        private void OnMeshDataReceived(MeshData meshData)
        {
            Mesh = meshData.CreateMesh();
            HasMesh = true;

            UpdateCallback();
        }
        public void RequestMesh(HeightMap mapData)
        {
            HasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _lod, OnMeshDataReceived);
        }
    }
}
