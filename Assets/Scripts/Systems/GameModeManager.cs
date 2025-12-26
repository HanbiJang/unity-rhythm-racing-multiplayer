using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager instance;

    [SerializeField, Header("Move Speed of Tile")]
    float g_RoadMoveSpeed;
    public float m_RoadMoveSpeed { get { return g_RoadMoveSpeed; } private set { g_RoadMoveSpeed = value; } }


    [SerializeField, Header("Length of Background Music(seconds)")]
    public float g_SoundLength;

    [SerializeField, Header("Music Note Progress")]
    public int totalNoteCount = 0;  // 전체 음악 노트 개수
    public int currentNoteIndex = 0;  // 현재 진행한 노트 인덱스

    // Player Info
    public float m_PlayerScore;
    public int m_PlayerMaxHealth;
    public int m_PlayerHealth;

    public float s_RoadMoveSpeed;

    public void SetSpeed() { s_RoadMoveSpeed = m_RoadMoveSpeed; }


    public ObjectSpawner m_ObjectSpawner;

    public float m_CurrentTime { get; private set; }

    public void SetGameOver()
    {
        bGameOver = true;
        m_ObjectSpawner?.StopSpawning();
        
        // 서버로 EndGame 패킷 전송
        if (ServerInterface.Instance != null && (GameState.IsTestMode || (ServerInterface.Instance.SocketConnection != null && ServerInterface.Instance.SocketConnection.Connected)))
        {
            EndGameData endGameData = new EndGameData(GameState.Instance.UserId, GameState.Instance.RoomId);
            ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, endGameData, (int)EPacketID.EndGame);
            Debug.Log($"Sent EndGame: UserID={GameState.Instance.UserId}, RoomID={GameState.Instance.RoomId}");
        }
    }

    public void ResetForLobby()
    {
        // 스코어/체력/시간 초기화
        m_PlayerScore = 0f;
        m_PlayerHealth = 0;
        m_PlayerMaxHealth = 0;
        m_CurrentTime = 0f;

        // 스포너/런타임 오브젝트 정리
        m_ObjectSpawner?.StopSpawning();
        // 필요시 풀링 매니저 리셋, DOTween.KillAll(), Addressables Release 등

        // UI 되돌리기
        if (UIManager.instance != null)
        {
            if (UIManager.instance.m_InGameUI) UIManager.instance.m_InGameUI.SetActive(false);
            if (UIManager.instance.m_MenuUI) UIManager.instance.m_MenuUI.SetActive(true);
        }

        bGameOver = false;
    }

    public void GameStart()
    {
        m_CurrentTime = 0f;
        currentNoteIndex = 0;  // 게임 시작 시 초기화

        //if(LoadingRoad() == DONE)

        if (UIManager.instance.m_MenuUI != null && UIManager.instance.m_InGameUI != null)
        {
            UIManager.instance.m_MenuUI.SetActive(false);
            UIManager.instance.m_InGameUI.SetActive(true);
        }

        bGameOver = false;
    }

    // Start is called before the first frame update
    void Awake()
    {
        Screen.SetResolution(1080, 1920, false);
        if (instance == null)
            instance = this;

        DontDestroyOnLoad(gameObject);

        Apply();
    }

    public void Apply()
    {
        m_RoadMoveSpeed = g_RoadMoveSpeed;
        SetSpeed();
    }

    public bool bGameOver { get; private set; } = false;
    void Update()
    {
        if (m_ObjectSpawner == null) return;

        m_CurrentTime += Time.deltaTime;
        if (bGameOver)
            return;

        // 점수는 서버에서만 계산하고 ScoreBroadcast를 통해 받아옴
        // m_PlayerScore는 ScoreBroadcast에서 서버 점수로 업데이트됨

        if (m_ObjectSpawner.IsInvoking() && m_CurrentTime >= g_SoundLength - 7.7f)
        {
            //bGameOver = true;
            m_ObjectSpawner.StopSpawning();
        }
        if (m_CurrentTime >= g_SoundLength)
        {
            if (!bGameOver)
            {
                SetGameOver();
            }
        }


    }
}
