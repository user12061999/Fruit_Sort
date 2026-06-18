# Hướng dẫn Setup — Bắn Dot Pixel + Băng Chuyền (Splines)

7 script nằm trong `Assets/_Game/Script/`, đều thuộc namespace **`FruitSort`**.
Không dùng Rigidbody cho tương tác dot–dot; mọi chuyển động chạy bằng Transform.

> **Input System (New):** project đặt `activeInputHandler = 1` nên `Shooter` dùng
> `UnityEngine.InputSystem` (`Mouse.current`). Giữ **chuột trái** để bắn.

## 0. CÁCH NHANH NHẤT — tự dựng scene 1 click

Đã thêm script editor `Assets/_Game/Script/Editor/FruitSortSceneBuilder.cs`.
1. Mở 1 scene bất kỳ (vd `Assets/_Game/Scenes/SampleScene.unity`).
2. Menu **Tools > FruitSort > Build Sample Scene**.
3. Tự tạo: Camera (orthographic), Conveyor (SplineContainer + ConveyorSpline, 4 knot),
   FallingPixelManager, PixelGridManager + GridOrigin, Shooter (kèm LineRenderer tracer),
   GameManager và 4 Bucket (mỗi màu 1 thùng) đặt sát đường spline.
4. Bấm **Play**, giữ **chuột trái** để bắn. Bấm **Ctrl+S** để lưu scene.

Mọi object builder tạo có tiền tố tên `_FS_`; chạy lại menu sẽ xoá & dựng lại sạch.
Phần dưới là hướng dẫn **dựng tay** nếu muốn tùy biến chi tiết.

## 1. Cài Splines Package

Dự án này **đã có sẵn** `com.unity.splines@2.8.4` (kiểm tra trong `Packages/manifest.json`).
Nếu cần cài lại ở project khác:

1. `Window > Package Manager`
2. Góc trên trái: `+` → **Add package by name…**
3. Nhập `com.unity.splines` , version `2.8.4` → **Add**

## 2. Tạo Sprite cho Dot (nếu chưa có)

Đã có `Assets/_Game/Sprites/WhiteDot.png`. Đảm bảo:
- Texture Type = **Sprite (2D and UI)**
- (Tuỳ chọn) Nếu dùng `sourceTexture` trong PixelGridManager để lấy màu từ ảnh,
  bật **Read/Write Enabled** trên ảnh nguồn đó.

## 3. Prefab `Dot`

1. Kéo `WhiteDot` vào Scene → đổi tên **Dot**.
2. Add component **Dot** (script).
3. (Tuỳ chọn) Add **CircleCollider2D** nếu muốn Shooter dùng Physics2D raycast
   (script vẫn chạy được không cần collider nhờ fallback hình học).
4. Kéo vào `Assets/_Game/Prefabs/` để tạo prefab, rồi xoá khỏi scene.

## 4. Setup Scene

Mở `Assets/_Game/Scenes/Gameplay.unity`. Cần các object sau:

**Camera**
- Main Camera, **Projection = Orthographic**, Size ~ 6–8.
- Đặt sao cho thấy lưới (trên) và băng chuyền (dưới).

**Spline (băng chuyền)**
1. `GameObject > Spline > Spline` → tạo object có `SplineContainer`.
2. Vẽ đường đi (nằm ngang, hơi cong tuỳ ý) ở phần dưới màn hình.
3. Add component **ConveyorSpline** vào chính object đó (nó `RequireComponent SplineContainer`).
   - `Belt Width`: bề rộng băng chuyền (vd 3). Chọn object để thấy 2 mép cyan (gizmo).

**Managers** (tạo các empty GameObject)
- `FallingPixelManager` (script cùng tên):
  - `Conveyor` = kéo object ConveyorSpline vào.
  - `Dot Size` = khớp với scale dot (vd 0.5).
  - `Belt Entry Y` = cao độ Y miệng băng chuyền (chỗ t=0 của spline).
  - `Max Dots` = 500.
- `PixelGridManager`:
  - `Dot Prefab` = prefab Dot.
  - `Falling Manager` = kéo FallingPixelManager.
  - `Grid Origin` = (tuỳ chọn) 1 empty đặt ở góc dưới-trái lưới.
  - `Columns/Rows/Spacing/Dot HP/Palette` chỉnh theo ý.
- `GameManager`:
  - Gán `Grid Manager`, `Falling Manager`, và (tuỳ chọn) các `Text` UI.
- `Shooter` (đặt ở đáy màn hình):
  - `Muzzle` (để trống = chính nó), `Grid Manager`, `Cam` = Main Camera.
  - `Aim At Mouse` = bật để nhắm chuột; tắt = bắn thẳng lên.
  - Giữ **chuột trái** để bắn.

**Bucket (thùng)**
1. Tạo sprite hình thùng (hoặc square), add component **Bucket**.
2. Đặt **cạnh đường spline**, trong tầm `Attract Radius`.
3. Cấu hình `Color Id` + `Color` khớp với 1 màu trong `Palette` của PixelGridManager.
4. `Max Fill` = số dot cần để đầy (vd 5).
5. (Tuỳ chọn) gán `Mouth` = 1 child Transform làm điểm hút.
6. Đặt **nhiều bucket** tuỳ ý, cho phép trùng màu.
   > Bucket tự đăng ký với FallingPixelManager khi `OnEnable`.

**UI (tuỳ chọn)**
- Canvas + 3 `Text`: Score / Dots left / On belt → gán vào GameManager.

## 5. Luồng hoạt động

1. `PixelGridManager` tạo lưới dot màu (random hoặc từ texture).
2. `Shooter` bắn → `DamageDot` → `Dot.TakeDamage`. HP=0 → dot tách khỏi lưới.
3. `PixelGridManager` gọi `FallingPixelManager.AddDot` → dot rơi tự do, vào belt tại
   **vị trí ngang random** trong bề rộng băng chuyền.
4. Trên belt: dot đi dọc spline (progress t 0→1), bị **tách khỏi nhau** bằng spatial grid,
   **xoay nhẹ**, **nhiễu tốc độ**, và **clamp** lệch ngang trong bề rộng.
5. Khi đi ngang **Bucket** đúng màu trong `Attract Radius` → bị hút vào (MoveTowards),
   tăng Fill. Fill đầy → bucket biến mất.
6. Dot trôi hết spline (t≥1) mà chưa được hút → bị xoá (for-loop ngược, không leak).

## 6. Lưu ý tối ưu Spatial Grid cho 500 dot

- **Cell size = `dotSize * cellSizeMultiplier`** (mặc định 1.2). Cell ~ kích thước dot
  giúp mỗi ô chỉ chứa vài dot → kiểm tra láng giềng rất rẻ.
- Mỗi dot chỉ duyệt **9 ô (Moore 3×3)** quanh nó, không phải toàn bộ danh sách → O(n) thay vì O(n²).
- **`maxNeighbors`** (mặc định 8): cắt sớm khi đã đủ neighbor, tránh ô quá đông.
- Grid **dựng lại mỗi frame** nhưng **tái dùng List qua pool** (`_listPool`) → gần như **0 GC alloc**.
- Khoá ô là `long` gói (x,y) → tra cứu Dictionary nhanh, không cấp phát.
- Đẩy mạnh/êm hơn: chỉnh `separationStrength`. Nếu giật: giảm `beltSpeed` hoặc `speedJitter`.
- Muốn nhẹ CPU hơn với 500 dot: tăng `cellSizeMultiplier` nhẹ (1.2→1.4) hoặc giảm `maxNeighbors` (8→4).

## 7. Thông số chỉnh nhanh (Inspector)

| Script | Thông số chính |
|---|---|
| Dot | `maxHP`, `color`, `colorId` |
| PixelGridManager | `columns`, `rows`, `spacing`, `dotHP`, `palette`, `sourceTexture` |
| Shooter | `damage`, `fireRate`, `maxRange`, `hitRadius`, `aimAtMouse` |
| FallingPixelManager | `maxDots`, `dotSize`, `gravity`, `beltSpeed`, `speedJitter`, `cellSizeMultiplier`, `maxNeighbors`, `separationStrength`, `maxSpin`, `beltEntryY` |
| ConveyorSpline | `beltWidth` |
| Bucket | `colorId`, `maxFill`, `attractRadius`, `attractSpeed` |
| GameManager | `scorePerSorted`, `scorePerBucket` |

> Lưu ý: vì Unity chưa kết nối lúc tạo code, hãy mở Editor và xem **Console** để
> xác nhận compile sạch trước khi gán tham chiếu.
