using UnityEngine;

[CreateAssetMenu(fileName = "Waypoint", menuName = "Waypoints/Waypoint")]
public class WaypointSO : ScriptableObject
{
    public string waypointName;
    public Vector3 position;
    public WaypointSO next; // referência direta ao próximo waypoint
}

