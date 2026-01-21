using UnityEngine;

public class NodeSfxManager : MonoBehaviour
{
    private static NodeSfxManager instance;

    [Header("Node Hit SFX")]
    [SerializeField] private AudioClip[] nodeHitClips;
    [SerializeField] private AudioClip defaultHitClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool randomizePitch = false;
    [SerializeField] [Range(0.8f, 1.2f)] private float pitchMin = 0.95f;
    [SerializeField] [Range(0.8f, 1.2f)] private float pitchMax = 1.05f;

    private AudioSource audioSource;

    public static NodeSfxManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NodeSfxManager>();
                if (instance == null)
                {
                    GameObject tmp = new GameObject(nameof(NodeSfxManager));
                    instance = tmp.AddComponent<NodeSfxManager>();
                }
            }
            DontDestroyOnLoad(instance);
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayNodeHit(int nodeType)
    {
        AudioClip clip = GetNodeHitClip(nodeType);
        if (clip == null || audioSource == null)
        {
            return;
        }

        float prevPitch = audioSource.pitch;
        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
        }

        audioSource.PlayOneShot(clip, volume);

        if (randomizePitch)
        {
            audioSource.pitch = prevPitch;
        }
    }

    public void StopAllSfx()
    {
        if (audioSource == null)
        {
            return;
        }
        audioSource.Stop();
    }

    private AudioClip GetNodeHitClip(int nodeType)
    {
        if (nodeHitClips != null && nodeHitClips.Length > 0)
        {
            int index = Mathf.Clamp(nodeType, 0, nodeHitClips.Length - 1);
            if (nodeHitClips[index] != null)
            {
                return nodeHitClips[index];
            }
        }
        return defaultHitClip;
    }
}
