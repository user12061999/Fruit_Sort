# Unity 2D Sprite Grid Fill

## Mục tiêu

Triển khai một material/shader cho `SpriteRenderer` trong Unity 2D. Sprite được chia thành
grid gồm `Columns` cột và `Rows` hàng. Khi tăng `Fill Amount`, sprite phải hiện dần theo
thứ tự:

1. Từ trái sang phải trong một hàng.
2. Bắt đầu từ hàng dưới cùng.
3. Sau khi đầy một hàng, tiếp tục sang hàng ngay phía trên.

Ví dụ grid `4 x 3` có thứ tự ô như sau:

```text
 8  9 10 11
 4  5  6  7
 0  1  2  3
```

## Thuộc tính

- `Columns`: số cột, số nguyên và tối thiểu là `1`.
- `Rows`: số hàng, số nguyên và tối thiểu là `1`.
- `Fill Amount`: giá trị chuẩn hóa từ `0` đến `1`.
- `Cell Gap`: khoảng trống trong suốt giữa các ô, từ `0` đến `0.45`. Giá trị `0` tắt
  đường phân cách.
- `Tint`: màu nhân với màu sprite.

## Quy tắc fill

```text
totalCells   = Columns * Rows
progress     = clamp(FillAmount, 0, 1) * totalCells
cellIndex    = row * Columns + column
cellProgress = clamp(progress - cellIndex, 0, 1)
```

- Ô có `cellProgress = 0` phải hoàn toàn ẩn.
- Ô có `cellProgress = 1` phải hiện hoàn toàn.
- Ô đang có `0 < cellProgress < 1` phải hiện dần từ mép trái sang mép phải.
- Phần chưa fill phải trong suốt, không chỉ đổi sang màu tối.
- Không được xuất hiện vạch một pixel tại các ô chưa fill khi `Fill Amount = 0`.

## Yêu cầu triển khai

1. Trước tiên kiểm tra project đang dùng Built-in Render Pipeline hay URP 2D và viết shader
   phù hợp với pipeline hiện tại.
2. Shader phải dùng được với `SpriteRenderer`, giữ alpha transparency và vertex color của
   sprite.
3. Shader mặc định là unlit. Nếu project đang dùng `Light2D` và material hiện tại là lit,
   hãy giữ khả năng nhận ánh sáng 2D.
4. Grid phải dựa trên tọa độ local chuẩn hóa của sprite, không dựa trực tiếp vào UV atlas.
   Hiệu ứng phải tiếp tục đúng khi sprite nằm trong Sprite Atlas hoặc là một sprite cắt từ
   spritesheet.
5. Có thể dùng một component C# nhỏ để truyền local bounds của `Sprite` vào shader.
6. Dùng `MaterialPropertyBlock` để nhiều `SpriteRenderer` dùng chung một material nhưng có
   `Fill Amount`, `Columns`, `Rows` và `Cell Gap` riêng.
7. Component phải cập nhật bounds khi sprite bị thay đổi ở runtime, kể cả khi sprite được
   thay bởi animation.
8. Không tạo một material instance mới trong mỗi frame.
9. Hỗ trợ chỉnh giá trị trong Inspector và bằng code, ví dụ:

```csharp
gridFill.FillAmount = currentValue / maxValue;
```

10. Giữ code đơn giản, không cần package bên thứ ba.

## Thiết lập mong muốn trong Unity

1. Tạo shader có tên `Custom/SpriteGridFill` hoặc tên tương đương phù hợp với project.
2. Tạo một Material sử dụng shader đó.
3. Gán Material cho `SpriteRenderer`.
4. Gắn component điều khiển grid fill lên cùng GameObject.
5. Cho phép chỉnh trực tiếp `Columns`, `Rows`, `Fill Amount` và `Cell Gap` trong Inspector.

## Tiêu chí nghiệm thu

- `Fill Amount = 0`: sprite hoàn toàn không nhìn thấy.
- `Fill Amount = 1`: toàn bộ sprite nhìn thấy, ngoại trừ khoảng trống do `Cell Gap` tạo ra.
- Với grid `4 x 3`, tăng fill phải đi theo thứ tự ô `0` đến `11` như sơ đồ trên.
- Ô hiện tại fill mượt từ trái sang phải, không bật hiện toàn bộ ngay lập tức.
- Thứ tự hàng luôn từ dưới lên trên.
- Sprite trong atlas không làm lệch grid hoặc hướng fill.
- Hai sprite dùng chung material vẫn có thể có hai mức fill khác nhau.
- Không có lỗi shader trong Console trên render pipeline và phiên bản Unity của project.
- Không phá sorting layer, order in layer, Sprite Mask hoặc màu của `SpriteRenderer` nếu các
  tính năng đó đang được project sử dụng.

## Kết quả cần trả về

- Các file shader và C# đã được thêm đúng thư mục trong project.
- Material mẫu nếu project có thư mục dành cho material hoặc sample assets.
- Hướng dẫn ngắn về cách gắn component và thay đổi `Fill Amount` bằng code.
- Xác nhận đã kiểm tra compile và chạy thử trong Unity Editor; nếu không thể chạy Editor,
  nêu rõ phần nào chưa được xác minh.
