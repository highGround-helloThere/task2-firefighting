using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public sealed class VisualBonusEnhancement : MonoBehaviour
{
    private Vector3[] route;
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
        route = BuildRouteFromScene();
        if (route != null && route.Length > 1)
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
        Material routeMaterial = MaterialFor(new Color(1f, 0.72f, 0.05f));
        if (routeMaterial != null) line.material = routeMaterial;
        line.textureMode = LineTextureMode.Tile;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
    }

    private static Vector3[] BuildRouteFromScene()
    {
        LightControl fireControl = FindFirstObjectByType<LightControl>();
        Vector3 start = fireControl != null && fireControl.robotRoot != null
            ? fireControl.robotRoot.position
            : new Vector3(-21.67f, 0f, 69.32f);
        Transform targetAnchor = FindNearestValidAnchor(fireControl, start);
        Vector3 target = targetAnchor != null ? targetAnchor.position : start + Vector3.forward * 8f;
        Vector3 approach = Vector3.Lerp(target, start, Mathf.Clamp01(2.5f / Mathf.Max(0.1f, Vector3.Distance(start, target))));

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(start, approach, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
            return ProjectToGround(path.corners);

        Vector3[] candidates = BuildFallbackCandidates(start, approach);
        for (int i = 0; i < candidates.Length; i += 4)
        {
            var candidate = new[] { candidates[i], candidates[i + 1], candidates[i + 2], candidates[i + 3] };
            if (IsClearRoute(candidate))
                return ProjectToGround(candidate);
        }

        Debug.LogWarning("[VisualBonus] 未找到无墙体穿越的路线，已隐藏路线指引。请在场景中烘焙 NavMesh 后重试。", this);
        return null;
    }

    private static Transform FindNearestValidAnchor(LightControl fireControl, Vector3 origin)
    {
        if (fireControl == null || fireControl.fireAnchors == null) return null;
        Transform best = null;
        float bestDistance = float.MaxValue;
        foreach (Transform anchor in fireControl.fireAnchors)
        {
            if (anchor == null) continue;
            float distance = Vector3.SqrMagnitude(anchor.position - origin);
            if (distance < bestDistance) { bestDistance = distance; best = anchor; }
        }
        return best;
    }

    private static Vector3[] BuildFallbackCandidates(Vector3 start, Vector3 target)
    {
        Vector3 side = Vector3.Cross(Vector3.up, target - start).normalized;
        Vector3 midpoint = Vector3.Lerp(start, target, 0.5f);
        var result = new Vector3[20];
        int index = 0;
        foreach (float offset in new[] { 0f, 2f, -2f, 4f, -4f })
        {
            Vector3 bend = midpoint + side * offset;
            result[index++] = start;
            result[index++] = Vector3.Lerp(start, bend, 0.5f);
            result[index++] = Vector3.Lerp(bend, target, 0.5f);
            result[index++] = target;
        }
        return result;
    }

    private static bool IsClearRoute(Vector3[] points)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 direction = points[i + 1] - points[i];
            float length = direction.magnitude;
            if (length < 0.2f) continue;
            direction /= length;
            Vector3 from = points[i] + direction * 0.4f + Vector3.up * 0.65f;
            Vector3 to = points[i + 1] - direction * 0.4f + Vector3.up * 0.65f;
            if (Physics.Linecast(from, to, out _, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
        }
        return true;
    }

    private static Vector3[] ProjectToGround(Vector3[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(points[i] + Vector3.up * 20f, Vector3.down, out hit, 40f))
                points[i].y = hit.point.y + 0.04f;
            else
                points[i].y += 0.04f;
        }
        return points;
    }

    private void CreateTrainingProps()
    {
        Material orange = MaterialFor(new Color(0.95f, 0.3f, 0.03f));
        Material cyan = MaterialFor(new Color(0.04f, 0.65f, 0.72f));
        if (route == null || route.Length < 2) return;
        Vector3 first = route[Mathf.Min(1, route.Length - 1)];
        Vector3 second = route[Mathf.Max(1, route.Length - 2)];
        CreateProp("Visual Bonus Safety Cone A", first + Vector3.up * 0.45f, new Vector3(0.55f, 0.9f, 0.55f), orange);
        CreateProp("Visual Bonus Safety Cone B", second + Vector3.up * 0.45f, new Vector3(0.55f, 0.9f, 0.55f), orange);
        CreateProp("Visual Bonus Checkpoint", route[route.Length - 1] + Vector3.up * 0.15f, new Vector3(1.5f, 0.3f, 0.3f), cyan);
    }

    private void CreateDynamicObstacle()
    {
        if (route == null || route.Length < 3) return;
        obstacleStart = route[1] + Vector3.up * 0.6f;
        obstacleEnd = route[route.Length - 2] + Vector3.up * 0.6f;
        dynamicObstacle = CreateProp("Visual Bonus Dynamic Obstacle", obstacleStart, new Vector3(1.2f, 1.2f, 1.2f), MaterialFor(new Color(0.95f, 0.12f, 0.04f)));
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
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;
        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
