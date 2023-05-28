using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class UpdatableData : ScriptableObject
    {
        public event System.Action OnValuesUpdated;
        public bool AutoUpdate;

        #if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += _OnValidate;
        }
        private void _OnValidate()
        {
            if (AutoUpdate)
            {
                UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
            }
        }
        public void NotifyOfUpdatedValues()
        {
            UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
            if (OnValuesUpdated != null)
            {
                OnValuesUpdated();
            }
        }
        #endif
    }
}
