using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
  public static MonsterController instance;
  public string id;
  private int hp;
  private int maxHp;
  private int dmg;
  public bool isDead = false;
  private Vector2 velocity;    // 현재 속도 벡터 저장
  public float moveSpeed = 3f; // 이동 속도  
  private Vector2 lastPosition;
  private Vector2 targetPosition; // 목표 좌표
  private Rigidbody2D rigid;

  public GameObject hpTextObject; // 체력 텍스트 오브젝트
  private TextMeshPro hpText; // TextMeshPro 컴포넌트

  void Awake()
  {
    rigid = GetComponent<Rigidbody2D>();
    if (instance == null)
    {
      instance = this;
      Debug.Log("MonsterController.instance가 초기화되었습니다.");
    }
  }


  public void Initialize(string monsterId, int hp, int dmg)
  {
    this.id = monsterId;
    this.hp = hp;
    this.maxHp = hp;
    this.dmg = dmg;
  }
  void Start()
  {
    if (hpTextObject == null)
    {
      Debug.LogError("hpTextObject 프리팹이 연결되지 않았습니다.");
      return;
    }

    // 프리팹의 인스턴스를 생성
    GameObject instantiatedHpText = Instantiate(hpTextObject, transform.position, Quaternion.identity);

    // TextMeshPro 컴포넌트를 가져옴
    hpText = instantiatedHpText.GetComponent<TextMeshPro>();

    if (hpText == null)
    {
      Debug.LogError("hpTextObject에 TextMeshPro 컴포넌트가 없습니다.");
      return;
    }

    // 텍스트 초기화
    UpdateHpText();

    // 텍스트를 몬스터의 자식으로 설정
    instantiatedHpText.transform.SetParent(transform);

    // 텍스트 위치를 몬스터 머리 위로 조정
    instantiatedHpText.transform.localPosition = new Vector3(0, 1.5f, 0);

    // hpTextObject에 인스턴스를 저장
    hpTextObject = instantiatedHpText;
  }

  // 이후 몬스터 로직 추가
  public void UpdateMonsterPosition(MonsterMove data)
  {
    if (isDead) return; // 사망 상태인 경우 차단

    // 모든 몬스터를 가져옴
    MonsterController[] monsters = FindObjectsOfType<MonsterController>();

    foreach (MonsterController monster in monsters)
    {
      // 몬스터별로 서버 데이터를 전달
      monster.UpdatePositionFromServer(data.monsterLocations);
    }
  }

  public void UpdatePositionFromServer(List<MonsterMove.MonstersNextLocation> monsterLocations)
  {
    if (isDead) return; // 사망 상태인 경우 차단

    // 서버에서 받은 location 리스트 순회
    foreach (var location in monsterLocations)
    {
      // 현재 몬스터의 id와 location.id가 일치하면 위치 업데이트
      if (location.id == this.id)
      {
        // 목표 좌표 업데이트
        targetPosition = new Vector2(location.x, location.y);
        Debug.Log($"targetPosition : {targetPosition}");
        return;
      }
    }

    // 해당 id가 없으면 경고 로그 출력
    // Debug.LogWarning($"id가 '{this.id}'인 몬스터를 찾을 수 없습니다.");
  }

  void Update()
  {
    if (isDead) return; // 사망 상태인 경우 차단

    // 매 프레임마다 마지막 위치를 저장
    lastPosition = transform.position;

    // 매 프레임마다 현재 위치에 속도를 더해 이동
    transform.position += (Vector3)velocity * Time.deltaTime;

    // 현재 위치에서 목표 위치로 서서히 이동
    if ((Vector2)transform.position != targetPosition)
    {
      Vector2 newPosition = Vector2.MoveTowards(
          transform.position,   // 현재 위치
          targetPosition,       // 목표 위치
          moveSpeed * Time.deltaTime // 이동 속도
      );
      Debug.Log($"newPosition : {newPosition}");

      // Rigidbody를 사용해 이동
      rigid.MovePosition(newPosition);
    }

    // 체력 텍스트의 위치를 몬스터 위치에 맞춰 업데이트
    if (hpTextObject != null)
    {
      hpTextObject.transform.position = transform.position + new Vector3(0, 1.5f, 0);
    }
  }

  public void TakeDamage(AttackResult res)
  {
    if (isDead) return;

    Debug.Log($"res : ${res.hp}, ${res.isDead}");

    if (res.isDead || res.hp <= 0)
    {
      Die();  // isDead 설정 직후 즉시 Die 호출
    }
    else
    {
      hp = res.hp;
      UpdateHpText();
    }

  }


  public void Die()
  {
    // if (isDead) return; // 이미 사망한 상태라면 중복 처리 방지

    // isDead = true;
    Debug.Log($"Destroying monster {id}");

    // MonsterManager에서 제거
    MonsterManager.instance.RemoveMonster(this);

    // 체력 텍스트 제거
    if (hpTextObject != null)
    {
      Destroy(hpTextObject);
    }

    // 인스턴스일 경우에만 삭제
    if (Application.isPlaying && gameObject.scene.IsValid())
    {
      Destroy(gameObject);
    }
    else
    {
      Debug.LogWarning("Attempted to destroy an asset instead of an instance.");
    }
  }

  private void UpdateHpText()
  {
    if (isDead) return; // 사망 상태인 경우 이동 처리 차단

    if (hpText != null)
    {
      hpText.text = $"{hp} / {maxHp}"; // 현재 체력 / 최대 체력 형식
    }
  }

  void OnCollisionEnter2D(Collision2D collision)
  {
    if (isDead) return; // 사망 상태인 경우 차단

    if (collision.gameObject.CompareTag("Player"))
    {
      // 충돌 시 위치를 이전 위치로 되돌림
      transform.position = lastPosition;
    }
  }
}

