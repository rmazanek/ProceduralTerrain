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
        public MeshSettings MeshSettings;
        [SerializeField] private HeightMapSettings heightMapSettings;
        public TextureData textureData;
        public Material terrainMaterial;
        [SerializeField, Range(0, MeshSettings.NumSupportedLODs-1)] private int editorPreviewLevelOfDetail;
        [SerializeField] private NoiseAlgorithm noiseAlgorithm;
        private float[,] falloffMap;
        public bool AutoUpdate = true;
        Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
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
        private void Start() 
        {
            textureData.ApplyToMaterial(terrainMaterial);
            textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        }
        public void DrawMapInEditor()
        {
            textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(MeshSettings.NumVertsPerLine, MeshSettings.NumVertsPerLine, heightMapSettings, Vector2.zero);
            MapDisplay display = FindObjectOfType<MapDisplay>();
            if (drawMode == DrawMode.NoiseMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.Values));
            }
            else if (drawMode == DrawMode.Mesh)
            {
                //falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize, TerrainData.FalloffSlope, TerrainData.FalloffOffset);
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.Values, MeshSettings, editorPreviewLevelOfDetail));
            }
            else if (drawMode == DrawMode.FalloffMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MeshSettings.NumVertsPerLine, heightMapSettings.FalloffSlope, heightMapSettings.FalloffOffset)));
            }
        }
        public void RequestHeightMap(Vector2 center, Action<HeightMap> callback)
        {
            ThreadStart threadStart = delegate 
            {
                HeightMapThread(center, callback);
            };

            new Thread(threadStart).Start();
        }
        private void HeightMapThread(Vector2 center, Action<HeightMap> callback)
        {
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(MeshSettings.NumVertsPerLine, MeshSettings.NumVertsPerLine, heightMapSettings, center);
            lock(heightMapThreadInfoQueue)
            {
                heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap> (callback, heightMap));
            }
        }
        public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback)
        {
            ThreadStart  threadStart = delegate
            {
                MeshDataThread(heightMap, lod, callback);
            };
            
            new Thread(threadStart).Start();
        }
        private void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.Values, MeshSettings, lod);
            lock(meshDataThreadInfoQueue)
            {
                meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData> (callback, meshData));
            }
        }
        private void Update()
        {
            CheckForNewHeightMapOnThread();
            CheckForNewMeshDataOnThread();
        }
        private void CheckForNewHeightMapOnThread()
        {
            if (heightMapThreadInfoQueue.Count > 0)
            {
                for (int i = 0; i < heightMapThreadInfoQueue.Count; i++)
                {
                MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue();
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
        //private float[,] ApplyFalloffMap(float[,] noiseMap)
        //{
		//    
        //    if (falloffMap == null) 
        //    {
        //        falloffMap = FalloffGenerator.GenerateFalloffMap(MeshSettings.NumVertsPerLine + 2, heightMapSettings.FalloffSlope, heightMapSettings.FalloffOffset);
        //    }
        //    for (int y = 0; y < MeshSettings.NumVertsPerLine + 2; y++) 
        //    {
		//    	for (int x = 0; x < MeshSettings.NumVertsPerLine + 2; x++) 
        //        {
        //            noiseMap [x, y] = Mathf.Clamp01(noiseMap [x, y] - falloffMap [x, y]);
		//    	}
		//    }
        //    return noiseMap;
        //}
        private void OnValidate() 
        {
            if (MeshSettings != null) 
            {
                // Ensure subscription counts stays at 1
                MeshSettings.OnValuesUpdated -= OnValuesUpdated;
                MeshSettings.OnValuesUpdated += OnValuesUpdated;
            }
            if (heightMapSettings != null)
            {
                // Ensure subscription counts stays at 1
                heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
                heightMapSettings.OnValuesUpdated += OnValuesUpdated;
            }
            if (textureData != null)
            {
                textureData.OnValuesUpdated -= OnTextureValuesUpdated;
                textureData.OnValuesUpdated += OnTextureValuesUpdated;
            }
            if (MeshSettings.UseFlatShading)
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
