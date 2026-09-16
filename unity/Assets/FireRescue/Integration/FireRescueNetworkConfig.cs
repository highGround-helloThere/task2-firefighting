using UnityEngine;

[CreateAssetMenu(menuName = "Fire Rescue/Integration/Network Config", fileName = "FireRescueNetworkConfig")]
public sealed class FireRescueNetworkConfig : ScriptableObject
{
    [Header("设备地址")]
    public string robotIp = "127.0.0.1";
    public string orangePiIp = "127.0.0.1";
    [Header("固定协议端口")]
    public int robotControlPort = 5075;
    public int videoPort = 8080;
    public int perceptionPort = 6101;
}
