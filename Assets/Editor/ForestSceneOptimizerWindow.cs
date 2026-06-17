using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class ForestSceneOptimizerWindow : EditorWindow
{
    [Header("Terrain")]
    private float terrainTreeDistance = 1000f;
    private int terrainTreeMaxFullLODCount = 25;
    private float terrainDetailDistance = 80f;
    private float terrainDetailDensity = 0.7f;
    private float terrainPixelError = 20f;

    [Header("Small Props / Debris")]
    private float smallPropMaxSize = 2.0f;
    private string clutterKeywordsCsv = "debris,log,stump,rock,pebble,twig,bush,grass,branch";
    private bool disableCastShadowsOnSmallProps = true;
    private bool disableReceiveShadowsOnSmallProps = true;
    private bool markSmallPropsStatic = true;
    private bool enableGpuInstancingOnSharedMaterials = true;

    [Header("Lights")]
    private bool disableShadowsOnNonDirectionalLights = true;
    private bool capNonDirectionalLightRange = false;
    private float maxNonDirectionalLightRange = 18f;

    [Header("Reflection Probes")]
    private bool lowerReflectionProbeResolution = true;
    private int reflectionProbeResolution = 64;

    [Header("Static Flags")]
    private bool markLargeEnvironmentStatic = true;

    [MenuItem("Tools/Optimization/Forest Scene Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<ForestSceneOptimizerWindow>("Forest Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Forest Scene Optimizer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Open the scene you want to optimize, make a backup first, then run this tool. " +
            "It is tuned for big outdoor forest scenes with lots of repeated debris.",
            MessageType.Info);

        EditorGUILayout.Space();

        GUILayout.Label("Terrain", EditorStyles.boldLabel);
        terrainTreeDistance = EditorGUILayout.FloatField("Tree Distance", terrainTreeDistance);
        terrainTreeMaxFullLODCount = EditorGUILayout.IntField("Tree Max Full LOD Count", terrainTreeMaxFullLODCount);
        terrainDetailDistance = EditorGUILayout.FloatField("Detail Distance", terrainDetailDistance);
        terrainDetailDensity = EditorGUILayout.Slider("Detail Density", terrainDetailDensity, 0.1f, 1f);
        terrainPixelError = EditorGUILayout.FloatField("Heightmap Pixel Error", terrainPixelError);

        EditorGUILayout.Space();

        GUILayout.Label("Small Props / Debris", EditorStyles.boldLabel);
        smallPropMaxSize = EditorGUILayout.FloatField("Small Prop Max Size", smallPropMaxSize);
        clutterKeywordsCsv = EditorGUILayout.TextField("Clutter Keywords", clutterKeywordsCsv);
        disableCastShadowsOnSmallProps = EditorGUILayout.Toggle("Disable Cast Shadows", disableCastShadowsOnSmallProps);
        disableReceiveShadowsOnSmallProps = EditorGUILayout.Toggle("Disable Receive Shadows", disableReceiveShadowsOnSmallProps);
        markSmallPropsStatic = EditorGUILayout.Toggle("Mark Small Props Static", markSmallPropsStatic);
        enableGpuInstancingOnSharedMaterials = EditorGUILayout.Toggle("Enable GPU Instancing", enableGpuInstancingOnSharedMaterials);

        EditorGUILayout.Space();

        GUILayout.Label("Lights", EditorStyles.boldLabel);
        disableShadowsOnNonDirectionalLights = EditorGUILayout.Toggle("Disable Non-Directional Light Shadows", disableShadowsOnNonDirectionalLights);
        capNonDirectionalLightRange = EditorGUILayout.Toggle("Cap Non-Directional Light Range", capNonDirectionalLightRange);
        using (new EditorGUI.DisabledScope(!capNonDirectionalLightRange))
        {
            maxNonDirectionalLightRange = EditorGUILayout.FloatField("Max Non-Directional Range", maxNonDirectionalLightRange);
        }

        EditorGUILayout.Space();

        GUILayout.Label("Reflection Probes", EditorStyles.boldLabel);
        lowerReflectionProbeResolution = EditorGUILayout.Toggle("Lower Probe Resolution", lowerReflectionProbeResolution);
        using (new EditorGUI.DisabledScope(!lowerReflectionProbeResolution))
        {
            reflectionProbeResolution = EditorGUILayout.IntPopup(
                "Probe Resolution",
                reflectionProbeResolution,
                new[] { "16", "32", "64", "128" },
                new[] { 16, 32, 64, 128 });
        }

        EditorGUILayout.Space();

        GUILayout.Label("Static Flags", EditorStyles.boldLabel);
        markLargeEnvironmentStatic = EditorGUILayout.Toggle("Mark Large Environment Static", markLargeEnvironmentStatic);

        EditorGUILayout.Space();

        if (GUILayout.Button("Analyze Current Scene", GUILayout.Height(30)))
        {
            AnalyzeCurrentScene();
        }

        if (GUILayout.Button("Optimize Current Scene", GUILayout.Height(40)))
        {
            OptimizeCurrentScene();
        }
    }

    private void AnalyzeCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("No loaded scene found.");
            return;
        }

        string[] keywords = ParseKeywords(clutterKeywordsCsv);

        int terrainCount = 0;
        int rendererCount = 0;
        int smallPropCount = 0;
        int lightsCount = 0;
        int probesCount = 0;

        foreach (Terrain terrain in GetSceneComponents<Terrain>(scene))
            terrainCount++;

        foreach (Renderer renderer in GetSceneComponents<Renderer>(scene))
        {
            rendererCount++;
            if (IsSmallProp(renderer, keywords))
                smallPropCount++;
        }

        foreach (Light light in GetSceneComponents<Light>(scene))
            lightsCount++;

        foreach (ReflectionProbe probe in GetSceneComponents<ReflectionProbe>(scene))
            probesCount++;

        Debug.Log(
            $"[Forest Optimizer] Scene: {scene.name}\n" +
            $"- Terrains: {terrainCount}\n" +
            $"- Renderers: {rendererCount}\n" +
            $"- Small props matched: {smallPropCount}\n" +
            $"- Lights: {lightsCount}\n" +
            $"- Reflection probes: {probesCount}");
    }

    private void OptimizeCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("No loaded scene found.");
            return;
        }

        string[] keywords = ParseKeywords(clutterKeywordsCsv);

        int terrainsChanged = 0;
        int renderersChanged = 0;
        int lightsChanged = 0;
        int probesChanged = 0;
        int gameObjectsMarkedStatic = 0;
        int materialsInstancingEnabled = 0;

        try
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            foreach (Terrain terrain in GetSceneComponents<Terrain>(scene))
            {
                Undo.RecordObject(terrain, "Optimize Terrain");
                terrain.treeDistance = terrainTreeDistance;
                terrain.treeMaximumFullLODCount = terrainTreeMaxFullLODCount;
                terrain.detailObjectDistance = terrainDetailDistance;
                terrain.detailObjectDensity = terrainDetailDensity;
                terrain.heightmapPixelError = terrainPixelError;
                EditorUtility.SetDirty(terrain);
                terrainsChanged++;
            }

            foreach (Renderer renderer in GetSceneComponents<Renderer>(scene))
            {
                if (renderer == null)
                    continue;

                GameObject go = renderer.gameObject;
                if (ShouldSkipObject(go))
                    continue;

                bool changedRenderer = false;
                bool isSmallProp = IsSmallProp(renderer, keywords);

                if (isSmallProp)
                {
                    if (disableCastShadowsOnSmallProps && renderer.shadowCastingMode != ShadowCastingMode.Off)
                    {
                        Undo.RecordObject(renderer, "Disable Cast Shadows");
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        changedRenderer = true;
                    }

                    if (disableReceiveShadowsOnSmallProps && renderer is MeshRenderer meshRenderer && meshRenderer.receiveShadows)
                    {
                        Undo.RecordObject(meshRenderer, "Disable Receive Shadows");
                        meshRenderer.receiveShadows = false;
                        changedRenderer = true;
                    }

                    if (markSmallPropsStatic && IsSafeToMarkStatic(go))
                    {
                        StaticEditorFlags desiredFlags =
                            StaticEditorFlags.BatchingStatic |
                            StaticEditorFlags.OccluderStatic |
                            StaticEditorFlags.OccludeeStatic |
                            StaticEditorFlags.ReflectionProbeStatic;

                        StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(go);
                        StaticEditorFlags newFlags = currentFlags | desiredFlags;

                        if (newFlags != currentFlags)
                        {
                            Undo.RecordObject(go, "Mark Small Prop Static");
                            GameObjectUtility.SetStaticEditorFlags(go, newFlags);
                            gameObjectsMarkedStatic++;
                        }
                    }
                }
                else if (markLargeEnvironmentStatic && IsSafeToMarkStatic(go))
                {
                    StaticEditorFlags desiredFlags =
                        StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.ReflectionProbeStatic;

                    StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(go);
                    StaticEditorFlags newFlags = currentFlags | desiredFlags;

                    if (newFlags != currentFlags)
                    {
                        Undo.RecordObject(go, "Mark Environment Static");
                        GameObjectUtility.SetStaticEditorFlags(go, newFlags);
                        gameObjectsMarkedStatic++;
                    }
                }

                if (enableGpuInstancingOnSharedMaterials)
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material mat = materials[i];
                        if (mat == null || mat.enableInstancing)
                            continue;

                        Undo.RecordObject(mat, "Enable GPU Instancing");
                        mat.enableInstancing = true;
                        EditorUtility.SetDirty(mat);
                        materialsInstancingEnabled++;
                    }
                }

                if (changedRenderer)
                {
                    EditorUtility.SetDirty(renderer);
                    renderersChanged++;
                }
            }

            foreach (Light light in GetSceneComponents<Light>(scene))
            {
                if (light == null)
                    continue;

                bool changedLight = false;

                if (disableShadowsOnNonDirectionalLights && light.type != LightType.Directional && light.shadows != LightShadows.None)
                {
                    Undo.RecordObject(light, "Disable Light Shadows");
                    light.shadows = LightShadows.None;
                    changedLight = true;
                }

                if (capNonDirectionalLightRange && light.type != LightType.Directional && light.range > maxNonDirectionalLightRange)
                {
                    Undo.RecordObject(light, "Cap Light Range");
                    light.range = maxNonDirectionalLightRange;
                    changedLight = true;
                }

                if (changedLight)
                {
                    EditorUtility.SetDirty(light);
                    lightsChanged++;
                }
            }

            foreach (ReflectionProbe probe in GetSceneComponents<ReflectionProbe>(scene))
            {
                if (probe == null)
                    continue;

                if (lowerReflectionProbeResolution && probe.resolution != reflectionProbeResolution)
                {
                    Undo.RecordObject(probe, "Lower Reflection Probe Resolution");
                    probe.resolution = reflectionProbeResolution;
                    EditorUtility.SetDirty(probe);
                    probesChanged++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(
                $"[Forest Optimizer] Finished optimizing scene '{scene.name}'.\n" +
                $"- Terrains changed: {terrainsChanged}\n" +
                $"- Renderers changed: {renderersChanged}\n" +
                $"- Lights changed: {lightsChanged}\n" +
                $"- Reflection probes changed: {probesChanged}\n" +
                $"- GameObjects marked static: {gameObjectsMarkedStatic}\n" +
                $"- Materials with GPU instancing enabled: {materialsInstancingEnabled}\n\n" +
                "Now test the scene, then save it if the result looks good.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Forest Optimizer] Failed: {ex}");
        }
    }

    private static string[] ParseKeywords(string csv)
    {
        string[] raw = csv.Split(',');
        List<string> result = new List<string>();

        for (int i = 0; i < raw.Length; i++)
        {
            string trimmed = raw[i].Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }

        return result.ToArray();
    }

    private static bool IsSmallProp(Renderer renderer, string[] keywords)
    {
        if (renderer == null)
            return false;

        string name = renderer.gameObject.name;
        string lowerName = name.ToLowerInvariant();

        for (int i = 0; i < keywords.Length; i++)
        {
            if (lowerName.Contains(keywords[i].ToLowerInvariant()))
                return true;
        }

        Bounds b = renderer.bounds;
        float maxSize = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));

        ForestSceneOptimizerWindow window = GetWindow<ForestSceneOptimizerWindow>();
        return maxSize <= window.smallPropMaxSize;
    }

    private static bool ShouldSkipObject(GameObject go)
    {
        if (go == null)
            return true;

        if (go.CompareTag("Player") || go.CompareTag("MainCamera"))
            return true;

        if (go.GetComponentInParent<CharacterController>() != null)
            return true;

        if (go.GetComponentInParent<Rigidbody>() != null)
            return true;

        if (go.GetComponentInParent<Animator>() != null)
            return true;

        return false;
    }

    private static bool IsSafeToMarkStatic(GameObject go)
    {
        if (go == null)
            return false;

        if (ShouldSkipObject(go))
            return false;

        if (go.GetComponent<Light>() != null)
            return false;

        if (go.GetComponent<ReflectionProbe>() != null)
            return false;

        return true;
    }

    private static IEnumerable<T> GetSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> results = new List<T>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            results.AddRange(roots[i].GetComponentsInChildren<T>(true));
        }

        return results;
    }
}