using System;
using UnityEngine;

public class Handler
{
    public static void InitialHandler(InitialResponse res)
    {
        try
        {
            GameManager.instance.GameStart();
            GameManager.instance.player.UpdatePositionFromServer(res.x, res.y);
            Debug.Log($"init handler: {res}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error InitialHandelr: {e.Message}");
        }
    }

    public static void TargetLocationHandler(TargetLocationResponse res)
    {
        try
        {
            // Debug.Log($"추측항법 !! x : {res.x}, y: {res.y}");
            GameManager.instance.player.UpdatePositionFromServer(res.x, res.y);
        }
        catch (Exception e)
        {

            Debug.LogError($"Error TargetLocationHandelr: {e.Message}");
        }

    }

    // 부딪힌 물체들이 일정 거리(1.5픽셀 정도) 뒤로 밀려난다.
    public static void OnCollisionHandler(OnCollisionResponse res)
    {
        try
        {
            Debug.Log($"퉁퉁퉁퉁 !! x : {res.x0}, y: {res.y0}");

            GameManager.instance.player.UpdatePositionFromServer(res.x0, res.y0);
        }
        catch (Exception e)
        {

            Debug.LogError($"Error TargetLocationHandelr: {e.Message}");
        }
    }
}