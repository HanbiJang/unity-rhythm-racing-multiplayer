using UnityEngine;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 콤보에 따라 Bloom 강도와 Motion Blur를 조절합니다.
/// 사용 방법:
/// 1. Package Manager에서 "Post Processing" 패키지 설치
/// 2. MainCamera에 Post Process Layer 컴포넌트 추가 (Layer: Everything)
/// 3. 씬에 빈 오브젝트 생성 → Post Process Volume 컴포넌트 추가 → Is Global 체크
///    → New Profile 생성 → Bloom / Motion Blur 항목 추가 및 체크
/// 4. 이 스크립트를 같은 오브젝트에 붙이기
/// </summary>
public class PostProcessingController : MonoBehaviour
{
    [Header("콤보 기준값 (이 콤보 이상이면 최대 효과)")]
    [SerializeField] int m_MaxCombo = 50;

    [Header("Bloom")]
    [SerializeField] float m_BloomMin = 1f;
    [SerializeField] float m_BloomMax = 6f;

    [Header("Motion Blur")]
    [SerializeField] float m_BlurMin = 0f;
    [SerializeField] float m_BlurMax = 180f;

    PostProcessVolume m_Volume;
    Bloom m_Bloom;
    MotionBlur m_MotionBlur;

    void Start()
    {
        m_Volume = GetComponent<PostProcessVolume>();
        if (m_Volume == null)
        {
            Debug.LogWarning("[PostProcessingController] PostProcessVolume이 없습니다.");
            return;
        }

        m_Volume.profile.TryGetSettings(out m_Bloom);
        m_Volume.profile.TryGetSettings(out m_MotionBlur);

        if (ComboTracker.Instance != null)
            ComboTracker.Instance.OnComboChanged += OnComboChanged;

        OnComboChanged(0);
    }

    void OnComboChanged(int combo)
    {
        float t = Mathf.Clamp01((float)combo / m_MaxCombo);

        if (m_Bloom != null)
            m_Bloom.intensity.value = Mathf.Lerp(m_BloomMin, m_BloomMax, t);

        if (m_MotionBlur != null)
            m_MotionBlur.shutterAngle.value = Mathf.Lerp(m_BlurMin, m_BlurMax, t);
    }

    void OnDestroy()
    {
        if (ComboTracker.Instance != null)
            ComboTracker.Instance.OnComboChanged -= OnComboChanged;
    }
}

#else

public class PostProcessingController : MonoBehaviour
{
    void Start()
    {
        Debug.LogWarning(
            "[PostProcessingController] Post Processing 패키지가 설치되지 않았습니다.\n" +
            "Window > Package Manager > Unity Registry에서 'Post Processing'을 검색해 설치하세요.");
    }
}

#endif
