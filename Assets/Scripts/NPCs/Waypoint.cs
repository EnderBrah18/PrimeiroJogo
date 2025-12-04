using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public string waypointName;
    public Transform point;
    public List<Waypoint> neighbors; // Waypoints conectados diretamente

    public Vector3 Position => transform.position;


}
