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
        public enum DrawMode { NoiseMap, ColorMap, Mesh, FalloffMap }
        [SerializeField] private DrawMode drawMode;
        [SerializeField] private Noise.NormalizeMode normalizeMode;
        [SerializeField, Range(0f, 3f)] private float normalizationFactor = 1f;
        [SerializeField] private bool useFlatShading;
        [SerializeField, Range(0, 6)] private int editorPreviewLevelOfDetail;
        [SerializeField] private NoiseAlgorithm noiseAlgorithm;
        [SerializeField] private int seed;
        [SerializeField] private float noiseScale;
        [SerializeField, Range(0, 10)] private int octaves;
        [SerializeField, Range(0f, 1f)] private float persistance;
        [SerializeField] private float lacunarity;
        [SerializeField] private Vector2 offset;
        [SerializeField] private bool useFalloff;
        [SerializeField] private float falloffSlope = 3;
        [SerializeField] private float falloffOffset = 2.2f;
        public static float FalloffSlope;
        public static float FalloffOffset;
        [SerializeField] private float meshHeightMultiplier;
        [SerializeField] private AnimationCurve meshHeightCurve;
        [SerializeField] TerrainType[] regions;
        private static MapGenerator instance;
        private float[,] falloffMap;
        public bool AutoUpdate = true;
        Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
        Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
        private void Awake() 
        {
            falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
        }
        public static int MapChunkSize
        {
            get {
                if (instance == null)
                {
                    instance = FindObjectOfType<MapGenerator>();
                }

                if (instance.useFlatShading) 
                {
                    // Flat shading requires more vertices (triangles can't have shared vertices when calculating normals), so the chunk size needs to be restricted
                    return 95;
                }
                else
                {
                    // Normal chunk size (241-1 for border tiles) when not using flatshading
                    return 239;
                }
            }
        }
        public void DrawMapInEditor()
        {
            MapData mapData = GenerateMapData(Vector2.zero);
            MapDisplay display = GetComponent<MapDisplay>();
            if (drawMode == DrawMode.NoiseMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
            }
            else if (drawMode == DrawMode.ColorMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.ColorMap, MapChunkSize, MapChunkSize));
            }
            else if (drawMode == DrawMode.Mesh)
            {
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLevelOfDetail, useFlatShading), TextureGenerator.TextureFromColorMap(mapData.ColorMap, MapChunkSize, MapChunkSize));
            }
            else if (drawMode == DrawMode.FalloffMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MapChunkSize)));
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
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, meshHeightMultiplier, meshHeightCurve, lod, useFlatShading);
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
            float [,] noiseMap = Noise.GenerateNoiseMap(noiseAlgorithm, MapChunkSize + 2, MapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode, normalizationFactor);
            Color[] colorMap = GenerateColorMap(noiseMap);
            

            return new MapData(noiseMap, colorMap);
        }
        private Color[] GenerateColorMap(float[,] noiseMap)
        {
            Color[] colorMap = new Color[MapChunkSize * MapChunkSize];

		    for (int y = 0; y < MapChunkSize; y++) 
            {
		    	for (int x = 0; x < MapChunkSize; x++) 
                {
		    		if (useFalloff) 
                    {
		    			noiseMap [x, y] = Mathf.Clamp01(noiseMap [x, y] - falloffMap [x, y]);
		    		}
		    		
                    float currentHeight = noiseMap [x, y];
		    		
                    for (int i = 0; i < regions.Length; i++) 
                    {
		    			if (currentHeight >= regions [i].Height) 
                        {
		    				colorMap [y * MapChunkSize + x] = regions [i].Color;
		    			} 
                        else 
                        {
		    				break;
		    			}
		    		}
		    	}
		    }
            return colorMap;
        }
        private void OnValidate() 
        {
            if (lacunarity < 1) { lacunarity = 1; } 
            if (octaves < 0) { octaves = 0; }
            FalloffSlope = falloffSlope;
            FalloffOffset = falloffOffset;
            falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
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
