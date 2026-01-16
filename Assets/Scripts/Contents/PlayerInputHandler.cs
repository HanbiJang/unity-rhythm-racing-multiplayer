using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector3 LeftPosition;
    public Vector3 RightPosition;
    public Vector3 MidPosition;
    
    [SerializeField, Range(0.1f, 1f)]
    float m_MoveSpeed = 0.3f;

    bool bLeft = false;
    bool bRight = false;
    
    Vector3 m_TargetPosition;

    void Update()
    {
        if(Input.GetAxisRaw("Horizontal") < 0)
        {
            bLeft = true;
            bRight= false;
        }
        if(Input.GetAxisRaw("Horizontal") > 0)
        {
            bRight = true;
            bLeft = false;
        }
        if (Mathf.Approximately(Input.GetAxisRaw("Horizontal"),0f))
        {
            bRight= false;
            bLeft = false;
        }
        
        // 목표 위치 계산
        m_TargetPosition = MidPosition + (bLeft?LeftPosition:Vector3.zero) + (bRight?RightPosition:Vector3.zero);
        
        // Lerp를 사용하여 부드럽게 이동
        gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, m_TargetPosition, m_MoveSpeed);
    }
}
