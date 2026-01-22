using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리듬게임 판정 결과를 화면에 표시하는 UI 컴포넌트
/// </summary>
public class JudgmentDisplayUI : MonoBehaviour
{
    private static JudgmentDisplayUI instance;

    public static JudgmentDisplayUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JudgmentDisplayUI>();
            }
            return instance;
        }
    }

    [Header("판정 텍스트 UI")]
    [SerializeField]
    private Text judgmentText;

    [Header("판정 색상 설정")]
    [SerializeField]
    private Color perfectColor = new Color(1f, 1f, 0f, 1f); // 노란색
    [SerializeField]
    private Color goodColor = new Color(0f, 1f, 0f, 1f);     // 초록색
    [SerializeField]
    private Color badColor = new Color(1f, 0.5f, 0f, 1f);   // 주황색
    [SerializeField]
    private Color missColor = new Color(1f, 0f, 0f, 1f);    // 빨간색

    [Header("애니메이션 설정")]
    [SerializeField]
    private float displayDuration = 1.5f;  // 판정 표시 시간
    [SerializeField]
    private float fadeOutDuration = 0.5f;   // 페이드 아웃 시간
    [SerializeField]
    private float scaleAnimationDuration = 0.2f; // 스케일 애니메이션 시간
    [SerializeField]
    private float maxScale = 1.5f;         // 최대 스케일

    private Coroutine displayCoroutine;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // CanvasGroup이 없으면 추가
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 초기 상태: 투명
        if (judgmentText != null)
        {
            judgmentText.color = new Color(judgmentText.color.r, judgmentText.color.g, judgmentText.color.b, 0f);
        }
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 판정 결과를 화면에 표시합니다.
    /// </summary>
    /// <param name="judgmentType">판정 타입</param>
    public void ShowJudgment(JudgmentSystem.JudgmentType judgmentType)
    {
        if (judgmentText == null)
        {
            Debug.LogWarning("[JudgmentDisplayUI] JudgmentText가 설정되지 않았습니다.");
            return;
        }

        // 기존 코루틴이 실행 중이면 중지
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        // 판정 타입에 따른 텍스트와 색상 설정
        string judgmentString = JudgmentSystem.GetJudgmentTypeString(judgmentType);
        Color judgmentColor = GetJudgmentColor(judgmentType);

        judgmentText.text = judgmentString;
        judgmentText.color = judgmentColor;

        // 애니메이션 시작
        displayCoroutine = StartCoroutine(DisplayJudgmentCoroutine());
    }

    /// <summary>
    /// 판정 타입에 따른 색상을 반환합니다.
    /// </summary>
    private Color GetJudgmentColor(JudgmentSystem.JudgmentType judgmentType)
    {
        switch (judgmentType)
        {
            case JudgmentSystem.JudgmentType.Perfect:
                return perfectColor;
            case JudgmentSystem.JudgmentType.Good:
                return goodColor;
            case JudgmentSystem.JudgmentType.Bad:
                return badColor;
            case JudgmentSystem.JudgmentType.Miss:
                return missColor;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 판정 표시 애니메이션 코루틴
    /// </summary>
    private IEnumerator DisplayJudgmentCoroutine()
    {
        // 초기 스케일 설정
        transform.localScale = Vector3.one;

        // 페이드 인 및 스케일 업 애니메이션
        float elapsed = 0f;
        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleAnimationDuration;
            
            // 스케일 애니메이션 (0 -> maxScale -> 1.0)
            float scale = Mathf.Lerp(0f, maxScale, t);
            if (t > 0.5f)
            {
                scale = Mathf.Lerp(maxScale, 1.0f, (t - 0.5f) * 2f);
            }
            transform.localScale = Vector3.one * scale;

            // 알파 페이드 인
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        transform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;

        // 표시 시간 대기
        yield return new WaitForSeconds(displayDuration - scaleAnimationDuration);

        // 페이드 아웃 애니메이션
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        displayCoroutine = null;
    }

    /// <summary>
    /// 판정 텍스트 UI를 설정합니다.
    /// </summary>
    public void SetJudgmentText(Text text)
    {
        judgmentText = text;
    }
}
