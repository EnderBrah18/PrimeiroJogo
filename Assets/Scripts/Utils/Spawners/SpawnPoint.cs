using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string spawnID = "Default";

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
