using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/RoomPrefabData")]
public class RoomPrefabData : ScriptableObject
{
    // Mask: N=1, S=2, E=4, W=8
    public int baseOpenMask = 0; // máscara no "orientação 0" do prefab
    public GameObject prefab;
    public Vector3 size = Vector3.one; // para checar overlap (pode vir do collider)
}
