using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain 
{
    public class MapPreview : MonoBehaviour
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
        public Renderer textureRenderer;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public bool AutoUpdate = true;
        private void OnTextureValuesUpdated()
        {
            textureData.ApplyToMaterial(terrainMaterial);
        }
        public void DrawMapInEditor()
        {
            textureData.ApplyToMaterial(terrainMaterial);
            textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(MeshSettings.NumVertsPerLine, MeshSettings.NumVertsPerLine, heightMapSettings, Vector2.zero);
            if (drawMode == DrawMode.NoiseMap)
            {
                DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
            }
            else if (drawMode == DrawMode.Mesh)
            {
                //falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize, TerrainData.FalloffSlope, TerrainData.FalloffOffset);
                DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.Values, MeshSettings, editorPreviewLevelOfDetail));
            }
            else if (drawMode == DrawMode.FalloffMap)
            {
                DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(MeshSettings.NumVertsPerLine, heightMapSettings.FalloffSlope, heightMapSettings.FalloffOffset), 0f, 1f)));
            }
        }
        public void DrawTexture(Texture2D texture)
        {
            textureRenderer.sharedMaterial.mainTexture = texture;
            textureRenderer.transform.localScale = new Vector3(-texture.width, 1, texture.height) / 10f;
            textureRenderer.gameObject.SetActive(true);
            meshFilter.gameObject.SetActive(false);
        }
        public void DrawMesh(MeshData meshData)
        {
            meshFilter.sharedMesh = meshData.CreateMesh();
            textureRenderer.gameObject.SetActive(false);
            meshFilter.gameObject.SetActive(true);
        }
        private void OnValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                DrawMapInEditor();
            }
        }
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
    }
}
