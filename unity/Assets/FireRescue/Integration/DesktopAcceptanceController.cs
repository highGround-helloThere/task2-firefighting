using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public sealed class DesktopAcceptanceController : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float turnSpeed = 100f;
    private RobotSyncManager robot;
    private bool emergency;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startCameraPosition;
    private Vector3 previousPosition;
    private Quaternion previousRotation;

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
        Camera camera = Camera.main;
        startCameraPosition = camera != null ? camera.transform.position : startPosition;
        previousPosition = transform.position;
        previousRotation = transform.rotation;
    }

    private IEnumerator Start()
    {
        // XR tracking applies the headset's room-space offset during the first frame.
        // Move the rig back by that horizontal offset so the authored camera position
        // remains at the maze entrance on every launch.
        yield return new WaitForEndOfFrame();

        Camera camera = Camera.main;
        if (camera != null && camera.transform.IsChildOf(transform))
        {
            Vector3 correction = startCameraPosition - camera.transform.position;
            correction.y = 0f;
            transform.position += correction;

            startPosition = transform.position;
            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }
    }

    private void LateUpdate()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) emergency = true;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) emergency = false;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            emergency = false;
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
        if (emergency)
        {
            RememberTransform();
            return;
        }

        Vector2 input = robot != null ? robot.CurrentManualInput : ReadKeyboardInput(keyboard);
        bool movedByXri = (transform.position - previousPosition).sqrMagnitude > 0.0000001f;
        bool turnedByXri = Quaternion.Angle(transform.rotation, previousRotation) > 0.001f;

        // XRI locomotion gets first chance. If it did not move the rig this frame,
        // apply the shared TonyPi command locally as a reliable fallback.
        if (!turnedByXri)
            transform.Rotate(0f, input.x * turnSpeed * Time.deltaTime, 0f);

        if (!movedByXri)
        {
            Vector3 motion = transform.forward * input.y * moveSpeed * Time.deltaTime;
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.Move(motion);
            else transform.position += motion;
        }

        RememberTransform();
    }

    private static Vector2 ReadKeyboardInput(Keyboard keyboard)
    {
        if (keyboard == null) return Vector2.zero;
        float forward = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        float turn = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        return new Vector2(turn, forward);
    }

    private void RememberTransform()
    {
        previousPosition = transform.position;
        previousRotation = transform.rotation;
    }
}
