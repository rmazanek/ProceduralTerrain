using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

namespace ProceduralTerrain 
{
    [RequireComponent(typeof(MapDisplay))]
    public class MapGenerator : MonoBehaviour
    {
        public enum DrawMode { NoiseMap, Mesh, FalloffMap }
        [SerializeField] private DrawMode drawMode;
        public TerrainData TerrainData;
        [SerializeField] private NoiseData noiseData;
        public TextureData textureData;
        public Material terrainMaterial;
        [SerializeField, Range(0, 6)] private int editorPreviewLevelOfDetail;
        [SerializeField] private NoiseAlgorithm noiseAlgorithm;
        private float[,] falloffMap;
        public bool AutoUpdate = true;
        Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
        Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
        private void OnValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                DrawMapInEditor();
            }
        }
        private void OnTextureValuesUpdated()
        {
            textureData.ApplyToMaterial(terrainMaterial);
        }
        public int MapChunkSize
        {
            get {
                if (TerrainData.UseFlatShading) 
                {
                    // Flat shading requires more vertices (triangles can't have shared vertices when calculating normals), so the chunk size needs to be restricted
                    return 95;
                }
                else
                {
                    // Normal chunk size (242 vertices (divisible by 12, 10, 8, 4, 2, 1 for LODs) - to get the number of squares in the grid - 2 for border squares which will not show (because they are used to calculate normals)) when not using flatshading
                    return 239;
                }
            }
        }
        public void DrawMapInEditor()
        {
            MapData mapData = GenerateMapData(Vector2.zero);
            MapDisplay display = FindObjectOfType<MapDisplay>();
            if (drawMode == DrawMode.NoiseMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
            }
            else if (drawMode == DrawMode.Mesh)
            {
                //falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize, TerrainData.FalloffSlope, TerrainData.FalloffOffset);
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, TerrainData.MeshHeightMultiplier, TerrainData.MeshHeightCurve, editorPreviewLevelOfDetail, TerrainData.UseFlatShading));
            }
            else if (drawMode == DrawMode.FalloffMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MapChunkSize, TerrainData.FalloffSlope, TerrainData.FalloffOffset)));
            }
        }
        public void RequestMapData(Vector2 center, Action<MapData> callback)
        {
            ThreadStart threadStart = delegate 
            {
                MapDataThread(center, callback);
            };

            new Thread(threadStart).Start();
        }
        private void MapDataThread(Vector2 center, Action<MapData> callback)
        {
            MapData mapData = GenerateMapData(center);
            lock(mapDataThreadInfoQueue)
            {
                mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData> (callback, mapData));
            }
        }
        public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
        {
            ThreadStart  threadStart = delegate
            {
                MeshDataThread(mapData, lod, callback);
            };
            
            new Thread(threadStart).Start();
        }
        private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, TerrainData.MeshHeightMultiplier, TerrainData.MeshHeightCurve, lod, TerrainData.UseFlatShading);
            lock(meshDataThreadInfoQueue)
            {
                meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData> (callback, meshData));
            }
        }
        private void Update()
        {
            CheckForNewMapDataOnThread();
            CheckForNewMeshDataOnThread();
        }
        private void CheckForNewMapDataOnThread()
        {
            if (mapDataThreadInfoQueue.Count > 0)
            {
                for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
                {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.Callback(threadInfo.Parameter);
                }
            }
        }
        private void CheckForNewMeshDataOnThread()
        {
            if (meshDataThreadInfoQueue.Count > 0)
            {
                for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
                {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.Callback(threadInfo.Parameter);
                }
            }
        }
        private MapData GenerateMapData(Vector2 center)
        {
            float [,] noiseMap = Noise.GenerateNoiseMap(noiseAlgorithm, MapChunkSize + 2, MapChunkSize + 2, noiseData.Seed, noiseData.NoiseScale, noiseData.Octaves, noiseData.Persistance, noiseData.Lacunarity, center + noiseData.Offset, noiseData.NormalizeMode, noiseData.NormalizationFactor);
            if (TerrainData.UseFalloff)
            {
                noiseMap = ApplyFalloffMap(noiseMap);
            }
            
            textureData.UpdateMeshHeights(terrainMaterial, TerrainData.MinHeight, TerrainData.MaxHeight);

            return new MapData(noiseMap);
        }
        private float[,] ApplyFalloffMap(float[,] noiseMap)
        {
		    
            if (falloffMap == null) 
            {
                falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize + 2, TerrainData.FalloffSlope, TerrainData.FalloffOffset);
            }
            for (int y = 0; y < MapChunkSize + 2; y++) 
            {
		    	for (int x = 0; x < MapChunkSize + 2; x++) 
                {
                    noiseMap [x, y] = Mathf.Clamp01(noiseMap [x, y] - falloffMap [x, y]);
		    	}
		    }
            return noiseMap;
        }
        private void OnValidate() 
        {
            if (TerrainData != null) 
            {
                // Ensure subscription counts stays at 1
                TerrainData.OnValuesUpdated -= OnValuesUpdated;
                TerrainData.OnValuesUpdated += OnValuesUpdated;
            }
            if (noiseData != null)
            {
                // Ensure subscription counts stays at 1
                noiseData.OnValuesUpdated -= OnValuesUpdated;
                noiseData.OnValuesUpdated += OnValuesUpdated;
            }
            if (textureData != null)
            {
                textureData.OnValuesUpdated -= OnTextureValuesUpdated;
                textureData.OnValuesUpdated += OnTextureValuesUpdated;
            }
            if (TerrainData.UseFlatShading)
            {
                if (editorPreviewLevelOfDetail == 5)
                {
                    editorPreviewLevelOfDetail = 6;
                    Debug.Log("LOD 5 cannot be used if using flatshading (requires vertex count indivisible by 5 * 2 (see Ep14 Procedural Landmass Generation)).");
                }
            }
        }
        struct MapThreadInfo<T>
        {
            public readonly Action<T> Callback;
            public readonly T Parameter;

            public MapThreadInfo(Action<T> callback, T parameter)
            {
                this.Callback = callback;
                this.Parameter = parameter;
            }
        }
    }
}
