using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public List<RoomConnector> connectors = new List<RoomConnector>();
    public Vector2Int gridPos;

    private void Awake()
    {
        connectors.Clear();
        connectors.AddRange(GetComponentsInChildren<RoomConnector>());
    }

    public RoomConnector GetConnector(Direction dir)
    {
        return connectors.Find(c => c.direction == dir);
    }
}
