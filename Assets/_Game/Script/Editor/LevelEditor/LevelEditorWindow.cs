#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace FruitSort.EditorTools
{
    /// <summary>
    /// Tool dựng level hoàn chỉnh: Bucket + Spawner + SpawnerColumn + ConveyorSpline.
    /// Mở: menu  Tools > FruitSort > Level Editor.
    ///
    /// Workflow:
    ///  1. Gán (hoặc "Tạo mới...") LevelData asset, kiểm tra prefab config.
    ///  2. Chọn mode trên toolbar rồi CLICK trong Scene View để đặt entity.
    ///     - Conveyor: click liên tiếp thêm knot, Enter chốt, Esc huỷ.
    ///     - Edit Belt: click chọn băng -> kéo handle từng knot.
    ///  3. Validate -> sửa lỗi -> "Save Scene -> Asset".
    ///  4. Scene gameplay chỉ cần LevelLoader + LevelData để dựng lại.
    ///
    /// Save/Load quét theo COMPONENT (Bucket/ModelDotSpawner/Column/ConveyorSpline) trong
    /// scene — designer có thể chỉnh tay từng object bằng Inspector rồi Save lại bình thường.
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        enum Mode { Select, Bucket, Spawner, Column, Conveyor, EditBelt, Connect, Grinder, Obstacle, Delete }

        static readonly string[] ModeLabels = { "Select", "Bucket", "Spawner", "Column", "Conveyor", "Edit Belt", "Connect", "Grinder", "Obstacle", "Delete" };

        LevelData _level;
        Mode _mode = Mode.Select;

        // ---- Thông số đặt entity ----
        int _bucketColorId = 0;
        int _bucketMaxFill = 5;
        Vector2 _bucketLaunchDir = Vector2.down;

        int _spawnerColorId = -1;
        int _spawnerTotalDots = 50;
        int _spawnerSpawnCount = 10;
        Vector2 _spawnerLaunchDir = Vector2.down;
        float _spawnerLaunchSpeed = 10f;
        bool _attachSpawnerToColumn = true;
        float _attachRadius = 2.5f;

        int _obstacleRequiredDots = 10;

        float _newBeltWidth = 3f;
        bool _newBeltClosed = false;

        // ---- Snap ----
        bool _snap = true;
        float _snapSize = 0.5f;

        // ---- Trạng thái conveyor ----
        ConveyorSpline _drawing;   // băng đang vẽ (Conveyor mode)
        ConveyorSpline _editing;   // băng đang chỉnh (EditBelt mode)

        ConveyorSpline _connectFrom;
        Vector3 _cursorPreviewPosition;
        bool _hasCursorPreview;
        Vector2 _scroll;
        List<LevelIssue> _issues;
        bool _showPrefabs = true;

        [MenuItem("Tools/FruitSort/Level Editor")]
        public static void Open()
        {
            var w = GetWindow<LevelEditorWindow>("Level Editor");
            w.minSize = new Vector2(330, 520);
            w.Show();
        }

        void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; }
        void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; FinishDrawing(); }

        // =========================================================
        // Window UI
        // =========================================================

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawAssetSection();
            if (_level == null)
            {
                EditorGUILayout.HelpBox("Gán hoặc tạo một LevelData asset để bắt đầu.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawPrefabSection();
            EditorGUILayout.Space();
            DrawModeToolbar();
            EditorGUILayout.Space();
            DrawPlacementSettings();
            EditorGUILayout.Space();
            DrawSwitchSection();
            EditorGUILayout.Space();
            DrawSnapSection();
            EditorGUILayout.Space();
            DrawValidationSection();

            EditorGUILayout.EndScrollView();
        }

        void DrawAssetSection()
        {
            EditorGUILayout.LabelField("Level Asset", EditorStyles.boldLabel);
            _level = (LevelData)EditorGUILayout.ObjectField("Level Data", _level, typeof(LevelData), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Tạo mới...")) CreateNewAsset();
                using (new EditorGUI.DisabledScope(_level == null))
                {
                    if (GUILayout.Button("Load -> Scene")) LoadAssetToScene();
                    if (GUILayout.Button("Save Scene -> Asset")) SaveSceneToAsset();
                }
            }
            if (GUILayout.Button("Xoá toàn bộ entity level trong scene")) ClearSceneEntities(true);
        }

        void DrawPrefabSection()
        {
            _showPrefabs = EditorGUILayout.Foldout(_showPrefabs, "Prefab Config", true);
            if (!_showPrefabs) return;

            EditorGUI.BeginChangeCheck();
            var bucket = (Bucket)EditorGUILayout.ObjectField("Bucket", _level.bucketPrefab, typeof(Bucket), false);
            var spawner = (ModelDotSpawner)EditorGUILayout.ObjectField("Spawner", _level.spawnerPrefab, typeof(ModelDotSpawner), false);
            var column = (ModelDotSpawnerColumn)EditorGUILayout.ObjectField("Column", _level.columnPrefab, typeof(ModelDotSpawnerColumn), false);
            var conveyor = (ConveyorSpline)EditorGUILayout.ObjectField("Conveyor (tuỳ chọn)", _level.conveyorPrefab, typeof(ConveyorSpline), false);
            var obstacle = (DotObstacle)EditorGUILayout.ObjectField("Obstacle (tuỳ chọn)", _level.obstaclePrefab, typeof(DotObstacle), false);
            var grinder = (ConveyorGrinder)EditorGUILayout.ObjectField("Grinder", _level.grinderPrefab, typeof(ConveyorGrinder), false);
            var db = (FruitDatabase)EditorGUILayout.ObjectField("Fruit Database", _level.fruitDatabase, typeof(FruitDatabase), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_level, "Level Prefab Config");
                _level.bucketPrefab = bucket;
                _level.spawnerPrefab = spawner;
                _level.columnPrefab = column;
                _level.conveyorPrefab = conveyor;
                _level.obstaclePrefab = obstacle;
                _level.grinderPrefab = grinder;
                _level.fruitDatabase = db;
                EditorUtility.SetDirty(_level);
            }
        }

        void DrawModeToolbar()
        {
            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
            int cur = (int)_mode;
            int next = GUILayout.SelectionGrid(cur, ModeLabels, 4, GUILayout.Height(48));
            if (next != cur)
            {
                if (_mode == Mode.Conveyor) FinishDrawing();
                _mode = (Mode)next;
                _connectFrom = null;
                SceneView.RepaintAll();
            }
            EditorGUILayout.HelpBox(HelpFor(_mode), MessageType.Info);
        }

        static string HelpFor(Mode m)
        {
            switch (m)
            {
                case Mode.Select: return "Công cụ mặc định của Unity: click chọn, kéo move, Ctrl+D duplicate, Delete xoá. " +
                                         "Kéo VÒNG TRÒN ở đầu mũi tên để xoay hướng phóng dot (giữ Shift = snap 15°).";
                case Mode.Bucket: return "Click trong Scene để đặt Bucket với màu/sức chứa bên dưới.";
                case Mode.Spawner: return "Click để đặt gói Spawner. Nếu bật 'Gắn vào Column gần', spawner tự thành con của column trong bán kính.";
                case Mode.Column: return "Click để đặt Column (cột spawner). Sau đó dùng mode Spawner đặt các gói vào cột.";
                case Mode.Conveyor: return "Click liên tiếp để thêm knot cho băng mới. ENTER chốt băng, ESC huỷ băng đang vẽ.";
                case Mode.EditBelt: return "Click 1 băng để chọn, kéo handle tròn ở mỗi knot để uốn. Panel bên dưới chỉnh width/loop/bo góc.";
                case Mode.Obstacle: return "Click để đặt vật cản. Dot bị chặn (không tiêu hao); đủ số dot đè lên cùng lúc thì vật cản vỡ.";
                case Mode.Delete: return "Click gần 1 entity (bucket/spawner/column/băng/vật cản) để xoá nó.";
                default: return "";
            }
        }

        void DrawPlacementSettings()
        {
            switch (_mode)
            {
                case Mode.Bucket:
                    EditorGUILayout.LabelField("Bucket mới", EditorStyles.boldLabel);
                    _bucketColorId = ColorIdField("Màu", _bucketColorId, false);
                    _bucketMaxFill = Mathf.Max(1, EditorGUILayout.IntField("Sức chứa (maxFill)", _bucketMaxFill));
                    _bucketLaunchDir = EditorGUILayout.Vector2Field("Hướng nhả dot", _bucketLaunchDir);
                    break;

                case Mode.Spawner:
                    EditorGUILayout.LabelField("Spawner mới", EditorStyles.boldLabel);
                    _spawnerColorId = ColorIdField("Màu", _spawnerColorId, true);
                    _spawnerTotalDots = Mathf.Max(1, EditorGUILayout.IntField("Tổng dot trong gói", _spawnerTotalDots));
                    _spawnerSpawnCount = Mathf.Max(1, EditorGUILayout.IntField("Dot mỗi click", _spawnerSpawnCount));
                    _spawnerLaunchDir = EditorGUILayout.Vector2Field("Hướng phóng dot", _spawnerLaunchDir);
                    _spawnerLaunchSpeed = Mathf.Max(0.1f, EditorGUILayout.FloatField("Tốc độ phóng", _spawnerLaunchSpeed));
                    _attachSpawnerToColumn = EditorGUILayout.Toggle("Gắn vào Column gần", _attachSpawnerToColumn);
                    if (_attachSpawnerToColumn)
                        _attachRadius = EditorGUILayout.Slider("Bán kính gắn", _attachRadius, 0.5f, 8f);
                    break;

                case Mode.Obstacle:
                    EditorGUILayout.LabelField("Vật cản mới", EditorStyles.boldLabel);
                    _obstacleRequiredDots = Mathf.Max(1, EditorGUILayout.IntField("Số dot đè lên để vỡ", _obstacleRequiredDots));
                    break;

                case Mode.Conveyor:
                    EditorGUILayout.LabelField("Băng mới", EditorStyles.boldLabel);
                    _newBeltWidth = EditorGUILayout.Slider("Bề rộng", _newBeltWidth, 0.2f, 10f);
                    _newBeltClosed = EditorGUILayout.Toggle("Khép kín (loop)", _newBeltClosed);
                    if (_drawing != null)
                    {
                        EditorGUILayout.HelpBox($"Đang vẽ: {_drawing.name} ({_drawing.Container.Spline.Count} knot)", MessageType.None);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Chốt băng (Enter)")) FinishDrawing();
                            if (GUILayout.Button("Huỷ (Esc)")) CancelDrawing();
                        }
                    }
                    break;

                case Mode.EditBelt:
                    EditorGUILayout.LabelField("Băng đang chọn", EditorStyles.boldLabel);
                    if (_editing == null)
                    {
                        EditorGUILayout.LabelField("(click 1 băng trong Scene để chọn)");
                        break;
                    }
                    EditorGUILayout.ObjectField("Selected", _editing, typeof(ConveyorSpline), true);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Insert knot")) InsertKnotMidLongest(_editing);
                        if (GUILayout.Button("Remove last")) RemoveLastKnot(_editing);
                    }

                    EditorGUI.BeginChangeCheck();
                    float w = EditorGUILayout.Slider("Bề rộng", _editing.beltWidth, 0.2f, 10f);
                    float corner = EditorGUILayout.Slider("Bo góc", _editing.cornerRadius, 0f, 3f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_editing, "Edit Belt");
                        _editing.beltWidth = w;
                        _editing.cornerRadius = corner;
                        _editing.Bake();
                        EditorUtility.SetDirty(_editing);
                    }

                    bool closed = EditorGUILayout.Toggle("Khép kín (loop)", _editing.IsClosed);
                    if (closed != _editing.IsClosed)
                    {
                        Undo.RecordObject(_editing.Container, "Toggle Belt Loop");
                        _editing.Container.Spline.Closed = closed;
                        _editing.Bake();
                        EditorUtility.SetDirty(_editing.Container);
                        MarkDirty();
                    }

                    DrawSelectedConveyorComponentSettings();
                    break;
            }
        }

        void DrawSelectedConveyorComponentSettings()
        {
            if (_editing == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Component settings", EditorStyles.boldLabel);
            bool changed = false;
            changed |= DrawAllSerializedProperties(_editing);
            changed |= DrawAllSerializedProperties(_editing.GetComponent<ConveyorBeltRenderer>());
            changed |= DrawAllSerializedProperties(_editing.GetComponent<ConveyorConnections>());

            if (!changed)
                return;

            _editing.Bake();
            ConveyorConnections.RebuildNetwork(_editing);
            EditorUtility.SetDirty(_editing);
            MarkDirty();
        }

        static bool DrawAllSerializedProperties(Component component)
        {
            if (component == null)
                return false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(component.GetType().Name, EditorStyles.boldLabel);
                var serialized = new SerializedObject(component);
                serialized.Update();
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyPath == "m_Script")
                        continue;

                    EditorGUILayout.PropertyField(property, true);
                }

                return serialized.ApplyModifiedProperties();
            }
        }

        void DrawSwitchSection()
        {
            if (_mode != Mode.EditBelt || _editing == null) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ConveyorSwitch", EditorStyles.boldLabel);

            ConveyorSwitch routeSwitch = _editing.GetComponent<ConveyorSwitch>();
            bool current = routeSwitch != null;
            bool requested = EditorGUILayout.Toggle("Bật switch định tuyến", current);
            if (requested != current)
            {
                if (requested)
                    routeSwitch = Undo.AddComponent<ConveyorSwitch>(_editing.gameObject);
                else
                    Undo.DestroyObjectImmediate(routeSwitch);
                MarkDirty();
                current = requested;
            }
            if (!current)
            {
                EditorGUILayout.HelpBox("Bật switch để cấu hình nhánh và các thông số ConveyorSwitch.", MessageType.None);
                return;
            }

            if (DrawAllSerializedProperties(routeSwitch))
            {
                ConveyorConnections.RebuildNetwork(_editing);
                EditorUtility.SetDirty(routeSwitch);
                MarkDirty();
            }

            var connections = _editing.GetComponent<ConveyorConnections>();
            EditorGUILayout.LabelField("Nhánh switch", EditorStyles.boldLabel);
            if (GUILayout.Button("Thêm nhánh (Connect mode)"))
            {
                _connectFrom = _editing;
                _mode = Mode.Connect;
                Notify("Click conveyor đích để thêm nhánh switch");
                SceneView.RepaintAll();
            }

            for (int i = connections.next.Count - 1; i >= 0; i--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(connections.next[i], typeof(ConveyorSpline), true);
                    if (GUILayout.Button("Xóa", GUILayout.Width(46)))
                    {
                        Undo.RecordObject(connections, "Remove Switch Branch");
                        connections.next.RemoveAt(i);
                        MarkDirty();
                    }
                }
            }
        }

        void DrawSnapSection()
        {
            EditorGUILayout.LabelField("Snap", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _snap = EditorGUILayout.Toggle("Snap grid", _snap);
                if (_snap) _snapSize = EditorGUILayout.FloatField(_snapSize, GUILayout.Width(60));
            }
        }

        void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validate", EditorStyles.boldLabel);
            if (GUILayout.Button("Kiểm tra level trong scene"))
                _issues = LevelValidator.ValidateScene(_level);

            if (_issues == null) return;
            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Không có lỗi. Level sẵn sàng để Save.", MessageType.Info);
                return;
            }
            foreach (var issue in _issues)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(issue.message, issue.type);
                    if (issue.context != null &&
                        GUILayout.Button("Chọn", GUILayout.Width(46), GUILayout.Height(38)))
                    {
                        EditorGUIUtility.PingObject(issue.context);
                        Selection.activeObject = issue.context;
                    }
                }
            }
        }

        /// <summary>Dropdown chọn colorId theo FruitDatabase (fallback IntField nếu chưa có DB).</summary>
        int ColorIdField(string label, int current, bool allowRandom)
        {
            var db = _level.fruitDatabase;
            if (db == null || db.fruits == null || db.fruits.Length == 0)
                return EditorGUILayout.IntField(label + " (colorId)", current);

            var ids = new List<int>();
            var names = new List<string>();
            if (allowRandom) { ids.Add(-1); names.Add("Random"); }
            foreach (var f in db.fruits)
            {
                if (f == null) continue;
                ids.Add(f.colorId);
                names.Add($"{f.colorId}: {f.fruitName}");
            }
            int idx = Mathf.Max(0, ids.IndexOf(current));
            idx = EditorGUILayout.Popup(label, idx, names.ToArray());
            return ids[idx];
        }

        // =========================================================
        // Scene View
        // =========================================================

        void OnSceneGUI(SceneView sv)
        {
            if (_level == null) return;

            Event e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                _cursorPreviewPosition = Snap(MouseWorld(e));
                _hasCursorPreview = true;
                sv.Repaint();
            }

            DrawSceneOverlays();
            DrawCursorPreview();

            if (_mode == Mode.Select)
            {
                DrawLaunchDirHandles();
                return;
            }

            int ctrl = GUIUtility.GetControlID(FocusType.Passive);
            if (_mode != Mode.EditBelt)
                HandleUtility.AddDefaultControl(ctrl); // nuốt click để không deselect/chọn nhầm

            if (_mode == Mode.EditBelt && _editing != null)
                DrawKnotHandles(_editing);

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Vector3 raw = MouseWorld(e);
                Vector3 wp = Snap(raw);
                switch (_mode)
                {
                    case Mode.Bucket: PlaceBucket(wp); e.Use(); break;
                    case Mode.Spawner: PlaceSpawner(wp); e.Use(); break;
                    case Mode.Column: PlaceColumn(wp); e.Use(); break;
                    case Mode.Obstacle: PlaceObstacle(wp); e.Use(); break;
                    case Mode.Conveyor: PlaceKnot(wp); e.Use(); break;
                    case Mode.EditBelt: _editing = PickConveyor(raw); Repaint(); e.Use(); break;
                    case Mode.Connect: ConnectAt(raw); e.Use(); break;
                    case Mode.Grinder: PlaceGrinder(wp); e.Use(); break;
                    case Mode.Delete: DeleteAt(raw); e.Use(); break;
                }
            }
            else if (e.type == EventType.KeyDown && _mode == Mode.Conveyor)
            {
                if (e.keyCode == KeyCode.Return) { FinishDrawing(); e.Use(); }
                else if (e.keyCode == KeyCode.Escape && _drawing != null) { CancelDrawing(); e.Use(); }
            }
        }

        Vector3 Snap(Vector3 p)
        {
            if (!_snap || _snapSize <= 0f) return p;
            p.x = Mathf.Round(p.x / _snapSize) * _snapSize;
            p.y = Mathf.Round(p.y / _snapSize) * _snapSize;
            p.z = 0f;
            return p;
        }

        static Vector3 MouseWorld(Event e)
        {
            Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float denom = Mathf.Approximately(r.direction.z, 0f) ? 1e-5f : r.direction.z;
            float t = -r.origin.z / denom;
            Vector3 p = r.origin + r.direction * t;
            p.z = 0f;
            return p;
        }

        // ---------------- Đặt entity ----------------

        void PlaceBucket(Vector3 pos)
        {
            if (_level.bucketPrefab == null) { Notify("Chưa gán Bucket prefab trong LevelData"); return; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(_level.bucketPrefab.gameObject);
            Undo.RegisterCreatedObjectUndo(go, "Place Bucket");
            go.transform.position = pos;
            go.name = $"Bucket_c{_bucketColorId}";

            var b = go.GetComponent<Bucket>();
            b.colorId = _bucketColorId;
            b.maxFill = _bucketMaxFill;
            if (_bucketLaunchDir.sqrMagnitude > 0.0001f) b.launchDirection = _bucketLaunchDir;
            if (b.fruitDatabase == null) b.fruitDatabase = _level.fruitDatabase;
            b.BindInputConveyor(PickConveyor(b.MouthPosition, float.MaxValue));
            b.RefreshVisuals();
            MarkPrefabOverride(b);
            MarkPrefabOverride(b.fruitSprite);  // sprite quả đổi theo colorId
            MarkPrefabOverride(b.body);
            MarkPrefabOverride(b.background);

            Selection.activeGameObject = go;
            MarkDirty();
        }

        void PlaceSpawner(Vector3 pos)
        {
            if (_level.spawnerPrefab == null) { Notify("Chưa gán Spawner prefab trong LevelData"); return; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(_level.spawnerPrefab.gameObject);
            Undo.RegisterCreatedObjectUndo(go, "Place Spawner");
            go.transform.position = pos;
            go.name = _spawnerColorId >= 0 ? $"Spawner_c{_spawnerColorId}" : "Spawner_random";

            var s = go.GetComponent<ModelDotSpawner>();
            s.fixedColorId = _spawnerColorId;
            s.totalClicks = _spawnerTotalDots;
            s.spawnCount = _spawnerSpawnCount;
            if (_spawnerLaunchDir.sqrMagnitude > 0.0001f) s.launchDirection = _spawnerLaunchDir;
            s.launchSpeed = _spawnerLaunchSpeed;
            if (s.fruitDatabase == null) s.fruitDatabase = _level.fruitDatabase;
            s.RefreshVisuals();
            MarkPrefabOverride(s);
            MarkPrefabOverride(s.packageSprite);

            if (_attachSpawnerToColumn)
            {
                var col = NearestColumn(pos, _attachRadius);
                if (col != null)
                {
                    Undo.SetTransformParent(go.transform, col.transform, "Attach Spawner To Column");
                    Notify($"Đã gắn vào {col.name}");
                }
            }

            Selection.activeGameObject = go;
            MarkDirty();
        }

        void PlaceColumn(Vector3 pos)
        {
            GameObject go;
            if (_level.columnPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(_level.columnPrefab.gameObject);
            }
            else
            {
                go = new GameObject();
                go.AddComponent<ModelDotSpawnerColumn>();
            }
            Undo.RegisterCreatedObjectUndo(go, "Place Column");
            go.name = "SpawnerColumn";
            go.transform.position = pos;

            Selection.activeGameObject = go;
            MarkDirty();
        }

        void PlaceObstacle(Vector3 pos)
        {
            if (_level.obstaclePrefab == null) { Notify("Chưa gán Obstacle prefab trong LevelData"); return; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(_level.obstaclePrefab.gameObject);
            Undo.RegisterCreatedObjectUndo(go, "Place Obstacle");
            go.transform.position = pos;
            go.name = $"Obstacle_x{_obstacleRequiredDots}";

            var o = go.GetComponent<DotObstacle>();
            o.requiredDots = _obstacleRequiredDots;
            MarkPrefabOverride(o);
            MarkPrefabOverride(o.body);

            Selection.activeGameObject = go;
            MarkDirty();
        }

        void PlaceGrinder(Vector3 pos)
        {
            GameObject go = _level.grinderPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(_level.grinderPrefab.gameObject)
                : new GameObject("Grinder");
            Undo.RegisterCreatedObjectUndo(go, "Place Grinder");
            go.name = "Grinder";
            go.transform.position = pos;
            if (go.GetComponent<ConveyorGrinder>() == null) go.AddComponent<ConveyorGrinder>();
            Selection.activeGameObject = go;
            MarkDirty();
        }

        static ModelDotSpawnerColumn NearestColumn(Vector3 pos, float maxDist)
        {
            ModelDotSpawnerColumn best = null;
            float bestD = maxDist;
            foreach (var c in Object.FindObjectsByType<ModelDotSpawnerColumn>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(c.transform.position, pos);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        // ---------------- Conveyor ----------------

        void PlaceKnot(Vector3 wp)
        {
            if (_drawing == null) _drawing = CreateConveyor(wp);
            else AppendKnot(_drawing, wp);
            Repaint();
        }

        ConveyorSpline CreateConveyor(Vector3 firstKnot)
        {
            GameObject go;
            if (_level.conveyorPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(_level.conveyorPrefab.gameObject);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                go = new GameObject();
            }
            go.name = "Conveyor";
            Undo.RegisterCreatedObjectUndo(go, "Create Conveyor");

            var container = go.GetComponent<SplineContainer>();
            if (container == null) container = go.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            spline.Closed = _newBeltClosed;
            spline.Add(new BezierKnot((float3)go.transform.InverseTransformPoint(firstKnot)), TangentMode.AutoSmooth);

            var conv = go.GetComponent<ConveyorSpline>();
            if (conv == null) conv = go.AddComponent<ConveyorSpline>();
            conv.beltWidth = _newBeltWidth;
            if (go.GetComponent<ConveyorConnections>() == null) go.AddComponent<ConveyorConnections>();

            MarkDirty();
            return conv;
        }

        void AppendKnot(ConveyorSpline conv, Vector3 world)
        {
            Undo.RecordObject(conv.Container, "Add Knot");
            conv.Container.Spline.Add(new BezierKnot((float3)conv.transform.InverseTransformPoint(world)), TangentMode.AutoSmooth);
            conv.Bake();
            var renderer = conv.GetComponent<ConveyorBeltRenderer>();
            if (renderer != null) renderer.RebuildMeshAndMaterials();
            MarkDirty();
        }

        void FinishDrawing()
        {
            if (_drawing == null) return;
            if (_drawing.Container.Spline.Count < 2)
            {
                Undo.DestroyObjectImmediate(_drawing.gameObject); // băng 1 knot -> bỏ
            }
            else
            {
                Undo.RecordObject(_drawing.Container, "Set Belt Loop");
                _drawing.Container.Spline.Closed = _newBeltClosed;
                _drawing.Bake();
                Selection.activeGameObject = _drawing.gameObject;
            }
            _drawing = null;
            MarkDirty();
        }

        void CancelDrawing()
        {
            if (_drawing != null) Undo.DestroyObjectImmediate(_drawing.gameObject);
            _drawing = null;
            MarkDirty();
        }

        void InsertKnotMidLongest(ConveyorSpline conv)
        {
            var spline = conv.Container.Spline;
            if (spline.Count < 2) return;
            int bestI = 0; float best = -1f;
            for (int i = 0; i < spline.Count - 1; i++)
            {
                float d = math.distance(spline[i].Position, spline[i + 1].Position);
                if (d > best) { best = d; bestI = i; }
            }
            float3 mid = (spline[bestI].Position + spline[bestI + 1].Position) * 0.5f;
            Undo.RecordObject(conv.Container, "Insert Knot");
            spline.Insert(bestI + 1, new BezierKnot(mid), TangentMode.AutoSmooth);
            conv.Bake();
            MarkDirty();
        }

        void RemoveLastKnot(ConveyorSpline conv)
        {
            var spline = conv.Container.Spline;
            if (spline.Count <= 2) return;
            Undo.RecordObject(conv.Container, "Remove Knot");
            spline.RemoveAt(spline.Count - 1);
            conv.Bake();
            MarkDirty();
        }

        void DrawKnotHandles(ConveyorSpline conv)
        {
            var spline = conv.Container.Spline;
            for (int i = 0; i < spline.Count; i++)
            {
                Vector3 world = conv.transform.TransformPoint((Vector3)spline[i].Position);
                EditorGUI.BeginChangeCheck();
                float size = HandleUtility.GetHandleSize(world) * 0.12f;
                Vector3 moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    moved = Snap(moved);
                    Undo.RecordObject(conv.Container, "Move Knot");
                    var k = spline[i];
                    k.Position = (float3)conv.transform.InverseTransformPoint(moved);
                    spline[i] = k;
                    spline.SetTangentMode(i, TangentMode.AutoSmooth);
                    conv.Bake();
                    MarkDirty();
                }
            }
        }

        ConveyorSpline PickConveyor(Vector3 world, float maxDist = 1.0f)
        {
            ConveyorSpline best = null;
            float bestD = maxDist;
            foreach (var c in Object.FindObjectsByType<ConveyorSpline>(FindObjectsSortMode.None))
            {
                float d = DistanceToSpline(c, world);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        static float DistanceToSpline(ConveyorSpline conv, Vector3 world)
        {
            conv.FindClosestProgress(world, out float dist);
            return dist;
        }

        void ConnectAt(Vector3 world)
        {
            if (_connectFrom == null)
            {
                _connectFrom = PickConveyor(world);
                if (_connectFrom != null) Notify("Selected source conveyor");
                return;
            }

            ConveyorSpline target = PickConveyor(world);
            if (target != null && target != _connectFrom)
            {
                ConveyorConnections connections = _connectFrom.GetComponent<ConveyorConnections>();
                Undo.RecordObject(connections, "Connect Conveyor");
                if (!connections.next.Contains(target)) connections.next.Add(target);
                connections.terminalGrinder = null;
                _connectFrom = null;
                MarkDirty();
                return;
            }

            ConveyorGrinder grinder = NearestGrinder(world, 1f);
            if (grinder == null) return;
            ConveyorConnections grinderConnections = _connectFrom.GetComponent<ConveyorConnections>();
            Undo.RecordObject(grinderConnections, "Connect Grinder");
            grinderConnections.terminalGrinder = grinder;
            _connectFrom = null;
            MarkDirty();
        }

        static ConveyorGrinder NearestGrinder(Vector3 position, float maxDistance)
        {
            ConveyorGrinder best = null;
            float bestDistance = maxDistance;
            foreach (ConveyorGrinder grinder in Object.FindObjectsByType<ConveyorGrinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float distance = Vector2.Distance(grinder.transform.position, position);
                if (distance < bestDistance) { bestDistance = distance; best = grinder; }
            }
            return best;
        }

        // ---------------- Delete ----------------

        void DeleteAt(Vector3 wp)
        {
            GameObject target = null;
            float best = 0.8f; // ngưỡng pick (world unit)

            foreach (var b in Object.FindObjectsByType<Bucket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(b.transform.position, wp);
                if (d < best) { best = d; target = b.gameObject; }
            }
            foreach (var s in Object.FindObjectsByType<ModelDotSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(s.transform.position, wp);
                if (d < best) { best = d; target = s.gameObject; }
            }
            foreach (var c in Object.FindObjectsByType<ModelDotSpawnerColumn>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(c.transform.position, wp);
                if (d < best) { best = d; target = c.gameObject; }
            }
            foreach (var o in Object.FindObjectsByType<DotObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(o.transform.position, wp);
                if (d < best) { best = d; target = o.gameObject; }
            }
            foreach (var grinder in Object.FindObjectsByType<ConveyorGrinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(grinder.transform.position, wp);
                if (d < best) { best = d; target = grinder.gameObject; }
            }
            foreach (var c in Object.FindObjectsByType<ConveyorSpline>(FindObjectsSortMode.None))
            {
                float d = DistanceToSpline(c, wp);
                if (d < best) { best = d; target = c.gameObject; }
            }

            if (target == null) return;
            if (_editing != null && target == _editing.gameObject) _editing = null;
            if (_drawing != null && target == _drawing.gameObject) _drawing = null;
            Undo.DestroyObjectImmediate(target);
            MarkDirty();
        }

        // ---------------- Overlay vẽ trong Scene ----------------

        void DrawSceneOverlays()
        {
            foreach (var grinder in Object.FindObjectsByType<ConveyorGrinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Color color = new Color(0.9f, 0.25f, 0.15f);
                Handles.color = color;
                Handles.DrawWireCube(grinder.transform.position, new Vector3(1.1f, 0.8f, 0f));
                Handles.Label(grinder.transform.position + Vector3.up * 0.55f, "Grinder", LabelStyle(color));
            }

            // Băng chuyền: 2 mép + đầu/cuối + link
            foreach (var c in Object.FindObjectsByType<ConveyorSpline>(FindObjectsSortMode.None))
            {
                var spline = c.Container != null ? c.Container.Spline : null;
                if (spline == null || spline.Count < 2) continue;

                Handles.color = (c == _editing) ? Color.yellow
                              : (c == _drawing) ? new Color(1f, 0.6f, 0.1f) : Color.cyan;
                const int seg = 64;
                Vector3 prevL = Vector3.zero, prevR = Vector3.zero;
                for (int i = 0; i <= seg; i++)
                {
                    float t = i / (float)seg;
                    Vector3 l = c.GetPositionOnSpline(t, +c.HalfWidth);
                    Vector3 r = c.GetPositionOnSpline(t, -c.HalfWidth);
                    if (i > 0) { Handles.DrawLine(prevL, l); Handles.DrawLine(prevR, r); }
                    prevL = l; prevR = r;
                }

                Vector3 start = c.GetPositionOnSpline(0f, 0f);
                Vector3 end = c.GetPositionOnSpline(1f, 0f);
                Handles.color = Color.green; Handles.DrawSolidDisc(start, Vector3.forward, 0.12f);
                Handles.color = Color.red; Handles.DrawSolidDisc(end, Vector3.forward, 0.12f);

                var conn = c.GetComponent<ConveyorConnections>();
                if (conn != null)
                {
                    Handles.color = Color.magenta;
                    foreach (var nx in conn.next)
                    {
                        if (nx == null) continue;
                        Handles.DrawLine(end, nx.GetPositionOnSpline(0f, 0f));
                    }
                }

                // Switch định tuyến: vòng vàng + label tại cuối băng.
                var routeSwitch = c.GetComponent<ConveyorSwitch>();
                if (routeSwitch != null)
                {
                    Color swCol = new Color(1f, 0.85f, 0.2f);
                    Handles.color = swCol;
                    Handles.DrawWireDisc(end, Vector3.forward, routeSwitch.clickRadius);
                    Handles.Label(end + Vector3.up * (routeSwitch.clickRadius + 0.2f),
                        $"Switch ({routeSwitch.ValidBranchCount} nhánh)", LabelStyle(swCol));
                }
            }

            // Bucket: vòng tròn màu + label
            foreach (var b in Object.FindObjectsByType<Bucket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Color col = ColorFor(b.colorId);
                Handles.color = col;
                Handles.DrawWireDisc(b.transform.position, Vector3.forward, 0.5f);
                Handles.Label(b.transform.position + Vector3.up * 0.6f,
                    $"Bucket c{b.colorId} ({b.maxFill})", LabelStyle(col));
                DrawLaunchArrow(b.MouthPosition, b.launchDirection, col);
            }

            // Spawner: khung vuông màu + label
            foreach (var s in Object.FindObjectsByType<ModelDotSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Color col = s.fixedColorId >= 0 ? ColorFor(s.fixedColorId) : Color.white;
                Handles.color = col;
                Vector3 p = s.transform.position;
                Handles.DrawWireCube(p, new Vector3(0.7f, 0.7f, 0f));
                string label = s.fixedColorId >= 0 ? $"Spawn c{s.fixedColorId} x{s.totalClicks}" : $"Spawn rnd x{s.totalClicks}";
                Handles.Label(p + Vector3.up * 0.5f, label, LabelStyle(col));
                Vector3 origin = s.spawnOrigin != null ? s.spawnOrigin.position : p;
                DrawLaunchArrow(origin, s.launchDirection, col);
            }

            // Column: label
            foreach (var col in Object.FindObjectsByType<ModelDotSpawnerColumn>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Handles.color = Color.gray;
                Handles.DrawWireCube(col.transform.position, new Vector3(1f, 1f, 0f));
                int n = col.GetComponentsInChildren<ModelDotSpawner>(true).Length;
                Handles.Label(col.transform.position + Vector3.up * 0.8f, $"Column ({n} gói)", LabelStyle(Color.gray));
            }

            // Obstacle: khung AABB vùng chặn + label
            foreach (var o in Object.FindObjectsByType<DotObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Color col = new Color(1f, 0.35f, 0.35f);
                Handles.color = col;
                if (o.TryGetWorldBounds(out Bounds b))
                    Handles.DrawWireCube(b.center, new Vector3(b.size.x, b.size.y, 0f));
                Handles.Label(o.transform.position + Vector3.up * 0.6f,
                    $"Obstacle x{o.requiredDots}", LabelStyle(col));
            }
        }

        // ---------------- Handle xoay hướng phóng (Select mode) ----------------

        /// <summary>Vẽ handle kéo ở đầu mũi tên cho mọi Bucket/Spawner để xoay launchDirection.</summary>
        void DrawCursorPreview()
        {
            if (!_hasCursorPreview) return;
            Vector3 p = _cursorPreviewPosition;
            Color color;

            switch (_mode)
            {
                case Mode.Bucket:
                    color = ColorFor(_bucketColorId);
                    Handles.color = color;
                    Handles.DrawWireDisc(p, Vector3.forward, 0.5f);
                    Handles.Label(p + Vector3.up * 0.6f, "Bucket", LabelStyle(color));
                    break;
                case Mode.Spawner:
                    color = _spawnerColorId >= 0 ? ColorFor(_spawnerColorId) : Color.white;
                    Handles.color = color;
                    Handles.DrawWireCube(p, new Vector3(0.7f, 0.7f, 0f));
                    Handles.Label(p + Vector3.up * 0.5f, "Spawner", LabelStyle(color));
                    break;
                case Mode.Column:
                    Handles.color = Color.gray;
                    Handles.DrawWireCube(p, new Vector3(1f, 1f, 0f));
                    Handles.Label(p + Vector3.up * 0.7f, "Column", LabelStyle(Color.gray));
                    break;
                case Mode.Obstacle:
                    color = new Color(1f, 0.35f, 0.35f);
                    Handles.color = color;
                    Handles.DrawWireCube(p, new Vector3(1f, 1f, 0f));
                    Handles.Label(p + Vector3.up * 0.6f, "Obstacle", LabelStyle(color));
                    break;
                case Mode.Grinder:
                    color = new Color(0.9f, 0.25f, 0.15f);
                    Handles.color = color;
                    Handles.DrawWireCube(p, new Vector3(1.1f, 0.8f, 0f));
                    Handles.Label(p + Vector3.up * 0.55f, "Grinder", LabelStyle(color));
                    break;
                case Mode.Conveyor:
                    Handles.color = Color.cyan;
                    Handles.DrawSolidDisc(p, Vector3.forward, 0.12f);
                    if (_drawing != null && _drawing.Container.Spline.Count > 0)
                    {
                        var spline = _drawing.Container.Spline;
                        Vector3 last = _drawing.transform.TransformPoint((Vector3)spline[spline.Count - 1].Position);
                        Handles.DrawDottedLine(last, p, 4f);
                    }
                    break;
                case Mode.Connect:
                    if (_connectFrom == null) break;
                    Handles.color = Color.magenta;
                    Handles.DrawDottedLine(_connectFrom.GetPositionOnSpline(1f, 0f), p, 4f);
                    break;
            }
        }

        void DrawLaunchDirHandles()
        {
            foreach (var b in Object.FindObjectsByType<Bucket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector2 nd = LaunchDirHandle(b.MouthPosition, b.launchDirection, ColorFor(b.colorId));
                if (nd != b.launchDirection)
                {
                    Undo.RecordObject(b, "Đổi hướng nhả dot");
                    b.launchDirection = nd;
                    MarkPrefabOverride(b);
                }
            }

            foreach (var s in Object.FindObjectsByType<ModelDotSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector3 origin = s.spawnOrigin != null ? s.spawnOrigin.position : s.transform.position;
                Color col = s.fixedColorId >= 0 ? ColorFor(s.fixedColorId) : Color.white;
                Vector2 nd = LaunchDirHandle(origin, s.launchDirection, col);
                if (nd != s.launchDirection)
                {
                    Undo.RecordObject(s, "Đổi hướng phóng dot");
                    s.launchDirection = nd;
                    MarkPrefabOverride(s);
                }
            }
        }

        /// <summary>
        /// Handle tròn ở đầu mũi tên hướng phóng; kéo để xoay quanh origin.
        /// Giữ SHIFT khi kéo = snap góc 15°. Trả về hướng mới (đã normalize).
        /// </summary>
        static Vector2 LaunchDirHandle(Vector3 origin, Vector2 dir, Color col)
        {
            if (dir.sqrMagnitude < 1e-4f) dir = Vector2.down;
            Vector3 d = ((Vector3)dir).normalized;
            Vector3 tip = origin + d * 1.1f;

            Handles.color = col;
            EditorGUI.BeginChangeCheck();
            float size = HandleUtility.GetHandleSize(tip) * 0.14f;
            Vector3 moved = Handles.FreeMoveHandle(tip, size, Vector3.zero, Handles.CircleHandleCap);
            if (!EditorGUI.EndChangeCheck()) return dir;

            Vector2 nd = new Vector2(moved.x - origin.x, moved.y - origin.y);
            if (nd.sqrMagnitude < 1e-4f) return dir;

            if (Event.current.shift)
            {
                float step = 15f * Mathf.Deg2Rad;
                float ang = Mathf.Round(Mathf.Atan2(nd.y, nd.x) / step) * step;
                nd = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            }
            return nd.normalized;
        }

        /// <summary>Mũi tên chỉ hướng phóng dot (launchDirection) từ spawner/bucket.</summary>
        static void DrawLaunchArrow(Vector3 from, Vector2 dir, Color col)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            Vector3 d = ((Vector3)dir).normalized;
            Vector3 tip = from + d * 1.1f;
            Handles.color = col;
            Handles.DrawLine(from, tip);
            Handles.DrawLine(tip, tip + Quaternion.Euler(0f, 0f, 150f) * d * 0.28f);
            Handles.DrawLine(tip, tip + Quaternion.Euler(0f, 0f, -150f) * d * 0.28f);
        }

        Color ColorFor(int colorId)
        {
            var f = _level != null && _level.fruitDatabase != null ? _level.fruitDatabase.GetById(colorId) : null;
            return f != null ? f.color : Color.white;
        }

        static GUIStyle _labelStyle;
        static GUIStyle LabelStyle(Color c)
        {
            if (_labelStyle == null)
                _labelStyle = new GUIStyle(EditorStyles.boldLabel);
            _labelStyle.normal.textColor = c;
            return _labelStyle;
        }

        // =========================================================
        // Save / Load
        // =========================================================

        void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Tạo Level Data", "Level_001", "asset", "Chọn nơi lưu level");
            if (string.IsNullOrEmpty(path)) return;

            var a = CreateInstance<LevelData>();
            // Kế thừa prefab config từ level đang mở cho đỡ phải gán lại.
            if (_level != null)
            {
                a.bucketPrefab = _level.bucketPrefab;
                a.spawnerPrefab = _level.spawnerPrefab;
                a.columnPrefab = _level.columnPrefab;
                a.conveyorPrefab = _level.conveyorPrefab;
                a.obstaclePrefab = _level.obstaclePrefab;
                a.grinderPrefab = _level.grinderPrefab;
                a.fruitDatabase = _level.fruitDatabase;
            }
            AssetDatabase.CreateAsset(a, path);
            AssetDatabase.SaveAssets();
            _level = a;
        }

        void SaveSceneToAsset()
        {
            Undo.RecordObject(_level, "Save Level");
            _level.conveyors.Clear();
            _level.conveyorLinks.Clear();
            _level.buckets.Clear();
            _level.spawners.Clear();
            _level.columns.Clear();
            _level.obstacles.Clear();
            _level.grinders.Clear();

            // ---- Conveyors + links ----
            var conveyors = new List<ConveyorSpline>(
                Object.FindObjectsByType<ConveyorSpline>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            conveyors.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // thứ tự ổn định cho index link
            var index = new Dictionary<ConveyorSpline, int>();
            for (int i = 0; i < conveyors.Count; i++) index[conveyors[i]] = i;

            foreach (var c in conveyors)
            {
                var spline = c.Container.Spline;
                var beltRenderer = c.GetComponent<ConveyorBeltRenderer>();
                var routeSwitch = c.GetComponent<ConveyorSwitch>();
                var cd = new LevelData.ConveyorData
                {
                    name = c.name,
                    beltWidth = c.beltWidth,
                    closed = spline.Closed,
                    hasSwitch = routeSwitch != null,
                    hasSwitchSettings = routeSwitch != null,
                    switchClickRadius = routeSwitch != null ? routeSwitch.clickRadius : 1.1f,
                    switchBranchClickPadding = routeSwitch != null ? routeSwitch.branchClickPadding : 0.15f,
                    switchBranchClickStartProgress = routeSwitch != null ? routeSwitch.branchClickStartProgress : 0.08f,
                    switchLinkedBranchSprite = routeSwitch != null ? routeSwitch.linkedBranchSprite : null,
                    switchIndicatorColor = routeSwitch != null ? routeSwitch.indicatorColor : new Color(1f, 0.85f, 0.2f),
                    switchLinkedSpriteProgress = routeSwitch != null ? routeSwitch.linkedSpriteProgress : 0.35f,
                    switchLinkedSpriteSize = routeSwitch != null ? routeSwitch.linkedSpriteSize : 0.45f,
                    switchLinkedSpriteSortingOrder = routeSwitch != null ? routeSwitch.linkedSpriteSortingOrder : 60,
                    switchUseCombinedRenderer = routeSwitch != null && routeSwitch.useCombinedRenderer,
                    switchLinkedKnotCount = routeSwitch != null ? routeSwitch.linkedKnotCount : 0,
                    switchHideSourceRenderer = routeSwitch == null || routeSwitch.hideSourceRenderer,
                    switchCombinedRendererZOffset = routeSwitch != null ? routeSwitch.combinedRendererZOffset : -0.002f,
                    switchDuplicateKnotTolerance = routeSwitch != null ? routeSwitch.duplicateKnotTolerance : 0.01f,
                    switchCombinedSortingOrderOffset = routeSwitch != null ? routeSwitch.combinedSortingOrderOffset : 1,
                    switchInactiveDimAlpha = routeSwitch != null ? routeSwitch.inactiveDimAlpha : 0.45f,
                    switchInactiveBranchZOffset = routeSwitch != null ? routeSwitch.inactiveBranchZOffset : 0.35f,
                    switchInactiveBranchSortingOffset = routeSwitch != null ? routeSwitch.inactiveBranchSortingOffset : 20,
                    switchActiveBranchOwnedKnotCount = routeSwitch != null ? routeSwitch.activeBranchOwnedKnotCount : 2,
                    switchActiveIndex = routeSwitch != null ? routeSwitch.ActiveIndex : 0,
                    hasRendererSettings = beltRenderer != null,
                    straightEdges = c.straightEdges,
                    cornerRadius = c.cornerRadius,
                    cornerSegments = c.cornerSegments,
                    bakeResolution = c.bakeResolution,
                    autoRebakeAtRuntime = c.autoRebakeAtRuntime,
                    beltMaterial = beltRenderer != null ? beltRenderer.beltMaterial : null,
                    beltTexture = beltRenderer != null ? beltRenderer.texture : null,
                    rendererSegments = beltRenderer != null ? beltRenderer.segments : 64,
                    rendererZOffset = beltRenderer != null ? beltRenderer.zOffset : 0.05f,
                    renderConnections = beltRenderer == null || beltRenderer.renderConnections,
                    connectionSegments = beltRenderer != null ? beltRenderer.connectionSegments : 16,
                    connectionMinLead = beltRenderer != null ? beltRenderer.connectionMinLead : 0.9f,
                    connectionWidthFactor = beltRenderer != null ? beltRenderer.connectionWidthFactor : 1.25f,
                    connectionMaxProgress = beltRenderer != null ? beltRenderer.connectionMaxProgress : 0.35f,
                    straightConnectionDot = beltRenderer != null ? beltRenderer.straightConnectionDot : 0.995f,
                    connectionSnapWidthFactor = beltRenderer != null ? beltRenderer.connectionSnapWidthFactor : 0.08f,
                    tilesAcrossWidth = beltRenderer != null ? beltRenderer.tilesAcrossWidth : 1f,
                    scrollSpeed = beltRenderer != null ? beltRenderer.scrollSpeed : 0.5f,
                    showWalls = beltRenderer != null && beltRenderer.showWalls,
                    wallWidth = beltRenderer != null ? beltRenderer.wallWidth : 0.15f,
                    wallZOffset = beltRenderer != null ? beltRenderer.wallZOffset : -0.01f,
                    scrollWalls = beltRenderer != null && beltRenderer.scrollWalls,
                    wallMaterialOuter = beltRenderer != null ? beltRenderer.wallMaterialOuter : null,
                    wallTextureOuter = beltRenderer != null ? beltRenderer.wallTextureOuter : null,
                    wallMaterialInner = beltRenderer != null ? beltRenderer.wallMaterialInner : null,
                    wallTextureInner = beltRenderer != null ? beltRenderer.wallTextureInner : null,
                };
                for (int i = 0; i < spline.Count; i++)
                    cd.knots.Add(c.transform.TransformPoint((Vector3)spline[i].Position));
                _level.conveyors.Add(cd);
            }
            foreach (var c in conveyors)
            {
                var conn = c.GetComponent<ConveyorConnections>();
                if (conn == null) continue;
                foreach (var nx in conn.next)
                {
                    if (nx == null || !index.ContainsKey(nx)) continue;
                    _level.conveyorLinks.Add(new LevelData.ConveyorLink { from = index[c], to = index[nx] });
                }
            }

            foreach (var grinder in Object.FindObjectsByType<ConveyorGrinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int inputConveyor = -1;
                foreach (var conveyor in conveyors)
                {
                    ConveyorConnections connections = conveyor.GetComponent<ConveyorConnections>();
                    if (connections != null && connections.terminalGrinder == grinder)
                    {
                        inputConveyor = index[conveyor];
                        break;
                    }
                }
                _level.grinders.Add(new LevelData.GrinderData { position = grinder.transform.position, inputConveyor = inputConveyor });
            }

            // ---- Buckets ----
            foreach (var b in Object.FindObjectsByType<Bucket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ConveyorSpline input = b.InputConveyor;
                if (input == null || !index.ContainsKey(input))
                    input = PickConveyor(b.MouthPosition, float.MaxValue);
                b.BindInputConveyor(input);

                _level.buckets.Add(new LevelData.BucketData
                {
                    position = b.transform.position,
                    colorId = b.colorId,
                    maxFill = b.maxFill,
                    launchDirection = b.launchDirection,
                    inputConveyor = input != null && index.TryGetValue(input, out int inputIndex)
                        ? inputIndex
                        : -1,
                });
            }

            // ---- Columns (spawner con lưu LOCAL position, đúng thứ tự cây) ----
            var columns = Object.FindObjectsByType<ModelDotSpawnerColumn>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var col in columns)
            {
                var cd = new LevelData.ColumnData
                {
                    position = col.transform.position,
                    clickAnywhereInColumn = col.clickAnywhereInColumn,
                };
                foreach (var s in col.GetComponentsInChildren<ModelDotSpawner>(true))
                    cd.spawners.Add(ToSpawnerData(s, col.transform.InverseTransformPoint(s.transform.position)));
                _level.columns.Add(cd);
            }

            // ---- Spawners đứng riêng (không thuộc column nào) ----
            foreach (var s in Object.FindObjectsByType<ModelDotSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (s.GetComponentInParent<ModelDotSpawnerColumn>(true) != null) continue;
                _level.spawners.Add(ToSpawnerData(s, s.transform.position));
            }

            // ---- Obstacles ----
            foreach (var o in Object.FindObjectsByType<DotObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                _level.obstacles.Add(new LevelData.ObstacleData
                {
                    position = o.transform.position,
                    requiredDots = o.requiredDots,
                });
            }

            EditorUtility.SetDirty(_level);
            AssetDatabase.SaveAssets();
            Notify($"Đã lưu: {_level.buckets.Count} bucket, {_level.spawners.Count} spawner, " +
                   $"{_level.columns.Count} column, {_level.conveyors.Count} băng, {_level.obstacles.Count} vật cản");
        }

        static LevelData.SpawnerData ToSpawnerData(ModelDotSpawner s, Vector3 position)
        {
            return new LevelData.SpawnerData
            {
                position = position,
                fixedColorId = s.fixedColorId,
                totalDots = s.totalClicks,
                spawnCount = s.spawnCount,
                launchDirection = s.launchDirection,
                launchSpeed = s.launchSpeed,
            };
        }

        void LoadAssetToScene()
        {
            if (!EditorUtility.DisplayDialog("Load Level",
                "Xoá toàn bộ entity level đang có trong scene và dựng lại từ asset?\n\n" +
                "Chỉnh sửa trong scene CHƯA bấm 'Save Scene -> Asset' sẽ mất.", "Load", "Huỷ"))
                return;

            ClearSceneEntities(false);
            var root = LevelBuilder.Build(_level);
            if (root != null)
            {
                // Áp lại config từ asset bằng code EDITOR: chống trường hợp Unity compile
                // thiếu LevelBuilder mới (runtime assembly stale — đã xảy ra nhiều lần).
                ReapplyDataToBuiltLevel(root);
                // Ghi TOÀN BỘ field thành prefab override — nếu không, giá trị chỉ nằm
                // trong RAM và rơi về default của prefab khi Play/recompile.
                RecordAllPrefabOverrides(root);
                Undo.RegisterCreatedObjectUndo(root, "Load Level");
            }
            MarkDirty();
            Notify($"Đã load level '{_level.name}'");
        }

        void ClearSceneEntities(bool confirm)
        {
            if (confirm && !EditorUtility.DisplayDialog("Xoá level",
                "Xoá toàn bộ Bucket / Spawner / Column / Conveyor / Obstacle trong scene?", "Xoá", "Huỷ"))
                return;

            var doomed = new HashSet<GameObject>();
            foreach (var c in Object.FindObjectsByType<ModelDotSpawnerColumn>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                doomed.Add(c.gameObject);
            foreach (var s in Object.FindObjectsByType<ModelDotSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.GetComponentInParent<ModelDotSpawnerColumn>(true) == null) doomed.Add(s.gameObject);
            foreach (var b in Object.FindObjectsByType<Bucket>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                doomed.Add(b.gameObject);
            foreach (var c in Object.FindObjectsByType<ConveyorSpline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                doomed.Add(c.gameObject);
            foreach (var o in Object.FindObjectsByType<DotObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                doomed.Add(o.gameObject);
            foreach (var grinder in Object.FindObjectsByType<ConveyorGrinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                doomed.Add(grinder.gameObject);
            // Root rỗng còn sót lại từ lần load trước.
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.name.StartsWith(LevelBuilder.RootName)) doomed.Add(go);

            foreach (var go in doomed)
                if (go != null) Undo.DestroyObjectImmediate(go);

            _drawing = null;
            _editing = null;
            MarkDirty();
        }

        // ---------------- Helpers ----------------

        void Notify(string msg) { ShowNotification(new GUIContent(msg)); }

        /// <summary>
        /// Ghi field vừa set bằng code vào m_Modifications của prefab instance.
        /// Thiếu bước này thì thay đổi RƠI VỀ DEFAULT của prefab khi scene reload/recompile.
        /// </summary>
        static void MarkPrefabOverride(Component c)
        {
            if (c == null) return;
            EditorUtility.SetDirty(c);
            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }

        /// <summary>
        /// Áp lại toàn bộ config từ LevelData lên level vừa build (khớp theo THỨ TỰ TẠO,
        /// trùng với thứ tự trong data). Bản sao phía editor của logic LevelBuilder —
        /// bảo đảm Load đúng kể cả khi Unity chưa recompile LevelBuilder runtime.
        /// </summary>
        void ReapplyDataToBuiltLevel(GameObject root)
        {
            var buckets = root.GetComponentsInChildren<Bucket>(true);
            for (int i = 0; i < buckets.Length && i < _level.buckets.Count; i++)
            {
                var bd = _level.buckets[i];
                var b = buckets[i];
                b.transform.position = bd.position;
                b.colorId = bd.colorId;
                b.maxFill = bd.maxFill;
                if (bd.launchDirection.sqrMagnitude > 0.0001f) b.launchDirection = bd.launchDirection;
                if (b.fruitDatabase == null) b.fruitDatabase = _level.fruitDatabase;
                b.RefreshVisuals();
                MarkPrefabOverride(b);
            }

            var standalone = new List<ModelDotSpawner>();
            foreach (var s in root.GetComponentsInChildren<ModelDotSpawner>(true))
                if (s.GetComponentInParent<ModelDotSpawnerColumn>(true) == null) standalone.Add(s);
            for (int i = 0; i < standalone.Count && i < _level.spawners.Count; i++)
                ApplySpawnerData(standalone[i], _level.spawners[i]);

            var cols = root.GetComponentsInChildren<ModelDotSpawnerColumn>(true);
            for (int i = 0; i < cols.Length && i < _level.columns.Count; i++)
            {
                var cd = _level.columns[i];
                cols[i].clickAnywhereInColumn = cd.clickAnywhereInColumn;
                MarkPrefabOverride(cols[i]);

                var kids = cols[i].GetComponentsInChildren<ModelDotSpawner>(true);
                for (int k = 0; k < kids.Length && k < cd.spawners.Count; k++)
                    ApplySpawnerData(kids[k], cd.spawners[k]);
            }

            var obstacles = root.GetComponentsInChildren<DotObstacle>(true);
            for (int i = 0; i < obstacles.Length && i < _level.obstacles.Count; i++)
            {
                var od = _level.obstacles[i];
                obstacles[i].transform.position = od.position;
                obstacles[i].requiredDots = Mathf.Max(1, od.requiredDots);
                MarkPrefabOverride(obstacles[i]);
            }
        }

        void ApplySpawnerData(ModelDotSpawner s, LevelData.SpawnerData sd)
        {
            s.fixedColorId = sd.fixedColorId;
            s.totalClicks = sd.totalDots;
            s.spawnCount = sd.spawnCount;
            if (sd.launchDirection.sqrMagnitude > 0.0001f) s.launchDirection = sd.launchDirection;
            s.launchSpeed = Mathf.Max(0.1f, sd.launchSpeed);
            if (s.fruitDatabase == null) s.fruitDatabase = _level.fruitDatabase;
            s.RefreshVisuals();
            MarkPrefabOverride(s);
        }

        /// <summary>Ghi override cho mọi component gameplay dưới root level vừa build.</summary>
        static void RecordAllPrefabOverrides(GameObject root)
        {
            foreach (var b in root.GetComponentsInChildren<Bucket>(true)) MarkPrefabOverride(b);
            foreach (var s in root.GetComponentsInChildren<ModelDotSpawner>(true)) MarkPrefabOverride(s);
            foreach (var c in root.GetComponentsInChildren<ModelDotSpawnerColumn>(true)) MarkPrefabOverride(c);
            foreach (var o in root.GetComponentsInChildren<DotObstacle>(true)) MarkPrefabOverride(o);
            foreach (var grinder in root.GetComponentsInChildren<ConveyorGrinder>(true)) MarkPrefabOverride(grinder);
            foreach (var c in root.GetComponentsInChildren<ConveyorSpline>(true))
            {
                MarkPrefabOverride(c);
                MarkPrefabOverride(c.Container); // knots của spline cũng là dữ liệu instance
                MarkPrefabOverride(c.GetComponent<ConveyorBeltRenderer>());
                MarkPrefabOverride(c.GetComponent<ConveyorConnections>());
                MarkPrefabOverride(c.GetComponent<ConveyorSwitch>());
            }
            // Sprite/màu do RefreshVisuals đổi nằm trên các SpriteRenderer -> cũng phải ghi.
            foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true)) MarkPrefabOverride(r);
            foreach (var g in root.GetComponentsInChildren<SpriteGridFill>(true)) MarkPrefabOverride(g);
        }

        static void MarkDirty()
        {
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
