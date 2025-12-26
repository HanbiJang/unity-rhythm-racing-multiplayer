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

    [SerializeField]
    Camera m_Camera;

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

    // Update is called once per frame
    protected override void Update()
    {
        // 게임 오버 시 이동 중지
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver)
        {
            return;
        }
        
        //Vector3 ang = Quaternion.ToEulerAngles(transform.rotation);
        base.Update();
        ctr.transform.localPosition = new Vector3((ctr.bLeft ? ctr.LeftPosition.x : 0f) + (ctr.bRight ? ctr.RightPosition.x : 0f), 0f,0f);
        //transform.rotation = Quaternion.Euler(ang.x,ang.y,0);
    }
}
