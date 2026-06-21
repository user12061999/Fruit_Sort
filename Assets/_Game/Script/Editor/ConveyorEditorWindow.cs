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
    /// Tool dựng/sửa mạng băng chuyền 2D tự do trên nền Unity Splines.
    /// Mở: menu  Tools > FruitSort > Conveyor Editor.
    ///
    /// Chế độ (Mode):
    ///  - Place:   click liên tiếp trong Scene để thêm knot cho 1 băng mới; Enter/nút Finish để chốt.
    ///  - Edit:    click chọn 1 băng -> hiện handle ở từng knot, kéo để UỐN; nút Insert/Remove knot.
    ///  - Connect: click băng nguồn rồi click băng đích -> tạo liên kết (cuối nguồn -> đầu đích), snap điểm nối.
    ///  - Delete:  click 1 băng để xoá.
    ///
    /// Lưu/Load: ghi toàn bộ băng trong scene ra ConveyorNetworkAsset (.asset) và dựng lại từ asset.
    /// </summary>
    public class ConveyorEditorWindow : EditorWindow
    {
        enum Mode { None, Place, Edit, Connect, Delete }

        const string Prefix = "_LS_Conveyor";

        Mode _mode = Mode.None;
        float _newBeltWidth = 3f;
        bool _autoAddRenderer = false;

        ConveyorNetworkAsset _asset;

        // Trạng thái phiên
        ConveyorSpline _active;     // băng đang vẽ (Place) — đang mở
        ConveyorSpline _selected;   // băng đang chọn (Edit/Connect/Delete)
        ConveyorSpline _connectFrom;

        [MenuItem("Tools/FruitSort/Conveyor Editor")]
        public static void Open()
        {
            var w = GetWindow<ConveyorEditorWindow>("Conveyor Editor");
            w.minSize = new Vector2(280, 360);
            w.Show();
        }

        void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; }
        void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; }

        // ---------------- Window UI ----------------

        void OnGUI()
        {
            EditorGUILayout.LabelField("Mạng băng chuyền", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                ModeButton(Mode.Place, "Place");
                ModeButton(Mode.Edit, "Edit");
                ModeButton(Mode.Connect, "Connect");
                ModeButton(Mode.Delete, "Delete");
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(HelpFor(_mode), MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Băng mới (Place)", EditorStyles.boldLabel);
            _newBeltWidth = EditorGUILayout.Slider("Bề rộng", _newBeltWidth, 0.2f, 10f);
            _autoAddRenderer = EditorGUILayout.Toggle("Thêm Belt Renderer", _autoAddRenderer);

            if (_mode == Mode.Place && _active != null)
            {
                EditorGUILayout.HelpBox($"Đang vẽ: {_active.name} ({_active.Container.Spline.Count} knot)", MessageType.None);
                if (GUILayout.Button("Finish băng này (Enter)")) FinishActive();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Băng đang chọn (Edit)", EditorStyles.boldLabel);
            if (_selected != null)
            {
                EditorGUILayout.ObjectField("Selected", _selected, typeof(ConveyorSpline), true);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Insert knot")) InsertKnotMidLongest(_selected);
                    if (GUILayout.Button("Remove last")) RemoveLastKnot(_selected);
                }
                float w = EditorGUILayout.Slider("Bề rộng băng", _selected.beltWidth, 0.2f, 10f);
                if (!Mathf.Approximately(w, _selected.beltWidth))
                {
                    Undo.RecordObject(_selected, "Belt Width");
                    _selected.beltWidth = w; _selected.Bake();
                    EditorUtility.SetDirty(_selected);
                }
            }
            else EditorGUILayout.LabelField("(chưa chọn)");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lưu / Load", EditorStyles.boldLabel);
            _asset = (ConveyorNetworkAsset)EditorGUILayout.ObjectField("Asset", _asset, typeof(ConveyorNetworkAsset), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_asset == null))
                {
                    if (GUILayout.Button("Save scene -> asset")) SaveToAsset(_asset);
                    if (GUILayout.Button("Load asset -> scene")) LoadFromAsset(_asset);
                }
            }
            if (GUILayout.Button("Tạo asset mới...")) CreateNewAsset();

            EditorGUILayout.Space();
            if (GUILayout.Button("Xoá tất cả băng trong scene")) ClearSceneConveyors();
        }

        void ModeButton(Mode m, string label)
        {
            bool on = _mode == m;
            var bg = GUI.backgroundColor;
            if (on) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                _mode = on ? Mode.None : m;
                _connectFrom = null;
                if (_mode != Mode.Place) FinishActive();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = bg;
        }

        static string HelpFor(Mode m)
        {
            switch (m)
            {
                case Mode.Place: return "Click trong Scene để thêm knot cho băng mới. Enter hoặc nút Finish để chốt băng.";
                case Mode.Edit: return "Click 1 băng để chọn. Kéo handle ở mỗi knot để uốn. Dùng Insert/Remove knot ở panel.";
                case Mode.Connect: return "Click băng NGUỒN rồi click băng ĐÍCH. Tạo liên kết cuối-nguồn -> đầu-đích và snap điểm nối.";
                case Mode.Delete: return "Click 1 băng để xoá khỏi scene.";
                default: return "Chọn một chế độ để bắt đầu.";
            }
        }

        // ---------------- Scene interaction ----------------

        void OnSceneGUI(SceneView sv)
        {
            DrawAllConveyorGizmos();

            if (_mode == Mode.None) return;

            Event e = Event.current;
            int ctrl = GUIUtility.GetControlID(FocusType.Passive);
            if (_mode != Mode.Edit)
                HandleUtility.AddDefaultControl(ctrl); // chặn deselect khi click vào trống

            // Edit: vẽ handle kéo knot (uốn)
            if (_mode == Mode.Edit && _selected != null)
                DrawKnotHandles(_selected);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector3 wp = MouseWorld(e);
                switch (_mode)
                {
                    case Mode.Place: OnPlaceClick(wp); e.Use(); break;
                    case Mode.Edit: _selected = PickConveyor(wp); Repaint(); e.Use(); break;
                    case Mode.Connect: OnConnectClick(wp); e.Use(); break;
                    case Mode.Delete: OnDeleteClick(wp); e.Use(); break;
                }
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && _mode == Mode.Place)
            {
                FinishActive(); e.Use();
            }
        }

        void OnPlaceClick(Vector3 wp)
        {
            if (_active == null) _active = CreateConveyor(wp);
            else AppendKnot(_active, wp);
            Repaint();
        }

        void OnConnectClick(Vector3 wp)
        {
            var c = PickConveyor(wp);
            if (c == null) return;
            if (_connectFrom == null) { _connectFrom = c; ShowNotification(new GUIContent("Nguồn: " + c.name)); }
            else
            {
                if (c != _connectFrom) ConnectConveyors(_connectFrom, c);
                _connectFrom = null;
            }
            SceneView.RepaintAll();
        }

        void OnDeleteClick(Vector3 wp)
        {
            var c = PickConveyor(wp);
            if (c == null) return;
            if (_selected == c) _selected = null;
            if (_active == c) _active = null;
            Undo.DestroyObjectImmediate(c.gameObject);
            MarkDirty();
        }

        // ---------------- Conveyor ops ----------------

        ConveyorSpline CreateConveyor(Vector3 firstKnotWorld)
        {
            var go = new GameObject($"{Prefix}_{System.DateTime.Now.Ticks % 100000}");
            Undo.RegisterCreatedObjectUndo(go, "Create Conveyor");
            var container = Undo.AddComponent<SplineContainer>(go);
            var spline = container.Spline;
            spline.Clear();
            spline.Add(new BezierKnot(ToLocal(go.transform, firstKnotWorld)), TangentMode.AutoSmooth);

            var conv = Undo.AddComponent<ConveyorSpline>(go);
            conv.beltWidth = _newBeltWidth;
            Undo.AddComponent<ConveyorConnections>(go);

            if (_autoAddRenderer)
            {
                Undo.AddComponent<MeshFilter>(go);
                Undo.AddComponent<MeshRenderer>(go);
                Undo.AddComponent<ConveyorBeltRenderer>(go);
            }
            MarkDirty();
            return conv;
        }

        void AppendKnot(ConveyorSpline conv, Vector3 world)
        {
            var spline = conv.Container.Spline;
            Undo.RecordObject(conv, "Add Knot");
            spline.Add(new BezierKnot(ToLocal(conv.transform, world)), TangentMode.AutoSmooth);
            conv.Bake();
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
            Undo.RecordObject(conv, "Insert Knot");
            spline.Insert(bestI + 1, new BezierKnot(mid), TangentMode.AutoSmooth);
            conv.Bake();
            MarkDirty();
        }

        void RemoveLastKnot(ConveyorSpline conv)
        {
            var spline = conv.Container.Spline;
            if (spline.Count <= 2) return;
            Undo.RecordObject(conv, "Remove Knot");
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
                    moved.z = 0f;
                    Undo.RecordObject(conv, "Bend Conveyor");
                    var k = spline[i];
                    k.Position = ToLocal(conv.transform, moved);
                    spline[i] = k;
                    spline.SetTangentMode(i, TangentMode.AutoSmooth);
                    conv.Bake();
                    MarkDirty();
                }
            }
        }

        void ConnectConveyors(ConveyorSpline from, ConveyorSpline to)
        {
            var conn = from.GetComponent<ConveyorConnections>();
            if (conn == null) conn = Undo.AddComponent<ConveyorConnections>(from.gameObject);
            if (!conn.next.Contains(to))
            {
                Undo.RecordObject(conn, "Connect Conveyor");
                conn.next.Add(to);
            }
            // Snap: cuối spline 'from' tới đầu spline 'to'
            var sf = from.Container.Spline; var st = to.Container.Spline;
            if (sf.Count > 0 && st.Count > 0)
            {
                Vector3 startTo = to.transform.TransformPoint((Vector3)st[0].Position);
                Undo.RecordObject(from, "Snap Conveyor");
                var k = sf[sf.Count - 1];
                k.Position = ToLocal(from.transform, startTo);
                sf[sf.Count - 1] = k;
                sf.SetTangentMode(sf.Count - 1, TangentMode.AutoSmooth);
                from.Bake();
            }
            MarkDirty();
        }

        // ---------------- Picking & drawing ----------------

        static Vector3 MouseWorld(Event e)
        {
            Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float denom = Mathf.Approximately(r.direction.z, 0f) ? 1e-5f : r.direction.z;
            float t = -r.origin.z / denom;
            Vector3 p = r.origin + r.direction * t;
            p.z = 0f;
            return p;
        }

        static float3 ToLocal(Transform tr, Vector3 world) => tr.InverseTransformPoint(world);

        ConveyorSpline PickConveyor(Vector3 world, float maxDist = 1.0f)
        {
            ConveyorSpline best = null; float bestD = maxDist;
            foreach (var c in FindAll())
            {
                float d = DistanceToSpline(c, world);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        static float DistanceToSpline(ConveyorSpline conv, Vector3 world, int steps = 40)
        {
            var container = conv.Container;
            if (container.Spline == null || container.Spline.Count < 1) return float.MaxValue;
            float best = float.MaxValue;
            for (int i = 0; i <= steps; i++)
            {
                container.Evaluate(i / (float)steps, out float3 p, out _, out _);
                float d = Vector3.Distance((Vector3)p, world);
                if (d < best) best = d;
            }
            return best;
        }

        void DrawAllConveyorGizmos()
        {
            foreach (var c in FindAll())
            {
                var spline = c.Container.Spline;
                if (spline == null || spline.Count < 1) continue;

                Handles.color = (c == _selected) ? Color.yellow
                              : (c == _connectFrom) ? new Color(1f, 0.5f, 0f) : Color.cyan;
                const int seg = 48;
                Vector3 prev = Vector3.zero;
                for (int i = 0; i <= seg; i++)
                {
                    c.Container.Evaluate(i / (float)seg, out float3 p, out _, out _);
                    if (i > 0) Handles.DrawLine(prev, (Vector3)p);
                    prev = (Vector3)p;
                }
                // đầu (xanh lá) / cuối (đỏ)
                Vector3 start = c.transform.TransformPoint((Vector3)spline[0].Position);
                Vector3 end = c.transform.TransformPoint((Vector3)spline[spline.Count - 1].Position);
                Handles.color = Color.green; Handles.DrawSolidDisc(start, Vector3.forward, 0.12f);
                Handles.color = Color.red; Handles.DrawSolidDisc(end, Vector3.forward, 0.12f);

                // mũi tên liên kết
                var conn = c.GetComponent<ConveyorConnections>();
                if (conn != null)
                {
                    Handles.color = Color.magenta;
                    foreach (var nx in conn.next)
                    {
                        if (nx == null) continue;
                        var ns = nx.Container.Spline;
                        if (ns == null || ns.Count < 1) continue;
                        Vector3 ne = nx.transform.TransformPoint((Vector3)ns[0].Position);
                        Handles.DrawLine(end, ne);
                    }
                }
            }
        }

        // ---------------- Save / Load ----------------

        static List<ConveyorSpline> FindAll()
        {
            var list = new List<ConveyorSpline>(
                Object.FindObjectsByType<ConveyorSpline>(FindObjectsSortMode.None));
            return list;
        }

        void SaveToAsset(ConveyorNetworkAsset asset)
        {
            var all = FindAll();
            var index = new Dictionary<ConveyorSpline, int>();
            for (int i = 0; i < all.Count; i++) index[all[i]] = i;

            Undo.RecordObject(asset, "Save Conveyor Network");
            asset.conveyors.Clear();
            asset.links.Clear();

            foreach (var c in all)
            {
                var data = new ConveyorNetworkAsset.ConveyorData { name = c.name, beltWidth = c.beltWidth };
                var spline = c.Container.Spline;
                for (int i = 0; i < spline.Count; i++)
                    data.knots.Add(c.transform.TransformPoint((Vector3)spline[i].Position));
                asset.conveyors.Add(data);
            }
            foreach (var c in all)
            {
                var conn = c.GetComponent<ConveyorConnections>();
                if (conn == null) continue;
                foreach (var nx in conn.next)
                {
                    if (nx == null || !index.ContainsKey(nx)) continue;
                    asset.links.Add(new ConveyorNetworkAsset.LinkData { from = index[c], to = index[nx] });
                }
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent($"Đã lưu {asset.conveyors.Count} băng"));
        }

        void LoadFromAsset(ConveyorNetworkAsset asset)
        {
            ClearSceneConveyors();
            var made = new List<ConveyorSpline>();
            foreach (var data in asset.conveyors)
            {
                var go = new GameObject(string.IsNullOrEmpty(data.name) ? Prefix : data.name);
                Undo.RegisterCreatedObjectUndo(go, "Load Conveyor");
                var container = go.AddComponent<SplineContainer>();
                var spline = container.Spline;
                spline.Clear();
                foreach (var w in data.knots)
                    spline.Add(new BezierKnot((float3)go.transform.InverseTransformPoint(w)), TangentMode.AutoSmooth);
                var conv = go.AddComponent<ConveyorSpline>();
                conv.beltWidth = data.beltWidth;
                go.AddComponent<ConveyorConnections>();
                conv.Bake();
                made.Add(conv);
            }
            foreach (var link in asset.links)
            {
                if (link.from < 0 || link.from >= made.Count || link.to < 0 || link.to >= made.Count) continue;
                var conn = made[link.from].GetComponent<ConveyorConnections>();
                if (!conn.next.Contains(made[link.to])) conn.next.Add(made[link.to]);
            }
            MarkDirty();
            ShowNotification(new GUIContent($"Đã load {made.Count} băng"));
        }

        void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Tạo Conveyor Network", "ConveyorNetwork", "asset", "Chọn nơi lưu asset");
            if (string.IsNullOrEmpty(path)) return;
            var a = CreateInstance<ConveyorNetworkAsset>();
            AssetDatabase.CreateAsset(a, path);
            AssetDatabase.SaveAssets();
            _asset = a;
        }

        // ---------------- Helpers ----------------

        void FinishActive()
        {
            if (_active != null && _active.Container.Spline.Count < 2)
            {
                // băng chỉ 1 knot -> bỏ
                Undo.DestroyObjectImmediate(_active.gameObject);
            }
            _active = null;
        }

        void ClearSceneConveyors()
        {
            foreach (var c in FindAll())
                Undo.DestroyObjectImmediate(c.gameObject);
            _active = _selected = _connectFrom = null;
            MarkDirty();
        }

        static void MarkDirty()
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
