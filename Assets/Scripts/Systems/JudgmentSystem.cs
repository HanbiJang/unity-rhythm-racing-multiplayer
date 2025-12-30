using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리듬게임 판정 시스템
/// Perfect, Good, Bad, Miss 판정을 처리합니다.
/// </summary>
public class JudgmentSystem : MonoBehaviour
{
    private static JudgmentSystem instance;

    public static JudgmentSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JudgmentSystem>();
                if (instance == null)
                {
                    GameObject tmp = new GameObject();
                    tmp.name = typeof(JudgmentSystem).Name;
                    instance = tmp.AddComponent<JudgmentSystem>();
                }
            }
            return instance;
        }
    }

    [Header("판정 윈도우 설정 (초 단위)")]
    [SerializeField]
    private float perfectWindow = 0.1f;  // Perfect 판정 범위 (±0.1초)

    [SerializeField]
    private float goodWindow = 0.2f;     // Good 판정 범위 (±0.2초)

    [SerializeField]
    private float badWindow = 0.3f;      // Bad 판정 범위 (±0.3초)

    [Header("판정 점수")]
    [SerializeField]
    private int perfectScore = 100;

    [SerializeField]
    private int goodScore = 50;

    [SerializeField]
    private int badScore = 10;

    [SerializeField]
    private int missScore = 0;

    /// <summary>
    /// 판정 결과 타입
    /// </summary>
    public enum JudgmentType
    {
        Perfect = 0,
        Good = 1,
        Bad = 2,
        Miss = 3
    }

    /// <summary>
    /// 판정 결과 데이터
    /// </summary>
    public class JudgmentResult
    {
        public JudgmentType type;
        public float timeDifference;  // 예상 타이밍과의 차이 (초)
        public int score;

        public JudgmentResult(JudgmentType type, float timeDifference, int score)
        {
            this.type = type;
            this.timeDifference = timeDifference;
            this.score = score;
        }
    }

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
    /// 타이밍 판정을 수행합니다.
    /// </summary>
    /// <param name="expectedTime">노트의 예상 타이밍 (게임 시작 후 경과 시간, 초)</param>
    /// <param name="currentTime">현재 게임 시간 (게임 시작 후 경과 시간, 초)</param>
    /// <returns>판정 결과</returns>
    public JudgmentResult Judge(float expectedTime, float currentTime)
    {
        float timeDifference = Mathf.Abs(currentTime - expectedTime);

        JudgmentType judgmentType;
        int score;

        if (timeDifference <= perfectWindow)
        {
            judgmentType = JudgmentType.Perfect;
            score = perfectScore;
        }
        else if (timeDifference <= goodWindow)
        {
            judgmentType = JudgmentType.Good;
            score = goodScore;
        }
        else if (timeDifference <= badWindow)
        {
            judgmentType = JudgmentType.Bad;
            score = badScore;
        }
        else
        {
            // 판정 윈도우를 벗어났지만 노트에 닿은 경우 (늦은 판정)
            // Miss로 처리하거나 Bad로 처리할 수 있습니다.
            judgmentType = JudgmentType.Bad;
            score = badScore;
        }

        return new JudgmentResult(judgmentType, timeDifference, score);
    }

    /// <summary>
    /// 판정 타입을 문자열로 반환합니다.
    /// </summary>
    public static string GetJudgmentTypeString(JudgmentType type)
    {
        switch (type)
        {
            case JudgmentType.Perfect:
                return "Perfect";
            case JudgmentType.Good:
                return "Good";
            case JudgmentType.Bad:
                return "Bad";
            case JudgmentType.Miss:
                return "Miss";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// 판정 윈도우 설정을 가져옵니다.
    /// </summary>
    public float GetPerfectWindow() => perfectWindow;
    public float GetGoodWindow() => goodWindow;
    public float GetBadWindow() => badWindow;

    /// <summary>
    /// 판정 윈도우 설정을 변경합니다.
    /// </summary>
    public void SetJudgmentWindows(float perfect, float good, float bad)
    {
        perfectWindow = perfect;
        goodWindow = good;
        badWindow = bad;
    }
}


