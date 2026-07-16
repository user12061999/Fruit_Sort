# FruitSort — Gameplay Implementation Specification

## 1. Mục tiêu

Triển khai vertical slice gameplay FruitSort:

```text
Fruit Spawner → Dot rơi vào Conveyor → ConveyorSwitch chọn nhánh
→ Bucket reserve/hút Dot → SpriteGridFill cập nhật
→ Bucket đúng màu hoàn thành → tất cả Bucket hoàn thành: thắng màn
```

Shader, tint, tên sprite và tên GameObject không được là nguồn dữ liệu gameplay. Luật màu,
số Dot, move, điểm, timer và win/loss phải nằm trong runtime state.

## 2. Phạm vi

### Bắt buộc

- `colorId` là nguồn chân lý cho màu.
- Spawner nhả Dot theo batch; Dot được pool và di chuyển trên Conveyor.
- ConveyorSwitch đổi nhánh, không tốn move.
- Bucket reserve, capture, release và phân biệt màu đúng/sai.
- `SpriteGridFill` hiển thị fill của Spawner và Bucket.
- Move limit, time limit, score, win/loss idempotent và validation/test tối thiểu.

### Không làm trong scope này

Không xây mới booster, worker, save/load hoàn chỉnh, combo phức tạp, tutorial, analytics,
monetization, level editor, Addressables hoặc DOTS/ECS. Chỉ nối API tối thiểu của hệ thống
đã tồn tại nếu cần để không làm hỏng compile hay serialized reference.

## 3. Quy tắc chỉnh project

1. Kiểm tra Unity version, render pipeline và class hiện có trước khi sửa.
2. Ưu tiên mở rộng `GamePlayManager`, `LevelData`, `FallingPixelManager`, `Bucket`,
   `ModelDotSpawner`, `ConveyorSpline`, `ConveyorSwitch` và `SpriteGridFill`.
3. Không tạo class trùng vai trò, không thêm package, không đổi pipeline, không đổi hàng
   loạt scene/prefab khi không cần.
4. Không phá serialization hoặc public API đang được scene/prefab dùng chỉ để đổi tên.
5. Sau mỗi nhóm thay đổi, kiểm tra compile bằng công cụ sẵn có. Không tuyên bố đã chạy
   Unity Editor, Play Mode hoặc test nếu chưa thực sự chạy.

Khi đặc tả xung đột với code: gameplay invariant → dữ liệu level hiện có → public API và
serialization đang dùng → tên đề xuất trong tài liệu.

## 4. Kiến trúc

```text
Model / Runtime State     View                         Controller
colorId                   SpriteRenderer               input validation
dot count                 SpriteGridFill               state transition
bucket reservation        animation / VFX / audio      simulation update
move / time / score                                     view synchronization
win / loss
```

- View không tự thay đổi gameplay.
- Animation không quyết định reservation hoặc completion.
- Một Dot chỉ có một owner logic tại một thời điểm.
- So sánh gameplay bằng `colorId`, không bằng màu render.

## 5. Game state và input

`GamePlayManager` là nguồn chân lý của `Playing`, `Won`, `Lost`, move, timer, score và event
kết thúc.

| Tương tác | Điều kiện | Kết quả | Tốn move |
| --- | --- | --- | ---: |
| Spawner | Còn Dot, game Playing, còn move | Nhả một batch | Có |
| Bucket | Có Dot, chưa completed/releasing, còn move | Nhả toàn bộ Dot | Có |
| Switch | Có nhánh hợp lệ, game Playing | Chọn nhánh kế tiếp | Không |

- Timer chỉ bắt đầu từ tương tác hợp lệ đầu tiên có thay đổi gameplay.
- Tương tác bị chặn không được trừ move, đổi state hoặc phát save event.
- Sau Won/Lost không spawn, release, cộng điểm hoặc phát event end thêm lần nữa.

## 6. Dot state machine

```text
InSpawner → Falling → OnBelt → Attracting → CapturedByBucket
CapturedByBucket → Launched → Falling / OnBelt
OnBelt → Removed (không còn đường Conveyor)
```

| State | Ý nghĩa |
| --- | --- |
| `InSpawner` | Chưa nhả khỏi gói. |
| `Falling` | Đi vào Conveyor. |
| `OnBelt` | Có progress trên spline và lateral offset. |
| `Attracting` | Đã được Bucket reserve, đang bay tới miệng Bucket. |
| `CapturedByBucket` | Đã commit vào Bucket, không còn mô phỏng. |
| `Launched` | Được Bucket nhả, có vận tốc/hướng ban đầu. |
| `Removed` | Rời level, trả về pool. |

`FallingPixelManager` cập nhật theo thứ tự: loại entry invalid → xây spatial grid dùng lại
collection → separation → cập nhật Falling/Launched/OnBelt → chuyển Conveyor → reserve
Bucket → cập nhật Attracting/commit/cancel → ghi transform → pool Dot Removed.

Không thêm/xóa trực tiếp khỏi collection đang iterate; dùng buffer.

## 7. Conveyor và switch

- Dot đi từ đầu tới cuối `ConveyorSpline` theo `beltSpeed`, luôn clamp trong belt width.
- Separation chỉ kiểm tra spatial hash lân cận, không quét toàn bộ Dot theo cặp.
- Cuối belt: ưu tiên nhánh do `ConveyorSwitch` chọn; nếu không có, dùng connection mặc
  định; nếu không có nhánh hợp lệ, Dot thành `Removed`.
- Routing dùng reference hoặc ID ổn định, không dùng tên GameObject.
- Switch clamp selected index, bỏ branch null và chỉ phát sự kiện khi nhánh thật sự đổi.

## 8. Bucket: màu và trạng thái

```csharp
int TargetColorId;  // màu đúng để hoàn thành
int? LockedColorId; // màu Dot đầu tiên đã commit; null khi rỗng
int CurrentFill;    // Dot đã commit
int VisibleFill;    // ô render đang hiện
int MaxFill;
```

`TargetColorId` không đổi trong level. `LockedColorId` được đặt khi commit Dot đầu tiên và
chỉ reset khi Bucket release hết hoặc level reset.

```text
Empty → Receiving
Receiving + full đúng màu → Completed
Receiving + full sai màu → Receiving (full wrong)
Receiving + release hợp lệ → Releasing → Empty
```

Bucket đầy sai màu không được Completed, không phát event completion và không đóng góp win.

### Reservation

Reservation phải theo từng Dot, không chỉ bằng counter:

```text
Reserved → tới mouth: Commit
Reserved → Dot remove/pool: Cancel
Reserved → Bucket release/disable: Cancel
Reserved → level end/timeout: Cancel
```

`TryReserve(dot)` chỉ thành công khi game Playing; Bucket Empty/Receiving; còn chỗ sau khi
tính reservation; Dot chưa được Bucket khác reserve; và, nếu đã lock, màu Dot khớp
`LockedColorId`. `TryCommitReservation(dot)` kiểm tra lại reservation và capacity.

Invariant:

```text
0 <= CurrentFill <= MaxFill
0 <= VisibleFill <= MaxFill
CurrentFill + ReservedCount <= MaxFill
```

### Commit và release

- Dot đúng màu commit: cộng `scorePerSorted` đúng một lần.
- Dot sai màu: chiếm ô, không cộng điểm.
- Full với `LockedColorId == TargetColorId`: state `Completed`, ngừng nhận Dot, cộng
  `scorePerBucket` và báo `GamePlayManager` đúng một lần.
- Release chỉ hợp lệ khi có nội dung, chưa completed và còn move. Sau guard hợp lệ: trừ một
  move, cancel reservation đang bay tới, nhả từng Dot sang `Launched`, và giảm `VisibleFill`
  từng Dot. Khi rỗng: fill = 0, lock = null, state `Empty`.

## 9. Spawner

Spawner giữ `colorId`, `totalDots`, `dotsLeft`, `dotsPerMove` và entry Conveyor.

`TrySpawnBatch()` thực hiện: guard game/move/dotsLeft → tính batch → lấy Dot từ pool và gán
`colorId` → chỉ giảm `dotsLeft` theo Dot spawn thành công → nếu đã spawn, ghi một move và
cập nhật grid fill. Spawner rỗng hoặc spawn thất bại không trừ move; `dotsLeft` không âm.

## 10. Điểm và end conditions

```text
Dot đúng màu commit: score += scorePerSorted
Bucket completed:    score += scorePerBucket
Dot sai màu:         không cộng điểm
```

- Win khi mọi Bucket bắt buộc ở `Completed`; `onWin` gọi một lần.
- Timer chỉ giảm sau khi start. Về 0 thì clamp và lose `OutOfTime` một lần.
- Hết move chỉ lose khi chưa thắng và không còn Dot active, attracting, launched,
  releasing hoặc reserved có thể commit. Dot còn trong Spawner khi không còn move không
  tính là tiến triển khả dụng.
- Cảnh báo Tight/Starved chỉ là HUD, không tự gây thua.

## 11. SpriteGridFill

`SpriteGridFill` chỉ là view: không thay đổi Dot, `colorId`, score, move, timer, Bucket
state hoặc win/loss.

```csharp
gridFill.FillAmount = maxFill > 0
    ? (float)visibleFill / maxFill
    : 0f;
```

- Bucket dùng `VisibleFill / MaxFill`; Spawner dùng `dotsLeft / totalDots`.
- Columns/Rows tối thiểu 1; FillAmount clamp `[0, 1]`; CellGap clamp `[0, 0.45]`.
- Fill từ trái sang phải, hàng dưới lên hàng trên:

```text
 8  9 10 11
 4  5  6  7
 0  1  2  3
```

```text
totalCells   = Columns * Rows
progress     = saturate(FillAmount) * totalCells
cellIndex    = row * Columns + column
cellProgress = saturate(progress - cellIndex)
```

`cellProgress = 0` phải transparent, `= 1` hiện đầy, ở giữa fill từ trái sang phải; fill 0
không có one-pixel artifact.

### Ràng buộc shader/component

1. Dùng đúng render pipeline, `SpriteRenderer`, texture alpha và vertex color.
2. Dùng local normalized coordinate, không UV texture gốc; hỗ trợ atlas/spritesheet và cập
   nhật bounds khi sprite đổi runtime.
3. Mặc định unlit; chỉ thêm URP 2D Lit khi sprite cần Light2D thực tế.
4. Phiên bản đầu chỉ bắt buộc Draw Mode Simple; nêu rõ limitation Tiled/Sliced nếu có.
5. Dùng `MaterialPropertyBlock`, cache property ID, reuse MPB; không `renderer.material`
   hay tạo material mới theo frame.
6. Chỉ apply khi giá trị/sprite đổi, không ghi đè property MPB ngoài feature này.

## 12. Level data, HUD và hiệu năng

- Object cần kết nối/khôi phục có ID không rỗng, không trùng; không dùng list index làm ID
  bền vững. Validate color, capacity, dot count, connection và branch.
- HUD chỉ cập nhật khi score, dot count, moves hoặc time đổi.
- Sau move phát `onInteractionRecorded`; sau thay đổi không tốn move nhưng ảnh hưởng
  gameplay (ví dụ Switch), phát `onStateChangedForSave` nếu hook tồn tại.
- Mục tiêu: 200 Dot, 20 Conveyor, 10 Bucket, hướng tới 0 B GC/frame steady state.
- Không LINQ, Find, GetComponent, lambda, List mới hoặc log spam trong simulation loop.
  Reuse spatial hash, buffers, MPB và pool Dot.

## 13. Thứ tự triển khai

1. Audit pipeline, Unity version, class hiện có và compile baseline.
2. Chốt state/color/LevelData validation/move-time-score guards.
3. Hoàn thiện SpriteGridFill độc lập và compile.
4. Hoàn thiện Dot pool, simulation, Conveyor và Switch.
5. Hoàn thiện Spawner, Bucket reservation/capture/release/completion và fill sync.
6. Nối score, end conditions và HUD hooks.
7. Chạy validation/test trong khả năng môi trường, nêu rõ phần chưa chạy.

## 14. Tiêu chí nghiệm thu

- Spawner rỗng không trừ move; batch hợp lệ trừ đúng một move và sinh Dot đúng colorId.
- Switch không trừ move; Dot đi nhánh đã chọn và bị pool khi hết network.
- Capacity 5, fill 4, ba Dot reserve cùng frame: chỉ một Dot thành công.
- Một Dot trong vùng hai Bucket: chỉ một Bucket sở hữu reservation.
- Dot reserved bị pool: reservation cancel và trả capacity.
- Bucket đầy sai màu không completed, không cộng điểm Bucket, không thắng; release hết mở
  lock và visible fill giảm từng Dot.
- Hai Bucket hoàn thành cùng frame: onWin chỉ gọi một lần.
- Timer chưa start không giảm; game Won/Lost không nhận input/score mới.
- Grid fill đúng thứ tự 0→11; Fill 0 trong suốt; atlas không lệch; hai renderer dùng chung
  material vẫn có fill riêng.
