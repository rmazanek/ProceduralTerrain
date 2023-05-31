using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [RequireComponent(typeof(ThreadedDataRequester))]
    public class TerrainGenerator : MonoBehaviour
    {
        const float viewerMoveThresholdForChunkUpdate = 25f;
        const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
        public int ColliderLODIndex;
        public LODInfo[] DetailLevels;
        public MeshSettings MeshSettings;
        public HeightMapSettings HeightMapSettings;
        public TextureData TextureSettings;
        public Transform Viewer;
        private Vector2 ViewerPosition;
        private Vector2 viewerPositionOld;
        [SerializeField] private Material mapMaterial;
        private float meshWorldSize;
        private int chunksVisibleInViewDst;
        private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
        public static List<TerrainChunk> VisibleTerrainChunks = new List<TerrainChunk>();
        private void Start() 
        {
            TextureSettings.ApplyToMaterial(mapMaterial);
            TextureSettings.UpdateMeshHeights(mapMaterial, HeightMapSettings.MinHeight, HeightMapSettings.MaxHeight);
            
            float maxViewDst = DetailLevels[DetailLevels.Length - 1].VisibleDistanceThreshold;
            meshWorldSize = MeshSettings.MeshWorldSize;
            chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

            UpdateVisibleChunks();
        }
        private void Update()
        {
            ViewerPosition = new Vector2(Viewer.position.x, Viewer.position.z);
            if (ViewerPosition != viewerPositionOld)
            {
                foreach (TerrainChunk chunk in VisibleTerrainChunks)
                {
                    chunk.UpdateCollisionMesh();
                }
            }
            if ((viewerPositionOld - ViewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
            {
                viewerPositionOld = ViewerPosition;
                UpdateVisibleChunks();
            }
        }
        private void UpdateVisibleChunks()
        {
            HashSet<Vector2> alreadyUpdatedChunkCoords = UpdateTerrainChunks();

            int currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / meshWorldSize);
            int currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / meshWorldSize);

            for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
                {
                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                    
                    if (alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) continue;
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, HeightMapSettings, MeshSettings, DetailLevels, ColliderLODIndex, transform, Viewer, mapMaterial);
                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
        private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
        {
            if (isVisible)
            {
                VisibleTerrainChunks.Add(chunk);
            }
            else
            {
                VisibleTerrainChunks.Remove(chunk);
            }
        }
        private HashSet<Vector2> UpdateTerrainChunks()
        {
            HashSet<Vector2> updatedChunkCoords = new HashSet<Vector2>();
            for (int i = VisibleTerrainChunks.Count - 1; i >= 0; i--)
            {
                updatedChunkCoords.Add(VisibleTerrainChunks[i].coord);
                VisibleTerrainChunks[i].UpdateTerrainChunk();
            }

            return updatedChunkCoords;
        }
    }
}