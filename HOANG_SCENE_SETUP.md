# HƯỚNG DẪN CÀI ĐẶT & TRẢI NGHIỆM: HOANG SCENE (UNITY 6)

Tài liệu này hướng dẫn chi tiết cách cài đặt, mở scene và thử nghiệm toàn bộ hệ thống Gameplay & Sát thương trong **Hoang Scene**.

---

## 1. Các file đóng gói để gửi cho người khác

Trong thư mục gốc của dự án đã được tạo sẵn file đóng gói:
* **`Hoang_Scene_Setup.unitypackage`** (Dung lượng: ~245 MB)
  * Chứa toàn bộ Scene `Hoang Scene.unity`.
  * Toàn bộ mã nguồn hệ thống máu: `PlayerHealth`, `PlayerFallDamage`, `ExplosiveBarrel`, `ShootingTurret`, `PlayerHUD`, `DamageSource`.
  * Các asset môi trường nhà xưởng (Map_v2), bộ vũ khí Easy Weapons, nhân vật Regular_Character, hiệu ứng cháy nổ, âm thanh.

---

## 2. Yêu cầu môi trường (System Requirements)

Để mở và chạy mượt mà trong Unity Editor:
1. **Phiên bản Unity:** Unity 6 (`6000.0` trở lên, khuyến nghị `6000.5.9f1`).
2. **Render Pipeline:** Universal Render Pipeline (URP).
3. **Input System:** Unity New Input System (kết hợp Active Input Handling: `Both`).
4. **Audio Middleware:** FMOD Studio Unity Integration (đã tích hợp trong package).

---

## 3. Các bước thiết lập cho người nhận (Setup Steps)

### Cách 1: Import vào Unity Project mới hoặc có sẵn
1. Mở Unity Hub, tạo hoặc mở một dự án Unity 6 URP.
2. Trong menu Unity, chọn: **Assets -> Import Package -> Custom Package...**
3. Trỏ tới file **`Hoang_Scene_Setup.unitypackage`** và nhấn **Import All**.
4. Vào thư mục `Assets/Scenes/`, nhấp đúp mở scene: **`Hoang Scene.unity`**.
5. Nhấn nút **Play ▶** trên thanh công cụ để trải nghiệm!

---

## 4. Các tính năng & Cơ chế trong Hoang Scene

### 4.1. Hệ thống Máu & Tử trận (`PlayerHealth.cs`)
* Thanh máu HUD trực quan ở góc dưới bên trái màn hình.
* Hiệu ứng màn hình chớp đỏ rát (Damage Flash) mỗi khi dính đòn.
* Cảnh báo viền đỏ nhấp nháy khi máu xuống dưới 25%.
* Khi hết máu (0 HP): nhân vật tử trận, camera gục ngã xuống sàn, mở khóa chuột và hiện thông báo **BẠN ĐÃ TỬ TRẬN**.
* Nhấn **[R]** để hồi sinh ngay lập tức tại điểm xuất phát.

### 4.2. Placeholder Âm thanh FMOD (`Hurt Sound` & `Die Sound`)
Trên GameObject **`Regular_Character`** (component `Player Health`):
* Ô **`Hurt Sound FMOD`**: Kéo FMOD Event khi dính đòn vào đây (Ví dụ: `event:/Character/Player Hurt`).
* Ô **`Die Sound FMOD`**: Kéo FMOD Event khi chết vào đây (Ví dụ: `event:/Character/Player Die`).
* *Lưu ý:* Nếu chưa gán Event, hệ thống sẽ phát âm thanh dự phòng Unity (nếu có) hoặc thông báo rõ ràng trong Console mà không gây lỗi game.

### 4.3. Các cơ chế gây sát thương trong màn chơi
1. **Sát thương rơi từ trên cao (Fall Damage):**
   * Rơi từ độ cao an toàn (< 3.5m - ví dụ nhảy thông thường) sẽ không mất máu.
   * Rơi từ độ cao > 3.5m sẽ nhận 12 sát thương/mét vượt ngưỡng.
   * Rơi từ độ cao tử thần (> 14m) sẽ chết ngay lập tức.
   * *Khu vực test:* Leo lên bục nhảy **`Fall_Damage_Test_Tower`** (có 3 mức: 2m an toàn, 5m vừa, 8m nặng).
2. **Sát thương từ bom / thùng nổ (Explosion Damage):**
   * Có 2 thùng bom đỏ **`Explosive_Barrel_01`** và **`02`** đặt cạnh người chơi.
   * Có thể bắn súng vào thùng để kích nổ hoặc đứng gần bấm **[E]** để kích nổ thử.
   * Sát thương và lực văng giảm dần theo khoảng cách từ tâm vụ nổ.
3. **Sát thương từ đạn súng (Bullet Damage):**
   * Trụ súng **`Shooting_Turret_Test`** tự động quay và bắn đạn vào người chơi (gây 15 HP/viên, có vệt đạn tracer vàng và tia lửa).
   * Nhấn phím **[T]** để bật/tắt trụ súng.

---

## 5. Danh sách phím tắt điều khiển & thử nghiệm

| Phím | Chức năng |
|---|---|
| **W, A, S, D** | Di chuyển nhân vật |
| **Chuột** | Xoay góc nhìn / Nhắm bắn |
| **Chuột trái** | Bắn vũ khí |
| **1 - 8 / Cuộn chuột** | Đổi vũ khí |
| **Space** | Nhảy |
| **[H]** | Tự trừ 15 HP (Test Hurt Sound & Chớp đỏ) |
| **[J]** | Hồi 25 HP |
| **[K]** | Tự sát (Test Die Sound & Tử trận) |
| **[R]** | Hồi sinh nhân vật sau khi chết |
| **[E]** | Kích nổ thùng bom đỏ khi đứng gần |
| **[T]** | Bật / Tắt trụ súng tự động |
| **[F1]** | Ẩn / Hiện bảng hướng dẫn phím tắt |
