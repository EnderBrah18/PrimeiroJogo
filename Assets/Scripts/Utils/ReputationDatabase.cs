using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Reputation/Database")]
public class ReputationDatabase : ScriptableObject
{
    public List<ReputationEntry> entries;
}

[System.Serializable]
public class ReputationEntry
{
    public string id;
    public int startValue;
}
