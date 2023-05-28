using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain
{
    public class TerrainChunk
    {
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;
        MapGenerator mapGenerator;
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        public TerrainChunk(MapGenerator mapGen, Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material mat)
        {
            this.detailLevels = detailLevels;

            mapGenerator = mapGen;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = mat;

            meshObject.transform.position = positionV3 * mapGenerator.TerrainData.UniformScale;
            meshObject.transform.localScale = Vector3.one * mapGenerator.TerrainData.UniformScale;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(mapGenerator, detailLevels[i].LOD, SetChunkInViewRangeToVisible);
                if (detailLevels[i].UseForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }
        private void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            SetChunkInViewRangeToVisible();
        }
        public void SetChunkInViewRangeToVisible()
        {
            if (!mapDataReceived) return;
            float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(EndlessTerrain.ViewerPosition));
            bool visible = viewerDistFromNearestEdge <= EndlessTerrain.MaxViewDst;

            if (!visible) return;
            
            int lodIndex = 0;
            for (int i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewerDistFromNearestEdge > detailLevels[i].VisibleDistanceThreshold)
                {
                    lodIndex = i + 1;
                }
                else
                {
                    break;
                }
            }
            if (lodIndex != previousLODIndex)
            {
                LODMesh lodMesh = lodMeshes[lodIndex];
                if(lodMesh.HasMesh)
                {
                    previousLODIndex = lodIndex;
                    meshFilter.mesh = lodMesh.Mesh;
                }
                else if (!lodMesh.HasRequestedMesh)
                {
                    lodMesh.RequestMesh(mapData);
                }
            }

            // Set collision mesh if player is close enough to render terrain at highest LOD
            if (lodIndex == 0)
            {
                SetCollisionMesh(meshCollider, collisionLODMesh, mapData);
            }
            EndlessTerrain.TerrainChunksVisibleLastUpdate.Add(this);

            SetVisible(visible);
        }
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
        private void SetCollisionMesh(MeshCollider meshCollider, LODMesh lodMesh, MapData mapData)
        {
            if (lodMesh.HasMesh)
            {
                meshCollider.sharedMesh = lodMesh.Mesh;
            }
            else if (!lodMesh.HasRequestedMesh)
            {
                lodMesh.RequestMesh(mapData);
            }
        }
    }
}