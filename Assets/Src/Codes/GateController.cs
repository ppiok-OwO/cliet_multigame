using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class GateController : MonoBehaviour
{
  public GameObject[] monsterPrefabs; // 소환할 몬스터 프리팹 배열
  public int gateId;
  public int monsterLv;
  public int waveCount = 3; // 웨이브 수
  public float waveInterval = 5f; // 웨이브 간격
  public int monstersPerWave = 1; // 웨이브당 소환할 몬스터 수
  public TextMeshPro interactionText; // 상호작용 텍스트 UI
  public float interactionRange = 3f; // 게이트와 플레이어 간 상호작용 거리
  public KeyCode interactionKey = KeyCode.E; // 상호작용 키
  public Transform player; // 플레이어의 Transform
  private bool isActivated = false; // 게이트가 활성화되었는지 여부
  private Camera mainCamera;

  private void Start()
  {
    // 메인 카메라 참조
    mainCamera = Camera.main;
    player = GameManager.instance.player.transform;
  }

  private void Update()
  {
    // 플레이어와 게이트 간 거리 계산
    float distanceToPlayer = Vector3.Distance(transform.position, player.position);

    // 상호작용 거리 안에 있는 경우 텍스트 표시
    if (distanceToPlayer <= interactionRange)
    {
      interactionText.gameObject.SetActive(true);
    }
    else
    {
      interactionText.gameObject.SetActive(false);
    }

    if (distanceToPlayer <= interactionRange && !isActivated)
    {
      if (Input.GetKeyDown(interactionKey))
      {
        isActivated = true;

        CreateMonster();
        // StartCoroutine(SpawnWaves());
      }

      isActivated = false;
    }
  }

  // public void CreateMonster()
  // {
  //   for (int wave = 0; wave < waveCount; wave++)
  //   {
  //     for (int i = 0; i < monstersPerWave; i++)
  //     {
  //       Vector3 randomPosition = GetRandomScreenPosition();
  //       int randomIndex = Random.Range(0, monsterPrefabs.Length);
  //       int monsterHp = 100 + monsterLv * 10;
  //       int monsterDmg = 10 + monsterLv;

  //       /** 여기서 서버로 몬스터의 데이터를 담아서 패킷을 보내야 함 **/
  //       NetworkManager.instance.SendCreateMonterPacket(randomPosition.x, randomPosition.y, randomIndex, gateId, monsterHp, monsterDmg);
  //     }
  //   }
  // }

  public void CreateMonster()
  {
    StartCoroutine(SpawnWavesWithInterval());
  }

  private IEnumerator SpawnWavesWithInterval()
  {

    for (int wave = 0; wave < waveCount; wave++)
    {
      List<CreateMonsterList.CreateMonster> monsters = new List<CreateMonsterList.CreateMonster>();
      for (int i = 0; i < monstersPerWave; i++)
      {
        // 랜덤 위치와 몬스터 데이터 생성
        Vector3 randomPosition = GetRandomScreenPosition();
        int randomIndex = Random.Range(0, monsterPrefabs.Length);
        int monsterHp = 100 + monsterLv * 10;
        int monsterDmg = 10 + monsterLv;

        monsters.Add(new CreateMonsterList.CreateMonster
        {
          monsterPosX = randomPosition.x,
          monsterPosY = randomPosition.y,
          monsterIndex = randomIndex,
          gateId = gateId,
          monsterHp = monsterHp,
          monsterDmg = monsterDmg
        });


      }

      // 서버로 패킷 전송
      NetworkManager.instance.SendCreateMonterPacket(monsters);

      // 각 웨이브 간격 대기
      yield return new WaitForSeconds(waveInterval);
    }

    Debug.Log("모든 웨이브가 완료되었습니다.");
  }

  public void SpawnWaves(int monsterIndex, float monsterX, float monsterY, int monsterHp, int monsterDmg)
  {
    // 몬스터 생성 로직
    GameObject monsterPrefab = monsterPrefabs[monsterIndex];
    Vector3 spawnPosition = new Vector3(monsterX, monsterY, 0); // 받은 위치 사용

    GameObject monster = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);

    // 몬스터 속성 초기화
    MonsterController monsterController = monster.GetComponent<MonsterController>();
    if (monsterController != null)
    {
      monsterController.Initialize(monsterHp, monsterDmg);
    }

    Debug.Log($"몬스터 생성 완료: 위치({monsterX}, {monsterY}), HP: {monsterHp}, DMG: {monsterDmg}");
  }

  private Vector3 GetRandomScreenPosition()
  {
    float screenX = Random.Range(0, Screen.width);
    float screenY = Random.Range(0, Screen.height);

    Vector3 screenPosition = new Vector3(screenX, screenY, mainCamera.nearClipPlane);
    Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

    // 플레이어와의 안전 거리 계산
    float safeDistance = 2f; // 안전 거리
    Transform player = GameObject.FindGameObjectWithTag("Player").transform;

    while (Vector3.Distance(worldPosition, player.position) < safeDistance)
    {
      screenX = Random.Range(0, Screen.width);
      screenY = Random.Range(0, Screen.height);
      screenPosition = new Vector3(screenX, screenY, mainCamera.nearClipPlane);
      worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
    }

    return worldPosition;
  }

  private void OnDrawGizmosSelected()
  {
    // 상호작용 범위 시각화 (디버깅 용도)
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, interactionRange);
  }
}