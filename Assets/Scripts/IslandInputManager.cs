using UnityEngine;

public class IslandInputManager : MonoBehaviour
{
    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Island island = hit.transform.GetComponent<Island>();

                if (island != null)
                {
                    island.SelectIsland();
                }
            }
        }
    }
}