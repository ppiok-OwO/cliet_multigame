using UnityEngine;

public class MonsterController : MonoBehaviour
{
  private int hp;
  private int dmg;
  public int monsterId;

  public void Initialize(int hp, int dmg)
  {
    this.hp = hp;
    this.dmg = dmg;
  }

  // 이후 몬스터 로직 추가
}
