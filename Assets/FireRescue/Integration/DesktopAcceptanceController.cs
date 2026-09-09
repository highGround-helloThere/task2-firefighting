using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class DesktopAcceptanceController : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float turnSpeed = 100f;
    public bool onlyWhenRobotDisconnected = true;
    private RobotSyncManager robot;
    private bool emergency;
    private Vector3 startPosition;
    private Quaternion startRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "FireRescue_Main") return;
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
        {
            var camera = Camera.main;
            if (camera != null) xrOrigin = camera.transform.root.gameObject;
        }
        if (xrOrigin != null && xrOrigin.GetComponent<DesktopAcceptanceController>() == null)
            xrOrigin.AddComponent<DesktopAcceptanceController>();
    }

    private void Awake()
    {
        robot = FindFirstObjectByType<RobotSyncManager>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) emergency = true;
        if (Keyboard.current.enterKey.wasPressedThisFrame) emergency = false;
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            emergency = false;
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
        if (emergency) return;
        if (onlyWhenRobotDisconnected && robot != null && robot.IsConnected) return;

        float forward = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
        float turn = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
        Vector3 motion = transform.forward * forward * moveSpeed * Time.deltaTime;
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null) controller.Move(motion);
        else transform.position += motion;
    }
}
