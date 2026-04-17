using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 서버 지시에 따라 노트를 3개 레인(Left/Centre/Right)에 스폰합니다.
/// 베지어 경로 의존성 제거 — X축 laneOffset 기준으로 위치를 결정합니다.
/// </summary>
public class NodeSpwaner : MonoBehaviour
{
    public static NodeSpwaner Instance { get; private set; }

    [SerializeField]
    List<GameObject> m_NodeList = new List<GameObject>();

    [SerializeField]
    Transform spawnOrigin;

    public int NodeListCount  => m_NodeList != null ? m_NodeList.Count : 0;
    public bool IsNodeListNull => m_NodeList == null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[NodeSpwaner] 중복 인스턴스 감지, 파괴: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        FindSpawnOrigin();
    }

    void Update()
    {
        if (spawnOrigin == null)
            FindSpawnOrigin();

#if UNITY_EDITOR
        // 디버그 키 (에디터 테스트 전용)
        if (Input.GetKeyDown(KeyCode.K)) SpawnNodeCentre();
        if (Input.GetKeyDown(KeyCode.J)) SpawnNodeLeft();
        if (Input.GetKeyDown(KeyCode.L)) SpawnNodeRight();
        if (Input.GetKeyDown(KeyCode.B)) SpawnNodeCentre(1);
        if (Input.GetKeyDown(KeyCode.N)) SpawnNodeLeft(1);
        if (Input.GetKeyDown(KeyCode.M)) SpawnNodeRight(1);
#endif
    }

    private void FindSpawnOrigin()
    {
        if (spawnOrigin != null) return;

        SpwanerFollower sf = FindObjectOfType<SpwanerFollower>();
        if (sf != null)
        {
            spawnOrigin = sf.transform;
            Debug.Log($"[NodeSpwaner] SpwanerFollower를 스폰 기준으로 설정: {spawnOrigin.name}");
        }
        else
        {
            Debug.LogError("[NodeSpwaner] SpwanerFollower를 찾을 수 없습니다.");
        }
    }

    public GameObject SpawnNodeCentre(int id = 0)
    {
        if (!ValidateSpawn(id)) return null;
        return Instantiate(m_NodeList[id], spawnOrigin.position, Quaternion.identity);
    }

    public GameObject SpawnNodeLeft(int id = 0)
    {
        if (!ValidateSpawn(id)) return null;
        float laneOffset = GameModeManager.instance != null ? GameModeManager.instance.laneOffset : 3f;
        return Instantiate(m_NodeList[id], spawnOrigin.position + Vector3.left * laneOffset, Quaternion.identity);
    }

    public GameObject SpawnNodeRight(int id = 0)
    {
        if (!ValidateSpawn(id)) return null;
        float laneOffset = GameModeManager.instance != null ? GameModeManager.instance.laneOffset : 3f;
        return Instantiate(m_NodeList[id], spawnOrigin.position + Vector3.right * laneOffset, Quaternion.identity);
    }

    private bool ValidateSpawn(int id)
    {
        if (spawnOrigin == null)
        {
            Debug.LogError("[NodeSpwaner] spawnOrigin이 null입니다. 노드를 스폰할 수 없습니다.");
            return false;
        }
        if (IsNodeListNull || id < 0 || id >= NodeListCount)
        {
            Debug.LogError($"[NodeSpwaner] 유효하지 않은 노드 ID: {id} (리스트 크기: {NodeListCount})");
            return false;
        }
        return true;
    }
}
