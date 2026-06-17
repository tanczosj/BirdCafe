using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MasterDevToolkitWindow : EditorWindow
{
    private enum Tab
    {
        Dashboard,
        Assets,
        Scripts,
        Scenes,
        Selection,
        Validation,
        Cleanup,
        Setup,
        Debug
    }

    private Tab currentTab;
    private Vector2 scroll;

    // Script Creator
    private string scriptName = "NewScript";
    private string scriptCode =
@"using UnityEngine;

public class NewScript : MonoBehaviour
{
    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
}";

    // Asset tools
    private string assetSearch = "";
    private string assetLabel = "Important";
    private string renamePrefix = "";
    private string renameSuffix = "";
    private string renameFind = "";
    private string renameReplace = "";
    private string customFolderName = "NewFolder";

    // Scene tools
    private string newSceneName = "NewScene";

    // Selection tools
    private string nameContains = "";
    private string tagSearch = "Player";
    private int layerSearch = 0;
    private string componentSearch = "Animator";
    private float gridSnapSize = 1f;
    private int groundSnapLayer = 0;
    private float groundSnapHeight = 50f;
    private float distributeSpacing = 2f;
    private float randomYawMin = 0f;
    private float randomYawMax = 360f;
    private float randomScaleMin = 0.9f;
    private float randomScaleMax = 1.1f;

    // Setup tools
    private string newTagName = "LockOnTarget";
    private string newLayerName = "Ground";

    // Debug tools
    private GameObject spawnPrefab;
    private float spawnDistance = 4f;
    private Material materialToAssign;

    [MenuItem("Tools/Master Dev Toolkit")]
    public static void Open()
    {
        GetWindow<MasterDevToolkitWindow>("Master Dev Toolkit");
    }

    private void OnGUI()
    {
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, Enum.GetNames(typeof(Tab)));
        GUILayout.Space(8);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        switch (currentTab)
        {
            case Tab.Dashboard:
                DrawDashboardTab();
                break;
            case Tab.Assets:
                DrawAssetsTab();
                break;
            case Tab.Scripts:
                DrawScriptsTab();
                break;
            case Tab.Scenes:
                DrawScenesTab();
                break;
            case Tab.Selection:
                DrawSelectionTab();
                break;
            case Tab.Validation:
                DrawValidationTab();
                break;
            case Tab.Cleanup:
                DrawCleanupTab();
                break;
            case Tab.Setup:
                DrawSetupTab();
                break;
            case Tab.Debug:
                DrawDebugTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    // DASHBOARD
    // ============================================================

    private void DrawDashboardTab()
    {
        Header("Master Dev Toolkit");

        EditorGUILayout.HelpBox(
            "This window gives you project organization, scene switching, script creation, validation, cleanup, setup, and debug tools.",
            MessageType.Info
        );

        Header("Fast Actions");

        if (GUILayout.Button("Save Project And Open Scenes"))
        {
            SaveProjectAndScenes();
        }

        if (GUILayout.Button("Refresh Asset Database"))
        {
            AssetDatabase.Refresh();
            Debug.Log("Asset database refreshed.");
        }

        if (GUILayout.Button("Clear Console"))
        {
            ClearConsole();
        }

        if (GUILayout.Button("Create Common Project Folders"))
        {
            CreateCommonFolders();
        }

        if (GUILayout.Button("Scan Current Scene For Problems"))
        {
            FindMissingScriptsInOpenScenes();
            FindMissingReferencesInOpenScenes();
            CountSceneObjects();
        }

        if (GUILayout.Button("Organize Assets By Type"))
        {
            ConfirmThen("Organize Assets", "This moves files into folders. Back up or commit your project first.", OrganizeAssetsByType);
        }

        Header("Project Summary");

        if (GUILayout.Button("Log Project Summary"))
        {
            LogProjectSummary();
        }
    }

    // ============================================================
    // ASSETS
    // ============================================================

    private void DrawAssetsTab()
    {
        Header("Asset Organizer");

        if (GUILayout.Button("Organize Assets By Type"))
        {
            ConfirmThen("Organize Assets", "This moves files into folders based on their extension.", OrganizeAssetsByType);
        }

        if (GUILayout.Button("Create Common Project Folders"))
        {
            CreateCommonFolders();
        }

        customFolderName = EditorGUILayout.TextField("Create Folder In Assets", customFolderName);

        if (GUILayout.Button("Create Custom Folder"))
        {
            EnsureFolder("Assets/" + CleanFileName(customFolderName));
            AssetDatabase.Refresh();
            Debug.Log("Created folder: Assets/" + CleanFileName(customFolderName));
        }

        Space();

        Header("Find Assets");

        assetSearch = EditorGUILayout.TextField("Search Name", assetSearch);

        if (GUILayout.Button("Search Assets By Name"))
        {
            SearchAssetsByName(assetSearch);
        }

        if (GUILayout.Button("Find References To Selected Asset"))
        {
            FindReferencesToSelectedAsset();
        }

        if (GUILayout.Button("Copy Selected Asset Paths To Console"))
        {
            CopySelectedAssetPathsToConsole();
        }

        if (GUILayout.Button("Reveal Selected Asset In File Explorer"))
        {
            RevealSelectedAsset();
        }

        Space();

        Header("Bulk Rename Selected Assets");

        renamePrefix = EditorGUILayout.TextField("Prefix", renamePrefix);
        renameSuffix = EditorGUILayout.TextField("Suffix", renameSuffix);
        renameFind = EditorGUILayout.TextField("Find Text", renameFind);
        renameReplace = EditorGUILayout.TextField("Replace With", renameReplace);

        if (GUILayout.Button("Rename Selected Assets"))
        {
            ConfirmThen("Rename Assets", "This renames selected project assets.", RenameSelectedAssets);
        }

        Space();

        Header("Labels");

        assetLabel = EditorGUILayout.TextField("Label", assetLabel);

        if (GUILayout.Button("Add Label To Selected Assets"))
        {
            AddLabelToSelectedAssets(assetLabel);
        }

        if (GUILayout.Button("Remove Label From Selected Assets"))
        {
            RemoveLabelFromSelectedAssets(assetLabel);
        }
    }

    private static void OrganizeAssetsByType()
    {
        Dictionary<string, string> map = new Dictionary<string, string>
        {
            { ".cs", "Scripts" },
            { ".asmdef", "Scripts/AssemblyDefinitions" },

            { ".prefab", "Prefabs" },

            { ".mat", "Materials" },
            { ".physicmaterial", "Materials/Physics" },

            { ".png", "Textures" },
            { ".jpg", "Textures" },
            { ".jpeg", "Textures" },
            { ".tga", "Textures" },
            { ".psd", "Textures" },
            { ".exr", "Textures" },
            { ".hdr", "Textures" },

            { ".fbx", "Models" },
            { ".obj", "Models" },
            { ".blend", "Models" },

            { ".unity", "Scenes" },

            { ".anim", "Animations/Clips" },
            { ".controller", "Animations/Controllers" },
            { ".overridecontroller", "Animations/Controllers" },
            { ".mask", "Animations/Masks" },

            { ".wav", "Audio" },
            { ".mp3", "Audio" },
            { ".ogg", "Audio" },

            { ".shader", "Shaders" },
            { ".shadergraph", "Shaders" },
            { ".compute", "Shaders" },

            { ".ttf", "Fonts" },
            { ".otf", "Fonts" },

            { ".asset", "ScriptableObjects" },
            { ".rendertexture", "RenderTextures" },

            { ".playable", "Timeline" },
            { ".signal", "Timeline" }
        };

        AssetDatabase.StartAssetEditing();

        try
        {
            string[] paths = AssetDatabase.GetAllAssetPaths();

            foreach (string path in paths)
            {
                if (!path.StartsWith("Assets/")) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (path.StartsWith("Assets/Editor/")) continue;

                string extension = Path.GetExtension(path).ToLower();
                if (string.IsNullOrEmpty(extension)) continue;

                string targetFolder = map.ContainsKey(extension) ? map[extension] : "Other";
                string targetDirectory = "Assets/" + targetFolder;

                EnsureFolder(targetDirectory);

                string fileName = Path.GetFileName(path);
                string targetPath = AssetDatabase.GenerateUniqueAssetPath(targetDirectory + "/" + fileName);

                if (path == targetPath) continue;

                string error = AssetDatabase.MoveAsset(path, targetPath);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning("Could not move " + path + ": " + error);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("Asset organization complete.");
    }

    private static void SearchAssetsByName(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            Debug.LogWarning("Search field is empty.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets(search);
        Debug.Log("Found " + guids.Length + " assets matching: " + search);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Debug.Log("Found asset: " + path, asset);
        }
    }

    private static void FindReferencesToSelectedAsset()
    {
        UnityEngine.Object selected = Selection.activeObject;

        if (selected == null)
        {
            Debug.LogWarning("Select an asset first.");
            return;
        }

        string selectedPath = AssetDatabase.GetAssetPath(selected);

        if (string.IsNullOrEmpty(selectedPath))
        {
            Debug.LogWarning("Selected object is not a project asset.");
            return;
        }

        string[] candidateGuids = AssetDatabase.FindAssets("t:Prefab t:Scene t:Material t:AnimatorController t:ScriptableObject");
        int found = 0;

        foreach (string guid in candidateGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (path == selectedPath) continue;

            string[] dependencies = AssetDatabase.GetDependencies(path, true);

            if (dependencies.Contains(selectedPath))
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                Debug.Log("Reference found in: " + path, obj);
                found++;
            }
        }

        Debug.Log("Reference scan complete. Found references: " + found);
    }

    private static void CopySelectedAssetPathsToConsole()
    {
        UnityEngine.Object[] selected = Selection.objects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No assets selected.");
            return;
        }

        foreach (UnityEngine.Object obj in selected)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log(path, obj);
            }
        }
    }

    private static void RevealSelectedAsset()
    {
        UnityEngine.Object selected = Selection.activeObject;

        if (selected == null)
        {
            Debug.LogWarning("Nothing selected.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selected);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Selected object is not a project asset.");
            return;
        }

        EditorUtility.RevealInFinder(path);
    }

    private void RenameSelectedAssets()
    {
        UnityEngine.Object[] selected = Selection.objects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No assets selected.");
            return;
        }

        int renamed = 0;

        foreach (UnityEngine.Object obj in selected)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path)) continue;
            if (AssetDatabase.IsValidFolder(path)) continue;

            string directory = Path.GetDirectoryName(path).Replace("\\", "/");
            string currentName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            string newName = currentName;

            if (!string.IsNullOrEmpty(renameFind))
            {
                newName = newName.Replace(renameFind, renameReplace);
            }

            newName = renamePrefix + newName + renameSuffix;
            newName = CleanFileName(newName);

            if (string.IsNullOrEmpty(newName)) continue;
            if (newName == currentName) continue;

            string newPath = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + newName + extension);
            string error = AssetDatabase.RenameAsset(path, Path.GetFileNameWithoutExtension(newPath));

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("Could not rename " + path + ": " + error);
            }
            else
            {
                renamed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Renamed assets: " + renamed);
    }

    private static void AddLabelToSelectedAssets(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            Debug.LogWarning("Label cannot be empty.");
            return;
        }

        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path)) continue;

            List<string> labels = new List<string>(AssetDatabase.GetLabels(obj));

            if (!labels.Contains(label))
            {
                labels.Add(label);
                AssetDatabase.SetLabels(obj, labels.ToArray());
                Debug.Log("Added label '" + label + "' to " + path, obj);
            }
        }
    }

    private static void RemoveLabelFromSelectedAssets(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            Debug.LogWarning("Label cannot be empty.");
            return;
        }

        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path)) continue;

            List<string> labels = new List<string>(AssetDatabase.GetLabels(obj));

            if (labels.Remove(label))
            {
                AssetDatabase.SetLabels(obj, labels.ToArray());
                Debug.Log("Removed label '" + label + "' from " + path, obj);
            }
        }
    }

    // ============================================================
    // SCRIPTS
    // ============================================================

    private void DrawScriptsTab()
    {
        Header("Quick Script Creator");

        scriptName = EditorGUILayout.TextField("Script Name", scriptName);

        GUILayout.Label("Code:");
        scriptCode = EditorGUILayout.TextArea(scriptCode, GUILayout.Height(350));

        if (GUILayout.Button("Create Script In Assets/Scripts"))
        {
            CreateScript();
        }

        Space();

        Header("Templates");

        if (GUILayout.Button("Load Basic MonoBehaviour Template"))
        {
            scriptName = "NewScript";
            scriptCode =
@"using UnityEngine;

public class NewScript : MonoBehaviour
{
    private void Awake()
    {
        
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
}";
        }

        if (GUILayout.Button("Load Singleton MonoBehaviour Template"))
        {
            scriptName = "NewSingleton";
            scriptCode =
@"using UnityEngine;

public class NewSingleton : MonoBehaviour
{
    public static NewSingleton Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}";
        }

        if (GUILayout.Button("Load ScriptableObject Template"))
        {
            scriptName = "NewData";
            scriptCode =
@"using UnityEngine;

[CreateAssetMenu(fileName = ""NewData"", menuName = ""Game/New Data"")]
public class NewData : ScriptableObject
{
    public string displayName;
}";
        }

        if (GUILayout.Button("Load Editor Window Template"))
        {
            scriptName = "NewEditorWindow";
            scriptCode =
@"using UnityEditor;
using UnityEngine;

public class NewEditorWindow : EditorWindow
{
    [MenuItem(""Tools/New Editor Window"")]
    public static void Open()
    {
        GetWindow<NewEditorWindow>(""New Editor Window"");
    }

    private void OnGUI()
    {
        GUILayout.Label(""New Tool"", EditorStyles.boldLabel);
    }
}";
        }
    }

    private void CreateScript()
    {
        if (string.IsNullOrWhiteSpace(scriptName))
        {
            Debug.LogError("Script name cannot be empty.");
            return;
        }

        EnsureFolder("Assets/Scripts");

        string cleanName = CleanFileName(scriptName);
        string fileName = cleanName.EndsWith(".cs") ? cleanName : cleanName + ".cs";
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scripts/" + fileName);

        File.WriteAllText(path, scriptCode);

        AssetDatabase.Refresh();

        Debug.Log("Created script: " + path);
    }

    // ============================================================
    // SCENES
    // ============================================================

    private void DrawScenesTab()
    {
        Header("Scene Switcher");

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(path);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(sceneName);

            if (GUILayout.Button("Open", GUILayout.Width(70)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }
            }

            if (GUILayout.Button("Add", GUILayout.Width(70)))
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            if (GUILayout.Button("Play", GUILayout.Width(70)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                    EditorApplication.isPlaying = true;
                }
            }

            if (GUILayout.Button("Boot", GUILayout.Width(70)))
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                EditorSceneManager.playModeStartScene = sceneAsset;
                Debug.Log("Play mode start scene set to: " + path);
            }

            EditorGUILayout.EndHorizontal();
        }

        Space();

        Header("Scene Creation");

        newSceneName = EditorGUILayout.TextField("New Scene Name", newSceneName);

        if (GUILayout.Button("Create Empty Scene In Assets/Scenes"))
        {
            CreateNewScene();
        }

        Space();

        Header("Build Settings");

        if (GUILayout.Button("Add Current Scene To Build Settings"))
        {
            AddCurrentSceneToBuildSettings();
        }

        if (GUILayout.Button("Add All Project Scenes To Build Settings"))
        {
            AddAllScenesToBuildSettings();
        }

        if (GUILayout.Button("Clear Play Mode Start Scene"))
        {
            EditorSceneManager.playModeStartScene = null;
            Debug.Log("Play mode start scene cleared.");
        }
    }

    private void CreateNewScene()
    {
        EnsureFolder("Assets/Scenes");

        string cleanName = CleanFileName(newSceneName);

        if (string.IsNullOrWhiteSpace(cleanName))
        {
            Debug.LogWarning("Scene name cannot be empty.");
            return;
        }

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/" + cleanName + ".unity");

        EditorSceneManager.SaveScene(newScene, path);
        AssetDatabase.Refresh();

        Debug.Log("Created scene: " + path);
    }

    private static void AddCurrentSceneToBuildSettings()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("Current scene has not been saved yet.");
            return;
        }

        AddScenePathToBuildSettings(scene.path);
    }

    private static void AddAllScenesToBuildSettings()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AddScenePathToBuildSettings(path);
        }

        Debug.Log("All project scenes added to build settings.");
    }

    private static void AddScenePathToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool alreadyExists = scenes.Any(s => s.path == scenePath);

        if (!alreadyExists)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Added to build settings: " + scenePath);
        }
    }

    // ============================================================
    // SELECTION
    // ============================================================

    private void DrawSelectionTab()
    {
        Header("Select Objects");

        nameContains = EditorGUILayout.TextField("Name Contains", nameContains);

        if (GUILayout.Button("Select Scene Objects By Name"))
        {
            SelectObjectsByName(nameContains);
        }

        tagSearch = EditorGUILayout.TextField("Tag", tagSearch);

        if (GUILayout.Button("Select Objects By Tag"))
        {
            SelectObjectsByTag(tagSearch);
        }

        layerSearch = EditorGUILayout.LayerField("Layer", layerSearch);

        if (GUILayout.Button("Select Objects By Layer"))
        {
            SelectObjectsByLayer(layerSearch);
        }

        componentSearch = EditorGUILayout.TextField("Component Type Name", componentSearch);

        if (GUILayout.Button("Select Objects With Component"))
        {
            SelectObjectsWithComponent(componentSearch);
        }

        Space();

        Header("Transform Tools");

        if (GUILayout.Button("Reset Selected Local Transform"))
        {
            ResetSelectedLocalTransforms();
        }

        if (GUILayout.Button("Create Empty Parent For Selected Objects"))
        {
            CreateParentForSelected();
        }

        if (GUILayout.Button("Unparent Selected Objects"))
        {
            UnparentSelected();
        }

        gridSnapSize = EditorGUILayout.FloatField("Grid Snap Size", gridSnapSize);

        if (GUILayout.Button("Snap Selected To Grid"))
        {
            SnapSelectedToGrid(gridSnapSize);
        }

        groundSnapLayer = EditorGUILayout.LayerField("Ground Layer", groundSnapLayer);
        groundSnapHeight = EditorGUILayout.FloatField("Ray Start Height", groundSnapHeight);

        if (GUILayout.Button("Snap Selected Down To Ground"))
        {
            SnapSelectedToGround(groundSnapLayer, groundSnapHeight);
        }

        Space();

        Header("Align And Distribute");

        if (GUILayout.Button("Align Selected X"))
        {
            AlignSelectedAxis(0);
        }

        if (GUILayout.Button("Align Selected Y"))
        {
            AlignSelectedAxis(1);
        }

        if (GUILayout.Button("Align Selected Z"))
        {
            AlignSelectedAxis(2);
        }

        distributeSpacing = EditorGUILayout.FloatField("Spacing", distributeSpacing);

        if (GUILayout.Button("Distribute Selected On X"))
        {
            DistributeSelectedAxis(0, distributeSpacing);
        }

        if (GUILayout.Button("Distribute Selected On Z"))
        {
            DistributeSelectedAxis(2, distributeSpacing);
        }

        Space();

        Header("Randomize");

        randomYawMin = EditorGUILayout.FloatField("Random Yaw Min", randomYawMin);
        randomYawMax = EditorGUILayout.FloatField("Random Yaw Max", randomYawMax);

        if (GUILayout.Button("Randomize Selected Y Rotation"))
        {
            RandomizeSelectedYaw(randomYawMin, randomYawMax);
        }

        randomScaleMin = EditorGUILayout.FloatField("Random Scale Min", randomScaleMin);
        randomScaleMax = EditorGUILayout.FloatField("Random Scale Max", randomScaleMax);

        if (GUILayout.Button("Randomize Selected Uniform Scale"))
        {
            RandomizeSelectedScale(randomScaleMin, randomScaleMax);
        }
    }

    private static void SelectObjectsByName(string contains)
    {
        if (string.IsNullOrWhiteSpace(contains))
        {
            Debug.LogWarning("Name search is empty.");
            return;
        }

        List<GameObject> matches = GetAllSceneObjects()
            .Where(o => o.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        Selection.objects = matches.ToArray();

        Debug.Log("Selected objects by name: " + matches.Count);
    }

    private static void SelectObjectsByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            Debug.LogWarning("Tag is empty.");
            return;
        }

        List<GameObject> matches = new List<GameObject>();

        foreach (GameObject obj in GetAllSceneObjects())
        {
            try
            {
                if (obj.CompareTag(tag))
                {
                    matches.Add(obj);
                }
            }
            catch
            {
                Debug.LogWarning("Tag does not exist: " + tag);
                return;
            }
        }

        Selection.objects = matches.ToArray();
        Debug.Log("Selected objects by tag: " + matches.Count);
    }

    private static void SelectObjectsByLayer(int layer)
    {
        List<GameObject> matches = GetAllSceneObjects()
            .Where(o => o.layer == layer)
            .ToList();

        Selection.objects = matches.ToArray();

        Debug.Log("Selected objects on layer " + LayerMask.LayerToName(layer) + ": " + matches.Count);
    }

    private static void SelectObjectsWithComponent(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            Debug.LogWarning("Component type name is empty.");
            return;
        }

        Type type = FindType(typeName);

        if (type == null)
        {
            Debug.LogWarning("Could not find type: " + typeName);
            return;
        }

        if (!typeof(Component).IsAssignableFrom(type))
        {
            Debug.LogWarning(typeName + " is not a Component type.");
            return;
        }

        List<GameObject> matches = new List<GameObject>();

        foreach (GameObject obj in GetAllSceneObjects())
        {
            if (obj.GetComponent(type) != null)
            {
                matches.Add(obj);
            }
        }

        Selection.objects = matches.ToArray();

        Debug.Log("Selected objects with component " + type.Name + ": " + matches.Count);
    }

    private static void ResetSelectedLocalTransforms()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Reset Local Transform");
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }
    }

    private static void CreateParentForSelected()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length == 0)
        {
            Debug.LogWarning("No scene objects selected.");
            return;
        }

        GameObject parent = new GameObject("Group");
        Undo.RegisterCreatedObjectUndo(parent, "Create Parent");

        Vector3 average = Vector3.zero;

        foreach (GameObject obj in selected)
        {
            average += obj.transform.position;
        }

        average /= selected.Length;
        parent.transform.position = average;

        foreach (GameObject obj in selected)
        {
            Undo.SetTransformParent(obj.transform, parent.transform, "Parent Selected Objects");
        }

        Selection.activeGameObject = parent;
    }

    private static void UnparentSelected()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.SetTransformParent(obj.transform, null, "Unparent Selected");
        }
    }

    private static void SnapSelectedToGrid(float size)
    {
        if (size <= 0f)
        {
            Debug.LogWarning("Grid size must be greater than zero.");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Snap To Grid");

            Vector3 p = obj.transform.position;
            p.x = Mathf.Round(p.x / size) * size;
            p.y = Mathf.Round(p.y / size) * size;
            p.z = Mathf.Round(p.z / size) * size;

            obj.transform.position = p;
        }
    }

    private static void SnapSelectedToGround(int layer, float height)
    {
        int mask = 1 << layer;

        foreach (GameObject obj in Selection.gameObjects)
        {
            Vector3 origin = obj.transform.position + Vector3.up * height;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, height * 2f, mask))
            {
                Undo.RecordObject(obj.transform, "Snap To Ground");
                obj.transform.position = hit.point;
            }
            else
            {
                Debug.LogWarning("No ground hit for: " + obj.name, obj);
            }
        }
    }

    private static void AlignSelectedAxis(int axis)
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length < 2)
        {
            Debug.LogWarning("Select at least two objects.");
            return;
        }

        float value = selected[0].transform.position[axis];

        for (int i = 1; i < selected.Length; i++)
        {
            Undo.RecordObject(selected[i].transform, "Align Objects");
            Vector3 p = selected[i].transform.position;
            p[axis] = value;
            selected[i].transform.position = p;
        }
    }

    private static void DistributeSelectedAxis(int axis, float spacing)
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length < 2)
        {
            Debug.LogWarning("Select at least two objects.");
            return;
        }

        Array.Sort(selected, (a, b) => a.transform.position[axis].CompareTo(b.transform.position[axis]));

        float start = selected[0].transform.position[axis];

        for (int i = 0; i < selected.Length; i++)
        {
            Undo.RecordObject(selected[i].transform, "Distribute Objects");

            Vector3 p = selected[i].transform.position;
            p[axis] = start + spacing * i;
            selected[i].transform.position = p;
        }
    }

    private static void RandomizeSelectedYaw(float min, float max)
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Randomize Yaw");

            Vector3 euler = obj.transform.eulerAngles;
            euler.y = UnityEngine.Random.Range(min, max);
            obj.transform.eulerAngles = euler;
        }
    }

    private static void RandomizeSelectedScale(float min, float max)
    {
        if (min <= 0f || max <= 0f)
        {
            Debug.LogWarning("Scale values must be greater than zero.");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Randomize Scale");

            float scale = UnityEngine.Random.Range(min, max);
            obj.transform.localScale = Vector3.one * scale;
        }
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private void DrawValidationTab()
    {
        Header("Scene Validation");

        if (GUILayout.Button("Find Missing Scripts In Open Scenes"))
        {
            FindMissingScriptsInOpenScenes();
        }

        if (GUILayout.Button("Find Missing Object References In Open Scenes"))
        {
            FindMissingReferencesInOpenScenes();
        }

        if (GUILayout.Button("Find Disabled Objects In Open Scenes"))
        {
            FindDisabledObjectsInOpenScenes();
        }

        if (GUILayout.Button("Find Duplicate Object Names In Open Scenes"))
        {
            FindDuplicateObjectNames();
        }

        if (GUILayout.Button("Find Renderers With No Collider"))
        {
            FindRenderersWithNoCollider();
        }

        if (GUILayout.Button("Find Objects On Default Layer"))
        {
            FindObjectsOnDefaultLayer();
        }

        if (GUILayout.Button("Count Scene Objects"))
        {
            CountSceneObjects();
        }

        if (GUILayout.Button("Count Selected Mesh Vertices And Triangles"))
        {
            CountSelectedMeshStats();
        }

        Space();

        Header("Prefab Validation");

        if (GUILayout.Button("Scan All Prefabs For Missing Scripts"))
        {
            ScanAllPrefabsForMissingScripts();
        }

        if (GUILayout.Button("Scan All Prefabs For Missing References"))
        {
            ScanAllPrefabsForMissingReferences();
        }

        if (GUILayout.Button("Validate Selected Prefabs"))
        {
            ValidateSelectedPrefabs();
        }
    }

    private static void FindMissingScriptsInOpenScenes()
    {
        int count = 0;

        foreach (GameObject obj in GetAllSceneObjects())
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    count++;
                    Debug.LogWarning("Missing script on: " + GetHierarchyPath(obj), obj);
                }
            }
        }

        Debug.Log("Missing script scan complete. Found: " + count);
    }

    private static void FindMissingReferencesInOpenScenes()
    {
        int count = 0;

        foreach (GameObject obj in GetAllSceneObjects())
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null) continue;

                count += LogMissingReferences(component, GetHierarchyPath(obj));
            }
        }

        Debug.Log("Missing reference scan complete. Found: " + count);
    }

    private static int LogMissingReferences(UnityEngine.Object obj, string ownerPath)
    {
        int count = 0;

        SerializedObject serializedObject = new SerializedObject(obj);
        SerializedProperty property = serializedObject.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                bool missingReference =
                    property.objectReferenceValue == null &&
                    property.objectReferenceInstanceIDValue != 0;

                if (missingReference)
                {
                    Debug.LogWarning(
                        "Missing reference on " + ownerPath +
                        " | Component/Object: " + obj.GetType().Name +
                        " | Field: " + property.propertyPath,
                        obj
                    );

                    count++;
                }
            }
        }

        return count;
    }

    private static void FindDisabledObjectsInOpenScenes()
    {
        int count = 0;

        foreach (GameObject obj in GetAllSceneObjects())
        {
            if (!obj.activeInHierarchy)
            {
                Debug.Log("Disabled object: " + GetHierarchyPath(obj), obj);
                count++;
            }
        }

        Debug.Log("Disabled objects found: " + count);
    }

    private static void FindDuplicateObjectNames()
    {
        Dictionary<string, List<GameObject>> groups = new Dictionary<string, List<GameObject>>();

        foreach (GameObject obj in GetAllSceneObjects())
        {
            if (!groups.ContainsKey(obj.name))
            {
                groups[obj.name] = new List<GameObject>();
            }

            groups[obj.name].Add(obj);
        }

        int duplicateGroups = 0;

        foreach (KeyValuePair<string, List<GameObject>> pair in groups)
        {
            if (pair.Value.Count <= 1) continue;

            duplicateGroups++;

            foreach (GameObject obj in pair.Value)
            {
                Debug.LogWarning("Duplicate name '" + pair.Key + "': " + GetHierarchyPath(obj), obj);
            }
        }

        Debug.Log("Duplicate name groups found: " + duplicateGroups);
    }

    private static void FindRenderersWithNoCollider()
    {
        int count = 0;

        foreach (GameObject obj in GetAllSceneObjects())
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            Collider collider = obj.GetComponent<Collider>();

            if (renderer != null && collider == null)
            {
                Debug.LogWarning("Renderer with no Collider: " + GetHierarchyPath(obj), obj);
                count++;
            }
        }

        Debug.Log("Renderers with no collider found: " + count);
    }

    private static void FindObjectsOnDefaultLayer()
    {
        int count = 0;

        foreach (GameObject obj in GetAllSceneObjects())
        {
            if (obj.layer == 0)
            {
                Debug.Log("Object on Default layer: " + GetHierarchyPath(obj), obj);
                count++;
            }
        }

        Debug.Log("Objects on Default layer: " + count);
    }

    private static void CountSceneObjects()
    {
        List<GameObject> objects = GetAllSceneObjects();

        int total = objects.Count;
        int active = objects.Count(o => o.activeInHierarchy);
        int inactive = total - active;
        int renderers = objects.Count(o => o.GetComponent<Renderer>() != null);
        int colliders = objects.Count(o => o.GetComponent<Collider>() != null);
        int rigidbodies = objects.Count(o => o.GetComponent<Rigidbody>() != null);
        int animators = objects.Count(o => o.GetComponent<Animator>() != null);
        int lights = objects.Count(o => o.GetComponent<Light>() != null);
        int cameras = objects.Count(o => o.GetComponent<Camera>() != null);

        Debug.Log(
            "Scene Object Count\n" +
            "Total: " + total + "\n" +
            "Active: " + active + "\n" +
            "Inactive: " + inactive + "\n" +
            "Renderers: " + renderers + "\n" +
            "Colliders: " + colliders + "\n" +
            "Rigidbodies: " + rigidbodies + "\n" +
            "Animators: " + animators + "\n" +
            "Lights: " + lights + "\n" +
            "Cameras: " + cameras
        );
    }

    private static void CountSelectedMeshStats()
    {
        int vertices = 0;
        int triangles = 0;

        foreach (GameObject obj in Selection.gameObjects)
        {
            MeshFilter[] filters = obj.GetComponentsInChildren<MeshFilter>(true);
            SkinnedMeshRenderer[] skinned = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh == null) continue;

                vertices += filter.sharedMesh.vertexCount;
                triangles += filter.sharedMesh.triangles.Length / 3;
            }

            foreach (SkinnedMeshRenderer renderer in skinned)
            {
                if (renderer.sharedMesh == null) continue;

                vertices += renderer.sharedMesh.vertexCount;
                triangles += renderer.sharedMesh.triangles.Length / 3;
            }
        }

        Debug.Log("Selected mesh stats | Vertices: " + vertices + " | Triangles: " + triangles);
    }

    private static void ScanAllPrefabsForMissingScripts()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            GameObject[] objects = prefab.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject)
                .ToArray();

            foreach (GameObject obj in objects)
            {
                Component[] components = obj.GetComponents<Component>();

                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        Debug.LogWarning("Missing script in prefab: " + path + " | Object: " + GetHierarchyPath(obj), prefab);
                        count++;
                    }
                }
            }
        }

        Debug.Log("Prefab missing script scan complete. Found: " + count);
    }

    private static void ScanAllPrefabsForMissingReferences()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            foreach (Component component in components)
            {
                if (component == null) continue;

                count += LogMissingReferences(component, path);
            }
        }

        Debug.Log("Prefab missing reference scan complete. Found: " + count);
    }

    private static void ValidateSelectedPrefabs()
    {
        int checkedCount = 0;

        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path)) continue;
            if (!path.EndsWith(".prefab")) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            checkedCount++;

            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            foreach (Component component in components)
            {
                if (component == null)
                {
                    Debug.LogWarning("Selected prefab has missing script: " + path, prefab);
                }
                else
                {
                    LogMissingReferences(component, path);
                }
            }
        }

        Debug.Log("Selected prefabs checked: " + checkedCount);
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void DrawCleanupTab()
    {
        Header("Safe Cleanup");

        if (GUILayout.Button("Clear Console"))
        {
            ClearConsole();
        }

        if (GUILayout.Button("Save Project And Open Scenes"))
        {
            SaveProjectAndScenes();
        }

        if (GUILayout.Button("Refresh Asset Database"))
        {
            AssetDatabase.Refresh();
            Debug.Log("Asset database refreshed.");
        }

        if (GUILayout.Button("Unload Unused Assets"))
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
            Debug.Log("Unused assets unloaded.");
        }

        Space();

        Header("Destructive Cleanup");

        EditorGUILayout.HelpBox("These can change or delete things. Use Git or make a backup first.", MessageType.Warning);

        if (GUILayout.Button("Delete Empty Folders"))
        {
            ConfirmThen("Delete Empty Folders", "This deletes empty folders inside Assets.", DeleteEmptyFolders);
        }

        if (GUILayout.Button("Remove Missing Scripts From Selected Objects"))
        {
            ConfirmThen("Remove Missing Scripts", "This removes missing script components from selected scene objects.", RemoveMissingScriptsFromSelected);
        }

        if (GUILayout.Button("Remove Missing Scripts From Open Scenes"))
        {
            ConfirmThen("Remove Missing Scripts", "This removes missing script components from every object in open scenes.", RemoveMissingScriptsFromOpenScenes);
        }

        if (GUILayout.Button("Delete Empty GameObjects From Selected Hierarchies"))
        {
            ConfirmThen("Delete Empty GameObjects", "This deletes empty children from selected hierarchies.", DeleteEmptyChildrenFromSelected);
        }
    }

    private static void DeleteEmptyFolders()
    {
        string assetsPath = Application.dataPath;
        string[] directories = Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length)
            .ToArray();

        int deleted = 0;

        foreach (string directory in directories)
        {
            bool hasFiles = Directory.GetFiles(directory).Length > 0;
            bool hasFolders = Directory.GetDirectories(directory).Length > 0;

            if (!hasFiles && !hasFolders)
            {
                string assetPath = FullPathToAssetPath(directory);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    if (AssetDatabase.DeleteAsset(assetPath))
                    {
                        deleted++;
                    }
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Deleted empty folders: " + deleted);
    }

    private static void RemoveMissingScriptsFromSelected()
    {
        int removed = 0;

        foreach (GameObject obj in Selection.gameObjects)
        {
            foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
            {
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
        }

        Debug.Log("Removed missing scripts from selected hierarchies: " + removed);
    }

    private static void RemoveMissingScriptsFromOpenScenes()
    {
        int removed = 0;

        foreach (GameObject obj in GetAllSceneObjects())
        {
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
        }

        Debug.Log("Removed missing scripts from open scenes: " + removed);
    }

    private static void DeleteEmptyChildrenFromSelected()
    {
        int deleted = 0;

        foreach (GameObject root in Selection.gameObjects)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true)
                .OrderByDescending(t => GetDepth(t))
                .ToArray();

            foreach (Transform child in children)
            {
                if (child == root.transform) continue;

                if (IsEmptyGameObject(child.gameObject))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    deleted++;
                }
            }
        }

        Debug.Log("Deleted empty child GameObjects: " + deleted);
    }

    private static bool IsEmptyGameObject(GameObject obj)
    {
        if (obj.transform.childCount > 0) return false;

        Component[] components = obj.GetComponents<Component>();

        return components.Length == 1 && components[0] is Transform;
    }

    private static int GetDepth(Transform t)
    {
        int depth = 0;

        while (t.parent != null)
        {
            depth++;
            t = t.parent;
        }

        return depth;
    }

    // ============================================================
    // SETUP
    // ============================================================

    private void DrawSetupTab()
    {
        Header("Project Folders");

        if (GUILayout.Button("Create Common Project Folders"))
        {
            CreateCommonFolders();
        }

        Space();

        Header("Tags And Layers");

        newTagName = EditorGUILayout.TextField("New Tag", newTagName);

        if (GUILayout.Button("Create Tag"))
        {
            AddTag(newTagName);
        }

        newLayerName = EditorGUILayout.TextField("New Layer", newLayerName);

        if (GUILayout.Button("Create Layer"))
        {
            AddLayer(newLayerName);
        }

        if (GUILayout.Button("Create Common Soulslike Tags And Layers"))
        {
            CreateCommonTagsAndLayers();
        }

        Space();

        Header("Scene Setup");

        if (GUILayout.Button("Create Basic Camera And Light"))
        {
            CreateBasicCameraAndLight();
        }

        if (GUILayout.Button("Create Simple Ground Plane"))
        {
            CreateSimpleGroundPlane();
        }

        if (GUILayout.Button("Create Test Player Capsule"))
        {
            CreateTestPlayerCapsule();
        }

        if (GUILayout.Button("Create Basic UI Canvas"))
        {
            CreateBasicUICanvas();
        }
    }

    private static void CreateCommonFolders()
    {
        string[] folders =
        {
            "Scripts",
            "Scripts/Player",
            "Scripts/Camera",
            "Scripts/Combat",
            "Scripts/Enemies",
            "Scripts/UI",
            "Scripts/Managers",
            "Scripts/ScriptableObjects",

            "Prefabs",
            "Prefabs/Player",
            "Prefabs/Enemies",
            "Prefabs/UI",
            "Prefabs/VFX",

            "Scenes",
            "Materials",
            "Textures",
            "Models",
            "Animations",
            "Animations/Clips",
            "Animations/Controllers",
            "Audio",
            "Audio/Music",
            "Audio/SFX",
            "Shaders",
            "ScriptableObjects",
            "Editor",
            "Resources",
            "UI",
            "VFX",
            "Plugins",
            "ThirdParty"
        };

        foreach (string folder in folders)
        {
            EnsureFolder("Assets/" + folder);
        }

        AssetDatabase.Refresh();
        Debug.Log("Common project folders created.");
    }

    private static void CreateCommonTagsAndLayers()
    {
        string[] tags =
        {
            "Player",
            "Enemy",
            "Ground",
            "LockOnTarget",
            "Interactable",
            "CameraTarget",
            "Boss",
            "Hitbox",
            "Hurtbox"
        };

        string[] layers =
        {
            "Player",
            "Enemy",
            "Ground",
            "Interactable",
            "LockOnTarget",
            "Hitbox",
            "Hurtbox"
        };

        foreach (string tag in tags)
        {
            AddTag(tag);
        }

        foreach (string layer in layers)
        {
            AddLayer(layer);
        }

        Debug.Log("Common tags and layers created.");
    }

    private static void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            Debug.LogWarning("Tag cannot be empty.");
            return;
        }

        SerializedObject tagManager = GetTagManager();
        SerializedProperty tagsProperty = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProperty.arraySize; i++)
        {
            SerializedProperty element = tagsProperty.GetArrayElementAtIndex(i);

            if (element.stringValue == tag)
            {
                Debug.Log("Tag already exists: " + tag);
                return;
            }
        }

        tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
        SerializedProperty newTag = tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1);
        newTag.stringValue = tag;

        tagManager.ApplyModifiedProperties();

        Debug.Log("Created tag: " + tag);
    }

    private static void AddLayer(string layer)
    {
        if (string.IsNullOrWhiteSpace(layer))
        {
            Debug.LogWarning("Layer cannot be empty.");
            return;
        }

        SerializedObject tagManager = GetTagManager();
        SerializedProperty layersProperty = tagManager.FindProperty("layers");

        for (int i = 0; i < layersProperty.arraySize; i++)
        {
            SerializedProperty element = layersProperty.GetArrayElementAtIndex(i);

            if (element.stringValue == layer)
            {
                Debug.Log("Layer already exists: " + layer);
                return;
            }
        }

        for (int i = 8; i < layersProperty.arraySize; i++)
        {
            SerializedProperty element = layersProperty.GetArrayElementAtIndex(i);

            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layer;
                tagManager.ApplyModifiedProperties();
                Debug.Log("Created layer: " + layer + " at slot " + i);
                return;
            }
        }

        Debug.LogWarning("No empty user layer slots available.");
    }

    private static SerializedObject GetTagManager()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        return new SerializedObject(assets[0]);
    }

    private static void CreateBasicCameraAndLight()
    {
        GameObject cameraObj = new GameObject("Main Camera");
        Undo.RegisterCreatedObjectUndo(cameraObj, "Create Camera");

        Camera camera = cameraObj.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 3f, -8f);
        cameraObj.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

        GameObject lightObj = new GameObject("Directional Light");
        Undo.RegisterCreatedObjectUndo(lightObj, "Create Directional Light");

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Selection.activeGameObject = cameraObj;

        Debug.Log("Created basic camera and light.");
    }

    private static void CreateSimpleGroundPlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(plane, "Create Ground Plane");

        plane.name = "Ground";
        plane.transform.position = Vector3.zero;
        plane.transform.localScale = new Vector3(5f, 1f, 5f);

        int groundLayer = LayerMask.NameToLayer("Ground");

        if (groundLayer >= 0)
        {
            plane.layer = groundLayer;
        }

        try
        {
            plane.tag = "Ground";
        }
        catch
        {
            // Tag may not exist yet.
        }

        Selection.activeGameObject = plane;
        Debug.Log("Created ground plane.");
    }

    private static void CreateTestPlayerCapsule()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(player, "Create Test Player");

        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);

        try
        {
            player.tag = "Player";
        }
        catch
        {
            Debug.LogWarning("Player tag does not exist yet. Use Setup > Create Common Soulslike Tags And Layers.");
        }

        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer >= 0)
        {
            player.layer = playerLayer;
        }

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;

        GameObject cameraTarget = new GameObject("CameraTarget");
        Undo.RegisterCreatedObjectUndo(cameraTarget, "Create Camera Target");

        cameraTarget.transform.SetParent(player.transform);
        cameraTarget.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        Selection.activeGameObject = player;

        Debug.Log("Created test player capsule.");
    }

    private static void CreateBasicUICanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create Event System");

            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        GameObject textObj = new GameObject("Dev Text");
        Undo.RegisterCreatedObjectUndo(textObj, "Create Dev Text");

        textObj.transform.SetParent(canvasObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = "DEV UI";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(300f, 100f);

        Selection.activeGameObject = canvasObj;

        Debug.Log("Created basic UI canvas.");
    }

    // ============================================================
    // DEBUG
    // ============================================================

    private void DrawDebugTab()
    {
        Header("Play Mode");

        if (GUILayout.Button(EditorApplication.isPlaying ? "Stop Play Mode" : "Start Play Mode"))
        {
            EditorApplication.isPlaying = !EditorApplication.isPlaying;
        }

        if (GUILayout.Button(EditorApplication.isPaused ? "Unpause" : "Pause"))
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
        }

        Space();

        Header("Spawn Tools");

        spawnPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", spawnPrefab, typeof(GameObject), false);
        spawnDistance = EditorGUILayout.FloatField("Spawn Distance", spawnDistance);

        if (GUILayout.Button("Spawn Prefab In Front Of Scene View Camera"))
        {
            SpawnPrefabInFrontOfSceneCamera();
        }

        if (GUILayout.Button("Create Dev Marker At Scene View Camera"))
        {
            CreateDevMarkerAtSceneCamera();
        }

        Space();

        Header("Selected Object Debug");

        if (GUILayout.Button("Log Selected Hierarchy Paths"))
        {
            LogSelectedHierarchyPaths();
        }

        if (GUILayout.Button("Toggle Selected Active"))
        {
            ToggleSelectedActive();
        }

        if (GUILayout.Button("Move Selected To Scene View Camera"))
        {
            MoveSelectedToSceneViewCamera();
        }

        if (GUILayout.Button("Frame Selected In Scene View"))
        {
            FrameSelectedInSceneView();
        }

        Space();

        Header("Material Tools");

        materialToAssign = (Material)EditorGUILayout.ObjectField("Material", materialToAssign, typeof(Material), false);

        if (GUILayout.Button("Assign Material To Selected Renderers"))
        {
            AssignMaterialToSelectedRenderers();
        }

        Space();

        Header("Screenshots");

        if (GUILayout.Button("Capture Game View Screenshot"))
        {
            CaptureScreenshot();
        }
    }

    private void SpawnPrefabInFrontOfSceneCamera()
    {
        if (spawnPrefab == null)
        {
            Debug.LogWarning("Assign a prefab first.");
            return;
        }

        SceneView view = SceneView.lastActiveSceneView;

        if (view == null)
        {
            Debug.LogWarning("No Scene View found.");
            return;
        }

        Vector3 position = view.camera.transform.position + view.camera.transform.forward * spawnDistance;
        GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);

        Undo.RegisterCreatedObjectUndo(spawned, "Spawn Prefab");
        spawned.transform.position = position;

        Selection.activeGameObject = spawned;

        Debug.Log("Spawned prefab: " + spawned.name, spawned);
    }

    private static void CreateDevMarkerAtSceneCamera()
    {
        SceneView view = SceneView.lastActiveSceneView;

        if (view == null)
        {
            Debug.LogWarning("No Scene View found.");
            return;
        }

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(marker, "Create Dev Marker");

        marker.name = "DEV_Marker";
        marker.transform.position = view.camera.transform.position + view.camera.transform.forward * 4f;
        marker.transform.localScale = Vector3.one * 0.25f;

        Selection.activeGameObject = marker;

        Debug.Log("Created dev marker.", marker);
    }

    private static void LogSelectedHierarchyPaths()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Debug.Log(GetHierarchyPath(obj), obj);
        }
    }

    private static void ToggleSelectedActive()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj, "Toggle Active");
            obj.SetActive(!obj.activeSelf);
        }
    }

    private static void MoveSelectedToSceneViewCamera()
    {
        SceneView view = SceneView.lastActiveSceneView;

        if (view == null)
        {
            Debug.LogWarning("No Scene View found.");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Move To Scene Camera");
            obj.transform.position = view.camera.transform.position + view.camera.transform.forward * 4f;
        }
    }

    private static void FrameSelectedInSceneView()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    private void AssignMaterialToSelectedRenderers()
    {
        if (materialToAssign == null)
        {
            Debug.LogWarning("Assign a material first.");
            return;
        }

        int count = 0;

        foreach (GameObject obj in Selection.gameObjects)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Undo.RecordObject(renderer, "Assign Material");
                renderer.sharedMaterial = materialToAssign;
                count++;
            }
        }

        Debug.Log("Assigned material to renderers: " + count);
    }

    private static void CaptureScreenshot()
    {
        EnsureFolder("Assets/Screenshots");

        string fileName = "Screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string path = "Assets/Screenshots/" + fileName;

        ScreenCapture.CaptureScreenshot(path);
        AssetDatabase.Refresh();

        Debug.Log("Screenshot saved to: " + path);
    }

    // ============================================================
    // GENERAL HELPERS
    // ============================================================

    private static void Header(string text)
    {
        GUILayout.Space(8);
        GUILayout.Label(text, EditorStyles.boldLabel);
        GUILayout.Space(3);
    }

    private static void Space()
    {
        GUILayout.Space(15);
    }

    private static void ConfirmThen(string title, string message, Action action)
    {
        if (EditorUtility.DisplayDialog(title, message, "Do It", "Cancel"))
        {
            action.Invoke();
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/");

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string CleanFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c.ToString(), "");
        }

        return name.Trim();
    }

    private static List<GameObject> GetAllSceneObjects()
    {
        List<GameObject> result = new List<GameObject>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (!scene.isLoaded) continue;

            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in transforms)
                {
                    result.Add(t.gameObject);
                }
            }
        }

        return result;
    }

    private static string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static Type FindType(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);

            if (type != null)
            {
                return type;
            }

            Type found = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SaveProjectAndScenes()
    {
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("Project and open scenes saved.");
    }

    private static void ClearConsole()
    {
        Type logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");

        if (logEntries == null)
        {
            Debug.LogWarning("Could not access console clear method.");
            return;
        }

        MethodInfo clear = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
        clear?.Invoke(null, null);
    }

    private static void LogProjectSummary()
    {
        string[] allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/"))
            .ToArray();

        int scripts = allAssets.Count(p => p.EndsWith(".cs"));
        int prefabs = allAssets.Count(p => p.EndsWith(".prefab"));
        int scenes = allAssets.Count(p => p.EndsWith(".unity"));
        int materials = allAssets.Count(p => p.EndsWith(".mat"));
        int textures = allAssets.Count(p =>
            p.EndsWith(".png") ||
            p.EndsWith(".jpg") ||
            p.EndsWith(".jpeg") ||
            p.EndsWith(".psd") ||
            p.EndsWith(".tga"));

        Debug.Log(
            "Project Summary\n" +
            "Assets: " + allAssets.Length + "\n" +
            "Scripts: " + scripts + "\n" +
            "Prefabs: " + prefabs + "\n" +
            "Scenes: " + scenes + "\n" +
            "Materials: " + materials + "\n" +
            "Textures: " + textures
        );
    }

    private static string FullPathToAssetPath(string fullPath)
    {
        fullPath = fullPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        if (!fullPath.StartsWith(dataPath))
        {
            return null;
        }

        return "Assets" + fullPath.Substring(dataPath.Length);
    }
}