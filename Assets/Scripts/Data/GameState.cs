using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    private static GameState instance; //Singleton

    public static GameState Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameState>();
                if (instance == null)
                {
                    GameObject tmp = new GameObject();
                    tmp.name = typeof(GameState).Name;
                    instance = tmp.AddComponent<GameState>();
                }
            }
            DontDestroyOnLoad(instance);
            return instance;
        }
    }

    //string ip = "10.88.164.52";
    string ip = "127.0.0.1";
    int portNum = 8888;

    // 테스트 모드 (서버 없이 클라이언트만 테스트)
    // true로 설정하면 서버 연결 없이 테스트 가능
    static bool isTestMode = false;

    ulong userId;
    ulong roomId;
    int userCount;
    List<KeyValuePair<ulong, ulong>> scoreLIst; //int,int는 :userID, Score순이다


    public ulong UserId { get { return userId; } set { userId = value; } }
    public ulong RoomId { get { return roomId; } set { roomId = value; } }

    public int UserCount { get { return userCount; } set { userCount = value; } }
    public List<KeyValuePair<ulong, ulong>> ScoreLIst { get { return scoreLIst; } set { scoreLIst = value; } }

    public string Ip { get { return ip; } set { ip = value; } }
    public int PortNum { get { return portNum; } set { portNum = value; } }
    
    public static bool IsTestMode { get { return isTestMode; } set { isTestMode = value; } }
    
    // 테스트 모드 활성화/비활성화 헬퍼 메서드
    public static void EnableTestMode() { isTestMode = true; Debug.Log("[Test Mode] 활성화됨"); }
    public static void DisableTestMode() { isTestMode = false; Debug.Log("[Test Mode] 비활성화됨"); }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
