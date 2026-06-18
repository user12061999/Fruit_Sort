using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameplaySceneBuilder
{
    private const string GameplayScenePath = "Assets/_Game/Scenes/Gameplay.unity";
    private const string GeneratedFolder = "Assets/_Game/Generated";
    private const string DotPrefabPath = GeneratedFolder + "/Dot.prefab";
    private const string PixelSpritePath = GeneratedFolder + "/WhitePixel.png";
    private const string PatternPath = GeneratedFolder + "/PixelPattern.asset";
    private const string LineMaterialPath = GeneratedFolder + "/SpriteLine.mat";

    static GameplaySceneBuilder()
    {
        EditorApplication.delayCall += BuildOnceWhenReady;
    }

    [MenuItem("Tools/Fruit Sort/Build Gameplay Scene")]
    public static void BuildGameplayScene()
    {
        EnsureFolder("Assets/_Game", "Generated");
        EnsureFolder("Assets/_Game", "Scenes");

        Sprite pixelSprite = GetOrCreatePixelSprite();
        Material lineMaterial = GetOrCreateLineMaterial();
        Texture2D pattern = GetOrCreatePatternTexture();
        Dot dotPrefab = GetOrCreateDotPrefab(pixelSprite);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Gameplay";

        Camera camera = CreateCamera();
        ConveyorSpline conveyor = CreateConveyor(lineMaterial);
        FallingPixelManager fallingManager = CreateFallingManager(conveyor);
        PixelGridManager gridManager = CreatePixelGrid(dotPrefab, pattern);
        Shooter shooter = CreatePlayer(pixelSprite, lineMaterial);
        Bucket[] buckets = CreateBuckets(conveyor, pixelSprite);
        GameManager gameManager = CreateGameManager(gridManager, fallingManager, buckets);
        CreateHud(gameManager);

        Selection.activeGameObject = gridManager.gameObject;
        EditorSceneManager.SaveScene(scene, GameplayScenePath);
        AddSceneToBuildSettings(GameplayScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Gameplay scene created at {GameplayScenePath}. Camera: {camera.name}, " +
            $"Shooter: {shooter.name}, Buckets: {buckets.Length}.");
    }

    [MenuItem("Tools/Fruit Sort/Validate Gameplay Scene")]
    public static void ValidateGameplayScene()
    {
        if (!File.Exists(GameplayScenePath))
        {
            throw new FileNotFoundException("Gameplay scene does not exist.", GameplayScenePath);
        }

        EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        Camera camera = Object.FindFirstObjectByType<Camera>();
        ConveyorSpline conveyor = Object.FindFirstObjectByType<ConveyorSpline>();
        FallingPixelManager falling = Object.FindFirstObjectByType<FallingPixelManager>();
        PixelGridManager grid = Object.FindFirstObjectByType<PixelGridManager>();
        Shooter shooter = Object.FindFirstObjectByType<Shooter>();
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        Bucket[] buckets = Object.FindObjectsByType<Bucket>(FindObjectsSortMode.None);

        if (camera == null || !camera.orthographic)
            throw new MissingReferenceException("Gameplay requires an orthographic camera.");
        if (conveyor == null || conveyor.splineContainer == null ||
            conveyor.splineContainer.Spline.Count < 2)
            throw new MissingReferenceException("Conveyor spline is missing or invalid.");
        if (falling == null || falling.conveyor != conveyor)
            throw new MissingReferenceException("FallingPixelManager conveyor reference is invalid.");
        if (grid == null || grid.dotPrefab == null || grid.sourceTexture == null)
            throw new MissingReferenceException("PixelGridManager prefab or texture reference is missing.");
        if (grid.dotPrefab.GetComponent<Rigidbody2D>() != null)
            throw new UnityException("Dot prefab must not contain Rigidbody2D.");
        if (shooter == null || game == null)
            throw new MissingReferenceException("Shooter or GameManager is missing.");
        if (buckets.Length != 4)
            throw new UnityException($"Expected 4 buckets, found {buckets.Length}.");

        Debug.Log("Gameplay scene validation passed: camera, spline, managers, Dot prefab, " +
            "shooter, UI, and 4 buckets are wired correctly.");
    }

    private static void BuildOnceWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildOnceWhenReady;
            return;
        }

        if (!File.Exists(GameplayScenePath))
        {
            BuildGameplayScene();
        }
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.055f, 0.095f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        return camera;
    }

    private static ConveyorSpline CreateConveyor(Material lineMaterial)
    {
        GameObject root = new GameObject("Conveyor");
        SplineContainer container = root.AddComponent<SplineContainer>();
        Spline spline = container.Spline;
        spline.Clear();
        spline.Add(new BezierKnot(new float3(-6f, -2.8f, 0f)), TangentMode.AutoSmooth);
        spline.Add(new BezierKnot(new float3(-2f, -2.55f, 0f)), TangentMode.AutoSmooth);
        spline.Add(new BezierKnot(new float3(2f, -2.8f, 0f)), TangentMode.AutoSmooth);
        spline.Add(new BezierKnot(new float3(6f, -2.55f, 0f)), TangentMode.AutoSmooth);

        ConveyorSpline conveyor = root.AddComponent<ConveyorSpline>();
        conveyor.splineContainer = container;
        conveyor.beltWidth = 1.35f;

        GameObject visual = new GameObject("Belt Visual");
        visual.transform.SetParent(root.transform, false);
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.useWorldSpace = true;
        line.positionCount = 49;
        line.startWidth = conveyor.beltWidth;
        line.endWidth = conveyor.beltWidth;
        line.startColor = new Color(0.14f, 0.18f, 0.25f, 1f);
        line.endColor = line.startColor;
        line.sortingOrder = -5;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        for (int i = 0; i < line.positionCount; i++)
        {
            line.SetPosition(i, conveyor.GetPositionOnSpline(i / (line.positionCount - 1f), 0f));
        }

        return conveyor;
    }

    private static FallingPixelManager CreateFallingManager(ConveyorSpline conveyor)
    {
        GameObject managerObject = new GameObject("Falling Pixel Manager");
        FallingPixelManager manager = managerObject.AddComponent<FallingPixelManager>();
        manager.conveyor = conveyor;
        manager.fallSpeed = 4.5f;
        manager.beltSpeed = 1.5f;
        manager.dotSize = 0.38f;
        manager.cellSizeMultiplier = 1.2f;
        manager.separationStrength = 5f;
        manager.maxNeighborsPerDot = 12;
        manager.maxDots = 500;
        manager.bucketAttractSpeed = 8f;
        return manager;
    }

    private static PixelGridManager CreatePixelGrid(Dot dotPrefab, Texture2D pattern)
    {
        GameObject gridObject = new GameObject("Pixel Grid");
        gridObject.transform.position = new Vector3(0f, 2.5f, 0f);
        PixelGridManager manager = gridObject.AddComponent<PixelGridManager>();
        manager.dotPrefab = dotPrefab;
        manager.sourceTexture = pattern;
        manager.buildOnStart = true;
        manager.dotSize = 0.38f;
        manager.defaultHp = 3;
        manager.colorPalette.Add(new Color(1f, 0.25f, 0.3f, 1f));
        manager.colorPalette.Add(new Color(0.25f, 0.9f, 0.45f, 1f));
        manager.colorPalette.Add(new Color(0.25f, 0.55f, 1f, 1f));
        return manager;
    }

    private static Shooter CreatePlayer(Sprite pixelSprite, Material lineMaterial)
    {
        GameObject player = new GameObject("Player Shooter");
        player.transform.position = new Vector3(0f, -5.6f, 0f);
        player.transform.localScale = new Vector3(0.7f, 0.85f, 1f);
        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = pixelSprite;
        renderer.color = new Color(0.2f, 0.9f, 1f, 1f);
        renderer.sortingOrder = 5;

        LineRenderer shotLine = player.AddComponent<LineRenderer>();
        shotLine.material = lineMaterial;
        shotLine.startWidth = 0.06f;
        shotLine.endWidth = 0.02f;
        shotLine.startColor = Color.white;
        shotLine.endColor = new Color(0.3f, 0.9f, 1f, 0.15f);
        shotLine.sortingOrder = 10;
        shotLine.enabled = false;

        Shooter shooter = player.AddComponent<Shooter>();
        shooter.damage = 1;
        shooter.fireInterval = 0.13f;
        shooter.maxDistance = 12f;
        shooter.dotLayerMask = ~0;
        shooter.shotLine = shotLine;
        return shooter;
    }

    private static Bucket[] CreateBuckets(ConveyorSpline conveyor, Sprite pixelSprite)
    {
        Color[] colors =
        {
            new Color(1f, 0.25f, 0.3f, 1f),
            new Color(0.25f, 0.9f, 0.45f, 1f),
            new Color(0.25f, 0.55f, 1f, 1f),
            new Color(1f, 0.25f, 0.3f, 1f)
        };
        float[] progress = { 0.28f, 0.48f, 0.68f, 0.86f };
        int[] ids = { 0, 1, 2, 0 };
        Bucket[] buckets = new Bucket[colors.Length];

        GameObject root = new GameObject("Buckets");
        for (int i = 0; i < buckets.Length; i++)
        {
            GameObject bucketObject = new GameObject($"Bucket Color {ids[i]} ({i + 1})");
            bucketObject.transform.SetParent(root.transform);
            bucketObject.transform.localScale = new Vector3(0.78f, 0.9f, 1f);
            SpriteRenderer renderer = bucketObject.AddComponent<SpriteRenderer>();
            renderer.sprite = pixelSprite;
            renderer.color = colors[i];
            renderer.sortingOrder = 2;

            Bucket bucket = bucketObject.AddComponent<Bucket>();
            bucket.colorId = ids[i];
            bucket.maxFill = 5;
            bucket.currentFill = 0;
            bucket.conveyor = conveyor;
            bucket.splineProgress = progress[i];
            bucket.lateralOffset = -0.35f;
            bucket.attractRadius = 0.85f;
            bucket.followSplinePosition = true;
            bucketObject.transform.position = conveyor.GetPositionOnSpline(progress[i], bucket.lateralOffset);
            buckets[i] = bucket;
        }

        return buckets;
    }

    private static GameManager CreateGameManager(PixelGridManager gridManager,
        FallingPixelManager fallingManager, Bucket[] buckets)
    {
        GameObject managerObject = new GameObject("Game Manager");
        GameManager manager = managerObject.AddComponent<GameManager>();
        manager.pixelGridManager = gridManager;
        manager.fallingPixelManager = fallingManager;
        manager.buckets.AddRange(buckets);
        return manager;
    }

    private static void CreateHud(GameManager gameManager)
    {
        GameObject canvasObject = new GameObject("HUD Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameManager.scoreText = CreateText(canvas.transform, "Score Text", "Score: 0",
            new Vector2(25f, -25f), new Vector2(430f, 70f), font, 34);
        gameManager.levelText = CreateText(canvas.transform, "Level Text", "Level: 1",
            new Vector2(25f, -95f), new Vector2(430f, 70f), font, 34);
        gameManager.remainingDotsText = CreateText(canvas.transform, "Dots Text", "Dots: 0",
            new Vector2(25f, -165f), new Vector2(430f, 70f), font, 34);
        gameManager.bucketFillText = CreateText(canvas.transform, "Bucket Fill Text", string.Empty,
            new Vector2(25f, -250f), new Vector2(500f, 260f), font, 28);

        Text hint = CreateText(canvas.transform, "Hint Text", "CLICK / SPACE TO SHOOT",
            new Vector2(0f, 35f), new Vector2(700f, 70f), font, 30);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 35f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.65f, 0.85f, 1f, 0.9f);
    }

    private static Text CreateText(Transform parent, string name, string content,
        Vector2 anchoredPosition, Vector2 size, Font font, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        return text;
    }

    private static Dot GetOrCreateDotPrefab(Sprite sprite)
    {
        Dot existing = AssetDatabase.LoadAssetAtPath<Dot>(DotPrefabPath);
        if (existing != null)
        {
            return existing;
        }

        GameObject dotObject = new GameObject("Dot");
        SpriteRenderer renderer = dotObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;
        BoxCollider2D collider = dotObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one * 0.86f;
        Dot dot = dotObject.AddComponent<Dot>();
        dot.maxHp = 3;
        dot.currentHp = 3;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(dotObject, DotPrefabPath);
        Object.DestroyImmediate(dotObject);
        return prefab.GetComponent<Dot>();
    }

    private static Sprite GetOrCreatePixelSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(PixelSpritePath);
        if (existing != null)
        {
            return existing;
        }

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(PixelSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(PixelSpritePath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PixelSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(PixelSpritePath);
    }

    private static Texture2D GetOrCreatePatternTexture()
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(PatternPath);
        if (existing != null)
        {
            return existing;
        }

        Color[] palette =
        {
            new Color(1f, 0.25f, 0.3f, 1f),
            new Color(0.25f, 0.9f, 0.45f, 1f),
            new Color(0.25f, 0.55f, 1f, 1f)
        };
        Texture2D texture = new Texture2D(12, 7, TextureFormat.RGBA32, false);
        texture.name = "PixelPattern";
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                int colorIndex = (x / 2 + y) % palette.Length;
                bool cutCorner = (x == 0 || x == texture.width - 1) &&
                    (y == 0 || y == texture.height - 1);
                texture.SetPixel(x, y, cutCorner ? Color.clear : palette[colorIndex]);
            }
        }

        texture.Apply();
        AssetDatabase.CreateAsset(texture, PatternPath);
        return texture;
    }

    private static Material GetOrCreateLineMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader) { name = "SpriteLine" };
        AssetDatabase.CreateAsset(material, LineMaterialPath);
        return material;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes;
                return;
            }
        }

        var updated = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(updated, 0);
        updated[updated.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updated;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
