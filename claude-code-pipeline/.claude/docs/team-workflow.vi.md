# Quy trình làm việc — Developer + Game Designer

> Bản dịch tiếng Việt của `team-workflow.md`. **Bản tiếng Anh là bản gốc** — lệch nhau thì bản
> tiếng Anh thắng; sửa bản gốc trước rồi mới dịch lại.

**Ai** làm **gì**, **khi nào**, và **bàn giao ở đâu**. Thứ tự lệnh nằm ở `/gd-workflow-help`
(skill đó còn đánh dấu "bạn đang ở đây"); lệch nhau thì skill thắng. Bản tóm tắt một trang:
`pipeline-field-guide.pdf` ở gốc repo.

---

## 1. Nguyên tắc — File là giao diện

Developer và designer không bao giờ bàn giao qua chat. Bàn giao bằng file:

| Thư mục / file | Ai ghi | Ai đọc | Bên còn lại được… |
|---|---|---|---|
| `design/gdd/game-concept.md`, workbook `.xlsx` gốc | Designer | Cả hai | góp ý, không sửa |
| `design/gdd/<system>.md` (GDD) | Dev, một mình, chạy `/design-system` — giá trị design trích từ docs của designer | Cả hai | sửa các `[PLACEHOLDER]` qua Open Questions |
| `design/pipeline/*.md` (flow map, difficulty model, mix matrix, level definition, level intent) | Designer, qua các skill `gd-*` | Dev, simulation, generator | chỉ đọc |
| `design/pipeline/level-curves.md` | Dòng Designed: `/gd-level-intent` · Dòng Measured: `/gd-level-audit` | Cả hai | chỉ dòng của mình |
| `docs/architecture/`, ADR, code trong `Assets/` | Dev | Designer chỉ qua dev | không bao giờ đụng |
| `docs/_session/active.md` | Người chạy session | Chính người đó, sau khi compact | — |

Hai luật suy ra từ đây, vốn đã là luật của dự án:

- **Dev không bao giờ quyết độ khó, balance, giá, bảng màu** (`CLAUDE.md` §10). Quyết định
  design còn trống thì ghi `UNDEFINED` (hoặc `[PLACEHOLDER]` đánh dấu rõ) và gửi designer — không bao giờ coi như đã chốt.
- **Designer không bao giờ đọc hay sửa code** — `/gd-mode` cưỡng chế điều này. Câu hỏi về việc
  game *hiện đang làm gì* thì hỏi dev.

---

## 2. Thiết lập một lần (lead dev)

1. Chép `CLAUDE.md` + `.claude/` vào dự án, chạy `/project-overview`, điền các `<...>`
   (`project_setup.md` §6).
2. Tạo `.asmdef` cho code root, wrapper `GameDebug` và define `ENABLE_LOGS`.
3. Branch (`rules.md` §4): `<integration branch>` do lead sở hữu; `feature/<name>` cho mỗi task
   của dev; `gd` cho designer — **không bao giờ có file `.cs`**. Merge vào integration branch ít
   nhất mỗi ngày.
4. Thống nhất quyền sở hữu file ở §1 với designer trước khi ai ghi gì.

---

## 3. Thói quen mỗi session

| | Developer | Designer |
|---|---|---|
| Đầu mỗi session | `/project-overview`, rồi nêu branch | `/gd-mode` |
| Kiểu trả lời | `/dev-brief on` — trả lời quyết định bằng `1A 2B`, `ok`, `skip N` | `/dev-brief on` dùng được cả trong `/gd-mode` |
| Unity Editor | `/use-mcp once` cho một lần kiểm tra, `on` cho đợt đo — còn lại tắt | không bao giờ |
| Phạm vi | một session = một stage = một branch (`context.md` §6) | một session cho mỗi chủ đề design |
| Nguồn sự thật | code + `docs/features/*.md` | workbook `.xlsx`, đồng bộ qua `gdd-sync` |

Mỗi người chạy session Claude riêng — không bao giờ dùng chung (`rules.md` §4).

---

## 4. Các phase — ai làm gì

```
Phase 0      Dev: base project           ∥   Designer: concept docs
                 ↓ cả hai xong
Phase 1A/1B  Dev: kiến trúc (1A)         ∥   Designer: difficulty model (1B)
                 ↓ cả hai xong — HỌP ĐỒNG BỘ
Phase 2      Designer: bot playstyle     →   Dev: simulation
Phase 3      vòng level hằng tuần (§5)
Phase 4      sau soft launch: calibrate, quay lại phase 3
```

### Phase 0 — Khởi động (song song)

| Developer | Designer |
|---|---|
| Base project: stack điền ở `project_setup.md` §1, asmdef, `GameDebug` | Concept: `design/gdd/game-concept.md` — viết tay hoặc dùng `/brainstorm` |
| Đã có code sẵn → `/reverse-document` để dựng lại docs từ code | Bắt đầu workbook `.xlsx` gốc |

**Bàn giao:** `game-concept.md` được cả hai duyệt. Chưa bên nào vào phase 1 trước đó.

### Phase 1A ∥ 1B — Song song, không ai chờ ai

| 1A — Developer | 1B — Designer (trong `/gd-mode`) |
|---|---|
| `/map-systems` → `systems-index.md` | `/gd-map-flow` → `flow-map.md` (**đầu tiên**, từ vựng cho mọi bước sau) |
| `/design-system` → mỗi system một GDD, **chỉ dev**. Luật, con số, khoảng giá trị trích từ docs của designer (file 1B, `.xlsx`) và dev xác nhận; docs chưa có thì dev điền giá trị tạm gắn `[PLACEHOLDER]` (hoặc `UNDEFINED`) kèm Open Question cho designer. Không hỏi cảm tính, không spawn agent trừ khi dev yêu cầu | `/gd-core-difficulty` → `difficulty-model.md` |
| `/create-architecture` → tách Model/View, danh sách ADR | `/gd-mechanic-difficulty` × từng mechanic |
| `/architecture-decision` × từng ADR bắt buộc | `/gd-mechanic-object-mix` + `/gd-mechanic-mix` (cả hai, trước bước sau) |
| | `/gd-level-definition` → tier profile |

Designer không tham gia session 1A. Designer xử lý Open Questions của các GDD sau —
xác nhận hoặc thay từng `[PLACEHOLDER]` trong GDD và field config tương ứng — tốt nhất sau khi
1B đã có các file độ khó, vì chúng chốt được phần lớn con số.

**Bàn giao — họp đồng bộ.** Checklist trước phase 2:
- [ ] Thư mục model không có tham chiếu `UnityEngine` (`architect.md` §1)
- [ ] `difficulty-model.md` và `level-definition.md` đã có và được designer ký duyệt
- [ ] Mỗi mechanic trong level definition đều có `design/pipeline/mechanics/<name>.md`

### Phase 2 — Dạy bot chơi (tuỳ chọn — chỉ cần khi muốn đo)

| Thứ tự | Ai | Lệnh |
|---|---|---|
| 1 | Designer được phỏng vấn | `/gd-bot-playstyle` → `bot-playstyle.md` |
| 2 | Dev dựng | `/gd-prototype-sim` → simulation C# thuần, bot, solver API, test |

Mechanic chưa có bot rule thì level chứa nó là `unscored` — sinh được, không đo được.

### Phase 4 — Hiệu chỉnh theo người chơi thật (sau soft launch)

Designer cung cấp dữ liệu (export analytics); dev chạy `/gd-calibrate` với 20% holdout bắt
buộc. Weights mới → quay lại phase 3.

---

## 5. Vòng level hằng tuần (phase 3)

| Khi nào | Ai | Làm gì | Kết quả |
|---|---|---|---|
| Đầu tuần | Designer | Cập nhật sheet intent → `/gd-level-intent` → **duyệt đường thiết kế** (●) | `level-intent.md`, dòng Designed trong `level-curves.md` |
| Một lần, trước audit đầu tiên | Designer | Điền bảng **Tolerances** trong `level-curves.md` | thiếu nó thì không ra được verdict |
| Giữa tuần, giờ cố định | Dev | `/use-mcp on` → `/gd-level-gen` → `/gd-level-audit` | candidate export ra folder riêng; dòng Measured (○) + verdict |
| Cuối tuần | Designer | Đọc `level-curves.md`: giữ / sửa / thay từng level | quyết định ghi cạnh từng level |
| Hằng ngày | Cả hai | Merge `gd` và `feature/*` vào integration branch | — |

Designer không tự chạy được gen hay audit — cả hai cần Editor bridge, mà chỉ dev bật. Vì vậy
lượt chạy của dev là **giờ cố định hằng tuần**, không phải yêu cầu đột xuất.

Level không bao giờ bị ghi đè: level sinh ra và level đã sửa đều vào folder riêng, designer
quyết cái nào thay cái nào (`anti-patterns.md` §5).

---

## 6. Quyết định và câu hỏi

- Mọi câu hỏi mở đều được ghi lại kèm **người sở hữu** (designer / dev / art), trong mục Open
  Questions của doc liên quan hoặc trong `docs/_session/active.md` — không bao giờ để trong chat.
- Khối cuối của `/dev-brief` (❓ quyết định · 🔒 duyệt · 🛠 hành động) là format để chuyển câu
  hỏi cho người kia: tiêu đề, mỗi lựa chọn một dòng, dòng 🧭 hiện trạng.
- Một quyết định chỉ chốt khi đã **nằm trong file**. Cuộc trò chuyện không phải là bản ghi.

---

## 7. Giới hạn đã biết

- Pipeline `gd-*` hợp với dòng **puzzle resource-flow** (sort, jam, tray, water sort…). Thể loại
  khác thì dừng ở `/gd-map-flow`; chỉ dùng phase 0 và 1A.
- Phase 2 được `/gd-workflow-help` đánh dấu **chưa kiểm chứng** — dành thời gian cho dev làm
  chắc simulation trước khi tin số liệu.
- Số liệu của bot xếp hạng level đáng tin; giá trị **tuyệt đối** chưa phải sự thật về người chơi
  cho tới phase 4 (`verification.md` §5).
