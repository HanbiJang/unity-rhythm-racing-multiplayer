using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeSpwaner : MonoBehaviour
{
    [SerializeField]
    List<GameObject> m_NodeList = new List<GameObject>();

    [SerializeField]
    Transform spawnOrigin;
    
    // 디버깅용 public getter
    public int NodeListCount { get { return m_NodeList != null ? m_NodeList.Count : 0; } }
    public bool IsNodeListNull { get { return m_NodeList == null; } }

    private void Update()
    {
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
        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"NodeSpwaner.cs:48\",\"message\":\"SpawnNodeCentre entry\",\"data\":{{\"id\":{id},\"nodeListCount\":{NodeListCount},\"isValidIndex\":{(!IsNodeListNull && id >= 0 && id < NodeListCount).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        GameObject go = Instantiate(m_NodeList[id], spawnOrigin.position + spawnOrigin.rotation * Vector3.left * 0.5f, spawnOrigin.rotation);
        return go;
    }
    public GameObject SpawnNodeLeft(int id = 0)
    {
        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"NodeSpwaner.cs:54\",\"message\":\"SpawnNodeLeft entry\",\"data\":{{\"id\":{id},\"nodeListCount\":{NodeListCount},\"isValidIndex\":{(!IsNodeListNull && id >= 0 && id < NodeListCount).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        GameObject go = Instantiate(m_NodeList[id], spawnOrigin.position + spawnOrigin.rotation * Vector3.left * 0.5f + spawnOrigin.rotation * Vector3.down * 3, spawnOrigin.rotation);
        return go;
    }
    public GameObject SpawnNodeRight(int id = 0)
    {
        // #region agent log
        try {
            string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"NodeSpwaner.cs:58\",\"message\":\"SpawnNodeRight entry\",\"data\":{{\"id\":{id},\"nodeListCount\":{NodeListCount},\"nodeListIsNull\":{IsNodeListNull.ToString().ToLower()},\"isValidIndex\":{(!IsNodeListNull && id >= 0 && id < NodeListCount).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
        } catch {}
        // #endregion
        
        GameObject go = Instantiate(m_NodeList[id], spawnOrigin.position + spawnOrigin.rotation * Vector3.left * 0.5f + spawnOrigin.rotation * Vector3.up * 3, spawnOrigin.rotation);
        return go;
    }
}
