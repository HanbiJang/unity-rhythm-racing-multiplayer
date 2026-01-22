using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatablePickupScript : PickupScript
{
    public override void OnPicked(Vector3 CrusherPosition) 
    {
        if(bPicked) { return; }
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForceAtPosition(new Vector3(Random.Range(1000f,-1000f), 3000f, Random.Range(1000f, -1000f)),CrusherPosition);
        bPicked = true;
        if (NodeSfxManager.Instance != null)
        {
            NodeSfxManager.Instance.PlayNodeHit(nodeType);
        }
        //GameModeManager.instance.m_PlayerScore += 3000;
        //

        // 판정 시스템을 사용하여 타이밍 판정
        JudgmentSystem.JudgmentResult judgmentResult = null;
        if (hasExpectedTime && JudgmentSystem.Instance != null && GameModeManager.instance != null)
        {
            float currentTime = GameModeManager.instance.m_CurrentTime;
            
            // 원래 스폰 시점에 계산된 expectedTime을 사용 (재계산하지 않음)
            judgmentResult = JudgmentSystem.Instance.Judge(expectedTime, currentTime);
            
            Debug.Log($"[FloatablePickupScript] Judgment: {JudgmentSystem.GetJudgmentTypeString(judgmentResult.type)}, " +
                     $"Expected: {expectedTime:F3}s, Current: {currentTime:F3}s, Time Diff: {judgmentResult.timeDifference:F3}s, Score: {judgmentResult.score}");
            
            // 판정 결과를 UI에 표시
            if (JudgmentDisplayUI.Instance != null)
            {
                JudgmentDisplayUI.Instance.ShowJudgment(judgmentResult.type);
            }
            
            // 콤보 업데이트
            if (ComboTracker.Instance != null)
            {
                ComboTracker.Instance.UpdateCombo(judgmentResult.type);
            }
        }
        else
        {
            // 판정 시스템을 사용할 수 없는 경우 기본값 설정
            Debug.LogWarning("[FloatablePickupScript] Cannot perform judgment - missing expected time or JudgmentSystem");
        }

        // 타격 지점 기반으로 화면에 보이는 위치에 이펙트 재생
        if (HitEffectManager.Instance != null)
        {
            // 타격 지점: 플레이어와 노트의 충돌 지점 (CrusherPosition)
            // 노트의 현재 위치와 플레이어 위치의 중간점 계산
            Vector3 hitPoint = (transform.position + CrusherPosition) * 0.5f;
            HitEffectManager.Instance.PlayAllEffects(hitPoint);
        }
        
        // Fail 타입이면 화면 플래시 이펙트 재생
        if (nodeType >= 3)
        {
            ScreenFlashManager.Instance.PlayFailFeedback();
        }

        // 서버로 Judgement 패킷 전송
        if (ServerInterface.Instance != null && (GameState.IsTestMode || (ServerInterface.Instance.SocketConnection != null && ServerInterface.Instance.SocketConnection.Connected)))
        {
            JudgementData judgementData = new JudgementData(GameState.Instance.UserId, GameState.Instance.RoomId, nodeType);
            
            // 판정 결과 추가
            if (judgmentResult != null)
            {
                judgementData.JudgmentType = (int)judgmentResult.type;
                judgementData.TimeDifference = judgmentResult.timeDifference;
                judgementData.Score = judgmentResult.score;
            }
            
            ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, judgementData, (int)EPacketID.Judgement);
            Debug.Log($"Sent Judgement: UserID={GameState.Instance.UserId}, RoomID={GameState.Instance.RoomId}, " +
                     $"NodeType={nodeType}, JudgmentType={judgmentResult?.type}, Score={judgmentResult?.score}");
        }

        Destroy(gameObject);
    }

    public override void OnMissed()
    {
        Debug.Log("Missed : "+name);
        
        // Miss 판정 생성 및 처리
        if (JudgmentSystem.Instance != null && GameModeManager.instance != null)
        {
            // Miss 판정 결과 생성
            float currentTime = GameModeManager.instance.m_CurrentTime;
            float timeDifference = hasExpectedTime ? Mathf.Abs(currentTime - expectedTime) : 999f;
            
            JudgmentSystem.JudgmentResult missResult = new JudgmentSystem.JudgmentResult(
                JudgmentSystem.JudgmentType.Miss,
                timeDifference,
                0
            );
            
            Debug.Log($"[FloatablePickupScript] Miss Judgment: Expected: {expectedTime:F3}s, Current: {currentTime:F3}s, Time Diff: {timeDifference:F3}s");
            
            // Miss 판정을 UI에 표시
            if (JudgmentDisplayUI.Instance != null)
            {
                JudgmentDisplayUI.Instance.ShowJudgment(JudgmentSystem.JudgmentType.Miss);
            }
            
            // 콤보 초기화
            if (ComboTracker.Instance != null)
            {
                ComboTracker.Instance.UpdateCombo(JudgmentSystem.JudgmentType.Miss);
            }
            
            // 서버로 Miss 판정 전송
            if (ServerInterface.Instance != null && (GameState.IsTestMode || (ServerInterface.Instance.SocketConnection != null && ServerInterface.Instance.SocketConnection.Connected)))
            {
                JudgementData judgementData = new JudgementData(GameState.Instance.UserId, GameState.Instance.RoomId, nodeType);
                judgementData.JudgmentType = (int)JudgmentSystem.JudgmentType.Miss;
                judgementData.TimeDifference = timeDifference;
                judgementData.Score = 0;
                
                ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, judgementData, (int)EPacketID.Judgement);
                Debug.Log($"Sent Miss Judgement: UserID={GameState.Instance.UserId}, RoomID={GameState.Instance.RoomId}, NodeType={nodeType}");
            }
        }
        else
        {
            // 판정 시스템을 사용할 수 없는 경우에도 콤보는 초기화
            if (ComboTracker.Instance != null)
            {
                ComboTracker.Instance.ResetCombo();
            }
        }

        Destroy(gameObject);
    }
}
