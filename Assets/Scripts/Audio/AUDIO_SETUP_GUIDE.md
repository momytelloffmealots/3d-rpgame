# Hệ Thống Âm Thanh RPG - Hướng Dẫn Cài Đặt (Setup Guide)

Tài liệu này hướng dẫn cách cài đặt, sử dụng và mở rộng Generic Audio Interaction Framework (Hệ thống Tương tác Âm thanh Tổng quát).

## 1. Tổng quan (Overview)
Audio Framework này được thiết kế để tách biệt hoàn toàn logic của gameplay khỏi logic của âm thanh. Thay vì gắn cứng (hard-code) các audio clip vào trong script của nhân vật hay vũ khí, các script gameplay giờ đây chỉ cần gửi đi một `AudioInteractionContext` (ví dụ: "Có một bước chân vừa chạm đất ở tọa độ này"). Hệ thống âm thanh sẽ tự động nhận diện bề mặt, tìm profile âm thanh phù hợp, thêm các biến thể (random pitch/volume), quản lý số lượng âm thanh phát ra (voice management) và phát âm thanh đó.

## 2. Kiến Trúc (Architecture)
**Sự kiện Gameplay (Gameplay Event)** → **AudioInteractionContext** → **AudioInteractionService**
↳ Sử dụng `SurfaceDetector` để tìm `SurfaceType` (Loại bề mặt)
↳ Tra cứu `AudioInteractionDatabase` để lấy `AudioInteractionProfile`
↳ Trích xuất `AudioInteractionData` (clip, âm lượng, độ cao âm)
↳ Chọn clip thông qua `AudioVariationUtility`
↳ Kiểm tra giới hạn số lượng phát đồng thời qua `AudioVoiceManager`
↳ Lấy một `PooledAudioSource` đang rảnh từ `AudioSourcePool`
↳ Cấu hình `AudioMixerController` & `AudioOcclusion` (Cản âm)
↳ **Phát Âm Thanh!**

## 3. Cấu trúc thư mục (Folder Structure)
- `Assets/Scripts/Audio/Core/` : Các kiểu dữ liệu cốt lõi và Service chính.
- `Assets/Scripts/Audio/Data/` : Các ScriptableObject chứa audio clip và luật phát âm.
- `Assets/Scripts/Audio/Debug/` : Công cụ debug và test.
- `Assets/Scripts/Audio/Interaction/` : Các "Cầu nối" (Bridge) kết nối gameplay với hệ thống âm thanh.
- `Assets/Scripts/Audio/Mixing/` : Trình quản lý AudioMixer.
- `Assets/Scripts/Audio/Playback/` : Logic tạo biến thể (variation) và quản lý voice.
- `Assets/Scripts/Audio/Pooling/` : Hệ thống Object pool để tái sử dụng AudioSource.
- `Assets/Scripts/Audio/Spatial/` : Logic âm thanh 3D và cản âm (Occlusion).
- `Assets/Scripts/Audio/Surface/` : Logic nhận diện bề mặt.

## 4. Trách nhiệm của từng File (File Responsibilities)

| File | Trách nhiệm | Được gọi bởi | Sẽ gọi |
|------|----------------|-----------|-------|
| `AudioInteractionContext.cs` | Cấu trúc dữ liệu chứa thông tin cho một yêu cầu phát âm thanh | Gameplay, Bridges | - |
| `InteractionType.cs` | Enum danh sách mọi loại tương tác (Footstep, v.v.) | Core, Data | - |
| `SurfaceType.cs` | Enum danh sách mọi loại bề mặt (Grass, Metal, v.v.) | Core, Data | - |
| `AudioInteractionService.cs` | Quản lý trung tâm. Nhận Context và phát âm. | Gameplay | Database, Pool, VoiceMgr, Variation |
| `AudioInteractionData.cs` | Chứa clip và cấu hình cho 1 loại tương tác (Footstep). | Profile | - |
| `AudioInteractionProfile.cs` | ScriptableObject chứa toàn bộ data cho 1 bề mặt. | Database | AudioInteractionData |
| `AudioInteractionDatabase.cs`| ScriptableObject map Bề mặt (Surface) với Profile. | Service | AudioInteractionProfile |
| `SurfaceIdentifier.cs` | MonoBehaviour dùng để gắn tag bề mặt cho GameObject. | SurfaceDetector| - |
| `SurfaceDetector.cs` | Dùng Raycast/Collider để tìm ra SurfaceType. | Bridges, Gameplay| SurfaceIdentifier |
| `AudioVariationUtility.cs` | Chọn clip (Trộn, Không lặp lại), thay đổi pitch/vol. | Service | - |
| `AudioVoiceManager.cs` | Giới hạn số âm thanh phát cùng lúc, cướp voice (stealing). | Service | PooledAudioSource |
| `AudioSourcePool.cs` | Quản lý việc tái sử dụng AudioSource. | Service | PooledAudioSource |
| `PooledAudioSource.cs` | Wrapper cho AudioSource, tự động trả về Pool khi chạy xong. | Pool, Service | Pool |
| `AudioMixerController.cs` | Điều khiển âm lượng AudioMixer (từ linear sang dB). | Service, UI | Unity AudioMixer |
| `AudioOcclusion.cs` | Áp dụng bộ lọc Low-pass filter khi nguồn âm bị che khuất. | Service | - |
| `AudioDebugger.cs` | Log sự kiện âm thanh và hiển thị thông tin lên màn hình. | Service | - |
| `ThirdPersonAudioBridge.cs` | Kết nối Animation Event của Unity với Audio Service. | Animation | Service, Detector |
| `WeaponAudioBridge.cs` | Kết nối EasyWeapons với Audio Service. | EasyWeapons | Service, Detector |
| `AudioTestController.cs` | Dùng phím tắt để test hệ thống framework. | User Input | Service, Bridges |

## 5. Cài đặt ban đầu trong Unity (Initial Unity Setup)
1. **Tạo Audio Mixer:** Tạo một Unity Audio Mixer. Thêm các Group: `Master`, `Music`, `SFX`, `Ambience`, `Voice`, `UI`. Bấm chuột phải vào tham số Volume của chúng chọn Expose và đặt tên biến Expose CHÍNH XÁC là `MasterVolume`, `MusicVolume`, v.v.
2. **Tạo Database:** Click chuột phải trong cửa sổ Project → `Create` → `RPG Audio` → `Interaction Database`. Đặt tên là `AudioInteractionDatabase`.
3. **Tạo Default Profile:** Click chuột phải → `Create` → `RPG Audio` → `Interaction Profile`. Đặt tên là `DefaultProfile`. Kéo thả nó vào ô fallback (defaultProfile) trong Database vừa tạo.
4. **Tạo AudioManager:** Tạo một GameObject trống trong Scene, đặt tên là `AudioManager`. Thêm các component sau vào nó:
   - `AudioInteractionService`
   - `AudioSourcePool`
   - `AudioMixerController`
   - `AudioDebugger`
5. **Gắn các Reference:** Trong component `AudioInteractionService`, kéo thả Database, Pool, Mixer Controller và Debugger vào các ô trống tương ứng.
6. **Setup cho ThirdPersonController:** Trên GameObject `PlayerArmature` của nhân vật (nơi chứa component Animator), thêm component `ThirdPersonAudioBridge`. *XÓA* (Clear) các clip Footstep và Landing bị hard-code trong component `ThirdPersonController` cũ để tránh việc phát âm thanh 2 lần.

## 6. Cách tạo một Bề mặt mới (Ví dụ: Bãi Cỏ - Grass)
1. Click chuột phải → `Create` → `RPG Audio` → `Interaction Profile`. Đặt tên là `GrassProfile`.
2. Đổi Surface Type của nó thành `Grass`.
3. Thêm các phần tử vào mảng `Interactions` (ví dụ: `Footstep`, `Landing`).
4. Kéo thả các AudioClip âm thanh bước chân trên cỏ vào đó.
5. Mở `AudioInteractionDatabase` và thêm một entry mới: Nối `Grass` với `GrassProfile`.
6. Gắn component `SurfaceIdentifier` vào GameObject mặt đất (sàn cỏ) trong Scene và chọn loại là `Grass`. (Hoặc bạn có thể đặt Layer/Material của mặt đất tên là "Grass").

## 7. Cách tạo một loại Tương tác mới (Ví dụ: Thay đạn - Reload)
1. Mở file code `InteractionType.cs` và thêm enum mới (ví dụ: `Reload`).
2. Mở các Audio Profile của bạn (ví dụ: DefaultProfile), thêm một Interaction mới vào danh sách, chọn `Reload` từ menu thả xuống. Gán AudioClip thay đạn vào.
3. Trong code gameplay, mỗi khi nạp đạn chỉ cần gọi: 
   `AudioInteractionService.Instance.Play(AudioInteractionContext.Create(InteractionType.Reload, SurfaceType.Default, transform.position));`

## 8. Ví dụ: Luồng chạy của Tiếng bước chân (Footstep)
**Quy trình:** Người chơi chạy → Animation gọi event `OnFootstep` → `ThirdPersonAudioBridge` tóm được event này → Bắn một tia Raycast xuống đất để kiểm tra bề mặt (ví dụ: chạm trúng `Stone` - Đá) → Tạo ra một `AudioInteractionContext` kèm theo tốc độ/cường độ → Gửi Context cho `AudioInteractionService` → Service tìm `StoneProfile` → Lấy mảng clip `Footstep` → Phát âm thanh!

## 9. Ví dụ: Vũng nước (Water Puddle)
**Tình huống:** Một mặt đất lớn là Đất (`Dirt`), nhưng có một vùng trigger nhỏ là vũng nước nằm trên đó.
**Giải pháp:** Thêm component `SurfaceIdentifier` (chọn `Water`) vào vùng trigger của vũng nước. Khi `SurfaceDetector` bắn tia raycast xuống, nó chạm vũng nước đầu tiên. Nó đọc được `Water`, vì thế nó sẽ phát âm thanh tiếng bước chân đạp nước (Water) thay vì tiếng đất (Dirt).

## 10. Ví dụ: Đạn bắn trúng Kim loại
**Quy trình:** Viên đạn chạm tường → Cầu nối `WeaponAudioBridge.PlayBulletImpact(hitInfo)` được gọi → Nó đọc `PhysicMaterial` của bức tường (ví dụ tên là "Metal") → Context được tạo cho tương tác `BulletImpact` trên bề mặt `Metal` → Service phát âm thanh đạn nảy/va chạm trên kim loại.

## 11. Ví dụ: Tiếp đất (Landing & Intensity)
Tiếp đất sử dụng **Cường độ (Intensity)**. Khi người chơi rơi xuống, `ThirdPersonAudioBridge` sẽ tính toán tốc độ rơi (fall speed). Nếu chỉ là nhảy nhẹ, Cường độ là `0.2`. Nếu ngã từ rất cao, Cường độ là `1.0`. `AudioInteractionData` sử dụng `intensityCurve` (đường cong cường độ) để tự động vặn nhỏ âm lượng cho các cú chạm đất nhẹ và giữ âm lượng to cho các cú ngã mạnh.

## 12. Thêm một Bề mặt mới bằng Code
1. Mở `SurfaceType.cs`. Thêm loại của bạn (Ví dụ: `Carpet` - Thảm).
2. Nếu bạn muốn hệ thống tự nhận diện theo tên của Layer/Material, mở `SurfaceDetector.cs` và thêm `private static readonly string[] CarpetKeywords = { "carpet", "rug" };`, sau đó map nó bên trong hàm `FromName()`.

## 13. Thêm một Tương tác mới bằng Code
1. Mở `InteractionType.cs` và thêm loại tương tác của bạn.
2. Trong `AudioVoiceManager.cs`, nếu tương tác này cần giới hạn số lượng phát đồng thời (để tránh điếc tai), hãy thêm nó vào Dictionary `_maxConcurrency`.

## 14. Mở rộng Hệ thống (Extending the System)
- **Kiểu Random mới:** Sửa `AudioVariationUtility.cs` (ví dụ: thêm Weighted Random - random theo tỉ lệ phần trăm).
- **Cơ chế Cản âm mới:** Sửa `AudioOcclusion.cs` (ví dụ: thêm bắn nhiều tia raycast để tính toán độ lọt âm qua cửa sổ/cổng không gian - portal).

## 15. Gỡ lỗi (Debugging)
- Bật `On-Screen Overlay` trong component `AudioDebugger`. Bạn sẽ thấy list âm thanh đang chạy trực tiếp trên màn hình game.
- Nếu một âm thanh không phát ra, hãy xem Console. Debugger sẽ báo lỗi màu vàng (Warning) nếu bạn quên gán profile hoặc thiếu clip.
- Ấn các phím từ `1` đến `0` trên bàn phím khi chạy `AudioTestScene` (có gắn `AudioTestController`) để test thử mọi âm thanh mà không cần phải chơi game.

## 16. Tối ưu Hiệu suất (Performance)
- **Pooling (Tái sử dụng):** `AudioSourcePool` tái sử dụng lại các AudioSource cũ. Sẽ KHÔNG BAO GIỜ có hàm `Instantiate` (tạo object mới) được gọi khi đang combat, giúp game không bị giật lag.
- **Concurrency (Đồng thời):** `AudioVoiceManager` ngăn chặn việc 100 vụ nổ phát âm thanh cùng một lúc. Nó sẽ ưu tiên cướp (steal) các âm thanh cũ hoặc quan trọng thấp hơn.
- **Culling:** Hãy thiết lập `spatialMaxDistance`. Unity sẽ tự động tắt (cull) các âm thanh ở khoảng cách quá xa.
