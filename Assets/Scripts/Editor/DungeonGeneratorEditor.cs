using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DungeonGenerator gen = (DungeonGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button(" Gerar Dungeon", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                gen.ClearDungeon();
                gen.Generate();
            }
            else
            {
                Debug.LogWarning("Entre em Play Mode para gerar a dungeon!");
            }
        }

        if (GUILayout.Button(" Limpar Dungeon", GUILayout.Height(25)))
        {
            gen.ClearDungeon();
        }

    }
}
