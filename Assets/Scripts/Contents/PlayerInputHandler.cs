using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector3 LeftPosition;
    public Vector3 RightPosition;
    public Vector3 MidPosition;

    // 레인 간 이동 속도 (값이 클수록 더 빠르게 목표 위치로 붙음)
    [SerializeField, Range(0.1f, 1f)]
    float m_MoveSpeed = 0.7f;

    bool bLeft = false;
    bool bRight = false;
    
    Vector3 m_TargetPosition;

    void Update()
    {
        // GameModeManager에서 레인 간격 가져오기
        float laneOffset = GameModeManager.instance != null ? GameModeManager.instance.laneOffset : 3f;

        // a: 왼쪽 레인, s: 가운데 레인, d: 오른쪽 레인
        if (Input.GetKeyDown(KeyCode.A))
        {
            bLeft = true;
            bRight = false;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            bRight = true;
            bLeft = false;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            // 가운데 레인
            bLeft = false;
            bRight = false;
        }

        // 목표 위치 계산 (가운데 + 좌/우 오프셋)
        // LeftPosition과 RightPosition의 방향을 유지하면서 크기만 laneOffset에 맞게 조절
        Vector3 leftOffset = LeftPosition.normalized * laneOffset;
        Vector3 rightOffset = RightPosition.normalized * laneOffset;
        
        m_TargetPosition = MidPosition
                           + (bLeft ? leftOffset : Vector3.zero)
                           + (bRight ? rightOffset : Vector3.zero);

        // Lerp를 사용하여 부드럽고 빠르게 이동
        gameObject.transform.position =
            Vector3.Lerp(gameObject.transform.position, m_TargetPosition, m_MoveSpeed);
    }
}
