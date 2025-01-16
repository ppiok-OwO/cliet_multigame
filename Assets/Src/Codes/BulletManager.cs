using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public static BulletManager instance; // 싱글톤 패턴
    public GameObject bulletPrefab;      // 총알 프리팹

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

    public void CreateBullet(float x0, float y0, float x1, float y1, float bulletSpeed)
    {
        // 총알 시작 위치와 목표 위치
        Vector2 startPosition = new Vector2(x0, y0);
        Vector2 targetPosition = new Vector2(x1, y1);

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
