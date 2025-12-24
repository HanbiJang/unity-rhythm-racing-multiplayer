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

        // 서버로 Judgement 패킷 전송
        if (ServerInterface.Instance != null && ServerInterface.Instance.SocketConnection != null && ServerInterface.Instance.SocketConnection.Connected)
        {
            JudgementData judgementData = new JudgementData(GameState.Instance.UserId, GameState.Instance.RoomId, nodeType);
            ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, judgementData, (int)EPacketID.Judgement);
            Debug.Log($"Sent Judgement: UserID={GameState.Instance.UserId}, RoomID={GameState.Instance.RoomId}, NodeType={nodeType}");
        }

        Destroy(gameObject, 0.7f);
    }

    public override void OnMissed()
    {
        Debug.Log("Missed : "+name);

        Destroy(gameObject, 0.9f);
    }
}
