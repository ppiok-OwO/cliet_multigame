using System.IO;
using UnityEngine;
using TMPro;

public class GateManager : MonoBehaviour
{
  public GameObject gatePrefab; // 생성할 게이트 프리팹
  public string jsonFilePath = "gate.json";
  private GateDataCollection gateDataCollection;
  public TextMeshPro interactionText; // 상호작용 텍스트 UI

  void Start()
  {
    LoadGateData();
  }

  void LoadGateData()
  {
    string filePath = Path.Combine(Application.streamingAssetsPath, jsonFilePath);
    if (File.Exists(filePath))
    {
      string jsonData = File.ReadAllText(filePath);
      gateDataCollection = JsonUtility.FromJson<GateDataCollection>(jsonData);
    }
    else
    {
      Debug.LogError("JSON 파일을 찾을 수 없습니다: " + filePath);
    }
  }

  public void CreateGates()
  {
    if (gateDataCollection == null || gateDataCollection.data.Length == 0)
    {
      Debug.LogError("게이트 데이터를 불러올 수 없습니다.");
      return;
    }

    foreach (var gateData in gateDataCollection.data)
    {
      GameObject gate = Instantiate(gatePrefab);
      gate.transform.position = new Vector3(gateData.position.x, gateData.position.y, 0);
      gate.name = $"Gate_{gateData.id}";
      // GateController에 데이터 전달
      GateController gateController = gate.GetComponent<GateController>();
      if (gateController != null)
      {
        gateController.gateId = gateData.id;
        gateController.waveCount = gateData.waveCount;
        gateController.monstersPerWave = gateData.monstersPerWave;
      }
    }
  }
}
