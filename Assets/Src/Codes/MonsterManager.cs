using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;


public class MonsterManager : MonoBehaviour
{
  public static MonsterManager instance;

  // 현재 활성화된 몬스터들을 저장하는 리스트
  private List<MonsterController> activeMonsters = new List<MonsterController>();

  void Awake()
  {
    if (instance == null)
    {
      instance = this;
    }
    else
    {
      Destroy(gameObject);
    }
  }

  // 몬스터 추가
  public void AddMonster(MonsterController monster)
  {
    activeMonsters.Add(monster);
  }

  // 몬스터 제거
  public void RemoveMonster(MonsterController monster)
  {
    if (activeMonsters.Contains(monster))
    {
      Debug.Log($"Removing monster {monster.id} from activeMonsters.");
      activeMonsters.Remove(monster);
    }
    else
    {
      Debug.LogWarning($"Attempted to remove non-existent monster {monster.id}.");
    }
  }


  // 활성화된 몬스터 리스트 반환
  public List<MonsterController> GetActiveMonsters()
  {
    return activeMonsters;
  }

  public MonsterController FindMonsterById(AttackResult res)
  {
    // 현재 활성화된 모든 MonsterController 검색
    MonsterController[] monsters = FindObjectsOfType<MonsterController>();

    // 각 몬스터의 id를 비교하여 일치하는 객체 반환
    foreach (MonsterController monster in monsters)
    {
      if (monster.id == res.monsterId)
      {
        return monster; // 일치하는 몬스터를 찾으면 반환
      }
    }

    Debug.LogWarning($"Monster with ID {res.monsterId} not found.");

    // 일치하는 몬스터가 없으면 null 반환
    return null;
  }
}

