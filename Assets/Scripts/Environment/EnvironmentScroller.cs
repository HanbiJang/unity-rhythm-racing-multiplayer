using UnityEngine;

public class EnvironmentScroller : MonoBehaviour
{
    float Speed => GameModeManager.instance != null
        ? GameModeManager.instance.m_RoadMoveSpeed
        : 10f;

    void Update()
    {
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver)
            return;

        transform.Translate(Vector3.back * Speed * Time.deltaTime, Space.World);
    }
}
