//using System.Collections;
//using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;

//public class LightControl : MonoBehaviour
//{
//    [Header("�ƹ�����")]
//    public Light pointLight;                      // ��Ҫ���Ƶ� PointLight
//    public float blinkInterval = 1.0f;            // 1 ����˸һ��

//    [Header("ץȡ����")]
//    public XRGrabInteractable cubeGrab;           // �����е� Cube_grab��XRGrabInteractable��

//    [Header("RobotSync")]
//    public RobotSyncManager robotSyncManager;     // ���볡����� RobotSyncManager ʵ��������֪ͨץȡ��

//    private bool _isBlinking = false;
//    private Coroutine _blinkCoroutine;
//    private Color _originalColor;
//    public Color blinkColor = Color.red;

//    void Start()
//    {
//        if (pointLight != null)
//        {
//            _originalColor = pointLight.color;
//            pointLight.enabled = false;
//        }

//        // ���Ļ�������ɫ�źţ������̣߳�
//        if (robotSyncManager != null)
//        {
//            robotSyncManager.onColorSignalReceived.AddListener(OnColorSignalReceived);
//        }

//        // ����ץȡ�¼��������ץ��ʱֹͣ��˸��֪ͨ�����ˣ�
//        if (cubeGrab != null)
//        {
//            cubeGrab.onSelectEntered.AddListener(OnCubeGrabbed);
//            cubeGrab.onSelectExited.AddListener(OnCubeReleased);
//        }
//    }

//    void OnDestroy()
//    {
//        if (robotSyncManager != null)
//            robotSyncManager.onColorSignalReceived.RemoveListener(OnColorSignalReceived);

//        if (cubeGrab != null)
//        {
//            cubeGrab.onSelectEntered.RemoveListener(OnCubeGrabbed);
//            cubeGrab.onSelectExited.RemoveListener(OnCubeReleased);
//        }

//        StopBlinking();
//    }

//    // Robot ������ɫ�¼�ʱ���ã�RobotSyncManager �ᱣ֤�������̴߳�����
//    private void OnColorSignalReceived(string color)
//    {
//        Debug.Log($"[LightControl] Received color: {color}"); // ��������
//        if (string.IsNullOrEmpty(color)) return;

//        color = color.Trim().ToUpperInvariant();
//        if (color == "RED")
//        {
//            //StartBlinking();
//        }
//        else if (color == "GREEN")
//        {
//            StartBlinking();
//            Debug.Log("[LightControl] Received GREEN (no action)");
//            // ��ɫû�ж���
//        }
//        else
//        {
//            // ��������
//        }
//    }

//    private void StartBlinking()
//    {
//        Debug.Log("[LightControl] StartBlinking called"); // ��������
//        if (_isBlinking) return;
//        _isBlinking = true;
//        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
//        _blinkCoroutine = StartCoroutine(BlinkRoutine());
//        Debug.Log("[LightControl] Start blinking (RED)");
//    }

//    private void StopBlinking()
//    {
//        if (!_isBlinking) return;
//        _isBlinking = false;
//        if (_blinkCoroutine != null)
//        {
//            StopCoroutine(_blinkCoroutine);
//            _blinkCoroutine = null;
//        }

//        if (pointLight != null)
//        {
//            pointLight.enabled = false;
//            pointLight.color = _originalColor;
//        }

//        Debug.Log("[LightControl] Stop blinking");
//    }

//    private IEnumerator BlinkRoutine()
//    {
//        if (pointLight == null) yield break;

//        // ȷ����ʼΪ�����Ӿ��ϸ��������ע�⣩
//        pointLight.enabled = true;
//        while (_isBlinking)
//        {
//            pointLight.color = blinkColor;
//            yield return new WaitForSeconds(blinkInterval);
//            pointLight.enabled = !pointLight.enabled;
//            yield return new WaitForSeconds(blinkInterval);
//            // ����һѭ�������л�
//        }

//        // ����ʱȷ���ر�
//        pointLight.enabled = false;
//        pointLight.color = _originalColor;
//    }

//    // �� Cube �����ץȡʱ�����ֻ����֣�
//    private void OnCubeGrabbed(XRBaseInteractor interactor)
//    {
//        // ֹͣ��˸��������Ϊ��
//        StopBlinking();

//        // ��֪ RobotSyncManager ����ץȡ��ʼ�������̷���ץȡ��Ϣ��
//        if (robotSyncManager != null && cubeGrab != null)
//        {
//            robotSyncManager.OnGrabStateChanged(true, cubeGrab.gameObject, true);
//        }
//    }

//    // �� Cube ���ſ�ʱ
//    private void OnCubeReleased(XRBaseInteractor interactor)
//    {
//        if (robotSyncManager != null && cubeGrab != null)
//        {
//            robotSyncManager.OnGrabStateChanged(false, cubeGrab.gameObject, true);
//        }
//    }
//}


//using System.Collections;
//using UnityEngine;


//public class LightControl : MonoBehaviour
//{
//    [Header("�ƹ�����")]
//    public Light pointLight;
//    public float blinkInterval = 1.0f;

//    [Header("ץȡ����")]
//    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable cubeGrab;

//    [Header("RobotSync")]
//    public RobotSyncManager robotSyncManager;

//    private bool _isBlinking = false;
//    private Coroutine _blinkCoroutine;
//    private Color _originalColor;
//    public Color blinkColor = Color.red;

//    void Start()
//    {
//        if (pointLight != null)
//        {
//            _originalColor = pointLight.color;
//            pointLight.enabled = false;
//        }

//        if (robotSyncManager != null)
//        {
//            robotSyncManager.onColorSignalReceived.AddListener(OnColorSignalReceived);
//        }

//        if (cubeGrab != null)
//        {
//            //cubeGrab.onSelectEntered.AddListener(OnCubeGrabbed);
//            //cubeGrab.onSelectExited.AddListener(OnCubeReleased);
//        }
//    }

//    void OnDestroy()
//    {
//        if (robotSyncManager != null)
//            robotSyncManager.onColorSignalReceived.RemoveListener(OnColorSignalReceived);

//        if (cubeGrab != null)
//        {
//            //cubeGrab.onSelectEntered.RemoveListener(OnCubeGrabbed);
//            //cubeGrab.onSelectExited.RemoveListener(OnCubeReleased);
//        }

//        StopBlinking();
//    }

//    private void OnColorSignalReceived(string color)
//    {
//        Debug.Log($"[LightControl] Received color: {color}");
//        if (string.IsNullOrEmpty(color)) return;

//        color = color.Trim().ToUpperInvariant();
//        if (color == "RED")
//        {
//            //StartBlinking();
//        }
//        else if (color == "GREEN")
//        {
//            StartBlinking();
//            Debug.Log("[LightControl] Received GREEN (no action)");
//        }
//    }

//    private void StartBlinking()
//    {
//        Debug.Log("[LightControl] StartBlinking called");
//        if (_isBlinking) return;
//        _isBlinking = true;
//        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
//        _blinkCoroutine = StartCoroutine(BlinkRoutine());
//        Debug.Log("[LightControl] Start blinking (RED)");

//        // === ������12 ����Զ�ֹͣ ===
//        StartCoroutine(BlinkTimeoutRoutine(12f));
//    }

//    private void StopBlinking()
//    {
//        if (!_isBlinking) return;
//        _isBlinking = false;
//        if (_blinkCoroutine != null)
//        {
//            StopCoroutine(_blinkCoroutine);
//            _blinkCoroutine = null;
//        }

//        if (pointLight != null)
//        {
//            pointLight.enabled = false;
//            pointLight.color = _originalColor;
//        }

//        Debug.Log("[LightControl] Stop blinking");
//    }

//    private IEnumerator BlinkRoutine()
//    {
//        if (pointLight == null) yield break;

//        pointLight.enabled = true;
//        while (_isBlinking)
//        {
//            pointLight.color = blinkColor;
//            yield return new WaitForSeconds(blinkInterval);
//            pointLight.enabled = !pointLight.enabled;
//            yield return new WaitForSeconds(blinkInterval);
//        }

//        pointLight.enabled = false;
//        pointLight.color = _originalColor;
//    }

//    // === ������12 ���ʱЭ�� ===
//    private IEnumerator BlinkTimeoutRoutine(float duration)
//    {
//        yield return new WaitForSeconds(duration);
//        if (_isBlinking)
//        {
//            StopBlinking();
//            Debug.Log("[LightControl] Auto stopped after 12s");
//        }
//    }

//    //private void OnCubeGrabbed(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
//    //{
//    //    StopBlinking();

//    //    if (robotSyncManager != null && cubeGrab != null)
//    //    {
//    //        robotSyncManager.OnGrabStateChanged(true, cubeGrab.gameObject, true);
//    //    }
//    //}

//    //private void OnCubeReleased(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
//    //{
//    //    if (robotSyncManager != null && cubeGrab != null)
//    //    {
//    //        robotSyncManager.OnGrabStateChanged(false, cubeGrab.gameObject, true);
//    //    }
//    //}
//}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightControl : MonoBehaviour
{
    [Header("灯光提示")]
    public Light pointLight;
    public float blinkInterval = 1.0f;
    public Color blinkColor = Color.red;

    [Header("RobotSync")]
    public RobotSyncManager robotSyncManager;

    [Header("火焰设置")]
    public GameObject firePrefab;                  // 拖入 VFX_Fire_01_Big prefab
    public Transform defaultFireAnchor;           // 机器人只发 BALL_DETECTED 时的默认火点
    public List<Transform> fireAnchors = new List<Transform>(); // 名称需和 targetId 对应
    public bool onlyOneFireAtATime = true;
    public bool lockRespawnAfterExtinguish = true;
    public bool parentFireToAnchor = false;
    public float spawnYOffset = 0.02f;
    [Min(0.1f)] public float fireScaleMultiplier = 1f;
    public Vector3 fireVisualScale = new Vector3(2f, 1.3f, 2f);
    [Min(0f)] public float fireEmissionMultiplier = 2f;
    [Min(0f)] public float fireLightIntensityMultiplier = 1.5f;
    public bool autoRegisterChildFires = true;

    [Header("熄灭判定")]
    public Transform robotRoot;                   // 机器人在 Unity 场景中的根节点
    public float extinguishDistance = 1.2f;       // 机器人靠近到这个距离内才允许灭火

    [Header("Progressive Extinguishing")]
    public bool legacyInstantExtinguishOnAction = false;
    [Min(0.1f)] public float requiredSpraySeconds = 3f;
    [Range(0.05f, 1f)] public float minimumFireScale = 0.2f;
    [Min(0f)] public float smokeLingerSeconds = 2f;

    private bool _isBlinking = false;
    private Coroutine _blinkCoroutine;
    private Color _originalColor;

    private readonly Dictionary<string, Transform> _anchorMap = new Dictionary<string, Transform>();
    private readonly Dictionary<string, GameObject> _activeFires = new Dictionary<string, GameObject>();
    private readonly HashSet<string> _extinguishedTargets = new HashSet<string>();
    private readonly Dictionary<string, FireVisualState> _fireVisualStates = new Dictionary<string, FireVisualState>();

    private class FireVisualState
    {
        public Vector3 initialScale;
        public float sprayedSeconds;
        public ParticleSystem[] particles;
        public float[] rateOverTimeMultipliers;
        public float[] rateOverDistanceMultipliers;
        public Light[] lights;
        public float[] lightIntensities;
    }

    private const string DefaultKey = "__default__";

    void Start()
    {
        if (pointLight != null)
        {
            _originalColor = pointLight.color;
            pointLight.enabled = false;
        }

        AutoAssignRobotSyncManager();
        RebuildAnchorMap();
        RegisterExistingChildFires();

        if (robotSyncManager != null)
        {
            robotSyncManager.onColorSignalReceived.AddListener(OnColorSignalReceived);
            robotSyncManager.onBallDetected.AddListener(OnBallDetected);
            robotSyncManager.onActionTriggered.AddListener(OnActionTriggered);
        }

        if (_activeFires.Count > 0)
        {
            StartBlinking();
        }
    }

    void OnDestroy()
    {
        if (robotSyncManager != null)
        {
            robotSyncManager.onColorSignalReceived.RemoveListener(OnColorSignalReceived);
            robotSyncManager.onBallDetected.RemoveListener(OnBallDetected);
            robotSyncManager.onActionTriggered.RemoveListener(OnActionTriggered);
        }

        StopBlinking();
        DestroyAllFires();
    }

    private void RebuildAnchorMap()
    {
        _anchorMap.Clear();

        if (defaultFireAnchor != null)
            _anchorMap[DefaultKey] = defaultFireAnchor;

        foreach (var anchor in fireAnchors)
        {
            if (anchor == null) continue;
            if (!_anchorMap.ContainsKey(anchor.name))
                _anchorMap.Add(anchor.name, anchor);
            else
                _anchorMap[anchor.name] = anchor;
        }
    }

    private void AutoAssignRobotSyncManager()
    {
        if (robotSyncManager != null)
            return;

#if UNITY_6000_0_OR_NEWER
        robotSyncManager = FindFirstObjectByType<RobotSyncManager>();
#else
        robotSyncManager = FindObjectOfType<RobotSyncManager>();
#endif
    }

    private void RegisterExistingChildFires()
    {
        if (!autoRegisterChildFires)
            return;

        foreach (Transform child in transform)
        {
            if (child == null || child.GetComponentInChildren<ParticleSystem>(true) == null)
                continue;

            GameObject childFire = child.gameObject;
            if (!childFire.activeInHierarchy || _activeFires.ContainsValue(childFire))
                continue;

            string key = BuildUniqueFireKey(childFire.name);
            _activeFires[key] = childFire;
            RegisterFireVisualState(key, childFire);
        }
    }

    private string BuildUniqueFireKey(string baseKey)
    {
        string key = NormalizeTargetKey(baseKey);
        if (!_activeFires.ContainsKey(key))
            return key;

        int suffix = 1;
        string candidate = key;
        while (_activeFires.ContainsKey(candidate))
        {
            candidate = $"{key}_{suffix++}";
        }

        return candidate;
    }

    private void OnColorSignalReceived(string color)
    {
        if (string.IsNullOrEmpty(color)) return;

        color = color.Trim().ToUpperInvariant();

        if (color == "RED")
        {
            SpawnNearestFireFromRedSignal();
        }
        else if (color == "GREEN" && _activeFires.Count == 0)
        {
            StopBlinking();
        }
    }

    private void OnBallDetected(string targetId)
    {
        if (firePrefab == null)
        {
            Debug.LogWarning("[LightControl] firePrefab 未设置。请拖入 VFX_Fire_01_Big。");
            return;
        }

        string key = NormalizeTargetKey(targetId);

        if (lockRespawnAfterExtinguish && _extinguishedTargets.Contains(key))
        {
            Debug.Log($"[LightControl] 目标 {key} 已处理，不再重复生成火焰。");
            return;
        }

        Transform anchor = ResolveAnchor(key);
        if (anchor == null)
        {
            Debug.LogWarning($"[LightControl] 找不到目标锚点：{key}");
            return;
        }

        if (_activeFires.TryGetValue(key, out var existing) && existing != null)
        {
            Debug.Log($"[LightControl] 目标 {key} 已存在火焰，不重复生成。");
            return;
        }

        if (onlyOneFireAtATime)
        {
            DestroyAllFires();
        }

        Vector3 spawnPos = anchor.position + Vector3.up * spawnYOffset;
        Quaternion spawnRot = anchor.rotation;

        GameObject fire = Instantiate(firePrefab, spawnPos, spawnRot);
        fire.name = firePrefab.name; // 让实例名保持为 VFX_Fire_01_Big

        if (parentFireToAnchor)
            fire.transform.SetParent(anchor, true);

        fire.transform.localScale *= Mathf.Max(0.1f, fireScaleMultiplier);
        ApplyFireVisualBoost(fire);

        _activeFires[key] = fire;
        RegisterFireVisualState(key, fire);

        StartBlinking();
        Debug.Log($"[LightControl] Spawn fire '{fire.name}' at target '{key}'");
    }

    private void ApplyFireVisualBoost(GameObject fire)
    {
        if (fire == null)
            return;

        Vector3 safeScale = new Vector3(
            Mathf.Max(0.1f, fireVisualScale.x),
            Mathf.Max(0.1f, fireVisualScale.y),
            Mathf.Max(0.1f, fireVisualScale.z));
        fire.transform.localScale = Vector3.Scale(fire.transform.localScale, safeScale);

        float emissionMultiplier = Mathf.Max(0f, fireEmissionMultiplier);
        foreach (var particle in fire.GetComponentsInChildren<ParticleSystem>(true))
        {
            var emission = particle.emission;
            emission.rateOverTimeMultiplier *= emissionMultiplier;
            emission.rateOverDistanceMultiplier *= emissionMultiplier;
        }

        float lightMultiplier = Mathf.Max(0f, fireLightIntensityMultiplier);
        foreach (var fireLight in fire.GetComponentsInChildren<Light>(true))
            fireLight.intensity *= lightMultiplier;
    }

    private void SpawnNearestFireFromRedSignal()
    {
        string key = FindNearestFireAnchorKeyAhead();
        if (string.IsNullOrEmpty(key))
        {
            Debug.Log("[LightControl] RED received, but no available fire anchor is ahead.");
            return;
        }

        OnBallDetected(key);
    }

    private string FindNearestFireAnchorKeyAhead()
    {
        if (_anchorMap.Count == 0)
            RebuildAnchorMap();

        if (robotRoot == null)
        {
            if (_anchorMap.TryGetValue(DefaultKey, out var defaultAnchor) &&
                IsFireAnchorAvailable(DefaultKey, defaultAnchor))
                return DefaultKey;

            foreach (var kv in _anchorMap)
            {
                if (kv.Value != null && IsFireAnchorAvailable(kv.Key, kv.Value))
                    return kv.Key;
            }

            return null;
        }

        float bestDist = float.MaxValue;
        string bestKey = null;
        Vector3 robotPos = robotRoot.position;
        Vector3 forward = Vector3.ProjectOnPlane(robotRoot.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.001f)
            return null;

        foreach (var kv in _anchorMap)
        {
            if (kv.Value == null || !IsFireAnchorAvailable(kv.Key, kv.Value))
                continue;

            Vector3 toAnchor = Vector3.ProjectOnPlane(kv.Value.position - robotPos, Vector3.up);
            float dist = toAnchor.magnitude;
            if (dist <= 0.001f)
                continue;

            bool isAhead = Vector3.Dot(forward, toAnchor / dist) > 0f;
            if (isAhead && dist < bestDist)
            {
                bestDist = dist;
                bestKey = kv.Key;
            }
        }

        return bestKey;
    }

    private bool IsFireAnchorAvailable(string key, Transform anchor)
    {
        if (_activeFires.TryGetValue(key, out var activeFire) && activeFire != null)
            return false;

        if (lockRespawnAfterExtinguish && _extinguishedTargets.Contains(key))
            return false;

        foreach (var pair in _anchorMap)
        {
            if (pair.Value != anchor)
                continue;

            if (_activeFires.TryGetValue(pair.Key, out activeFire) && activeFire != null)
                return false;

            if (lockRespawnAfterExtinguish && _extinguishedTargets.Contains(pair.Key))
                return false;
        }

        return true;
    }

    private void OnActionTriggered()
    {
        if (!legacyInstantExtinguishOnAction)
            return;

        if (_activeFires.Count == 0)
        {
            Debug.Log("[LightControl] 当前没有火焰，无需处理。");
            return;
        }

        string nearestKey = FindNearestFireKeyInRange();
        if (string.IsNullOrEmpty(nearestKey))
        {
            Debug.Log("[LightControl] 已触发动作，但机器人还未靠近任何火焰。");
            return;
        }

        ExtinguishFire(nearestKey);
    }

    public bool ApplySpray(Vector3 origin, Vector3 direction, float range, float coneAngle, float spraySeconds)
    {
        if (_activeFires.Count == 0 || spraySeconds <= 0f || direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();
        string bestKey = null;
        float bestDistance = float.MaxValue;

        foreach (var pair in _activeFires)
        {
            GameObject fire = pair.Value;
            if (fire == null)
                continue;

            Vector3 toFire = fire.transform.position - origin;
            float distance = toFire.magnitude;
            if (distance > range || distance <= 0.001f)
                continue;

            float angle = Vector3.Angle(direction, toFire / distance);
            if (angle <= coneAngle && distance < bestDistance)
            {
                bestKey = pair.Key;
                bestDistance = distance;
            }
        }

        if (string.IsNullOrEmpty(bestKey))
            return false;

        ApplySprayProgress(bestKey, spraySeconds);
        return true;
    }

    private void RegisterFireVisualState(string key, GameObject fire)
    {
        if (fire == null)
            return;

        ParticleSystem[] particles = fire.GetComponentsInChildren<ParticleSystem>(true);
        Light[] lights = fire.GetComponentsInChildren<Light>(true);
        var state = new FireVisualState
        {
            initialScale = fire.transform.localScale,
            sprayedSeconds = 0f,
            particles = particles,
            rateOverTimeMultipliers = new float[particles.Length],
            rateOverDistanceMultipliers = new float[particles.Length],
            lights = lights,
            lightIntensities = new float[lights.Length]
        };

        for (int i = 0; i < particles.Length; i++)
        {
            var emission = particles[i].emission;
            state.rateOverTimeMultipliers[i] = emission.rateOverTimeMultiplier;
            state.rateOverDistanceMultipliers[i] = emission.rateOverDistanceMultiplier;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                state.lightIntensities[i] = lights[i].intensity;
        }

        _fireVisualStates[key] = state;
    }

    private void ApplySprayProgress(string key, float spraySeconds)
    {
        if (!_activeFires.TryGetValue(key, out var fire) || fire == null)
            return;

        if (!_fireVisualStates.TryGetValue(key, out var state))
        {
            RegisterFireVisualState(key, fire);
            state = _fireVisualStates[key];
        }

        state.sprayedSeconds += spraySeconds;
        float progress = Mathf.Clamp01(state.sprayedSeconds / Mathf.Max(0.1f, requiredSpraySeconds));
        float visualStrength = Mathf.Lerp(1f, minimumFireScale, progress);
        fire.transform.localScale = state.initialScale * visualStrength;

        for (int i = 0; i < state.particles.Length; i++)
        {
            if (state.particles[i] == null)
                continue;

            var emission = state.particles[i].emission;
            emission.rateOverTimeMultiplier = state.rateOverTimeMultipliers[i] * visualStrength;
            emission.rateOverDistanceMultiplier = state.rateOverDistanceMultipliers[i] * visualStrength;
        }

        for (int i = 0; i < state.lights.Length; i++)
        {
            if (state.lights[i] != null)
                state.lights[i].intensity = state.lightIntensities[i] * visualStrength;
        }

        if (progress >= 1f)
            ExtinguishFire(key);
    }

    private string FindNearestFireKeyInRange()
    {
        if (robotRoot == null)
        {
            foreach (var kv in _activeFires)
            {
                if (kv.Value != null) return kv.Key;
            }
            return null;
        }

        float bestDist = float.MaxValue;
        string bestKey = null;
        Vector3 robotPos = robotRoot.position;

        foreach (var kv in _activeFires)
        {
            if (kv.Value == null) continue;

            float dist = Vector3.Distance(robotPos, kv.Value.transform.position);
            if (dist <= extinguishDistance && dist < bestDist)
            {
                bestDist = dist;
                bestKey = kv.Key;
            }
        }

        return bestKey;
    }

    private void ExtinguishFire(string key)
    {
        if (!_activeFires.TryGetValue(key, out var fire))
            return;

        if (fire != null)
        {
            foreach (var particle in fire.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            Destroy(fire, smokeLingerSeconds);
        }

        _activeFires.Remove(key);
        _fireVisualStates.Remove(key);

        if (lockRespawnAfterExtinguish)
            _extinguishedTargets.Add(key);

        if (_activeFires.Count == 0)
            StopBlinking();

        Debug.Log($"[LightControl] 火焰 {key} 已熄灭。");
    }

    public void ResetExtinguishedTargets()
    {
        _extinguishedTargets.Clear();
        Debug.Log("[LightControl] 已清空已处理火点列表，可再次生成火焰。");
    }

    private string NormalizeTargetKey(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return DefaultKey;

        return targetId.Trim();
    }

    private Transform ResolveAnchor(string key)
    {
        if (_anchorMap.Count == 0)
            RebuildAnchorMap();

        if (_anchorMap.TryGetValue(key, out var anchor))
            return anchor;

        if (_anchorMap.TryGetValue(DefaultKey, out var fallback))
            return fallback;

        return null;
    }

    private void DestroyAllFires()
    {
        var keys = new List<string>(_activeFires.Keys);
        foreach (var key in keys)
        {
            if (_activeFires[key] != null)
                Destroy(_activeFires[key]);
        }
        _activeFires.Clear();
        _fireVisualStates.Clear();
    }

    private void StartBlinking()
    {
        if (_isBlinking) return;

        _isBlinking = true;
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlinking()
    {
        if (!_isBlinking) return;

        _isBlinking = false;
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        if (pointLight != null)
        {
            pointLight.enabled = false;
            pointLight.color = _originalColor;
        }
    }

    private IEnumerator BlinkRoutine()
    {
        if (pointLight == null) yield break;

        pointLight.enabled = true;

        while (_isBlinking)
        {
            pointLight.color = blinkColor;
            yield return new WaitForSeconds(blinkInterval);
            pointLight.enabled = !pointLight.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }

        pointLight.enabled = false;
        pointLight.color = _originalColor;
    }
}


