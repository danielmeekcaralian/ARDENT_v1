using UnityEngine;
using UnityEngine.UI;

public class IslandCameraController : MonoBehaviour
{
    public Transform[] points;

    public Button leftButton;
    public Button rightButton;

    public float moveSpeed = 5f;

    private int index = 0;

    private float fixedY;
    private float fixedZ;

    void Start()
    {
        // Lock starting camera position so it never "drifts"
        fixedY = transform.position.y;
        fixedZ = transform.position.z;

        UpdateButtons();
    }

    void Update()
    {
        Vector3 targetPos = points[index].position;

        // 🔥 LOCK AXES (prevents unwanted movement)
        targetPos.y = fixedY;
        targetPos.z = fixedZ;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * moveSpeed
        );
    }

    public void NextIsland()
    {
        if (index < points.Length - 1)
        {
            index++;
            UpdateButtons();
        }
    }

    public void PreviousIsland()
    {
        if (index > 0)
        {
            index--;
            UpdateButtons();
        }
    }

    void UpdateButtons()
    {
        if (leftButton != null)
            leftButton.interactable = index > 0;

        if (rightButton != null)
            rightButton.interactable = index < points.Length - 1;
    }
}