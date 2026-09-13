using UnityEngine;

public class ARInfoCard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
                return;
        }

        // Make the canvas face the camera
        Vector3 direction =
            transform.position - mainCamera.transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation =
            Quaternion.LookRotation(direction, Vector3.up);
    }
}