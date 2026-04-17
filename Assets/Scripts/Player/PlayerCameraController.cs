using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public static PlayerCameraController Instance { get; private set; }

    [SerializeField, Header("플레이어 Transform (자동으로 찾을 수도 있음)")]
    Transform m_PlayerTransform;

    [SerializeField, Header("카메라와 플레이어 사이의 거리 (뒤편)")]
    float m_FollowDistance = 10f;

    [SerializeField, Header("카메라 높이 오프셋")]
    float m_HeightOffset = 5f;

    [SerializeField, Header("카메라 이동 속도 (값이 클수록 빠르게 따라옴)")]
    [Range(0.1f, 10f)]
    float m_FollowSpeed = 5f;

    [SerializeField, Header("Player 태그 이름 (기본값: Player)")]
    string m_PlayerTag = "Player";

    [SerializeField, Header("플레이어 찾기 재시도 간격 (초)")]
    float m_FindPlayerRetryInterval = 1f;

    [Header("데드존 (이 범위 안에서는 카메라가 움직이지 않음)")]
    [SerializeField] float m_DeadZoneX = 1.5f;  // 좌우 허용 범위
    [SerializeField] float m_DeadZoneY = 1f;     // 상하 허용 범위

    [Header("카메라 셰이크")]
    [SerializeField] float m_ShakeDecay = 8f;

    [Header("콤보 FOV 효과")]
    [SerializeField] float m_BaseFOV = 60f;
    [SerializeField] float m_MaxFOVBonus = 15f;
    [SerializeField] int m_MaxComboForFOV = 50;
    [SerializeField] float m_FOVSmoothSpeed = 3f;

    bool m_PlayerFound = false;
    Camera m_Camera;
    float m_TargetFOV;

    // 카메라가 바라보는 월드 기준점 (데드존 중심)
    Vector3 m_AnchorPosition;

    float m_ShakeIntensity;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        m_Camera = GetComponentInChildren<Camera>();
        if (m_Camera == null) m_Camera = Camera.main;
        if (m_Camera != null)
        {
            m_BaseFOV = m_Camera.fieldOfView;
            m_TargetFOV = m_BaseFOV;
        }

        if (ComboTracker.Instance != null)
            ComboTracker.Instance.OnComboChanged += OnComboChanged;

        FindPlayer();
    }

    public static void Shake(float intensity)
    {
        if (Instance != null)
            Instance.m_ShakeIntensity = intensity;
    }

    void OnComboChanged(int combo)
    {
        float t = Mathf.Clamp01((float)combo / m_MaxComboForFOV);
        m_TargetFOV = m_BaseFOV + m_MaxFOVBonus * t;
    }

    void FindPlayer()
    {
        if (m_PlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(m_PlayerTag);
            if (playerObj != null)
            {
                m_PlayerTransform = playerObj.transform;
                m_PlayerFound = true;
            }
            else
            {
                m_PlayerFound = false;
                StartCoroutine(RetryFindPlayer());
                return;
            }
        }
        else
        {
            m_PlayerFound = true;
        }

        // 초기 앵커를 플레이어 위치로 설정
        m_AnchorPosition = m_PlayerTransform.position;
        UpdateCameraPosition();
    }

    IEnumerator RetryFindPlayer()
    {
        yield return new WaitForSeconds(m_FindPlayerRetryInterval);
        if (m_PlayerTransform == null) FindPlayer();
    }

    void LateUpdate()
    {
        if (!m_PlayerFound || m_PlayerTransform == null) return;

        UpdateCameraWithDeadZone();

        // 셰이크
        if (m_ShakeIntensity > 0.001f)
        {
            transform.position += Random.insideUnitSphere * m_ShakeIntensity;
            m_ShakeIntensity = Mathf.Lerp(m_ShakeIntensity, 0f, Time.deltaTime * m_ShakeDecay);
        }
        else
        {
            m_ShakeIntensity = 0f;
        }

        // 콤보 FOV
        if (m_Camera != null)
            m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, m_TargetFOV, Time.deltaTime * m_FOVSmoothSpeed);
    }

    void UpdateCameraWithDeadZone()
    {
        Vector3 playerPos = m_PlayerTransform.position;

        // 플레이어가 앵커 기준 데드존을 벗어났을 때만 앵커 이동
        float dx = playerPos.x - m_AnchorPosition.x;
        float dy = playerPos.y - m_AnchorPosition.y;

        if (Mathf.Abs(dx) > m_DeadZoneX)
            m_AnchorPosition.x += dx - Mathf.Sign(dx) * m_DeadZoneX;

        if (Mathf.Abs(dy) > m_DeadZoneY)
            m_AnchorPosition.y += dy - Mathf.Sign(dy) * m_DeadZoneY;

        // Z는 항상 따라감
        m_AnchorPosition.z = playerPos.z;

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        Vector3 targetPosition = m_AnchorPosition - Vector3.forward * m_FollowDistance;
        targetPosition.y = m_AnchorPosition.y + m_HeightOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, m_FollowSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    // 씬뷰에서 데드존 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(m_AnchorPosition, new Vector3(m_DeadZoneX * 2f, m_DeadZoneY * 2f, 0.1f));
    }
#endif
}
