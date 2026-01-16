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
        //SoundManager.instance.PlaySound(me);
        //GameModeManager.instance.m_PlayerScore += 3000;
        //

        // 판정 시스템을 사용하여 타이밍 판정
        JudgmentSystem.JudgmentResult judgmentResult = null;
        if (hasExpectedTime && JudgmentSystem.Instance != null && GameModeManager.instance != null)
        {
            float currentTime = GameModeManager.instance.m_CurrentTime;
            judgmentResult = JudgmentSystem.Instance.Judge(expectedTime, currentTime);
            
            Debug.Log($"[FloatablePickupScript] Judgment: {JudgmentSystem.GetJudgmentTypeString(judgmentResult.type)}, " +
                     $"Time Diff: {judgmentResult.timeDifference:F3}s, Score: {judgmentResult.score}");
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

        Destroy(gameObject, 0.7f);
    }

    public override void OnMissed()
    {
        Debug.Log("Missed : "+name);

        Destroy(gameObject, 0.9f);
    }
}
