using UnityEngine;

public class InfiniteMapManager : MonoBehaviour
{
    [Tooltip("NoteMovement speed와 동일하게 설정")]
    public float speed = 10f;

    [Tooltip("맵 세그먼트 하나의 Z축 길이")]
    public float segmentLength = 100f;

    [Tooltip("재활용할 맵 세그먼트들 (2개 이상 권장)")]
    public Transform[] segments;

    public Transform playerTransform;

    void Update()
    {
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver)
            return;

        float playerZ = playerTransform != null ? playerTransform.position.z : 0f;

        // 모든 세그먼트 이동
        Vector3 delta = Vector3.back * speed * Time.deltaTime;
        foreach (var seg in segments)
            seg.Translate(delta, Space.World);

        // 가장 뒤(minZ)와 앞(maxZ) 세그먼트 탐색
        Transform rearmost = null;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (var seg in segments)
        {
            if (seg.position.z < minZ) { minZ = seg.position.z; rearmost = seg; }
            if (seg.position.z > maxZ) { maxZ = seg.position.z; }
        }

        // 가장 뒤 세그먼트가 플레이어를 지나면 맨 앞으로 순간이동
        if (rearmost != null && rearmost.position.z < playerZ)
        {
            Vector3 p = rearmost.position;
            rearmost.position = new Vector3(p.x, p.y, maxZ + segmentLength);

            Debug.Log($"[InfiniteMap] {rearmost.name} 재배치 → Z={maxZ + segmentLength}");
        }
    }
}
