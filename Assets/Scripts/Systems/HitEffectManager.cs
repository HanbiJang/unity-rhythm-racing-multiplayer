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

    // 판정 타입별 색상
    static readonly Color ColorPerfect = new Color(1f, 0.85f, 0f);   // 골드
    static readonly Color ColorGood    = new Color(0f, 1f, 0.4f);    // 초록
    static readonly Color ColorBad     = new Color(0.3f, 0.5f, 1f);  // 파랑
    static readonly Color ColorDefault = Color.white;

    /// <summary>판정 타입에 맞는 색상으로 이펙트를 재생합니다.</summary>
    public void PlayJudgmentEffect(Vector3 hitPosition, JudgmentSystem.JudgmentType type)
    {
        Color color = type switch
        {
            JudgmentSystem.JudgmentType.Perfect => ColorPerfect,
            JudgmentSystem.JudgmentType.Good    => ColorGood,
            JudgmentSystem.JudgmentType.Bad     => ColorBad,
            _                                   => ColorDefault,
        };
        PlayHitPointEffect(hitPosition, color);
    }

    /// <summary>
    /// 타격 지점 기반으로 카메라 시야에 보이는 위치에 이펙트를 재생합니다.
    /// </summary>
    public void PlayHitPointEffect(Vector3 hitPosition, Color tint = default)
    {
        if (hitPointEffectPrefab == null || mainCamera == null) return;

        if (tint == default) tint = ColorDefault;

        Vector3 effectPosition = GetVisiblePositionFromCamera(hitPosition);
        GameObject effectInstance = Instantiate(hitPointEffectPrefab, effectPosition, Quaternion.identity);

        if (hitEffectScale > 0f)
            effectInstance.transform.localScale *= hitEffectScale;

        // 파티클 색상 적용
        foreach (var ps in effectInstance.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startColor = tint;
        }

        // 재생 시간 후 파괴
        float maxDuration = 2f;
        foreach (var ps in effectInstance.GetComponentsInChildren<ParticleSystem>())
        {
            float d = ps.main.duration + ps.main.startLifetime.constantMax;
            if (d > maxDuration) maxDuration = d;
        }
        Destroy(effectInstance, maxDuration);
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

