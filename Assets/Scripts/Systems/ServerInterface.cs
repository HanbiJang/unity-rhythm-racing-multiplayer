using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

//
using UnityEngine.SceneManagement;


/// <summary>
/// 패킷 송수신 관련 사이즈 정의 클래스
/// </summary>
class Defines
{
    public const int BUFFERMAXSIZE = 256;
    public const int HEADERSIZE = 8;
    public const int PACKETBODYSIZEPOS = 0;
    public const int PACKETIDPOS = 4;
}


/// <summary>
/// 패킷의 종류
/// </summary>
public enum EPacketID
{
    None = 0,
    ReadyGame, //JoinGame을 받은 이후 클라이언트가 게임에 대한 준비가 끝난 후 전송
    Judgement, //클라이언트가 노드에 닿았을 때 판정 전송
    RetryGame, //클라이언트가 게임 재시작 결정
    EndGame, //해당 방을 탈출하면서 종료
    JoinGame, //client accept 이후 유저 아이디를 생성하고 게임방에 배정, 해당 정보 전달
    StartGame, //게임방에 있는 모든 유저가 ReadyGame 패킷을 전송하면 해당 패킷을 클라이언트에 전송
    SpawnNode, //게임방에서 장애물 생성
    ScoreBroadcast, //각 방에 있는 클라이언트들의 점수 동기화

    //add another packet ID here

    MAX_DO_NOT_USE_THIS
}

/// <summary>
/// (1) 서버에서 주는 Byte배열을 게임 내에서 쓸 수 있는 형태로 변환한다
/// (2) 패킷을 서버에서 온전히 모두 받을 수 있도록 서버에서 주는 Byte 리스트를 이어붙임
/// </summary>
public class ServerInterface : MonoBehaviour
{

    List<KeyValuePair<int, byte[]>> actionbytes = new List<KeyValuePair<int, byte[]>>();

    private static ServerInterface instance;
    TcpClient socketConnection = new TcpClient();
    int totalLength = 0;
    int totalBytes = 0;

    bool bListening = false;
    List<byte[]> resultbytes = new List<byte[]>();

    //===
    int totalLength2 = 0;
    int totalBytes2 = 0;
    List<byte[]> resultbytes2 = new List<byte[]>();

    AsyncOperation gameSceneLoadAsync;

    //===
    NetworkStream stream;


    public static ServerInterface Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ServerInterface>();
                if (instance == null)
                {
                    GameObject tmp = new GameObject();
                    tmp.name = typeof(ServerInterface).Name;
                    instance = tmp.AddComponent<ServerInterface>();
                }
            }
            DontDestroyOnLoad(instance);
            return instance;
        }
    }

    public TcpClient SocketConnection
    {
        get
        {
            return socketConnection;
        }
        private set { }
    }
    public bool BListening
    {
        get
        {
            if (GameState.IsTestMode)
                return true; // 테스트 모드에서는 항상 리스닝 중인 것처럼
            return bListening;
        }
    }
    
    // 테스트 모드에서 가짜 연결 상태 반환
    public bool IsConnected
    {
        get
        {
            if (GameState.IsTestMode)
                return true; // 테스트 모드에서는 항상 연결된 것처럼
            return socketConnection != null && socketConnection.Connected;
        }
    }

    public AsyncOperation GameSceneLoadAsync
    {
        get
        {
            return gameSceneLoadAsync;
        }
    }

    /// <summary>
    /// 서버와 데이터 교환 중단
    /// </summary>
    public void StopClientListening()
    {
        bListening = false;
    }
    /// <summary>
    /// 서버와 데이터 교환 재시작
    /// </summary>
    public void DoClientListening()
    {
        bListening = true;
    }

    private void Update()
    {
        // 테스트 모드일 때는 서버 통신 스킵
        if (GameState.IsTestMode)
        {
            return;
        }
        
        if (!bListening && socketConnection.Connected)
        {
            stream = socketConnection.GetStream();
            bListening = true;
        }
        if (bListening && socketConnection.Connected)
        {
            ReceiveDataFromServer(socketConnection); //서버로부터 계속 데이터를 받는 상태
        }
    }


    public void ConnectToTcpServer(string Ip, int port)
    {
        // 테스트 모드일 때는 가짜 연결만 시뮬레이션
        if (GameState.IsTestMode)
        {
            Debug.Log("[Test Mode] Simulating server connection");
            bListening = true;
            
            // 테스트 모드에서 자동으로 JoinGame 시뮬레이션
            StartCoroutine(SimulateJoinGame());
            return;
        }
        
        if (!socketConnection.Connected)
        {
            Debug.Log("try");
            try
            {
                socketConnection.ConnectAsync(Ip, port); //비동기로 연결 시도
                stream = socketConnection.GetStream(); //***
            }
            catch (Exception e)
            {
                Debug.Log("On client connect exception " + e);
            }
        }
    }
    
    // 테스트 모드에서 JoinGame 패킷 시뮬레이션
    IEnumerator SimulateJoinGame()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 가짜 JoinGame 데이터 생성 (1인 플레이)
        JoinGameData joinData = new JoinGameData();
        joinData.UserID = 999; // 테스트용 UserID
        joinData.RoomID = 1; // 테스트용 RoomID
        
        GameState.Instance.UserId = joinData.UserID;
        GameState.Instance.RoomId = joinData.RoomID;
        
        // JoinGame 패킷 시뮬레이션
        byte[] joinBytes = joinData.ConvertToByte();
        ClientAction((int)EPacketID.JoinGame, socketConnection, joinBytes);
        
        Debug.Log("[Test Mode] Simulated JoinGame (1인 플레이)");
    }

    public void DisconnectToTcpServer()
    {
        if (socketConnection.Connected)
        {
            try
            {
                if (socketConnection.Connected)
                    socketConnection.Close();
            }
            catch (Exception e)
            {
                Debug.Log("On client disconnect exception " + e);
            }
        }
    }

    /// <summary>
    /// 서버에게 데이터를 보낸다
    /// </summary>
    public void SendDataToServer(TcpClient socketConnection, PacketData packetData, int packetType)
    {
        // 테스트 모드일 때는 로그만 남기고 실제 전송 안 함
        if (GameState.IsTestMode)
        {
            EPacketID packetID = (EPacketID)packetType;
            Debug.Log($"[Test Mode] Simulated Send: {packetID} (실제 서버 전송 안 함)");
            
            // 테스트 모드에서 특정 패킷에 대한 응답 시뮬레이션
            if (packetID == EPacketID.ReadyGame)
            {
                StartCoroutine(SimulateStartGame());
            }
            else if (packetID == EPacketID.Judgement)
            {
                // 테스트 모드에서 Judgement 패킷을 보내면 점수 시뮬레이션
                JudgementData judgementData = packetData as JudgementData;
                if (judgementData != null)
                {
                    // GameModeManager가 없으면 안전하게 스킵
                    if (GameModeManager.instance == null)
                    {
                        Debug.LogWarning("[Test Mode] GameModeManager.instance 가 없어 점수 시뮬레이션을 건너뜁니다.");
                        return;
                    }

                    // 간단한 점수 계산 시뮬레이션
                    ulong baseScore = 0;
                    switch (judgementData.NodeType)
                    {
                        case 0: baseScore = 3000; break; // ObjectA
                        case 1: baseScore = 2000; break; // ObjectB
                        case 2: baseScore = 4500; break; // ObjectC
                    }
                    
                    if (baseScore > 0)
                    {
                        // 점수 누적 (테스트용)
                        ulong currentScore = (ulong)GameModeManager.instance.m_PlayerScore;
                        ScoreBroadcast.SimulateScoreBroadcast(currentScore + baseScore);
                    }
                }
            }
            return;
        }

        byte[] buffer = CreateSendPacket(packetData, packetType);

        if (!socketConnection.Connected)
        {
            Debug.Log("[Send Error] Please Connect to server - try connect");
            ConnectToTcpServer(GameState.Instance.Ip, GameState.Instance.PortNum);

            if (!socketConnection.Connected) return;
        }
        // Get a stream object for writing.             

        if (stream != null && stream.CanWrite && socketConnection.Connected)
        {
            // Write byte array to socketConnection stream.
            string str = ".";
            for (int i = 0; i < buffer.Length; i++)
            {
                str += buffer[i].ToString();
            }
            Debug.Log("Client SendDataToServer " + str);
            stream.Write(buffer, 0, buffer.Length);
        }

    }
    
    // 테스트 모드에서 StartGame 패킷 시뮬레이션
    IEnumerator SimulateStartGame()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 가짜 StartGame 데이터 생성 (1인 플레이)
        StartGameData startData = new StartGameData();
        startData.UserCount = 1;
        startData.UserIds = new List<ulong> { GameState.Instance.UserId };
        startData.TotalNoteCount = 100;  // 테스트 모드용 기본값 (실제로는 서버에서 받아야 함)
        
        byte[] startBytes = startData.ConvertToByte();
        ClientAction((int)EPacketID.StartGame, socketConnection, startBytes);
        
        Debug.Log($"[Test Mode] Simulated StartGame (1인 플레이, Total Notes: {startData.TotalNoteCount})");
    }


    public void ReceiveDataFromServer(TcpClient socketConnection)
    {

        if (!socketConnection.Connected)
        {
            Debug.Log("[Receive Error] Please Connect to server - try connect");
            ConnectToTcpServer(GameState.Instance.Ip, GameState.Instance.PortNum);

            if (!socketConnection.Connected) return;
        }
        try
        {
            if (stream.DataAvailable)
            {
                ByteToAction2(socketConnection);
            }

        }
        catch (SocketException socketExcpetion)
        {
            Debug.LogError("Socket exception: " + socketExcpetion);
        }

    }


    /// <summary>
    /// 서버로부터 패킷을 이어서 받고 클라이언트가 할 동작을 구분짓는다
    /// </summary>
    public void ByteToAction(TcpClient socketConnection)
    {
        List<KeyValuePair<int, byte[]>> packetList = new List<KeyValuePair<int, byte[]>>();
        int packetType = 0; //None
        int packetSize = 0;

        //await stream.ReadAsync(buffer, 0, buffer.Length) //비동기
        //stream = socketConnection.GetStream();

        //바이트 모두 받기
        while (true)
        {
            if (!stream.DataAvailable) break; //스트림이 empty면 종료

            //ClearBuffer();

            byte[] packet = new byte[Defines.BUFFERMAXSIZE]; // packet received from server
            int streamRead = 0;
            bool HeaderRead = false;

            //1. 헤더 읽기
            if (!HeaderRead)
            {
                streamRead = stream.Read(packet, 0, Defines.HEADERSIZE); //헤더만큼 읽는다
                if (streamRead >= Defines.HEADERSIZE)
                {   //데이터를 헤더 사이즈 이상 읽은 경우
                    //totalBytes += streamRead;
                    packetType = BitConverter.ToInt32(packet, 0);
                    packetSize = BitConverter.ToInt32(packet, sizeof(int)); // read packet size header
                    totalLength += packetSize;

                    if (packetType == 0 || packetSize <= 0) //이상한 데이터일 경우
                    {
                        Debug.Log("Header Read Error");
                        ClearBuffer();
                        return; //다음을 대기
                    }
                    else HeaderRead = true;
                }
                else
                {
                    Debug.Log("Header Read Error");
                    ClearBuffer();
                    return;
                }
            }

            // 헤더 읽기 성공
            byte[] bff = new byte[packetSize];
            streamRead = stream.Read(bff, 0, packetSize); //패킷 길이만큼 스트림에서 가져온다
            byte[] bodypacket = new byte[streamRead];
            Array.Copy(bff, 0, bodypacket, 0, bodypacket.Length);
            totalBytes += streamRead;
            resultbytes.Add(bodypacket); //데이터 추가

        }

        //byte 이어 붙이기
        byte[] bodyOnly = new byte[totalLength];
        int remainLength = totalBytes;
        int offset_ = 0;
        foreach (byte[] bodyPacket in resultbytes)
        {
            if (offset_ >= totalBytes) break;
            Array.Copy(bodyPacket, 0, bodyOnly, offset_, bodyPacket.Length);
            offset_ += bodyPacket.Length;
            remainLength -= bodyPacket.Length;
            if (remainLength <= 0) break;

        }
        if (bodyOnly.Length > 0)
        {  //패킷의 종류에 따라서 행동 결정하기
            ClientAction(packetType, socketConnection, bodyOnly);
        }
        else
        {
            Debug.Log("Data Body Read Fail");
        }

        ClearBuffer();
        return;

    }

    public void ByteToAction2(TcpClient socketConnection)
    {
        List<KeyValuePair<int, byte[]>> packetList = new List<KeyValuePair<int, byte[]>>();
        while (stream.DataAvailable)
        {
            byte[] header = new byte[Defines.BUFFERMAXSIZE];
            int readSize = 0;
            int packetType = 0;
            int packetSize = 0;

            // read header
            readSize = stream.Read(header, 0, Defines.HEADERSIZE);
            if (readSize == Defines.HEADERSIZE)
            {
                packetType = BitConverter.ToInt32(header, 0);
                packetSize = BitConverter.ToInt32(header, sizeof(int));
            }
            else
            {
                Debug.Log("[Network] Header Read Error. ReadSize: " + readSize);
                ClearBuffer();
                break;
            }

            // read body
            byte[] body = new byte[packetSize];
            readSize = stream.Read(body, 0, packetSize);
            if (readSize != packetSize)
            {
                Debug.Log("[Network] Body Read Error. ReadSize: " + readSize);
                ClearBuffer();
                break;
            }

            packetList.Add(new KeyValuePair<int, byte[]>(packetType, body));
        }

        foreach (KeyValuePair<int, byte[]> packet in packetList)
        {
            ClientAction(packet.Key, socketConnection, packet.Value);
        }
    }

    public byte[] CreateSendPacket(PacketData packetData, int packetType)
    {
        //헤더 + 데이터 조합으로 만들어서 반환      
        byte[] body = packetData.ConvertToByte();
        PacketHeader packetHeader = new PacketHeader(packetType, body.Length);
        byte[] header = packetHeader.ConvertToByte();

        byte[] packet = new byte[body.Length + header.Length];
        Array.Copy(header, 0, packet, 0, Defines.HEADERSIZE);
        Array.Copy(body, 0, packet, Defines.HEADERSIZE, body.Length);

        return packet;
    }

    /// <summary>
    /// 패킷 주고 받기 관련 정보를 초기화
    /// </summary>
    void InitPacketData()
    {
        this.totalBytes = 0;
        this.totalLength = 0;
    }

    /// <summary>
    /// 패킷 주고 받기 관련 정보를 초기화
    /// </summary>
    void InitPacketData2()
    {
        this.totalBytes2 = 0;
        this.totalLength2 = 0;
    }

    /// <summary>
    /// 버퍼에 담긴 내용물을 비운다 (데이터를 버린다?)
    /// </summary>
    void ClearBuffer()
    {
        //Array.Clear(this.ResultByteBuffer, 0, this.ResultByteBuffer.Length);
        this.resultbytes.Clear();
        InitPacketData();
    }

    /// <summary>
    /// 버퍼에 담긴 내용물을 비운다 (데이터를 버린다?)
    /// </summary>
    void ClearBuffer2()
    {
        //Array.Clear(this.ResultByteBuffer, 0, this.ResultByteBuffer.Length);
        this.resultbytes2.Clear();
        InitPacketData2();
    }

    /// <summary>
    /// 패킷의 종류에 따라서 클라이언트에서 어떤 동작을 할지 분기를 탄다
    /// </summary>
    public void ClientAction(int PacketId_, TcpClient socketConnection, byte[] byteData)
    {
        EPacketID PacketId = (EPacketID)PacketId_;
        //Debug.LogError(PacketId_);
        ActionSelector actionSelector = new ActionSelector(PacketId);
        if (actionSelector.CliendAction != null) actionSelector.Do(byteData);
    }

    //===

    /// <summary>
    /// 게임이 매칭되면 씬을 비동기 로드하고 서버에게 레디 패킷을 보내는 함수
    /// </summary>
    public void AsyncLoadAndReady(int sceneNum)
    {
        StartCoroutine(CoAsyncLoadAndReady(sceneNum));
    }

    IEnumerator CoAsyncLoadAndReady(int sceneNum)
    {
        gameSceneLoadAsync = SceneManager.LoadSceneAsync(sceneNum);
        gameSceneLoadAsync.allowSceneActivation = false; //자동 씬 전환 막기

        while (!gameSceneLoadAsync.isDone) //완료가 되었는지 체크 
        {

            //준비가 되었다면, 서버에게 Ready Game Data 전송
            if (gameSceneLoadAsync.progress >= 0.9f)
            {
                Debug.Log("Scene Load isDone");
                ReadyGameData readyGameData = new ReadyGameData(GameState.Instance.UserId, GameState.Instance.RoomId); //GameState에 있는 데이터를 전송    
                ServerInterface.Instance.SendDataToServer(ServerInterface.Instance.SocketConnection, readyGameData, (int)EPacketID.ReadyGame);
                yield break;
            }
        }

        yield return null;
    }
}

