using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector2 direction; // 총알의 이동 방향
    private float speed;       // 총알의 속도

    // 총알 초기화 메서드
    public void Initialize(Vector2 startPosition, Vector2 targetPosition, float bulletSpeed)
    {
        // 위치 설정
        transform.position = startPosition;

        // 방향 벡터 계산
        direction = (targetPosition - startPosition).normalized;

        // 속도 설정
        speed = bulletSpeed;
    }

    private void Update()
    {
        // 매 프레임마다 총알 이동
        transform.Translate(direction * speed * Time.deltaTime);

        // 화면 밖으로 나가거나 오래된 총알 제거
        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 몬스터와 충돌한 경우
        if (collision.CompareTag("Enemy"))
        {
            MonsterController monster = collision.GetComponent<MonsterController>();
            if (monster != null)
            {
                // 서버로 몬스터 데미지 처리 요청
                Debug.Log($"Hit monster {monster.id}");
                NetworkManager.instance.SendAttackMonsterPacket(
                    monster.transform.position.x,
                    monster.transform.position.y,
                    monster.id
                );
            }

            // 총알 제거
            Destroy(gameObject);
        }
    }
}
