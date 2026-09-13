using UnityEngine;

[System.Serializable]
public class ARObjectData
{
    [Header("Inventory")]
    public Sprite inventoryImage;

    [Header("AR Object")]
    public GameObject prefab;
}