# LoopSort — Conveyor Editor Spec

**Phạm vi:** Editor đặt / nối / uốn path cho conveyor 2D tự do (free-form, spline-based). Tập trung nhóm **định tuyến item theo hình dạng/hướng đi**.
**Môi trường:** Dùng chung cho cả **Unity Editor** (thiết kế level) và **runtime** (in-game level editor).
**Lưu trữ:** **ScriptableObject** asset là nguồn chính (single source of truth).
**Phiên bản:** Draft v1 — 2026-06-20.

---

## 1. Mục tiêu & nguyên tắc

- Conveyor là **đường đi (path) có hướng**; định tuyến chỉ xảy ra tại **node**. Không dùng grid ô vuông.
- Path gốc là **spline** (Catmull-Rom đi qua điểm, hoặc Bezier có tay nắm). Straight và Arc là trường hợp đặc biệt của spline.
- Một model duy nhất chạy được ở cả Editor và runtime. Lớp UI/tương tác tách rời khỏi lớp dữ liệu.
- Item di chuyển theo **chiều dài cung (arc-length)** để tốc độ ổn định bất kể độ cong (không trượt theo tham số `t` thô — sẽ giật ở đoạn cong gắt).

---

## 2. Data model (ScriptableObject)

Tách rõ **Node** (điểm nối + định tuyến) và **Path** (đoạn băng nối 2 node).

### 2.1. Asset gốc

```csharp
[CreateAssetMenu(menuName = "LoopSort/Conveyor Graph")]
public class ConveyorGraph : ScriptableObject
{
    public string graphId;
    public List<ConveyorNode> nodes = new();
    public List<ConveyorPath> paths = new();

    [System.NonSerialized] public Dictionary<int, ConveyorNode> nodeLookup;
    [System.NonSerialized] public Dictionary<int, ConveyorPath> pathLookup;

    public void RebuildLookups() { /* dựng dict từ list khi load */ }
}
```

> Lý do dùng `List` + `int id` thay vì tham chiếu trực tiếp: serialize sạch trong SO, dễ export JSON về sau, tránh vòng tham chiếu khi inspector vẽ.

### 2.2. Node

```csharp
public enum NodeType { Endpoint, Connector, Splitter, Merger, Crossing }

[System.Serializable]
public class ConveyorNode
{
    public int id;
    public Vector2 position;
    public NodeType type = NodeType.Endpoint;

    // id của các path cắm vào node này
    public List<int> incomingPathIds = new();
    public List<int> outgoingPathIds = new();

    // Luật định tuyến (chỉ dùng cho Splitter/Merger)
    public RoutingRule routing = new();
}
```

| NodeType | In | Out | Ý nghĩa |
|---|---|---|---|
| `Endpoint` | 0 | 1 | Nguồn (spawn) hoặc đích (sink) của luồng |
| `Connector` | 1 | 1 | Nối 2 path thành luồng liên tục |
| `Splitter` | 1 | N | Chia luồng ra nhiều nhánh |
| `Merger` | N | 1 | Gộp nhiều luồng vào một |
| `Crossing` | 2 | 2 | Hai luồng giao nhau hình học, **không trộn** (in_i nối thẳng tới out_i) |

### 2.3. Path

```csharp
public enum PathKind { Straight, Arc, Spline }

[System.Serializable]
public class ConveyorPath
{
    public int id;
    public int fromNodeId;
    public int toNodeId;

    public PathKind kind = PathKind.Spline;

    // Control points ở local/world 2D. Với Straight: 2 điểm.
    // Arc: 3 điểm (đầu, giữa cung, cuối). Spline: >=2, nội suy Catmull-Rom.
    public List<Vector2> controlPoints = new();

    public float speed = 1f;        // đơn vị/giây
    public bool reversed = false;   // đảo chiều dòng (tùy chọn, nhóm này có thể bỏ)

    // Cache tính sẵn khi chỉnh sửa xong (không serialize)
    [System.NonSerialized] public float cachedLength;
    [System.NonSerialized] public Vector2[] sampledPoints; // bảng arc-length
}
```

### 2.4. Luật định tuyến

```csharp
public enum SplitMode { RoundRobin, Ratio, ByItemType, FirstAvailable }
public enum MergePriority { RoundRobin, FixedOrder, FirstComeFirstServe }

[System.Serializable]
public class RoutingRule
{
    public SplitMode splitMode = SplitMode.RoundRobin;
    public List<float> ratios = new();        // dùng khi Ratio (khớp thứ tự outgoingPathIds)
    public MergePriority mergePriority = MergePriority.RoundRobin;
}
```

### 2.5. Item (runtime, không thuộc asset)

```csharp
public class ItemOnConveyor
{
    public int itemId;
    public int currentPathId;
    public float distanceAlong;   // mét dọc theo path (0..cachedLength)
    public int itemType;          // dùng cho ByItemType
}
```

---

## 3. Hình học spline & arc-length

1. **Nội suy:** Catmull-Rom đi qua mọi control point (trực giác khi kéo). Cho phép tùy chọn Bezier nếu cần tay nắm độc lập.
2. **Lấy mẫu (sampling):** Khi path thay đổi, lấy mẫu N điểm đều theo tham số rồi dựng **bảng arc-length** (mảng `(t, cumulativeLength)`).
3. **Truy vấn vị trí:** Cho `distanceAlong` → tra bảng → nội suy ra `t` → ra `(position, tangent)`. Tangent dùng để xoay sprite item theo hướng băng.
4. **`cachedLength`** = tổng chiều dài; dùng để biết khi nào item rời path.
5. Lấy mẫu lại chỉ khi control point/kind đổi (dirty flag), không tính mỗi frame.

---

## 4. Quy tắc nối (connection rules)

- Path luôn nối **đúng 2 node** (`fromNodeId`, `toNodeId`). Không có path "lơ lửng" khi lưu (editor cảnh báo nếu có).
- Khi nối path vào node, cập nhật `incoming/outgoingPathIds` tương ứng và **tự đổi `NodeType`** theo bậc:
  - 1 in + 1 out → `Connector`
  - 1 in + ≥2 out → `Splitter`
  - ≥2 in + 1 out → `Merger`
  - 0 in + 1 out hoặc 1 in + 0 out → `Endpoint`
  - 2 in + 2 out đánh dấu thủ công → `Crossing`
- **Hướng** suy ra từ from→to. Điểm đầu path khớp `fromNode.position`, điểm cuối khớp `toNode.position` (control point đầu/cuối "snap" vào node).
- **Tiếp tuyến liền mạch (tùy chọn):** tại `Connector`, có thể ép tangent vào = tangent ra để băng không gãy khúc.

---

## 5. Công cụ Editor (tool đặt / nối / uốn)

Kiến trúc: **một lõi logic** (`ConveyorEditController`) thao tác trên `ConveyorGraph`, dùng chung. Hai lớp "host" gọi vào lõi:

- **Editor host:** `EditorWindow` + `Handles`/`Event` trong Scene view.
- **Runtime host:** Canvas UI + raycast chuột/cảm ứng trong game.

### 5.1. Các chế độ (tool modes)

| Mode | Thao tác | Kết quả |
|---|---|---|
| **Select** | Click node/path | Chọn để di chuyển/sửa/xóa |
| **Place Node** | Click vùng trống | Tạo `Endpoint` tại vị trí (snap tùy chọn) |
| **Draw Path** | Click node A → kéo → thả ở node B (hoặc thả vùng trống để tạo node mới) | Tạo `ConveyorPath` nối A→B; cập nhật node type |
| **Bend** | Kéo control point giữa của path | Uốn cong; thêm/bớt control point bằng double-click |
| **Split Junction** | Click 1 path | Chèn node giữa path, tách thành 2 path (để rẽ nhánh) |
| **Delete** | Click node/path | Xóa; dọn tham chiếu ở node liên quan |

### 5.2. Tương tác uốn path

- Mỗi path hiện các **control point** dạng handle tròn; đoạn giữa kéo được để thêm điểm.
- **Thêm điểm:** double-click trên path → chèn control point tại vị trí gần nhất theo arc-length.
- **Xóa điểm:** chọn handle giữa + Delete (không xóa được 2 điểm đầu/cuối vì gắn node).
- **Snap:** tùy chọn snap góc (15°), snap vào node/điểm gần (bán kính px), bật/tắt bằng phím Ctrl.
- Khi kéo, **re-sample** path đang sửa để preview cập nhật mượt; commit khi thả chuột.

### 5.3. Phản hồi hình ảnh

- Mũi tên hướng dọc path (từ→đến).
- Màu node theo type (Endpoint/Connector/Splitter/Merger/Crossing).
- Highlight đỏ cho lỗi: path lơ lửng, node trùng vị trí, Splitter chưa đủ nhánh, ratio không khớp số out.
- Tùy chọn xem trước item chạy (play preview) ngay trong editor.

### 5.4. Tiện ích

- **Undo/Redo:** Editor host dùng `Undo.RecordObject`. Runtime host dùng command stack riêng (lưu diff thao tác).
- **Multi-select + di chuyển nhóm.**
- **Duplicate** node/path.
- **Grid/guide tùy chọn** (chỉ hỗ trợ thị giác, không ép grid logic).

---

## 6. Lưu / Load

### 6.1. Nguồn chính — ScriptableObject

- Mỗi level/đồ thị = một asset `ConveyorGraph` (`.asset`).
- **Editor:** `AssetDatabase.CreateAsset` / `EditorUtility.SetDirty` + `SaveAssets`.
- **Runtime đọc:** tham chiếu trực tiếp asset (đã build vào game) hoặc qua Addressables/Resources.
- Sau khi load: gọi `RebuildLookups()` + re-sample toàn bộ path (vì cache không serialize).

### 6.2. Runtime ghi (user-generated)

SO không tạo/ghi được lúc runtime trong build. Với editor in-game:

- Giữ `ConveyorGraph` **trong bộ nhớ** (`ScriptableObject.CreateInstance`) khi chỉnh sửa.
- Khi lưu: serialize **list `nodes` + `paths`** ra **JSON** bằng `JsonUtility` (hoặc adapter) → ghi vào `Application.persistentDataPath`.
- Khi load: đọc JSON → `JsonUtility.FromJsonOverwrite` vào instance SO → `RebuildLookups()`.

> Vì data model đã thuần POCO (`List` + `int id` + `Vector2`), cùng một cấu trúc serialize được sang cả SO lẫn JSON mà không đổi schema. Một `IConveyorStore` với 2 cài đặt (`SoStore`, `JsonStore`) để host gọi thống nhất.

### 6.3. Versioning

- Thêm `int schemaVersion` trong `ConveyorGraph`. Viết `Migrate(old → new)` khi đổi schema để không vỡ asset/JSON cũ.

---

## 7. Kiến trúc lớp (tóm tắt)

```
ConveyorGraph (SO)            ← dữ liệu
ConveyorMath                 ← spline, arc-length, sampling (static)
ConveyorEditController       ← lõi thao tác: AddNode, ConnectPath, BendPath, Split, Delete, Validate
IConveyorStore               ← Save/Load   →  SoStore | JsonStore
ConveyorEditorWindow         ← host Unity Editor (Handles)
ConveyorRuntimeEditorUI      ← host runtime (Canvas/raycast)
ConveyorSimulator            ← chạy item theo arc-length (preview & gameplay)
```

`EditController` không biết mình đang ở Editor hay runtime — nhận lệnh từ host, sửa graph, trả validation. Đây là điểm mấu chốt để "dùng chung cả hai".

---

## 8. Validation (chạy khi lưu & realtime)

| Lỗi | Điều kiện | Mức |
|---|---|---|
| Path lơ lửng | `from`/`to` null hoặc node không tồn tại | Block save |
| Node trùng vị trí | 2 node cách nhau < epsilon | Warning |
| Splitter thiếu nhánh | type=Splitter nhưng out < 2 | Warning |
| Ratio sai | `splitMode=Ratio` & `ratios.Count != out.Count` | Block save |
| Crossing sai bậc | type=Crossing nhưng in≠2 hoặc out≠2 | Block save |
| Vòng lặp cô lập | (cho LoopSort: hợp lệ, nhưng cảnh báo nếu không có Endpoint nguồn) | Info |

---

## 9. Edge cases cần xử lý

- **Loop kín** (đặc trưng LoopSort): graph không có Endpoint nguồn — hợp lệ; simulator phải xử lý item chạy vòng vô hạn cho tới khi bị lấy ra ở Splitter/sink.
- **Item tới node cùng lúc** (Merger): quyết định theo `mergePriority`, tránh chồng item.
- **Path rất ngắn / 0 length:** chặn tạo, hoặc gộp 2 node.
- **Control point ngược chiều** gây spline tự cắt: cho phép nhưng cảnh báo (item có thể nhảy).
- **Xóa node đang có path:** xóa kèm path hoặc chặn (mặc định: hỏi/confirm).

---

## 10. Lộ trình triển khai gợi ý

1. Data model + `ConveyorMath` (spline, arc-length) + unit test sampling.
2. `ConveyorEditController` (add/connect/bend/delete) + validation, test bằng code (không UI).
3. `ConveyorEditorWindow` với Handles — đủ đặt/nối/uốn/lưu SO.
4. `ConveyorSimulator` + play preview trong editor.
5. `JsonStore` + `ConveyorRuntimeEditorUI` cho in-game editor.
6. Migration & polish (undo runtime, snap, multi-select).

---

## 11. Ngoài phạm vi (giai đoạn sau)

Sorter/Filter theo loại, Diverter có tín hiệu, speed zones, lift/tầng, bridge logic, buffer/storage — thuộc nhóm "chức năng" và "hành vi đặc biệt", sẽ spec riêng sau khi nhóm định tuyến hình học chạy ổn.
