using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndGame : MonoBehaviour, IClientAction
{
    /// <summary>
    /// 해당 방을 탈출하면서 종료한다
    /// </summary>
    /// <param name="byteData"></param>
    public void Do(byte[] byteData)
    {
        Debug.Log("[EndGame] EndGame packet received from server");

        //해당 방의 데이터
        EndGameData endGameData = new EndGameData();
        endGameData.ConvertToGameData(byteData);
        Debug.Log($"[EndGame] UserID: {endGameData.UserID}, RoomID: {endGameData.RoomID}");

        // 1) 인게임 정지 신호 (플레이어 이동 중지 포함)
        if (GameModeManager.instance != null)
        {
            GameModeManager.instance.SetGameOver();
            Debug.Log("[EndGame] GameOver state set");
        }
        else
        {
            Debug.LogError("[EndGame] GameModeManager.instance is null!");
        }

        // 2) 결과 데이터 갱신 (이미 주기적 ScoreBroadcast로도 받음)
        //    결과씬/패널에서 GameState.Instance.ScoreLIst를 사용

        // 2-1) 음악 즉시 중지
        SoundManager soundManager = FindObjectOfType<SoundManager>();
        if (soundManager != null)
        {
            soundManager.StopMusic();
        }
        NodeSfxManager nodeSfxManager = FindObjectOfType<NodeSfxManager>();
        if (nodeSfxManager != null)
        {
            nodeSfxManager.StopAllSfx();
        }

        // 3) 결과 화면으로 전환 (오버레이 패널)
        ResultFlow.GoToResult();
    }
}
