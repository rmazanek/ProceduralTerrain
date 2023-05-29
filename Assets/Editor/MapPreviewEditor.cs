using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ProceduralTerrain 
{
    [CustomEditor(typeof(MapPreview))]
    public class MapPreviewEditor : Editor
    {
        public override void OnInspectorGUI() 
        {
            MapPreview mapPreview = (MapPreview)target;

            if (GUILayout.Button("Generate", GUILayout.Height(40f)))
            {
                mapPreview.DrawMapInEditor();
            }

            if (DrawDefaultInspector())
            {
                if(mapPreview.AutoUpdate)
                {
                    mapPreview.DrawMapInEditor();
                }
            }
        }
    }
}