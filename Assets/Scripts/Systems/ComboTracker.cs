using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 콤보 추적 시스템
/// </summary>
public class ComboTracker : MonoBehaviour
{
    private static ComboTracker instance;

    public static ComboTracker Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ComboTracker>();
                if (instance == null)
                {
                    GameObject tmp = new GameObject();
                    tmp.name = typeof(ComboTracker).Name;
                    instance = tmp.AddComponent<ComboTracker>();
                }
            }
            return instance;
        }
    }

    private int currentCombo = 0;
    private int maxCombo = 0;

    public int CurrentCombo => currentCombo;
    public int MaxCombo => maxCombo;

    public System.Action<int> OnComboChanged;
    public System.Action OnComboReset;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 콤보를 증가시킵니다.
    /// </summary>
    public void AddCombo()
    {
        currentCombo++;
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
        OnComboChanged?.Invoke(currentCombo);
    }

    /// <summary>
    /// 콤보를 초기화합니다.
    /// </summary>
    public void ResetCombo()
    {
        if (currentCombo > 0)
        {
            currentCombo = 0;
            OnComboReset?.Invoke();
            OnComboChanged?.Invoke(currentCombo);
        }
    }

    /// <summary>
    /// 판정 결과에 따라 콤보를 업데이트합니다.
    /// </summary>
    public void UpdateCombo(JudgmentSystem.JudgmentType judgmentType)
    {
        switch (judgmentType)
        {
            case JudgmentSystem.JudgmentType.Perfect:
            case JudgmentSystem.JudgmentType.Good:
                AddCombo();
                break;
            case JudgmentSystem.JudgmentType.Bad:
            case JudgmentSystem.JudgmentType.Miss:
                ResetCombo();
                break;
        }
    }

    /// <summary>
    /// 게임 시작 시 콤보를 초기화합니다.
    /// </summary>
    public void ResetForNewGame()
    {
        currentCombo = 0;
        maxCombo = 0;
        OnComboChanged?.Invoke(currentCombo);
    }
}
