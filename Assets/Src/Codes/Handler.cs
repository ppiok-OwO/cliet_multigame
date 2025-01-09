using System;
using UnityEngine;

public class Handler
{
    public static void InitialHandler(InitialResponse res) {
        try 
        {
            GameManager.instance.GameStart();
            GameManager.instance.player.UpdatePositionFromServer(res.x, res.y);
        } catch(Exception e)
        {
            Debug.LogError($"Error InitialHandelr: {e.Message}");
        }
    }

    public static void TargetLocationHandler(TargetLocationResponse res) {
        try
        {
            Console.WriteLine($"x : {res.x}, y: {res.y}");
            GameManager.instance.player.UpdatePositionFromServer(res.x, res.y);
        }
        catch (Exception e)
        {
            
            Debug.LogError($"Error TargetLocationHandelr: {e.Message}");
        }
        
    }
}