FRUITSORT - CONVEYOR CONNECTION FIX 1.0.0
========================================

Mục đích
--------
- Trim phần cuối conveyor nguồn và phần đầu conveyor đích tại góc nối.
- Dựng centerline liên tục và mượn lead-in thật của conveyor đích.
- Nối thẳng không tạo round patch, tránh điểm nối bị phình.
- Hỗ trợ route đang active của ConveyorSwitch.

Cài đặt
-------
1. Import file .unitypackage.
2. Trong hộp thoại hiện ra, chọn "Cài đặt ngay".
   Có thể chạy lại tại:
   Tools > FruitSort > Conveyor Connection Fix > Install or Update
3. Công cụ tìm ConveyorBeltRenderer.cs và ConveyorSwitch.cs hiện có,
   sao lưu nội dung cũ rồi ghi đè nội dung mới. File .meta/GUID được giữ nguyên.
4. Chờ Unity compile xong và kiểm tra Console.
5. Trên component ConveyorBeltRenderer, mở context menu và chọn:
   Rebuild Belt Mesh

Thiết lập khởi điểm
-------------------
Render Connections           = true
Connection Segments          = 24
Connection Min Lead          = 0.9
Connection Width Factor      = 1.25
Connection Max Progress      = 0.35
Straight Connection Dot      = 0.995
Connection Snap Width Factor = 0.08

Lưu ý hình học
--------------
- Điểm cuối source (t=1) và điểm đầu target (t=0) nên trùng hoặc rất gần nhau.
- Tangent source phải đi vào junction; tangent target phải đi ra khỏi junction.
- Nếu endpoint lệch quá tolerance, renderer dùng cubic transition thay vì cung tròn.
- Không giữ thêm bản .cs khác có cùng class ConveyorBeltRenderer hoặc ConveyorSwitch.

Khôi phục
---------
Tools > FruitSort > Conveyor Connection Fix > Restore Latest Backup

Phạm vi package
---------------
Package chỉ thay nội dung ConveyorBeltRenderer.cs và ConveyorSwitch.cs.
ConveyorSpline.cs và ConveyorConnections.cs hiện có được giữ nguyên.
