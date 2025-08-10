using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Vivox;
using System;
using System.IO;
using System.Threading.Tasks;
#if AUTH_PACKAGE_PRESENT
using Unity.Services.Authentication;
#endif

public class VivoxVoiceManager : MonoBehaviour
{
    public const string LobbyChannelName = "lobbyChannel";

    // Check to see if we're about to be destroyed.
    static object m_Lock = new object();
    static VivoxVoiceManager m_Instance;

    //These variables should be set to the projects Vivox credentials if the authentication package is not being used
    //Credentials are available on the Vivox Developer Portal (developer.vivox.com) or the Unity Dashboard (dashboard.unity3d.com), depending on where the organization and project were made
    [SerializeField]
    string _key;
    [SerializeField]
    string _issuer;
    [SerializeField]
    string _domain;
    [SerializeField]
    string _server;

    public bool usePiper = false;
    
    /// <summary>
    /// Access singleton instance through this propriety.
    /// </summary>
    public static VivoxVoiceManager Instance
    {
        get
        {
            lock (m_Lock)
            {
                if (m_Instance == null)
                {
                    // Search for existing instance.
                    m_Instance = (VivoxVoiceManager)FindObjectOfType(typeof(VivoxVoiceManager));

                    // Create new instance if one doesn't already exist.
                    if (m_Instance == null)
                    {
                        // Need to create a new GameObject to attach the singleton to.
                        var singletonObject = new GameObject();
                        m_Instance = singletonObject.AddComponent<VivoxVoiceManager>();
                        singletonObject.name = typeof(VivoxVoiceManager).ToString() + " (Singleton)";
                    }
                }
                // Make instance persistent even if its already in the scene
                DontDestroyOnLoad(m_Instance.gameObject);
                return m_Instance;
            }
        }
    }

    async void Awake()
    {
        if (m_Instance != this && m_Instance != null)
        {
            Debug.LogWarning(
                "Multiple VivoxVoiceManager detected in the scene. Only one VivoxVoiceManager can exist at a time. The duplicate VivoxVoiceManager will be destroyed.");
            Destroy(this);
        }
        var options = new InitializationOptions();
        if (CheckManualCredentials())
        {
            options.SetVivoxCredentials(_server, _domain, _issuer, _key);
        }

        PiperManager.Instance.OnAudioDataGenerated += OnAudioDataGenerated;
        
        await UnityServices.InitializeAsync(options);
        await VivoxService.Instance.InitializeAsync();

    }

    private void OnDestroy()
    {
        if(PiperManager.Instance != null)
        {
            PiperManager.Instance.OnAudioDataGenerated -= OnAudioDataGenerated;
        }
    }

    void OnAudioDataGenerated(float[] audioData, int sampleRate)
    {
        if (usePiper)
        {
            var filePath = Path.Combine(Application.persistentDataPath, "PiperAudio.wav");
            SaveToWav(filePath, audioData, sampleRate);
            VivoxService.Instance.StartAudioInjection(filePath);
        }
    }

    public async Task InitializeAsync(string playerName)
    {

#if AUTH_PACKAGE_PRESENT
        if (!CheckManualCredentials())
        {
            AuthenticationService.Instance.SwitchProfile(playerName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
#endif
    }

    bool CheckManualCredentials()
    {
        return !(string.IsNullOrEmpty(_issuer) && string.IsNullOrEmpty(_domain) && string.IsNullOrEmpty(_server));
    }
    
    public void TextToSpeechSendMessage(string message)
    {
        if (usePiper)
        {
            PiperManager.Instance.Synthesize(message);
        }
        else
        {
            VivoxService.Instance.TextToSpeechSendMessage(message, TextToSpeechMessageType.RemoteTransmissionWithLocalPlayback);
        }
    }
    
    void SaveToWav(string filePath, float[] audioData, int sampleRate)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        using (var writer = new BinaryWriter(fileStream))
        {
            int sampleCount = audioData.Length;
            int channelCount = 1; // 모노 채널

            // WAV 헤더 작성
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2); // 전체 파일 크기
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // 서브 청크 크기
            writer.Write((short)1); // 오디오 포맷 (PCM)
            writer.Write((short)channelCount); // 채널 수
            writer.Write(sampleRate); // 샘플링 레이트
            writer.Write(sampleRate * channelCount * 2); // 바이트 레이트
            writer.Write((short)(channelCount * 2)); // 블록 정렬
            writer.Write((short)16); // 비트 깊이

            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(sampleCount * 2); // 데이터 청크 크기

            // 오디오 데이터 작성
            foreach (var sample in audioData)
            {
                short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(intSample);
            }
        }

        Debug.Log($"WAV 파일이 저장되었습니다: {filePath}");
    }
}
