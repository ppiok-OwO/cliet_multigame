using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    public InputField ipInputField;
    public InputField portInputField;
    public InputField deviceIdInputField;
    public GameObject uiNotice;
    public TcpClient TcpClient { get; private set; }
    private bool _connected = false;
    public bool Connected => TcpClient.Connected && _connected;
    public NetworkStream Stream { get; private set; }

    WaitForSecondsRealtime wait;

    private byte[] receiveBuffer = new byte[4096];
    private List<byte> incompleteData = new List<byte>();
    private object monsterPosX;
    const string IP_KEY = "ip";
    const string PORT_KEY = "port";
    const string DEVICE_ID_KEY = "deviceId";
    void Awake()
    {
        instance = this;
        wait = new WaitForSecondsRealtime(5);
        ipInputField.text = PlayerPrefs.GetString(IP_KEY);
        portInputField.text = PlayerPrefs.GetString(PORT_KEY);
        deviceIdInputField.text = PlayerPrefs.GetString(DEVICE_ID_KEY);
    }
    public void OnStartButtonClicked()
    {
        string ip = ipInputField.text;
        string port = portInputField.text;
        string deviceId = deviceIdInputField.text;

        PlayerPrefs.SetString(IP_KEY, ip);
        PlayerPrefs.SetString(PORT_KEY, port);
        PlayerPrefs.SetString(DEVICE_ID_KEY, deviceId);

        if (IsValidPort(port))
        {
            int portNumber = int.Parse(port);

            if (deviceIdInputField.text != "")
            {
                GameManager.instance.deviceId = deviceIdInputField.text;
            }
            else
            {
                if (GameManager.instance.deviceId == "")
                {
                    GameManager.instance.deviceId = GenerateUniqueID();
                }
            }

            if (ConnectToServer(ip, portNumber))
            {
                StartGame();
            }
            else
            {
                AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
                StartCoroutine(NoticeRoutine(1));
            }

        }
        else
        {
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
            StartCoroutine(NoticeRoutine(0));
        }
    }

    bool IsValidIP(string ip)
    {
        // 간단한 IP 유효성 검사
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    bool IsValidPort(string port)
    {
        // 간단한 포트 유효성 검사 (0 - 65535)
        if (int.TryParse(port, out int portNumber))
        {
            return portNumber > 0 && portNumber <= 65535;
        }
        return false;
    }

    bool ConnectToServer(string ip, int port)
    {
        try
        {
            TcpClient = new TcpClient(ip, port);
            Stream = TcpClient.GetStream();
            _connected = true;
            Debug.Log($"Connected to {ip}:{port}");

            return true;
        }
        catch (SocketException e)
        {
            Debug.LogError($"SocketException: {e}");
            return false;
        }
    }

    string GenerateUniqueID()
    {
        return System.Guid.NewGuid().ToString();
    }

    void StartGame()
    {
        // 게임 시작 코드 작성
        Debug.Log("Game Started");
        StartReceiving(); // Start receiving data
        SendInitialPacket();
    }

    IEnumerator NoticeRoutine(int index)
    {

        uiNotice.SetActive(true);
        uiNotice.transform.GetChild(index).gameObject.SetActive(true);

        yield return wait;

        uiNotice.SetActive(false);
        uiNotice.transform.GetChild(index).gameObject.SetActive(false);
    }

    public static byte[] ToBigEndian(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return bytes;
    }

    byte[] CreatePacketHeader(int dataLength, Packets.PacketType packetType)
    {
        int packetLength = 4 + 1 + dataLength; // 전체 패킷 길이 (헤더 포함)
        byte[] header = new byte[5]; // 4바이트 길이 + 1바이트 타입

        // 첫 4바이트: 패킷 전체 길이
        byte[] lengthBytes = BitConverter.GetBytes(packetLength);
        lengthBytes = ToBigEndian(lengthBytes);
        Array.Copy(lengthBytes, 0, header, 0, 4);

        // 다음 1바이트: 패킷 타입
        header[4] = (byte)packetType;

        return header;
    }

    // 공통 패킷 생성 함수
    async void SendPacket<T>(T payload, uint handlerId)
    {
        if (!Connected) return;

        // ArrayBufferWriter<byte>를 사용하여 직렬화
        var payloadWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(payloadWriter, payload);
        byte[] payloadData = payloadWriter.WrittenSpan.ToArray();

        CommonPacket commonPacket = new CommonPacket
        {
            handlerId = handlerId,
            userId = GameManager.instance.deviceId,
            version = GameManager.instance.version,
            payload = payloadData,
        };

        // ArrayBufferWriter<byte>를 사용하여 직렬화
        var commonPacketWriter = new ArrayBufferWriter<byte>();
        Packets.Serialize(commonPacketWriter, commonPacket);
        byte[] data = commonPacketWriter.WrittenSpan.ToArray();

        // 헤더 생성
        byte[] header = CreatePacketHeader(data.Length, Packets.PacketType.Normal);

        // 패킷 생성
        byte[] packet = new byte[header.Length + data.Length];
        Array.Copy(header, 0, packet, 0, header.Length);
        Array.Copy(data, 0, packet, header.Length, data.Length);

        await Task.Delay(GameManager.instance.latency);

        // 패킷 전송
        Stream.Write(packet, 0, packet.Length);
    }

    void SendInitialPacket()
    {
        InitialPayload initialPayload = new InitialPayload
        {
            deviceId = GameManager.instance.deviceId,
            playerId = GameManager.instance.playerId,
            latency = GameManager.instance.latency,
        };

        // handlerId는 0으로 가정
        SendPacket(initialPayload, (uint)Packets.HandlerIds.Init);
    }

    public void SendLocationUpdatePacket(float x, float y)
    {
        // Debug.Log($"플레이어 좌표: {x}, {y}");
        LocationUpdatePayload locationUpdatePayload = new LocationUpdatePayload
        {
            x = x,
            y = y,
        };

        SendPacket(locationUpdatePayload, (uint)Packets.HandlerIds.LocationUpdate);
    }

    // 위치 및 속도 데이터를 서버로 전송하는 함수
    public void SendPositionAndVelocityPacket(float velX, float velY)
    {
        PositionVelocityPayload payload = new PositionVelocityPayload
        {
            velocityX = velX,
            velocityY = velY,
        };

        // 핸들러 ID는 새로 정의해야 함 (Packets.HandlerIds.PositionVelocity)
        SendPacket(payload, (uint)Packets.HandlerIds.PositionVelocity);
    }

    public void SendDisconnectPacket()
    {
        if (!Connected) return;

        try
        {
            Disconnect disconnect = new Disconnect();
            SendPacket(disconnect, (uint)Packets.HandlerIds.Disconnect);
            Debug.Log("Disconnect 패킷 전송 완료");

            System.Threading.Thread.Sleep(100); // 연결 종료 패킷을 보낸 뒤 100ms 대기
            _connected = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Disconnect 패킷 전송 실패: {ex.Message}");
        }
    }

    public void SendOnCollisionPacket(float x0, float y0, float x1, float y1)
    {
        // 내 좌표와 충돌한 오브젝트의 좌표를 보내기
        try
        {
            OnCollision payload = new OnCollision
            {
                x0 = x0,
                y0 = y0,
                x1 = x1,
                y1 = y1
            };

            SendPacket(payload, (uint)Packets.HandlerIds.OnCollision);
        }
        catch (Exception ex)
        {

            Debug.LogError($"OnCollision 패킷 전송 실패: {ex.Message}");
        }
    }

    public void SendCreateMonsterPacket(List<CreateMonsterList.CreateMonster> monsters)
    {
        try
        {
            CreateMonsterList payload = new CreateMonsterList
            {
                monsters = monsters
            };
            // Debug.Log($"monsterHp: {monsterHp}, monsterDmg: {monsterDmg}");

            SendPacket(payload, (uint)Packets.HandlerIds.CreateMonster);
        }
        catch (Exception ex)
        {
            Debug.LogError($"CreateMonter 패킷 전송 실패: {ex.Message}");
        }
    }

    public void SendAttackMonsterPacket(float monsterX, float monsterY, string monsterId)
    {
        try
        {
            AttackMonster payload = new AttackMonster
            {
                monsterX = monsterX,
                monsterY = monsterY,
                monsterId = monsterId,
            };

            SendPacket(payload, (uint)Packets.HandlerIds.AttackMonster);
        }
        catch (Exception ex)
        {
            Debug.LogError($"AttackMonster 패킷 전송 실패: {ex.Message}");
        }
    }

    void StartReceiving()
    {
        _ = ReceivePacketsAsync();
    }

    async System.Threading.Tasks.Task ReceivePacketsAsync()
    {
        while (TcpClient.Connected)
        {
            try
            {
                int bytesRead = await Stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                if (bytesRead > 0)
                {
                    ProcessReceivedData(receiveBuffer, bytesRead);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Receive error: {e.Message}");
                break;
            }
        }
    }

    void ProcessReceivedData(byte[] data, int length)
    {
        incompleteData.AddRange(data.AsSpan(0, length).ToArray());

        while (incompleteData.Count >= 5)
        {
            // 패킷 길이와 타입 읽기
            byte[] lengthBytes = incompleteData.GetRange(0, 4).ToArray();
            int packetLength = BitConverter.ToInt32(ToBigEndian(lengthBytes), 0); // 패킷 길이
            Packets.PacketType packetType = (Packets.PacketType)incompleteData[4]; // 패킷 타입

            if (incompleteData.Count < packetLength)
            {
                // 데이터가 충분하지 않으면 반환
                return;
            }

            // 패킷 데이터 추출
            byte[] packetData = incompleteData.GetRange(5, packetLength - 5).ToArray();
            incompleteData.RemoveRange(0, packetLength);

            // Debug.Log($"Received packet: Length = {packetLength}, Type = {packetType}");

            switch (packetType)
            {
                case Packets.PacketType.Ping:
                    HandlePingPacket(packetData);
                    break;
                case Packets.PacketType.Normal:
                    HandleNormalPacket(packetData);
                    break;
                case Packets.PacketType.Broadcast:
                    HandleLocationPacket(packetData);
                    break;
                case Packets.PacketType.Location:
                    // HandleLocationPacket(packetData);
                    HandleTargetLocationPacket(packetData);
                    break;
                case Packets.PacketType.OnCollision:
                    HandleOncollisionPacket(packetData);
                    break;
                case Packets.PacketType.Init:
                    HandleInitPacket(packetData);
                    break;
                case Packets.PacketType.CreateMonster:
                    HandleUpdateMonsterPacket(packetData);
                    break;
                case Packets.PacketType.MonsterMove:
                    HandleMonsterMovePacket(packetData);
                    break;
                case Packets.PacketType.Attack:
                    HandleAttackMonsterPacket(packetData);
                    break;
            }
        }
    }

    void HandleNormalPacket(byte[] packetData)
    {
        // 패킷 데이터 처리
        var response = Packets.Deserialize<Response>(packetData);

        Debug.Log($"Received Response - HandlerId: {response.handlerId}, ResponseCode: {response.responseCode}, Timestamp: {response.timestamp}");

        if (response.responseCode != 0 && !uiNotice.activeSelf)
        {
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
            StartCoroutine(NoticeRoutine(2));
            return;
        }

        if (response.data != null && response.data.Length > 0)
        {

            switch ((Packets.HandlerIds)response.handlerId)
            {
                case Packets.HandlerIds.Init:
                    {
                        Handler.InitialHandler(Packets.ParsePayload<InitialResponse>(response.data));

                        break;
                    }

                default:
                    Debug.LogWarning($"Unhandled handlerId: {response.handlerId}");
                    break;
            }
            ProcessResponseData(response.data);
        }
    }

    void ProcessResponseData(byte[] data)
    {
        try
        {
            // var specificData = Packets.Deserialize<SpecificDataType>(data);
            string jsonString = Encoding.UTF8.GetString(data);
            Debug.Log($"Processed SpecificDataType: {jsonString}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing response data: {e.Message}");
        }
    }

    void HandleInitPacket(byte[] data)
    {
        try
        {
            InitialResponse response;

            if (data.Length > 0)
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<InitialResponse>(data);
                Handler.InitialHandler(response);
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleInitPacket: {e.Message}");
        }
    }

    void HandleLocationPacket(byte[] data)
    {
        try
        {
            LocationUpdate response;

            if (data.Length > 0)
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<LocationUpdate>(data);
            }
            else
            {
                // data가 비어있을 경우 빈 배열을 전달
                response = new LocationUpdate { users = new List<LocationUpdate.UserLocation>() };
            }

            if (Spawner.instance)
            {
                Spawner.instance.Spawn(response);
            }
            else
            {
                Debug.Log("뭔가 없음");
            }


        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleLocationPacket: {e.Message}");
        }
    }

    void HandleTargetLocationPacket(byte[] data)
    {
        try
        {
            TargetLocationResponse response;

            if (data.Length > 0)
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<TargetLocationResponse>(data);
                Handler.TargetLocationHandler(response);
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleTargetLocationPacket: {e.Message}");
        }
    }

    void HandleOncollisionPacket(byte[] data)
    {
        try
        {
            OnCollision response;

            if (data.Length > 0)
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<OnCollision>(data);
                Handler.OnCollisionHandler(response);
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleOncollisionPacket: {e.Message}");
        }
    }

    void HandleUpdateMonsterPacket(byte[] data)
    {
        try
        {
            CreateMonsterList response;

            if (data.Length > 0)
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<CreateMonsterList>(data);
            }
            else
            {
                // data가 비어있을 경우 빈 배열을 전달
                response = new CreateMonsterList { monsters = new List<CreateMonsterList.CreateMonster>() };
            }

            Spawner.instance.SpawnMonsters(response);

        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleUpdateMonsterPacket: {e.Message}");
        }
    }

    async void HandlePingPacket(byte[] data)
    {
        // 타임스탬프
        // 바로 돌려줄 예정

        // 헤더 생성
        byte[] header = CreatePacketHeader(data.Length, Packets.PacketType.Ping);

        // 패킷 생성
        byte[] packet = new byte[header.Length + data.Length];
        Array.Copy(header, 0, packet, 0, header.Length);
        Array.Copy(data, 0, packet, header.Length, data.Length);

        await Task.Delay(GameManager.instance.latency);

        // 패킷 전송
        Stream.Write(packet, 0, packet.Length);
    }

    void HandleMonsterMovePacket(byte[] data)
    {
        try
        {
            MonsterMove response;

            if (data.Length > 0)
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<MonsterMove>(data);

                if (MonsterController.instance)
                {
                    MonsterController.instance.UpdateMonsterPosition(response);

                }
            }
            else
            {
                // data가 비어있을 경우 빈 배열을 전달
                response = new MonsterMove { monsterLocations = new List<MonsterMove.MonstersNextLocation>() };
                // Debug.LogWarning("서버로부터 빈 몬스터 데이터 수신");
            }


        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleMonsterMovePacket: {e.Message}");
        }
    }

    void HandleAttackMonsterPacket(byte[] data)
    {
        try
        {
            AttackResult response;

            if (data.Length > 0)
            {
                // 패킷 데이터 처리
                response = Packets.Deserialize<AttackResult>(data);

                // 현재 플레이어 객체 가져오기
                Player player = GameManager.instance.GetPlayer(); // GameManager에서 현재 플레이어 객체를 가져오는 메서드
                if (player == null)
                {
                    Debug.LogWarning("Player instance not found. Cannot create bullet.");
                    return;
                }

                // 몬스터 객체 가져오기
                MonsterController targetMonster = MonsterManager.instance.FindMonsterById(response);
                if (targetMonster == null)
                {
                    Debug.LogWarning($"Monster with ID {response.monsterId} not found. Cannot create bullet.");
                    return;
                }

                // 총알 생성 (플레이어 위치 -> 몬스터 위치)
                BulletManager.instance.CreateBullet(
                    player.transform.position, // 시작 위치: 플레이어의 현재 위치
                    targetMonster.transform.position, // 목표 위치: 몬스터의 현재 위치
                    response.bulletSpeed // 서버에서 제공한 총알 속도
                );


                if (targetMonster != null)
                {
                    targetMonster.TakeDamage(response);
                }
                else
                {
                    Debug.LogWarning($"Monster with ID {response.monsterId} not found.");
                }
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Error HandleAttackMonsterPacket: {e.Message}");
        }
    }

    // 클라는 강제종료도 마음대로 못하게 하자
    void OnApplicationQuit()
    {
        SendDisconnectPacket();
    }
}

