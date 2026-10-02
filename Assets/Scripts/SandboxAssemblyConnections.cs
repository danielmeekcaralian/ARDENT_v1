using UnityEngine;

[CreateAssetMenu(fileName = "SandboxAssemblyConnections", menuName = "ARDENT/Sandbox Assembly Connections")]
public sealed class SandboxAssemblyConnections : ScriptableObject
{
    public const string ResourcePath = "HardwareProfiles/SandboxAssemblyConnections";
    [Tooltip("Authored snap pairs. Both parts must be unlocked; hardware compatibility is checked before installation.")]
    public SandboxAssemblyRules.Rule[] connections = new SandboxAssemblyRules.Rule[0];
}
