using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetryGame : MonoBehaviour, IClientAction
{
    public void Do(byte[] byteData)
    {
        Debug.Log("RetryGame()");

        RetryData data = new RetryData();
        data.ConvertToGameData(byteData);
        Debug.Log("userID " + data.UserID + " RoomID " + data.RoomID);

        // 게임 상태 초기화
        if (GameModeManager.instance != null)
        {
            GameModeManager.instance.ResetForLobby();
        }

        // Ready 상태로 다시 전환 (게임 재시작 준비)
        ClientState.Set(GameClientState.Lobby);
        
        // 서버에 ReadyGame 패킷 전송하여 재시작 준비 완료 알림
        if (ServerInterface.Instance != null && (GameState.IsTestMode || (ServerInterface.Instance.SocketConnection != null && ServerInterface.Instance.SocketConnection.Connected)))
        {
            ReadyGameData readyGameData = new ReadyGameData(GameState.Instance.UserId, GameState.Instance.RoomId);
            ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, readyGameData, (int)EPacketID.ReadyGame);
            Debug.Log($"Sent ReadyGame for retry: UserID={GameState.Instance.UserId}, RoomID={GameState.Instance.RoomId}");
        }
    }
}
