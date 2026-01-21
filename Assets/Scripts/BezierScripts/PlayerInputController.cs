using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public Vector3 LeftPosition;
    public Vector3 RightPosition;
    public Vector3 MidPosition;

    public bool bLeft = false;
    public bool bRight = false;

    void Update()
    {
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

        // 이 스크립트는 플레이어 위치 자체는 다른 컴포넌트가 처리할 수 있으므로
        // 필요하다면 아래 라인을 해제해서 직접 위치를 갱신할 수도 있습니다.
        //transform.position = MidPosition + (bLeft ? LeftPosition : Vector3.zero) + (bRight ? RightPosition : Vector3.zero);
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickupItem") )
        {
            PickupScript ps = other.gameObject.GetComponent<PickupScript>();
            ps.OnPicked(transform.position);
        }
    }
}
