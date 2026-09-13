using UnityEngine;

public class ARObjectInfo : MonoBehaviour
{
    [Header("Object Information")]
    public string objectName;

    [TextArea(3, 6)]
    public string information;
}