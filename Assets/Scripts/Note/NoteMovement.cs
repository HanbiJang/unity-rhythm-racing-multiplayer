using UnityEngine;

/// <summary>
/// 노트를 -Z 방향(플레이어 쪽)으로 일정 속도로 이동시킵니다.
/// 노트 프리팹에 PathFollower 대신 이 컴포넌트를 붙이세요.
/// </summary>
public class NoteMovement : MonoBehaviour
{
    [Tooltip("노트 이동 속도 (단위: m/s)")]
    public float speed = 10f;

    void Start()
    {
        var col = GetComponent<Collider>();
        var rb  = GetComponent<Rigidbody>();
        Debug.Log($"[NoteMovement] 스폰됨 — {gameObject.name} | tag={gameObject.tag} | " +
                  $"Collider={col?.GetType().Name ?? "없음"} isTrigger={col?.isTrigger} | " +
                  $"Rigidbody={rb != null} | speed={speed}");
    }

    void Update()
    {
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver)
            return;

        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);
    }
}
