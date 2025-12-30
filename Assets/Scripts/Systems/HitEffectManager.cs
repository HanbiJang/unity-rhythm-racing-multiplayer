using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중 시 이펙트를 관리하는 매니저
/// 타격 지점과 화면 테두리에 이펙트를 재생합니다.
/// </summary>
public class HitEffectManager : MonoBehaviour
{
    private static HitEffectManager instance;

    public static HitEffectManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HitEffectManager>();
                if (instance == null)
                {
                    GameObject tmp = new GameObject();
                    tmp.name = typeof(HitEffectManager).Name;
                    instance = tmp.AddComponent<HitEffectManager>();
                }
            }
            return instance;
        }
    }

    [Header("타격 지점 이펙트")]
    [SerializeField]
    private GameObject hitPointEffectPrefab;  // 타격 지점에 재생할 이펙트 프리팹 (FBX 파티클)

    [Header("화면 테두리 이펙트")]
    [SerializeField]
    private GameObject screenEdgeEffectPrefab;  // 화면 테두리에 재생할 이펙트 프리팹

    [SerializeField]
    private float screenEdgeOffset = 0.05f;  // 화면 테두리로부터의 오프셋 (0~1, 화면 크기의 비율)

    [SerializeField]
    private bool playOnAllEdges = true;  // 모든 테두리에 재생할지 여부

    [SerializeField]
    private ScreenEdge[] playOnEdges = new ScreenEdge[] 
    { 
        ScreenEdge.Top, 
        ScreenEdge.Bottom, 
        ScreenEdge.Left, 
        ScreenEdge.Right 
    };  // 특정 테두리에만 재생할 경우

    private Camera mainCamera;

    /// <summary>
    /// 화면 테두리 위치
    /// </summary>
    public enum ScreenEdge
    {
        Top,
        Bottom,
        Left,
        Right
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        // 메인 카메라 찾기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    /// <summary>
    /// 타격 지점에 이펙트를 재생합니다.
    /// </summary>
    /// <param name="hitPosition">타격 지점 (월드 좌표)</param>
    public void PlayHitPointEffect(Vector3 hitPosition)
    {
        if (hitPointEffectPrefab == null)
        {
            Debug.LogWarning("[HitEffectManager] Hit point effect prefab is not assigned!");
            return;
        }

        // 이펙트 인스턴스 생성
        GameObject effectInstance = Instantiate(hitPointEffectPrefab, hitPosition, Quaternion.identity);
        
        // 이펙트가 자동으로 파괴되지 않으면 ParticleSystem을 확인해서 재생 시간 후 파괴
        ParticleSystem[] particles = effectInstance.GetComponentsInChildren<ParticleSystem>();
        if (particles.Length > 0)
        {
            float maxDuration = 0f;
            foreach (var particle in particles)
            {
                float duration = particle.main.duration + particle.main.startLifetime.constantMax;
                if (duration > maxDuration)
                    maxDuration = duration;
            }
            
            if (maxDuration > 0f)
            {
                Destroy(effectInstance, maxDuration);
            }
        }
        else
        {
            // ParticleSystem이 없으면 기본 시간 후 파괴
            Destroy(effectInstance, 2f);
        }

        Debug.Log($"[HitEffectManager] Played hit point effect at {hitPosition}");
    }

    /// <summary>
    /// 화면 테두리에 이펙트를 재생합니다.
    /// </summary>
    public void PlayScreenEdgeEffect()
    {
        if (screenEdgeEffectPrefab == null)
        {
            Debug.LogWarning("[HitEffectManager] Screen edge effect prefab is not assigned!");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("[HitEffectManager] Main camera not found! Cannot play screen edge effect.");
            return;
        }

        if (playOnAllEdges)
        {
            // 모든 테두리에 재생
            PlayEdgeEffectAt(ScreenEdge.Top);
            PlayEdgeEffectAt(ScreenEdge.Bottom);
            PlayEdgeEffectAt(ScreenEdge.Left);
            PlayEdgeEffectAt(ScreenEdge.Right);
        }
        else
        {
            // 지정된 테두리에만 재생
            foreach (var edge in playOnEdges)
            {
                PlayEdgeEffectAt(edge);
            }
        }
    }

    /// <summary>
    /// 특정 화면 테두리에 이펙트를 재생합니다.
    /// </summary>
    /// <param name="edge">재생할 테두리 위치</param>
    private void PlayEdgeEffectAt(ScreenEdge edge)
    {
        Vector3 worldPosition = GetScreenEdgeWorldPosition(edge);
        
        if (worldPosition == Vector3.zero)
        {
            Debug.LogWarning($"[HitEffectManager] Could not calculate world position for edge: {edge}");
            return;
        }

        // 이펙트 인스턴스 생성
        GameObject effectInstance = Instantiate(screenEdgeEffectPrefab, worldPosition, Quaternion.identity);
        
        // 카메라를 향하도록 회전 (선택사항)
        if (mainCamera != null)
        {
            effectInstance.transform.LookAt(mainCamera.transform);
        }

        // 이펙트가 자동으로 파괴되지 않으면 ParticleSystem을 확인해서 재생 시간 후 파괴
        ParticleSystem[] particles = effectInstance.GetComponentsInChildren<ParticleSystem>();
        if (particles.Length > 0)
        {
            float maxDuration = 0f;
            foreach (var particle in particles)
            {
                float duration = particle.main.duration + particle.main.startLifetime.constantMax;
                if (duration > maxDuration)
                    maxDuration = duration;
            }
            
            if (maxDuration > 0f)
            {
                Destroy(effectInstance, maxDuration);
            }
        }
        else
        {
            // ParticleSystem이 없으면 기본 시간 후 파괴
            Destroy(effectInstance, 2f);
        }

        Debug.Log($"[HitEffectManager] Played screen edge effect at {edge}");
    }

    /// <summary>
    /// 화면 테두리의 월드 좌표를 계산합니다.
    /// </summary>
    /// <param name="edge">테두리 위치</param>
    /// <returns>월드 좌표</returns>
    private Vector3 GetScreenEdgeWorldPosition(ScreenEdge edge)
    {
        if (mainCamera == null)
            return Vector3.zero;

        Vector2 screenPoint = Vector2.zero;
        float depth = 10f;  // 카메라로부터의 거리 (필요에 따라 조정)

        switch (edge)
        {
            case ScreenEdge.Top:
                // 상단 중앙
                screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * (1f - screenEdgeOffset));
                break;
            case ScreenEdge.Bottom:
                // 하단 중앙
                screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * screenEdgeOffset);
                break;
            case ScreenEdge.Left:
                // 왼쪽 중앙
                screenPoint = new Vector2(Screen.width * screenEdgeOffset, Screen.height * 0.5f);
                break;
            case ScreenEdge.Right:
                // 오른쪽 중앙
                screenPoint = new Vector2(Screen.width * (1f - screenEdgeOffset), Screen.height * 0.5f);
                break;
        }

        // 화면 좌표를 월드 좌표로 변환
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        
        return worldPosition;
    }

    /// <summary>
    /// 타격 지점과 화면 테두리 이펙트를 모두 재생합니다.
    /// </summary>
    /// <param name="hitPosition">타격 지점 (월드 좌표)</param>
    public void PlayAllEffects(Vector3 hitPosition)
    {
        PlayHitPointEffect(hitPosition);
        PlayScreenEdgeEffect();
    }

    /// <summary>
    /// 이펙트 프리팹을 설정합니다.
    /// </summary>
    public void SetHitPointEffectPrefab(GameObject prefab)
    {
        hitPointEffectPrefab = prefab;
    }

    public void SetScreenEdgeEffectPrefab(GameObject prefab)
    {
        screenEdgeEffectPrefab = prefab;
    }
}

