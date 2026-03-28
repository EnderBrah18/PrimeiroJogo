using System.Collections.Generic;
using UnityEngine;

public class GlobalVariableSystem : MonoBehaviour, ISOSavable
{
    [System.Serializable]
    public class GlobalVariableData
    {
        public string name;
        public int value;
    }

    [System.Serializable]
    public class GlobalVariableWrapper
    {
        public List<GlobalVariableData> variables;
    }

    public static GlobalVariableSystem Instance;

    private Dictionary<string, int> intVariables = new Dictionary<string, int>();

    private void Awake()
    {
        SaveSystem.Instance.RegisterSOSavable(this);

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    public void SetValue(string name, int value)
    {
        intVariables[name] = value;
    }

    public int GetValue(string name)
    {
        if (intVariables.TryGetValue(name, out int value))
            return value;

        return 0;
    }

    public bool Compare(string varName, string op, int compareValue)
    {
        int val = GetValue(varName);

        switch (op)
        {
            case "=": return val == compareValue;
            case ">": return val > compareValue;
            case "<": return val < compareValue;
            case ">=": return val >= compareValue;
            case "<=": return val <= compareValue;
            case "!=": return val != compareValue;
        }

        return false;
    }

    public Dictionary<string, int> GetAllVariables()
    {
        return intVariables;
    }

    public string variableName;
    public int value;

    public string GetSaveKey() => $"GlobalVar_{variableName}";

    public string SaveData()
    {
        var wrapper = new GlobalVariableWrapper
        {
            variables = new List<GlobalVariableData>()
        };

        foreach (var kvp in intVariables)
            wrapper.variables.Add(new GlobalVariableData { name = kvp.Key, value = kvp.Value });

        return JsonUtility.ToJson(wrapper, true);
    }

    public void LoadData(string json)
    {
        var wrapper = JsonUtility.FromJson<GlobalVariableWrapper>(json);
        intVariables.Clear();
        foreach (var v in wrapper.variables)
            intVariables[v.name] = v.value;
    }
}
