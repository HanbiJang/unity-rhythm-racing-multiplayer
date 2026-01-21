using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour, IClientAction
{
    //게임을 시작함
    public void Do(byte[] byteData)
    {
        Debug.Log("StartGame()");

        //게임 시작에 관한 데이터
        StartGameData data = new StartGameData();
        data.ConvertToGameData(byteData);
        Debug.Log("UserID " + data.UserCount + "UserIds.Count " + data.UserIds.Count);
        Debug.Log("Total Note Count: " + data.TotalNoteCount);
        Debug.Log("Start Time (UTC ms): " + data.StartTimeUtcMs);

        // GameModeManager에 전체 노트 개수 저장
        if (GameModeManager.instance != null)
        {
            GameModeManager.instance.totalNoteCount = data.TotalNoteCount;
            GameModeManager.instance.currentNoteIndex = 0;  // 초기화
            GameModeManager.instance.SetSyncedStartTime(data.StartTimeUtcMs);
            Debug.Log($"GameModeManager: Total Notes = {GameModeManager.instance.totalNoteCount}");
        }
        
        // 전역에 시작 시각 저장 (씬 로딩 이후 SoundManager가 읽을 수 있도록)
        GameState.Instance.SyncedStartTimeUtcMs = data.StartTimeUtcMs;
        GameState.Instance.HasSyncedStartTime = data.StartTimeUtcMs > 0;

        // 이미 SoundManager가 있으면 즉시 동기 재생 예약
        SoundManager soundManager = FindObjectOfType<SoundManager>();
        if (soundManager != null && data.StartTimeUtcMs > 0)
        {
            soundManager.StartSyncedPlayback(data.StartTimeUtcMs);
        }

        //게임을 시작       
        //if (ServerInterface.Instance.GameSceneLoadAsync.isDone) //로딩이 끝났다면
        if(ServerInterface.Instance.GameSceneLoadAsync.progress >= 0.9f)
        {
            Debug.Log("Scene Change");
            UIManager.instance.HideLoading(); //로딩 종료
            
            ServerInterface.Instance.GameSceneLoadAsync.allowSceneActivation = true; //자동 씬 전환 막기
        }

        else
        {
            //UIManager.instance.ShowLoading(); //로딩 재개
            Debug.Log("Start Error, Next Scene is not Loaded" + ServerInterface.Instance.GameSceneLoadAsync.progress);
        }
    }
}
