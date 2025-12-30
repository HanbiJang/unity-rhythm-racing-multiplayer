using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation.Examples;

public class SpawnNode : MonoBehaviour, IClientAction
{
    /// <summary>
    /// 노트가 플레이어에게 도달하는 예상 시간을 계산합니다.
    /// </summary>
    private float CalculateExpectedTime(GameObject node)
    {
        if (GameModeManager.instance == null)
        {
            Debug.LogWarning("[SpawnNode] GameModeManager is null, using current time as expected time");
            return 0f;
        }

        // 현재 게임 시간
        float currentTime = GameModeManager.instance.m_CurrentTime;

        // SpwanerFollower 찾기
        SpwanerFollower spawnerFollower = FindObjectOfType<SpwanerFollower>();
        if (spawnerFollower == null)
        {
            Debug.LogWarning("[SpawnNode] SpwanerFollower not found, using current time as expected time");
            return currentTime;
        }

        // 노트의 PathFollower 찾기
        PathFollower nodeFollower = node.GetComponent<PathFollower>();
        if (nodeFollower == null)
        {
            Debug.LogWarning("[SpawnNode] Node doesn't have PathFollower, using current time as expected time");
            return currentTime;
        }

        // 노트와 플레이어 사이의 거리 계산
        // SpwanerFollower가 플레이어보다 앞서 있는 거리를 가져옴
        float gapDistance = spawnerFollower.GapBetweenPlayer;

        // 노트의 이동 속도
        float nodeSpeed = nodeFollower.speed;
        if (nodeSpeed <= 0f)
        {
            Debug.LogWarning("[SpawnNode] Node speed is 0 or negative, using default calculation");
            // 기본값으로 대략적인 시간 계산 (예: 2초)
            return currentTime + 2f;
        }

        // 노트가 플레이어에게 도달하는 데 걸리는 시간
        float timeToReachPlayer = gapDistance / nodeSpeed;

        // 예상 타이밍 = 현재 시간 + 도달 시간
        float expectedTime = currentTime + timeToReachPlayer;

        return expectedTime;
    }
    //서버에서 준 노드 데이터를 기반으로 노드를 스폰하는 코드
    public void Do(byte[] byteData)
    {
        Debug.Log("SpawnNode()");

        //데이터 저장
        SpawnNodeData data = new SpawnNodeData();
        data.ConvertToGameData(byteData);
        Debug.Log("NodeType " + data.NodeType + "NodePos " + data.NodePos);

        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"E\",\"location\":\"SpawnNode.cs:15\",\"message\":\"Received NodeType from server\",\"data\":{{\"nodeType\":{data.NodeType},\"nodePos\":{data.NodePos}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion

        //위 data 활용
        // 씬 전환 중일 수 있으므로 여러 번 시도
        NodeSpwaner ns = null;
        GameObject editorOnlyObj = GameObject.FindWithTag("EditorOnly");
        if (editorOnlyObj != null)
        {
            ProxyScript proxyScript = editorOnlyObj.GetComponent<ProxyScript>();
            if (proxyScript != null && proxyScript.proxy != null)
            {
                ns = proxyScript.proxy.GetComponent<NodeSpwaner>();
            }
        }

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
        int nodeId = data.NodeType < 3 ? data.NodeType : data.NodeType - 3; // Fail 타입은 일반 노드와 같은 프리팹 사용
        
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
                // 노트가 플레이어에게 도달하는 시간을 계산
                // SpwanerFollower는 플레이어보다 GapBetweenPlayer만큼 앞서 있음
                // 노트의 이동 속도는 PathFollower의 speed를 사용
                float expectedTime = CalculateExpectedTime(spawnedNode);
                ps.SetExpectedTime(expectedTime);
                
                Debug.Log($"[SpawnNode] Node spawned with expected time: {expectedTime:F2}s, NodeType: {data.NodeType}, NodePos: {data.NodePos}");
            }
        }

        // 노트가 스폰될 때마다 진행 인덱스 증가
        if (GameModeManager.instance != null)
        {
            GameModeManager.instance.currentNoteIndex++;
            Debug.Log($"Note Progress: {GameModeManager.instance.currentNoteIndex} / {GameModeManager.instance.totalNoteCount}");
        }
    }
}
