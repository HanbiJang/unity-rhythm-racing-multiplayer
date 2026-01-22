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
    
    /// <summary>
    /// 노트가 플레이어에게 도달할 때 실제 위치를 기반으로 expectedTime을 재계산합니다.
    /// </summary>
    protected float RecalculateExpectedTime()
    {
        if (GameModeManager.instance == null)
            return -1f;
            
        // PathFollower 컴포넌트 찾기
        PathCreation.Examples.PathFollower nodeFollower = GetComponent<PathCreation.Examples.PathFollower>();
        PlayerFollower playerFollower = FindObjectOfType<PlayerFollower>();
        
        if (nodeFollower == null || playerFollower == null || nodeFollower.pathCreator == null)
            return -1f;
        
        // 노트와 플레이어의 실제 경로상 거리 계산
        float nodeDistance = nodeFollower.pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        float playerDistance = nodeFollower.pathCreator.path.GetClosestDistanceAlongPath(playerFollower.transform.position);
        float actualGap = Mathf.Abs(nodeDistance - playerDistance);
        
        // 노트의 이동 속도
        float nodeSpeed = nodeFollower.speed;
        if (nodeSpeed <= 0f)
            return -1f;
        
        // 실제 도달 시간 계산
        float timeToReach = actualGap / nodeSpeed;
        float currentTime = GameModeManager.instance.m_CurrentTime;
        
        // 재계산된 expectedTime = 현재 시간 (노트가 이미 플레이어에게 도달했으므로)
        // 판정을 위해 약간의 오프셋을 추가할 수도 있지만, 일단 현재 시간을 기준으로 함
        return currentTime;
    }
}