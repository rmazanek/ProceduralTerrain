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
        private int chunkSize;
        private int chunksVisibleInViewDst;
        private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
        public static List<TerrainChunk> VisibleTerrainChunks = new List<TerrainChunk>();
        private void Start() 
        {
            mapGenerator = FindObjectOfType<MapGenerator>();

            MaxViewDst = DetailLevels[DetailLevels.Length - 1].VisibleDistanceThreshold;
            chunkSize = mapGenerator.MapChunkSize - 1;
            chunksVisibleInViewDst = Mathf.RoundToInt(MaxViewDst / chunkSize);

            UpdateVisibleChunks();
        }
        private void Update()
        {
            ViewerPosition = new Vector2(Viewer.position.x, Viewer.position.z) / mapGenerator.TerrainData.UniformScale;
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
            HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
            alreadyUpdatedChunkCoords = UpdateTerrainChunks();

            int currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / chunkSize);

            for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
                {
                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                    
                    if (alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) return;
                    
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(mapGenerator, viewedChunkCoord, chunkSize, DetailLevels, ColliderLODIndex, transform, mapMaterial));
                    }
                }
            }
        }
        private HashSet<Vector2> UpdateTerrainChunks()
        {
            HashSet<Vector2> updatedChunkCoords = new HashSet<Vector2>();
            for (int i = VisibleTerrainChunks.Count - 1; i >= 0; i--)
            {
                VisibleTerrainChunks[i].UpdateTerrainChunk();
                updatedChunkCoords.Add(VisibleTerrainChunks[i].coord);
            }

            return updatedChunkCoords;
        }
        private void AddTerrainChunkToVisibleLastUpdateList(Vector2 coord)
        {
            if (terrainChunkDictionary[coord].IsVisible())
            {
                VisibleTerrainChunks.Add(terrainChunkDictionary[coord]);
            }
        }
    }
}