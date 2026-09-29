using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public partial class ARPlacementManager
{
    public bool TryCheckpointSurface(Vector2 screen, out Pose pose)
    {
        pose = default;
        if (raycastManager == null || !raycastManager.Raycast(screen, hits, TrackableType.PlaneWithinPolygon)) return false;
        pose = hits[0].pose;
        return true;
    }

    public void BeginCheckpointWorkspace(Pose pose)
    {
        CancelPlacement();
        assemblyScale = 1f;
        GetPlacementParent(pose);
    }

    public GameObject SpawnCheckpointObject(GameObject prefab, Vector3 offset)
    {
        var instance = Instantiate(prefab, assemblyRoot);
        instance.transform.localPosition = offset;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = prefab.transform.localScale;
        var manipulator = instance.GetComponent<ARObjectManipulator>();
        if (manipulator != null) manipulator.InitializeScaleBaseline();
        return instance;
    }

    public void ClearCheckpointWorkspace()
    {
        if (assemblyRoot != null)
        {
            assemblyRoot.gameObject.SetActive(false);
            Destroy(assemblyRoot.gameObject);
        }
        assemblyRoot = null;
        assemblyScale = 1f;
    }
}
