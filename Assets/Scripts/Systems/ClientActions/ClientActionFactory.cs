using UnityEngine;
class ClientActionFactory : MonoBehaviour
{
    public static IClientAction CreateAction(EPacketID pachetID)
    {
        switch (pachetID)
        {
            case EPacketID.RetryGame:
                return new RetryGame();
            case EPacketID.EndGame:
                return new EndGame();
            case EPacketID.JoinGame:
                return new JoinGame();
            case EPacketID.StartGame:
                return new StartGame();
            case EPacketID.SpawnNode:
                return new SpawnNode();
            case EPacketID.ScoreBroadcast:
                return new ScoreBroadcast();
            default:
                Debug.Log("ClientActionFactory Null - Bad Packet / pachetID :" + pachetID);
                return null;
        }
    }
}
