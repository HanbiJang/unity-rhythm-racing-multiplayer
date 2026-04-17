using UnityEngine;

/// <summary>
/// AudioSource.GetSpectrumData()로 베이스 주파수를 읽어
/// 씬 조명, Skybox Exposure, Ambient Light를 음악 비트에 맞춰 반응시킵니다.
/// SoundManager.Instance의 AudioSource를 자동으로 사용합니다.
/// </summary>
public class BeatReactor : MonoBehaviour
{
    // SoundManager AudioSource 자동 참조 — Inspector 연결 불필요
    AudioSource m_AudioSource;

    [Header("반응 조명 목록 (선택)")]
    [SerializeField] Light[] m_Lights;

    [Header("조명 색상 (저강도 → 고강도)")]
    [SerializeField] Color m_LowColor  = new Color(0.2f, 0.1f, 0.5f);
    [SerializeField] Color m_HighColor = new Color(1f, 0.4f, 0f);

    [Header("조명 강도 범위")]
    [SerializeField] float m_MinIntensity = 0.5f;
    [SerializeField] float m_MaxIntensity = 8f;

    [Header("발광 머티리얼 반응 (Emission)")]
    [SerializeField] EmissiveTarget[] m_EmissiveTargets;
    [SerializeField, ColorUsage(false, true)] Color m_EmissionLow  = Color.black;
    [SerializeField, ColorUsage(false, true)] Color m_EmissionHigh = new Color(2f, 0.5f, 4f);

    [System.Serializable]
    public struct EmissiveTarget
    {
        public GameObject TargetObject;   // 오브젝트 (자식 포함 탐색)
        public int        MaterialIndex;  // 각 Renderer에서 몇 번째 머티리얼인지
    }

    [Header("하늘 반응 (Skybox)")]
    [SerializeField] bool  m_ReactSkybox      = true;
    [SerializeField] float m_SkyboxExpMin     = 0.5f;
    [SerializeField] float m_SkyboxExpMax     = 2.5f;

    [Header("환경광 반응 (Ambient)")]
    [SerializeField] bool  m_ReactAmbient     = true;
    [SerializeField] Color m_AmbientLow       = new Color(0.05f, 0.02f, 0.15f);
    [SerializeField] Color m_AmbientHigh      = new Color(0.6f, 0.2f, 1f);

    [Header("감도 / 반응 속도")]
    [SerializeField] float m_Sensitivity  = 100f;
    [SerializeField] float m_DecaySpeed   = 10f;

    [Header("베이스 주파수 구간 (FFT 빈 인덱스)")]
    [SerializeField] int m_BassStart = 0;
    [SerializeField] int m_BassEnd   = 5;

    float[] m_SpectrumData = new float[256];
    float   m_SmoothedLevel;

    void Update()
    {
        if (m_AudioSource == null)
            m_AudioSource = SoundManager.Instance != null
                ? SoundManager.Instance.GetComponent<AudioSource>() : null;

        if (m_AudioSource == null || !m_AudioSource.isPlaying) return;

        m_AudioSource.GetSpectrumData(m_SpectrumData, 0, FFTWindow.BlackmanHarris);

        // 베이스 구간 평균
        float bassLevel = 0f;
        int count = Mathf.Clamp(m_BassEnd, m_BassStart + 1, m_SpectrumData.Length) - m_BassStart;
        for (int i = m_BassStart; i < m_BassStart + count; i++)
            bassLevel += m_SpectrumData[i];
        bassLevel /= count;

        float target = bassLevel * m_Sensitivity;
        if (target > m_SmoothedLevel)
            m_SmoothedLevel = target;
        else
            m_SmoothedLevel = Mathf.Lerp(m_SmoothedLevel, target, m_DecaySpeed * Time.deltaTime);

        float t = Mathf.Clamp01(m_SmoothedLevel);

        // 조명
        if (m_Lights != null)
        {
            float intensity = Mathf.Lerp(m_MinIntensity, m_MaxIntensity, t);
            Color col = Color.Lerp(m_LowColor, m_HighColor, t);
            foreach (var light in m_Lights)
            {
                if (light == null) continue;
                light.intensity = intensity;
                light.color     = col;
            }
        }

        // Skybox Exposure
        if (m_ReactSkybox && RenderSettings.skybox != null
            && RenderSettings.skybox.HasProperty("_Exposure"))
        {
            float exp = Mathf.Lerp(m_SkyboxExpMin, m_SkyboxExpMax, t);
            RenderSettings.skybox.SetFloat("_Exposure", exp);
            DynamicGI.UpdateEnvironment();
        }

        // Ambient Light
        if (m_ReactAmbient)
            RenderSettings.ambientLight = Color.Lerp(m_AmbientLow, m_AmbientHigh, t);

        // 오브젝트별 Emission 반응
        if (m_EmissiveTargets != null && m_EmissiveTargets.Length > 0)
        {
            Color emissionColor = Color.Lerp(m_EmissionLow, m_EmissionHigh, t);
            foreach (var emissive in m_EmissiveTargets)
            {
                if (emissive.TargetObject == null) continue;
                var renderers = emissive.TargetObject.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    var mats = r.materials;
                    int idx = emissive.MaterialIndex;
                    if (idx < 0 || idx >= mats.Length) continue;
                    mats[idx].SetColor("_EmissionColor", emissionColor);
                }
            }
        }
    }
}
