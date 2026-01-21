using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation.Examples;

public class NodeSpwaner : MonoBehaviour
{
    [SerializeField]
    List<GameObject> m_NodeList = new List<GameObject>();

    [SerializeField]
    Transform spawnOrigin;
    
    // SpwanerFollower 참조 (경로를 따라 이동하는 스포너)
    private SpwanerFollower spawnerFollower;
    
    // 디버깅용 public getter
    public int NodeListCount { get { return m_NodeList != null ? m_NodeList.Count : 0; } }
    public bool IsNodeListNull { get { return m_NodeList == null; } }

    private void Start()
    {
        FindAndSetSpawnOrigin();
    }

    /// <summary>
    /// SpwanerFollower가 있는 오브젝트를 찾아서 spawnOrigin으로 설정
    /// 단순화된 구조: NodeSpawner -> RotateOffset (SpwanerFollower 컴포넌트 직접 붙임)
    /// </summary>
    private void FindAndSetSpawnOrigin()
    {
        // 1. SpwanerFollower 컴포넌트 찾기 (RotateOffset에 직접 붙어있음)
        if (spawnerFollower == null)
        {
            spawnerFollower = FindObjectOfType<SpwanerFollower>();
        }

        // 2. SpwanerFollower의 Transform 사용 (RotateOffset GameObject)
        if (spawnerFollower != null)
        {
            spawnOrigin = spawnerFollower.transform;
            Debug.Log($"[NodeSpwaner] Found SpwanerFollower on '{spawnOrigin.name}', using as spawnOrigin");
            return;
        }

        // 3. 자식 중에서 "RotateOffset" 찾기 (폴백)
        Transform rotateOffset = transform.Find("RotateOffset");
        if (rotateOffset != null)
        {
            spawnOrigin = rotateOffset;
            Debug.Log("[NodeSpwaner] Found RotateOffset child, using as spawnOrigin");
            return;
        }

        // 4. 인스펙터에서 할당된 spawnOrigin 확인
        if (spawnOrigin != null && spawnOrigin != transform)
        {
            // SpwanerFollower 컴포넌트가 있는지 확인
            if (spawnOrigin.GetComponent<SpwanerFollower>() != null)
            {
                Debug.Log("[NodeSpwaner] Using manually assigned spawnOrigin with SpwanerFollower");
                return;
            }
            else
            {
                Debug.LogWarning($"[NodeSpwaner] spawnOrigin '{spawnOrigin.name}' doesn't have SpwanerFollower component!");
            }
        }

        // 5. spawnOrigin이 NodeSpawner 자체를 가리키는 경우 (잘못된 할당)
        if (spawnOrigin == transform)
        {
            Debug.LogWarning("[NodeSpwaner] spawnOrigin is set to NodeSpawner itself! This is wrong.");
            spawnOrigin = null;
        }

        // 6. 최종 경고
        if (spawnOrigin == null)
        {
            Debug.LogError("[NodeSpwaner] spawnOrigin is null! Cannot find SpwanerFollower. Please ensure RotateOffset has SpwanerFollower component.");
        }
    }

    private void Update()
    {
        // spawnOrigin이 null이거나 잘못된 위치(NodeSpawner 자체)에 있으면 다시 찾기 시도
        if (spawnOrigin == null || spawnOrigin == transform)
        {
            FindAndSetSpawnOrigin();
        }

        if (Input.GetKeyDown(KeyCode.K)) 
        {
            SpawnNodeCentre();
        }

        if (Input.GetKeyDown(KeyCode.J)) 
        {
            SpawnNodeLeft();
        }

        if (Input.GetKeyDown(KeyCode.L)) 
        {
            SpawnNodeRight();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnNodeCentre(1);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            SpawnNodeLeft(1);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            SpawnNodeRight(1);
        }
    }

    public GameObject SpawnNodeCentre(int id = 0)
    {
        // spawnOrigin 확인
        if (spawnOrigin == null)
        {
            Debug.LogError("[NodeSpwaner] spawnOrigin is null! Cannot spawn node.");
            return null;
        }

        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"NodeSpwaner.cs:48\",\"message\":\"SpawnNodeCentre entry\",\"data\":{{\"id\":{id},\"nodeListCount\":{NodeListCount},\"isValidIndex\":{(!IsNodeListNull && id >= 0 && id < NodeListCount).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        // 경로 위의 중앙 위치에 스폰 (spawnOrigin의 위치와 회전 사용)
        Vector3 spawnPosition = spawnOrigin.position + spawnOrigin.rotation * Vector3.left * 0.5f;
        Quaternion spawnRotation = spawnOrigin.rotation;
        
        GameObject go = Instantiate(m_NodeList[id], spawnPosition, spawnRotation);
        return go;
    }
    
    public GameObject SpawnNodeLeft(int id = 0)
    {
        // spawnOrigin 확인
        if (spawnOrigin == null)
        {
            Debug.LogError("[NodeSpwaner] spawnOrigin is null! Cannot spawn node.");
            return null;
        }

        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"NodeSpwaner.cs:54\",\"message\":\"SpawnNodeLeft entry\",\"data\":{{\"id\":{id},\"nodeListCount\":{NodeListCount},\"isValidIndex\":{(!IsNodeListNull && id >= 0 && id < NodeListCount).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        // GameModeManager에서 레인 간격 가져오기
        float laneOffset = GameModeManager.instance != null ? GameModeManager.instance.laneOffset : 3f;
        
        // 경로 위의 왼쪽 위치에 스폰 (laneOffset 사용)
        Vector3 spawnPosition = spawnOrigin.position + spawnOrigin.rotation * Vector3.left * 0.5f + spawnOrigin.rotation * Vector3.down * laneOffset;
        Quaternion spawnRotation = spawnOrigin.rotation;
        
        GameObject go = Instantiate(m_NodeList[id], spawnPosition, spawnRotation);
        return go;
    }
    
    public GameObject SpawnNodeRight(int id = 0)
    {
        // spawnOrigin 확인
        if (spawnOrigin == null)
        {
            Debug.LogError("[NodeSpwaner] spawnOrigin is null! Cannot spawn node.");
            return null;
        }

        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"NodeSpwaner.cs:58\",\"message\":\"SpawnNodeRight entry\",\"data\":{{\"id\":{id},\"nodeListCount\":{NodeListCount},\"nodeListIsNull\":{IsNodeListNull.ToString().ToLower()},\"isValidIndex\":{(!IsNodeListNull && id >= 0 && id < NodeListCount).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        // GameModeManager에서 레인 간격 가져오기
        float laneOffset = GameModeManager.instance != null ? GameModeManager.instance.laneOffset : 3f;
        
        // 경로 위의 오른쪽 위치에 스폰 (laneOffset 사용)
        Vector3 spawnPosition = spawnOrigin.position + spawnOrigin.rotation * Vector3.left * 0.5f + spawnOrigin.rotation * Vector3.up * laneOffset;
        Quaternion spawnRotation = spawnOrigin.rotation;
        
        GameObject go = Instantiate(m_NodeList[id], spawnPosition, spawnRotation);
        return go;
    }
}
