using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class VisualBonusEnhancement : MonoBehaviour
{
    private readonly Vector3[] route =
    {
        new Vector3(-18f, 0.035f, 70f),
        new Vector3(-12f, 0.035f, 70f),
        new Vector3(-12f, 0.035f, 78f),
        new Vector3(-2f, 0.035f, 78f),
        new Vector3(5f, 0.035f, 77f)
    };
    private GameObject dynamicObstacle;
    private Vector3 obstacleStart;
    private Vector3 obstacleEnd;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name == "FireRescue_Main")
            new GameObject("Visual Bonus Enhancement").AddComponent<VisualBonusEnhancement>();
    }

    private void Start()
    {
        CreateRouteLine();
        CreateTrainingProps();
        CreateDynamicObstacle();
    }

    private void Update()
    {
        if (dynamicObstacle == null) return;
        float progress = (Mathf.Sin(Time.time * 0.75f) + 1f) * 0.5f;
        dynamicObstacle.transform.position = Vector3.Lerp(obstacleStart, obstacleEnd, progress);
    }

    private void CreateRouteLine()
    {
        GameObject lineObject = new GameObject("Visual Bonus Ground Route");
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = route.Length;
        line.SetPositions(route);
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.material = MaterialFor(new Color(1f, 0.72f, 0.05f));
        line.textureMode = LineTextureMode.Tile;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
    }

    private void CreateTrainingProps()
    {
        Material orange = MaterialFor(new Color(0.95f, 0.3f, 0.03f));
        Material cyan = MaterialFor(new Color(0.04f, 0.65f, 0.72f));
        CreateProp("Visual Bonus Safety Cone A", new Vector3(-15f, 0.45f, 70f), new Vector3(0.55f, 0.9f, 0.55f), orange);
        CreateProp("Visual Bonus Safety Cone B", new Vector3(-9f, 0.45f, 78f), new Vector3(0.55f, 0.9f, 0.55f), orange);
        CreateProp("Visual Bonus Checkpoint", new Vector3(2f, 0.15f, 78f), new Vector3(1.5f, 0.3f, 0.3f), cyan);
    }

    private void CreateDynamicObstacle()
    {
        dynamicObstacle = CreateProp("Visual Bonus Dynamic Obstacle", new Vector3(-15f, 0.6f, 75f), new Vector3(1.2f, 1.2f, 1.2f), MaterialFor(new Color(0.95f, 0.12f, 0.04f)));
        obstacleStart = new Vector3(-15f, 0.6f, 75f);
        obstacleEnd = new Vector3(-8f, 0.6f, 75f);
    }

    private static GameObject CreateProp(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = name;
        prop.transform.position = position;
        prop.transform.localScale = scale;
        prop.GetComponent<Renderer>().material = material;
        return prop;
    }

    private static Material MaterialFor(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        return material;
    }
}
