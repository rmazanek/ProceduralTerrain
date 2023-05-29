using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class EndlessTerrain : MonoBehaviour
    {
        const float viewerMoveThresholdForChunkUpdate = 25f;
        const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
        public static float ColliderGenerationDistanceThreshold = 5f;
        public int ColliderLODIndex;
        public LODInfo[] DetailLevels;
        public static float MaxViewDst;
        public Transform Viewer;
        public static Vector2 ViewerPosition;
        Vector2 viewerPositionOld;
        private static MapGenerator mapGenerator;
        [SerializeField] private Material mapMaterial;
        private float meshWorldSize;
        private int chunksVisibleInViewDst;
        private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
        public static List<TerrainChunk> VisibleTerrainChunks = new List<TerrainChunk>();
        private void Start() 
        {
            mapGenerator = FindObjectOfType<MapGenerator>();

            MaxViewDst = DetailLevels[DetailLevels.Length - 1].VisibleDistanceThreshold;
            meshWorldSize = mapGenerator.MeshSettings.MeshWorldSize;
            chunksVisibleInViewDst = Mathf.RoundToInt(MaxViewDst / meshWorldSize);

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
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(mapGenerator, viewedChunkCoord, meshWorldSize, DetailLevels, ColliderLODIndex, transform, mapMaterial));
                    }
                }
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