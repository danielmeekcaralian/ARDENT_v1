using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class HardwareModelPreview : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Layer 31 is unused in this project's TagManager. Reserve it for this viewer.
    private const int PreviewLayer = 31;
    private RawImage image;
    private GameObject rig, model;
    private Transform pivot;
    private Camera previewCamera;
    private RenderTexture texture;
    private readonly List<Mesh> bakedMeshes = new List<Mesh>();
    private readonly Dictionary<Camera, int> cameraMasks = new Dictionary<Camera, int>();
    private float yaw, pitch;
    private bool dragging;
    private int pointer;

    public bool Show(GameObject prefab)
    {
        Hide();
        if (prefab == null) return false;
        image = GetComponent<RawImage>();
        EnsureRig();
        // Copy only renderable geometry: never instantiate AR scripts, colliders or audio.
        model = new GameObject("PreviewModel"); model.transform.SetParent(pivot, false);
        CopyMeshes(prefab.transform, model.transform, true);
        var renderers = model.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) { Hide(); return false; }
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float radius = Mathf.Max(bounds.extents.magnitude, .00001f);
        model.transform.localScale = Vector3.one / radius;
        model.transform.localPosition = -bounds.center / radius;
        yaw = 25f; pitch = 15f;
        pivot.localRotation = Quaternion.Euler(pitch, yaw, 0);
        image.color = Color.white; image.raycastTarget = true;
        gameObject.SetActive(true);
        UpdateTexture();
        previewCamera.enabled = true;
        return true;
    }

    private void EnsureRig()
    {
        if (rig != null) { rig.SetActive(true); return; }
        rig = new GameObject("HardwarePreviewRig");
        var pivotObject = new GameObject("RotationPivot");
        pivot = pivotObject.transform; pivot.SetParent(rig.transform, false);
        var cameraObject = new GameObject("PreviewCamera");
        cameraObject.transform.SetParent(rig.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.orthographic = true;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(.045f, .065f, .09f, 1);
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.nearClipPlane = .1f; previewCamera.farClipPlane = 10;
        previewCamera.transform.localPosition = new Vector3(0, 0, -4);
        previewCamera.allowHDR = false; previewCamera.allowMSAA = false;
        AddLight("KeyLight", Quaternion.Euler(35,-35,0), 1.2f);
        AddLight("FillLight", Quaternion.Euler(-15,140,0), .6f);
        foreach (var camera in Camera.allCameras)
        {
            if (camera == previewCamera) continue;
            cameraMasks[camera] = camera.cullingMask;
            camera.cullingMask &= ~(1 << PreviewLayer);
        }
    }

    private void AddLight(string name, Quaternion rotation, float intensity)
    {
        var obj = new GameObject(name); obj.transform.SetParent(rig.transform,false);
        obj.transform.localRotation = rotation;
        var light = obj.AddComponent<Light>(); light.type = LightType.Directional;
        light.intensity = intensity; light.cullingMask = 1 << PreviewLayer;
        light.shadows = LightShadows.None;
    }

    private void CopyMeshes(Transform source, Transform parent, bool root)
    {
        if (!root && !source.gameObject.activeSelf) return;
        var obj = new GameObject(source.name); obj.layer = PreviewLayer;
        var target = obj.transform; target.SetParent(parent, false);
        target.localPosition = root ? Vector3.zero : source.localPosition;
        target.localRotation = source.localRotation; target.localScale = source.localScale;
        var filter = source.GetComponent<MeshFilter>();
        var renderer = source.GetComponent<MeshRenderer>();
        var skinned = source.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = null; Material[] materials = null;
        if (filter != null && renderer != null && renderer.enabled && filter.sharedMesh != null)
        { mesh = filter.sharedMesh; materials = renderer.sharedMaterials; }
        else if (skinned != null && skinned.enabled && skinned.sharedMesh != null)
        { mesh = new Mesh(); skinned.BakeMesh(mesh); bakedMeshes.Add(mesh); materials = skinned.sharedMaterials; }
        if (mesh != null)
        {
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var copy = obj.AddComponent<MeshRenderer>(); copy.sharedMaterials = materials;
            copy.shadowCastingMode = ShadowCastingMode.Off; copy.receiveShadows = false;
        }
        foreach (Transform child in source) CopyMeshes(child, target, false);
    }

    private void LateUpdate()
    {
        if (model != null) UpdateTexture();
    }

    private void UpdateTexture()
    {
        if (image == null || previewCamera == null) return;
        Rect rect = image.rectTransform.rect;
        float aspect = Mathf.Clamp(rect.width / Mathf.Max(1, rect.height), .1f, 10f);
        int width = Mathf.RoundToInt(768 * Mathf.Min(1, aspect));
        int height = Mathf.RoundToInt(768 / Mathf.Max(1, aspect));
        if (texture == null || texture.width != width || texture.height != height)
        {
            ReleaseTexture();
            texture = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
            texture.name = "HardwareLibraryPreview"; texture.Create();
            previewCamera.targetTexture = texture; image.texture = texture;
        }
        previewCamera.aspect = aspect;
        // A unit bounding sphere stays inside the frame at every drag angle.
        previewCamera.orthographicSize = 1.15f / Mathf.Min(1,aspect);
    }

    public void OnBeginDrag(PointerEventData data)
    { if (model == null || dragging || data.button != PointerEventData.InputButton.Left) return; dragging = true; pointer = data.pointerId; }
    public void OnDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != pointer || pivot == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, data.position, data.pressEventCamera, out var now) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, data.position-data.delta, data.pressEventCamera, out var before)) return;
        Vector2 delta = now-before;
        yaw -= delta.x / Mathf.Max(1,image.rectTransform.rect.width) * 240;
        pitch = Mathf.Clamp(pitch + delta.y / Mathf.Max(1,image.rectTransform.rect.height) * 180,-85,85);
        pivot.localRotation = Quaternion.Euler(pitch,yaw,0);
    }
    public void OnEndDrag(PointerEventData data) { if (data.pointerId == pointer) dragging = false; }
    private void OnEnable() { if (previewCamera != null && model != null) { rig.SetActive(true); previewCamera.enabled = true; } }
    private void OnDisable() { dragging = false; if (previewCamera != null) previewCamera.enabled = false; if (rig != null) rig.SetActive(false); }

    public void Hide()
    {
        dragging = false;
        if (model != null) { model.SetActive(false); Destroy(model); model = null; }
        if (pivot != null) pivot.localRotation = Quaternion.identity;
        foreach (var mesh in bakedMeshes) if (mesh != null) Destroy(mesh);
        bakedMeshes.Clear();
        if (previewCamera != null) previewCamera.enabled = false;
        ReleaseTexture();
        gameObject.SetActive(false);
    }
    private void ReleaseTexture()
    {
        if (previewCamera != null) previewCamera.targetTexture = null;
        if (image != null) image.texture = null;
        if (texture != null) { texture.Release(); Destroy(texture); texture = null; }
    }
    private void OnDestroy()
    {
        ReleaseTexture();
        foreach (var mesh in bakedMeshes) if (mesh != null) Destroy(mesh);
        foreach (var entry in cameraMasks) if (entry.Key != null) entry.Key.cullingMask = entry.Value;
        if (rig != null) Destroy(rig);
    }
}
