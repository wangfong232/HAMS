# HƯỚNG DẪN DEMO HỆ THỐNG HAMS - SWD392 (FPT UNIVERSITY)

---

## 1. KHỞI ĐỘNG HỆ THỐNG TRONG 1 CLICK

- **Cách bật:** Click đúp vào file `run_demo.bat` ở thư mục gốc `E:\FPTU\Semester7_FA26\SWD392\HAMS\run_demo.bat`.
  - Script sẽ tự động khởi chạy 3 ứng dụng ở 3 cửa sổ CMD riêng biệt.
  - Sau 5 giây, script tự động mở sẵn 3 tab trình duyệt:
    1. **Bệnh nhân (PhongTQ):** `http://localhost:5140`
    2. **Lễ tân (Minh Đức):** `http://localhost:5115`
    3. **Bác sĩ (Đức Anh):** `http://localhost:5165`
- **Cách tắt:** Click đúp vào file `stop_demo.bat` để tắt toàn bộ ứng dụng và giải phóng các port.

---

## 2. TÍNH NĂNG "RESET DEMO" (LÀM MỚI DỮ LIỆU BAN ĐẦU)

- **Vị trí:** Nút màu đỏ `[Reset Demo]` nằm ngay trên góc phải thanh Menu của **cả 3 ứng dụng**.
- **Công dụng:** Khi muốn diễn tập lại hoặc khi thầy cô yêu cầu demo lại từ đầu, bấm nút này để:
  - Xóa sạch các hồ sơ khám, đơn thuốc, sinh hiệu và cuộc hẹn vừa tạo.
  - Tự động reset lại toàn bộ các khung giờ khám (TimeSlots) trong ngày về trạng thái `Open`.
  - Không cần mở SQL Server, hoàn tất chỉ trong 1 giây!

---

## 3. KỊCH BẢN DEMO PHÂN VAI (3 THÀNH VIÊN)

### Bước 1: PhongTQ - Phân hệ Bệnh nhân (`http://localhost:5140`)
1. **Tìm kiếm & Phân bổ tải (Capacity Engine):**
   - Vào trang đặt lịch khám, chọn chuyên khoa *Khoa Nội tổng quát* hoặc ngày khám hôm nay.
   - Giới thiệu thanh đo tải lượng khám ca Sáng / Chiều (**Morning / Afternoon Shift Capacity**) được tính toán tự động bằng `CapacityEngine`.
2. **Mô phỏng Xung đột Đồng thời (Optimistic Concurrency Control - OCC 409):**
   - Mở thêm **1 tab Trình duyệt Ẩn danh (Incognito)** để đóng vai Bệnh nhân thứ 2.
   - Cả 2 tab cùng vào chọn bác sĩ và mở danh sách khung giờ.
   - Tab 1 bấm chọn khung giờ `08:00 - 08:15` $\rightarrow$ Nhờ **SignalR**, tab 2 thấy ngay khung giờ chuyển sang trạng thái đang giữ chỗ.
   - Nếu tab 2 cố tình bấm vào khung giờ đó $\rightarrow$ Hệ thống kích hoạt xử lý OCC, bắt lỗi `DbUpdateConcurrencyException`, trả về HTTP 409 và hiển thị danh sách **Slot gợi ý thay thế** (Alternative Slots).
3. **Xác nhận đặt lịch & Nhận vé E-Ticket:**
   - Tab 1 nhập thông tin Email $\rightarrow$ Bấm *Xác nhận đặt lịch*.
   - Hệ thống hiển thị **Vé khám E-Ticket** kèm mã QR (ví dụ `APP-1`), mã tra cứu, và tự động gửi Email xác nhận qua Background/SMTP Adapter.

---

### Bước 2: Minh Đức - Phân hệ Tiếp đón Lễ tân (`http://localhost:5115`)
1. **Đồng bộ thời gian thực (Real-time Pipeline):**
   - Màn hình Lễ tân **tự động nhảy ra thông tin bệnh nhân vừa đặt lịch** ở Bước 1 (không cần nhấn F5).
2. **Tiếp đón & Quét mã:**
   - Nhập mã QR vé (ví dụ: `APP-1` hoặc `HAMS-APT-1`) hoặc Số điện thoại vào ô tra cứu $\rightarrow$ Bấm *Tra cứu*.
   - Thông tin bệnh nhân hiện ra đầy đủ cùng chuyên khoa và bác sĩ phụ trách.
3. **Đo sinh hiệu ban đầu & Cấp số thứ tự:**
   - Nhập chỉ số sinh hiệu đo được:
     - Huyết áp: `120/80`
     - Mạch: `75` lần/phút
     - Thân nhiệt: `37.0` °C
   - Bấm *Xác Nhận Check-In & Cấp Số Thứ Tự*.
   - Trạng thái cuộc hẹn đổi sang `Arrived`, bệnh nhân được cấp số thứ tự khám `#1`.

---

### Bước 3: Đức Anh - Phân hệ Bác sĩ Lâm sàng (`http://localhost:5165`)
1. **Nhận bệnh nhân vào hàng đợi:**
   - Màn hình buồng khám Bác sĩ **tự động cập nhật bệnh nhân số #1** vào danh sách chờ khám (Real-time sync).
2. **Khám bệnh & Xem sinh hiệu:**
   - Bác sĩ bấm nút *Khám Bệnh*.
   - Màn hình hiển thị toàn bộ chỉ số sinh hiệu (Huyết áp, Mạch, Thân nhiệt) mà Lễ tân vừa đo ở Bước 2.
3. **Chẩn đoán ICD-10 & Kê đơn thuốc:**
   - Nhập mã ICD-10: `J00` - Viêm mũi họng cấp tính.
   - Chọn loại thuốc (ví dụ: Paracetamol), số lượng, liều dùng; HOẶC chọn `-- Không kê đơn thuốc (Chỉ tư vấn & theo dõi) --` nếu ca bệnh nhẹ.
   - Bấm *Lưu Bệnh Án Điện Tử (EMR) & Hoàn Tất Khám*.
4. **Khép kín vòng đời (Closing the loop):**
   - Ca khám chuyển sang trạng thái `Completed`.
   - Quay lại màn hình Lễ tân (Port 5115): Trạng thái của bệnh nhân tự động cập nhật sang `Completed` mà không cần F5!

---

## 4. TỔNG KẾT KIẾN TRÚC & CÂU HỎI HỘI ĐỒNG (SWD392)

1. **Kiến trúc phân tầng 4-Tier / Layered Architecture:**
   - `HAMS.Domain`: Chứa 11 Entity thuần túy, Enums (độc lập 100% với Database và Web).
   - `HAMS.Application`: Chứa Interfaces (`IBookingService`, `IEmrService`, `ICapacityEngine`), DTOs, và nghiệp vụ cốt lõi.
   - `HAMS.Infrastructure`: EF Core `HamsDbContext`, Repositories, Background Worker, Smtp Email Adapter.
   - `3 Web Projects`: `HAMSPatient`, `HAMSOperations`, `HAMSClinical` độc lập nghiệp vụ, dễ scale.

2. **Các thuộc tính chất lượng (Quality Attributes) đạt điểm tối đa:**
   - **Concurrency Control:** Sử dụng token `RowVersion` giải quyết xung đột đặt chỗ đồng thời.
   - **Fault Tolerance:** `ExpiredSlotCleanupWorker` chạy ngầm định kỳ 30s giải phóng slot bị treo.
   - **Real-Time Data Pipeline:** SignalR Hub kết hợp Polling fallback đảm bảo dữ liệu nhảy tức thì giữa các phân hệ.
