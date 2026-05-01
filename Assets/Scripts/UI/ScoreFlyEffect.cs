using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 노트를 수집할 때 플레이어 위치에서 ScoreText로 날아가는 별똥별 이펙트.
/// RoadScene Canvas 아래에 붙여서 사용합니다.
/// </summary>
public class ScoreFlyEffect : MonoBehaviour
{
    public static ScoreFlyEffect Instance { get; private set; }

    [Header("참조")]
    [SerializeField] RectTransform m_ScoreTextRect;   // ScoreText의 RectTransform
    [SerializeField] Canvas        m_Canvas;           // 인게임 Canvas

    [Header("별 설정")]
    [SerializeField] Sprite m_StarSprite;              // 별 스프라이트 (없으면 흰 원으로 대체)
    [SerializeField] Color  m_StarColor  = Color.white;
    [SerializeField] float  m_StarSize   = 24f;
    [SerializeField] int    m_StarCount  = 5;          // 한 번에 날아가는 별 개수
    [SerializeField] float  m_SpawnDelay = 0.06f;      // 별 사이 발사 간격
    [SerializeField] float  m_FlyDuration = 0.6f;      // 날아가는 시간
    [SerializeField] float  m_ArcHeight  = 80f;        // 포물선 높이

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>플레이어 월드 좌표에서 ScoreText로 별을 날립니다.</summary>
    public void Play(Vector3 worldPosition)
    {
        if (m_ScoreTextRect == null || m_Canvas == null) return;
        StartCoroutine(SpawnStars(worldPosition));
    }

    IEnumerator SpawnStars(Vector3 worldPosition)
    {
        // 월드 좌표 → 캔버스 로컬 좌표 변환
        Vector2 startCanvasPos = WorldToCanvasPos(worldPosition);

        for (int i = 0; i < m_StarCount; i++)
        {
            // 시작 위치에 약간 랜덤 오프셋 (한 점에서 퍼지는 느낌)
            Vector2 offset = Random.insideUnitCircle * 30f;
            StartCoroutine(FlyOneStar(startCanvasPos + offset));
            yield return new WaitForSeconds(m_SpawnDelay);
        }
    }

    IEnumerator FlyOneStar(Vector2 startPos)
    {
        // 별 UI 생성
        GameObject star = new GameObject("Star", typeof(Image));
        star.transform.SetParent(m_Canvas.transform, false);
        star.transform.SetAsLastSibling();

        RectTransform rect = star.GetComponent<RectTransform>();
        rect.sizeDelta      = Vector2.one * m_StarSize;
        rect.anchoredPosition = startPos;

        Image img   = star.GetComponent<Image>();
        img.sprite  = m_StarSprite;
        img.color   = m_StarColor;
        img.raycastTarget = false;

        Vector2 endPos = m_ScoreTextRect.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < m_FlyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_FlyDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic

            // 포물선 경로
            Vector2 linear = Vector2.Lerp(startPos, endPos, ease);
            float arc = Mathf.Sin(Mathf.PI * t) * m_ArcHeight;
            rect.anchoredPosition = linear + Vector2.up * arc;

            // 끝에 가까워지면 서서히 사라짐
            img.color = new Color(m_StarColor.r, m_StarColor.g, m_StarColor.b,
                                  Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.7f) / 0.3f)));

            yield return null;
        }

        Destroy(star);
    }

    Vector2 WorldToCanvasPos(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_Canvas.GetComponent<RectTransform>(),
            screenPos,
            m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out Vector2 canvasPos
        );
        return canvasPos;
    }
}
