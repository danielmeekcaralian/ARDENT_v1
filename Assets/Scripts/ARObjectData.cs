using UnityEngine;

[System.Serializable]
public class ARObjectData
{
    [SerializeField, HideInInspector] private string libraryItemId;
    public string LibraryItemId => libraryItemId;

#if UNITY_EDITOR
    public void RefreshLibraryIdentity()
    {
        libraryItemId = prefab == null ? string.Empty :
            UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(prefab));
    }
#endif

    [Header("Inventory")]
    public Sprite inventoryImage;

    [Header("AR Object")]
    public GameObject prefab;
}