using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResultFlow
{
    public static void GoToResult()
    {
        Debug.Log("[ResultFlow] GoToResult() called");
        
        // 인게임 UI 숨기기
        if (UIManager.instance != null && UIManager.instance.m_InGameUI != null)
        {
            UIManager.instance.m_InGameUI.SetActive(false);
            Debug.Log("[ResultFlow] InGameUI hidden");
        }
        
        // 오버레이 패널을 쓰는 경우:
        // FindWithTag는 비활성화된 오브젝트를 찾지 못하므로, 모든 오브젝트를 검색
        GameObject panel = null;
        
        // 먼저 활성화된 오브젝트에서 찾기
        panel = GameObject.FindWithTag("ResultPanel");
        
        // 찾지 못했으면 모든 오브젝트에서 찾기 (비활성화된 것 포함)
        if (panel == null)
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.CompareTag("ResultPanel") && obj.scene.isLoaded)
                {
                    panel = obj;
                    break;
                }
            }
        }
        
        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log($"[ResultFlow] ResultPanel activated: {panel.name}");
            
            // ResultUIController가 있으면 Refresh 호출
            var resultController = panel.GetComponent<Assets.Scripts.ResultUIController>();
            if (resultController != null)
            {
                resultController.Refresh();
                Debug.Log("[ResultFlow] ResultUIController refreshed");
            }
            else
            {
                Debug.LogWarning("[ResultFlow] ResultUIController component not found on ResultPanel");
            }
            return;
        }
        
        Debug.LogError("[ResultFlow] ResultPanel not found! Make sure there's a GameObject with 'ResultPanel' tag in the scene.");
    }

    public static void BackToLobby()
    {
        // 결과 패널 닫고 로비로 이동
        GameModeManager.instance?.ResetForLobby();

        // 로비 씬으로
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);

        // 네트워크 정책(5) 참고: 유지/재연결 처리
        // NetworkPolicy.BackToLobbyConnectionPolicy();
    }
}
