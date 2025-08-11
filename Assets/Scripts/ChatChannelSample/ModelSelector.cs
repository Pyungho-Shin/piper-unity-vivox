using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using TMPro;

public class ModelSelector : MonoBehaviour
{
    public TMP_Dropdown modelDropdown;

    private void Start()
    {
        if (modelDropdown == null)
        {
            Debug.LogError("Dropdown 컴포넌트가 할당되지 않았습니다.");
            return;
        }
        
        // 모델 이름 목록을 로드하고 드롭다운에 추가합니다.
        LoadModelNames();

        // 드롭다운의 값 변경 이벤트 리스너를 등록합니다.
        modelDropdown.onValueChanged.AddListener(OnModelSelected);
    }

    private void LoadModelNames()
    {
        string modelsPath = Path.Combine(Application.streamingAssetsPath, "Models");
        List<string> modelNames = new List<string>();

        // 첫 번째 옵션으로 "NotSelected"를 추가합니다.
        modelNames.Add("NotSelected");

        if (Directory.Exists(modelsPath))
        {
            string[] sentisFiles = Directory.GetFiles(modelsPath, "*.sentis");
            
            foreach (string filePath in sentisFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                modelNames.Add(fileName);
            }

            modelDropdown.ClearOptions();
            modelDropdown.AddOptions(modelNames);
            
            // 드롭다운의 초기 선택을 "NotSelected"로 설정합니다.
            modelDropdown.value = 0;
            modelDropdown.RefreshShownValue();
        }
        else
        {
            Debug.LogError("Models 폴더를 찾을 수 없습니다: " + modelsPath);
            modelDropdown.ClearOptions();
            modelDropdown.AddOptions(new List<string> { "NotSelected" });
        }
    }

    private void OnModelSelected(int index)
    {
        // 드롭다운에서 선택된 옵션의 텍스트를 가져옵니다.
        string selectedModelName = modelDropdown.options[index].text;
        Debug.Log("선택된 모델: " + selectedModelName);

        // 여기에서 선택된 모델 이름을 사용하여 실제 모델 파일을 로드하거나 다른 로직을 수행할 수 있습니다.
        // 예:
        // string sentisPath = Path.Combine(Application.streamingAssetsPath, "Models", selectedModelName + ".sentis");
        // string tokenPath = Path.Combine(Application.streamingAssetsPath, "Models", selectedModelName + ".onnx.json");
        // 모델과 토큰 파일을 로드하는 로직...
        
        // PiperManager의 LoadNewModel 메서드를 호출하여 모델 교체
        if (PiperManager.Instance != null)
        {
            PiperManager.Instance.LoadNewModel(selectedModelName);
        }
        else
        {
            Debug.LogError("PiperManager 인스턴스를 찾을 수 없습니다.");
        }
    }
}