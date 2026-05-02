using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CreateModeNoteEditor : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureEditorExists()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != "CreateMode")
        {
            return;
        }

        if (FindObjectOfType<CreateModeNoteEditor>() != null)
        {
            return;
        }

        GameObject editorObject = new GameObject("CreateModeNoteEditor");
        editorObject.AddComponent<CreateModeNoteEditor>();
    }
    [Header("Songs")]
    [SerializeField] private List<AudioClip> songs = new List<AudioClip>();
    [SerializeField] private AudioSource audioSource;

    [Header("Recording")]
    [SerializeField] private float gridSeconds = 0.25f;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode centerKey = KeyCode.S;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    [Header("Fail Nodes")]
    [SerializeField] private int failCountMin = 0;
    [SerializeField] private int failCountMax = 50;
    [SerializeField] private int failNodeCount = 10;

    [Header("Hit Timing Simulation")]
    [Tooltip("실제 게임에서 노트가 생성되는 위치와 플레이어 사이 거리. SpwanerFollower가 씬에 있으면 GapBetweenPlayer 값을 자동으로 사용하고, 없을 때만 이 값을 사용합니다.")]
    [SerializeField] private float simulationGapDistance = 30f;
    [Tooltip("실제 게임에서 노트가 이동하는 속도 (GameModeManager.m_RoadMoveSpeed 와 동일하게 맞추세요).")]
    [SerializeField] private float simulationNodeSpeed = 40f;
    [Tooltip("인게임씬에서의 SpawnerFollower - GapBetweenPlayer와 같은 값을 사용해야합니다")]
    [SerializeField] private float GapBetweenPlayer = 30f;

    [Header("XML Output")]
    [SerializeField] private string outputRelativePath = "MyServer/MyServer/MusicNodeData.xml";

    [Header("UI References (옵션)")]
    [SerializeField] private Button recordButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button endButton;
    [SerializeField] private Slider failSlider;

    private readonly List<RecordEntry> recorded = new List<RecordEntry>();
    private readonly System.Random rng = new System.Random();

    private int selectedSongIndex;
    private bool isRecording;
    private float recordEndTime;
    private string status = "대기 중";

    private struct KeyLog { public string Text; public float ExpireTime; }
    private readonly List<KeyLog> keyLogs = new List<KeyLog>();
    private const float keyLogDuration = 1.5f;

    private void Start()
    {
        Debug.Log("[CreateModeNoteEditor] Start 호출됨");
        EnsureAudioSource();
        LoadSongsIfEmpty();
        if (songs.Count > 0)
        {
            selectedSongIndex = Mathf.Clamp(selectedSongIndex, 0, songs.Count - 1);
            audioSource.clip = songs[selectedSongIndex];
        }

        SetupUIBindings();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!isRecording)
        {
            return;
        }

        if (Input.GetKeyDown(leftKey))
        {
            RecordHit(0);
            AddKeyLog("A 입력");
        }
        else if (Input.GetKeyDown(centerKey))
        {
            RecordHit(1);
            AddKeyLog("S 입력");
        }
        else if (Input.GetKeyDown(rightKey))
        {
            RecordHit(2);
            AddKeyLog("D 입력");
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        DrawKeyLogs();
    }

    private void StartRecording()
    {
        if (songs.Count == 0 || audioSource.clip == null)
        {
            status = "노래가 없습니다.";
            return;
        }

        recorded.Clear();
        recordEndTime = 0f;
        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.Play();
        isRecording = true;
        status = "녹음 시작";
    }

    private void StopRecording()
    {
        if (!isRecording)
        {
            return;
        }

        isRecording = false;
        recordEndTime = audioSource.time;
        audioSource.Stop();
        status = $"녹음 종료 (기록 {recorded.Count}개)";
    }

    private void EndAndSave()
    {
        if (isRecording)
        {
            StopRecording();
        }

        if (songs.Count == 0 || audioSource.clip == null)
        {
            status = "저장 실패: 노래가 없습니다.";
            return;
        }

        float duration = recordEndTime > 0f ? recordEndTime : audioSource.clip.length;
        List<NodeEntry> nodes = BuildNodeList(duration);
        string outputPath = GetOutputPath();
        WriteXml(nodes, outputPath);
        status = $"XML 저장 완료: {outputPath}";
    }

    // ===== UI에서 호출할 공개 메서드들 =====
    public void UI_StartRecord()
    {
        StartRecording();
    }

    public void UI_StopRecord()
    {
        StopRecording();
    }

    public void UI_EndAndSave()
    {
        EndAndSave();
    }

    public void UI_OnFailCountChanged(float value)
    {
        failNodeCount = Mathf.RoundToInt(value);
    }

    public void UI_SelectSong(int index)
    {
        if (songs == null || songs.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, songs.Count - 1);
        selectedSongIndex = index;
        audioSource.clip = songs[selectedSongIndex];
        status = $"선택됨: {audioSource.clip.name}";
    }

    private void RecordHit(int posIndex)
    {
        float time = audioSource.isPlaying ? audioSource.time : Time.time;
        recorded.Add(new RecordEntry { Time = time, Pos = posIndex });
        status = $"기록: {time:F3}s, pos={posIndex}";
    }

    private void AddKeyLog(string text)
    {
        keyLogs.Add(new KeyLog { Text = text, ExpireTime = Time.unscaledTime + keyLogDuration });
    }

    private void DrawKeyLogs()
    {
        float now = Time.unscaledTime;
        keyLogs.RemoveAll(k => k.ExpireTime <= now);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 60,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float boxWidth = 300f;
        float boxHeight = 100f;
        float x = Screen.width * 0.5f - boxWidth * 0.5f;
        float startY = Screen.height * 0.5f - 40f;

        for (int i = keyLogs.Count - 1; i >= 0; i--)
        {
            float age = 1f - Mathf.Clamp01((keyLogs[i].ExpireTime - now) / keyLogDuration);
            float alpha = 1f - age;

            style.normal.textColor = new Color(0.2f, 1f, 0.2f, alpha);
            float y = startY - (keyLogs.Count - 1 - i) * (boxHeight + 4f);
            GUI.Label(new Rect(x, y, boxWidth, boxHeight), keyLogs[i].Text, style);
        }
    }

    private List<NodeEntry> BuildNodeList(float duration)
    {
        List<NodeEntry> result = new List<NodeEntry>();

        // 실제 게임에서는 서버가 Node.Time 시점에 노드를 스폰하고,
        // 노드는 앞에서부터 플레이어 쪽으로 이동해서 일정 시간 후에 판정선에 도달합니다.
        // CreateMode에서 우리가 기록하는 시간은 "플레이어가 치고 싶은 순간(히트 타이밍)" 이므로,
        // 서버에 넘기는 XML Time 값은 "스폰 시점 = 히트 타이밍 - 이동 시간" 이 되어야 합니다.

        // 실제 GapBetweenPlayer(SpwanerFollower) 값을 우선 사용, 없으면 simulationGapDistance 사용
        float gapDistance = simulationGapDistance;  

        if (GapBetweenPlayer > 0f)
        {
            gapDistance = GapBetweenPlayer;
        }

        float travelTime = 0f;
        if (simulationNodeSpeed > 0f && gapDistance > 0f)
        {
            travelTime = gapDistance / simulationNodeSpeed;
        }

        foreach (RecordEntry entry in recorded)
        {
            float hitTime = entry.Time;
            // 스폰 시점 = 히트 타임 - 이동 시간 (0초 미만으로는 내려가지 않게 클램프)
            float spawnTime = Mathf.Max(0f, hitTime - travelTime);

            result.Add(new NodeEntry
            {
                Time = spawnTime,
                Pos = entry.Pos,
                Type = UnityEngine.Random.Range(0, 3)
            });
        }

        if (gridSeconds <= 0f)
        {
            gridSeconds = 0.25f;
        }

        List<float> candidateTimes = BuildCandidateFailTimes(duration);
        int failCount = Mathf.Clamp(failNodeCount, 0, candidateTimes.Count);
        for (int i = 0; i < failCount; i++)
        {
            int pickIndex = rng.Next(candidateTimes.Count);
            float candidateHitTime = candidateTimes[pickIndex];
            candidateTimes.RemoveAt(pickIndex);

            float time = Mathf.Max(0f, candidateHitTime - travelTime);

            result.Add(new NodeEntry
            {
                Time = time,
                Pos = rng.Next(0, 3),
                Type = rng.Next(3, 6)
            });
        }

        result.Sort((a, b) => a.Time.CompareTo(b.Time));
        return result;
    }

    private List<float> BuildCandidateFailTimes(float duration)
    {
        HashSet<int> occupiedSlots = new HashSet<int>();
        foreach (RecordEntry entry in recorded)
        {
            int slot = Mathf.RoundToInt(entry.Time / gridSeconds);
            occupiedSlots.Add(slot);
        }

        int maxSlot = Mathf.Max(0, Mathf.FloorToInt(duration / gridSeconds));
        List<float> candidates = new List<float>();
        for (int slot = 0; slot <= maxSlot; slot++)
        {
            if (occupiedSlots.Contains(slot))
            {
                continue;
            }

            float time = slot * gridSeconds;
            candidates.Add(time);
        }

        return candidates;
    }

    private string GetOutputPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, outputRelativePath);
        string dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return outputPath;
    }

    private void WriteXml(List<NodeEntry> nodes, string outputPath)
    {
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            NewLineChars = "\n",
            OmitXmlDeclaration = false
        };

        using (XmlWriter writer = XmlWriter.Create(outputPath, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("AllNode");
            foreach (NodeEntry node in nodes)
            {
                writer.WriteStartElement("Node");
                writer.WriteElementString("Time", node.Time.ToString("F3", CultureInfo.InvariantCulture));
                writer.WriteElementString("Type", node.Type.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("Pos", node.Pos.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
    }

    private void SetupUIBindings()
    {
        // 인스펙터에서 지정된 경우 그대로 사용
        // 비어있으면 이름으로 자동 탐색 (RecordButton, StopButton, EndButton, FailSlider)
        if (recordButton == null)
        {
            GameObject go = GameObject.Find("RecordButton");
            if (go != null) recordButton = go.GetComponent<Button>();
        }

        if (stopButton == null)
        {
            GameObject go = GameObject.Find("StopButton");
            if (go != null) stopButton = go.GetComponent<Button>();
        }

        if (endButton == null)
        {
            GameObject go = GameObject.Find("EndButton");
            if (go != null) endButton = go.GetComponent<Button>();
        }

        if (failSlider == null)
        {
            GameObject go = GameObject.Find("FailSlider");
            if (go != null) failSlider = go.GetComponent<Slider>();
        }

        if (recordButton != null)
        {
            recordButton.onClick.RemoveListener(UI_StartRecord);
            recordButton.onClick.AddListener(UI_StartRecord);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(UI_StopRecord);
            stopButton.onClick.AddListener(UI_StopRecord);
        }

        if (endButton != null)
        {
            endButton.onClick.RemoveListener(UI_EndAndSave);
            endButton.onClick.AddListener(UI_EndAndSave);
        }

        if (failSlider != null)
        {
            failSlider.minValue = failCountMin;
            failSlider.maxValue = failCountMax;
            failSlider.value = failNodeCount;
            failSlider.onValueChanged.RemoveListener(UI_OnFailCountChanged);
            failSlider.onValueChanged.AddListener(UI_OnFailCountChanged);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        GameObject audioObject = GameObject.Find("CreateModeAudio");
        if (audioObject == null)
        {
            audioObject = new GameObject("CreateModeAudio");
        }

        audioSource = audioObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = audioObject.AddComponent<AudioSource>();
        }
    }

    private void LoadSongsIfEmpty()
    {
        if (songs.Count > 0)
        {
            return;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null && !songs.Contains(clip))
            {
                songs.Add(clip);
            }
        }
#endif
    }

    [Serializable]
    private struct RecordEntry
    {
        public float Time;
        public int Pos;
    }

    private struct NodeEntry
    {
        public float Time;
        public int Type;
        public int Pos;
    }
}
