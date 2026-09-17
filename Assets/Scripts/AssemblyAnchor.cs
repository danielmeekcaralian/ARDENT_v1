using UnityEngine;

public class AssemblyAnchor : MonoBehaviour
{
    [Header("Anchor Information")]
    public string anchorID;

    [Header("Targets")]
    [SerializeField] private Transform targetRoot;

    public AssemblyTarget FindTarget(string targetID)
    {
        Transform searchRoot =
            targetRoot != null
            ? targetRoot
            : transform;

        AssemblyTarget[] targets =
            searchRoot.GetComponentsInChildren<AssemblyTarget>(
                true
            );

        foreach (AssemblyTarget target in targets)
        {
            if (target.targetID == targetID)
            {
                return target;
            }
        }

        return null;
    }
}