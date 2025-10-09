using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class LevelEditorManager : MonoBehaviour
{
#if UNITY_EDITOR

    public List<GameObject> PlacedObjects = new List<GameObject>();

    public void CleanupNullReferences()
    {
        PlacedObjects.RemoveAll(obj => obj == null);
    }

    public void ClearPlacedObjects()
    {
        PlacedObjects.Clear();
    }

    [Button]
    private void ClearAndDestroyPlacedObjects()
    {
        for (int i = PlacedObjects.Count - 1; i >= 0; i--)
        {
            if (PlacedObjects[i] != null)
                DestroyImmediate(PlacedObjects[i]);
            PlacedObjects.RemoveAt(i);
        }
    }

#endif
}
