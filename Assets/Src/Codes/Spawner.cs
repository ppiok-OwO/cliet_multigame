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
}