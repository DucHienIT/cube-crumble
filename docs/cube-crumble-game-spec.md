# Cube Crumble — Game Design & Technical Specification

> Tài liệu này mô tả đầy đủ luật chơi, cơ chế, giao diện và kiến trúc kỹ thuật của game **Cube Crumble** (iOS App Store), dùng làm brief để **Claude Code CLI** clone lại game bằng web (HTML5/Canvas hoặc React) hoặc engine tùy chọn.

---

## 1. Tổng quan game

- **Thể loại:** Puzzle tap / color-sorting, chơi theo lượt, có giới hạn thời gian.
- **Nền tảng gốc:** iOS (mobile portrait).
- **Vòng lặp cốt lõi (core loop):** Chạm vào một khối lập phương (cube) trong một khối hình đa-cube ở trên màn hình → khối đó vỡ vụn thành các viên bi màu → bi rơi xuống các "hộp chứa" (container) ở dưới → hộp chứa đúng màu và đủ số lượng thì hoàn thành và được thay bằng hộp mới → dọn sạch toàn bộ hình khối trước khi hết giờ hoặc hết chỗ chứa.
- **Mục tiêu thắng:** Crumble (làm vỡ) toàn bộ khối hình ở đầu màn trước khi:
  - Hết thời gian (đồng hồ đếm ngược), hoặc
  - "Shared slot" (khay chứa tạm) bị đầy (thua ngay lập tức).
- **Câu cửa miệng mô tả trên App Store:**
  "Tap a cube to crumble it into a set of balls, then watch them fall into the containers below. Each container accepts matching colors, and when a container is filled, a new one comes forward from the back row. If you tap a cube whose color does not match any available container, the extra balls move into the shared slot. The shared slot has limited space, and if it fills up, the level fails."

---

## 2. Thành phần màn hình (từ ảnh chụp gameplay)

Bố cục dọc (từ trên xuống dưới):

1. **Thanh trên cùng (HUD)**
   - Nút **Pause** (góc trái, icon ⏸)
   - **Đồng hồ đếm ngược** dạng viên thuốc màu đen, icon đồng hồ vàng + thời gian còn lại (mm:ss), ví dụ `01:58`
   - **Level hiện tại** (góc phải), ví dụ `Lvl 8`

2. **Khối hình đa-cube (Cube Shape)**
   - Một khối 3D isometric ghép từ nhiều cube đơn vị, mỗi cube có 1 màu (đỏ, tím, cam, vàng...).
   - Hình dạng tổng thể là một **polycube ngẫu nhiên/được thiết kế theo level**, viền xanh dương đậm bao quanh toàn bộ khối.
   - Khi chạm (tap) vào một mặt cube: cube đó "nổ" thành các viên bi nhỏ cùng màu, hiệu ứng hạt vỡ (mảnh vụn) bắn ra, bi rơi xuống theo trọng lực (có vật lý nảy nhẹ).
   - Sau khi 1 cube bị crumble, cube ở "lớp dưới/lớp sau" có thể lộ ra và trở thành cube có thể tap tiếp theo (giống thứ tự lớp trong isometric stacking).

3. **Chỉ số tiến trình:** `10/50` — số bi đã được xử lý / tổng số bi mục tiêu để hoàn thành màn (hoặc tổng số bi sẽ được sinh ra từ toàn bộ khối).

4. **Shared Slot (khay dùng chung / khay tạm)**
   - Là một cái phễu/khay hình thang màu trắng nằm ngay dưới khối cube, có viền xanh dương.
   - Đây là nơi **hứng tạm các viên bi mà không có container nào đang nhận màu đó**.
   - Có sức chứa giới hạn (hiển thị số lượng bi hiện có trong khay, dạng chồng bi lên nhau).
   - **Nếu khay này đầy → thua màn ngay lập tức.**
   - Bi trong shared slot có thể tự động "chuyển" sang container tương ứng nếu sau đó có container trống nhận đúng màu (cơ chế tùy chọn nâng cao — xem mục 4.4).

5. **Hàng Container (hộp chứa) — 4 slot hiển thị cùng lúc**
   - Mỗi container là một hộp màu (khớp với màu bi cần chứa), có sức chứa cố định — trong ảnh mỗi hộp hiển thị **3 chấm tròn (○○○)** = cần 3 viên bi cùng màu để lấp đầy.
   - Màu container ví dụ trong ảnh: tím, tím (đang được lấp đầy — hiệu ứng phát sáng), tím, cam/nâu.
   - Khi 1 container được lấp đầy đủ số bi yêu cầu → container biến mất/nổ hiệu ứng, và **container tiếp theo trong hàng đợi (back row/queue)** trượt lên thay vào vị trí đó.
   - Có một **hàng đợi container** phía sau không hiển thị hết (giống queue trong game "sort puzzle"), được sinh ra theo thứ tự định trước cho từng level.

---

## 3. Luật chơi chi tiết

### 3.1 Vòng lặp chính
1. Người chơi tap vào 1 cube khả dụng (thường là cube ở lớp trên cùng/ngoài cùng của khối isometric).
2. Cube vỡ ra thành N viên bi cùng màu (N tùy theo kích thước/loại cube, ví dụ 3-6 bi).
3. Từng viên bi "rơi" (animation) xuống khu vực container:
   - Nếu có **container đang mở và cùng màu với bi** → bi bay vào container đó, tăng dần chỉ số lấp đầy.
   - Nếu **không có container nào cùng màu** đang active → bi rơi vào **Shared Slot**.
4. Khi 1 container đầy (đủ số bi yêu cầu) → container "hoàn thành", biến mất, container kế tiếp trong queue được đẩy lên thay thế vị trí đó.
5. Cập nhật bộ đếm tiến trình `X/50`.
6. Lặp lại cho đến khi:
   - **Thắng:** toàn bộ khối cube bị crumble hết (không còn cube nào) VÀ/HOẶC đạt đủ số bi mục tiêu trước khi hết giờ.
   - **Thua:** Shared Slot đầy, HOẶC hết thời gian mà chưa hoàn thành khối.

### 3.2 Điều kiện thua
- Shared Slot đạt sức chứa tối đa (ví dụ 10-12 bi) trong khi vẫn còn bi/cube cần xử lý.
- Đồng hồ đếm ngược về 0 trước khi crumble hết khối.

### 3.3 Chiến thuật cốt lõi
- Người chơi phải **dự đoán thứ tự các container sẽ xuất hiện** (vì container queue có thể xem trước hoặc một phần) để chọn tap đúng cube màu tương ứng, tránh đẩy bi dư vào Shared Slot.
- Việc chọn cube nào để tap trước/sau ảnh hưởng đến việc bi có "khớp" được container đang mở hay không → đây là yếu tố puzzle chính.

### 3.4 Độ khó tăng dần theo level
- Số lượng cube trong khối tăng.
- Số màu khác nhau tăng (nhiều màu hơn => khó dự đoán match hơn).
- Thời gian giới hạn giảm dần hoặc số bi mục tiêu tăng.
- Sức chứa Shared Slot có thể không đổi trong khi độ phức tạp tăng → tăng độ khó thực tế.

---

## 4. Đặc tả hệ thống (Game Systems) cho việc lập trình

### 4.1 Cube Shape (Khối hình đa-cube)
```
CubeUnit {
  id: string
  color: ColorEnum
  gridPosition: {x, y, z}     // vị trí trong không gian isometric
  isExposed: bool             // có thể tap được không (dựa theo layer che khuất)
  ballCount: int               // số bi sinh ra khi vỡ (vd 3-6)
}

CubeShapeLevel {
  levelId: int
  cubes: CubeUnit[]
  totalBalls: int             // = tổng ballCount của tất cả cube => hiển thị mẫu số X/50
}
```
- Khối hình có thể được định nghĩa dạng **voxel map 3D** (mảng 3 chiều đánh dấu ô nào có cube, màu gì).
- Thuật toán xác định `isExposed`: một cube "lộ ra" (tap được) khi không bị cube khác che ở phía trước theo hướng nhìn isometric (thường là hướng top-front-right). Có thể đơn giản hóa: cube ở "lớp ngoài cùng" của mỗi cụm.
- Khi cube bị crumble, cập nhật lại cube nào trở thành `isExposed = true` tiếp theo (giống logic "peel layer" của game xếp hình 3D).

### 4.2 Ball & Physics
```
Ball {
  id: string
  color: ColorEnum
  position: {x, y}
  velocity: {vx, vy}
  state: "falling" | "in_container" | "in_shared_slot"
}
```
- Vật lý đơn giản: trọng lực kéo bi xuống, có thể thêm easing/bounce nhẹ khi bi đến điểm đích.
- Bi bay theo đường cong (parabol hoặc bezier) tới vị trí container/slot tương ứng, không cần physics engine phức tạp — dùng tween/animation là đủ.

### 4.3 Container Queue
```
Container {
  id: string
  color: ColorEnum
  capacity: int       // số bi cần để đầy (vd 3)
  filled: int         // số bi hiện có
  state: "active" | "completed"
}

ContainerManager {
  activeSlots: Container[4]      // 4 container hiển thị
  queue: Container[]             // hàng đợi phía sau, định nghĩa theo level
  onContainerFilled(container) -> remove, shift queue vào activeSlots
}
```
- Khi `filled == capacity` → trigger hiệu ứng hoàn thành, xóa khỏi `activeSlots`, lấy phần tử đầu của `queue` đẩy vào vị trí trống.
- Level designer định nghĩa trước thứ tự đầy đủ của `queue` để đảm bảo game có thể completable (tổng số bi mỗi màu trong toàn bộ cube khớp với tổng capacity các container cùng màu, cộng biên độ cho phép rơi vào Shared Slot).

### 4.4 Shared Slot
```
SharedSlot {
  capacity: int        // vd 10
  currentBalls: Ball[]
  isOverflowing: bool  // khi length > capacity => Game Over
}
```
- Cơ chế cơ bản (bắt buộc): bi màu không khớp container nào đang active → vào Shared Slot, không tự thoát ra trừ khi thiết kế thêm cơ chế "khi container cùng màu mở ra sau đó thì lấy bi từ Shared Slot bù vào trước" (tùy chọn nâng cao, tăng chiến thuật; có thể để v1 đơn giản: bi vào Shared Slot sẽ ở lại đó vĩnh viễn cho tới hết màn).
- Khi `currentBalls.length > capacity` → Game Over (Level Failed).

### 4.5 Timer & Level State
```
LevelState {
  levelIndex: int
  timeRemaining: int (seconds)
  ballsProcessed: int
  totalBallsTarget: int   // hiển thị "10/50"
  status: "playing" | "won" | "lost_time" | "lost_overflow"
}
```
- Đồng hồ đếm ngược chạy real-time khi `status == "playing"`.
- Khi `status` chuyển sang `won`/`lost_*` → dừng timer, hiện màn hình kết quả (Win/Lose overlay), cho phép Retry / Next Level.

### 4.6 HUD
- Pause button → mở modal Pause (Resume / Restart / Quit to Menu).
- Level label góc phải trên.
- Progress counter `X/Y` ngay dưới đồng hồ, cập nhật realtime mỗi khi có bi được xử lý thành công (vào container hoặc shared slot — tùy định nghĩa, thường tính cả 2 loại là "processed").

---

## 5. Đề xuất kiến trúc kỹ thuật để clone

### 5.1 Công nghệ đề xuất
- **Frontend:** React + Canvas (hoặc PixiJS/Phaser 3 cho animation 2D mượt) — Phaser 3 phù hợp nhất cho game 2D có physics/tween đơn giản.
- **Ngôn ngữ:** TypeScript.
- **State management:** Zustand hoặc React Context cho state UI (level, timer, HUD); game logic core tách riêng thành module thuần JS/TS không phụ thuộc UI framework (dễ test).
- **Rendering khối cube isometric:** dùng sprite 2D vẽ theo phối cảnh isometric (không cần WebGL 3D thật) — vẽ 3 mặt (top/left/right) mỗi cube bằng polygon màu khác sắc độ để tạo cảm giác khối 3D, giống ảnh gốc.

### 5.2 Cấu trúc thư mục đề xuất
```
cube-crumble/
├── src/
│   ├── core/                # Game logic thuần, không phụ thuộc UI
│   │   ├── CubeShape.ts
│   │   ├── BallSystem.ts
│   │   ├── ContainerManager.ts
│   │   ├── SharedSlot.ts
│   │   └── LevelState.ts
│   ├── levels/
│   │   └── level_data.json  # định nghĩa voxel map + container queue mỗi level
│   ├── scenes/               # Phaser scenes hoặc React components
│   │   ├── GameScene.ts
│   │   ├── HUD.tsx
│   │   └── ResultOverlay.tsx
│   ├── assets/
│   │   ├── sprites/
│   │   └── sounds/
│   └── App.tsx
├── public/
├── package.json
└── README.md
```

### 5.3 Thứ tự triển khai đề xuất (checklist cho Claude Code)
1. Dựng khung project (Vite + React + TypeScript + Phaser hoặc PixiJS).
2. Implement `CubeShape` — định nghĩa voxel map + render isometric cube tĩnh (chưa tương tác).
3. Thêm tap detection: xác định cube nào exposed, cube nào tap được, hiển thị highlight khi hover/tap.
4. Implement crumble animation: cube vỡ → sinh N bi, hiệu ứng particle vỡ vụn.
5. Implement `Ball` fall animation (tween xuống container/shared slot).
6. Implement `ContainerManager`: 4 slot hiển thị, logic fill/complete/queue shift.
7. Implement `SharedSlot`: hiển thị chồng bi, tính overflow → trigger Game Over.
8. Implement HUD: pause, timer countdown, level label, progress `X/Y`.
9. Implement Win/Lose overlay + Retry/Next level flow.
10. Data-drive levels: viết JSON định nghĩa cho nhiều level (voxel map, màu, container queue, thời gian, sức chứa shared slot).
11. Polish: âm thanh, hiệu ứng particle, animation glow khi container gần đầy.
12. (Optional) Thêm progression/level select screen, lưu tiến trình (localStorage).

### 5.4 Định dạng dữ liệu level mẫu (JSON)
```json
{
  "levelId": 8,
  "timeLimitSeconds": 118,
  "sharedSlotCapacity": 10,
  "targetBalls": 50,
  "cubes": [
    { "x": 0, "y": 0, "z": 0, "color": "purple", "ballCount": 4 },
    { "x": 1, "y": 0, "z": 0, "color": "red", "ballCount": 4 },
    { "x": 2, "y": 0, "z": 0, "color": "orange", "ballCount": 3 }
  ],
  "containerQueue": [
    { "color": "purple", "capacity": 3 },
    { "color": "red", "capacity": 3 },
    { "color": "orange", "capacity": 3 },
    { "color": "yellow", "capacity": 3 },
    { "color": "purple", "capacity": 3 }
  ]
}
```

---

## 6. Bảng màu tham khảo (từ ảnh gốc)

| Màu       | Mã hex gợi ý |
|-----------|--------------|
| Tím (Purple) | `#6C4BE0` / `#8B5CF6` |
| Đỏ (Red)     | `#D6362E` |
| Cam (Orange) | `#F5871F` |
| Vàng (Yellow)| `#F9C233` |
| Nền xanh nhạt (Background) | `#CFE3F0` |
| Viền khối (Outline) | `#1E3A8A` (xanh navy đậm) |

---

## 7. Ghi chú bổ sung
- Đây là clone dựa trên quan sát ảnh chụp màn hình + mô tả công khai trên App Store; số liệu cụ thể (sức chứa container, sức chứa shared slot, số bi mỗi cube...) là **giả định hợp lý**, nên điều chỉnh qua playtesting.
- Khi triển khai với Claude Code CLI, có thể yêu cầu build từng phần theo checklist mục 5.3, mỗi bước có thể là 1 lệnh/1 phiên làm việc riêng để dễ kiểm soát.
