using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField, Header("플레이어 Transform (자동으로 찾을 수도 있음)")]
    Transform m_PlayerTransform;
    
    [SerializeField, Header("카메라와 플레이어 사이의 거리 (뒤편)")]
    float m_FollowDistance = 10f;
    
    [SerializeField, Header("카메라 높이 오프셋")]
    float m_HeightOffset = 5f;
    
    [SerializeField, Header("플레이어를 바라보는 속도 (값이 클수록 빠르게 플레이어를 바라봄)")]
    [Range(0.1f, 10f)]
    float m_LookSpeed = 2f;
    
    [SerializeField, Header("카메라 이동 속도 (값이 클수록 빠르게 따라옴)")]
    [Range(0.1f, 10f)]
    float m_FollowSpeed = 5f;
    
    [SerializeField, Header("Player 태그 이름 (기본값: Player)")]
    string m_PlayerTag = "Player";
    
    [SerializeField, Header("플레이어 찾기 재시도 간격 (초)")]
    float m_FindPlayerRetryInterval = 1f;
    
    bool m_PlayerFound = false;

    void Start()
    {
        FindPlayer();
    }
    
    void FindPlayer()
    {
        // 플레이어 Transform을 찾지 못했다면 자동으로 찾기
        if (m_PlayerTransform == null)
        {
            // Player 태그로 찾기
            GameObject playerObj = GameObject.FindGameObjectWithTag(m_PlayerTag);
            if (playerObj != null)
            {
                m_PlayerTransform = playerObj.transform;
                m_PlayerFound = true;
                Debug.Log($"PlayerCameraController: 플레이어를 찾았습니다. 위치: {m_PlayerTransform.position}");
            }
            else
            {
                Debug.LogWarning($"PlayerCameraController: 플레이어를 찾을 수 없습니다. {m_FindPlayerRetryInterval}초 후 다시 시도합니다.");
                m_PlayerFound = false;
                StartCoroutine(RetryFindPlayer());
            }
        }
        else
        {
            m_PlayerFound = true;
            Debug.Log($"PlayerCameraController: Inspector에서 설정된 플레이어를 사용합니다. 위치: {m_PlayerTransform.position}");
        }
        
        // 플레이어를 찾았다면 초기 위치 설정
        if (m_PlayerFound && m_PlayerTransform != null)
        {
            UpdateCameraPosition(1f); // 즉시 위치 설정
        }
    }
    
    IEnumerator RetryFindPlayer()
    {
        yield return new WaitForSeconds(m_FindPlayerRetryInterval);
        if (m_PlayerTransform == null)
        {
            FindPlayer();
        }
    }

    void LateUpdate()
    {
        // 플레이어를 찾지 못했다면 업데이트하지 않음
        if (!m_PlayerFound || m_PlayerTransform == null)
            return;

        UpdateCameraPosition(m_FollowSpeed * Time.deltaTime);
    }
    
    void UpdateCameraPosition(float lerpSpeed)
    {
        // 플레이어의 뒤편 위치 계산
        Vector3 playerPosition = m_PlayerTransform.position;
        Vector3 playerForward = m_PlayerTransform.forward;
        
        // 플레이어의 뒤편 위치 (플레이어의 forward 방향의 반대)
        Vector3 targetPosition = playerPosition - playerForward * m_FollowDistance;
        targetPosition.y = playerPosition.y + m_HeightOffset; // 높이 오프셋 적용

        // 부드럽게 카메라 위치 이동
        // lerpSpeed가 1.0 이상이면 즉시 이동, 그 외에는 Lerp 사용
        if (lerpSpeed >= 1f)
        {
            transform.position = targetPosition;
        }
        else
        {
            // Lerp의 세 번째 인자는 0~1 사이여야 하므로 Time.deltaTime * m_FollowSpeed를 그대로 사용
            transform.position = Vector3.Lerp(transform.position, targetPosition, m_FollowSpeed * Time.deltaTime);
        }

        // 플레이어를 바라보기
        Vector3 lookDirection = playerPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f) // 거의 0이 아닌 경우에만
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            if (lerpSpeed >= 1f)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_LookSpeed * Time.deltaTime);
            }
        }
    }
}
