using System.Collections;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private Player currentPlayer;

    [Header("# Game Control")]
    public bool isLive;
    public float gameTime;
    public int targetFrameRate;
    public string version = "1.0.0";
    public int latency = 33;

    [Header("# Player Info")]
    public uint playerId;
    public string deviceId;

    [Header("# Game Object")]
    public PoolManager pool;
    public Player player;
    public GameObject hud;
    public GameObject GameStartUI;
    public GameObject gate;

    void Awake()
    {
        instance = this;
        Application.targetFrameRate = targetFrameRate;
        playerId = (uint)Random.Range(0, 4);

    }

    public void GameStart()
    {
        player.deviceId = deviceId;
        player.gameObject.SetActive(true);
        hud.SetActive(true);
        GameStartUI.SetActive(false);
        isLive = true;

        GateManager gateManager = FindObjectOfType<GateManager>();
        if (gateManager != null)
        {
            gateManager.CreateGates();
        }

        AudioManager.instance.PlayBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }

    public void GameOver()
    {
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        isLive = false;
        yield return new WaitForSeconds(0.5f);

        AudioManager.instance.PlayBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose);
    }

    public void GameRetry()
    {
        SceneManager.LoadScene(0);
    }

    public void GameQuit()
    {
        try
        {
            // NetworkManager를 통해 종료 패킷 보내기
            if (NetworkManager.instance != null && NetworkManager.instance.TcpClient.Connected)
            {
                NetworkManager.instance.SendDisconnectPacket();
            }
        }
        catch (SocketException ex)
        {
            Debug.LogError($"소켓 오류: {ex.Message}");
        }
        finally
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void Update()
    {
        if (!isLive)
        {
            return;
        }
        gameTime += Time.deltaTime;
    }

    public Player GetPlayer()
    {
        if (currentPlayer == null)
        {
            currentPlayer = FindObjectOfType<Player>();
        }
        return currentPlayer;
    }
}
