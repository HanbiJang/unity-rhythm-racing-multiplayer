using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    //Queue<GameObject> soundQueue;

    [SerializeField]
    public AudioClip[] audioQueue;

    int curSoundIndex = 0;
    AudioSource audioSource;
    bool hasScheduledStart = false;
    double scheduledStartDspTime = 0;

    [Header("Music Timing")]
    [Tooltip("노트가 플레이어와 맞부딪히는 순간에 박자가 오도록, 노래를 얼마나 늦게 재생할지 지정합니다. (초 단위)")]
    [SerializeField] private float musicDelaySeconds = 0f;

    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SoundManager>();
                if (instance == null)
                {
                    GameObject tmp = new GameObject();
                    tmp.name = typeof(SoundManager).Name;
                    instance = tmp.AddComponent<SoundManager>();
                }
            }
            DontDestroyOnLoad(instance);
            return instance;
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Road 씬에 AudioListener가 없으면 자동 추가
        if (FindObjectOfType<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // StartGame 패킷에서 예약된 시작 시간이 있으면 동기 재생
        if (GameState.Instance != null && GameState.Instance.HasSyncedStartTime)
        {
            StartSyncedPlayback(GameState.Instance.SyncedStartTimeUtcMs);
        }
        else
        {
            PlayImmediately();
        }
    }

    private void Update()
    {
        if (audioSource == null)
            return;

        if (hasScheduledStart)
        {
            if (AudioSettings.dspTime < scheduledStartDspTime)
            {
                return;
            }
            if (!audioSource.isPlaying)
            {
                return;
            }
            hasScheduledStart = false;
        }

        // 음악은 1회만 재생하고 반복 재생하지 않음
    }

    public void StartSyncedPlayback(long startTimeUtcMs)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[SoundManager] AudioSource is missing.");
            return;
        }
        if (audioQueue == null || audioQueue.Length == 0)
        {
            Debug.LogWarning("[SoundManager] audioQueue is empty.");
            return;
        }

        curSoundIndex = 0;
        audioSource.clip = audioQueue[curSoundIndex];
        audioSource.Stop();
        audioSource.loop = false;

        double nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        // 노트/플레이어 히트 타이밍과의 갭만큼 노래를 늦게 재생하기 위해, 시작 시각에 musicDelaySeconds를 더해줍니다.
        double startSeconds = startTimeUtcMs / 1000.0 + musicDelaySeconds;
        double delaySeconds = startSeconds - nowSeconds;

        if (delaySeconds >= 0)
        {
            scheduledStartDspTime = AudioSettings.dspTime + delaySeconds;
            hasScheduledStart = true;
            audioSource.PlayScheduled(scheduledStartDspTime);
        }
        else
        {
            float offset = (float)(-delaySeconds);
            if (audioSource.clip != null && audioSource.clip.length > 0f)
            {
                float maxOffset = Mathf.Max(0f, audioSource.clip.length - 0.01f);
                audioSource.time = Mathf.Min(offset, maxOffset);
            }
            audioSource.Play();
            hasScheduledStart = false;
        }
    }

    void PlayImmediately()
    {
        if (audioQueue == null || audioQueue.Length == 0)
        {
            Debug.LogWarning("[SoundManager] audioQueue is empty.");
            return;
        }
        curSoundIndex = 0;
        audioSource.clip = audioQueue[curSoundIndex];
        audioSource.loop = false;

        if (musicDelaySeconds > 0f)
        {
            audioSource.PlayDelayed(musicDelaySeconds);
        }
        else
        {
            audioSource.Play();
        }

        hasScheduledStart = false;
    }

    public void StopMusic()
    {
        if (audioSource == null)
        {
            return;
        }
        hasScheduledStart = false;
        audioSource.Stop();
    }

}
