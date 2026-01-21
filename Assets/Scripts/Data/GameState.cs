using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public enum EMatchMode
    {
        Solo = 0,
        Multi = 1,
    }
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
    long syncedStartTimeUtcMs;
    bool hasSyncedStartTime;
    EMatchMode matchMode = EMatchMode.Multi;
    string playerNickname = "Player";
    Dictionary<ulong, string> userNicknames = new Dictionary<ulong, string>();


    public ulong UserId { get { return userId; } set { userId = value; } }
    public ulong RoomId { get { return roomId; } set { roomId = value; } }

    public int UserCount { get { return userCount; } set { userCount = value; } }
    public List<KeyValuePair<ulong, ulong>> ScoreLIst { get { return scoreLIst; } set { scoreLIst = value; } }
    public long SyncedStartTimeUtcMs { get { return syncedStartTimeUtcMs; } set { syncedStartTimeUtcMs = value; } }
    public bool HasSyncedStartTime { get { return hasSyncedStartTime; } set { hasSyncedStartTime = value; } }
    public EMatchMode MatchMode { get { return matchMode; } set { matchMode = value; } }
    public string PlayerNickname { get { return playerNickname; } set { playerNickname = value; } }
    public Dictionary<ulong, string> UserNicknames { get { return userNicknames; } set { userNicknames = value; } }

    public const int NicknameMaxBytes = 16;

    public static void EncodeNickname(string nickname, out ulong part1, out ulong part2)
    {
        byte[] nameBytes = GetNicknameBytes(nickname);
        part1 = BitConverter.ToUInt64(nameBytes, 0);
        part2 = BitConverter.ToUInt64(nameBytes, 8);
    }

    public static string DecodeNickname(ulong part1, ulong part2)
    {
        byte[] bytes = new byte[NicknameMaxBytes];
        Array.Copy(BitConverter.GetBytes(part1), 0, bytes, 0, 8);
        Array.Copy(BitConverter.GetBytes(part2), 0, bytes, 8, 8);

        int length = NicknameMaxBytes;
        while (length > 0 && bytes[length - 1] == 0)
        {
            length--;
        }
        if (length <= 0)
            return string.Empty;

        return Encoding.UTF8.GetString(bytes, 0, length);
    }

    static byte[] GetNicknameBytes(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            return new byte[NicknameMaxBytes];
        }

        byte[] raw = Encoding.UTF8.GetBytes(nickname);
        if (raw.Length <= NicknameMaxBytes)
        {
            byte[] padded = new byte[NicknameMaxBytes];
            Array.Copy(raw, padded, raw.Length);
            return padded;
        }

        int len = NicknameMaxBytes;
        while (len > 0)
        {
            string candidate = Encoding.UTF8.GetString(raw, 0, len);
            byte[] candidateBytes = Encoding.UTF8.GetBytes(candidate);
            if (candidateBytes.Length <= NicknameMaxBytes)
            {
                byte[] padded = new byte[NicknameMaxBytes];
                Array.Copy(candidateBytes, padded, candidateBytes.Length);
                return padded;
            }
            len--;
        }

        return new byte[NicknameMaxBytes];
    }

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
