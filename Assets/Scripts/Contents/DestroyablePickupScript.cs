using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyablePickupScript : PickupScript
{

    public override void OnPicked(Vector3 CrusherPosition)
    {
        if (bPicked) { return; }
        Debug.Log("AAAAAAAAAAAAA");

        bPicked = true;
        
        // 서버로 Judgement 패킷 전송
        if (ServerInterface.Instance != null && ServerInterface.Instance.SocketConnection != null && ServerInterface.Instance.SocketConnection.Connected)
        {
            JudgementData judgementData = new JudgementData(GameState.Instance.UserId, GameState.Instance.RoomId, nodeType);
            ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, judgementData, (int)EPacketID.Judgement);
            Debug.Log($"Sent Judgement: UserID={GameState.Instance.UserId}, RoomID={GameState.Instance.RoomId}, NodeType={nodeType}");
        }
    }

    public override void OnMissed()
    {
        Debug.Log("Missed : " + name);

        Destroy(gameObject, 0.9f);
    }

}
