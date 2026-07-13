
Hôm qua 21:49

unity-sprite-grid-fill-spec.md
Tệp
đây là file md game FruitSort bạn có gợi ý gì tiếp theo không



File hiện đã vượt xa một đặc tả shader: nó mô tả gần như toàn bộ gameplay loop, state machine của dot, Conveyor, Bucket, combo, save/load và điều kiện thắng/thua. Tuy nhiên tên file vẫn là unity-sprite-grid-fill-spec.md, dễ khiến Codex/Claude hiểu sai phạm vi triển khai. 


Việc nên làm tiếp theo
1. Tách tài liệu thành các file độc lập
Nên chia thành cấu trúc:

Docs/FruitSort/
├── 00_GameOverview.md
├── 01_CoreGameplayRules.md
├── 02_RuntimeArchitecture.md
├── 03_LevelDataSchema.md
├── 04_DotStateMachine.md
├── 05_ConveyorSystem.md
├── 06_BucketSystem.md
├── 07_GameplayManager.md
├── 08_SaveLoadSystem.md
├── 09_LevelEditor.md
├── 10_TestPlan.md
└── Features/
    └── SpriteGridFill.md
File hiện tại nên được đổi thành FruitSort_GameplaySpecification.md, sau đó trích riêng phần shader sang SpriteGridFill.md.

Lợi ích chính là tránh việc AI code toàn bộ gameplay khi bạn chỉ muốn sửa shader.

2. Làm rõ mô hình màu của Bucket
Đây là phần cần sửa trước khi code vì tài liệu hiện có khả năng gây nhầm lẫn giữa:

targetColorId
lockedColorId
Nên định nghĩa rõ:

public int TargetColorId { get; }
public int? LockedColorId { get; private set; }
Quy tắc đề xuất:

TargetColorId: màu đúng để hoàn thành Bucket.

LockedColorId: màu của dot đầu tiên Bucket đã nhận.

Bucket rỗng có LockedColorId = null.

Dot đầu tiên thiết lập LockedColorId.

Bucket chỉ nhận thêm dot có cùng LockedColorId.

Bucket chỉ hoàn thành khi:

CurrentFill == MaxFill &&
LockedColorId == TargetColorId
Nếu Bucket đầy bằng màu sai, không được phát OnBucketFull theo nghĩa hoàn thành màn. Nên dùng hai trạng thái riêng:

FilledCorrectly
FilledIncorrectly
Nếu không làm rõ điểm này, hệ thống thắng màn có thể coi Bucket đầy sai màu là hoàn thành.

3. Chốt state machine của Bucket
Tài liệu đã có state machine cho dot nhưng chưa có state machine chính thức cho Bucket.

Nên bổ sung:

Empty
  │ nhận dot đầu tiên
  ▼
ReceivingCorrect / ReceivingWrong
  │                       │
  │ đầy đúng              │ người chơi chạm
  ▼                       ▼
Completed              Releasing
                           │
                           ▼
                         Empty
Có thể triển khai bằng enum:

public enum BucketState
{
    Empty,
    Receiving,
    Releasing,
    Completed,
    Disabled
}
Tránh dùng nhiều biến boolean như:

isFull
isReleasing
isCompleted
isLocked
canReceive
Các boolean này rất dễ tạo ra trạng thái không hợp lệ, ví dụ vừa isFull vừa isReleasing.

4. Định nghĩa rõ dữ liệu LevelData
Đây nên là đầu việc tiếp theo sau đặc tả gameplay.

Ví dụ:

[CreateAssetMenu(menuName = "FruitSort/Level Data")]
public sealed class LevelData : ScriptableObject
{
    public string levelId;

    public int moveLimit;
    public float timeLimit;

    public List<SpawnerData> spawners;
    public List<BucketData> buckets;
    public List<ConveyorData> conveyors;
    public List<ConveyorSwitchData> switches;
}
Mỗi phần tử runtime cần có ID ổn định:

[Serializable]
public sealed class BucketData
{
    public string id;
    public int targetColorId;
    public int capacity;
    public Vector3 position;
}
[Serializable]
public sealed class ConveyorData
{
    public string id;
    public List<Vector3> controlPoints;
    public List<string> nextConveyorIds;
    public float speed;
    public float width;
}
Không nên dùng index danh sách làm ID save vì việc chèn hoặc xóa object trong Level Editor sẽ làm hỏng dữ liệu save cũ.

5. Triển khai một vertical slice nhỏ trước
Không nên code toàn bộ hệ thống cùng lúc. Màn test đầu tiên chỉ cần:

1 Fruit Spawner.

2 loại màu.

1 Conveyor.

1 ConveyorSwitch.

2 Bucket.

Không booster.

Không save/load.

Không worker.

Không combo.

Không timer.

Luồng nghiệm thu:

Tap Spawner
→ Dot rơi xuống Conveyor
→ Dot di chuyển
→ Switch đổi đường
→ Bucket reserve dot
→ Dot bị hút
→ Fill tăng
→ Bucket hoàn thành
→ Thắng màn
Sau khi luồng này ổn định mới thêm:

Release Bucket.

Move limit.

Time limit.

Combo và score.

Save/load.

Worker animation.

Booster.

6. Tách gameplay logic khỏi animation
Nên tách rõ ba lớp:

Model
├── số dot
├── màu
├── reservation
├── trạng thái
└── luật thắng/thua

View
├── SpriteRenderer
├── SpriteGridFill
├── animation
├── particle
└── âm thanh

Controller
├── nhận input
├── gọi model
└── đồng bộ view
Ví dụ Bucket không nên đợi animation hút dot kết thúc rồi mới quyết định có nhận dot hay không.

Reservation phải xảy ra ngay trong logic:

if (bucket.TryReserve(dot))
{
    dot.BeginAttraction(bucket);
}
Khi animation tới miệng Bucket:

bucket.TryCommitReservation(dot);
Nếu dot bị xóa hoặc thay đổi trạng thái giữa đường:

bucket.CancelReservation(dot);
7. Bổ sung vòng đời reservation
Tài liệu đã nhắc đến reservation nhưng cần mô tả đầy đủ các trường hợp hủy:

Reserved
├── dot đến Bucket        → Commit
├── Bucket bắt đầu release → Cancel
├── Bucket bị disable      → Cancel
├── Dot bị destroy         → Cancel
├── Level kết thúc         → Cancel
└── Attraction timeout     → Cancel
Có thể quản lý bằng ID:

private readonly HashSet<int> reservedDotIds = new();
Không chỉ lưu một biến số reservedCount, vì khi dot bị destroy giữa đường sẽ khó biết reservation nào cần trả lại.

8. Làm rõ thuật toán thua vì hết move
Đoạn “không còn khả năng hoàn thành Bucket” là một bài toán tương đối phức tạp.

Không nên ngay lập tức xây solver toàn bộ mạng Conveyor. Phiên bản đầu có thể dùng quy tắc bảo thủ:

bool HasPotentialProgress()
{
    return activeDots.Count > 0
        || releasingDots.Count > 0
        || spawners.Any(x => x.DotsLeft > 0);
}
Khi:

movesLeft == 0 && !HasPotentialProgress()
thì thua.

Sau này mới nâng cấp theo từng màu:

availableDotsByColor >= requiredSlotsByColor
Cần xác định rõ dot trong Bucket sai màu có được tính là “còn kiếm được” hay không, vì chúng chỉ có thể quay lại Conveyor nếu người chơi còn move để release.

9. Thêm Object Pooling từ đầu
Vì gameplay có thể có nhiều dot, nên dot không nên liên tục:

Instantiate
Destroy
Nên tạo:

DotPool
BucketEffectPool
ScorePopupPool
FallingPixelManager chỉ quản lý dữ liệu các dot đang active:

List<FallingPixel> activePixels;
Khi dot vào Bucket:

activePixels.Remove(dot);
dotPool.Release(dot);
Phần hiển thị dot trong Bucket có thể dùng sprite riêng hoặc pooled visual, không cần giữ dot runtime đầy đủ.

10. Chốt thứ tự update trong FallingPixelManager
Nên cố định pipeline để tránh lỗi cùng một dot bị bắt hai lần:

1. Remove invalid entries
2. Build spatial grid
3. Calculate separation
4. Update Falling dots
5. Update OnBelt dots
6. Resolve conveyor transitions
7. Find Bucket candidates
8. Reserve dots
9. Update Attracting dots
10. Commit captured dots
11. Apply transforms
12. Remove or pool completed dots
Không sửa trực tiếp danh sách đang iterate. Dùng các buffer:

toAdd
toRemove
toCapture
toLaunch
11. Bổ sung tiêu chí hiệu năng
Nên thêm benchmark cụ thể vào tài liệu:

Target device: Android tầm trung
Target FPS: 60 FPS
Dot đồng thời: 200
Conveyor: 20
Bucket: 10
GC allocation gameplay: 0 B/frame
Maximum draw calls: tùy art pipeline
Các ràng buộc nên có:

Không LINQ trong Update.

Không GetComponent theo frame.

Không FindObjectOfType.

Không tạo List, lambda hoặc coroutine mới cho từng dot.

Cache shader property IDs.

Spatial hash dùng lại collection.

MaterialPropertyBlock dùng lại, không tạo theo frame.

12. Bổ sung test plan
Các test quan trọng nhất:

Bucket capacity
Bucket capacity = 5
4 dot đã capture
3 dot cùng reserve trong một frame
Kết quả: chỉ 1 dot được reserve
Double capture
Một dot nằm trong vùng hút của hai Bucket
Kết quả: chỉ một Bucket có quyền reserve
Release giữa lúc đang hút
Bucket có dot đang Attracting
Người chơi release Bucket
Kết quả: reservation bị hủy hoặc release bị chặn theo luật thống nhất
End game idempotency
Hai Bucket hoàn thành cùng frame
Kết quả: onWin chỉ được gọi một lần
Fill shader
FillAmount = 0
FillAmount = 1
FillAmount < 0
FillAmount > 1
Sprite thay bằng animation
SpriteRenderer.flipX / flipY
Sprite Atlas packed sprite
Hai SpriteRenderer dùng chung material
13. Phần SpriteGridFill cần bổ sung
Đặc tả shader khá đầy đủ, nhưng nên xác định thêm:

Có hỗ trợ SpriteRenderer.flipX và flipY không.

Grid có xoay theo sprite khi sprite bị packed rotation trong atlas không.

Cell Gap tính theo phần trăm mỗi cell hay theo pixel màn hình.

Khi sprite dùng 9-slice hoặc tiled draw mode thì xử lý thế nào.

Có cần hỗ trợ Sprite Mask đầy đủ không.

Lit shader và unlit shader là hai shader riêng hay dùng keyword.

Bounds lấy từ sprite.rect, sprite.bounds hay local vertex positions.

Đề xuất giới hạn phiên bản đầu:

SpriteRenderer Draw Mode = Simple
Không hỗ trợ Tiled/Sliced
Hỗ trợ Sprite Atlas
Hỗ trợ flipX/flipY
Hỗ trợ Sprite Mask
Có hai shader riêng:
- Custom/SpriteGridFillUnlit
- Custom/SpriteGridFillLit2D
Thứ tự phát triển đề xuất
Phase 1
├── FruitData
├── Dot model
├── DotPool
├── Conveyor
└── FallingPixelManager

Phase 2
├── Bucket reservation
├── Bucket capture
├── Bucket release
└── SpriteGridFill

Phase 3
├── Fruit Spawner
├── ConveyorSwitch
├── GamePlayManager
└── Win/Lose

Phase 4
├── LevelData
├── LevelLoader
└── Level Editor

Phase 5
├── Score
├── Combo
├── HUD
└── Booster

Phase 6
├── Save/Load
├── Resume
├── Worker
└── Polish/performance
Ưu tiên cao nhất hiện tại là tách tài liệu, làm rõ TargetColorId và LockedColorId, sau đó viết LevelDataSchema.md và dựng vertical slice một Spawner–Conveyor–Bucket. Đây là nền tảng để tránh phải sửa kiến trúc khi gameplay đã phát triển lớn.




→

Hôm nay 12:33
bạn hãy update lại giúp tôi để tôi đưa lại vào cho codex chạy



Đã xử lý trong 2m 43s
Đã cập nhật tài liệu gốc thành đặc tả triển khai hoàn chỉnh cho Codex. 


FruitSort-Codex-Implementation-Spec.md

Bản mới đã chốt:

Phạm vi hiện tại là vertical slice: Spawner → Conveyor → Switch → Bucket → Win/Lose.

Phân biệt rõ TargetColorId và LockedColorId.

Bucket đầy sai màu không được tính hoàn thành.

Reservation theo từng Dot, không vượt capacity.

Dot state machine, pooling và thứ tự simulation.

SpriteGridFill dùng MaterialPropertyBlock, hỗ trợ Sprite Atlas.

Thứ tự triển khai bắt buộc theo từng phase.

Test case, acceptance criteria và Definition of Done.

Không cho Codex tự mở rộng sang save/load, booster, worker hoặc level editor quá sớm.

Có thể gửi file cho Codex cùng lệnh sau:

Đọc toàn bộ file FruitSort-Codex-Implementation-Spec.md, sau đó kiểm tra cấu trúc project Unity hiện tại và triển khai đúng theo thứ tự các phase trong tài liệu.

Ưu tiên mở rộng các class hiện có, không tạo hệ thống trùng chức năng, không phá serialization hoặc prefab/scene đang sử dụng.

Hãy triển khai trực tiếp code, shader và test cần thiết. Sau khi hoàn thành, trả về báo cáo đúng format tại mục “Kết quả Codex phải trả về”. Không được tuyên bố đã chạy Unity Editor hoặc test nếu môi trường không thực sự cho phép.

Thinking of follow-ups…



Continue

Show me an example

Summarize this
Sign in for unlimited smart follow-ups
Sign in →



→



Tối ưu Prompt
Thư viện Prompt
Thư viện
/
FruitSort-Codex-Implementation-Spec.md


# FruitSort — Codex Implementation Specification

## 0. Mục đích tài liệu

Tài liệu này là chỉ dẫn triển khai cho Codex trong project Unity hiện tại.

Mục tiêu của lần triển khai này là tạo một **vertical slice gameplay FruitSort có thể chạy được** gồm:

```text
Fruit Spawner
→ Dot rơi vào Conveyor
→ Dot di chuyển trên mạng Conveyor
→ ConveyorSwitch đổi nhánh
→ Bucket reserve và hút Dot
→ Bucket cập nhật SpriteGridFill
→ Bucket đúng màu hoàn thành
→ Tất cả Bucket đúng màu hoàn thành thì thắng
```

Không được coi shader hoặc trạng thái render là nguồn dữ liệu gameplay. Mọi luật màu, số lượng Dot, move, score và trạng thái thắng/thua phải nằm trong gameplay model/runtime state.

---

## 1. Quy tắc bắt buộc trước khi sửa code

Trước khi tạo hoặc chỉnh sửa file:

1. Kiểm tra phiên bản Unity trong `ProjectSettings/ProjectVersion.txt`.
2. Kiểm tra project đang dùng:
   - Built-in Render Pipeline; hoặc
   - URP/URP 2D Renderer.
3. Tìm các class hiện có có chức năng tương đương:
   - `GamePlayManager`
   - `LevelLoader`
   - `LevelData`
   - `FruitData`
   - `FallingPixelManager`
   - `ConveyorSpline`
   - `ConveyorSwitch`
   - `Bucket`
   - `ModelDotSpawner`
   - `SpriteGridFill`
4. Ưu tiên mở rộng code hiện có. Không tạo class trùng chức năng hoặc class có tên gần giống nếu project đã có implementation phù hợp.
5. Tuân thủ namespace, assembly definition, coding convention và cấu trúc thư mục hiện tại.
6. Không thêm package bên thứ ba.
7. Không tự thay đổi scene/prefab hàng loạt nếu không cần thiết.
8. Sau mỗi nhóm thay đổi, kiểm tra compile bằng công cụ khả dụng trong workspace.
9. Không được tuyên bố đã chạy Unity Editor hoặc Play Mode nếu môi trường không cho phép. Phải ghi rõ phần nào mới chỉ được kiểm tra tĩnh.

Nếu đặc tả này xung đột với code đã tồn tại, ưu tiên:

```text
Gameplay invariant
> dữ liệu level hiện có
> public API đang được scene/prefab sử dụng
> tên class được đề xuất trong tài liệu này
```

Không được phá serialization hiện có chỉ để đổi tên đẹp hơn.

---

## 2. Phạm vi triển khai hiện tại

### 2.1. Bắt buộc triển khai

- Fruit/Data color ID làm nguồn chân lý.
- Level data đủ để dựng vertical slice.
- Fruit Spawner nhả Dot theo batch.
- Dot pooling.
- Dot state machine.
- Conveyor movement và routing.
- ConveyorSwitch đổi nhánh, không tốn move.
- Bucket reservation, capture, correct/wrong color và release.
- `SpriteGridFill` dùng cho Bucket và Fruit Spawner.
- Move limit cơ bản.
- Time limit cơ bản, chỉ bắt đầu sau tương tác hợp lệ đầu tiên.
- Score cơ bản.
- Điều kiện win/loss idempotent.
- HUD hook/event cơ bản nếu project đã có HUD.
- Test hoặc validation tối thiểu cho các invariant quan trọng.

### 2.2. Chưa triển khai trong lần này

Chỉ tạo extension point hoặc TODO rõ ràng, không xây hệ thống hoàn chỉnh cho:

- Booster nam châm.
- Worker mang Bucket đi.
- Save/load và resume đầy đủ.
- Combo phức tạp.
- Starved/Tight solver hoàn chỉnh.
- Tutorial.
- Analytics.
- Monetization.
- Level Editor mới.
- Addressables.
- DOTS/ECS/Jobs/Burst migration.

Không được mở rộng phạm vi sang các hệ thống trên trừ khi chúng đã tồn tại và chỉ cần nối một API nhỏ để code compile.

---

## 3. Kiến trúc mong muốn

Tách gameplay thành ba lớp logic:

```text
Model / Runtime State
├── colorId
├── số lượng Dot
├── reservation
├── Bucket state
├── move/time/score
└── win/loss

View
├── SpriteRenderer
├── SpriteGridFill
├── animation
├── particle
└── audio

Controller / Manager
├── input validation
├── state transition
├── simulation update
└── đồng bộ View
```

Quy tắc:

- View không tự quyết định gameplay.
- Animation không được là điều kiện duy nhất để reserve/capture Dot.
- Shader không được thay đổi số Dot hoặc Bucket state.
- Gameplay logic phải có thể kiểm tra bằng code mà không phụ thuộc màu tint.

---

## 4. Cấu trúc thư mục đề xuất

Chỉ áp dụng nếu project chưa có cấu trúc tương đương:

```text
Assets/_Game/
├── Data/
│   ├── FruitData.cs
│   └── LevelData.cs
├── Runtime/
│   ├── Core/
│   │   ├── GamePlayManager.cs
│   │   └── FruitSortTypes.cs
│   ├── Dots/
│   │   ├── DotRuntime.cs
│   │   ├── DotPool.cs
│   │   └── FallingPixelManager.cs
│   ├── Conveyors/
│   │   ├── ConveyorSpline.cs
│   │   └── ConveyorSwitch.cs
│   ├── Buckets/
│   │   └── Bucket.cs
│   ├── Spawners/
│   │   └── ModelDotSpawner.cs
│   ├── Level/
│   │   └── LevelLoader.cs
│   └── Rendering/
│       └── SpriteGridFill.cs
├── Shaders/
│   ├── SpriteGridFill.shader
│   └── SpriteGridFillLit2D.shader   // chỉ khi URP 2D Lit thực sự cần
└── Materials/
    └── M_SpriteGridFill.mat         // chỉ tạo nếu phù hợp convention project
```

Không di chuyển file hiện có chỉ để khớp cấu trúc này.

---

## 5. Kiểu dữ liệu và enum lõi

Nếu project chưa có enum tương đương, tạo trong file dùng chung:

```csharp
public enum FruitSortGameState
{
    None = 0,
    Playing = 1,
    Won = 2,
    Lost = 3
}

public enum DotState
{
    InSpawner = 0,
    Falling = 1,
    OnBelt = 2,
    Attracting = 3,
    CapturedByBucket = 4,
    Launched = 5,
    Removed = 6
}

public enum BucketState
{
    Empty = 0,
    Receiving = 1,
    Releasing = 2,
    Completed = 3,
    Disabled = 4
}

public enum FruitSortLoseReason
{
    None = 0,
    OutOfTime = 1,
    OutOfMoves = 2
}
```

Tránh thay thế state machine bằng nhiều boolean chồng chéo như:

```text
isFull + isReleasing + isCompleted + isLocked + canReceive
```

Có thể giữ một số property dẫn xuất, nhưng nguồn chân lý phải là state rõ ràng.

---

## 6. FruitData và colorId

`colorId` là nguồn chân lý duy nhất cho luật màu.

Không được so sánh gameplay bằng:

- `SpriteRenderer.color`
- material tint
- shader property
- tên sprite
- tên GameObject

Nếu project chưa có `FruitData`, dùng ScriptableObject tối thiểu:

```csharp
[CreateAssetMenu(menuName = "FruitSort/Fruit Data")]
public sealed class FruitData : ScriptableObject
{
    [SerializeField] private int colorId;
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color displayColor = Color.white;

    public int ColorId => colorId;
    public Sprite Sprite => sprite;
    public Color DisplayColor => displayColor;
}
```

`displayColor` chỉ phục vụ hiển thị.

---

## 7. LevelData và ID ổn định

Mọi object cần khôi phục hoặc kết nối phải có ID ổn định.

Không dùng index của list làm ID lâu dài.

Nếu project chưa có schema tương đương, mở rộng hoặc tạo:

```csharp
[CreateAssetMenu(menuName = "FruitSort/Level Data")]
public sealed class LevelData : ScriptableObject
{
    [SerializeField] private string levelId;
    [Min(0)] [SerializeField] private int moveLimit;
    [Min(0f)] [SerializeField] private float timeLimit;

    [SerializeField] private List<SpawnerConfig> spawners = new();
    [SerializeField] private List<BucketConfig> buckets = new();
    [SerializeField] private List<ConveyorConfig> conveyors = new();
    [SerializeField] private List<ConveyorSwitchConfig> switches = new();
}
```

Các config tối thiểu:

```csharp
[Serializable]
public sealed class SpawnerConfig
{
    public string id;
    public int colorId;
    public int totalDots;
    public int dotsPerMove = 1;
    public Vector3 position;
    public string entryConveyorId;
}

[Serializable]
public sealed class BucketConfig
{
    public string id;
    public int targetColorId;
    public int capacity;
    public Vector3 position;
}

[Serializable]
public sealed class ConveyorConfig
{
    public string id;
    public List<Vector3> controlPoints = new();
    public List<string> nextConveyorIds = new();
    public float beltSpeed = 1f;
    public float beltWidth = 1f;
}

[Serializable]
public sealed class ConveyorSwitchConfig
{
    public string id;
    public string conveyorId;
    public List<string> branchConveyorIds = new();
    public int initialBranchIndex;
}
```

Nếu project dùng prefab reference hoặc transform data khác, giữ schema hiện có nhưng vẫn đảm bảo:

- ID không rỗng.
- ID không trùng trong cùng một level.
- Mọi connection ID phải resolve được.
- Capacity và số Dot không âm.

Thêm validation trong `OnValidate`, custom validator hoặc runtime guard phù hợp convention project.

---

## 8. GamePlayManager

`GamePlayManager` là nguồn chân lý cho:

- game state;
- moves left;
- time left;
- timer started;
- score;
- win/loss;
- event gameplay cấp cao.

API mong muốn hoặc API tương đương:

```csharp
public bool CanUseMove { get; }
public FruitSortGameState State { get; }
public int MovesLeft { get; }
public float TimeLeft { get; }
public int Score { get; }

public void ConfigureFromLevel(LevelData levelData);
public bool TryRecordMove();
public void RecordNonMoveInteraction();
public void OnDotSorted(DotRuntime dot, Bucket bucket);
public void OnBucketCompleted(Bucket bucket);
public void EvaluateEndConditions();
```

### 8.1. Luật move

| Tương tác | Tốn move |
| --- | ---: |
| Fruit Spawner nhả một batch hợp lệ | Có |
| Bucket có Dot bắt đầu release hợp lệ | Có |
| ConveyorSwitch đổi sang nhánh hợp lệ | Không |
| Tương tác bị chặn | Không |

Một thao tác tốn move chỉ được thay đổi gameplay sau khi `CanUseMove == true`.

Không trừ move nếu:

- Spawner rỗng.
- Bucket rỗng.
- Bucket đang release/completed.
- Game không ở `Playing`.
- Tương tác không tạo ra thay đổi state.

### 8.2. Bắt đầu timer

Timer chỉ bắt đầu sau tương tác gameplay hợp lệ đầu tiên:

- spawn một batch;
- release Bucket;
- đổi ConveyorSwitch hợp lệ nếu thiết kế hiện tại coi đây là tương tác bắt đầu level.

Ưu tiên bắt đầu timer cho mọi tương tác hợp lệ làm thay đổi state, kể cả switch, để tránh exploit đứng đổi nhánh trước khi timer chạy. Nếu game hiện tại có luật khác, giữ luật hiện có nhưng ghi rõ trong code.

### 8.3. Idempotency

Sau khi state là `Won` hoặc `Lost`:

- không nhận thêm input gameplay;
- không trừ move;
- không tăng score;
- không spawn/capture/release Dot mới;
- không gọi event kết thúc lần hai.

Dùng guard ở đầu public mutation method.

---

## 9. Dot runtime và state machine

State machine:

```text
InSpawner
    │ spawn
    ▼
Falling
    │ chạm/đến entry Conveyor
    ▼
OnBelt
    ├── Bucket reserve ─────────► Attracting ──► CapturedByBucket
    ├── hết mạng Conveyor ──────► Removed
    └── bị loại hợp lệ ─────────► Removed

CapturedByBucket
    │ Bucket release
    ▼
Launched
    │ quay lại entry/belt
    ├────────► Falling
    └────────► OnBelt
```

### 9.1. DotRuntime tối thiểu

Dot phải chứa runtime data độc lập với tint:

```csharp
public sealed class DotRuntime : MonoBehaviour
{
    public int RuntimeId { get; }
    public int ColorId { get; }
    public DotState State { get; }
    public ConveyorSpline CurrentConveyor { get; }
    public Bucket ReservedBucket { get; }
}
```

Có thể dùng struct runtime data riêng nếu project đang tối ưu theo data-oriented pattern.

### 9.2. Transition guard

Mọi transition phải đi qua method rõ ràng, ví dụ:

```csharp
bool TryEnterFalling(...);
bool TryEnterBelt(...);
bool TryBeginAttraction(Bucket bucket);
bool TryCapture(Bucket bucket);
bool TryLaunch(...);
void MarkRemoved();
```

Không set `State` tùy ý từ nhiều class.

### 9.3. Không xử lý hai lần

Dot ở các trạng thái sau không được update/score/capture lần nữa:

- `CapturedByBucket`
- `Removed`
- null/destroyed
- đã được đưa về pool

---

## 10. DotPool

Không dùng `Instantiate/Destroy` cho mỗi lần spawn/capture nếu gameplay có nhiều Dot.

Yêu cầu:

- Pool prefab Dot.
- `Get` phải reset toàn bộ runtime state.
- `Release` phải hủy reservation còn sót.
- Dot đã release về pool không còn nằm trong active simulation list.
- Không phát sinh GC mỗi frame từ pool.

Nếu project đã dùng `UnityEngine.Pool.ObjectPool<T>`, có thể dùng API đó. Không thêm package mới.

---

## 11. FallingPixelManager

Hệ thống mô phỏng dùng Transform, không bắt buộc Rigidbody.

Thứ tự update cố định:

```text
1. Dọn entry null/invalid/Removed
2. Rebuild hoặc update spatial grid
3. Tính separation từ các ô lân cận
4. Update Falling
5. Update OnBelt
6. Resolve cuối Conveyor và routing
7. Tìm Bucket candidate
8. Reserve Dot
9. Update Attracting
10. Commit capture tại mouth
11. Apply transform
12. Xử lý danh sách add/remove/pool
```

Không thêm/xóa trực tiếp active list trong lúc đang iterate. Dùng buffer tái sử dụng:

```text
toAdd
toRemove
toCapture
toLaunch
```

Không dùng LINQ trong update loop.

### 11.1. Spatial grid

Spatial grid chỉ cần kiểm tra cell hiện tại và cell lân cận.

Mục tiêu là tránh separation O(n²).

Yêu cầu:

- collection được reuse;
- không tạo dictionary/list mới mỗi frame nếu có thể;
- dot chỉ tương tác separation với dot active phù hợp;
- clamp lateral offset trong `beltWidth`.

---

## 12. ConveyorSpline và routing

Dot trên Conveyor có tối thiểu:

- normalized progress hoặc traveled distance;
- lateral offset;
- speed;
- current conveyor ID/reference.

Quy tắc:

1. Dot đi từ đầu đến cuối spline theo `beltSpeed`.
2. Có thể giữ jitter/rotation nhỏ nếu code hiện có hỗ trợ, nhưng không được làm dot thoát khỏi belt width.
3. Khi đến cuối Conveyor:
   - nếu có `ConveyorSwitch` đang điều khiển, dùng branch được chọn;
   - nếu không, dùng connection mặc định hợp lệ;
   - nếu không có connection hợp lệ, chuyển `Removed` và trả về pool.
4. Dot không tự quay lại đầu mạng khi hết đường.
5. Routing phải dựa trên reference/ID, không dựa trên tên GameObject.

Nếu project không có spline package, ưu tiên dùng implementation polyline/sample point hiện có. Không thêm spline package mới chỉ để hoàn thành task.

---

## 13. ConveyorSwitch

Yêu cầu:

- Chứa danh sách branch hợp lệ.
- `SelectedBranchIndex` luôn clamp trong range.
- Click/tap chuyển sang branch tiếp theo hoặc theo UI hiện có.
- Đổi switch không tốn move.
- Không đổi state khi game đã kết thúc.
- Phát event state changed nếu project đã có save hook.

API tương đương:

```csharp
public bool TrySelectNextBranch();
public ConveyorSpline GetSelectedBranch();
```

Không cho phép branch null hoặc connection không thuộc danh sách hợp lệ.

---

## 14. Bucket — mô hình màu bắt buộc

Bucket phải phân biệt hai khái niệm:

```csharp
public int TargetColorId { get; }
public int? LockedColorId { get; }
```

- `TargetColorId`: màu đúng để hoàn thành Bucket.
- `LockedColorId`: màu của Dot đầu tiên Bucket hiện đang chứa.
- Bucket rỗng có `LockedColorId == null`.
- Dot đầu tiên được commit sẽ khóa Bucket theo màu đó.
- Khi đã khóa, Bucket chỉ reserve thêm Dot có cùng `LockedColorId`.
- `LockedColorId` không thay đổi cho tới khi Bucket release hết hoặc được reset.

### 14.1. Bucket state machine

```text
Empty
  │ commit Dot đầu tiên
  ▼
Receiving
  ├── đủ capacity và LockedColorId == TargetColorId ─► Completed
  ├── đủ capacity nhưng sai màu ──────────────────────► Receiving (full wrong)
  └── người chơi release ─────────────────────────────► Releasing

Releasing
  │ release hết
  ▼
Empty
```

Bucket đầy sai màu **không được coi là completed**.

Có thể có property:

```csharp
public bool IsFull => CurrentFill >= MaxFill;
public bool IsCorrectColor => LockedColorId.HasValue && LockedColorId.Value == TargetColorId;
public bool IsCompleted => State == BucketState.Completed;
```

### 14.2. Số lượng và invariant

Bucket phải giữ:

```csharp
0 <= CurrentFill <= MaxFill
0 <= VisibleFill <= MaxFill
0 <= ReservedCount
CurrentFill + ReservedCount <= MaxFill
```

Ý nghĩa:

- `CurrentFill`: số Dot đã commit logic vào Bucket.
- `VisibleFill`: số ô đang hiển thị; giảm từng bước khi release animation.
- `ReservedCount`: Dot đang bay vào Bucket nhưng chưa commit.

### 14.3. Reservation lifecycle

Reservation phải quản lý theo `RuntimeId`, không chỉ bằng một counter nếu có khả năng Dot bị hủy giữa đường.

```text
Reserved
├── đến mouth hợp lệ       → Commit
├── Dot bị remove/pool     → Cancel
├── Bucket bắt đầu release → Cancel
├── Bucket disable/reset   → Cancel
├── level kết thúc         → Cancel
└── attraction timeout     → Cancel
```

API đề xuất hoặc tương đương:

```csharp
public bool TryReserve(DotRuntime dot);
public bool TryCommitReservation(DotRuntime dot);
public void CancelReservation(DotRuntime dot);
public bool TryReleaseContents();
```

`TryReserve` phải kiểm tra:

- game đang `Playing`;
- Bucket state là `Empty` hoặc `Receiving`;
- Bucket chưa completed/disabled/releasing;
- còn capacity sau khi tính reservation;
- Dot chưa được reserve bởi Bucket khác;
- màu Dot khớp `LockedColorId` nếu Bucket đã khóa.

`TryCommitReservation` phải kiểm tra lại reservation và capacity trong cùng frame.

### 14.4. Correct/wrong color

Khi commit Dot:

- Nếu `LockedColorId == TargetColorId`, Dot được tính sorted và có thể cộng score.
- Nếu sai màu, Dot vẫn hiển thị và chiếm capacity nhưng không cộng điểm.
- Bucket đầy sai màu không phát event completed.
- Người chơi phải release Bucket để mở khóa màu.

### 14.5. Release

Release chỉ hợp lệ khi:

- Bucket có ít nhất một Dot;
- game đang `Playing`;
- Bucket không completed;
- Bucket không đang release;
- có thể dùng move.

Khi release:

1. Trừ đúng một move sau khi xác nhận có nội dung để release.
2. Hủy reservation đang bay vào Bucket hoặc xử lý theo policy hiện có; mặc định hủy reservation để tránh commit vào Bucket đang release.
3. Chuyển state sang `Releasing`.
4. Nhả Dot lần lượt.
5. Sau mỗi Dot nhả, giảm `VisibleFill` ngay để shader giảm từng ô.
6. Dot chuyển sang `Launched`, sau đó quay lại `Falling` hoặc `OnBelt` qua entry hợp lệ.
7. Khi release hết:
   - `CurrentFill = 0`;
   - `VisibleFill = 0`;
   - `LockedColorId = null`;
   - state về `Empty`.

Không tạo Dot mới vô hạn khi release. Số Dot nhả ra phải bằng số Dot logic đã chứa.

### 14.6. Completion

Bucket chỉ hoàn thành khi:

```csharp
CurrentFill == MaxFill &&
LockedColorId.HasValue &&
LockedColorId.Value == TargetColorId
```

Khi completed:

- state chuyển `Completed` đúng một lần;
- ngừng reserve Dot;
- hủy reservation thừa nếu có;
- phát `OnBucketCompleted` đúng một lần;
- cộng score Bucket đúng một lần;
- chưa cần triển khai worker trong task hiện tại.

---

## 15. ModelDotSpawner

Spawner chứa:

- stable ID;
- `colorId`;
- `totalDots`;
- `dotsLeft`;
- `dotsPerMove`;
- entry Conveyor hoặc spawn target.

API tương đương:

```csharp
public bool TrySpawnBatch();
```

`TrySpawnBatch`:

1. Guard game state.
2. Guard `dotsLeft > 0`.
3. Guard `CanUseMove`.
4. Xác định `spawnCount = min(dotsPerMove, dotsLeft)`.
5. Chỉ sau khi xác nhận spawnCount > 0 mới trừ một move.
6. Lấy Dot từ pool và gán đúng `colorId`.
7. Giảm `dotsLeft` đúng số lượng đã spawn thành công.
8. Cập nhật:

```csharp
SpriteGridFill.FillAmount = totalDots > 0
    ? (float)dotsLeft / totalDots
    : 0f;
```

Nếu pool không cấp đủ Dot, không được âm `dotsLeft`; chỉ giảm theo số Dot thực tế đã spawn hoặc rollback thao tác theo policy nhất quán.

---

## 16. SpriteGridFill

### 16.1. Vai trò

`SpriteGridFill` chỉ là view component.

Nó không được tự thay đổi:

- số Dot;
- `colorId`;
- score;
- move;
- timer;
- Bucket state;
- win/loss.

### 16.2. Thuộc tính

- `Columns`: integer, minimum `1`.
- `Rows`: integer, minimum `1`.
- `FillAmount`: normalized `[0, 1]`.
- `CellGap`: normalized `[0, 0.45]` trong phạm vi mỗi cell.
- `Tint`: màu nhân với texture/vertex color.

Ví dụ grid `4 x 3`:

```text
 8  9 10 11
 4  5  6  7
 0  1  2  3
```

Fill từ trái sang phải, từ hàng dưới lên hàng trên.

### 16.3. Công thức fill

```text
totalCells   = Columns * Rows
progress     = saturate(FillAmount) * totalCells
cellIndex    = row * Columns + column
cellProgress = saturate(progress - cellIndex)
```

- `cellProgress == 0`: cell hoàn toàn transparent.
- `cellProgress == 1`: cell hiện đầy đủ.
- `0 < cellProgress < 1`: hiện từ mép trái sang phải.
- Phần chưa fill phải alpha bằng `0`, không chỉ làm tối màu.
- `FillAmount == 0` không được xuất hiện pixel line ngoài ý muốn.

### 16.4. Shader requirements

1. Tự kiểm tra render pipeline hiện tại.
2. Hỗ trợ `SpriteRenderer`.
3. Giữ texture alpha và vertex color.
4. Mặc định unlit.
5. Nếu project dùng URP 2D Lit và sprite hiện tại cần Light2D, tạo/giữ phiên bản lit tương thích thay vì làm hỏng lighting.
6. Grid dựa trên local normalized coordinate của sprite, không dựa trực tiếp vào atlas UV.
7. Hỗ trợ sprite trong Sprite Atlas và sprite cắt từ spritesheet.
8. Hỗ trợ `SpriteRenderer.flipX` và `flipY` hoặc ghi rõ limitation nếu API pipeline khiến việc hỗ trợ cần thay đổi ngoài phạm vi.
9. Giữ sorting layer và order in layer.
10. Giữ Sprite Mask nếu shader pipeline hiện tại hỗ trợ.
11. Phiên bản đầu chỉ bắt buộc `SpriteRenderer.drawMode == Simple`. Không cần hỗ trợ Tiled/Sliced nếu project không dùng.

### 16.5. C# component requirements

- Dùng `MaterialPropertyBlock`.
- Không gọi `renderer.material` trong update loop.
- Không tạo material instance theo frame.
- Cache shader property ID bằng `Shader.PropertyToID`.
- Reuse một `MaterialPropertyBlock` cho mỗi component.
- Clamp Columns/Rows/Fill/Gap.
- Cập nhật local bounds khi sprite thay đổi runtime, kể cả do animation.
- Không ghi đè property ngoài phạm vi component nếu project đang dùng MPB cho feature khác; phải đọc MPB hiện tại trước khi set property.
- Chỉ apply lại khi value hoặc sprite thay đổi, không bắt buộc set mỗi frame nếu không cần.

API mong muốn:

```csharp
public int Columns { get; set; }
public int Rows { get; set; }
public float FillAmount { get; set; }
public float CellGap { get; set; }
public Color Tint { get; set; }
public void Refresh();
```

Sử dụng:

```csharp
bucketGridFill.FillAmount = maxFill > 0
    ? (float)visibleFill / maxFill
    : 0f;
```

### 16.6. Bounds

Ưu tiên truyền local sprite bounds/rect cần thiết vào shader qua MPB.

Không giả định UV luôn từ `(0,0)` đến `(1,1)` của texture gốc vì sprite có thể nằm trong atlas.

Nếu mesh sprite không phải full rectangle, shader vẫn phải dùng local normalized coordinate nhất quán trên geometry hiện có.

---

## 17. Score và combo phạm vi hiện tại

Bắt buộc score cơ bản:

```text
Dot đúng màu commit: score += scorePerSorted
Bucket completed:     score += scorePerBucket
Dot sai màu:          không cộng điểm
```

Combo đầy đủ chưa bắt buộc trong vertical slice.

Nếu project đã có combo, giữ API hiện có nhưng đảm bảo:

- chỉ Dot đúng màu mới tăng combo;
- một Dot không ghi điểm hai lần;
- game kết thúc không nhận thêm score.

---

## 18. Win/Loss

### 18.1. Win

Win khi tất cả Bucket gameplay bắt buộc đều ở `Completed`.

Không dùng `Bucket.IsFull` đơn thuần vì Bucket có thể đầy sai màu.

Điều kiện đúng:

```csharp
allRequiredBuckets.All(bucket => bucket.State == BucketState.Completed)
```

Gọi win event đúng một lần.

### 18.2. OutOfTime

- Chỉ giảm timer sau khi timer đã được start.
- Khi `timeLeft <= 0`, clamp về `0` và lose `OutOfTime` đúng một lần.

### 18.3. OutOfMoves

Vertical slice dùng policy bảo thủ:

```csharp
movesLeft <= 0 &&
!HasPotentialProgress()
```

`HasPotentialProgress()` tối thiểu xét:

- Dot đang active trên scene;
- Dot đang Attracting;
- Dot đang Launched;
- Dot đang release;
- Dot đã reserve và có khả năng commit.

Dot còn trong Spawner nhưng không còn move không được coi là tiến triển khả dụng.

Không cần xây solver toàn bộ mạng Conveyor trong task hiện tại.

---

## 19. Event và HUD

Ưu tiên event thay vì HUD polling liên tục:

```csharp
public event Action<int> MovesChanged;
public event Action<float> TimeChanged;
public event Action<int> ScoreChanged;
public event Action<int> ActiveDotCountChanged;
public event Action<FruitSortLoseReason> Lost;
public event Action Won;
```

Chỉ cập nhật text khi value thực sự thay đổi.

Nếu project dùng UnityEvent, có thể giữ UnityEvent thay vì ép đổi sang C# event.

Sau thao tác tốn move, nếu project đã có save hook:

```text
onInteractionRecorded
```

Sau thay đổi không tốn move nhưng ảnh hưởng gameplay, ví dụ switch:

```text
onStateChangedForSave
```

Không triển khai save serialization đầy đủ trong task hiện tại.

---

## 20. Hiệu năng và cấp phát

Mục tiêu tham chiếu cho vertical slice:

```text
Active Dot đồng thời: ít nhất 200
Conveyor:             ít nhất 20
Bucket:               ít nhất 10
Gameplay GC:          hướng tới 0 B/frame trong steady state
```

Bắt buộc:

- Không LINQ trong `Update`, `LateUpdate`, simulation loop.
- Không `FindObjectOfType`/`FindFirstObjectByType` theo frame.
- Không `GetComponent` theo frame nếu có thể cache.
- Không tạo lambda/coroutine/list mới cho mỗi Dot mỗi frame.
- Không dùng `renderer.material` theo frame.
- Reuse list, dictionary, MPB và buffers.
- Pool Dot.
- Không log spam theo frame.

Không cần micro-optimize editor-only code.

---

## 21. Thứ tự triển khai bắt buộc

Codex phải triển khai theo thứ tự để giảm lỗi kiến trúc:

### Phase A — Audit và compile baseline

- Xác định pipeline và version.
- Lập danh sách file/class hiện có liên quan.
- Ghi nhận compile error sẵn có trước khi sửa.

### Phase B — Core types/data

- Enum/state.
- FruitData/colorId.
- LevelData validation cần thiết.
- GamePlayManager state/move/time/score guard.

### Phase C — SpriteGridFill độc lập

- Shader đúng pipeline.
- Component MPB.
- Inspector/runtime API.
- Không nối gameplay trước khi shader/component compile.

### Phase D — Dot simulation

- DotRuntime.
- DotPool.
- FallingPixelManager.
- Conveyor movement/routing.
- ConveyorSwitch.

### Phase E — Spawner và Bucket

- Spawn batch/move.
- Reservation lifecycle.
- Capture correct/wrong.
- Release.
- Completion.
- Đồng bộ grid fill.

### Phase F — End conditions và HUD hooks

- Score.
- Win/loss idempotent.
- Events/HUD integration.

### Phase G — Validation

- Compile.
- Static review invariant.
- Play Mode/manual checklist nếu Editor khả dụng.
- Ghi rõ limitation còn lại.

Không bắt đầu worker, booster hoặc save/load đầy đủ trước khi Phase G đạt acceptance criteria.

---

## 22. Acceptance criteria

### 22.1. SpriteGridFill

- `FillAmount = 0`: sprite hoàn toàn không nhìn thấy.
- `FillAmount = 1`: toàn bộ sprite hiển thị, trừ khoảng gap.
- Grid `4 x 3` fill cell `0 → 11` đúng thứ tự.
- Cell hiện tại fill mượt từ trái sang phải.
- Hàng fill từ dưới lên trên.
- Không có one-pixel artifact tại fill `0`.
- Sprite Atlas không làm lệch grid.
- Hai SpriteRenderer dùng chung material có fill riêng.
- Sprite thay runtime cập nhật bounds.
- Không tạo material instance theo frame.
- Không phá alpha và vertex color.

### 22.2. Spawner

- Spawner rỗng không trừ move.
- Spawn batch hợp lệ trừ đúng một move.
- `dotsLeft` không âm.
- Fill của gói bằng `dotsLeft / totalDots`.
- Dot spawn có đúng `colorId`.

### 22.3. Conveyor

- Dot đi đúng chiều và không thoát belt width bất hợp lý.
- Switch đổi branch mà không trừ move.
- Dot dùng branch đang chọn.
- Hết network thì Dot được remove/pool, không loop ngầm.

### 22.4. Bucket reservation

Test bắt buộc:

```text
Capacity = 5
CurrentFill = 4
3 Dot cùng cố reserve trong một frame
Kết quả: chỉ 1 Dot reserve thành công
```

```text
Một Dot nằm trong vùng của hai Bucket
Kết quả: chỉ một Bucket sở hữu reservation
```

```text
Dot đã reserve bị remove/pool
Kết quả: reservation được cancel và capacity được trả lại
```

### 22.5. Bucket màu

- Bucket rỗng nhận Dot đầu tiên và khóa theo màu đó.
- Bucket đã khóa chỉ nhận cùng màu khóa.
- Dot đúng TargetColor cộng score.
- Dot sai TargetColor không cộng score.
- Bucket đầy sai màu không completed và không tính win.
- Release hết làm `LockedColorId = null`.
- Release giảm visible fill từng Dot.

### 22.6. Completion/end state

```text
Hai Bucket hoàn thành cùng frame
Kết quả: win event chỉ gọi một lần
```

```text
Game đã Won/Lost
Kết quả: input tiếp theo không spawn, release, trừ move hoặc cộng score
```

```text
Timer chưa start
Kết quả: timeLeft không giảm
```

```text
MovesLeft = 0 nhưng Dot active vẫn có thể vào Bucket
Kết quả: chưa lose ngay
```

---

## 23. Yêu cầu test và validation

Nếu project có test assembly, thêm EditMode test cho logic thuần hoặc PlayMode test phù hợp.

Ưu tiên test:

1. Bucket reservation capacity.
2. Double reservation prevention.
3. Correct/wrong completion.
4. Release reset.
5. Game end idempotency.
6. SpriteGridFill property clamping.

Nếu không thể chạy test vì môi trường thiếu Unity Editor:

- vẫn viết test nếu project hỗ trợ;
- kiểm tra compile/static reference trong khả năng;
- không ghi “tests passed”;
- ghi rõ “tests added but not executed”.

---

## 24. Quy tắc an toàn khi chỉnh project

- Không xóa asset người dùng.
- Không regenerate GUID/meta không cần thiết.
- Không sửa package manifest trừ khi bắt buộc và được giải thích.
- Không thay đổi Input System toàn project nếu chỉ cần xử lý click/tap cục bộ.
- Không thay đổi render pipeline.
- Không đổi tag/layer/sorting layer tự động nếu chưa xác minh chúng tồn tại.
- Không tạo singleton mới nếu project đã có dependency/injection pattern.
- Không dùng static registry không được cleanup qua domain reload/scene unload.

---

## 25. Kết quả Codex phải trả về

Sau khi hoàn thành, trả về báo cáo theo format:

```markdown
## Implemented
- ...

## Modified/Created Files
- `path/to/file.cs`: mục đích
- `path/to/file.shader`: mục đích

## Integration Instructions
1. ...
2. ...

## Validation Performed
- Compile/static check: ...
- Unity Editor/Play Mode: ...
- Tests: ...

## Remaining Limitations
- ...

## Existing Issues Not Caused By This Change
- ...
```

Không được bỏ qua phần limitation hoặc existing issue.

---

## 26. Definition of Done

Task chỉ được coi là hoàn thành khi:

- Code compile trong phạm vi có thể xác minh.
- Không có class duplicate chức năng.
- Vertical slice Spawner → Conveyor → Switch → Bucket hoạt động theo logic đã nêu hoặc đã được nối đầy đủ về mặt code nếu không thể chạy Editor.
- Bucket phân biệt `TargetColorId` và `LockedColorId`.
- Bucket đầy sai màu không thắng.
- Reservation không vượt capacity.
- Dot được pool.
- `SpriteGridFill` dùng MPB và hỗ trợ material dùng chung.
- Win/loss idempotent.
- Có hướng dẫn gắn component/prefab/scene cần thiết.
- Báo cáo trung thực phần chưa được chạy thử.

Minimap

Side Chat

Dấu trang

Ghi chú

Gợi ý tiếp theo FruitSort
Thư viện
/
FruitSort-Codex-Implementation-Spec.md


# FruitSort — Codex Implementation Specification

## 0. Mục đích tài liệu

Tài liệu này là chỉ dẫn triển khai cho Codex trong project Unity hiện tại.

Mục tiêu của lần triển khai này là tạo một **vertical slice gameplay FruitSort có thể chạy được** gồm:

```text
Fruit Spawner
→ Dot rơi vào Conveyor
→ Dot di chuyển trên mạng Conveyor
→ ConveyorSwitch đổi nhánh
→ Bucket reserve và hút Dot
→ Bucket cập nhật SpriteGridFill
→ Bucket đúng màu hoàn thành
→ Tất cả Bucket đúng màu hoàn thành thì thắng
```

Không được coi shader hoặc trạng thái render là nguồn dữ liệu gameplay. Mọi luật màu, số lượng Dot, move, score và trạng thái thắng/thua phải nằm trong gameplay model/runtime state.

---

## 1. Quy tắc bắt buộc trước khi sửa code

Trước khi tạo hoặc chỉnh sửa file:

1. Kiểm tra phiên bản Unity trong `ProjectSettings/ProjectVersion.txt`.
2. Kiểm tra project đang dùng:
   - Built-in Render Pipeline; hoặc
   - URP/URP 2D Renderer.
3. Tìm các class hiện có có chức năng tương đương:
   - `GamePlayManager`
   - `LevelLoader`
   - `LevelData`
   - `FruitData`
   - `FallingPixelManager`
   - `ConveyorSpline`
   - `ConveyorSwitch`
   - `Bucket`
   - `ModelDotSpawner`
   - `SpriteGridFill`
4. Ưu tiên mở rộng code hiện có. Không tạo class trùng chức năng hoặc class có tên gần giống nếu project đã có implementation phù hợp.
5. Tuân thủ namespace, assembly definition, coding convention và cấu trúc thư mục hiện tại.
6. Không thêm package bên thứ ba.
7. Không tự thay đổi scene/prefab hàng loạt nếu không cần thiết.
8. Sau mỗi nhóm thay đổi, kiểm tra compile bằng công cụ khả dụng trong workspace.
9. Không được tuyên bố đã chạy Unity Editor hoặc Play Mode nếu môi trường không cho phép. Phải ghi rõ phần nào mới chỉ được kiểm tra tĩnh.

Nếu đặc tả này xung đột với code đã tồn tại, ưu tiên:

```text
Gameplay invariant
> dữ liệu level hiện có
> public API đang được scene/prefab sử dụng
> tên class được đề xuất trong tài liệu này
```

Không được phá serialization hiện có chỉ để đổi tên đẹp hơn.

---

## 2. Phạm vi triển khai hiện tại

### 2.1. Bắt buộc triển khai

- Fruit/Data color ID làm nguồn chân lý.
- Level data đủ để dựng vertical slice.
- Fruit Spawner nhả Dot theo batch.
- Dot pooling.
- Dot state machine.
- Conveyor movement và routing.
- ConveyorSwitch đổi nhánh, không tốn move.
- Bucket reservation, capture, correct/wrong color và release.
- `SpriteGridFill` dùng cho Bucket và Fruit Spawner.
- Move limit cơ bản.
- Time limit cơ bản, chỉ bắt đầu sau tương tác hợp lệ đầu tiên.
- Score cơ bản.
- Điều kiện win/loss idempotent.
- HUD hook/event cơ bản nếu project đã có HUD.
- Test hoặc validation tối thiểu cho các invariant quan trọng.

### 2.2. Chưa triển khai trong lần này

Chỉ tạo extension point hoặc TODO rõ ràng, không xây hệ thống hoàn chỉnh cho:

- Booster nam châm.
- Worker mang Bucket đi.
- Save/load và resume đầy đủ.
- Combo phức tạp.
- Starved/Tight solver hoàn chỉnh.
- Tutorial.
- Analytics.
- Monetization.
- Level Editor mới.
- Addressables.
- DOTS/ECS/Jobs/Burst migration.

Không được mở rộng phạm vi sang các hệ thống trên trừ khi chúng đã tồn tại và chỉ cần nối một API nhỏ để code compile.

---

## 3. Kiến trúc mong muốn

Tách gameplay thành ba lớp logic:

```text
Model / Runtime State
├── colorId
├── số lượng Dot
├── reservation
├── Bucket state
├── move/time/score
└── win/loss

View
├── SpriteRenderer
├── SpriteGridFill
├── animation
├── particle
└── audio

Controller / Manager
├── input validation
├── state transition
├── simulation update
└── đồng bộ View
```

Quy tắc:

- View không tự quyết định gameplay.
- Animation không được là điều kiện duy nhất để reserve/capture Dot.
- Shader không được thay đổi số Dot hoặc Bucket state.
- Gameplay logic phải có thể kiểm tra bằng code mà không phụ thuộc màu tint.

---

## 4. Cấu trúc thư mục đề xuất

Chỉ áp dụng nếu project chưa có cấu trúc tương đương:

```text
Assets/_Game/
├── Data/
│   ├── FruitData.cs
│   └── LevelData.cs
├── Runtime/
│   ├── Core/
│   │   ├── GamePlayManager.cs
│   │   └── FruitSortTypes.cs
│   ├── Dots/
│   │   ├── DotRuntime.cs
│   │   ├── DotPool.cs
│   │   └── FallingPixelManager.cs
│   ├── Conveyors/
│   │   ├── ConveyorSpline.cs
│   │   └── ConveyorSwitch.cs
│   ├── Buckets/
│   │   └── Bucket.cs
│   ├── Spawners/
│   │   └── ModelDotSpawner.cs
│   ├── Level/
│   │   └── LevelLoader.cs
│   └── Rendering/
│       └── SpriteGridFill.cs
├── Shaders/
│   ├── SpriteGridFill.shader
│   └── SpriteGridFillLit2D.shader   // chỉ khi URP 2D Lit thực sự cần
└── Materials/
    └── M_SpriteGridFill.mat         // chỉ tạo nếu phù hợp convention project
```

Không di chuyển file hiện có chỉ để khớp cấu trúc này.

---

## 5. Kiểu dữ liệu và enum lõi

Nếu project chưa có enum tương đương, tạo trong file dùng chung:

```csharp
public enum FruitSortGameState
{
    None = 0,
    Playing = 1,
    Won = 2,
    Lost = 3
}

public enum DotState
{
    InSpawner = 0,
    Falling = 1,
    OnBelt = 2,
    Attracting = 3,
    CapturedByBucket = 4,
    Launched = 5,
    Removed = 6
}

public enum BucketState
{
    Empty = 0,
    Receiving = 1,
    Releasing = 2,
    Completed = 3,
    Disabled = 4
}

public enum FruitSortLoseReason
{
    None = 0,
    OutOfTime = 1,
    OutOfMoves = 2
}
```

Tránh thay thế state machine bằng nhiều boolean chồng chéo như:

```text
isFull + isReleasing + isCompleted + isLocked + canReceive
```

Có thể giữ một số property dẫn xuất, nhưng nguồn chân lý phải là state rõ ràng.

---

## 6. FruitData và colorId

`colorId` là nguồn chân lý duy nhất cho luật màu.

Không được so sánh gameplay bằng:

- `SpriteRenderer.color`
- material tint
- shader property
- tên sprite
- tên GameObject

Nếu project chưa có `FruitData`, dùng ScriptableObject tối thiểu:

```csharp
[CreateAssetMenu(menuName = "FruitSort/Fruit Data")]
public sealed class FruitData : ScriptableObject
{
    [SerializeField] private int colorId;
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color displayColor = Color.white;

    public int ColorId => colorId;
    public Sprite Sprite => sprite;
    public Color DisplayColor => displayColor;
}
```

`displayColor` chỉ phục vụ hiển thị.

---

## 7. LevelData và ID ổn định

Mọi object cần khôi phục hoặc kết nối phải có ID ổn định.

Không dùng index của list làm ID lâu dài.

Nếu project chưa có schema tương đương, mở rộng hoặc tạo:

```csharp
[CreateAssetMenu(menuName = "FruitSort/Level Data")]
public sealed class LevelData : ScriptableObject
{
    [SerializeField] private string levelId;
    [Min(0)] [SerializeField] private int moveLimit;
    [Min(0f)] [SerializeField] private float timeLimit;

    [SerializeField] private List<SpawnerConfig> spawners = new();
    [SerializeField] private List<BucketConfig> buckets = new();
    [SerializeField] private List<ConveyorConfig> conveyors = new();
    [SerializeField] private List<ConveyorSwitchConfig> switches = new();
}
```

Các config tối thiểu:

```csharp
[Serializable]
public sealed class SpawnerConfig
{
    public string id;
    public int colorId;
    public int totalDots;
    public int dotsPerMove = 1;
    public Vector3 position;
    public string entryConveyorId;
}

[Serializable]
public sealed class BucketConfig
{
    public string id;
    public int targetColorId;
    public int capacity;
    public Vector3 position;
}

[Serializable]
public sealed class ConveyorConfig
{
    public string id;
    public List<Vector3> controlPoints = new();
    public List<string> nextConveyorIds = new();
    public float beltSpeed = 1f;
    public float beltWidth = 1f;
}

[Serializable]
public sealed class ConveyorSwitchConfig
{
    public string id;
    public string conveyorId;
    public List<string> branchConveyorIds = new();
    public int initialBranchIndex;
}
```

Nếu project dùng prefab reference hoặc transform data khác, giữ schema hiện có nhưng vẫn đảm bảo:

- ID không rỗng.
- ID không trùng trong cùng một level.
- Mọi connection ID phải resolve được.
- Capacity và số Dot không âm.

Thêm validation trong `OnValidate`, custom validator hoặc runtime guard phù hợp convention project.

---

## 8. GamePlayManager

`GamePlayManager` là nguồn chân lý cho:

- game state;
- moves left;
- time left;
- timer started;
- score;
- win/loss;
- event gameplay cấp cao.

API mong muốn hoặc API tương đương:

```csharp
public bool CanUseMove { get; }
public FruitSortGameState State { get; }
public int MovesLeft { get; }
public float TimeLeft { get; }
public int Score { get; }

public void ConfigureFromLevel(LevelData levelData);
public bool TryRecordMove();
public void RecordNonMoveInteraction();
public void OnDotSorted(DotRuntime dot, Bucket bucket);
public void OnBucketCompleted(Bucket bucket);
public void EvaluateEndConditions();
```

### 8.1. Luật move

| Tương tác | Tốn move |
| --- | ---: |
| Fruit Spawner nhả một batch hợp lệ | Có |
| Bucket có Dot bắt đầu release hợp lệ | Có |
| ConveyorSwitch đổi sang nhánh hợp lệ | Không |
| Tương tác bị chặn | Không |

Một thao tác tốn move chỉ được thay đổi gameplay sau khi `CanUseMove == true`.

Không trừ move nếu:

- Spawner rỗng.
- Bucket rỗng.
- Bucket đang release/completed.
- Game không ở `Playing`.
- Tương tác không tạo ra thay đổi state.

### 8.2. Bắt đầu timer

Timer chỉ bắt đầu sau tương tác gameplay hợp lệ đầu tiên:

- spawn một batch;
- release Bucket;
- đổi ConveyorSwitch hợp lệ nếu thiết kế hiện tại coi đây là tương tác bắt đầu level.

Ưu tiên bắt đầu timer cho mọi tương tác hợp lệ làm thay đổi state, kể cả switch, để tránh exploit đứng đổi nhánh trước khi timer chạy. Nếu game hiện tại có luật khác, giữ luật hiện có nhưng ghi rõ trong code.

### 8.3. Idempotency

Sau khi state là `Won` hoặc `Lost`:

- không nhận thêm input gameplay;
- không trừ move;
- không tăng score;
- không spawn/capture/release Dot mới;
- không gọi event kết thúc lần hai.

Dùng guard ở đầu public mutation method.

---

## 9. Dot runtime và state machine

State machine:

```text
InSpawner
    │ spawn
    ▼
Falling
    │ chạm/đến entry Conveyor
    ▼
OnBelt
    ├── Bucket reserve ─────────► Attracting ──► CapturedByBucket
    ├── hết mạng Conveyor ──────► Removed
    └── bị loại hợp lệ ─────────► Removed

CapturedByBucket
    │ Bucket release
    ▼
Launched
    │ quay lại entry/belt
    ├────────► Falling
    └────────► OnBelt
```

### 9.1. DotRuntime tối thiểu

Dot phải chứa runtime data độc lập với tint:

```csharp
public sealed class DotRuntime : MonoBehaviour
{
    public int RuntimeId { get; }
    public int ColorId { get; }
    public DotState State { get; }
    public ConveyorSpline CurrentConveyor { get; }
    public Bucket ReservedBucket { get; }
}
```

Có thể dùng struct runtime data riêng nếu project đang tối ưu theo data-oriented pattern.

### 9.2. Transition guard

Mọi transition phải đi qua method rõ ràng, ví dụ:

```csharp
bool TryEnterFalling(...);
bool TryEnterBelt(...);
bool TryBeginAttraction(Bucket bucket);
bool TryCapture(Bucket bucket);
bool TryLaunch(...);
void MarkRemoved();
```

Không set `State` tùy ý từ nhiều class.

### 9.3. Không xử lý hai lần

Dot ở các trạng thái sau không được update/score/capture lần nữa:

- `CapturedByBucket`
- `Removed`
- null/destroyed
- đã được đưa về pool

---

## 10. DotPool

Không dùng `Instantiate/Destroy` cho mỗi lần spawn/capture nếu gameplay có nhiều Dot.

Yêu cầu:

- Pool prefab Dot.
- `Get` phải reset toàn bộ runtime state.
- `Release` phải hủy reservation còn sót.
- Dot đã release về pool không còn nằm trong active simulation list.
- Không phát sinh GC mỗi frame từ pool.

Nếu project đã dùng `UnityEngine.Pool.ObjectPool<T>`, có thể dùng API đó. Không thêm package mới.

---

## 11. FallingPixelManager

Hệ thống mô phỏng dùng Transform, không bắt buộc Rigidbody.

Thứ tự update cố định:

```text
1. Dọn entry null/invalid/Removed
2. Rebuild hoặc update spatial grid
3. Tính separation từ các ô lân cận
4. Update Falling
5. Update OnBelt
6. Resolve cuối Conveyor và routing
7. Tìm Bucket candidate
8. Reserve Dot
9. Update Attracting
10. Commit capture tại mouth
11. Apply transform
12. Xử lý danh sách add/remove/pool
```

Không thêm/xóa trực tiếp active list trong lúc đang iterate. Dùng buffer tái sử dụng:

```text
toAdd
toRemove
toCapture
toLaunch
```

Không dùng LINQ trong update loop.

### 11.1. Spatial grid

Spatial grid chỉ cần kiểm tra cell hiện tại và cell lân cận.

Mục tiêu là tránh separation O(n²).

Yêu cầu:

- collection được reuse;
- không tạo dictionary/list mới mỗi frame nếu có thể;
- dot chỉ tương tác separation với dot active phù hợp;
- clamp lateral offset trong `beltWidth`.

---

## 12. ConveyorSpline và routing

Dot trên Conveyor có tối thiểu:

- normalized progress hoặc traveled distance;
- lateral offset;
- speed;
- current conveyor ID/reference.

Quy tắc:

1. Dot đi từ đầu đến cuối spline theo `beltSpeed`.
2. Có thể giữ jitter/rotation nhỏ nếu code hiện có hỗ trợ, nhưng không được làm dot thoát khỏi belt width.
3. Khi đến cuối Conveyor:
   - nếu có `ConveyorSwitch` đang điều khiển, dùng branch được chọn;
   - nếu không, dùng connection mặc định hợp lệ;
   - nếu không có connection hợp lệ, chuyển `Removed` và trả về pool.
4. Dot không tự quay lại đầu mạng khi hết đường.
5. Routing phải dựa trên reference/ID, không dựa trên tên GameObject.

Nếu project không có spline package, ưu tiên dùng implementation polyline/sample point hiện có. Không thêm spline package mới chỉ để hoàn thành task.

---

## 13. ConveyorSwitch

Yêu cầu:

- Chứa danh sách branch hợp lệ.
- `SelectedBranchIndex` luôn clamp trong range.
- Click/tap chuyển sang branch tiếp theo hoặc theo UI hiện có.
- Đổi switch không tốn move.
- Không đổi state khi game đã kết thúc.
- Phát event state changed nếu project đã có save hook.

API tương đương:

```csharp
public bool TrySelectNextBranch();
public ConveyorSpline GetSelectedBranch();
```

Không cho phép branch null hoặc connection không thuộc danh sách hợp lệ.

---

## 14. Bucket — mô hình màu bắt buộc

Bucket phải phân biệt hai khái niệm:

```csharp
public int TargetColorId { get; }
public int? LockedColorId { get; }
```

- `TargetColorId`: màu đúng để hoàn thành Bucket.
- `LockedColorId`: màu của Dot đầu tiên Bucket hiện đang chứa.
- Bucket rỗng có `LockedColorId == null`.
- Dot đầu tiên được commit sẽ khóa Bucket theo màu đó.
- Khi đã khóa, Bucket chỉ reserve thêm Dot có cùng `LockedColorId`.
- `LockedColorId` không thay đổi cho tới khi Bucket release hết hoặc được reset.

### 14.1. Bucket state machine

```text
Empty
  │ commit Dot đầu tiên
  ▼
Receiving
  ├── đủ capacity và LockedColorId == TargetColorId ─► Completed
  ├── đủ capacity nhưng sai màu ──────────────────────► Receiving (full wrong)
  └── người chơi release ─────────────────────────────► Releasing

Releasing
  │ release hết
  ▼
Empty
```

Bucket đầy sai màu **không được coi là completed**.

Có thể có property:

```csharp
public bool IsFull => CurrentFill >= MaxFill;
public bool IsCorrectColor => LockedColorId.HasValue && LockedColorId.Value == TargetColorId;
public bool IsCompleted => State == BucketState.Completed;
```

### 14.2. Số lượng và invariant

Bucket phải giữ:

```csharp
0 <= CurrentFill <= MaxFill
0 <= VisibleFill <= MaxFill
0 <= ReservedCount
CurrentFill + ReservedCount <= MaxFill
```

Ý nghĩa:

- `CurrentFill`: số Dot đã commit logic vào Bucket.
- `VisibleFill`: số ô đang hiển thị; giảm từng bước khi release animation.
- `ReservedCount`: Dot đang bay vào Bucket nhưng chưa commit.

### 14.3. Reservation lifecycle

Reservation phải quản lý theo `RuntimeId`, không chỉ bằng một counter nếu có khả năng Dot bị hủy giữa đường.

```text
Reserved
├── đến mouth hợp lệ       → Commit
├── Dot bị remove/pool     → Cancel
├── Bucket bắt đầu release → Cancel
├── Bucket disable/reset   → Cancel
├── level kết thúc         → Cancel
└── attraction timeout     → Cancel
```

API đề xuất hoặc tương đương:

```csharp
public bool TryReserve(DotRuntime dot);
public bool TryCommitReservation(DotRuntime dot);
public void CancelReservation(DotRuntime dot);
public bool TryReleaseContents();
```

`TryReserve` phải kiểm tra:

- game đang `Playing`;
- Bucket state là `Empty` hoặc `Receiving`;
- Bucket chưa completed/disabled/releasing;
- còn capacity sau khi tính reservation;
- Dot chưa được reserve bởi Bucket khác;
- màu Dot khớp `LockedColorId` nếu Bucket đã khóa.

`TryCommitReservation` phải kiểm tra lại reservation và capacity trong cùng frame.

### 14.4. Correct/wrong color

Khi commit Dot:

- Nếu `LockedColorId == TargetColorId`, Dot được tính sorted và có thể cộng score.
- Nếu sai màu, Dot vẫn hiển thị và chiếm capacity nhưng không cộng điểm.
- Bucket đầy sai màu không phát event completed.
- Người chơi phải release Bucket để mở khóa màu.

### 14.5. Release

Release chỉ hợp lệ khi:

- Bucket có ít nhất một Dot;
- game đang `Playing`;
- Bucket không completed;
- Bucket không đang release;
- có thể dùng move.

Khi release:

1. Trừ đúng một move sau khi xác nhận có nội dung để release.
2. Hủy reservation đang bay vào Bucket hoặc xử lý theo policy hiện có; mặc định hủy reservation để tránh commit vào Bucket đang release.
3. Chuyển state sang `Releasing`.
4. Nhả Dot lần lượt.
5. Sau mỗi Dot nhả, giảm `VisibleFill` ngay để shader giảm từng ô.
6. Dot chuyển sang `Launched`, sau đó quay lại `Falling` hoặc `OnBelt` qua entry hợp lệ.
7. Khi release hết:
   - `CurrentFill = 0`;
   - `VisibleFill = 0`;
   - `LockedColorId = null`;
   - state về `Empty`.

Không tạo Dot mới vô hạn khi release. Số Dot nhả ra phải bằng số Dot logic đã chứa.

### 14.6. Completion

Bucket chỉ hoàn thành khi:

```csharp
CurrentFill == MaxFill &&
LockedColorId.HasValue &&
LockedColorId.Value == TargetColorId
```

Khi completed:

- state chuyển `Completed` đúng một lần;
- ngừng reserve Dot;
- hủy reservation thừa nếu có;
- phát `OnBucketCompleted` đúng một lần;
- cộng score Bucket đúng một lần;
- chưa cần triển khai worker trong task hiện tại.

---

## 15. ModelDotSpawner

Spawner chứa:

- stable ID;
- `colorId`;
- `totalDots`;
- `dotsLeft`;
- `dotsPerMove`;
- entry Conveyor hoặc spawn target.

API tương đương:

```csharp
public bool TrySpawnBatch();
```

`TrySpawnBatch`:

1. Guard game state.
2. Guard `dotsLeft > 0`.
3. Guard `CanUseMove`.
4. Xác định `spawnCount = min(dotsPerMove, dotsLeft)`.
5. Chỉ sau khi xác nhận spawnCount > 0 mới trừ một move.
6. Lấy Dot từ pool và gán đúng `colorId`.
7. Giảm `dotsLeft` đúng số lượng đã spawn thành công.
8. Cập nhật:

```csharp
SpriteGridFill.FillAmount = totalDots > 0
    ? (float)dotsLeft / totalDots
    : 0f;
```

Nếu pool không cấp đủ Dot, không được âm `dotsLeft`; chỉ giảm theo số Dot thực tế đã spawn hoặc rollback thao tác theo policy nhất quán.

---

## 16. SpriteGridFill

### 16.1. Vai trò

`SpriteGridFill` chỉ là view component.

Nó không được tự thay đổi:

- số Dot;
- `colorId`;
- score;
- move;
- timer;
- Bucket state;
- win/loss.

### 16.2. Thuộc tính

- `Columns`: integer, minimum `1`.
- `Rows`: integer, minimum `1`.
- `FillAmount`: normalized `[0, 1]`.
- `CellGap`: normalized `[0, 0.45]` trong phạm vi mỗi cell.
- `Tint`: màu nhân với texture/vertex color.

Ví dụ grid `4 x 3`:

```text
 8  9 10 11
 4  5  6  7
 0  1  2  3
```

Fill từ trái sang phải, từ hàng dưới lên hàng trên.

### 16.3. Công thức fill

```text
totalCells   = Columns * Rows
progress     = saturate(FillAmount) * totalCells
cellIndex    = row * Columns + column
cellProgress = saturate(progress - cellIndex)
```

- `cellProgress == 0`: cell hoàn toàn transparent.
- `cellProgress == 1`: cell hiện đầy đủ.
- `0 < cellProgress < 1`: hiện từ mép trái sang phải.
- Phần chưa fill phải alpha bằng `0`, không chỉ làm tối màu.
- `FillAmount == 0` không được xuất hiện pixel line ngoài ý muốn.

### 16.4. Shader requirements

1. Tự kiểm tra render pipeline hiện tại.
2. Hỗ trợ `SpriteRenderer`.
3. Giữ texture alpha và vertex color.
4. Mặc định unlit.
5. Nếu project dùng URP 2D Lit và sprite hiện tại cần Light2D, tạo/giữ phiên bản lit tương thích thay vì làm hỏng lighting.
6. Grid dựa trên local normalized coordinate của sprite, không dựa trực tiếp vào atlas UV.
7. Hỗ trợ sprite trong Sprite Atlas và sprite cắt từ spritesheet.
8. Hỗ trợ `SpriteRenderer.flipX` và `flipY` hoặc ghi rõ limitation nếu API pipeline khiến việc hỗ trợ cần thay đổi ngoài phạm vi.
9. Giữ sorting layer và order in layer.
10. Giữ Sprite Mask nếu shader pipeline hiện tại hỗ trợ.
11. Phiên bản đầu chỉ bắt buộc `SpriteRenderer.drawMode == Simple`. Không cần hỗ trợ Tiled/Sliced nếu project không dùng.

### 16.5. C# component requirements

- Dùng `MaterialPropertyBlock`.
- Không gọi `renderer.material` trong update loop.
- Không tạo material instance theo frame.
- Cache shader property ID bằng `Shader.PropertyToID`.
- Reuse một `MaterialPropertyBlock` cho mỗi component.
- Clamp Columns/Rows/Fill/Gap.
- Cập nhật local bounds khi sprite thay đổi runtime, kể cả do animation.
- Không ghi đè property ngoài phạm vi component nếu project đang dùng MPB cho feature khác; phải đọc MPB hiện tại trước khi set property.
- Chỉ apply lại khi value hoặc sprite thay đổi, không bắt buộc set mỗi frame nếu không cần.

API mong muốn:

```csharp
public int Columns { get; set; }
public int Rows { get; set; }
public float FillAmount { get; set; }
public float CellGap { get; set; }
public Color Tint { get; set; }
public void Refresh();
```

Sử dụng:

```csharp
bucketGridFill.FillAmount = maxFill > 0
    ? (float)visibleFill / maxFill
    : 0f;
```

### 16.6. Bounds

Ưu tiên truyền local sprite bounds/rect cần thiết vào shader qua MPB.

Không giả định UV luôn từ `(0,0)` đến `(1,1)` của texture gốc vì sprite có thể nằm trong atlas.

Nếu mesh sprite không phải full rectangle, shader vẫn phải dùng local normalized coordinate nhất quán trên geometry hiện có.

---

## 17. Score và combo phạm vi hiện tại

Bắt buộc score cơ bản:

```text
Dot đúng màu commit: score += scorePerSorted
Bucket completed:     score += scorePerBucket
Dot sai màu:          không cộng điểm
```

Combo đầy đủ chưa bắt buộc trong vertical slice.

Nếu project đã có combo, giữ API hiện có nhưng đảm bảo:

- chỉ Dot đúng màu mới tăng combo;
- một Dot không ghi điểm hai lần;
- game kết thúc không nhận thêm score.

---

## 18. Win/Loss

### 18.1. Win

Win khi tất cả Bucket gameplay bắt buộc đều ở `Completed`.

Không dùng `Bucket.IsFull` đơn thuần vì Bucket có thể đầy sai màu.

Điều kiện đúng:

```csharp
allRequiredBuckets.All(bucket => bucket.State == BucketState.Completed)
```

Gọi win event đúng một lần.

### 18.2. OutOfTime

- Chỉ giảm timer sau khi timer đã được start.
- Khi `timeLeft <= 0`, clamp về `0` và lose `OutOfTime` đúng một lần.

### 18.3. OutOfMoves

Vertical slice dùng policy bảo thủ:

```csharp
movesLeft <= 0 &&
!HasPotentialProgress()
```

`HasPotentialProgress()` tối thiểu xét:

- Dot đang active trên scene;
- Dot đang Attracting;
- Dot đang Launched;
- Dot đang release;
- Dot đã reserve và có khả năng commit.

Dot còn trong Spawner nhưng không còn move không được coi là tiến triển khả dụng.

Không cần xây solver toàn bộ mạng Conveyor trong task hiện tại.

---

## 19. Event và HUD

Ưu tiên event thay vì HUD polling liên tục:

```csharp
public event Action<int> MovesChanged;
public event Action<float> TimeChanged;
public event Action<int> ScoreChanged;
public event Action<int> ActiveDotCountChanged;
public event Action<FruitSortLoseReason> Lost;
public event Action Won;
```

Chỉ cập nhật text khi value thực sự thay đổi.

Nếu project dùng UnityEvent, có thể giữ UnityEvent thay vì ép đổi sang C# event.

Sau thao tác tốn move, nếu project đã có save hook:

```text
onInteractionRecorded
```

Sau thay đổi không tốn move nhưng ảnh hưởng gameplay, ví dụ switch:

```text
onStateChangedForSave
```

Không triển khai save serialization đầy đủ trong task hiện tại.

---

## 20. Hiệu năng và cấp phát

Mục tiêu tham chiếu cho vertical slice:

```text
Active Dot đồng thời: ít nhất 200
Conveyor:             ít nhất 20
Bucket:               ít nhất 10
Gameplay GC:          hướng tới 0 B/frame trong steady state
```

Bắt buộc:

- Không LINQ trong `Update`, `LateUpdate`, simulation loop.
- Không `FindObjectOfType`/`FindFirstObjectByType` theo frame.
- Không `GetComponent` theo frame nếu có thể cache.
- Không tạo lambda/coroutine/list mới cho mỗi Dot mỗi frame.
- Không dùng `renderer.material` theo frame.
- Reuse list, dictionary, MPB và buffers.
- Pool Dot.
- Không log spam theo frame.

Không cần micro-optimize editor-only code.

---

## 21. Thứ tự triển khai bắt buộc

Codex phải triển khai theo thứ tự để giảm lỗi kiến trúc:

### Phase A — Audit và compile baseline

- Xác định pipeline và version.
- Lập danh sách file/class hiện có liên quan.
- Ghi nhận compile error sẵn có trước khi sửa.

### Phase B — Core types/data

- Enum/state.
- FruitData/colorId.
- LevelData validation cần thiết.
- GamePlayManager state/move/time/score guard.

### Phase C — SpriteGridFill độc lập

- Shader đúng pipeline.
- Component MPB.
- Inspector/runtime API.
- Không nối gameplay trước khi shader/component compile.

### Phase D — Dot simulation

- DotRuntime.
- DotPool.
- FallingPixelManager.
- Conveyor movement/routing.
- ConveyorSwitch.

### Phase E — Spawner và Bucket

- Spawn batch/move.
- Reservation lifecycle.
- Capture correct/wrong.
- Release.
- Completion.
- Đồng bộ grid fill.

### Phase F — End conditions và HUD hooks

- Score.
- Win/loss idempotent.
- Events/HUD integration.

### Phase G — Validation

- Compile.
- Static review invariant.
- Play Mode/manual checklist nếu Editor khả dụng.
- Ghi rõ limitation còn lại.

Không bắt đầu worker, booster hoặc save/load đầy đủ trước khi Phase G đạt acceptance criteria.

---

## 22. Acceptance criteria

### 22.1. SpriteGridFill

- `FillAmount = 0`: sprite hoàn toàn không nhìn thấy.
- `FillAmount = 1`: toàn bộ sprite hiển thị, trừ khoảng gap.
- Grid `4 x 3` fill cell `0 → 11` đúng thứ tự.
- Cell hiện tại fill mượt từ trái sang phải.
- Hàng fill từ dưới lên trên.
- Không có one-pixel artifact tại fill `0`.
- Sprite Atlas không làm lệch grid.
- Hai SpriteRenderer dùng chung material có fill riêng.
- Sprite thay runtime cập nhật bounds.
- Không tạo material instance theo frame.
- Không phá alpha và vertex color.

### 22.2. Spawner

- Spawner rỗng không trừ move.
- Spawn batch hợp lệ trừ đúng một move.
- `dotsLeft` không âm.
- Fill của gói bằng `dotsLeft / totalDots`.
- Dot spawn có đúng `colorId`.

### 22.3. Conveyor

- Dot đi đúng chiều và không thoát belt width bất hợp lý.
- Switch đổi branch mà không trừ move.
- Dot dùng branch đang chọn.
- Hết network thì Dot được remove/pool, không loop ngầm.

### 22.4. Bucket reservation

Test bắt buộc:

```text
Capacity = 5
CurrentFill = 4
3 Dot cùng cố reserve trong một frame
Kết quả: chỉ 1 Dot reserve thành công
```

```text
Một Dot nằm trong vùng của hai Bucket
Kết quả: chỉ một Bucket sở hữu reservation
```

```text
Dot đã reserve bị remove/pool
Kết quả: reservation được cancel và capacity được trả lại
```

### 22.5. Bucket màu

- Bucket rỗng nhận Dot đầu tiên và khóa theo màu đó.
- Bucket đã khóa chỉ nhận cùng màu khóa.
- Dot đúng TargetColor cộng score.
- Dot sai TargetColor không cộng score.
- Bucket đầy sai màu không completed và không tính win.
- Release hết làm `LockedColorId = null`.
- Release giảm visible fill từng Dot.

### 22.6. Completion/end state

```text
Hai Bucket hoàn thành cùng frame
Kết quả: win event chỉ gọi một lần
```

```text
Game đã Won/Lost
Kết quả: input tiếp theo không spawn, release, trừ move hoặc cộng score
```

```text
Timer chưa start
Kết quả: timeLeft không giảm
```

```text
MovesLeft = 0 nhưng Dot active vẫn có thể vào Bucket
Kết quả: chưa lose ngay
```

---

## 23. Yêu cầu test và validation

Nếu project có test assembly, thêm EditMode test cho logic thuần hoặc PlayMode test phù hợp.

Ưu tiên test:

1. Bucket reservation capacity.
2. Double reservation prevention.
3. Correct/wrong completion.
4. Release reset.
5. Game end idempotency.
6. SpriteGridFill property clamping.

Nếu không thể chạy test vì môi trường thiếu Unity Editor:

- vẫn viết test nếu project hỗ trợ;
- kiểm tra compile/static reference trong khả năng;
- không ghi “tests passed”;
- ghi rõ “tests added but not executed”.

---

## 24. Quy tắc an toàn khi chỉnh project

- Không xóa asset người dùng.
- Không regenerate GUID/meta không cần thiết.
- Không sửa package manifest trừ khi bắt buộc và được giải thích.
- Không thay đổi Input System toàn project nếu chỉ cần xử lý click/tap cục bộ.
- Không thay đổi render pipeline.
- Không đổi tag/layer/sorting layer tự động nếu chưa xác minh chúng tồn tại.
- Không tạo singleton mới nếu project đã có dependency/injection pattern.
- Không dùng static registry không được cleanup qua domain reload/scene unload.

---

## 25. Kết quả Codex phải trả về

Sau khi hoàn thành, trả về báo cáo theo format:

```markdown
## Implemented
- ...

## Modified/Created Files
- `path/to/file.cs`: mục đích
- `path/to/file.shader`: mục đích

## Integration Instructions
1. ...
2. ...

## Validation Performed
- Compile/static check: ...
- Unity Editor/Play Mode: ...
- Tests: ...

## Remaining Limitations
- ...

## Existing Issues Not Caused By This Change
- ...
```

Không được bỏ qua phần limitation hoặc existing issue.

---

## 26. Definition of Done

Task chỉ được coi là hoàn thành khi:

- Code compile trong phạm vi có thể xác minh.
- Không có class duplicate chức năng.
- Vertical slice Spawner → Conveyor → Switch → Bucket hoạt động theo logic đã nêu hoặc đã được nối đầy đủ về mặt code nếu không thể chạy Editor.
- Bucket phân biệt `TargetColorId` và `LockedColorId`.
- Bucket đầy sai màu không thắng.
- Reservation không vượt capacity.
- Dot được pool.
- `SpriteGridFill` dùng MPB và hỗ trợ material dùng chung.
- Win/loss idempotent.
- Có hướng dẫn gắn component/prefab/scene cần thiết.
- Báo cáo trung thực phần chưa được chạy thử.
