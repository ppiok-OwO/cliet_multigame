using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Spawner : MonoBehaviour
{
    public static Spawner instance;
    private HashSet<string> currentUsers = new HashSet<string>();

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

        foreach (string userId in currentUsers)
        {
            if (!newUsers.Contains(userId))
            {
                GameManager.instance.pool.Remove(userId);
            }
        }

        currentUsers = newUsers;
    }

    public void SpawnMonsters(CreateMonsterList data)
    {
        if (!GameManager.instance.isLive)
        {
            return;
        }

        foreach (CreateMonsterList.CreateMonster monster in data.monsters)
        {
            // 게이트 ID를 기준으로 GateController 찾기
            GateController gateController = FindGateById(monster.gateId);
            if (gateController != null)
            {
                gateController.SpawnWaves(monster.monsterId, monster.monsterIndex, monster.monsterPosX, monster.monsterPosY, monster.monsterHp, monster.monsterDmg);
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
}