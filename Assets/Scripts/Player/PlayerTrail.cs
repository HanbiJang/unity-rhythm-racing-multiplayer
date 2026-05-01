using UnityEngine;

/// <summary>
/// 플레이어의 X 이동 히스토리를 기록하고 배경 속도 기반으로 Z를 계산해
/// 항상 일정한 꼬리를 유지합니다. 절대 사라지지 않습니다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PlayerTrail : MonoBehaviour
{
    [Header("트레일 설정")]
    [SerializeField] int   m_PointCount  = 40;    // 꼬리 점 개수 (많을수록 부드러움)
    [SerializeField] float m_SampleRate  = 30f;   // 초당 샘플링 횟수
    [SerializeField] float m_StartWidth  = 0.15f;
    [SerializeField] Color m_StartColor  = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] Color m_EndColor    = new Color(1f, 0.15f, 0.15f, 0f);

    LineRenderer m_Line;
    float[]      m_XHistory;      // 과거 X 위치 기록 (0 = 가장 최근)
    float        m_Timer;
    float        m_SampleInterval;
    Vector3[]    m_Pts;

    void Awake()
    {
        m_Line = GetComponent<LineRenderer>();
        m_Line.positionCount     = m_PointCount;
        m_Line.useWorldSpace     = true;
        m_Line.startWidth        = m_StartWidth;
        m_Line.endWidth          = 0f;
        m_Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_Line.receiveShadows    = false;

        m_SampleInterval = 1f / m_SampleRate;
        m_XHistory       = new float[m_PointCount];
        m_Pts            = new Vector3[m_PointCount];

        for (int i = 0; i < m_PointCount; i++)
            m_XHistory[i] = transform.position.x;

        ApplyGradient();
    }

    // Inspector에서 값 바꾸면 즉시 반영
    void OnValidate()
    {
        if (m_Line == null) m_Line = GetComponent<LineRenderer>();
        if (m_Line == null) return;
        m_Line.startWidth = m_StartWidth;
        ApplyGradient();
    }

    void Update()
    {
        float speed = GameModeManager.instance != null
            ? GameModeManager.instance.EffectiveBackgroundSpeed
            : 10f;

        // 일정 간격으로 현재 X 위치 샘플링
        m_Timer += Time.deltaTime;
        if (m_Timer >= m_SampleInterval)
        {
            m_Timer = 0f;
            System.Array.Copy(m_XHistory, 0, m_XHistory, 1, m_PointCount - 1);
            m_XHistory[0] = transform.position.x;
        }

        // 각 점의 Z = 인덱스 * 배경속도 * 샘플간격 (오래될수록 뒤로)
        float zStep = speed * m_SampleInterval;
        for (int i = 0; i < m_PointCount; i++)
        {
            m_Pts[i] = new Vector3(
                m_XHistory[i],
                transform.position.y,
                transform.position.z - i * zStep
            );
        }

        m_Line.SetPositions(m_Pts);
    }

    void ApplyGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(m_StartColor, 0f),
                new GradientColorKey(m_EndColor,   1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        m_Line.colorGradient = gradient;
    }
}
