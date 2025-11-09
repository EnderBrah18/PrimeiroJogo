using System.Collections.Generic;
using UnityEngine;
using System.Collections;


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

    private static bool mapaGerado = false;
    public GameObject LoadingScreen;
    private GameObject player_;

    // ============================================================
    // ====================== GERAÇÃO ==============================
    // ============================================================

    private void Start()
    {
        LoadingScreen = GameObject.Find("LoadingScreen");
        player_ = GameObject.FindGameObjectWithTag("Player");
        mapaGerado = false;
        StartCoroutine(GenerateCoroutine());
    }

    public void Generate()
    {
        // Em vez de rodar diretamente, enfileira como tarefa
        StartCoroutine(GenerateCoroutine());
    }

    [ContextMenu("Gerar Dungeon")]

    public IEnumerator GenerateCoroutine()
    {
        ClearDungeon();
        yield return null;

        placedRooms.Clear();
        openRooms.Clear();

        Room start = Instantiate(startRoom.prefab, Vector3.zero, Quaternion.identity, this.transform).GetComponent<Room>();
        // Snap da sala inicial para a grade (evita drift)
        start.transform.position = new Vector3(
            Mathf.Round(start.transform.position.x / gridSize) * gridSize,
            start.transform.position.y,
            Mathf.Round(start.transform.position.z / gridSize) * gridSize
        );
        start.gridPos = Vector2Int.zero;
        placedRooms.Add(Vector2Int.zero, start);
        openRooms.Add(start);

        int roomCount = 1;

        // 🔹 Gera o conteúdo do primeiro tile antes de seguir
        TileSpawner startSpawner = start.GetComponentInChildren<TileSpawner>();
        if (startSpawner != null)
            yield return StartCoroutine(startSpawner.GenerateGridSpawnsCoroutine());

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

                if (placedRooms.ContainsKey(newGrid) || IsPositionOccupied(newGrid))
                    continue;

                var candidate = PickCompatiblePrefab(connector.direction);
                if (candidate == null)
                    continue;

                bool placed = false;

                int required = MaskUtils.DirectionToMask(MaskUtils.Opposite(connector.direction));

                // Só aceita se o prefab tiver abertura compatível
                if ((candidate.baseOpenMask & required) != 0)
                {
                    GameObject temp = Instantiate(candidate.prefab, Vector3.zero, Quaternion.identity, this.transform);
                    Room newRoom = temp.GetComponent<Room>();

                    Direction oppositeDir = MaskUtils.Opposite(connector.direction);
                    RoomConnector newOpposite = newRoom.GetConnector(oppositeDir);
                    if (newOpposite == null)
                    {
                        DestroyImmediate(temp);
                        continue;
                    }

                    // Alinhamento normal
                    Vector3 offsetWorld = connector.transform.position - newOpposite.transform.position;
                    temp.transform.position += offsetWorld;

                    // Snap ao grid
                    temp.transform.position = new Vector3(
                        Mathf.Round(temp.transform.position.x / gridSize) * gridSize,
                        temp.transform.position.y,
                        Mathf.Round(temp.transform.position.z / gridSize) * gridSize
                    );

                    newRoom.gridPos = new Vector2Int(
                        Mathf.RoundToInt(temp.transform.position.x / gridSize),
                        Mathf.RoundToInt(temp.transform.position.z / gridSize)
                    );

                    // 🔹 Verifica sobreposição física antes de registrar
                    bool overlaps = false;
                    foreach (var existingRoom in placedRooms.Values)
                    {
                        float dist = Vector3.Distance(existingRoom.transform.position, temp.transform.position);
                        if (dist < gridSize * 0.4f)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (overlaps)
                    {
                        Debug.LogWarning($"⚠ Sala '{candidate.prefab.name}' sobrepõe outra em {newRoom.gridPos} — destruída.");
                        DestroyImmediate(temp);
                        continue;
                    }

                    // 🔹 Marca conectores e registra a nova sala
                    connector.isConnected = true;
                    newOpposite.isConnected = true;

                    placedRooms.Add(newRoom.gridPos, newRoom);
                    openRooms.Add(newRoom);
                    roomCount++;

                    placed = true;

                    // 🔹 Aguarda TileSpawner (se existir)
                    TileSpawner spawner = newRoom.GetComponentInChildren<TileSpawner>();
                    if (spawner != null)
                        yield return StartCoroutine(spawner.GenerateGridSpawnsCoroutine());
                }
            }
        }

        yield return StartCoroutine(FillOpenConnectors());
        ConnectAdjacentDoors();
        ForceCloseAllUnconnected();

        Debug.Log($"✅ Dungeon gerada com {roomCount} salas conectadas (tiles e objetos).");
        mapaGerado = true;

        MapWasGenerated();
    }

    // ============================================================
    // ============= ETAPA 1 – FECHAMENTO AUTOMÁTICO ===============
    // ============================================================

    private IEnumerator FillOpenConnectors()
    {
        List<(RoomConnector, Vector2Int)> openEnds = new List<(RoomConnector, Vector2Int)>();
        int attempted = 0;     // quantas tentativas de criação de sala
        int created = 0;       // quantas salas realmente criadas
        int destroyed = 0;     // quantas foram destruídas por sobreposição ou erro

        Debug.Log($"🧩 [FillOpenConnectors] Iniciando preenchimento — salas existentes: {placedRooms.Count}");

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

        Debug.Log($"🔍 {openEnds.Count} conectores abertos identificados para verificação.");

        // 2️⃣ Tenta preencher cada conector aberto
        foreach (var entry in openEnds)
        {
            attempted++;

            var connector = entry.Item1;
            var gridPos = entry.Item2;

            if (placedRooms.ContainsKey(gridPos))
            {
                Debug.Log($"⚠ Grid {gridPos} já ocupado, pulando.");
                continue;
            }

            // Determina máscara dos vizinhos
            int neighborMask = 0;
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                Vector2Int neighborOffset = DirToOffset(dir);
                if (placedRooms.ContainsKey(gridPos + neighborOffset))
                    neighborMask |= MaskUtils.DirectionToMask(dir);
            }

            RoomPrefabData bestMatch = FindRoomByMaskAndDirection(neighborMask, connector.direction);
            if (bestMatch == null)
            {
                Debug.Log($"⚠ Nenhum prefab compatível encontrado para o conector em {gridPos}.");
                continue;
            }

            // 3️⃣ Instancia o prefab sem confiar no grid
            GameObject go = Instantiate(bestMatch.prefab, Vector3.zero, Quaternion.identity, this.transform);
            Room newRoom = go.GetComponent<Room>();

            Direction fromDir = MaskUtils.Opposite(connector.direction);
            RoomConnector newOpposite = newRoom.GetConnector(fromDir);

            if (newOpposite == null)
            {
                Debug.LogWarning($"❌ Prefab '{bestMatch.prefab.name}' não tem conector oposto ({fromDir}) — destruído.");
                DestroyImmediate(go);
                destroyed++;
                continue;
            }

            // 4️⃣ Alinha fisicamente pelo conector
            Vector3 offsetWorld = connector.transform.position - newOpposite.transform.position;
            go.transform.position += offsetWorld;

            // Snap ao grid
            go.transform.position = new Vector3(
                Mathf.Round(go.transform.position.x / gridSize) * gridSize,
                go.transform.position.y,
                Mathf.Round(go.transform.position.z / gridSize) * gridSize
            );

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
                Debug.LogWarning($"⚠ Sala '{bestMatch.prefab.name}' em {gridPos} sobrepõe outra — destruída.");
                DestroyImmediate(go);
                destroyed++;
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
            created++;

            TileSpawner spawner = newRoom.GetComponentInChildren<TileSpawner>();
            if (spawner != null)
                yield return StartCoroutine(spawner.GenerateGridSpawnsCoroutine());

            Debug.Log($"✅ Nova sala criada: '{bestMatch.prefab.name}' em {newRoom.gridPos} (total agora: {placedRooms.Count})");
        }

        Debug.Log($"🏁 [FillOpenConnectors] Finalizado — tentativas: {attempted}, criadas: {created}, destruídas: {destroyed}, total final de salas: {placedRooms.Count}");
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

        int capsCreated = 0;
        List<(RoomConnector, Vector2Int)> remaining = new List<(RoomConnector, Vector2Int)>();

        // 🔹 Coleta todos os conectores abertos
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

        // 🔹 Para cada conector aberto, instancia um cap se possível
        foreach (var entry in remaining)
        {
            var connector = entry.Item1;
            var grid = entry.Item2;

            if (placedRooms.ContainsKey(grid)) continue;

            // Instancia o prefab de fechamento
            GameObject go = Instantiate(capRoom.prefab, Vector3.zero, Quaternion.identity, this.transform);
            Room cap = go.GetComponent<Room>();

            Direction fromDir = MaskUtils.Opposite(connector.direction);
            RoomConnector capCon = cap.GetConnector(fromDir);

            if (capCon == null)
            {
                Debug.LogWarning($"⚠ Cap {cap.name} não possui conector oposto esperado ({fromDir}) — destruído.");
                DestroyImmediate(go);
                continue;
            }

            // 🔹 Calcula offset e aplica posição
            Vector3 offsetWorld = connector.transform.position - capCon.transform.position;
            go.transform.position += offsetWorld;

            // 🔹 Snap ao grid (evita drift acumulado)
            go.transform.position = new Vector3(
                Mathf.Round(go.transform.position.x / gridSize) * gridSize,
                go.transform.position.y,
                Mathf.Round(go.transform.position.z / gridSize) * gridSize
            );

            // 🔹 Verifica sobreposição física
            bool overlaps = false;
            foreach (var kvp in placedRooms)
            {
                float dist = Vector3.Distance(go.transform.position, kvp.Value.transform.position);
                if (dist < gridSize * 0.4f)
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
            {
                Debug.LogWarning($"⚠ Cap {cap.name} sobreposto — removido.");
                DestroyImmediate(go);
                continue;
            }

            // 🔹 Marca conectores como conectados
            connector.isConnected = true;
            capCon.isConnected = true;

            // 🔹 Registra a nova “sala” de fechamento
            cap.gridPos = grid;
            placedRooms.Add(grid, cap);
            capsCreated++;
        }

        Debug.Log($"✅ {capsCreated} caps criados para fechar aberturas restantes ({capRoom.prefab.name}).");
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

    private RoomPrefabData FindRoomByMaskAndDirection(int neighborMask, Direction requiredOpenDir)
    {
        List<RoomPrefabData> candidates = new List<RoomPrefabData>();
        int requiredBit = MaskUtils.DirectionToMask(MaskUtils.Opposite(requiredOpenDir));

        foreach (var prefab in roomPrefabs)
        {
            int prefabMask = prefab.baseOpenMask;
            if ((prefabMask & requiredBit) == 0)
                continue;

            // Aceita se o prefab compartilha pelo menos uma abertura com a máscara de vizinhança
            if (MaskUtils.CommonOpenings(prefabMask, neighborMask) > 0)
                candidates.Add(prefab);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
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

    private bool IsPositionOccupied(Vector2Int grid)
    {
        foreach (var r in placedRooms.Values)
        {
            Vector2Int g = new Vector2Int(
                Mathf.RoundToInt(r.transform.position.x / gridSize),
                Mathf.RoundToInt(r.transform.position.z / gridSize)
            );
            if (g == grid) return true;
        }
        return false;
    }

    public void MapWasGenerated()
    {
        if (!mapaGerado)
        {
            Debug.Log("O mapa ainda não foi gerado.");
            return;
        }

        Debug.Log("O mapa já foi gerado.");

        if (player_ != null)
        {
            player_.transform.position = new Vector3(0, 15, 0);
            Debug.Log("Player movido para o centro do mapa.");
        }

        LoadingScreen.SetActive(false);
    }
}
