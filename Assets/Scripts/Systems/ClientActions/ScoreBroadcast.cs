using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreBroadcast : MonoBehaviour, IClientAction
{
    //서버에서 점수 데이터를 전달 후, 클라이언트가 하는 일
    public void Do(byte[] byteData)
    {
        Debug.Log("ScoreBroadcast()");

        //data save
        ScoreBroadcastData data = new ScoreBroadcastData();
        data.ConvertToGameData(byteData);
        Debug.Log("UserCount " + data.UserCount + "ScoreLIst ... Count");

        GameState.Instance.UserCount = data.UserCount;
        GameState.Instance.ScoreLIst = data.ScoreLIst;

        // 서버에서 계산한 점수를 GameModeManager에 동기화
        if (GameModeManager.instance != null && data.ScoreLIst != null)
        {
            // 자신의 UserID에 해당하는 점수 찾기
            ulong myUserId = GameState.Instance.UserId;
            foreach (var scorePair in data.ScoreLIst)
            {
                if (scorePair.Key == myUserId)
                {
                    // 서버에서 계산한 점수로 업데이트
                    GameModeManager.instance.m_PlayerScore = scorePair.Value;
                    Debug.Log($"Score updated from server: {scorePair.Value}");
                    break;
                }
            }
        }
    }
}
