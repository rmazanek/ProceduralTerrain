using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class TerrainChunk
    {
        private const float ColliderGenerationDistanceThreshold = 5f;
        public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
        public Vector2 coord;
        private Vector2 sampleCenter;
        private GameObject meshObject;
        private Bounds bounds;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private int colliderLODIndex;
        private HeightMap heightMap;
        private bool heightMapReceived;
        private int previousLODIndex = -1;
        private bool hasSetCollider;
        private float maxViewDistance;
        private HeightMapSettings heightMapSettings;
        private MeshSettings meshSettings;
        private Transform viewer;
        public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material mat)
        {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;
            this.heightMapSettings = heightMapSettings;
            this.meshSettings = meshSettings;
            this.viewer = viewer;

            sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.Scale;
            Vector2 position = coord * meshSettings.MeshWorldSize;
            bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

            meshObject = new GameObject("Terrain Chunk");
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = mat;

            meshObject.transform.position = new Vector3(position.x, 0, position.y);
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].LOD);
                lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex)
                {
                    lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
                }
            }

            maxViewDistance = detailLevels[detailLevels.Length - 1].VisibleDistanceThreshold;
        }
        public void Load()
        {
            ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, heightMapSettings, sampleCenter), OnHeightMapReceived);
        }
        private void OnHeightMapReceived(object heightMap)
        {
            this.heightMap = (HeightMap)heightMap;
            heightMapReceived = true;

            UpdateTerrainChunk();
        }
        private Vector2 viewerPosition {
            get {
                return new Vector2(viewer.position.x, viewer.position.z);
            }
        }
        public void UpdateTerrainChunk()
        {
            if (!heightMapReceived) return;
            float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool wasVisible = IsVisible();
            bool visible = viewerDistFromNearestEdge <= maxViewDistance;

            if (visible)
            {
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
                        lodMesh.RequestMesh(heightMap, meshSettings);
                    }
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
            SetVisible(visibleThisFrame);
            if (OnVisibilityChanged != null)
            {
                OnVisibilityChanged(this, visibleThisFrame);
            }
        }
        public void UpdateCollisionMesh()
        {
            if (hasSetCollider) return;

            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].SqrVisibleDistanceThreshold)
            {
                if (!lodMeshes[colliderLODIndex].HasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }
            
            if (sqrDstFromViewerToEdge < ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)
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
                    lodMesh.RequestMesh(heightMap, meshSettings);
                }
            }
        }
    }
}