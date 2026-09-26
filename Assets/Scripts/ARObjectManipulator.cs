using UnityEngine;

public class ARObjectManipulator : MonoBehaviour
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool isLocked;
    private bool scaleInitialized;
    private Vector3 baselineScale;
    private float scaleMultiplier = 1f;
    public bool IsLocked => isLocked;

    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        if (!scaleInitialized) InitializeScaleBaseline();
        originalScale = baselineScale;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    public void MoveTo(Vector3 position)
    {
        if (isLocked)
            return;

        transform.position = position;
    }

    public void Rotate(float amount)
    {
        if (isLocked)
            return;

        transform.Rotate(
            Vector3.up,
            amount,
            Space.World
        );
    }

    public void MoveVertical(float worldDistance)
    {
        if (isLocked) return;
        transform.position += Vector3.up * worldDistance;
    }

    public void TiltLocal(Vector3 axis, float degrees)
    {
        if (isLocked) return;
        transform.Rotate(axis, degrees, Space.Self);
    }

    // Placement calls this after setting the prefab's authored baseline size.
    // The fallback in Start supports models placed directly in a scene.
    public void InitializeScaleBaseline()
    {
        baselineScale = transform.localScale;
        originalScale = baselineScale;
        scaleMultiplier = 1f;
        scaleInitialized = true;
    }

    public void ChangeScale(float amount, float maximumMultiplier)
    {
        if (isLocked) return;
        if (!scaleInitialized) InitializeScaleBaseline();
        scaleMultiplier = ARScaleMath.NextMultiplier(scaleMultiplier, amount, maximumMultiplier);
        // Keep each model's dimensions and any deliberately nonuniform proportions.
        transform.localScale = baselineScale * scaleMultiplier;
    }

    public void ResetTransform()
    {
        if (isLocked)
            return;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
        scaleMultiplier = 1f;
    }
}