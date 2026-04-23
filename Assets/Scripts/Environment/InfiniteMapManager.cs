using UnityEngine;

public class InfiniteMapManager : MonoBehaviour
{
    [Tooltip("맵 세그먼트 하나의 Z축 길이")]
    public float segmentLength = 100f;

    [Tooltip("재활용할 맵 세그먼트들 (2개 이상 권장)")]
    public Transform[] segments;

    public Transform playerTransform;

    float Speed => GameModeManager.instance != null
        ? GameModeManager.instance.EffectiveBackgroundSpeed
        : 10f;

    void Update()
    {
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver)
            return;

        float playerZ = playerTransform != null ? playerTransform.position.z : 0f;

        Vector3 delta = Vector3.back * Speed * Time.deltaTime;
        foreach (var seg in segments)
            seg.Translate(delta, Space.World);

        Transform rearmost = null;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (var seg in segments)
        {
            if (seg.position.z < minZ) { minZ = seg.position.z; rearmost = seg; }
            if (seg.position.z > maxZ) { maxZ = seg.position.z; }
        }

        if (rearmost != null && rearmost.position.z < playerZ)
        {
            Vector3 p = rearmost.position;
            rearmost.position = new Vector3(p.x, p.y, maxZ + segmentLength);
        }
    }
}
