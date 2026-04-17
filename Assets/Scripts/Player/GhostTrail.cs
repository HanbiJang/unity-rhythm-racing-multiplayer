using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 이동 시 반투명 고스트 잔상을 생성합니다.
/// Visual Transform(차체 메시)의 자식 MeshRenderer를 복사해 서서히 사라지게 합니다.
/// </summary>
public class GhostTrail : MonoBehaviour
{
    [Header("잔상 설정")]
    [SerializeField] Transform    m_VisualRoot;          // 차체 메시 루트 (PlayerController의 Visual Transform과 동일)
    [SerializeField] Material     m_GhostMaterial;       // 잔상용 투명 머티리얼
    [SerializeField] float        m_SpawnInterval = 0.05f;  // 잔상 생성 간격 (초)
    [SerializeField] float        m_LifeTime      = 0.3f;   // 잔상 유지 시간 (초)
    [SerializeField] Color        m_GhostColor    = new Color(0.5f, 0.8f, 1f, 0.5f);  // 잔상 색상

    float m_Timer;

    struct GhostInstance
    {
        public GameObject Root;
        public Material[] Mats;
        public float      BornTime;
    }

    readonly List<GhostInstance> m_Ghosts = new List<GhostInstance>();

    void Update()
    {
        m_Timer += Time.deltaTime;
        if (m_Timer >= m_SpawnInterval)
        {
            m_Timer = 0f;
            SpawnGhost();
        }

        FadeGhosts();
    }

    void SpawnGhost()
    {
        if (m_VisualRoot == null || m_GhostMaterial == null) return;

        GameObject ghostRoot = new GameObject("Ghost");
        ghostRoot.transform.SetPositionAndRotation(m_VisualRoot.position, m_VisualRoot.rotation);
        ghostRoot.transform.localScale = m_VisualRoot.lossyScale;

        var renderers = m_VisualRoot.GetComponentsInChildren<MeshRenderer>();
        var matList   = new List<Material>();

        foreach (var r in renderers)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var child = new GameObject("GhostMesh");
            child.transform.SetParent(ghostRoot.transform, false);
            child.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
            child.transform.localScale = r.transform.lossyScale;

            child.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var mr = child.AddComponent<MeshRenderer>();

            // 슬롯 수만큼 같은 고스트 머티리얼 적용
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(m_GhostMaterial);
                mats[i].color = m_GhostColor;
            }
            mr.materials = mats;
            matList.AddRange(mats);
        }

        m_Ghosts.Add(new GhostInstance
        {
            Root     = ghostRoot,
            Mats     = matList.ToArray(),
            BornTime = Time.time
        });
    }

    void FadeGhosts()
    {
        for (int i = m_Ghosts.Count - 1; i >= 0; i--)
        {
            var g = m_Ghosts[i];
            float age = Time.time - g.BornTime;
            float t   = age / m_LifeTime;

            if (t >= 1f)
            {
                Destroy(g.Root);
                m_Ghosts.RemoveAt(i);
                continue;
            }

            float alpha = Mathf.Lerp(m_GhostColor.a, 0f, t);
            Color c = new Color(m_GhostColor.r, m_GhostColor.g, m_GhostColor.b, alpha);
            foreach (var mat in g.Mats)
                if (mat != null) mat.color = c;
        }
    }
}
