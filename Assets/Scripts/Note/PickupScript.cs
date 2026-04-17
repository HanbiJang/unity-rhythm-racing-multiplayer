using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupScript : MonoBehaviour
{
    public bool bPicked;
    public int nodeType = 0; // 노드 타입 (0: ObjectA, 1: ObjectB, 2: ObjectC, 3: AFail, 4: BFail, 5: CFail)

    [Header("판정 정보")]
    public float expectedTime = 0f;
    public bool hasExpectedTime = false;

    [Header("물리 이펙트")]
    [Tooltip("true면 충돌 시 Rigidbody에 물리 힘을 가하고 날아감 (FloatablePickupScript 동작)")]
    public bool isFloatable = false;

    void Awake()
    {
        // 모든 콜라이더를 트리거로 설정 (GetComponent는 첫 번째만 반환하므로 GetComponents 사용)
        var cols = GetComponents<Collider>();
        foreach (var col in cols)
        {
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log($"[PickupScript] {gameObject.name} — {col.GetType().Name} 트리거로 변경");
            }
        }
    }

    public void SetExpectedTime(float time)
    {
        expectedTime = time;
        hasExpectedTime = true;
    }

    public void OnPicked(Vector3 crusherPosition)
    {
        if (bPicked) return;
        bPicked = true;

        if (isFloatable)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForceAtPosition(
                    new Vector3(Random.Range(-1000f, 1000f), 3000f, Random.Range(-1000f, 1000f)),
                    crusherPosition
                );
            }
        }

        if (NodeSfxManager.Instance != null)
            NodeSfxManager.Instance.PlayNodeHit(nodeType);

        JudgmentSystem.JudgmentResult judgmentResult = null;
        if (hasExpectedTime && JudgmentSystem.Instance != null && GameModeManager.instance != null)
        {
            float currentTime = GameModeManager.instance.m_CurrentTime;
            judgmentResult = JudgmentSystem.Instance.Judge(expectedTime, currentTime);

            Debug.Log($"[PickupScript] Judgment: {JudgmentSystem.GetJudgmentTypeString(judgmentResult.type)}, " +
                      $"Expected: {expectedTime:F3}s, Current: {currentTime:F3}s, " +
                      $"Diff: {judgmentResult.timeDifference:F3}s, Score: {judgmentResult.score}");

            if (JudgmentDisplayUI.Instance != null)
                JudgmentDisplayUI.Instance.ShowJudgment(judgmentResult.type);

            if (ComboTracker.Instance != null)
                ComboTracker.Instance.UpdateCombo(judgmentResult.type);
        }
        else
        {
            Debug.LogWarning("[PickupScript] Cannot perform judgment - missing expected time or JudgmentSystem");
        }

        if (HitEffectManager.Instance != null)
        {
            Vector3 hitPoint = (transform.position + crusherPosition) * 0.5f;
            HitEffectManager.Instance.PlayAllEffects(hitPoint);
        }

        if (nodeType >= 3)
            ScreenFlashManager.Instance.PlayFailFeedback();

        SendJudgementToServer(judgmentResult);

        if (!isFloatable)
            Destroy(gameObject);
    }

    public void OnMissed()
    {
        Debug.Log("Missed : " + name);

        JudgmentSystem.JudgmentResult missResult = null;
        if (JudgmentSystem.Instance != null && GameModeManager.instance != null)
        {
            float currentTime = GameModeManager.instance.m_CurrentTime;
            float timeDifference = hasExpectedTime ? Mathf.Abs(currentTime - expectedTime) : 999f;
            missResult = new JudgmentSystem.JudgmentResult(JudgmentSystem.JudgmentType.Miss, timeDifference, 0);

            Debug.Log($"[PickupScript] Miss - Expected: {expectedTime:F3}s, Current: {currentTime:F3}s, Diff: {timeDifference:F3}s");

            if (JudgmentDisplayUI.Instance != null)
                JudgmentDisplayUI.Instance.ShowJudgment(JudgmentSystem.JudgmentType.Miss);

            if (ComboTracker.Instance != null)
                ComboTracker.Instance.UpdateCombo(JudgmentSystem.JudgmentType.Miss);
        }
        else
        {
            if (ComboTracker.Instance != null)
                ComboTracker.Instance.ResetCombo();
        }

        SendJudgementToServer(missResult);
        Destroy(gameObject);
    }

    private void SendJudgementToServer(JudgmentSystem.JudgmentResult result)
    {
        if (ServerInterface.Instance == null) return;
        if (!GameState.IsTestMode &&
            (ServerInterface.Instance.SocketConnection == null || !ServerInterface.Instance.SocketConnection.Connected))
            return;

        JudgementData data = new JudgementData(GameState.Instance.UserId, GameState.Instance.RoomId, nodeType);
        if (result != null)
        {
            data.JudgmentType = (int)result.type;
            data.TimeDifference = result.timeDifference;
            data.Score = result.score;
        }

        ServerInterface.Instance.SendDataToServer(
            ServerInterface.Instance.SocketConnection, data, (int)EPacketID.Judgement);
        Debug.Log($"Sent Judgement: UserID={GameState.Instance.UserId}, NodeType={nodeType}, " +
                  $"JudgmentType={result?.type}, Score={result?.score}");
    }
}
