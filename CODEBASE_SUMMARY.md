# TỔNG QUAN CODEBASE DỰ ÁN 3D-RPGAME (UNITY)

> **Ngày cập nhật:** 11/10/2026  
> **Phiên bản Unity:** Unity 6 (`6000.5.9f1`)  
> **Render Pipeline:** Universal Render Pipeline (URP 17.5.0)  
> **Input System:** Unity New Input System (`com.unity.inputsystem 1.20.0`)  
> **Audio Middleware:** FMOD Studio Unity Integration  
> **MCP Tooling:** FastMCP Server (`com.coplaydev.unity-mcp` v10.3.0)  

---

## 1. Mục lục
1. [Cấu trúc thư mục dự án](#2-cấu-trúc-thư-mục-dự-án)
2. [Các hệ thống cốt lõi (Core Systems)](#3-các-hệ-thống-cốt-lõi-core-systems)
   - [Hệ thống điều khiển nhân vật (Starter Assets)](#31-hệ-thống-điều-khiển-nhân-vật-starter-assets)
   - [Hệ thống vũ khí & chiến đấu (Easy Weapons)](#32-hệ-thống-vũ-khí--chiến-đấu-easy-weapons)
   - [Hệ thống âm thanh tương tác (Audio & FMOD)](#33-hệ-thống-âm-thanh-tương-tác-audio--fmod)
   - [Hệ thống chu kỳ ngày/đêm & ánh sáng (Lighting Manager)](#34-hệ-thống-chu-kỳ-ngàyđêm--ánh-sáng-lighting-manager)
   - [Tài nguyên môi trường công nghiệp (Industrial Assets)](#35-tài-nguyên-môi-trường-công-nghiệp-industrial-assets)
3. [Tích hợp MCP (Model Context Protocol)](#4-tích-hợp-mcp-model-context-protocol)
4. [Các Scene trong dự án](#5-các-scene-trong-dự-án)
5. [Đánh giá hiện trạng & Khuyến nghị kỹ thuật](#6-đánh-giá-hiện-trạng--khuyến-nghị-kỹ-thuật)

---

## 2. Cấu trúc thư mục dự án

```text
3d-rpgame/
├── Assets/
│   ├── AUDIO SOURCE/               # Thư viện nhạc nền BGM (.mp3)
│   ├── Easy Weapons/               # Bộ công cụ súng đạn & chiến đấu (Raycast, Projectile, Beam)
│   │   ├── Scripts/                # Code logic vũ khí, máu, nổ, pool đạn
│   │   └── Textures/Prefabs/       # Crosshair, bullet hole, particle effects
│   ├── LightingManager_Assets/     # Quản lý chu kỳ ngày đêm (Day-Night Cycle, LightingPresets)
│   ├── Plugins/
│   │   └── FMOD/                   # FMOD Unity Integration plugin & bridge scripts
│   ├── RPG_FPS_game_assets_industrial/ # Modular props (thùng phuy, container, nhà xưởng, đường xá)
│   ├── Scenes/
│   │   ├── main.unity              # Scene gameplay chính đang phát triển
│   │   └── script/
│   │       └── SurfaceInteractionAudio.cs # Script phát âm thanh tương tác theo bề mặt vật liệu
│   ├── Settings/                   # Thiết lập cấu hình URP (Forward Renderer, Quality Presets)
│   ├── SPRITE/                     # Decal & splat models (.fbx, .obj, .mat)
│   ├── Standard Assets/            # Âm thanh cơ bản (Jump.wav, Land.wav)
│   └── Starter Assets/             # Unity Starter Assets (FirstPerson & ThirdPerson Controller)
├── Packages/
│   └── manifest.json               # Package dependencies (Cinemachine 3.x, URP 17.x, Input System, v.v.)
└── ProjectSettings/                # Cấu hình Layer, Tag, Input, Graphics, URP của Unity
```

---

## 3. Các hệ thống cốt lõi (Core Systems)

### 3.1. Hệ thống điều khiển nhân vật (Starter Assets)
* **Vị trí code:** `Assets/Starter Assets/Runtime/`
* **Các script chính:**
  - `ThirdPersonController.cs`: Điều khiển góc nhìn người thứ ba (di chuyển, chạy sprint, nhảy, trọng lực tùy chỉnh, tiếp đất grounded check).
  - `FirstPersonController.cs`: Điều khiển góc nhìn người thứ nhất (camera pitch, yaw, di chuyển).
  - `StarterAssetsInputs.cs`: Cầu nối trung gian tiếp nhận dữ liệu từ **New Input System** (`PlayerInput`) và phân phối các biến cờ: `move`, `look`, `jump`, `sprint`.
  - `BasicRigidBodyPush.cs`: Xử lý tương tác vật lý khi nhân vật va chạm và đẩy các vật thể `Rigidbody`.
  - `TouchscreenInput.cs` & `VirtualInput.cs`: Hỗ trợ giao diện điều khiển cảm ứng cho nền tảng mobile.

### 3.2. Hệ thống vũ khí & chiến đấu (Easy Weapons)
* **Vị trí code:** `Assets/Easy Weapons/Scripts/`
* **Các script chính:**
  - `WeaponSystem.cs`: Quản lý danh sách vũ khí trên tay người chơi, hỗ trợ chuyển đổi súng bằng phím số (1-9) hoặc cuộn chuột (`Mouse ScrollWheel`).
  - `Weapon.cs`: Lớp điều khiển cơ chế bắn vũ khí với 3 dạng chính:
    1. **Raycast**: Bắn tia trúng đích tức thì, tính độ tản đạn (spread), tạo vệt đạn và hiệu ứng va đập.
    2. **Projectile**: Sinh đầu đạn vật lý/tên lửa bay theo quỹ đạo (`Projectile.cs`).
    3. **Beam**: Chiếu tia laser liên tục qua LineRenderer kèm cơ chế tích nhiệt (heat/overheat).
    - Hỗ trợ nạp đạn (reload), ngắm bắn (iron sights), recoil (độ giật), chế độ bắn đơn/liên thanh (Semi/Full-Auto).
  - `BulletHolePool.cs` & `BulletHole.cs`: Object pooling tạo vết đạn trên bề mặt theo Tag/Material/PhysicMaterial để tối ưu hiệu năng.
  - `Health.cs`: Quản lý lượng máu của Entity/Player, kích hoạt sự kiện tử trận (`Die`), sinh hiệu ứng xác hoặc nổ (`Explosion.cs`).
  - `SlowMotion.cs`: Hiệu ứng thời gian chậm (bullet time) qua điều chỉnh `Time.timeScale`.

### 3.3. Hệ thống âm thanh tương tác (Audio & FMOD)
* **Vị trí code:** `Assets/Scenes/script/SurfaceInteractionAudio.cs` & `Assets/Plugins/FMOD/`
* **Chi tiết kỹ thuật:**
  - `SurfaceInteractionAudio.cs`:
    - Bắn Raycast hướng xuống dưới đất (`Vector3.down`) để xác định Layer của bề mặt (`Grass`, `Stone`, `Wood`, `Metal`, `Water`, `Sand`, `Snow`).
    - Ngẫu nhiên pitch và chọn AudioClip tương ứng để phát qua `AudioSource.PlayOneShot()`, giảm sự lặp âm đơn điệu.
  - **FMOD Integration:**
    - Dự án tích hợp thư viện FMOD Studio (`FMODStudioSettings.asset`, `RuntimeManager.cs`, `StudioEventEmitter.cs`).
    - Sẵn sàng mở rộng để chuyển đổi âm thanh bước chân và hiệu ứng súng đạn từ AudioSource Unity thông thường sang các Event FMOD chuyên nghiệp.
  - **BGM/Music:** Đã tích hợp bộ soundtrack chất lượng cao tại `Assets/AUDIO SOURCE/tf/`.

### 3.4. Hệ thống chu kỳ ngày/đêm & ánh sáng (Lighting Manager)
* **Vị trí code:** `Assets/LightingManager_Assets/`
* **Các script chính:**
  - `LightingManager.cs` (`[ExecuteAlways]`):
    - Tự động xoay Sun Directional Light và Moon theo thời gian trong ngày (`TimeOfDay` từ 0h - 24h).
    - Đồng bộ màu sắc ánh sáng môi trường (Ambient Color), màu mặt trời, sương mù (Fog Color) thông qua ScriptableObject `LightingPreset.cs`.
    - Hỗ trợ đổi Skybox và kích hoạt chuỗi sự kiện `EventInfo.cs` tại các khung giờ cụ thể.

### 3.5. Tài nguyên môi trường công nghiệp (Industrial Assets)
* **Vị trí:** `Assets/RPG_FPS_game_assets_industrial/`
* **Nội dung:**
  - Tập hợp prefab modular: Thùng gỗ, thùng phuy (`Barrel`), container hàng hải (`Cargo_container`), hàng rào bê tông, ống dẫn công nghiệp, quạt thông gió, máy phát điện.
  - Map mẫu demo: `Map_v1.unity`, `Map_v2.unity`, `Assets_showcase_scene.unity` kèm dữ liệu nướng ánh sáng (`LightingData.asset`).

---

## 4. Tích hợp MCP (Model Context Protocol)

* **Gói cài đặt:** `com.coplaydev.unity-mcp` (nguồn GitHub: `CoplayDev/unity-mcp`).
* **Trạng thái kết nối:**
  - MCP Daemon: `FastMCP 3.4.8` chạy qua Python/uv tại port `8080` (`http://127.0.0.1:8080/mcp`).
  - WebSocket Hub: Đã kết nối với Unity Editor phiên bản 6000.5.9f1.
  - Công cụ hỗ trợ: 36 native tools hỗ trợ can thiệp GameObjects, Components, Scripts, Profiling, Testing, VFX và Scene trực tiếp từ xa.

---

## 5. Các Scene trong dự án

| Scene | Đường dẫn | Mục đích |
|---|---|---|
| **main** | `Assets/Scenes/main.unity` | Scene gameplay chính tích hợp nhân vật, môi trường và âm thanh. |
| **Map_v1** | `Assets/RPG_FPS_game_assets_industrial/Map_v1.unity` | Bản đồ môi trường nhà xưởng công nghiệp phiên bản 1. |
| **Map_v2** | `Assets/RPG_FPS_game_assets_industrial/Map_v2.unity` | Bản đồ môi trường nhà xưởng công nghiệp phiên bản 2. |
| **first_person** | `Assets/Easy Weapons/Demo/first_person.unity` | Demo mẫu góc nhìn FPS bắn súng của Easy Weapons. |
| **newdemo** | `Assets/Easy Weapons/Demo/newdemo.unity` | Demo showcase tổng hợp kho vũ khí và tính năng súng đạn. |
| **ExampleScene** | `Assets/LightingManager_Assets/ExampleScene/ExampleScene.unity` | Demo chu kỳ ngày đêm và ánh sáng động. |
| **Playground** | `Assets/Starter Assets/Sample/.../Playground.unity` | Sân tập di chuyển mẫu góc nhìn FPS & TPS. |

---

## 6. Đánh giá hiện trạng & Khuyến nghị kỹ thuật

1. **Sự xung đột giữa Input System cũ và mới:**
   - `Starter Assets` sử dụng **Unity New Input System** (`UnityEngine.InputSystem`).
   - `Easy Weapons` (`WeaponSystem.cs`, `Weapon.cs`) sử dụng **Legacy Input Manager** (`Input.GetButtonDown`, `Input.GetAxis`).
   - *Khuyến nghị:* Cần cấu hình ProjectSettings Input Handling là `Both` hoặc refactor `WeaponSystem` đọc input từ `StarterAssetsInputs` để gameplay đồng bộ và chạy mượt trên cả tay cầm/bàn phím.

2. **Hợp nhất hệ thống âm thanh:**
   - Hiện `SurfaceInteractionAudio.cs` đang dùng `AudioSource` thuần kiểm tra bằng Layer.
   - Dự án đã cài đặt sẵn FMOD Studio. Trong tương lai nên tạo FMOD Parameter (ví dụ: `Surface_Type = 0..7`) gắn vào Event `event:/Player/Footsteps` để tận dụng tối đa FMOD Mixer và âm thanh không gian 3D.
