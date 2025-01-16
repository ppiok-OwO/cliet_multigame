using UnityEngine;

public class BulletManager : MonoBehaviour
{
  public static BulletManager instance;
  public GameObject bulletPrefab; // 총알 프리팹

  private void Awake()
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

  public void CreateBullet(Vector2 startPosition, Vector2 targetPosition, float bulletSpeed)
  {
    // 총알 생성
    GameObject bullet = Instantiate(bulletPrefab, startPosition, Quaternion.identity);

    // 총알 초기화
    Bullet bulletScript = bullet.GetComponent<Bullet>();
    if (bulletScript != null)
    {
      bulletScript.Initialize(startPosition, targetPosition, bulletSpeed);
    }
    else
    {
      Debug.LogError("Bullet script not found on bulletPrefab!");
    }
  }
}
