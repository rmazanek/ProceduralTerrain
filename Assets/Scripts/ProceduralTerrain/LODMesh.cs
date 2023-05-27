using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain
{
    public class LODMesh
    {
        private MapGenerator _mapGenerator;
        public Mesh Mesh;
        public bool HasRequestedMesh;
        public bool HasMesh;
        int _lod;
        System.Action _updateCallback;
        public LODMesh(MapGenerator mapGenerator, int lod, System.Action updateCallback)
        {
            this._mapGenerator = mapGenerator;
            this._lod = lod;
            this._updateCallback = updateCallback;
        }
        private void OnMeshDataReceived(MeshData meshData)
        {
            Mesh = meshData.CreateMesh();
            HasMesh = true;

            _updateCallback();
        }
        public void RequestMesh(MapData mapData)
        {
            HasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _lod, OnMeshDataReceived);
        }
    }
}
