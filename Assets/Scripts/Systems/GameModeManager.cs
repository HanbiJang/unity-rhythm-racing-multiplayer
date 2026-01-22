using System;
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

    [SerializeField, Header("Lane Settings")]
    [Tooltip("레인 간격 (플레이어 좌우 이동 폭 및 노트 스폰 위치 조절)")]
    public float laneOffset = 3f;

    // Player Info
    public float m_PlayerScore;
    public int m_PlayerMaxHealth;
    public int m_PlayerHealth;

    public float s_RoadMoveSpeed;

    public void SetSpeed() { s_RoadMoveSpeed = m_RoadMoveSpeed; }


    public ObjectSpawner m_ObjectSpawner;

    public float m_CurrentTime { get; private set; }
    bool m_useSyncedClock = false;
    double m_startUtcSeconds = 0;

    public void SetGameOver()
    {
        if (bGameOver)
            return;

        bGameOver = true;
        m_ObjectSpawner?.StopSpawning();
        
        // 서버로 EndGame 패킷 전송
        if (ServerInterface.Instance != null && (GameState.IsTestMode || (ServerInterface.Instance.SocketConnection != null && ServerInterface.Instance.SocketConnection.Connected)))
        {
            EndGameData endGameData = new EndGameData(GameState.Instance.UserId, GameState.Instance.RoomId);
            ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, endGameData, (int)EPacketID.EndGame);
            Debug.Log($"Sent EndGame: UserID={GameState.Instance.UserId}, RoomID={GameState.Instance.RoomId}");
        }

        ResultFlow.GoToResult();
    }

    public void ResetForLobby()
    {
        // 스코어/체력/시간 초기화
        m_PlayerScore = 0f;
        m_PlayerHealth = 0;
        m_PlayerMaxHealth = 0;
        m_CurrentTime = 0f;
        m_useSyncedClock = false;

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

        // 콤보 초기화
        if (ComboTracker.Instance != null)
        {
            ComboTracker.Instance.ResetForNewGame();
        }

        bGameOver = false;
    }

    public void SetSyncedStartTime(long startTimeUtcMs)
    {
        if (startTimeUtcMs <= 0)
        {
            m_useSyncedClock = false;
            return;
        }

        m_useSyncedClock = true;
        m_startUtcSeconds = startTimeUtcMs / 1000.0;
        m_CurrentTime = 0f;
    }

    // Start is called before the first frame update
    void Awake()
    {
        Screen.SetResolution(1080, 1920, false);
        
        // 이미 인스턴스가 있으면 새로 생성된 것을 파괴 (싱글톤 패턴)
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[GameModeManager] Duplicate instance detected. Destroying {gameObject.name}. Using existing instance from {instance.gameObject.name}.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);

        Apply();

        if (GameState.Instance != null && GameState.Instance.HasSyncedStartTime)
        {
            SetSyncedStartTime(GameState.Instance.SyncedStartTimeUtcMs);
        }
    }

    public void Apply()
    {
        m_RoadMoveSpeed = g_RoadMoveSpeed;
        SetSpeed();
    }

    public bool bGameOver { get; private set; } = false;
    void Update()
    {
        UpdateCurrentTime();
        if (m_ObjectSpawner == null) return;
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

    void UpdateCurrentTime()
    {
        if (m_useSyncedClock)
        {
            double nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            double elapsed = nowSeconds - m_startUtcSeconds;
            if (elapsed < 0)
            {
                m_CurrentTime = 0f;
                // #region agent log
                try {
                    string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H3\",\"location\":\"GameModeManager.cs:UpdateCurrentTime\",\"message\":\"Negative elapsed time\",\"data\":{{\"nowSeconds\":{nowSeconds},\"m_startUtcSeconds\":{m_startUtcSeconds},\"elapsed\":{elapsed}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                    System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
                } catch {}
                // #endregion
                return;
            }
            m_CurrentTime = (float)elapsed;
        }
        else
        {
            m_CurrentTime += Time.deltaTime;
        }
        
        // #region agent log (주기적으로 시간 확인 - 1초마다만)
        try {
            if (Time.frameCount % 60 == 0) { // 약 1초마다
                string logEntry = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"H3\",\"location\":\"GameModeManager.cs:UpdateCurrentTime\",\"message\":\"Current time update\",\"data\":{{\"m_CurrentTime\":{m_CurrentTime},\"m_useSyncedClock\":{m_useSyncedClock.ToString().ToLower()},\"m_startUtcSeconds\":{m_startUtcSeconds}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\GitRepo\Unity Racing Game\.cursor\debug.log", logEntry);
            }
        } catch {}
        // #endregion
    }
}
