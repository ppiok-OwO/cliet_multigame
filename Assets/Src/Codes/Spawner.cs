using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Spawner : MonoBehaviour
{
    public static Spawner instance;
    public GameObject[] monsterPrefabs; // 소환할 몬스터 프리팹 배열

    private HashSet<string> currentUsers = new HashSet<string>();
    private HashSet<string> currentMonsters = new HashSet<string>();

    void Awake()
    {
        instance = this;
    }

    public void Spawn(LocationUpdate data)
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        HashSet<string> newUsers = new HashSet<string>();
        HashSet<string> newMonsters = new HashSet<string>();

        string currentDeviceId = GameManager.instance.deviceId; // 현재 클라이언트의 deviceId 가져오기

        foreach (LocationUpdate.UserLocation user in data.users)
        {
            if (user.id == currentDeviceId)
            {
                // 자신의 캐릭터는 제외
                continue;
            }
            newUsers.Add(user.id);

            GameObject player = GameManager.instance.pool.Get(user);
            PlayerPrefab playerScript = player.GetComponent<PlayerPrefab>();
            playerScript.UpdatePosition(user.x, user.y);
        }

        foreach (LocationUpdate.MonsterLocation monster in data.monsters)
        {
            newMonsters.Add(monster.id);

            // 게이트 ID를 기준으로 GateController 찾기
            GateController gateController = FindGateById(monster.gateId);

            GameObject monsterPrefab = monsterPrefabs[monster.index];
            Vector3 spawnPosition = new Vector3(monster.x, monster.y, 0); // 받은 위치 사용

            GameObject monsterObject = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);

            // 몬스터 속성 초기화
            MonsterController monsterController = monsterObject.GetComponent<MonsterController>();
            if (monsterController != null)
            {
                monsterController.Initialize(monster.hp, monster.dmg);
            }

            Debug.Log($"몬스터 생성 완료: 위치({monster.x}, {monster.y}), HP: {monster.hp}, DMG: {monster.dmg}");


            // if (gateController != null)
            // {
            //     gateController.SpawnWaves(monster.index, monster.x, monster.y, monster.hp, monster.dmg);
            // }
        }

        foreach (string userId in currentUsers)
        {
            if (!newUsers.Contains(userId))
            {
                GameManager.instance.pool.Remove(userId);
            }
        }

        foreach (string monsterId in currentMonsters)
        {
            if (!newMonsters.Contains(monsterId))
            {
                GameManager.instance.pool.Remove(monsterId);
            }
        }

        currentUsers = newUsers;
        currentMonsters = newMonsters;
    }

    public void SpawnMonsters(UpdateMonster data)
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        foreach (UpdateMonster.MonsterLocation monster in data.monsters)
        {
            // 게이트 ID를 기준으로 GateController 찾기
            GateController gateController = FindGateById(monster.gateId);
            if (gateController != null)
            {
                gateController.SpawnWaves(monster.index, monster.x, monster.y, monster.hp, monster.dmg);
            }
        }
    }

    // Gate ID를 기반으로 GateController를 찾는 헬퍼 메서드
    private GateController FindGateById(int gateId)
    {
        GateController[] gates = FindObjectsOfType<GateController>();
        foreach (GateController gate in gates)
        {
            if (gate.gateId == gateId)
            {
                return gate;
            }
        }
        return null;
    }

    public Transform GetClosestPlayer(Vector3 position) // 몬스터의 좌표를 인자로 받는다.
    {
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (string userId in currentUsers)
        {
            GameObject player = GameManager.instance.pool.GetUserObject(userId);
            if (player == null)
            {
                continue;
            }

            float distance = Vector3.Distance(player.transform.position, position); // transform.position은 x, y, z를 필드로 가지는 구조체
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }

        return closestPlayer;
    }
}