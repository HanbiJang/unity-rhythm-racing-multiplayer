using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중 시 이펙트를 관리하는 매니저
/// 타격 지점 기반으로 화면에 보이는 위치에 이펙트를 재생합니다.
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

    [Header("카메라 기준 이펙트 깊이")]
    [SerializeField]
    private float hitEffectDepth = 6f;  // 카메라로부터의 거리(월드 단위)

    [Header("타격 이펙트 스케일")]
    [SerializeField]
    private float hitEffectScale = 2.2f;  // 이펙트 크기 배율

    private Camera mainCamera;

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
    /// 타격 지점 기반으로 카메라 시야에 보이는 위치에 이펙트를 재생합니다.
    /// </summary>
    /// <param name="hitPosition">타격 지점 (월드 좌표)</param>
    public void PlayHitPointEffect(Vector3 hitPosition)
    {
        if (hitPointEffectPrefab == null)
        {
            Debug.LogWarning("[HitEffectManager] Hit point effect prefab is not assigned!");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("[HitEffectManager] Main camera not found! Cannot play hit effect.");
            return;
        }

        Vector3 effectPosition = GetVisiblePositionFromCamera(hitPosition);

        // 이펙트 인스턴스 생성
        GameObject effectInstance = Instantiate(hitPointEffectPrefab, effectPosition, Quaternion.identity);
        if (hitEffectScale > 0f)
        {
            effectInstance.transform.localScale *= hitEffectScale;
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

        Debug.Log($"[HitEffectManager] Played hit point effect at {effectPosition}");
    }

    /// <summary>
    /// 타격 지점 방향의 카메라 레이 위에서 플레이어에게 보이는 위치를 계산합니다.
    /// </summary>
    private Vector3 GetVisiblePositionFromCamera(Vector3 hitPosition)
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 toHit = hitPosition - cameraPosition;
        Vector3 direction = toHit.sqrMagnitude > 0.0001f ? toHit.normalized : mainCamera.transform.forward;

        if (Vector3.Dot(mainCamera.transform.forward, direction) <= 0f)
        {
            direction = mainCamera.transform.forward;
        }

        float depth = Mathf.Max(hitEffectDepth, mainCamera.nearClipPlane + 0.05f);
        return cameraPosition + direction * depth;
    }

    /// <summary>
    /// 타격 지점과 화면 테두리 이펙트를 모두 재생합니다.
    /// </summary>
    /// <param name="hitPosition">타격 지점 (월드 좌표)</param>
    public void PlayAllEffects(Vector3 hitPosition)
    {
        PlayHitPointEffect(hitPosition);
    }

    /// <summary>
    /// 이펙트 프리팹을 설정합니다.
    /// </summary>
    public void SetHitPointEffectPrefab(GameObject prefab)
    {
        hitPointEffectPrefab = prefab;
    }

}

