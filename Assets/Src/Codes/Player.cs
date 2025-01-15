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
    public int hp = 100;
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
    private int type;

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

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        onCollision = true;

        // GameObject otherObject = collision.gameObject;
        // if (otherObject.CompareTag("Player"))
        // {
        //     type = 0;
        // }
        // if (otherObject.CompareTag("Enemy"))
        // {
        //     hp -= 10; // 적과 충돌 시 체력 10만큼 감소
        //     type = 1;
        // }
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
        Debug.Log($"서버로부터 받은 좌표: x={x}, y={y}");

        targetPosition = new Vector2(x, y);
        isTargetPositionSet = true;
    }
}
