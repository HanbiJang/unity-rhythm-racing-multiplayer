using UnityEngine;

public class EnvironmentScroller : MonoBehaviour
{
    [Tooltip("NoteMovement의 speed와 동일한 값으로 설정하세요.")]
    public float speed = 10f;

    void Update()
    {
        if (GameModeManager.instance != null && GameModeManager.instance.bGameOver)
            return;

        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);
    }
}
