using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject m_InGameUI;
    public GameObject m_MenuUI;

    [SerializeField, Range(0f, 1f)]
    float m_HealthRate;


    [SerializeField, Header("Canvas Elements")]
    Text m_txScore;
    [SerializeField]
    Text m_txCurrentRanking;

    [SerializeField]
    Image m_ImageLeft;
    [SerializeField]
    Image m_ImageRight;

    [SerializeField]
    int LoadingRotateSpeed = 20;

    [SerializeField]
    GameObject LoadingObject;

    [SerializeField]
    GameObject LoadingScene;

    [SerializeField, Header("Nickname Input (Multi Only)")]
    InputField m_NicknameInput;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            BtnStartSolo();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            BtnStartMulti();
        }

        // StartScene에서는 InGame UI를 업데이트하지 않음
        // Road 씬의 InGameUIController가 자체적으로 업데이트함
        if (m_InGameUI == null || m_InGameUI.active == false)
            return;

        // StartScene에 있는 UI 업데이트는 여기서 처리
        // Road 씬의 UI 업데이트는 InGameUIController에서 처리
    }

    //===

    private void Start()
    {
        if (instance == null)
            instance = GetComponent<UIManager>();
        DontDestroyOnLoad(instance);
        HideLoading();
    }

    /// <summary>
    /// 게임 시작 버튼
    /// </summary>
    public void BtnStart()
    {
        StartGameWithMode(GameState.EMatchMode.Multi);

        //테스트 코드, (서버에서 데이터를 받았다고 치고) 임의로 5초 뒤에 Join 동작을, 다시 5초 뒤에 Start 동작을 실행함
        //StartCoroutine(testCo());
    }

    public void BtnStartSolo()
    {
        StartGameWithMode(GameState.EMatchMode.Solo);
    }

    public void BtnStartMulti()
    {
        StartGameWithMode(GameState.EMatchMode.Multi);
    }

    void StartGameWithMode(GameState.EMatchMode mode)
    {
        GameState.Instance.MatchMode = mode;
        GameState.Instance.PlayerNickname = GetNicknameForMode(mode);
        if (mode == GameState.EMatchMode.Multi)
        {
            ShowLoading(); // 멀티만 매칭 대기 로딩 표시
        }
        else
        {
            HideLoading();
        }

        ServerInterface.Instance.ConnectToTcpServer(GameState.Instance.Ip, GameState.Instance.PortNum);
    }

    string GetNicknameForMode(GameState.EMatchMode mode)
    {
        string nickname = m_NicknameInput != null ? m_NicknameInput.text : string.Empty;
        nickname = nickname != null ? nickname.Trim() : string.Empty;
        return string.IsNullOrEmpty(nickname) ? "Player" : nickname;
    }
    
    /// <summary>
    /// 테스트 모드 토글 버튼 (옵션)
    /// </summary>
    public void ToggleTestMode()
    {
        if (GameState.IsTestMode)
        {
            GameState.DisableTestMode();
        }
        else
        {
            GameState.EnableTestMode();
        }
    }

    //IEnumerator testCo()
    //{
        //yield return new WaitForSeconds(3f);
        //ServerInterface.Instance.ClientAction((int)EPacketID.JoinGame, ServerInterface.Instance.SocketConnection, new byte[16]);

        //yield return new WaitForSeconds(3f);
        //ServerInterface.Instance.ClientAction((int)EPacketID.StartGame, ServerInterface.Instance.SocketConnection, new byte[16]);
    //}


    //로딩 UI를 보여줌
    public void ShowLoading()
    {
        if (LoadingObject == null) { Debug.Log("[ShowLoading] Please Assign LoadingObject"); StopCoroutine("CoLoading"); return; }
        if (LoadingScene == null) { Debug.Log("[ShowLoading] Please Assign LoadingObject"); return; }
        LoadingScene.SetActive(true);
        StartCoroutine(CoLoading(LoadingObject, LoadingRotateSpeed));
    }
    public void HideLoading()
    {
        if (LoadingScene == null) { Debug.Log("[HideLoading] Please Assign LoadingObject"); return; }
        StopCoroutine("CoLoading");
        LoadingObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        LoadingScene.SetActive(false);
    }

    /// <summary>
    /// 로딩 이미지를 빙글 빙글 돌리는 기능
    /// </summary>
    IEnumerator CoLoading(GameObject LoadingImage, int speed)
    {
        Debug.Log("매칭을 대기 중... ");
        LoadingScene.SetActive(true);

        while (true)
        {
            if (LoadingImage == null) yield break;

            LoadingImage.transform.Rotate(Vector3.forward * speed);
            yield return new WaitForSeconds(0.05f);
        }

    }


    /// <summary>
    /// 게임 종료 후 로비(메뉴) 화면으로 돌아가는 함수
    /// </summary>
    public void BackToLobby()
    {
        Debug.Log("Lobby로 복귀합니다.");

        // 1. 인게임 UI 숨기기
        if (m_InGameUI != null)
            m_InGameUI.SetActive(false);

        // 2. 메뉴(로비) UI 보이기
        if (m_MenuUI != null)
            m_MenuUI.SetActive(true);

        // 3. 로딩 화면이 켜져 있다면 끄기
        HideLoading();
    }

}
