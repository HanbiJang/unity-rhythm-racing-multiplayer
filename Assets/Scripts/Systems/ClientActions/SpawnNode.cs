using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnNode : MonoBehaviour, IClientAction
{
    /// <summary>
    /// 노트가 플레이어에게 도달하는 예상 시간을 계산합니다.
    /// 서버에서 받은 노트 타이밍 정보를 사용합니다.
    /// </summary>
    private float CalculateExpectedTime(GameObject node, int nodeTimeMs)
    {
        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H1\",\"location\":\"SpawnNode.cs:CalculateExpectedTime\",\"message\":\"CalculateExpectedTime entry\",\"data\":{{\"nodeTimeMs\":{nodeTimeMs}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        if (GameModeManager.instance == null)
        {
            Debug.LogWarning("[SpawnNode] GameModeManager is null, using current time as expected time");
            return 0f;
        }

        // 서버에서 받은 노트 타이밍을 초 단위로 변환
        float serverNoteTime = nodeTimeMs / 1000f;

        // SpwanerFollower 찾기 (노트와 플레이어 사이의 거리와 속도를 얻기 위해)
        SpwanerFollower spawnerFollower = FindObjectOfType<SpwanerFollower>();
        
        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H2\",\"location\":\"SpawnNode.cs:CalculateExpectedTime\",\"message\":\"SpwanerFollower search\",\"data\":{{\"spawnerFollowerFound\":{(spawnerFollower != null).ToString().ToLower()},\"serverNoteTime\":{serverNoteTime}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        if (spawnerFollower == null)
        {
            Debug.LogWarning("[SpawnNode] SpwanerFollower not found, using server note time");
            // #region agent log
            try {
                string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H2\",\"location\":\"SpawnNode.cs:CalculateExpectedTime\",\"message\":\"SpwanerFollower not found\",\"data\":{{\"serverNoteTime\":{serverNoteTime}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
            } catch {}
            // #endregion
            return serverNoteTime;
        }

        // 노트와 플레이어 사이의 거리 계산
        float gapDistance = spawnerFollower.GapBetweenPlayer;

        // 노트 이동 속도는 SpwanerFollower.speed 기준
        float nodeSpeed = spawnerFollower.speed;

        // NoteMovement 컴포넌트가 있으면 그 속도를 우선 사용
        NoteMovement noteMovement = node.GetComponent<NoteMovement>();
        if (noteMovement != null && noteMovement.speed > 0f)
            nodeSpeed = noteMovement.speed;

        if (nodeSpeed <= 0f)
        {
            Debug.LogWarning("[SpawnNode] Node speed is 0 or negative, using server note time");
            // #region agent log
            try {
                string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H2\",\"location\":\"SpawnNode.cs:CalculateExpectedTime\",\"message\":\"Node speed invalid\",\"data\":{{\"nodeSpeed\":{nodeSpeed},\"spawnerSpeed\":{spawnerFollower.speed},\"serverNoteTime\":{serverNoteTime}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
            } catch {}
            // #endregion
            return serverNoteTime;
        }

        // 노트가 플레이어에게 도달하는 데 걸리는 시간
        float timeToReachPlayer = gapDistance / nodeSpeed;

        // 예상 타이밍 = 서버 노트 타이밍 + 도달 시간
        // 서버 노트 타이밍은 게임 시작 후 경과 시간이므로, 도달 시간을 더하면 됩니다
        float expectedTime = serverNoteTime + timeToReachPlayer;

        // 디버그 로그
        float currentTime = GameModeManager.instance.m_CurrentTime;
        Debug.Log($"[SpawnNode] NodeTimeMs: {nodeTimeMs}, ServerNoteTime: {serverNoteTime:F3}s, " +
                 $"GapDistance: {gapDistance:F2}, NodeSpeed: {nodeSpeed:F2}, TimeToReach: {timeToReachPlayer:F3}s, ExpectedTime: {expectedTime:F3}s, CurrentTime: {currentTime:F3}s");

        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H2\",\"location\":\"SpawnNode.cs:CalculateExpectedTime\",\"message\":\"CalculateExpectedTime result\",\"data\":{{\"nodeTimeMs\":{nodeTimeMs},\"serverNoteTime\":{serverNoteTime},\"gapDistance\":{gapDistance},\"nodeSpeed\":{nodeSpeed},\"timeToReachPlayer\":{timeToReachPlayer},\"expectedTime\":{expectedTime},\"currentTime\":{currentTime}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion

        return expectedTime;
    }
    // 중복 패킷 필터링용 (NodeTimeMs 기준)
    private static readonly System.Collections.Generic.HashSet<int> s_ProcessedNodeTimes
        = new System.Collections.Generic.HashSet<int>();

    public static void ClearProcessedNodes() => s_ProcessedNodeTimes.Clear();

    //서버에서 준 노드 데이터를 기반으로 노드를 스폰하는 코드
    public void Do(byte[] byteData)
    {
        //데이터 저장
        SpawnNodeData data = new SpawnNodeData();
        data.ConvertToGameData(byteData);

        // 동일 NodeTimeMs 패킷 중복 차단
        if (!s_ProcessedNodeTimes.Add(data.NodeTimeMs))
        {
            Debug.LogWarning($"[SpawnNode] 중복 패킷 무시 — NodeTimeMs={data.NodeTimeMs}");
            return;
        }
        // Debug.Log("NodeType " + data.NodeType + "NodePos " + data.NodePos);
        
        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H1\",\"location\":\"SpawnNode.cs:Do\",\"message\":\"Received SpawnNodeData from server\",\"data\":{{\"nodeType\":{data.NodeType},\"nodePos\":{data.NodePos},\"nodeTimeMs\":{data.NodeTimeMs}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion

        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"E\",\"location\":\"SpawnNode.cs:15\",\"message\":\"Received NodeType from server\",\"data\":{{\"nodeType\":{data.NodeType},\"nodePos\":{data.NodePos}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion

        //위 data 활용
        // 씬 전환 중일 수 있으므로 여러 번 시도
        NodeSpwaner ns = FindObjectOfType<NodeSpwaner>();

        if (ns == null)
        {
            Debug.LogWarning("NodeSpwaner not found! Scene might still be loading. Skipping node spawn.");
            return;
        }

        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\",\"location\":\"SpawnNode.cs:25\",\"message\":\"NodeSpwaner found, checking m_NodeList\",\"data\":{{\"nodeListCount\":{ns.NodeListCount},\"nodeListIsNull\":{ns.IsNodeListNull.ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion

        // 노드 타입에 따라 다른 노드 스폰 (0: ObjectA, 1: ObjectB, 2: ObjectC, 3: AFail, 4: BFail, 5: CFail)
        // Fail 타입은 3번 프리팹 사용
        int nodeId = data.NodeType < 3 ? data.NodeType : 3;
        
        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\",\"location\":\"SpawnNode.cs:30\",\"message\":\"Calculated nodeId\",\"data\":{{\"originalNodeType\":{data.NodeType},\"calculatedNodeId\":{nodeId},\"nodeListCount\":{ns.NodeListCount}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        // 인덱스 범위 체크 및 클램핑 (m_NodeList 크기에 맞춤)
        if (ns.IsNodeListNull || ns.NodeListCount == 0)
        {
            Debug.LogError($"[SpawnNode] NodeList is null or empty! Cannot spawn node.");
            return;
        }
        
        // nodeId를 유효한 범위로 클램핑
        if (nodeId < 0)
            nodeId = 0;
        else if (nodeId >= ns.NodeListCount)
        {
            Debug.LogWarning($"[SpawnNode] nodeId {nodeId} is out of range (max: {ns.NodeListCount - 1}). Clamping to {ns.NodeListCount - 1}");
            nodeId = ns.NodeListCount - 1;
        }
        
        // 노드 위치에 따라 스폰
        GameObject spawnedNode = null;
        
        // #region agent log
        try {
            bool isValidIndex = !ns.IsNodeListNull && nodeId >= 0 && nodeId < ns.NodeListCount;
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"SpawnNode.cs:35\",\"message\":\"Before spawning, validating nodeId\",\"data\":{{\"nodeId\":{nodeId},\"nodeListCount\":{ns.NodeListCount},\"nodePos\":{data.NodePos},\"isValidIndex\":{isValidIndex.ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        switch (data.NodePos)
        {
            case 0: // Left
                spawnedNode = ns.SpawnNodeLeft(nodeId);
                break;
            case 1: // Center
                spawnedNode = ns.SpawnNodeCentre(nodeId);
                break;
            case 2: // Right
                spawnedNode = ns.SpawnNodeRight(nodeId);
                break;
            default:
                spawnedNode = ns.SpawnNodeCentre(nodeId);
                break;
        }

        // 스폰된 노드에 타입 정보 및 예상 타이밍 설정
        if (spawnedNode != null)
        {
            PickupScript ps = spawnedNode.GetComponent<PickupScript>();
            if (ps != null)
            {
                ps.nodeType = data.NodeType;
                
                // 예상 타이밍 계산
                // 서버에서 받은 노트 타이밍 정보를 사용하여 계산
                float expectedTime = CalculateExpectedTime(spawnedNode, data.NodeTimeMs);
                ps.SetExpectedTime(expectedTime);
                
                Debug.Log($"[SpawnNode] Node spawned with expected time: {expectedTime:F3}s, NodeType: {data.NodeType}, NodePos: {data.NodePos}, NodeTimeMs: {data.NodeTimeMs}");
            }
        }

        // 노트가 스폰될 때마다 진행 인덱스 증가
        if (GameModeManager.instance != null)
        {
            GameModeManager.instance.currentNoteIndex++;
            // Debug.Log($"Note Progress: {GameModeManager.instance.currentNoteIndex} / {GameModeManager.instance.totalNoteCount}");
        }
    }
}
