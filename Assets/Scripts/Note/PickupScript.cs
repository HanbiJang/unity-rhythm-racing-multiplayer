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
        if (JudgmentSystem.Instance != null)
        {
            judgmentResult = JudgmentSystem.Instance.JudgeByDistance(transform.position.z, crusherPosition.z);

            Debug.Log($"[PickupScript] Judgment: {JudgmentSystem.GetJudgmentTypeString(judgmentResult.type)}, " +
                      $"NoteZ: {transform.position.z:F3}, PlayerZ: {crusherPosition.z:F3}, " +
                      $"ZDiff: {judgmentResult.timeDifference:F3}, Score: {judgmentResult.score}");

            if (JudgmentDisplayUI.Instance != null)
                JudgmentDisplayUI.Instance.ShowJudgment(judgmentResult.type);

            if (ComboTracker.Instance != null)
                ComboTracker.Instance.UpdateCombo(judgmentResult.type);
        }
        else
        {
            Debug.LogWarning("[PickupScript] Cannot perform judgment - missing expected time or JudgmentSystem");
        }

        // 판정 기반 카메라 셰이크
        if (judgmentResult != null)
        {
            float shakeIntensity = judgmentResult.type switch
            {
                JudgmentSystem.JudgmentType.Perfect => 0.2f,
                JudgmentSystem.JudgmentType.Good    => 0.1f,
                JudgmentSystem.JudgmentType.Bad     => 0.05f,
                _                                   => 0f,
            };
            if (shakeIntensity > 0f)
                PlayerCameraController.Shake(shakeIntensity);
        }

        // 판정 기반 파티클 색상
        if (HitEffectManager.Instance != null)
        {
            Vector3 hitPoint = (transform.position + crusherPosition) * 0.5f;
            if (judgmentResult != null)
                HitEffectManager.Instance.PlayJudgmentEffect(hitPoint, judgmentResult.type);
            else
                HitEffectManager.Instance.PlayAllEffects(hitPoint);
        }

        // 별똥별 이펙트: 플레이어 위치 → ScoreText
        if (ScoreFlyEffect.Instance != null && nodeType < 3)
            ScoreFlyEffect.Instance.Play(crusherPosition);

        if (nodeType >= 3)
        {
            ScreenFlashManager.Instance.PlayFailFeedback();

            // Fail 노드: 점수 1000 차감 (0 미만으로는 내려가지 않음)
            if (GameModeManager.instance != null)
                GameModeManager.instance.m_PlayerScore = Mathf.Max(0f, GameModeManager.instance.m_PlayerScore - 1000f);

            // 콤보 초기화
            if (ComboTracker.Instance != null)
                ComboTracker.Instance.ResetCombo();

            GameModeManager.instance?.ResetSpeedOnMiss();

            // 서버에 Miss 판정으로 전송 (서버 측 콤보/점수도 초기화)
            judgmentResult = new JudgmentSystem.JudgmentResult(JudgmentSystem.JudgmentType.Miss, 0f, -1000);
        }

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

        GameModeManager.instance?.ResetSpeedOnMiss();

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
