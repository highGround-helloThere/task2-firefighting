using UnityEngine;

[CreateAssetMenu(menuName = "Fire Rescue/Integration/Network Config", fileName = "FireRescueNetworkConfig")]
public sealed class FireRescueNetworkConfig : ScriptableObject
{
    [Header("设备地址")]
    public string robotIp = "192.168.137.121";
    public string orangePiIp = "192.168.137.121";
    [Header("固定协议端口")]
    public int robotControlPort = 5075;
    public int videoPort = 8080;
    public int perceptionPort = 6101;
}
