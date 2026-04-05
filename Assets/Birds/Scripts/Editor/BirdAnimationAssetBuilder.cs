using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BirdCafe.Unity.Birds;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace BirdCafe.Unity.Birds.Editor
{
    public static class BirdAnimationAssetBuilder
    {
        private const int GridColumns = 6;
        private const int GridRows = 6;
        private const int FrameCount = GridColumns * GridRows;
        private const float DefaultFps = BirdAnimationStateClip.DefaultFps;

        private const string SourceRoot = "Assets/Birds/Source";
        private const string GeneratedRoot = "Assets/Birds/Generated";
        private const string SetRoot = GeneratedRoot + "/Sets";
        private const string LibraryRoot = GeneratedRoot + "/Library";
        private const string LibraryPath = LibraryRoot + "/BirdAppearanceLibrary.asset";

        private sealed class ClipBuildData
        {
            public string SpeciesId;
            public string CostumeId;
            public string StateKey;
            public Sprite[] Frames;
        }

        [MenuItem("Tools/Bird Cafe/Rebuild Bird Animation Assets")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/Birds");
            EnsureFolder(GeneratedRoot);
            EnsureFolder(SetRoot);
            EnsureFolder(LibraryRoot);

            if (!AssetDatabase.IsValidFolder(SourceRoot))
            {
                Debug.LogWarning($"[BirdAnimationAssetBuilder] Source folder not found: {SourceRoot}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceRoot });
            var grouped = new Dictionary<string, List<ClipBuildData>>(StringComparer.Ordinal);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryParsePath(path, out string speciesId, out string costumeId, out string stateKey))
                {
                    Debug.LogWarning($"[BirdAnimationAssetBuilder] Skipping unexpected source path: {path}");
                    continue;
                }

                ConfigureAndSlice(path);

                Sprite[] frames = LoadFrames(path);
                if (frames.Length == 0)
                {
                    Debug.LogWarning($"[BirdAnimationAssetBuilder] No frames found for {path}");
                    continue;
                }

                string key = MakeKey(speciesId, costumeId);
                if (!grouped.TryGetValue(key, out List<ClipBuildData> clips))
                {
                    clips = new List<ClipBuildData>();
                    grouped[key] = clips;
                }

                clips.Add(new ClipBuildData
                {
                    SpeciesId = speciesId,
                    CostumeId = costumeId,
                    StateKey = stateKey,
                    Frames = frames
                });
            }

            var setAssets = new List<BirdAnimationSet>();

            foreach (KeyValuePair<string, List<ClipBuildData>> pair in grouped)
            {
                ClipBuildData first = pair.Value[0];
                string costumeToken = string.IsNullOrWhiteSpace(first.CostumeId)
                    ? "base"
                    : SanitizeToken(first.CostumeId);

                string setPath = $"{SetRoot}/BirdSet_{SanitizeToken(first.SpeciesId)}_{costumeToken}.asset";

                BirdAnimationSet set = AssetDatabase.LoadAssetAtPath<BirdAnimationSet>(setPath);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<BirdAnimationSet>();
                    AssetDatabase.CreateAsset(set, setPath);
                }

                set.SetIdentity(first.SpeciesId, first.CostumeId);
                set.ReplaceClips(pair.Value
                    .OrderBy(c => c.StateKey, StringComparer.Ordinal)
                    .Select(c => new BirdAnimationStateClip
                    {
                        StateKey = c.StateKey,
                        Frames = c.Frames,
                        FramesPerSecond = DefaultFps,
                        PlayOnce = true
                    })
                    .ToList());

                EditorUtility.SetDirty(set);
                setAssets.Add(set);
            }

            BirdAppearanceLibrary library = AssetDatabase.LoadAssetAtPath<BirdAppearanceLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<BirdAppearanceLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.ReplaceSets(setAssets
                .OrderBy(s => s.SpeciesId, StringComparer.Ordinal)
                .ThenBy(s => s.CostumeId, StringComparer.Ordinal)
                .ToList());

            EditorUtility.SetDirty(library);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BirdAnimationAssetBuilder] Built {setAssets.Count} set assets from {guids.Length} source textures.");
        }

        private static bool TryParsePath(string assetPath, out string speciesId, out string costumeId, out string stateKey)
        {
            speciesId = null;
            costumeId = null;
            stateKey = null;

            string relative = assetPath.Replace('\\', '/');
            string[] parts = relative.Split('/');
            int rootIndex = Array.IndexOf(parts, "Source");
            if (rootIndex < 0 || parts.Length < rootIndex + 4)
            {
                return false;
            }

            speciesId = parts[rootIndex + 1];
            string costumeFolder = parts[rootIndex + 2];
            stateKey = Path.GetFileNameWithoutExtension(parts[rootIndex + 3]);

            costumeId = string.Equals(costumeFolder, "base", StringComparison.OrdinalIgnoreCase)
                ? null
                : costumeFolder;

            return !string.IsNullOrWhiteSpace(speciesId) && !string.IsNullOrWhiteSpace(stateKey);
        }

        private static void ConfigureAndSlice(string path)
        {
            if (!TryGetSourceImageSize(path, out int sourceWidth, out int sourceHeight))
            {
                Debug.LogWarning($"[BirdAnimationAssetBuilder] Could not read source image size for {path}");
                return;
            }

            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                Debug.LogWarning($"[BirdAnimationAssetBuilder] Invalid source image size for {path}: {sourceWidth}x{sourceHeight}");
                return;
            }

            if (sourceWidth % GridColumns != 0 || sourceHeight % GridRows != 0)
            {
                Debug.LogWarning(
                    $"[BirdAnimationAssetBuilder] Source image {path} is {sourceWidth}x{sourceHeight}, which is not evenly divisible by {GridColumns}x{GridRows}.");
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool importerDirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importerDirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importerDirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                importerDirty = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                importerDirty = true;
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                importerDirty = true;
            }

            int requiredSize = Mathf.NextPowerOfTwo(Mathf.Max(sourceWidth, sourceHeight));
            requiredSize = Mathf.Clamp(requiredSize, 32, 8192);

            if (importer.maxTextureSize < requiredSize)
            {
                importer.maxTextureSize = requiredSize;
                importerDirty = true;
            }

            if (importerDirty)
            {
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    return;
                }
            }

            int frameWidth = sourceWidth / GridColumns;
            int frameHeight = sourceHeight / GridRows;

            var factory = new SpriteDataProviderFactories();
            factory.Init();

            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                Debug.LogWarning($"[BirdAnimationAssetBuilder] Could not get sprite data provider for {path}");
                return;
            }

            dataProvider.InitSpriteEditorDataProvider();

            SpriteRect[] spriteRects = BuildSpriteRects(path, frameWidth, frameHeight);
            dataProvider.SetSpriteRects(spriteRects);

            ISpriteNameFileIdDataProvider nameFileIdDataProvider =
                dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();

            if (nameFileIdDataProvider != null)
            {
                var nameFileIdPairs = new List<SpriteNameFileIdPair>(spriteRects.Length);
                for (int i = 0; i < spriteRects.Length; i++)
                {
                    nameFileIdPairs.Add(new SpriteNameFileIdPair(spriteRects[i].name, spriteRects[i].spriteID));
                }

                nameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
            }

            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static SpriteRect[] BuildSpriteRects(string path, int frameWidth, int frameHeight)
        {
            var rects = new SpriteRect[FrameCount];
            int index = 0;
            string baseName = Path.GetFileNameWithoutExtension(path);

            for (int row = 0; row < GridRows; row++)
            {
                for (int col = 0; col < GridColumns; col++)
                {
                    int x = col * frameWidth;
                    int y = (GridRows - 1 - row) * frameHeight;

                    rects[index] = new SpriteRect
                    {
                        name = $"{baseName}_{index:D2}",
                        spriteID = GUID.Generate(),
                        rect = new Rect(x, y, frameWidth, frameHeight),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };

                    index++;
                }
            }

            return rects;
        }

        private static bool TryGetSourceImageSize(string assetPath, out int width, out int height)
        {
            width = 0;
            height = 0;

            string projectRoot = Directory.GetCurrentDirectory();
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));

            if (!File.Exists(fullPath))
            {
                return false;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D temp = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                if (!temp.LoadImage(bytes, true))
                {
                    return false;
                }

                width = temp.width;
                height = temp.height;
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temp);
            }
        }

        private static Sprite[] LoadFrames(string path)
        {
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrEmpty(parent))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static string MakeKey(string speciesId, string costumeId)
        {
            return $"{speciesId}|{costumeId ?? string.Empty}";
        }

        private static string SanitizeToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "base";
            }

            char[] safe = raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(safe);
        }
    }
}