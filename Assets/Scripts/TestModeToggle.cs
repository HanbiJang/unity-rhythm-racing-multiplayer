using UnityEngine;

/// <summary>
/// 테스트 모드를 쉽게 전환할 수 있는 헬퍼 스크립트
/// 씬의 아무 오브젝트에나 붙이고 Inspector에서 체크박스로 제어 가능
/// </summary>
public class TestModeToggle : MonoBehaviour
{
    [SerializeField, Header("테스트 모드 (서버 없이 클라이언트만 테스트)")]
    bool testMode = false;
    
    [SerializeField, Header("게임 시작 시 자동으로 테스트 모드 활성화")]
    bool autoEnableOnStart = false;
    
    private void Start()
    {
        if (autoEnableOnStart)
        {
            GameState.EnableTestMode();
            Debug.Log("[Test Mode] 자동 활성화됨");
        }
    }
    
    private void OnValidate()
    {
        // Inspector에서 값이 변경될 때마다 적용
        if (Application.isPlaying)
        {
            if (testMode)
            {
                GameState.EnableTestMode();
            }
            else
            {
                GameState.DisableTestMode();
            }
        }
    }
    
    private void Update()
    {
        // T 키로 토글 (옵션)
        if (Input.GetKeyDown(KeyCode.T))
        {
            testMode = !testMode;
            if (testMode)
            {
                GameState.EnableTestMode();
            }
            else
            {
                GameState.DisableTestMode();
            }
        }
    }
}

