using PathCreation.Examples;
using PathCreation;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerFollower : PathFollower
{
    [SerializeField]
    PlayerInputController ctr;

    /// <summary>
    /// 음악 노트 기준 진행도를 계산합니다 (0.0 ~ 1.0)
    /// </summary>
    public float GetNodeBasedProgress()
    {
        if (GameModeManager.instance == null)
            return 0f;

        int totalNotes = GameModeManager.instance.totalNoteCount;
        int currentNote = GameModeManager.instance.currentNoteIndex;

        // 전체 노트 개수가 0이면 시간 기준으로 폴백
        if (totalNotes <= 0)
        {
            // 시간 기준으로 대략적인 진행도 계산
            if (GameModeManager.instance.g_SoundLength > 0)
            {
                return Mathf.Clamp01(GameModeManager.instance.m_CurrentTime / GameModeManager.instance.g_SoundLength);
            }
            return 0f;
        }

        // 진행도 = 현재 진행한 노트 / 전체 노트 개수
        float progress = (float)currentNote / totalNotes;
        return Mathf.Clamp01(progress);
    }

    [SerializeField, Range(0.1f, 1f)]
    float m_MoveSpeed = 0.3f;
    
    Vector3 m_TargetLocalPosition;

    // Update is called once per frame
    protected override void Update()
    {
        // 게임 오버 시 이동 중지
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver)
        {
            return;
        }

        bool syncedByTime = false;

        // 음악 시간 기준으로 경로 진행도 동기화
        if (pathCreator != null && GameModeManager.instance != null && GameModeManager.instance.g_SoundLength > 0f)
        {
            float t = Mathf.Clamp01(GameModeManager.instance.m_CurrentTime / GameModeManager.instance.g_SoundLength);
            float pathLength = pathCreator.path.length;

            // 음악 진행도에 맞게 distanceTravelled 강제 설정
            distanceTravelled = pathLength * t;
            transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
            transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);

            syncedByTime = true;
        }

        // 시간 기반 동기화가 불가능할 때만 기본 PathFollower 이동 사용
        if (!syncedByTime)
        {
            base.Update();
        }
        
        // GameModeManager에서 레인 간격 가져오기
        float laneOffset = GameModeManager.instance != null ? GameModeManager.instance.laneOffset : 3f;
        
        // 목표 위치 계산 (좌/우 레인 이동)
        // LeftPosition과 RightPosition의 방향을 유지하면서 크기만 laneOffset에 맞게 조절
        float leftX = ctr.LeftPosition.magnitude > 0 ? (ctr.LeftPosition.normalized.x * laneOffset) : 0f;
        float rightX = ctr.RightPosition.magnitude > 0 ? (ctr.RightPosition.normalized.x * laneOffset) : 0f;
        
        m_TargetLocalPosition = new Vector3(
            (ctr.bLeft ? leftX : 0f) + (ctr.bRight ? rightX : 0f),
            0f,
            0f
        );
        
        // Lerp를 사용하여 부드럽게 이동
        ctr.transform.localPosition = Vector3.Lerp(ctr.transform.localPosition, m_TargetLocalPosition, m_MoveSpeed);
    }
}
