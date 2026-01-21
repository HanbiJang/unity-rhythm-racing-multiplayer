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
        if (data.Entries != null)
        {
            if (GameState.Instance.UserNicknames == null)
            {
                GameState.Instance.UserNicknames = new Dictionary<ulong, string>();
            }
            foreach (var entry in data.Entries)
            {
                GameState.Instance.UserNicknames[entry.UserId] = entry.Nickname;
            }
        }

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
    
    // 테스트 모드에서 점수 브로드캐스트 시뮬레이션
    public static void SimulateScoreBroadcast(ulong score)
    {
        if (GameModeManager.instance != null)
        {
            GameModeManager.instance.m_PlayerScore = score;
            
            // GameState에도 업데이트
            if (GameState.Instance.ScoreLIst == null)
            {
                GameState.Instance.ScoreLIst = new List<KeyValuePair<ulong, ulong>>();
            }
            if (GameState.Instance.UserNicknames == null)
            {
                GameState.Instance.UserNicknames = new Dictionary<ulong, string>();
            }
            
            // 기존 점수 업데이트 또는 추가
            bool found = false;
            for (int i = 0; i < GameState.Instance.ScoreLIst.Count; i++)
            {
                if (GameState.Instance.ScoreLIst[i].Key == GameState.Instance.UserId)
                {
                    GameState.Instance.ScoreLIst[i] = new KeyValuePair<ulong, ulong>(GameState.Instance.UserId, (ulong)score);
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                GameState.Instance.ScoreLIst.Add(new KeyValuePair<ulong, ulong>(GameState.Instance.UserId, (ulong)score));
            }

            GameState.Instance.UserNicknames[GameState.Instance.UserId] = GameState.Instance.PlayerNickname;
            
            GameState.Instance.UserCount = GameState.Instance.ScoreLIst.Count;
        }
    }
}
