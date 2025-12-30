using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PickupScript : MonoBehaviour
{
    public bool bPicked;
    public int nodeType = 0; // 노드 타입 (0: ObjectA, 1: ObjectB, 2: ObjectC, 3: AFail, 4: BFail, 5: CFail)
    
    [Header("판정 정보")]
    public float expectedTime = 0f;  // 노트의 예상 타이밍 (게임 시작 후 경과 시간, 초)
    public bool hasExpectedTime = false;  // 예상 타이밍이 설정되었는지 여부
    
    public abstract void OnPicked(Vector3 CrusherPosition);
    public abstract void OnMissed();
    
    /// <summary>
    /// 예상 타이밍을 설정합니다.
    /// </summary>
    public void SetExpectedTime(float time)
    {
        expectedTime = time;
        hasExpectedTime = true;
    }
}