using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonGenerator : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Sala inicial (sempre igual)")]
    public RoomPrefabData startRoom;

    [Tooltip("Lista de possíveis salas (9 tipos de prefabs, incluindo os de fechamento)")]
    public List<RoomPrefabData> roomPrefabs;

    [Tooltip("Prefab usado para fechar aberturas isoladas")]
    public RoomPrefabData capRoom;

    [Tooltip("Número máximo de salas geradas antes do fechamento")]
    public int maxRooms = 10;

    [Tooltip("Tamanho de um tile na grade (usado só pra registrar posições lógicas)")]
    public float gridSize = 50f;

    [Header("Debug / Visualização")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.cyan;

    private Dictionary<Vector2Int, Room> placedRooms = new Dictionary<Vector2Int, Room>();
    private List<Room> openRooms = new List<Room>();

    // ============================================================
    // ====================== GERAÇÃO ==============================
    // ============================================================

    [ContextMenu("Gerar Dungeon")]
    public void Generate()
    {
        ClearDungeon();

        placedRooms.Clear();
        openRooms.Clear();

        // 🔹 Cria a sala inicial
        Room start = Instantiate(startRoom.prefab, Vector3.zero, Quaternion.identity, this.transform).GetComponent<Room>();
        start.gridPos = Vector2Int.zero;
        placedRooms.Add(Vector2Int.zero, start);
        openRooms.Add(start);

        int roomCount = 1;

        // 🔹 Loop de geração
        while (roomCount < maxRooms && openRooms.Count > 0)
        {
            int randomIndex = Random.Range(0, openRooms.Count);
            Room current = openRooms[randomIndex];
            openRooms.RemoveAt(randomIndex);

            foreach (var connector in current.connectors)
            {
                if (connector.isConnected)
                    continue;

                Vector2Int offset = DirToOffset(connector.direction);
                Vector2Int newGrid = current.gridPos + offset;

                if (placedRooms.ContainsKey(newGrid))
                    continue;

                // 🔹 Controle de densidade (opcional)
                int neighborCount = CountNeighborRooms(newGrid);
                float densityChance = Mathf.Lerp(1f, 0.6f, neighborCount / 4f);
                if (Random.value > densityChance)
                    continue;

                // 🔹 Escolhe um prefab compatível
                var candidate = PickCompatiblePrefab(connector.direction);
                if (candidate == null)
                    continue;

                bool placedSuccessfully = false;

                for (int rot = 0; rot < 4 && !placedSuccessfully; rot++)
                {
                    int rotatedMask = MaskUtils.RotateMask(candidate.baseOpenMask, rot);
                    int required = MaskUtils.DirectionToMask(MaskUtils.Opposite(connector.direction));

                    if ((rotatedMask & required) == 0)
                        continue;

                    Quaternion rotQ = Quaternion.Euler(0, rot * 90f, 0);
                    GameObject temp = Instantiate(candidate.prefab, Vector3.zero, rotQ, this.transform);
                    Room newRoom = temp.GetComponent<Room>();

                    Direction oppositeDir = MaskUtils.Opposite(connector.direction);
                    RoomConnector newOpposite = newRoom.GetConnector(oppositeDir);
                    if (newOpposite == null)
                    {
                        DestroyImmediate(temp);
                        continue;
                    }

                    // 🔹 Alinha conectores
                    Vector3 offsetWorld = connector.transform.position - newOpposite.transform.position;
                    temp.transform.position += offsetWorld;

                    // 🔹 Calcula posição de grade
                    newRoom.gridPos = new Vector2Int(
                        Mathf.RoundToInt(temp.transform.position.x / gridSize),
                        Mathf.RoundToInt(temp.transform.position.z / gridSize)
                    );

                    // 🔹 Verifica sobreposição
                    bool overlaps = false;
                    foreach (var existingRoom in placedRooms.Values)
                    {
                        float dist = Vector3.Distance(existingRoom.transform.position, newRoom.transform.position);
                        if (dist < gridSize * 0.7f)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (overlaps)
                    {
                        DestroyImmediate(temp);
                        continue;
                    }

                    // 🔹 Marca conectores
                    connector.isConnected = true;
                    newOpposite.isConnected = true;

                    // 🔹 Registra a nova sala
                    placedRooms.Add(newRoom.gridPos, newRoom);
                    openRooms.Add(newRoom);
                    placedSuccessfully = true;
                    roomCount++;
                }
            }
        }


        FillOpenConnectors();
        ConnectAdjacentDoors();
        ForceCloseAllUnconnected();

        Debug.Log($"✅ Dungeon gerada com {roomCount} salas (fechada completamente).");
    }

    // ============================================================
    // ============= ETAPA 1 – FECHAMENTO AUTOMÁTICO ===============
    // ============================================================

    private void FillOpenConnectors()
    {
        List<(RoomConnector, Vector2Int)> openEnds = new List<(RoomConnector, Vector2Int)>();

        // 1️⃣ Coleta de conectores não conectados
        foreach (var kvp in placedRooms)
        {
            Room r = kvp.Value;
            foreach (var connector in r.connectors)
            {
                if (!connector.isConnected)
                {
                    Vector2Int offset = DirToOffset(connector.direction);
                    Vector2Int targetGrid = r.gridPos + offset;

                    if (!placedRooms.ContainsKey(targetGrid))
                        openEnds.Add((connector, targetGrid));
                }
            }
        }

        // 2️⃣ Tenta preencher cada conector aberto
        foreach (var entry in openEnds)
        {
            var connector = entry.Item1;
            var gridPos = entry.Item2;

            if (placedRooms.ContainsKey(gridPos))
                continue;

            // Determina máscara dos vizinhos
            int neighborMask = 0;
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                Vector2Int neighborOffset = DirToOffset(dir);
                if (placedRooms.ContainsKey(gridPos + neighborOffset))
                    neighborMask |= MaskUtils.DirectionToMask(dir);
            }

            RoomPrefabData bestMatch = FindRoomByMask(neighborMask);
            if (bestMatch == null) continue;

            // 3️⃣ Instancia o prefab sem confiar no grid
            GameObject go = Instantiate(bestMatch.prefab, Vector3.zero, Quaternion.identity, this.transform);
            Room newRoom = go.GetComponent<Room>();

            Direction fromDir = MaskUtils.Opposite(connector.direction);
            RoomConnector newOpposite = newRoom.GetConnector(fromDir);

            if (newOpposite == null)
            {
                DestroyImmediate(go);
                continue;
            }

            // 4️⃣ Alinha fisicamente pelo conector
            Vector3 offsetWorld = connector.transform.position - newOpposite.transform.position;
            go.transform.position += offsetWorld;

            // 5️⃣ Verifica sobreposição física após alinhar
            bool overlapsExisting = false;
            foreach (var kvp in placedRooms)
            {
                float dist = Vector3.Distance(kvp.Value.transform.position, go.transform.position);
                if (dist < gridSize * 0.4f)
                {
                    overlapsExisting = true;
                    break;
                }
            }

            if (overlapsExisting)
            {
                DestroyImmediate(go);
                continue;
            }

            // 6️⃣ Atualiza o gridPos baseado na posição real da sala
            newRoom.gridPos = new Vector2Int(
                Mathf.RoundToInt(go.transform.position.x / gridSize),
                Mathf.RoundToInt(go.transform.position.z / gridSize)
            );

            // 7️⃣ Marca os conectores como conectados
            connector.isConnected = true;
            newOpposite.isConnected = true;

            // 8️⃣ Registra a nova sala no dicionário
            placedRooms[newRoom.gridPos] = newRoom;
        }
    }

    // ============================================================
    // ============ ETAPA 2 – CONECTAR PORTAS VIZINHAS =============
    // ============================================================

    private void ConnectAdjacentDoors()
    {
        foreach (var kvp in placedRooms)
        {
            Room room = kvp.Value;

            foreach (var connector in room.connectors)
            {
                if (connector.isConnected) continue;

                Vector2Int targetPos = room.gridPos + DirToOffset(connector.direction);
                if (placedRooms.TryGetValue(targetPos, out Room neighbor))
                {
                    Direction oppositeDir = MaskUtils.Opposite(connector.direction);
                    RoomConnector opposite = neighbor.GetConnector(oppositeDir);
                    if (opposite != null && !opposite.isConnected)
                    {
                        connector.isConnected = true;
                        opposite.isConnected = true;
                    }
                }
            }
        }
    }

    // ============================================================
    // ======== ETAPA 3 – FECHAR RESTANTES COM CAP PREFAB ==========
    // ============================================================

    private void ForceCloseAllUnconnected()
    {
        if (capRoom == null)
        {
            Debug.LogWarning("⚠ Nenhum prefab de fechamento (capRoom) definido!");
            return;
        }

        List<(RoomConnector, Vector2Int)> remaining = new List<(RoomConnector, Vector2Int)>();

        foreach (var kvp in placedRooms)
        {
            Room r = kvp.Value;
            foreach (var connector in r.connectors)
            {
                if (!connector.isConnected)
                {
                    Vector2Int offset = DirToOffset(connector.direction);
                    Vector2Int newPos = r.gridPos + offset;
                    remaining.Add((connector, newPos));
                }
            }
        }

        foreach (var entry in remaining)
        {
            var connector = entry.Item1;
            var grid = entry.Item2;

            if (placedRooms.ContainsKey(grid)) continue;

            GameObject go = Instantiate(capRoom.prefab, Vector3.zero, Quaternion.identity, this.transform);
            Room cap = go.GetComponent<Room>();
            cap.gridPos = grid;

            Direction fromDir = MaskUtils.Opposite(connector.direction);
            RoomConnector capCon = cap.GetConnector(fromDir);

            if (capCon != null)
            {
                Vector3 offsetWorld = connector.transform.position - capCon.transform.position;
                go.transform.position += offsetWorld;
                connector.isConnected = true;
                capCon.isConnected = true;
            }

            placedRooms.Add(grid, cap);
        }

        Debug.Log($"🔹 Todas as aberturas restantes foram fechadas com {capRoom.prefab.name}.");
    }

    // ============================================================
    // ====================== LIMPEZA ==============================
    // ============================================================

    [ContextMenu("Limpar Dungeon")]
    public void ClearDungeon()
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in transform)
            toDestroy.Add(child.gameObject);

        foreach (var obj in toDestroy)
        {
            if (obj == null) continue;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        placedRooms.Clear();
        openRooms.Clear();

#if UNITY_EDITOR
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
#endif
    }

    // ============================================================
    // ====================== UTILITÁRIOS ==========================
    // ============================================================

    private int CountNeighborRooms(Vector2Int pos)
    {
        int count = 0;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var d in dirs)
        {
            if (placedRooms.ContainsKey(pos + d)) count++;
        }
        return count;
    }

    private RoomPrefabData FindRoomByMask(int mask)
    {
        RoomPrefabData best = null;
        int bestScore = int.MaxValue;

        foreach (var prefab in roomPrefabs)
        {
            int diff = Mathf.Abs(prefab.baseOpenMask - mask);
            if (diff < bestScore)
            {
                best = prefab;
                bestScore = diff;
            }
        }
        return best;
    }

    private RoomPrefabData PickCompatiblePrefab(Direction dir)
    {
        List<RoomPrefabData> compatible = new List<RoomPrefabData>();
        int requiredMask = MaskUtils.DirectionToMask(MaskUtils.Opposite(dir));

        foreach (var prefab in roomPrefabs)
        {
            if ((prefab.baseOpenMask & requiredMask) != 0)
                compatible.Add(prefab);
        }

        if (compatible.Count == 0)
        {
            Debug.LogWarning($"Nenhum prefab compatível encontrado para {dir}");
            return null;
        }

        return compatible[Random.Range(0, compatible.Count)];
    }

    private Vector2Int DirToOffset(Direction d)
    {
        return d switch
        {
            Direction.North => new Vector2Int(0, 1),
            Direction.South => new Vector2Int(0, -1),
            Direction.East => new Vector2Int(1, 0),
            Direction.West => new Vector2Int(-1, 0),
            _ => Vector2Int.zero
        };
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = gizmoColor;

        foreach (var kvp in placedRooms)
        {
            if (kvp.Value == null) continue;
            Gizmos.DrawWireCube(kvp.Value.transform.position, Vector3.one * 5f);
        }
    }
}
