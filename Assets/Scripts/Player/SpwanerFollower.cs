using UnityEngine;

/// <summary>
/// 노트 스포너를 플레이어 앞 고정 거리에 위치시킵니다.
/// 베지어 경로 이동 제거 — GapBetweenPlayer 만큼 Z 오프셋으로 고정.
/// </summary>
public class SpwanerFollower : MonoBehaviour
{
    [SerializeField]
    public float GapBetweenPlayer = 30f;

    [Tooltip("노트 이동 속도 (CalculateExpectedTime 계산에 사용)")]
    public float speed = 10f;

    private Transform playerTransform;

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (playerTransform == null)
            FindPlayer();

        if (playerTransform != null)
        {
            // X는 항상 0 고정 (레인 중심). 플레이어의 레인 이동 X를 따라가면 스폰 위치가 틀어짐
            transform.position = new Vector3(
                0f,
                playerTransform.position.y,
                playerTransform.position.z + GapBetweenPlayer
            );
        }
    }

    private void FindPlayer()
    {
        PlayerController pf = FindObjectOfType<PlayerController>();
        if (pf != null)
            playerTransform = pf.transform;
    }
}
