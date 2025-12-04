using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestData))]
public class QuestDataEditor : Editor
{
    SerializedProperty steps;
    SerializedProperty questName;
    SerializedProperty rewardGold;
    SerializedProperty requirementVariable;
    SerializedProperty requirementOperator;
    SerializedProperty requirementValue;

    bool[] foldouts;
    bool[] deliverFoldouts; // para cada step, foldout dos deliverItems

    void OnEnable()
    {
        steps = serializedObject.FindProperty("steps");

        questName = serializedObject.FindProperty("questName");
        rewardGold = serializedObject.FindProperty("rewardGold");
        requirementVariable = serializedObject.FindProperty("requirementVariable");
        requirementOperator = serializedObject.FindProperty("requirementOperator");
        requirementValue = serializedObject.FindProperty("requirementValue");

        foldouts = new bool[Mathf.Max(1, steps?.arraySize ?? 1)];
        deliverFoldouts = new bool[Mathf.Max(1, steps?.arraySize ?? 1)];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Quest Config", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(questName);
        EditorGUILayout.PropertyField(rewardGold);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Requirement (Optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(requirementVariable);
        EditorGUILayout.PropertyField(requirementOperator);
        EditorGUILayout.PropertyField(requirementValue);

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("QUEST STEPS", EditorStyles.boldLabel);

        QuestData data = (QuestData)target;

        if (data.steps == null)
            data.steps = new System.Collections.Generic.List<QuestStep>();

        string[] stepIDs = new string[data.steps.Count];

        for (int i = 0; i < data.steps.Count; i++)
        {
            if (string.IsNullOrEmpty(data.steps[i].stepID))
                data.steps[i].stepID = $"Step_{i + 1}";

            stepIDs[i] = data.steps[i].stepID;
        }

        if (foldouts.Length != steps.arraySize)
        {
            foldouts = new bool[steps.arraySize];
            deliverFoldouts = new bool[steps.arraySize];
        }

        for (int i = 0; i < steps.arraySize; i++)
        {
            var step = steps.GetArrayElementAtIndex(i);
            string displayName = $"{step.FindPropertyRelative("stepID").stringValue} - {step.FindPropertyRelative("stepName").stringValue}";
            EditorGUILayout.BeginVertical("box");

            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], displayName, true);

            if (foldouts[i])
            {
                EditorGUILayout.PropertyField(step.FindPropertyRelative("stepID"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("stepName"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("targetID"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("requiredAmount"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("countProgressBeforeStart"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("completeVariableName"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("completeVariableValue"));

                // --- DeliverItems ---
                var deliverItemsProp = step.FindPropertyRelative("deliverItems");
                if (deliverItemsProp != null)
                {
                    deliverFoldouts[i] = EditorGUILayout.Foldout(deliverFoldouts[i], "Deliver Items", true);
                    if (deliverFoldouts[i])
                    {
                        EditorGUI.indentLevel++;
                        for (int j = 0; j < deliverItemsProp.arraySize; j++)
                        {
                            var item = deliverItemsProp.GetArrayElementAtIndex(j);
                            EditorGUILayout.BeginVertical("box");

                            EditorGUILayout.PropertyField(item.FindPropertyRelative("delvItem"), new GUIContent("Item SO"));
                            EditorGUILayout.PropertyField(item.FindPropertyRelative("amount"));
                            EditorGUILayout.PropertyField(item.FindPropertyRelative("targetVariable"));

                            if (GUILayout.Button("Remove Item"))
                                deliverItemsProp.DeleteArrayElementAtIndex(j);

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space();
                        }

                        if (GUILayout.Button("Add Deliver Item"))
                            deliverItemsProp.InsertArrayElementAtIndex(deliverItemsProp.arraySize);

                        EditorGUI.indentLevel--;
                    }
                }

                // --- Branches ---
                var branches = step.FindPropertyRelative("nextBranches");
                EditorGUILayout.PropertyField(branches, new GUIContent("Branches"), true);
                for (int b = 0; b < branches.arraySize; b++)
                {
                    var branch = branches.GetArrayElementAtIndex(b);
                    var nextStepIDProp = branch.FindPropertyRelative("nextStepID");
                    int currentIndex = Mathf.Max(0, System.Array.IndexOf(stepIDs, nextStepIDProp.stringValue));
                    int selectedIndex = EditorGUILayout.Popup("Next Step Target", currentIndex, stepIDs);
                    nextStepIDProp.stringValue = stepIDs[selectedIndex];
                }

                if (GUILayout.Button("Remove Step"))
                    steps.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add Step"))
        {
            steps.InsertArrayElementAtIndex(steps.arraySize);
            var newStep = steps.GetArrayElementAtIndex(steps.arraySize - 1);
            newStep.FindPropertyRelative("stepID").stringValue = $"Step_{steps.arraySize}";
            newStep.FindPropertyRelative("stepName").stringValue = "New Step";
        }

        serializedObject.ApplyModifiedProperties();
    }
}
