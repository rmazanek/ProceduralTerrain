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
        int _lod;
        public LODMesh(int lod)
        {
            this._lod = lod;
        }
        private void OnMeshDataReceived(object meshDataObject)
        {
            Mesh = ((MeshData)meshDataObject).CreateMesh();
            HasMesh = true;

            UpdateCallback();
        }
        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            HasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.Values, meshSettings, _lod), OnMeshDataReceived);
        }
    }
}
