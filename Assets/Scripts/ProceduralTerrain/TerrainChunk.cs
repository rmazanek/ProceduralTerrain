using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralTerrain
{
    public class TerrainChunk
    {
        public Vector2 coord;
        private Vector2 position;
        private GameObject meshObject;
        private Bounds bounds;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private MapGenerator mapGenerator;
        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private int colliderLODIndex;
        private MapData mapData;
        private bool mapDataReceived;
        private int previousLODIndex = -1;
        private bool hasSetCollider;
        public TerrainChunk(MapGenerator mapGen, Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material mat)
        {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;

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
                lodMeshes[i] = new LODMesh(mapGenerator, detailLevels[i].LOD);
                lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex)
                {
                    lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }
        private void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }
        public void UpdateTerrainChunk()
        {
            if (!mapDataReceived) return;
            float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(EndlessTerrain.ViewerPosition));
            bool wasVisible = IsVisible();
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
            
            UpdateVisibility(wasVisible, visible);
        }
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
        private void UpdateVisibility(bool visibleLastFrame, bool visibleThisFrame)
        {
            if (visibleLastFrame == visibleThisFrame) return;
            if (visibleThisFrame)
            {
                EndlessTerrain.VisibleTerrainChunks.Add(this);
            }
            else
            {
                EndlessTerrain.VisibleTerrainChunks.Remove(this);
            }
            SetVisible(visibleThisFrame);
        }
        public void UpdateCollisionMesh()
        {
            if (hasSetCollider) return;

            float sqrDstFromViewerToEdge = bounds.SqrDistance(EndlessTerrain.ViewerPosition);

            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].SqrVisibleDistanceThreshold)
            {
                if (!lodMeshes[colliderLODIndex].HasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(mapData);
                }
            }
            
            if (sqrDstFromViewerToEdge < EndlessTerrain.ColliderGenerationDistanceThreshold * EndlessTerrain.ColliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].HasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].Mesh;
                    hasSetCollider = true;
                }
            }
        }
        private void UpdateLOD(int previousLODIndex, int currentLODIndex)
        {
            if (currentLODIndex != previousLODIndex)
            {
                LODMesh lodMesh = lodMeshes[currentLODIndex];
                if(lodMesh.HasMesh)
                {
                    previousLODIndex = currentLODIndex;
                    meshFilter.mesh = lodMesh.Mesh;
                }
                else if (!lodMesh.HasRequestedMesh)
                {
                    lodMesh.RequestMesh(mapData);
                }
            }
        }
    }
}