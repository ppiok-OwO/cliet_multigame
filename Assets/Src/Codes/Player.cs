using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed;
    public float attackSpeed = 0.2f;
    public int range = 30;
    public bool onCollision = false;
    public string deviceId;
    public RuntimeAnimatorController[] animCon;

    Rigidbody2D rigid;
    SpriteRenderer spriter;
    Animator anim;
    TextMeshPro myText;

    private Vector2 targetPosition; // 서버로부터 받은 목표 위치를 저장할 변수
    private bool isTargetPositionSet = false; // 서버에서 목표 위치를 받은 상태인지
    private Vector2 lastPosition; // 이전 프레임의 위치를 저장
    private Vector2 velocity;    // 속도 계산용 벡터
    private Coroutine attackRoutine; // 공격 코루틴 상태 저장

    [SerializeField] private GameObject bulletPrefab;  // Inspector에서 총알 프리팹 연결


    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myText = GetComponentInChildren<TextMeshPro>();
    }

    void OnEnable()
    {
        if (deviceId.Length > 5)
        {
            myText.text = deviceId[..5];
        }
        else
        {
            myText.text = deviceId;
        }
        myText.GetComponent<MeshRenderer>().sortingOrder = 6;

        anim.runtimeAnimatorController = animCon[GameManager.instance.playerId];

        // 공격 주기 시작
        if (attackRoutine == null)
        {
            attackRoutine = StartCoroutine(AttackMonsterRoutine());
        }
    }

    void OnDisable()
    {
        // Coroutine 중단
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    // 2초에 한 번씩 몬스터를 공격하는 Coroutine
    private IEnumerator AttackMonsterRoutine()
    {
        while (true) // 게임이 실행되는 동안 반복
        {
            if (GameManager.instance.isLive)
            {
                AttackMonster(); // 몬스터 공격
            }
            yield return new WaitForSeconds(attackSpeed); // 공격 속도 간격
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.instance.isLive) return;

        if (isTargetPositionSet)
        {
            // 서버로부터 받은 위치로 이동
            rigid.MovePosition(targetPosition);
            isTargetPositionSet = false; // 목표 위치로 이동 후 상태 초기화
        }

        if (!onCollision)
        {
            // 입력 기반으로 위치 업데이트
            inputVec.x = Input.GetAxisRaw("Horizontal");
            inputVec.y = Input.GetAxisRaw("Vertical");

            // 입력 값이 있는 경우에만 패킷 전송
            if (inputVec != Vector2.zero)
            {
                // 추측항법을 위한 단위 벡터 전송
                NetworkManager.instance.SendPositionAndVelocityPacket(inputVec.x, inputVec.y);
            }
        }
        // 세션 내 모든 플레이어의 위치를 브로드캐스트하기 위해 좌표 전송
        NetworkManager.instance.SendLocationUpdatePacket(rigid.position.x, rigid.position.y);
    }



    void FixedUpdate()
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        if (isTargetPositionSet)
        {
            // 서버로부터 받은 위치로 이동
            rigid.MovePosition(targetPosition);
            isTargetPositionSet = false; // 목표 위치로 이동 후 상태 초기화
        }
    }

    // Update가 끝난이후 적용
    void LateUpdate()
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0)
        {
            spriter.flipX = inputVec.x < 0;
        }
    }

    void AttackMonster()
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        // 유저의 사정거리 안에 몬스터가 들어오면 가장 가까운 적에게 공격 시작
        // 플레이어의 위치 가져오기
        Vector2 playerPosition = new Vector2(rigid.position.x, rigid.position.y);

        // 가장 가까운 몬스터를 찾기 위한 변수 초기화
        MonsterController closestMonster = null;
        float closestDistance = float.MaxValue;

        // 활성화된 몬스터 리스트 가져오기
        List<MonsterController> monsters = MonsterManager.instance.GetActiveMonsters();

        // 모든 몬스터를 순회하여 가장 가까운 몬스터를 찾음
        foreach (var monster in monsters)
        {
            Vector2 monsterPosition = monster.transform.position;
            float distance = Vector2.Distance(playerPosition, monsterPosition);

            if (distance <= range && distance < closestDistance)
            {
                closestMonster = monster;
                closestDistance = distance;
            }
        }

        if (closestMonster == null)
        {
            Debug.Log("몬스터가 없다!");
            return; // 공격할 몬스터가 없음
        }

        Debug.Log($"몬스터 {closestMonster.id} 공격 중!");

        // 서버로 유저와 몬스터의 좌표, 해당 몬스터의 id 전송
        NetworkManager.instance.SendAttackMonsterPacket(closestMonster.transform.position.x, closestMonster.transform.position.x, closestMonster.id);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        onCollision = true;

        GameObject otherObject = collision.gameObject;
        // if (otherObject.CompareTag("Player"))
        // {
        //     type = 0;
        // }
        if (otherObject.CompareTag("Enemy"))
        {
            // 체력 감소 패킷 보내기
            
        }
        //if (otherObject.CompareTag("Item"))
        //{
        //    PlayerInventory.AddItem(otherObject);
        //    Destroy(otherObject); // 아이템 삭제
        //}

        if (collision.contacts.Length > 0)
        {
            // 첫 번째 충돌 지점
            Vector2 contactPoint = collision.contacts[0].point;

            // x, y 좌표 추출
            float contactX = contactPoint.x;
            float contactY = contactPoint.y;

            Debug.Log($"충돌 지점 x: {contactX}, y: {contactY}");
            // 서버로 충돌 패킷 보내기
            NetworkManager.instance.SendOnCollisionPacket(rigid.position.x, rigid.position.y, contactX, contactY);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // 충돌이 끝났을 때 상태 초기화
        onCollision = false;
    }

    public void UpdatePositionFromServer(float x, float y)
    {
        // Debug.Log($"서버로부터 받은 좌표: x={x}, y={y}");

        targetPosition = new Vector2(x, y);
        isTargetPositionSet = true;
    }
}
