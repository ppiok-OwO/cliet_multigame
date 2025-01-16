using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
  public static MonsterController instance;
  private string id;
  private int hp;
  private int dmg;
  private Vector2 velocity;    // 현재 속도 벡터 저장
  public float moveSpeed = 3f; // 이동 속도  
  private Vector2 lastPosition;
  private Vector2 targetPosition; // 목표 좌표
  private Rigidbody2D rigid;

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
    this.dmg = dmg;
  }

  // 이후 몬스터 로직 추가
  public void UpdateMonsterPosition(MonsterMove data)
  {
    Debug.Log($"서버에서 전달된 몬스터 데이터: {data.monsterLocations.Count}개");
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
    // 서버에서 받은 location 리스트 순회
    foreach (var location in monsterLocations)
    {
      // 현재 몬스터의 id와 location.id가 일치하면 위치 업데이트
      if (location.id == this.id)
      {
        // 목표 좌표 업데이트
        targetPosition = new Vector2(location.x, location.y);
        Debug.Log($"몬스터 {this.id}의 목표 좌표가 업데이트되었습니다: {targetPosition}");

        return;
      }
    }

    // 해당 id가 없으면 경고 로그 출력
    // Debug.LogWarning($"id가 '{this.id}'인 몬스터를 찾을 수 없습니다.");
  }

  void Update()
  {
    // 매 프레임마다 마지막 위치를 저장
    lastPosition = transform.position;

    if (velocity != Vector2.zero)
    {
      Debug.Log($"몬스터 {id} 이동 중: {velocity}, 위치: {transform.position}");
    }

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

      // Rigidbody를 사용해 이동
      rigid.MovePosition(newPosition);

      Debug.Log($"몬스터 {id}가 이동 중: 현재 위치 {transform.position}, 목표 위치 {targetPosition}");
    }
  }

  void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Player"))
    {
      // 충돌 시 위치를 이전 위치로 되돌림
      transform.position = lastPosition;
    }
  }
}
