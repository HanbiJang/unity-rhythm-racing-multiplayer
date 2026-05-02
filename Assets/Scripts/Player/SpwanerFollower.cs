using UnityEngine;

public class SpwanerFollower : MonoBehaviour
{
    public float GapBetweenPlayer = 30f;

    // 외부(SpawnNode)에서 속도를 읽을 때 사용 — GameModeManager 값을 반환
    public float speed => GameModeManager.instance != null
        ? GameModeManager.instance.m_RoadMoveSpeed
        : 40f;

    private Transform playerTransform;

    void Start() => FindPlayer();

    void Update()
    {
        if (playerTransform == null) FindPlayer();

        if (playerTransform != null)
        {
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
        if (pf != null) playerTransform = pf.transform;
    }
}
