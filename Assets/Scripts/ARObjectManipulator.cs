using UnityEngine;

public class ARObjectManipulator : MonoBehaviour
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool isLocked;

    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
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

    public void ChangeScale(
        float amount,
        float minimumScale,
        float maximumScale)
    {
        if (isLocked)
            return;

        float currentScale =
            transform.localScale.x;

        float newScale =
            Mathf.Clamp(
                currentScale + amount,
                minimumScale,
                maximumScale
            );

        transform.localScale =
            Vector3.one * newScale;
    }

    public void ResetTransform()
    {
        if (isLocked)
            return;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
    }
}