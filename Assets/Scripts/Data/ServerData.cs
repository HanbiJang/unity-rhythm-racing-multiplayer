using System;
using System.Collections.Generic;
//===
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine;

/// <summary>
/// Server와 Client 간의 주고받는 데이터
/// </summary>
public class PacketData : TSData
{
    //Deserialize
    public PacketData ConvertToGameData(byte[] ByteArr)
    {
        int current_pos = 0;

        Type t = this.GetType();
        FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);//private Field 허용

        int curFieldIdx = 0;
        foreach (FieldInfo f in fields)
        {
            int size = 0;
            object obj = null;

            if (f.FieldType.Equals(typeof(List<KeyValuePair<int, int>>)))
            {
                //List 변환
                List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();
                int tmpPos = current_pos;
                while (tmpPos < ByteArr.Length)
                {
                    byte[] keyBuffer = new byte[sizeof(UInt64)]; //1개
                    byte[] valueBuffer = new byte[sizeof(int)]; //1개
                    Array.Copy(ByteArr, tmpPos, keyBuffer, 0, sizeof(UInt64));
                    Array.Copy(ByteArr, tmpPos + sizeof(UInt64), valueBuffer, 0, sizeof(int)); //각 buffer에 값 저장

                    int key = BitConverter.ToInt32(keyBuffer, 0);
                    int value = BitConverter.ToInt32(valueBuffer, 0); //값 추출

                    list.Add(new KeyValuePair<int, int>(key, value));

                    tmpPos += sizeof(UInt64) + sizeof(int); //한 pair처리
                }

                fields[curFieldIdx].SetValue(this, list);
            }
            if (f.FieldType.Equals(typeof(List<KeyValuePair<ulong, ulong>>)))
            {
                //List 변환
                List<KeyValuePair<ulong, ulong>> list = new List<KeyValuePair<ulong, ulong>>();
                int tmpPos = current_pos;
                while (tmpPos < ByteArr.Length)
                {
                    byte[] keyBuffer = new byte[sizeof(ulong)]; //1개
                    byte[] valueBuffer = new byte[sizeof(ulong)]; //1개
                    Array.Copy(ByteArr, tmpPos, keyBuffer, 0, sizeof(ulong));
                    Array.Copy(ByteArr, tmpPos + sizeof(ulong), valueBuffer, 0, sizeof(ulong)); //각 buffer에 값 저장

                    ulong key = BitConverter.ToUInt32(keyBuffer, 0);
                    ulong value = BitConverter.ToUInt32(valueBuffer, 0); //값 추출

                    list.Add(new KeyValuePair<ulong, ulong>(key, value));

                    tmpPos += sizeof(ulong) + sizeof(ulong); //한 pair처리
                }

                fields[curFieldIdx].SetValue(this, list);
            }
            else if (f.FieldType.Equals(typeof(List<ulong>)))
            {
                //List 변환
                List<ulong> list = new List<ulong>();
                int tmpPos = current_pos;
                while (tmpPos < ByteArr.Length)
                {
                    byte[] Buffer = new byte[sizeof(UInt64)]; //1개
                    Array.Copy(ByteArr, tmpPos, Buffer, 0, sizeof(UInt64));

                    ulong key = BitConverter.ToUInt64(Buffer, 0);

                    list.Add(key);

                    tmpPos += sizeof(UInt64); //한 pair처리
                }

                fields[curFieldIdx].SetValue(this, list);
            }
            else
            {
                if (current_pos < ByteArr.Length)
                {
                    if (f.FieldType.Equals(typeof(int)))
                    {
                        obj = BitConverter.ToInt32(ByteArr, current_pos);
                        fields[curFieldIdx].SetValue(this, Convert.ToInt32(obj));
                        size = sizeof(int);
                    }
                    else if (f.FieldType.Equals(typeof(UInt64)))
                    {
                        obj = BitConverter.ToInt64(ByteArr, current_pos);
                        fields[curFieldIdx].SetValue(this, Convert.ToUInt64(obj));
                        size = sizeof(UInt64);
                    }

                    ++curFieldIdx;
                }
            }

            current_pos += size;
        }

        return this;
    }

    //Serialize
    public byte[] ConvertToByte()
    {
        int current_pos = 0;
        byte[] result = new byte[GetClassSize()];
        Type t = this.GetType();
        FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);//private Field 허용

        foreach (FieldInfo f in fields)
        {
            int sizeOfField = 0;
            if (f.FieldType.Equals(typeof(List<KeyValuePair<int, int>>)))
            {
                //List<KeyValuePair<UInt64, int>> 변환
                List<byte[]> KeyValueBufferResultList = new List<byte[]>();

                List<KeyValuePair<int, int>> list = (List<KeyValuePair<int, int>>)f.GetValue(this);
                foreach (KeyValuePair<int, int> ele in list) //실제 필드 내에 있는 List
                {
                    byte[] keyBuffer = BitConverter.GetBytes(ele.Key);
                    byte[] valueBuffer = BitConverter.GetBytes(ele.Value);
                    byte[] keyValueBytes = new byte[keyBuffer.Length + valueBuffer.Length];

                    Array.Copy(keyBuffer, 0, keyValueBytes, 0, keyBuffer.Length);
                    Array.Copy(valueBuffer, 0, keyValueBytes, keyBuffer.Length, valueBuffer.Length); //byte 배열 합치기
                    KeyValueBufferResultList.Add(keyValueBytes);
                }

                //List<byte[]>를 직렬화하기
                byte[] serializedData = KeyValueBufferResultList.SelectMany(x => x).ToArray();
                Array.Copy(serializedData, 0, result, current_pos, serializedData.Length);
                current_pos += serializedData.Length; //pair 정보들을 byte로 변환
            }
            else if (f.FieldType.Equals(typeof(List<KeyValuePair<ulong, ulong>>)))
            {
                //List<KeyValuePair<UInt64, int>> 변환
                List<byte[]> KeyValueBufferResultList = new List<byte[]>();

                List<KeyValuePair<ulong, ulong>> list = (List<KeyValuePair<ulong, ulong>>)f.GetValue(this);
                foreach (KeyValuePair<ulong, ulong> ele in list) //실제 필드 내에 있는 List
                {
                    byte[] keyBuffer = BitConverter.GetBytes(ele.Key);
                    byte[] valueBuffer = BitConverter.GetBytes(ele.Value);
                    byte[] keyValueBytes = new byte[keyBuffer.Length + valueBuffer.Length];

                    Array.Copy(keyBuffer, 0, keyValueBytes, 0, keyBuffer.Length);
                    Array.Copy(valueBuffer, 0, keyValueBytes, keyBuffer.Length, valueBuffer.Length); //byte 배열 합치기
                    KeyValueBufferResultList.Add(keyValueBytes);
                }

                //List<byte[]>를 직렬화하기
                byte[] serializedData = KeyValueBufferResultList.SelectMany(x => x).ToArray();
                Array.Copy(serializedData, 0, result, current_pos, serializedData.Length);
                current_pos += serializedData.Length; //pair 정보들을 byte로 변환
            }
            else if (f.FieldType.Equals(typeof(List<ulong>)))
            {
                List<byte[]> resultList = new List<byte[]>();
                f.GetValue(this);

                List<ulong> list = (List<ulong>)f.GetValue(this);

                foreach (ulong ele in list) //실제 필드 내에 있는 List
                {
                    byte[] Buffer = BitConverter.GetBytes(ele);

                    resultList.Add(Buffer);
                }

                //List<byte[]>를 직렬화하기
                byte[] serializedData = resultList.SelectMany(x => x).ToArray();
                Array.Copy(serializedData, 0, result, current_pos, serializedData.Length);
                current_pos += serializedData.Length; //pair 정보들을 byte로 변환
            }
            else
            {
                Type curType = f.FieldType;
                byte[] buffer = null;

                sizeOfField = Marshal.SizeOf(f.FieldType); //주의***
                if (f.FieldType.Equals(typeof(int))) { buffer = BitConverter.GetBytes((int)f.GetValue(this)); }
                else if (f.FieldType.Equals(typeof(UInt64))) { buffer = BitConverter.GetBytes((UInt64)f.GetValue(this)); }
                else if (f.FieldType.Equals(typeof(float))) { buffer = BitConverter.GetBytes((float)f.GetValue(this)); }
                
                if (buffer != null)
                {
                    Array.Copy(buffer, 0, result, current_pos, sizeOfField);
                }
                else
                {
                    Debug.LogError($"[PacketData.ConvertToByte] Unsupported field type: {f.FieldType.Name} in field: {f.Name}");
                }
            }
            current_pos += sizeOfField;
        }

        return result;
    }

    //Get Class Size
    int GetClassSize()
    {
        int size = 0;
        Type t = this.GetType();
        FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);//private Field 허용

        foreach (FieldInfo f in fields)
        {
            if (f.FieldType.Equals(typeof(int))) size += sizeof(int); //주의***
            else if (f.FieldType.Equals(typeof(UInt64))) size += sizeof(UInt64);
            else if (f.FieldType.Equals(typeof(float))) size += sizeof(float);
            else if (f.FieldType.Equals(typeof(List<KeyValuePair<int, int>>)))
            {
                List<KeyValuePair<int, int>> list = (List<KeyValuePair<int, int>>)f.GetValue(this); //현재 클래스의 List형 필드를 가져옴
                size += list.Count * (sizeof(int) + sizeof(int));
            }
        }

        return size;
    }
}

[System.Serializable]
//[Client -> Server] 모든 패킷 앞에 붙어 어떤 패킷이 들어왔는지 확인
public class PacketHeader : PacketData
{
    int _packetType;
    int _bodyLength;

    public int packetType { get { return _packetType; } set { _packetType = value; } }
    public int bodyLength { get { return _bodyLength; } set { _bodyLength = value; } }

    //생성자
    public PacketHeader()
    {
        _packetType = 0;
        _bodyLength = 0;
    }
    public PacketHeader(int packetType_, int _bodyLength_)
    {
        _packetType = packetType_;
        _bodyLength = _bodyLength_;
    }
}

[System.Serializable]
//[Client -> Server] JoinGame을 받은 이후 클라이언트가 게임에 대한 준비가 끝난 후 전송
public class ReadyGameData : PacketData
{
    UInt64 _userID;
    UInt64 _roomID;

    public UInt64 userID { get { return _userID; } set { _userID = value; } }
    public UInt64 roomID { get { return _roomID; } set { _roomID = value; } }

    //생성자
    public ReadyGameData()
    {
        _userID = 0;
        _roomID = 0;
    }
    public ReadyGameData(UInt64 userID_, UInt64 roomID_)
    {
        _userID = userID_;
        _roomID = roomID_;
    }
}

[System.Serializable]
//[Client -> Server] 클라이언트가 노드에 닿았을 때 판정 전송
public class JudgementData : PacketData
{
    UInt64 _userID;
    UInt64 _roomID;
    int _NodeType;
    int _JudgmentType;  // 판정 타입 (0: Perfect, 1: Good, 2: Bad, 3: Miss)
    float _TimeDifference;  // 예상 타이밍과의 차이 (초)
    int _Score;  // 판정 점수

    public UInt64 userID { get { return _userID; } set { _userID = value; } }
    public UInt64 roomID { get { return _roomID; } set { _roomID = value; } }
    public int NodeType { get { return _NodeType; } set { _NodeType = value; } }
    public int JudgmentType { get { return _JudgmentType; } set { _JudgmentType = value; } }
    public float TimeDifference { get { return _TimeDifference; } set { _TimeDifference = value; } }
    public int Score { get { return _Score; } set { _Score = value; } }

    //생성자
    public JudgementData()
    {
        _userID = 0;
        _roomID = 0;
        _NodeType = 0;
        _JudgmentType = 0;
        _TimeDifference = 0f;
        _Score = 0;
    }
    public JudgementData(UInt64 userID_, UInt64 roomID_, int NodeType_)
    {
        _userID = userID_;
        _roomID = roomID_;
        _NodeType = NodeType_;
        _JudgmentType = 0;
        _TimeDifference = 0f;
        _Score = 0;
    }
}

[System.Serializable]
//[Client -> Server] 해당 방은 종료하고, 새로운 게임방을 구성하여 대기
public class RetryData : PacketData
{
    UInt64 _userID;
    UInt64 _roomID;

    public UInt64 UserID { get { return _userID; } set { _userID = value; } }
    public UInt64 RoomID { get { return _roomID; } set { _roomID = value; } }

    //생성자
    public RetryData()
    {
        _userID = 0;
        _roomID = 0;
    }
    public RetryData(UInt64 userID_, UInt64 roomID_)
    {
        _userID = userID_;
        _roomID = roomID_;
    }
}

[System.Serializable]
//[Client -> Server] 해당 방을 탈출하면서 종료
public class EndGameData : PacketData
{
    UInt64 _userID;
    UInt64 _roomID;

    public UInt64 UserID { get { return _userID; } set { _userID = value; } }
    public UInt64 RoomID { get { return _roomID; } set { _roomID = value; } }

    //생성자
    public EndGameData()
    {
        _userID = 0;
        _roomID = 0;
    }
    public EndGameData(UInt64 userID_, UInt64 roomID_)
    {
        _userID = userID_;
        _roomID = roomID_;
    }

}

[System.Serializable]
//[Server -> Client] client accept 이후 유저 아이디를 생성하고 게임방에 배정, 해당 정보 전달
public class JoinGameData : PacketData
{
    UInt64 _userID;
    UInt64 _roomID;

    public UInt64 UserID { get { return _userID; } set { _userID = value; } }
    public UInt64 RoomID { get { return _roomID; } set { _roomID = value; } }

    //생성자
    public JoinGameData()
    {
        _userID = 0;
        _roomID = 0;
    }
    public JoinGameData(UInt64 userID_, UInt64 roomID_)
    {
        _userID = userID_;
        _roomID = roomID_;
    }
}

[System.Serializable]
//[Server -> Client] 게임방에 있는 모든 유저가 ReadyGame 패킷을 전송하면 해당 패킷을 클라이언트에 전송
public class StartGameData : PacketData
{
    int userCount;
    List<UInt64> userIds;
    int totalNoteCount;  // 전체 음악 노트 개수

    public int UserCount { get { return userCount; } set { userCount = value; } }
    public List<UInt64> UserIds { get { return userIds; } set { userIds = value; } }
    public int TotalNoteCount { get { return totalNoteCount; } set { totalNoteCount = value; } }

    //생성자
    public StartGameData()
    {
        userCount = 0;
        userIds = new List<ulong>();
        totalNoteCount = 0;
    }
    public StartGameData(int userCount_, List<UInt64> roomID_, int totalNoteCount_ = 0)
    {
        userCount = userCount_;
        userIds = roomID_;
        totalNoteCount = totalNoteCount_;
    }

    // StartGameData는 커스텀 파싱 필요 (List 뒤에 int가 있으므로)
    public new PacketData ConvertToGameData(byte[] ByteArr)
    {
        int offset = 0;

        // userCount 읽기
        if (offset + sizeof(int) > ByteArr.Length) return this;
        userCount = BitConverter.ToInt32(ByteArr, offset);
        offset += sizeof(int);

        // userIds 읽기 (userCount만큼만)
        userIds = new List<UInt64>();
        for (int i = 0; i < userCount; i++)
        {
            if (offset + sizeof(UInt64) > ByteArr.Length) break;
            UInt64 userId = BitConverter.ToUInt64(ByteArr, offset);
            userIds.Add(userId);
            offset += sizeof(UInt64);
        }

        // totalNoteCount 읽기
        if (offset + sizeof(int) <= ByteArr.Length)
        {
            totalNoteCount = BitConverter.ToInt32(ByteArr, offset);
        }

        return this;
    }
}

[System.Serializable]
//[Server -> Client] 게임방에서 장애물 생성
public class SpawnNodeData : PacketData
{
    int _NodeType;
    int _NodePos;

    public int NodeType { get { return _NodeType; } set { _NodeType = value; } }
    public int NodePos { get { return _NodePos; } set { _NodePos = value; } }

    //생성자
    public SpawnNodeData()
    {
        _NodeType = 0;
        _NodePos = 0;
    }
    public SpawnNodeData(int NodeType_, int NodePos_)
    {
        _NodeType = NodeType_;
        _NodePos = NodePos_;
    }
}

[System.Serializable]
//[Server -> Client] 각 방에 있는 클라이언트들의 점수 동기화
public class ScoreBroadcastData : PacketData
{
    int _userCount;
    List<KeyValuePair<ulong, ulong>> _scoreLIst;

    public int UserCount { get { return _userCount; } set { _userCount = value; } }
    public List<KeyValuePair<ulong, ulong>> ScoreLIst { get { return _scoreLIst; } set { _scoreLIst = value; } }

    //생성자
    public ScoreBroadcastData()
    {
        _userCount = 0;
        _scoreLIst = new List<KeyValuePair<ulong, ulong>>();
    }
    public ScoreBroadcastData(int userCount_, List<KeyValuePair<ulong, ulong>> scoreLIst_)
    {
        _userCount = userCount_;
        _scoreLIst = scoreLIst_;
    }
}
