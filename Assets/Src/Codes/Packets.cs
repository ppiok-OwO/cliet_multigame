using UnityEngine;
using ProtoBuf;
using System.IO;
using System.Buffers;
using System.Collections.Generic;
using System;
using System.Text;

public class Packets
{
    public enum PacketType { Ping, Normal, Location = 3 }
    public enum HandlerIds {
        Init = 0,
        LocationUpdate = 2 ,
        PositionVelocity = 3
    }

    public static void Serialize<T>(IBufferWriter<byte> writer, T data)
    {
        Serializer.Serialize(writer, data);
    }

    public static T Deserialize<T>(byte[] data) {
        try {
            using (var stream = new MemoryStream(data)) {
                return ProtoBuf.Serializer.Deserialize<T>(stream);
            }
        } catch (Exception ex) {
            Debug.LogError($"Deserialize: Failed to deserialize data. Exception: {ex}");
            throw;
        }
    }

    private static T DeserializeJson<T>(string jsonString)
    {
        return JsonUtility.FromJson<T>(jsonString);
    }

    public static T ParsePayload<T>(byte[] data)
    {
        // 서버로부터 수신한 바이트 배열 (예시 데이터)
        // 1. 바이트 배열을 UTF-8 문자열로 변환
        string jsonString = Encoding.UTF8.GetString(data);

        // InitialResponse로 디시리얼라이즈
        T response = DeserializeJson<T>(jsonString);
        return response;
    }
}

[ProtoContract]
public class InitialPayload
{
    [ProtoMember(1, IsRequired = true)]
    public string deviceId { get; set; }

    [ProtoMember(2, IsRequired = true)]
    public uint playerId { get; set; }
    
    [ProtoMember(3, IsRequired = true)]
    public float latency { get; set; }
}

[ProtoContract]
public class CommonPacket
{
    [ProtoMember(1)]
    public uint handlerId { get; set; }

    [ProtoMember(2)]
    public string userId { get; set; }

    [ProtoMember(3)]
    public string version { get; set; }

    [ProtoMember(4)]
    public byte[] payload { get; set; }
}

[ProtoContract]
public class LocationUpdatePayload {
    [ProtoMember(1, IsRequired = true)]
    public float x { get; set; }
    [ProtoMember(2, IsRequired = true)]
    public float y { get; set; }
}

[ProtoContract]
public class LocationUpdate
{
    [ProtoMember(1)]
    public List<UserLocation> users { get; set; }

    [ProtoContract]
    public class UserLocation
    {
        [ProtoMember(1)]
        public string id { get; set; }

        [ProtoMember(2)]
        public uint playerId { get; set; }

        [ProtoMember(3)]
        public float x { get; set; }

        [ProtoMember(4)]
        public float y { get; set; }
    }
}

[ProtoContract]
public class PositionVelocityPayload
{
    [ProtoMember(1, IsRequired = true)]
    public float x { get; set; }

    [ProtoMember(2, IsRequired = true)]
    public float y { get; set; }

    [ProtoMember(3, IsRequired = true)]
    public float velocityX { get; set; }

    [ProtoMember(4, IsRequired = true)]
    public float velocityY { get; set; }
}

[ProtoContract]
public class Response {
    [ProtoMember(1)]
    public uint handlerId { get; set; }

    [ProtoMember(2)]
    public uint responseCode { get; set; }

    [ProtoMember(3)]
    public long timestamp { get; set; }

    [ProtoMember(4)]
    public byte[] data { get; set; }
}

public class InitialResponse
{
    public string userId;
    public float x;
    public float y;
}

public class TargetLocationResponse{
    public float x;
    public float y;
}