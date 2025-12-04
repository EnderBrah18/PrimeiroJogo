using UnityEngine;

[System.Serializable]
public class RoutineEntry
{
    public string name;
    public NPCRoutine routine;
    public float startHour; // 0–24
}

[CreateAssetMenu(fileName = "NPCSchedule", menuName = "NPC/Schedule")]
public class NPCSchedule : ScriptableObject
{
    public RoutineEntry[] entries;
}
