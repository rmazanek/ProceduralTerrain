using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class UpdatableData : ScriptableObject
    {
        public event System.Action OnValuesUpdated;
        public bool AutoUpdate;

        protected virtual void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += _OnValidate;
        }
        private void _OnValidate()
        {
            if (AutoUpdate)
            {
                NotifyOfUpdatedValues();
            }
        }
        public void NotifyOfUpdatedValues()
        {
            if (OnValuesUpdated != null)
            {
                OnValuesUpdated();
            }
        }
    }
}
