using UnityEngine;

/// <summary>
/// 플레이어 통합 컨트롤러.
/// - 일반 모드 : A/S/D 입력 → 레인 전환 이동 + 노트 충돌 판정
/// - Miss Zone 모드 : 플레이어 뒤 트리거 오브젝트에 부착 → 놓친 노트 Miss 처리
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("레인 설정")]
    [SerializeField, Range(0.1f, 1f)]
    float m_MoveSpeed = 0.3f;

    [Header("Miss Zone 모드 (플레이어 뒤쪽 트리거 오브젝트에 붙일 때 체크)")]
    [SerializeField] bool m_IsMissZone = false;

    // ──────────────────────────────────────────
    bool m_bLeft;
    bool m_bRight;
    Vector3 m_TargetLocalPosition;

    void Awake()
    {
        // Rigidbody: OnTriggerEnter 동작에 필요, kinematic으로 물리 영향 차단
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Collider: isTrigger=true 이어야 OnTriggerEnter가 불림
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log($"[PlayerController] Collider isTrigger 자동 설정 ({gameObject.name})");
        }

        Debug.Log($"[PlayerController] Awake 완료 — isMissZone={m_IsMissZone}, " +
                  $"Collider={col?.GetType().Name ?? "없음"}, isTrigger={col?.isTrigger}, obj={gameObject.name}");
    }

    // ── 진행도 ──────────────────────────────────
    /// <summary>노트 기준 진행도 (0~1). 프로그레스바용.</summary>
    public float GetNodeBasedProgress()
    {
        if (GameModeManager.instance == null) return 0f;

        int total   = GameModeManager.instance.totalNoteCount;
        int current = GameModeManager.instance.currentNoteIndex;

        if (total <= 0)
        {
            float len = GameModeManager.instance.g_SoundLength;
            return len > 0 ? Mathf.Clamp01(GameModeManager.instance.m_CurrentTime / len) : 0f;
        }
        return Mathf.Clamp01((float)current / total);
    }

    // ── 매 프레임 ────────────────────────────────
    void Update()
    {
        if (m_IsMissZone) return;
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver) return;

        // 입력
        if      (Input.GetKeyDown(KeyCode.A)) { m_bLeft = true;  m_bRight = false; }
        else if (Input.GetKeyDown(KeyCode.D)) { m_bRight = true; m_bLeft  = false; }
        else if (Input.GetKeyDown(KeyCode.S)) { m_bLeft  = false; m_bRight = false; }

        // 레인 이동 (-X: 왼쪽, 0: 중앙, +X: 오른쪽)
        float lane = GameModeManager.instance != null ? GameModeManager.instance.laneOffset : 3f;
        float targetX = m_bLeft ? -lane : (m_bRight ? lane : 0f);

        m_TargetLocalPosition = new Vector3(targetX, 0f, 0f);

        transform.localPosition = Vector3.Lerp(transform.localPosition, m_TargetLocalPosition, m_MoveSpeed);
    }

    // ── 충돌 ─────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[PlayerController] OnTriggerEnter — tag={other.tag}, obj={other.name}, isMissZone={m_IsMissZone}");

        if (!other.CompareTag("PickupItem"))
        {
            // Debug.Log($"[PlayerController] 태그 불일치로 무시 (tag={other.tag})");
            return;
        }

        PickupScript ps = other.GetComponent<PickupScript>();
        if (ps == null)
        {
            Debug.LogWarning($"[PlayerController] PickupScript 컴포넌트 없음 — {other.name}");
            return;
        }

        if (m_IsMissZone)
        {
            Debug.Log($"[PlayerController] Miss Zone 처리 — {other.name}");
            if (!ps.bPicked) ps.OnMissed();
            else             Destroy(other.gameObject);
        }
        else
        {
            Debug.Log($"[PlayerController] OnPicked 호출 — {other.name}");
            ps.OnPicked(transform.position);
        }
    }
}
