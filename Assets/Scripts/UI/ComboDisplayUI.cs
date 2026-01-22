using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 콤보를 화면에 표시하는 UI 컴포넌트
/// </summary>
public class ComboDisplayUI : MonoBehaviour
{
    private static ComboDisplayUI instance;

    public static ComboDisplayUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ComboDisplayUI>();
            }
            return instance;
        }
    }

    [Header("콤보 텍스트 UI")]
    [SerializeField]
    private Text comboText;

    [SerializeField]
    private Text comboLabelText; // "COMBO" 라벨 텍스트 (선택사항)

    [Header("애니메이션 설정")]
    [SerializeField]
    private float scaleAnimationDuration = 0.2f; // 스케일 애니메이션 시간
    [SerializeField]
    private float maxScale = 1.3f; // 최대 스케일
    [SerializeField]
    private float comboShowThreshold = 5; // 이 콤보 이상일 때만 표시

    [Header("색상 설정")]
    [SerializeField]
    private Color normalColor = Color.white;
    [SerializeField]
    private Color highComboColor = new Color(1f, 0.8f, 0f, 1f); // 높은 콤보일 때 색상 (노란색)

    [SerializeField]
    private int highComboThreshold = 50; // 높은 콤보로 간주하는 기준

    private CanvasGroup canvasGroup;
    private Coroutine scaleCoroutine;
    private int lastCombo = -1;

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

        // 초기 상태 설정
        UpdateComboDisplay(0);
    }

    private void OnEnable()
    {
        // ComboTracker 이벤트 구독
        if (ComboTracker.Instance != null)
        {
            ComboTracker.Instance.OnComboChanged += UpdateComboDisplay;
            ComboTracker.Instance.OnComboReset += OnComboReset;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (ComboTracker.Instance != null)
        {
            ComboTracker.Instance.OnComboChanged -= UpdateComboDisplay;
            ComboTracker.Instance.OnComboReset -= OnComboReset;
        }
    }

    /// <summary>
    /// 콤보 표시를 업데이트합니다.
    /// </summary>
    private void UpdateComboDisplay(int combo)
    {
        if (comboText == null)
        {
            Debug.LogWarning("[ComboDisplayUI] ComboText가 설정되지 않았습니다.");
            return;
        }

        // 콤보가 임계값 이상일 때만 표시
        if (combo >= comboShowThreshold)
        {
            canvasGroup.alpha = 1f;
            comboText.text = combo.ToString();
            
            // 높은 콤보일 때 색상 변경
            if (combo >= highComboThreshold)
            {
                comboText.color = highComboColor;
            }
            else
            {
                comboText.color = normalColor;
            }

            // 콤보가 증가했을 때만 애니메이션 재생
            if (combo > lastCombo && lastCombo >= comboShowThreshold)
            {
                PlayScaleAnimation();
            }
        }
        else
        {
            // 콤보가 낮으면 숨김
            canvasGroup.alpha = 0f;
        }

        lastCombo = combo;
    }

    /// <summary>
    /// 콤보가 리셋되었을 때 호출됩니다.
    /// </summary>
    private void OnComboReset()
    {
        // 페이드 아웃 애니메이션 (선택사항)
        StartCoroutine(FadeOutCoroutine());
    }

    /// <summary>
    /// 스케일 애니메이션을 재생합니다.
    /// </summary>
    private void PlayScaleAnimation()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleAnimationCoroutine());
    }

    /// <summary>
    /// 스케일 애니메이션 코루틴
    /// </summary>
    private IEnumerator ScaleAnimationCoroutine()
    {
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;

        // 스케일 업
        while (elapsed < scaleAnimationDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (scaleAnimationDuration * 0.5f);
            float scale = Mathf.Lerp(1f, maxScale, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        // 스케일 다운
        elapsed = 0f;
        while (elapsed < scaleAnimationDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (scaleAnimationDuration * 0.5f);
            float scale = Mathf.Lerp(maxScale, 1f, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
        scaleCoroutine = null;
    }

    /// <summary>
    /// 페이드 아웃 코루틴
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        float fadeDuration = 0.3f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 콤보 텍스트 UI를 설정합니다.
    /// </summary>
    public void SetComboText(Text text)
    {
        comboText = text;
    }

    /// <summary>
    /// 콤보 라벨 텍스트 UI를 설정합니다.
    /// </summary>
    public void SetComboLabelText(Text text)
    {
        comboLabelText = text;
    }
}
