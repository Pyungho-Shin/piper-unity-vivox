using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

[RequireComponent(typeof(AudioSource))]
public class PiperManager : MonoBehaviour
{
    // public ModelAsset modelAsset;
    public ESpeakTokenizer tokenizer;

    public Text voiceNameText;

    private Worker engine;
    private AudioSource audioSource;
    private bool isInitialized = false;

    public bool playImmediately = true;

    [Range(0.0f, 1.0f)] public float commaDelay = 0.1f;
    [Range(0.0f, 1.0f)] public float periodDelay = 0.5f;
    [Range(0.0f, 1.0f)] public float questionExclamationDelay = 0.6f;

    public Action<float[], int> OnAudioDataGenerated;
    
    private static PiperManager _instance;

    public static PiperManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PiperManager>();
            }
            return _instance;
        }
    }
    
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component not found! It will be added automatically.");
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        OnAudioDataGenerated += (audioData, sampleRate) =>
        {
            if (audioData != null && audioData.Length > 0 )
            {
                if (playImmediately)
                {
                    Debug.Log($"Audio data generated with length: {audioData.Length}, Sample Rate: {sampleRate}");
                    AudioClip clip = AudioClip.Create("GeneratedSpeech", audioData.Length, 1, sampleRate, false);
                    clip.SetData(audioData, 0);
                    audioSource.PlayOneShot(clip);
                }
            }
            else
            {
                Debug.LogError("Generated audio data is null or empty.");
            }
        };
        
        StartCoroutine(InitializePiper());
    }

    private IEnumerator InitializePiper()
    {
        string espeakDataPath;

        #if UNITY_ANDROID && !UNITY_EDITOR
            espeakDataPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data");

            if (!Directory.Exists(espeakDataPath))
            {
                Debug.Log("Android: eSpeak data not found in persistentDataPath. Starting copy process...");

                string zipSourcePath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data.zip");
                string zipDestPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data.zip");

                using (UnityWebRequest www = UnityWebRequest.Get(zipSourcePath))
                {
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to load espeak-ng-data.zip from StreamingAssets: {www.error}");
                        yield break;
                    }

                    File.WriteAllBytes(zipDestPath, www.downloadHandler.data);

                    try
                    {
                        ZipFile.ExtractToDirectory(zipDestPath, Application.persistentDataPath);
                        Debug.Log("eSpeak data successfully unzipped to persistentDataPath.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error unzipping eSpeak data: {e.Message}");
                        yield break;
                    }
                    finally
                    {
                        if (File.Exists(zipDestPath))
                        {
                            File.Delete(zipDestPath);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Android: eSpeak data already exists in persistentDataPath.");
            }
        #else
            espeakDataPath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data");
            Debug.Log($"Editor/Standalone: Using eSpeak data directly from StreamingAssets: {espeakDataPath}");
            yield return null;
        #endif

        InitializeESpeak(espeakDataPath);

        // 이 부분에서 ESpeak 초기화만 수행하고, 모델은 로드하지 않음
        InitializeESpeak(espeakDataPath);
        
        isInitialized = true;
        Debug.Log("Piper Manager base initialization complete (eSpeak). Ready to load models.");
        // LoadModelFromStreamingAssets("en_US-amy-medium");
    }

    private void InitializeESpeak(string dataPath)
    {
        int initResult = ESpeakNG.espeak_Initialize(0, 0, dataPath, 0);

        if (initResult > 0)
        {
            Debug.Log($"[PiperManager] eSpeak-ng Initialization SUCCEEDED. Data path: {dataPath}");

            if (tokenizer == null || string.IsNullOrEmpty(tokenizer.Voice))
            {
                Debug.LogError("[PiperManager] Tokenizer is not assigned or has no voice name.");
                return;
            }

            string voiceName = tokenizer.Voice;
            int voiceResult = ESpeakNG.espeak_SetVoiceByName(voiceName);

            if (voiceResult == 0)
                Debug.Log($"[PiperManager] Set voice to '{voiceName}' SUCCEEDED.");
            else
                Debug.LogError($"[PiperManager] Set voice to '{voiceName}' FAILED. Error code: {voiceResult}");
        }
        else
        {
            Debug.LogError($"[PiperManager] eSpeak-ng Initialization FAILED. Error code: {initResult}");
        }
    }
    
    // public void LoadModelFromStreamingAssets(string modelFileName)
    // {
    //     if (!isInitialized)
    //     {
    //         Debug.LogError("Piper Manager is not ready. eSpeak data not initialized.");
    //         return;
    //     }
    //
    //     StartCoroutine(LoadModelCoroutine(modelFileName));
    //     StartCoroutine(tokenizer.LoadJsonFromStreamingAssets(modelFileName));
    // }

    private IEnumerator LoadModelCoroutine(string modelFileName)
    {
        // 기존 엔진이 있다면 해제
        if (engine != null)
        {
            Debug.Log("Disposing of the old model engine.");
            engine.Dispose();
            engine = null; // null로 설정하여 참조 제거
        }
        
        
        
        
        // Android의 경우 StreamingAssets 경로가 WWW를 통해 접근해야 함
        #if UNITY_ANDROID && !UNITY_EDITOR
        string fullPath = modelPath;
        using (UnityWebRequest www = UnityWebRequest.Get(fullPath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load model from StreamingAssets: {www.error}");
                yield break;
            }

            // 모델 데이터를 메모리로 로드
            byte[] modelData = www.downloadHandler.data;
            try
            {
                var model = ModelLoader.Load(modelData);
                engine?.Dispose(); // 기존 엔진이 있다면 폐기
                engine = new Worker(model, BackendType.GPUPixel);
                voiceNameText.text = $"Model: {modelFileName}";
                Debug.Log($"Model '{modelFileName}' loaded successfully.");
                _WarmupModel();
                Debug.Log("Finished warmup.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading model from byte array: {e.Message}");
            }
        }
        #else
        string modelPath = Path.Combine(Application.streamingAssetsPath, "Models", modelFileName + ".sentis");
        if (!File.Exists(modelPath))
        {
            Debug.LogError($"Model file not found at path: {modelPath}");
            yield break;
        }
        
        var model = ModelLoader.Load(modelPath);
        // engine?.Dispose();
        engine = new Worker(model, BackendType.GPUPixel);
        voiceNameText.text = $"Model: {modelFileName}";
        Debug.Log($"Model '{modelFileName}' loaded successfully.");
        
        ESpeakNG.espeak_SetVoiceByName(tokenizer.Voice);
        
        _WarmupModel();
        Debug.Log("Finished warmup.");
        yield return null;
        #endif
    }
    
    // 새로운 public 메서드 추가 (외부 UI에서 호출될 메서드)
    public void LoadNewModel(string modelFileName)
    {
        if (!isInitialized)
        {
            Debug.LogError("Piper Manager is not ready. eSpeak data not initialized.");
            return;
        }
    
        // 토크나이저와 모델을 순차적으로 로드
        // 코루틴은 StartCoroutine으로 실행해야 함
        StartCoroutine(LoadModelAndTokenizerAsync(modelFileName));
    }
    
    private IEnumerator LoadModelAndTokenizerAsync(string modelFileName)
    {
        // 1. 토크나이저 JSON 파일 로드
        Debug.Log($"Loading tokenizer config for model: {modelFileName}");
        yield return StartCoroutine(tokenizer.LoadJsonFromStreamingAssets(modelFileName));

        // 토크나이저 초기화가 성공했는지 확인
        if (!tokenizer.IsInitialized)
        {
            Debug.LogError("Failed to initialize tokenizer. Aborting model load.");
            yield break;
        }

        // 2. 센티스 모델 파일 로드
        Debug.Log($"Loading Sentis model file for: {modelFileName}");
        yield return StartCoroutine(LoadModelCoroutine(modelFileName));
    
        // 이 시점에서 `LoadModelCoroutine` 내의 `engine`이 초기화되었는지 확인
        if (engine != null)
        {
            Debug.Log($"Model '{modelFileName}' and its tokenizer have been successfully loaded and set up.");
        }
        else
        {
            Debug.LogError($"Model '{modelFileName}' loading failed.");
        }
    }

    void OnDestroy()
    {
        engine?.Dispose();
    }

    public void OnSubmitText(Text textField)
    {
        if (string.IsNullOrEmpty(textField.text))
        {
            Debug.LogError("Input text is empty. Please enter some text.");
            return;
        }

        Debug.Log($"Input text: {textField.text}");
        Synthesize(textField.text);
    }
    
    public void Synthesize(string text)
    {
        if (!isInitialized)
        {
            Debug.LogError("Piper Manager is not initialized.");
            return;
        }
        StartCoroutine(SynthesizeCoroutine(text));
    }

    private IEnumerator SynthesizeCoroutine(string text)
    {
        string delayPunctuationPattern = @"([,.?!;:])";
        string nonDelayPunctuationPattern = @"[^\w\s,.?!;:]";

        string[] parts = Regex.Split(text, delayPunctuationPattern);

        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part.Trim()))
            {
                continue;
            }

            bool isDelayPunctuation = Regex.IsMatch(part, "^" + delayPunctuationPattern + "$");

            if (isDelayPunctuation)
            {
                float delay = 0f;
                switch (part)
                {
                    case ",":
                    case ";":
                    case ":":
                        delay = commaDelay;
                        break;
                    case ".":
                        delay = periodDelay;
                        break;
                    case "?":
                    case "!":
                        delay = questionExclamationDelay;
                        break;
                }
                if (delay > 0)
                {
                    Debug.Log($"Pausing for '{part}' for {delay} seconds.");
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                string cleanedChunk = Regex.Replace(part, nonDelayPunctuationPattern, " ");
                cleanedChunk = cleanedChunk.Trim();

                if (!string.IsNullOrEmpty(cleanedChunk))
                {
                    Debug.Log($"Processing text chunk: \"{cleanedChunk}\"");
                    _SynthesizeAndPlayChunk(cleanedChunk);
                    yield return new WaitWhile(() => audioSource.isPlaying);
                }
            }
        }
        Debug.Log("Finished playing all chunks.");
    }

    private void _SynthesizeAndPlayChunk(string textChunk)
    {
        string phonemeStr = Phonemize(textChunk);
        if (string.IsNullOrEmpty(phonemeStr))
        {
            Debug.LogError($"Phoneme conversion failed for chunk: \"{textChunk}\"");
            return;
        }

        string[] phonemeArray = phonemeStr.Trim().Select(c => c.ToString()).ToArray();
        int[] phonemeTokens = tokenizer.Tokenize(phonemeArray);

        float[] scales = tokenizer.GetInferenceParams();
        int[] inputLength = { phonemeTokens.Length };

        Debug.Log($"Model inputs prepared. Token count: {inputLength[0]}, Scales: [{string.Join(", ", scales)}]");

        using var phonemesTensor = new Tensor<int>(new TensorShape(1, phonemeTokens.Length), phonemeTokens);
        using var lengthTensor = new Tensor<int>(new TensorShape(1), inputLength);
        using var scalesTensor = new Tensor<float>(new TensorShape(3), scales);

        engine.SetInput("input", phonemesTensor);
        engine.SetInput("input_lengths", lengthTensor);
        engine.SetInput("scales", scalesTensor);

        engine.Schedule();

        using var outputTensor = (engine.PeekOutput() as Tensor<float>).ReadbackAndClone();
        float[] audioData = outputTensor.DownloadToArray();

        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("Failed to generate audio data or the data is empty.");
            return;
        }
        Debug.Log($"Generated audio data length: {audioData.Length}");

        int sampleRate = tokenizer.SampleRate;
        
        
        OnAudioDataGenerated?.Invoke(audioData, sampleRate);
        // AudioClip clip = AudioClip.Create("GeneratedSpeech", audioData.Length, 1, sampleRate, false);
        // clip.SetData(audioData, 0);
        //
        // Debug.Log($"Speech generated! AudioClip length: {clip.length:F2}s. Playing.");
        // audioSource.PlayOneShot(clip);
    }
    
    private void _WarmupModel()
    {
        Debug.Log("Warming up the model with a dummy run...");
        string warmupText = "hello";

        string phonemeStr = Phonemize(warmupText);
        if (string.IsNullOrEmpty(phonemeStr))
        {
            Debug.LogError("Warmup failed: Phoneme conversion failed.");
            return;
        }

        string[] phonemeArray = phonemeStr.Trim().Select(c => c.ToString()).ToArray();
        int[] phonemeTokens = tokenizer.Tokenize(phonemeArray);

        float[] scales = tokenizer.GetInferenceParams();
        int[] inputLength = { phonemeTokens.Length };

        using var phonemesTensor = new Tensor<int>(new TensorShape(1, phonemeTokens.Length), phonemeTokens);
        using var lengthTensor = new Tensor<int>(new TensorShape(1), inputLength);
        using var scalesTensor = new Tensor<float>(new TensorShape(3), scales);

        engine.SetInput("input", phonemesTensor);
        engine.SetInput("input_lengths", lengthTensor);
        engine.SetInput("scales", scalesTensor);

        engine.Schedule();
        
        using var outputTensor = (engine.PeekOutput() as Tensor<float>).ReadbackAndClone();
        
        if (outputTensor.shape[0] > 0)
        {
            Debug.Log($"Model warmup successful. Generated dummy audio data length: {outputTensor.shape[0]}.");
        }
        else
        {
            Debug.LogError("Model warmup failed: Generated output data is empty.");
        }
    }
    
    private string Phonemize(string text)
    {
        Debug.Log($"Phonemizing text: \"{text}\"");
        IntPtr textPtr = IntPtr.Zero;
        try
        {
            Debug.Log($"[PiperManager] Cleaned text for phonemization: \"{text}\"");
            byte[] textBytes = Encoding.UTF8.GetBytes(text + "\0");
            textPtr = Marshal.AllocHGlobal(textBytes.Length);
            Marshal.Copy(textBytes, 0, textPtr, textBytes.Length);
            
            IntPtr pointerToText = textPtr;

            int textMode = 0; // espeakCHARS_AUTO=0
            int phonemeMode = 2; // bit 1: 0=eSpeak's ascii phoneme names, 1= International Phonetic Alphabet (as UTF-8 characters).

            IntPtr resultPtr = ESpeakNG.espeak_TextToPhonemes(ref pointerToText, textMode, phonemeMode);

            if (resultPtr != IntPtr.Zero)
            {
                string phonemeString = PtrToUtf8String(resultPtr);
                Debug.Log($"[PHONEMES] {phonemeString}");
                return phonemeString;
            }
            else
            {
                Debug.LogError("[PiperManager] Phonemize FAILED. The function returned a null pointer.");
                return null;
            }
        }
        finally
        {
            if (textPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(textPtr);
            }
        }
    }
    
    private static string PtrToUtf8String(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return "";
        var byteList = new List<byte>();
        for (int offset = 0; ; offset++)
        {
            byte b = Marshal.ReadByte(ptr, offset);
            if (b == 0) break;
            byteList.Add(b);
        }
        return Encoding.UTF8.GetString(byteList.ToArray());
    }
}