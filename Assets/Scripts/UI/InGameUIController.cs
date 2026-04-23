using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUIController : MonoBehaviour
{
    public GameObject m_InGameUI;
    [SerializeField, Header("in-game UI Elements")]
    Image m_ProgressBarFillImage;  // Slider → Image Fill 방식으로 변경
    [SerializeField]
    Text m_txScore;
    [SerializeField]
    Text m_txCurrentRanking;

    [SerializeField, Range(0f, 1f)]
    float m_HealthRate;
    [SerializeField]
    Image m_HealthBarImageLeft;
    [SerializeField]
    Image m_HealthBarImageRight;

    [SerializeField, Header("My Player Name (Solo & Multi)")]
    Text m_txMyPlayerName;

    [SerializeField, Header("Other Player UI (Multi Only)")]
    Text m_txOtherPlayerName;
    [SerializeField]
    Text m_txOtherPlayerScore;
    [SerializeField]
    Text m_txOtherPlayerRanking;

    // PlayerController 참조 캐싱
    private PlayerController playerFollower;

    private bool m_isMultiMode;
    private float m_otherUpdateTimer = 0f;
    private const float OTHER_UPDATE_INTERVAL = 1f;

    // Start is called before the first frame update
    void Start()
    {
        m_isMultiMode = GameState.Instance != null && GameState.Instance.MatchMode == GameState.EMatchMode.Multi;

        // 내 닉네임 오브젝트 찾기 (SerializeField 미연결 시 폴백)
        if (m_txMyPlayerName == null)
        {
            var obj = GameObject.Find("PlayerName");
            if (obj != null) m_txMyPlayerName = obj.GetComponent<Text>();
        }
        // 닉네임은 게임 시작 전에 확정되므로 한 번만 세팅
        if (m_txMyPlayerName != null)
            m_txMyPlayerName.text = GameState.Instance?.PlayerNickname ?? "Player";

        // 씬에서 Other Player 오브젝트 이름으로 찾기 (SerializeField 미연결 시 폴백)
        if (m_txOtherPlayerName == null)
        {
            var obj = GameObject.Find("PlayerName_Other");
            if (obj != null) m_txOtherPlayerName = obj.GetComponent<Text>();
        }
        if (m_txOtherPlayerScore == null)
        {
            var obj = GameObject.Find("ScoreText_Other");
            if (obj != null) m_txOtherPlayerScore = obj.GetComponent<Text>();
        }
        if (m_txOtherPlayerRanking == null)
        {
            var obj = GameObject.Find("CurRankingText_Other");
            if (obj != null) m_txOtherPlayerRanking = obj.GetComponent<Text>();
        }

        // Solo 모드: Other Player UI 비활성화
        SetOtherPlayerUIActive(m_isMultiMode);

        // Road 씬에서 프로그레스바 찾기
        if (m_ProgressBarFillImage == null)
        {
            GameObject progressBarObj = GameObject.FindWithTag("MusicProgressBar");
            if (progressBarObj != null)
            {
                m_ProgressBarFillImage = progressBarObj.GetComponent<Image>();
                if (m_ProgressBarFillImage != null)
                {
                    m_ProgressBarFillImage.type = Image.Type.Filled;
                    m_ProgressBarFillImage.fillMethod = Image.FillMethod.Horizontal;
                    m_ProgressBarFillImage.fillAmount = 0f;
                }
            }
        }
    }

    void SetOtherPlayerUIActive(bool active)
    {
        if (m_txOtherPlayerName != null) m_txOtherPlayerName.gameObject.SetActive(active);
        if (m_txOtherPlayerScore != null) m_txOtherPlayerScore.gameObject.SetActive(active);
        if (m_txOtherPlayerRanking != null) m_txOtherPlayerRanking.gameObject.SetActive(active);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameModeManager.instance == null)
            return;

        // PlayerController 찾기 (처음 한 번만)
        if (playerFollower == null)
        {
            playerFollower = FindObjectOfType<PlayerController>();
        }

        // 점수 업데이트
        if (m_txScore != null)
        {
            m_txScore.text = Mathf.Floor(GameModeManager.instance.m_PlayerScore).ToString();
        }

        // 내 등수 업데이트
        if (m_txCurrentRanking != null)
        {
            int myRank = GetMyRanking();
            m_txCurrentRanking.text = myRank > 0 ? $"{myRank}st" : "-";
        }

        // 프로그레스바 업데이트 (노트 기준 진행도)
        if (m_ProgressBarFillImage != null)
        {
            if (playerFollower != null)
            {
                m_ProgressBarFillImage.fillAmount = playerFollower.GetNodeBasedProgress();
            }
            else
            {
                // PlayerController를 찾지 못한 경우 시간 기준으로 폴백
                if (GameModeManager.instance.g_SoundLength > 0)
                {
                    m_ProgressBarFillImage.fillAmount = GameModeManager.instance.m_CurrentTime / GameModeManager.instance.g_SoundLength;
                }
            }
        }

        // 체력바 업데이트
        if (GameModeManager.instance.m_PlayerMaxHealth > 0)
        {
            m_HealthRate = (float)GameModeManager.instance.m_PlayerHealth / GameModeManager.instance.m_PlayerMaxHealth;
        }

        if (m_HealthBarImageLeft != null && m_HealthBarImageRight != null)
        {
            m_HealthBarImageLeft.color = m_HealthBarImageRight.color = 0.7f * Color.red * Mathf.Lerp(1, 0, m_HealthRate);
        }

        // 멀티 모드: 상대방 정보 1초마다 갱신
        if (m_isMultiMode)
        {
            m_otherUpdateTimer += Time.deltaTime;
            if (m_otherUpdateTimer >= OTHER_UPDATE_INTERVAL)
            {
                m_otherUpdateTimer = 0f;
                UpdateOtherPlayerUI();
            }
        }
    }

    int GetMyRanking()
    {
        var list = GameState.Instance?.ScoreLIst;
        if (list == null || list.Count == 0) return 0;

        var sorted = new List<KeyValuePair<ulong, ulong>>(list);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        ulong myId = GameState.Instance.UserId;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Key == myId)
                return i + 1;
        }
        return 0;
    }

    void UpdateOtherPlayerUI()
    {
        var list = GameState.Instance?.ScoreLIst;
        if (list == null || list.Count < 2) return;

        var sorted = new List<KeyValuePair<ulong, ulong>>(list);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        ulong myId = GameState.Instance.UserId;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Key == myId) continue;

            ulong otherId = sorted[i].Key;
            ulong otherScore = sorted[i].Value;
            int otherRank = i + 1;

            if (m_txOtherPlayerScore != null)
                m_txOtherPlayerScore.text = otherScore.ToString();

            if (m_txOtherPlayerRanking != null)
                m_txOtherPlayerRanking.text = $"{otherRank}st";

            if (m_txOtherPlayerName != null)
            {
                string name = null;
                GameState.Instance.UserNicknames?.TryGetValue(otherId, out name);
                m_txOtherPlayerName.text = string.IsNullOrEmpty(name) ? "Player" : name;
            }
            break;
        }
    }
}
