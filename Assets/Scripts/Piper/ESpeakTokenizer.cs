using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Collections;
using System.IO;

public class AudioConfig
{
    public int sample_rate { get; set; }
    public string quality { get; set; }
}

public class ESpeakConfig
{
    public string voice { get; set; }
}

public class InferenceConfig
{
    public float noise_scale { get; set; }
    public float length_scale { get; set; }
    public float noise_w { get; set; }
}

public class PiperConfig
{
    public AudioConfig audio { get; set; }
    public ESpeakConfig espeak { get; set; }
    public InferenceConfig inference { get; set; }
    public string phoneme_type { get; set; }

    [JsonProperty("phoneme_id_map")]
    public Dictionary<string, int[]> PhonemeIdMap { get; set; }
}

public class ESpeakTokenizer : MonoBehaviour
{
    // public TextAsset jsonFile;

    public int SampleRate { get; private set; }
    public string Quality { get; private set; }
    public string Voice { get; private set; }
    public string PhonemeType { get; private set; }

    private PiperConfig config;
    private float[] inferenceParams;
    private bool isInitialized = false;

    // isInitialized에 대한 public getter 추가
    public bool IsInitialized { get { return isInitialized; } }
    
    // public string jsonFileName = "piper_config.json"; // Default JSON file name
    // void Awake()
    // {
    //     Initialize();
    // }

    // public void Initialize(string jsonFileName)
    // {
    //     if (isInitialized)
    //     {
    //         Debug.LogWarning("Tokenizer is already initialized. Skipping.");
    //         return;
    //     }
    //     
    //     StartCoroutine(LoadJsonFromStreamingAssets(jsonFileName));
    // }
    
    public IEnumerator LoadJsonFromStreamingAssets(string jsonFileName)
    {
        // 새로운 파일을 로드하기 전에 기존 상태를 초기화
        isInitialized = false;
        config = null;
        inferenceParams = null;
        
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Models", jsonFileName + ".onnx.json");

        string jsonText = null;

        #if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest www = UnityWebRequest.Get(jsonFilePath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load tokenizer JSON from StreamingAssets: {www.error}");
                yield break;
            }
            jsonText = www.downloadHandler.text;
        }
        #else
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError($"Tokenizer JSON file not found at path: {jsonFilePath}");
            yield break;
        }
        jsonText = File.ReadAllText(jsonFilePath);
        #endif

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("Loaded JSON text is null or empty.");
            yield break;
        }

        try
        {
            config = JsonConvert.DeserializeObject<PiperConfig>(jsonText);
            if (config == null || config.audio == null || config.espeak == null || config.inference == null || config.PhonemeIdMap == null)
            {
                Debug.LogError("JSON data is missing required fields or is invalid. Deserialization resulted in a partially/fully null object.");
                config = null;
                yield break;
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Failed to parse JSON file. Error: {ex.Message}");
            yield break;
        }

        inferenceParams = new float[3]
        {
            config.inference.noise_scale,
            config.inference.length_scale,
            config.inference.noise_w
        };

        this.SampleRate = config.audio.sample_rate;
        this.Quality = config.audio.quality;
        this.Voice = config.espeak.voice;
        this.PhonemeType = config.phoneme_type;
        
        isInitialized = true;
        Debug.Log("JSON parsing and setup completed successfully.");
        Debug.Log($"Extracted Settings: SampleRate={SampleRate}, Quality='{Quality}', Voice='{Voice}', PhonemeType='{PhonemeType}'");
    }

    public int[] Tokenize(string[] phonemes)
    {
        if (!isInitialized)
        {
            Debug.LogError("Tokenizer is not initialized. Check for errors during Awake().");
            return null;
        }

        int estimatedCapacity = (phonemes != null ? phonemes.Length * 2 : 0) + 3;
        var tokenizedList = new List<int>(estimatedCapacity) { 1, 0 };

        if (phonemes != null && phonemes.Length > 0)
        {
            foreach (string phoneme in phonemes)
            {
                if (config.PhonemeIdMap.TryGetValue(phoneme, out int[] ids) && ids.Length > 0)
                {
                    tokenizedList.Add(ids[0]);
                    tokenizedList.Add(0);
                }
                else
                {
                    Debug.LogWarning($"Token not found for phoneme: '{phoneme}'. It will be skipped.");
                }
            }
        }

        tokenizedList.Add(2);

        return tokenizedList.ToArray();
    }

    public float[] GetInferenceParams()
    {
        if (!isInitialized)
        {
            Debug.LogError("Tokenizer is not initialized. Cannot get inference parameters.");
            return null;
        }
        return (float[])inferenceParams.Clone();
    }
}