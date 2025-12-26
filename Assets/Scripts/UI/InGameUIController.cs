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

    // PlayerFollower 참조 캐싱
    private PlayerFollower playerFollower;


    // Start is called before the first frame update
    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {
        if (GameModeManager.instance == null)
            return;

        // PlayerFollower 찾기 (처음 한 번만)
        if (playerFollower == null)
        {
            playerFollower = FindObjectOfType<PlayerFollower>();
        }

        // 점수 업데이트
        if (m_txScore != null)
        {
            m_txScore.text = Mathf.Floor(GameModeManager.instance.m_PlayerScore).ToString();
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
                // PlayerFollower를 찾지 못한 경우 시간 기준으로 폴백
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
    }
}
