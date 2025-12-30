using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyablePickupScript : PickupScript
{

    public override void OnPicked(Vector3 CrusherPosition)
    {
        if (bPicked) { return; }

        bPicked = true;
        
        // 판정 시스템을 사용하여 타이밍 판정
        JudgmentSystem.JudgmentResult judgmentResult = null;
        if (hasExpectedTime && JudgmentSystem.Instance != null && GameModeManager.instance != null)
        {
            float currentTime = GameModeManager.instance.m_CurrentTime;
            judgmentResult = JudgmentSystem.Instance.Judge(expectedTime, currentTime);
            
            Debug.Log($"[DestroyablePickupScript] Judgment: {JudgmentSystem.GetJudgmentTypeString(judgmentResult.type)}, " +
                     $"Time Diff: {judgmentResult.timeDifference:F3}s, Score: {judgmentResult.score}");
        }
        else
        {
            // 판정 시스템을 사용할 수 없는 경우 기본값 설정
            Debug.LogWarning("[DestroyablePickupScript] Cannot perform judgment - missing expected time or JudgmentSystem");
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
    }

    public override void OnMissed()
    {
        Debug.Log("Missed : " + name);

        Destroy(gameObject, 0.9f);
    }

}
