using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FireRescueTrainingScene : MonoBehaviour
{
    private GameObject player;
    private GameObject fire;
    private float extinguishProgress;
    private Vector3 obstacleStart = new Vector3(0f, 0.6f, 5f);
    private Vector3 obstacleEnd = new Vector3(4f, 0.6f, 5f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name == "FireRescue_Training")
            new GameObject("Visual Bonus Training Builder").AddComponent<FireRescueTrainingScene>();
    }

    private void Start()
    {
        BuildEnvironment();
        BuildPlayer();
    }

    private void Update()
    {
        float obstacleT = (Mathf.Sin(Time.time * 0.8f) + 1f) * 0.5f;
        GameObject obstacle = GameObject.Find("Training Dynamic Obstacle");
        if (obstacle != null) obstacle.transform.position = Vector3.Lerp(obstacleStart, obstacleEnd, obstacleT);

        if (fire != null && Input.GetKey(KeyCode.J))
        {
            extinguishProgress = Mathf.Clamp01(extinguishProgress + Time.deltaTime / 3f);
            fire.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, extinguishProgress);
            Light fireLight = fire.GetComponentInChildren<Light>();
            if (fireLight != null) fireLight.intensity = Mathf.Lerp(4f, 0f, extinguishProgress);
        }
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(new Rect(24f, 24f, 620f, 32f), "VISUAL BONUS TRAINING  |  现场资产复用训练场");
        GUI.Label(new Rect(24f, 52f, 620f, 28f), "WASD 导航   J 持续灭火   O 检查动态障碍   Esc 急停");
        if (extinguishProgress >= 1f)
            GUI.Label(new Rect(Screen.width * 0.5f - 90f, 70f, 240f, 36f), "MISSION COMPLETE");
    }

    private void BuildEnvironment()
    {
        Material floor = MaterialFor(new Color(0.12f, 0.16f, 0.18f));
        Material wall = MaterialFor(new Color(0.28f, 0.31f, 0.32f));
        Material hazard = MaterialFor(new Color(0.95f, 0.45f, 0.04f));
        CreateBox("Training Floor", new Vector3(0f, -0.15f, 8f), new Vector3(18f, 0.3f, 24f), floor);
        CreateBox("North Wall", new Vector3(0f, 2f, 20f), new Vector3(18f, 4f, 0.4f), wall);
        CreateBox("West Wall", new Vector3(-9f, 2f, 8f), new Vector3(0.4f, 4f, 24f), wall);
        CreateBox("East Wall", new Vector3(9f, 2f, 8f), new Vector3(0.4f, 4f, 24f), wall);
        CreateBox("Branch Wall A", new Vector3(-4f, 2f, 7f), new Vector3(0.4f, 4f, 10f), wall);
        CreateBox("Branch Wall B", new Vector3(4f, 2f, 13f), new Vector3(0.4f, 4f, 10f), wall);
        CreateBox("Completion Gate", new Vector3(0f, 1.5f, 19f), new Vector3(5f, 3f, 0.25f), hazard);
        CreateBox("Training Dynamic Obstacle", obstacleStart, new Vector3(1.2f, 1.2f, 1.2f), hazard);
        fire = CreateFire(new Vector3(6.2f, 0.1f, 16f));

        for (int i = 0; i < 5; i++)
            CreateBox("Route Marker " + i, new Vector3(-7f + i * 3.5f, 0.02f, 2f + i * 3f), new Vector3(0.7f, 0.04f, 0.7f), hazard);

        GameObject lightObject = new GameObject("Training Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private void BuildPlayer()
    {
        foreach (Camera existingCamera in FindObjectsOfType<Camera>())
            existingCamera.enabled = false;

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Training Robot Avatar";
        player.transform.position = new Vector3(0f, 1f, 1f);
        player.GetComponent<Renderer>().material = MaterialFor(new Color(0.1f, 0.55f, 0.72f));
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        player.AddComponent<TrainingKeyboardController>();
        GameObject cameraObject = new GameObject("Training Camera");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
    }

    private GameObject CreateFire(Vector3 position)
    {
        GameObject root = new GameObject("Training Fire Source");
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = 0.8f;
        main.startSpeed = 1.3f;
        main.startSize = 0.65f;
        main.startColor = new Color(1f, 0.22f, 0.02f);
        var emission = particles.emission;
        emission.rateOverTime = 45f;
        Light light = new GameObject("Fire Light").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.2f, 0.03f);
        light.range = 7f;
        light.intensity = 4f;
        return root;
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().material = material;
        return box;
    }

    private static Material MaterialFor(Color color)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        return material;
    }
}

public sealed class TrainingKeyboardController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float turnSpeed = 100f;
    private CharacterController controller;

    private void Awake() { controller = GetComponent<CharacterController>(); }

    private void Update()
    {
        float forward = Input.GetAxisRaw("Vertical");
        float turn = Input.GetAxisRaw("Horizontal");
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
        Vector3 motion = transform.forward * forward * moveSpeed;
        motion.y = -1f;
        controller.Move(motion * Time.deltaTime);
    }
}
