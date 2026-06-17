using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameplaySceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Gameplay_BeltMagnet.unity";
    private const string ArtFolder = "Assets/_Game/Art/Generated";
    private const string PrefabFolder = "Assets/_Game/Prefabs";
    private const string MaterialFolder = "Assets/_Game/Materials";

    [MenuItem("Tools/LoopSort/Create Gameplay Scene")]
    public static void CreateGameplayScene()
    {
        EnsureFolders();
        EnsureSortingLayer("Background");
        EnsureSortingLayer("Conveyor");
        EnsureSortingLayer("Seeds");
        EnsureSortingLayer("UI");

        Material seedMaterial = CreateColorMaterial("Seed_Instanced", Color.white, true);
        Mesh seedMesh = CreateSeedMesh();
        Material backgroundMaterial = CreateColorMaterial("Background", new Color(0.075f, 0.085f, 0.1f), false);
        Material beltMaterial = CreateColorMaterial("ConveyorBelt", new Color(0.18f, 0.19f, 0.22f), false);
        Material beltStripeMaterial = CreateColorMaterial("ConveyorStripe", new Color(0.32f, 0.34f, 0.38f), false);
        Material[] colorMaterials =
        {
            CreateColorMaterial("SeedBox_Red", GetColor(SeedColor.Red), false),
            CreateColorMaterial("SeedBox_Blue", GetColor(SeedColor.Blue), false),
            CreateColorMaterial("SeedBox_Yellow", GetColor(SeedColor.Yellow), false)
        };

        Seed seedPrefab = CreateSeedPrefab(seedMaterial, seedMesh);
        if (seedPrefab == null)
            throw new System.InvalidOperationException("Could not create or load Seed prefab component.");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Gameplay_BeltMagnet";

        CreateCamera();
        CreateLight();

        GameObject world = new GameObject("World");
        GameObject systems = new GameObject("Systems");
        GameObject uiRoot = new GameObject("UI");

        CreateBackground(backgroundMaterial, world.transform);
        CreateConveyor(beltMaterial, beltStripeMaterial, world.transform);

        ConveyorManager conveyor = CreateSystem<ConveyorManager>("ConveyorManager", systems.transform);
        SetSerialized(conveyor, "baseSpeed", 2.5f);

        SeedPool pool = CreateSystem<SeedPool>("SeedPool", systems.transform);
        Seed seedTemplate = CreateSeedTemplate(seedMaterial, seedMesh, pool.transform);
        SetSerialized(pool, "seedPrefab", seedTemplate);
        SetSerialized(pool, "poolSize", 250);
        SetSerialized(pool, "maxActiveSeeds", 180);

        CreateSeedBoxes(colorMaterials, world.transform);
        CreateTargetSlots(colorMaterials, world.transform);

        UIManager uiManager = CreateUi(uiRoot.transform);
        GameManager gameManager = CreateSystem<GameManager>("GameManager", systems.transform);
        SetSerialized(gameManager, "targetAmount", 30);
        SetSerialized(gameManager, "levelDuration", 60f);
        SetSerialized(gameManager, "uiManager", uiManager);

        Selection.activeObject = world;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created gameplay scene: " + ScenePath);
    }

    private static T CreateSystem<T>(string name, Transform parent) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<T>();
    }

    private static void CreateCamera()
    {
        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);

        Camera cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.075f, 0.085f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateLight()
    {
        GameObject lightGo = new GameObject("Directional Light");
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.7f;
    }

    private static void CreateBackground(Material material, Transform parent)
    {
        GameObject go = CreateQuad("Background", material, "Background", 0);
        go.transform.SetParent(parent);
        go.transform.position = Vector3.zero;
        go.transform.localScale = new Vector3(18f, 11f, 1f);
    }

    private static void CreateConveyor(Material beltMaterial, Material stripeMaterial, Transform parent)
    {
        GameObject belt = CreateQuad("Conveyor Belt", beltMaterial, "Conveyor", 0);
        belt.transform.SetParent(parent);
        belt.transform.position = new Vector3(1.1f, -1.9f, 0f);
        belt.transform.localScale = new Vector3(7.6f, 1.15f, 1f);

        for (int i = 0; i < 9; i++)
        {
            GameObject stripe = CreateQuad("Belt Stripe " + (i + 1), stripeMaterial, "Conveyor", 1);
            stripe.transform.SetParent(parent);
            stripe.transform.position = new Vector3(-5.6f + i * 1.45f, -1.9f, -0.02f);
            stripe.transform.localScale = new Vector3(0.08f, 0.62f, 1f);
            stripe.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
        }

        GameObject endZone = new GameObject("Offscreen Despawn Hint");
        endZone.transform.SetParent(parent);
        endZone.transform.position = new Vector3(10.8f, -1.9f, 0f);
    }

    private static void CreateSeedBoxes(Material[] colorMaterials, Transform parent)
    {
        SeedColor[] colors = { SeedColor.Red, SeedColor.Blue, SeedColor.Yellow };
        Vector3[] positions =
        {
            new Vector3(-6f, 3.7f, 0f),
            new Vector3(-4.8f, 3.7f, 0f),
            new Vector3(-3.6f, 3.7f, 0f)
        };

        for (int i = 0; i < colors.Length; i++)
        {
            GameObject box = CreateQuad(colors[i] + " SeedBox", colorMaterials[i], "UI", 0);
            box.transform.SetParent(parent);
            box.transform.position = positions[i];
            box.transform.localScale = Vector3.one * 0.85f;

            BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            SeedBox seedBox = box.AddComponent<SeedBox>();
            SetSerialized(seedBox, "boxColor", colors[i]);

            GameObject dropPoint = new GameObject("Belt Drop Point");
            dropPoint.transform.SetParent(box.transform);
            dropPoint.transform.position = new Vector3(-2.2f + i * 0.85f, -1.78f, 0f);
            SetSerialized(seedBox, "beltDropPoint", dropPoint.transform);
            SetSerialized(seedBox, "randomDropOffset", new Vector2(0.55f, 0.12f));
            SetSerialized(seedBox, "minSeedsPerBurst", 5);
            SetSerialized(seedBox, "maxSeedsPerBurst", 10);
            SetSerialized(seedBox, "burstInterval", 0.1f);
        }
    }

    private static void CreateTargetSlots(Material[] colorMaterials, Transform parent)
    {
        SeedColor[] colors = { SeedColor.Red, SeedColor.Blue, SeedColor.Yellow };
        Vector3[] positions =
        {
            new Vector3(-0.9f, -1.2f, 0f),
            new Vector3(1.1f, -1.2f, 0f),
            new Vector3(3.1f, -1.2f, 0f)
        };

        for (int i = 0; i < colors.Length; i++)
        {
            GameObject slot = CreateQuad(colors[i] + " TargetSlot", colorMaterials[i], "Conveyor", 3);
            slot.transform.SetParent(parent);
            slot.transform.position = positions[i];
            slot.transform.localScale = new Vector3(1.2f, 0.8f, 1f);

            CircleCollider2D collider = slot.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.22f;

            TargetSlot targetSlot = slot.AddComponent<TargetSlot>();
            SetSerialized(targetSlot, "slotColor", colors[i]);
            SetSerialized(targetSlot, "magnetRadius", 2.2f);
            SetSerialized(targetSlot, "magnetForce", 7.5f);
            SetSerialized(targetSlot, "collectDistance", 0.2f);

            GameObject particlesGo = CreateHitParticles(colors[i]);
            particlesGo.transform.SetParent(slot.transform);
            particlesGo.transform.localPosition = Vector3.zero;
            SetSerialized(targetSlot, "hitParticles", particlesGo.GetComponent<ParticleSystem>());
        }
    }

    private static UIManager CreateUi(Transform parent)
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(parent);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingLayerName = "UI";

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(parent);
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        UIManager manager = canvasGo.AddComponent<UIManager>();

        TMP_Text timer = CreateText(canvasGo.transform, "TimerText", "60", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), 58, TextAlignmentOptions.Center);
        TMP_Text progress = CreateText(canvasGo.transform, "ProgressText", "0/30", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -64f), 42, TextAlignmentOptions.Left);
        TMP_Text combo = CreateText(canvasGo.transform, "ComboText", "x1", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -64f), 42, TextAlignmentOptions.Right);
        TMP_Text result = CreateText(canvasGo.transform, "ResultText", "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 84, TextAlignmentOptions.Center);

        Slider slider = CreateProgressSlider(canvasGo.transform);

        SetSerialized(manager, "timerText", timer);
        SetSerialized(manager, "progressText", progress);
        SetSerialized(manager, "comboText", combo);
        SetSerialized(manager, "resultText", result);
        SetSerialized(manager, "progressSlider", slider);

        return manager;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, int size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(420f, 90f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Slider CreateProgressSlider(Transform parent)
    {
        GameObject root = new GameObject("ProgressSlider");
        root.transform.SetParent(parent, false);

        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -132f);
        rt.sizeDelta = new Vector2(520f, 22f);

        Slider slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;

        GameObject bg = CreateUiImage(root.transform, "Background", new Color(0.17f, 0.18f, 0.2f), Vector2.zero, Vector2.one);
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(3f, 3f);
        fillAreaRt.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = CreateUiImage(fillArea.transform, "Fill", new Color(0.25f, 0.9f, 0.58f), Vector2.zero, Vector2.one);
        slider.targetGraphic = bg.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();

        return slider;
    }

    private static GameObject CreateUiImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    private static Seed CreateSeedPrefab(Material material, Mesh seedMesh)
    {
        if (material == null)
            throw new System.ArgumentNullException(nameof(material));
        if (seedMesh == null)
            throw new System.ArgumentNullException(nameof(seedMesh));

        string path = PrefabFolder + "/Seed.prefab";

        GameObject seedGo = CreateSeedObject("Seed", material, seedMesh);

        PrefabUtility.SaveAsPrefabAsset(seedGo, path);
        Object.DestroyImmediate(seedGo);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null ? prefab.GetComponent<Seed>() : null;
    }

    private static Seed CreateSeedTemplate(Material material, Mesh seedMesh, Transform parent)
    {
        GameObject template = CreateSeedObject("Seed Template", material, seedMesh);
        template.transform.SetParent(parent);
        template.SetActive(false);
        return template.GetComponent<Seed>();
    }

    private static GameObject CreateSeedObject(string name, Material material, Mesh seedMesh)
    {
        GameObject seedGo = new GameObject(name);
        seedGo.name = name;
        seedGo.transform.localScale = Vector3.one * 0.22f;

        MeshFilter meshFilter = seedGo.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = seedMesh;

        MeshRenderer meshRenderer = seedGo.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerName = "Seeds";
        meshRenderer.sortingOrder = 0;

        Rigidbody2D rb = seedGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D collider = seedGo.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        TrailRenderer trail = seedGo.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.startWidth = 0.28f;
        trail.endWidth = 0f;
        trail.numCapVertices = 4;
        trail.material = material;
        trail.sortingLayerName = "Seeds";
        trail.enabled = false;
        trail.emitting = false;

        Seed seed = seedGo.AddComponent<Seed>();
        SetSerialized(seed, "visualRenderer", meshRenderer);
        SetSerialized(seed, "trailRenderer", trail);
        SetSerialized(seed, "enableTrail", false);
        SetSerialized(seed, "maxTrailActiveSeeds", 45);
        SetSerialized(seed, "despawnX", 10.7f);

        return seedGo;
    }

    private static GameObject CreateQuad(string name, Material material, string sortingLayer, int sortingOrder)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;

        return go;
    }

    private static GameObject CreateHitParticles(SeedColor color)
    {
        GameObject go = new GameObject("Hit Particles");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 0.22f;
        main.startSpeed = 2.8f;
        main.startSize = 0.12f;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.startColor = GetColor(color);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Seeds";
        renderer.sortingOrder = 10;

        return go;
    }

    private static Material CreateColorMaterial(string name, Color color, bool enableInstancing)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.enableInstancing = enableInstancing;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh CreateSeedMesh()
    {
        string path = ArtFolder + "/SeedCircleMesh.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "SeedCircleMesh";
            AssetDatabase.CreateAsset(mesh, path);
        }

        const int segments = 16;
        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uv = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            float x = Mathf.Cos(angle) * 0.5f;
            float y = Mathf.Sin(angle) * 0.5f;
            vertices[i + 1] = new Vector3(x, y, 0f);
            uv[i + 1] = new Vector2(x + 0.5f, y + 0.5f);
        }

        for (int i = 0; i < segments; i++)
        {
            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = i == segments - 1 ? 1 : i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static Material CreateSeedMaterial()
    {
        string path = MaterialFolder + "/Seed_Instanced.mat";
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Sprite CreateCircleSprite(string name, Color color, int size)
    {
        return CreateSprite(name, size, size, (x, y) =>
        {
            float r = size * 0.5f - 1f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            float alpha = Mathf.Clamp01(r - dist + 1f);
            return new Color(color.r, color.g, color.b, color.a * alpha);
        });
    }

    private static Sprite CreateRoundedRectSprite(string name, Color color, int width, int height, int radius)
    {
        return CreateSprite(name, width, height, (x, y) =>
        {
            if (radius <= 0)
                return color;

            float dx = Mathf.Max(radius - x, x - (width - radius - 1), 0);
            float dy = Mathf.Max(radius - y, y - (height - radius - 1), 0);
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Clamp01(radius - dist + 1f);
            return new Color(color.r, color.g, color.b, color.a * alpha);
        });
    }

    private static Sprite CreateSprite(string name, int width, int height, System.Func<int, int, Color> pixel)
    {
        string path = ArtFolder + "/" + name + ".asset";

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = name + "_Texture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, pixel(x, y));
        }

        texture.Apply();

        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(texture, path);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            64f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = name + "_Sprite";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite loadedSprite)
                return loadedSprite;
        }

        return null;
    }

    private static Color GetColor(SeedColor color)
    {
        switch (color)
        {
            case SeedColor.Red:
                return new Color(1f, 0.22f, 0.18f);
            case SeedColor.Blue:
                return new Color(0.18f, 0.45f, 1f);
            case SeedColor.Yellow:
                return new Color(1f, 0.86f, 0.18f);
            case SeedColor.Green:
                return new Color(0.2f, 0.85f, 0.35f);
            default:
                return Color.white;
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/_Game");
        EnsureFolder("Assets/_Game/Editor");
        EnsureFolder("Assets/_Game/Art");
        EnsureFolder(ArtFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder("Assets/_Game/Scenes");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void EnsureSortingLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

        for (int i = 0; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.FindPropertyRelative("name").stringValue == layerName)
                return;
        }

        layers.InsertArrayElementAtIndex(layers.arraySize);
        SerializedProperty newLayer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
        newLayer.FindPropertyRelative("name").stringValue = layerName;
        newLayer.FindPropertyRelative("uniqueID").intValue = Random.Range(100000, 999999999);
        newLayer.FindPropertyRelative("locked").boolValue = false;
        tagManager.ApplyModifiedProperties();
    }

    private static void SetSerialized(Object target, string propertyName, object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);

        if (prop == null)
        {
            Debug.LogWarning("Missing serialized field " + propertyName + " on " + target.name);
            return;
        }

        if (value is int intValue)
            prop.intValue = intValue;
        else if (value is float floatValue)
            prop.floatValue = floatValue;
        else if (value is bool boolValue)
            prop.boolValue = boolValue;
        else if (value is Vector2 vector2Value)
            prop.vector2Value = vector2Value;
        else if (value is SeedColor colorValue)
            prop.enumValueIndex = (int)colorValue;
        else if (value is Object objectValue)
            prop.objectReferenceValue = objectValue;
        else
            Debug.LogWarning("Unsupported serialized value for " + propertyName);

        so.ApplyModifiedProperties();
    }
}
