# Windows Codex handoff · GazeFlow 0.2.0-exp.1

将下面整个执行任务交给 Windows 上的 Codex。它应先发现环境，再在 VR 项目的副本中操作；不要把它当作已经完成验收的说明。

---

你现在负责在这台 Windows 电脑上，验证 GazeFlow `0.2.0-exp.1` 的新增研究目标／AOI 射线命中功能，并回归原有 VR 密室实验 研究 pipeline。请实际完成可执行工作、保留日志，并在需要我佩戴设备或操作控制器时给我明确指令。

## 目标与版本定位

仓库：https://github.com/DolbyWu/neon-unity-research-toolkit

开发分支：`feature/neon-xr-research-targets`。

本次固定代码提交：`04ff19498532c523d1b24b32b0674b0ae2c2d0ef`，两个包的版本均应为 `0.2.0-exp.1`。分支之后可能有文档更新；不要悄悄改用最新功能代码。该提交已经存在，不要求创建 Release 或标签。

必须先阅读：

- `docs/neon-xr-prototype.md`
- `Packages/com.dolbywu.neon-research/Documentation~/neon-xr-targets.md`
- `QUICKSTART.md`
- `docs/hardware-acceptance-2026-11-07.md`
- `Packages/com.binyanxu.gazeflow.neonxr/README.md`

这是 **GitHub 完整版的实验开发版本**。我要保留参与者 archive、原始眼动／瞳孔、头部姿态、行为日志、可选 Python／FFmpeg VR 视频／音频录制、视频偏移与时间轴回顾、人工标注及导出。不是安装排除了外部录屏的 Asset Store 版本。不要更改或重新提交 Unity 商店，不修改 main 或 `store/0.1.0-preview.6`。

## 一、发现这台电脑的环境

先检查当前工作区、相关仓库说明／AGENTS.md、Unity Hub 中的项目、可访问的目录和已安装 Unity Editor。找出：

- 原 VR 密室实验 项目绝对路径、实际实验场景、Unity 版本、当前 XR／OpenXR／Meta／XRI 依赖。
- 旧插件的安装方式：UPM tarball、Git、embedded/local package、`Assets` 下的复制脚本，还是混合状态。
- `Packages/manifest.json`、`packages-lock.json`、相关 asmdef，以及旧插件脚本／GUID 所在位置。
- Python、FFmpeg、FFprobe、Quest Link／Oculus Mirror、Neon Companion 与当前录屏设置。
- 源项目是否正在 Unity 中打开、是否有未保存更改；不要自动退出或丢弃更改。

如果不能可靠确定原项目路径，先只问我这个路径；如果存在多个候选项目，列出名称和路径让我选择。其他能从文件／已授权 UI 获得的信息自己核实。

## 二、创建项目副本并保留基线

Unity Hub 用于添加／打开副本；如果没有合适的复制功能，关闭相关项目后，在文件系统复制，再通过 Hub 的 Add／Add project from disk 添加副本。不要假设 Hub 一定有 Duplicate 按钮。

- 副本命名为 `<原项目名>_GazeFlow_0.2.0-exp.1_Test_<日期>`，放在原目录之外，不能是其子目录；若已存在则使用新名称，不覆盖。
- 先清点必要目录。复制 `Assets`（包括所有 `.meta`）、`Packages`、`ProjectSettings` 及实验所需其他配置／外部引用资源。记录指向原目录或外部 local package 的依赖，避免从副本编辑到原文件。
- `UserSettings` 只复制需要的实验配置，并重新检查机器相关路径。不要默认搬运真实参与者数据；原 archive／录像／标注必须留在原位置且不被写入。
- `Library`、`Temp`、`Obj`、构建缓存和 `.git` 通常不需要复制。仅在确定不是项目输入时排除目录，不能按模糊名称排除实验资源。
- 若用 `robocopy`，不要使用 `/MIR`、`/PURGE`；确认源／目标不是同一路径或嵌套关系，并处理退出码：0–7 通常不是复制失败，8 及以上停止检查。
- 记录原始 manifest／lock、关键插件与实验脚本、场景和 prefab 的 SHA-256；只在副本建立本地 checkpoint／备份。完成后用于确认原项目没有被修改。
- 用该项目实际 Unity 版本打开副本。已知基线是 Windows Unity `6000.3.23f1`，不要自动升级／降级原项目。若当前版本不同，记录并评估副本的兼容性。
- 更新前先做一次短基线检查：无编译错误，实验场景可用。若旧 pipeline 已有问题，记录清楚，不归因给新插件，也不要假装基线通过。

给副本配置**全新的测试数据输出目录**和匿名技术测试 ID，例如 `GF-TECH-001`；不要让副本继续写入旧研究 archive。先保持自动录制关闭，直至配置完成。

## 三、在副本替换旧插件

先解释实际识别到的旧插件路径和替换方式，再执行可恢复的替换；不需要为每一个常规步骤反复问我确认。

- UPM 安装：移除／替换副本中旧的核心包引用。已知核心 ID 为 `com.dolbywu.neon-research`；商店版本可能为 `individual.binyan-xu.gazeflow`。同一副本只保留一个核心实现。
- `Assets` 下安装：只把确认属于旧插件的文件及对应 `.meta` 移到**副本外的备份目录**，不要移到仍会编译的 `Assets` 子目录。目录混有实验脚本时，不要整目录移走；先逐项确定归属。
- 不按 `Neon`／`Study` 等关键词批量删除文件，不删除其他 Pupil Labs、Meta、XR 或实验组件。不直接编辑 `Library/PackageCache`。
- 保留实验场景、prefab、交互回调、字段和 GUID 引用。升级前后检查 Missing Script、UnityEvent 目标、头部引用；不能通过静默移除报错组件来“修复”。
- 依赖锁文件让 Unity 正常重新解析；不为了消除报错随意清空所有依赖或降级 XR 栈。

在项目之外找一个稳定位置 clone 仓库，不要 clone 到 `Assets` 中。可以使用以下命令（路径自行根据本机确定；不要覆盖已有 checkout）：

```powershell
git clone --branch feature/neon-xr-research-targets https://github.com/DolbyWu/neon-unity-research-toolkit.git <新的仓库目录>
git -C <新的仓库目录> checkout --detach 04ff19498532c523d1b24b32b0674b0ae2c2d0ef
git -C <新的仓库目录> rev-parse HEAD
```

若本机没有 Git，使用这个固定提交的源码 ZIP：

https://github.com/DolbyWu/neon-unity-research-toolkit/archive/04ff19498532c523d1b24b32b0674b0ae2c2d0ef.zip

安装顺序：

1. Package Manager → Add／Install package from disk → `<仓库>/Packages/com.dolbywu.neon-research/package.json`。
2. 安装官方 Neon XR Core 的固定 Git 版本：

   ```text
   https://github.com/pupil-labs/neon-xr.git?path=/com.pupil-labs.neon-xr.core#f4dee47b9690a059b1c4660628596d7303ade59a
   ```

3. 按官方文档完成 Addressables 设置、资源组导入／构建和 NeonXR prefab 配置。核对实际解析的依赖，不自动降级现有 XR／Input System 栈或导入整套模板替换实验。
4. 再从 disk 安装 `<仓库>/Packages/com.binyanxu.gazeflow.neonxr/package.json`。
5. 核对两个 GazeFlow 包均为 `0.2.0-exp.1`、官方 Core 为检查过的 `2.0.0`，并记录实际锁定提交。

官方设置文档：https://docs.pupil-labs.com/neon/neon-xr/neon-xr-core-package/

若依赖解析或编译失败，保存准确报错，定位后在副本／独立修复分支做最小修复。不要只靠关闭 Console 或修改版本显示来宣布通过。记录相对固定提交的 diff；未经指示不推送修复或发布软件。

## 四、恢复原 pipeline 并添加三个研究目标

1. 在 **Tools → Neon Research → Setup and Recording Settings** 配置新输出目录、实际 Companion IP／端口和原有原始数据接收。这里仍使用 Neon Research 菜单名称。
2. 核对 Windows 上 Python、FFmpeg、FFprobe 的真实路径与版本，恢复可选视频／麦克风设置。先用原有跑通过的窗口录制方式，确认 Oculus／Quest Mirror 窗口名称、尺寸和正确 VR 视角。不要因为新空间适配器能工作就关闭原始眼动接收；它目前不替代完整原始眼动／瞳孔记录器。
3. 保留原有头部姿态、区域和行为适配器。检查抓取／释放、任务事件回调，避免重复挂载或重复写入。
4. 在副本实验场景中选择三个明确对象：钥匙、线索、门。实际没有这些对象时选择等价目标并记录。添加 `StudyResearchTarget`，使用稳定、唯一 ID，例如 `key_01`、`clue_01`、`door_01`，并设置名称／分组。
5. 目标需要启用的非 Trigger collider。不要把原来用于触发器的 collider 改成实体碰撞体而破坏游戏；需要时增加只用于研究检测的子 collider，并检查物理碰撞矩阵，确保不会改变交互／移动。触发器专用 AOI 暂不支持。
6. 在官方 `NeonGazeDataProvider` 中确认没有启用模拟眼动。完成官方 mount calibration；用官方视线可视化和已知目标先检查多个头姿下的位置与方向。不要把“已经连接”当成空间映射通过。
7. 添加 `NeonXRResearchProvider`，把 `neon` 指向官方 provider，`headReference` 指向实际随头移动的 XR 摄像机／头 Transform，不能随意使用 XR rig 根节点。记录校准 ID；在我实际确认已知目标映射后才设置 `mappingVerified=true`。没有确认时应记录 `mapping_unverified`。
8. 添加且只启用一个 `StudyGazeTargetRecorder`，在 Play 前绑定 `provider`，设置合理距离和碰撞层。遮挡物必须在检测 mask 中；忽略的层不可能遮挡射线。该原型忽略 Trigger collider。
9. 新增菜单 **Tools → GazeFlow → Research Target Setup (Experimental)** 用于配置检查。主设置窗口中旧的“无物体映射”提示描述的是原始接收路径，不表示这个可选适配器一定没有安装；同时也不能因为出现新菜单就声称空间映射成功。
10. 将现有交互适配器的 `objectId` 与研究目标 ID 对齐。CSV 是输出记录，不会自动驱动抓取或开门；既有 UnityEvent 必须确实连接。

若要先排查目标配置，可以在另一个测试场景导入 Three research targets 示例；不要把示例立方体和另一套 recorder 随意叠加到实际实验中。

## 五、软件检查与人工真机试验

先确认所有包真实编译。可在 EditMode 运行 `GazeTargetTests`；完整原有测试及会关闭／重开场景的序列化测试应放到另一个空白验收项目执行，不要对正在编辑的实验场景直接批量运行。记录测试 XML、失败与跳过；静态编译不算 EditMode 测试通过。

涉及佩戴 Neon／Quest、移动头部和控制器动作时，分步提示我操作，等我完成再检查结果；不要把代码或合成数据当作实际佩戴证据。

至少运行下列四组测试，使用不同会话并保留 session ID：

| 场景 | 人工操作与检查 | 成功依据 |
|---|---|---|
| A：完整原 pipeline＋三个目标 | 新建匿名技术档案；依次看钥匙、线索、门；改变头部姿态再看；抓取／释放并触发原有任务事件；录制正确 VR 视频及已启用音频，正常停止 | archive、原始眼动、头部、行为、媒体、HTML 都能找到且可读取；新增目标与实际对象相符 |
| B：遮挡／未确认映射 | 在副本的短测试会话中先不确认映射；确认后用 mask 内的非目标物体遮挡目标 | 未确认时不产生目标命中；遮挡后不把后方目标继续算作命中。不要永久破坏实验布局 |
| C：断连／重连 | 先正常采集，再按我可执行的方式中断 Neon 流约 5–10 秒，再恢复；同时保持视频和行为记录观察 | `stale_or_disconnected`／其他准确缺失状态可见；缺口不累计为有效持续时长；恢复情况有证据。不能预先保证恢复一定成功 |
| D：新会话及回顾保存 | 正常结束旧会话，再启动新的技术会话；分别回顾、标注和导出 | 不串用旧会话数据；文件正常关闭；JSON 可重新导入同一会话，原始记录不被覆盖 |

如果摘下 Neon 没有触发 SDK 的 worn 状态，不要强行写成 `not_worn`，应报告实际 SDK 状态及观察。出现缺失瞳孔字段时不能填零或制造数据；先检查 Companion Eye State 和原有接收设置。

## 六、检查真实产物，而不只看 Console

针对每个实际 session，列出完整路径和证据：

- 参与者档案 `participant.json` 及本次会话 `participant_snapshot.json`／`participant_summary.txt`，确认是新测试目录而不是旧 archive。
- `export_manifest.json`：正常关闭、录制错误、实际计数和时间范围。`closed=true` 不证明数据完整或视频成功。
- `Neon-*/NeonGaze.csv`、`session.json`、`clock_sync.csv`（实际存在时）及原始 payload；保留真实缺失和质量信息。
- `head_pose.csv`、`game_events.csv`、区域日志；确认原实验的行为事件未丢失／重复。
- 新增 `gaze_targets.csv`：状态分布、有效行与目标 ID、缺口、源／校准、设备时间及其语义。没有文件时必须解释是否未绑定 provider、没有回调或会话未录制，不能伪造文件。
- `Video/quest-view.mp4`：用 FFprobe 检查可解码、时长、流和正确视角，打开播放检查内容；有音轨不等于声音清楚。检查 `media_capture.json`／finalization 元数据；停止后等待真正完成封装。
- `Replay/study-replay.html`：使用本地真实会话产物，不用仓库合成演示替代验收。

## 七、检查 HTML 到底展示了什么

在 Chrome／Edge 打开该会话 HTML，并逐项验证：

1. 正确 session／参与者、眼动轨迹、行为事件、区域和质量信息；旧功能能继续使用。
2. **Load session video** 能否加载 `../Video/quest-view.mp4`；若浏览器不允许，使用 **Choose local video**，如实记录是否需要手动选择，不能声称始终一键导入。
3. 通过明显事件／标记检查视频偏移；检查开始、中间、结束，记录估计误差与方法，不以单个点对齐推断整段精确同步。不允许把调到看起来一致的偏移写成硬件同步。
4. **Research target hits** 面板是否出现；其回调数量／状态与 CSV 一致。筛选 `key_01`，点击命中回调后是否定位到正确实验时间，并在视频覆盖范围内移动到相应视频位置。
5. 查看首次命中时间、回调次数、观察到的访问和保守覆盖时长。断连间隔不应计入；这些不是 fixation 次数／时长，也不能证明理解或注意。
6. 目标面板使用 **Unity callback time**，独立于眼动轨迹的 corrected／receive 时间选择。SDK 最新 RTSP 快照的设备时间不保证与 ray 原子配对；保留限制，不为了好看改写时间戳。
7. 对同一个 `objectId` 关联目标回调与抓取／任务事件。视线与行为共用会话时轴，但需要检查记录的实际时间关系，不默认零延迟。
8. 创建一个点标注、一个区间标注；填写标注者、观察／回溯访谈证据来源、描述或参与者解释。当前不是自动把 AOI ID 写入标注的完整对象绑定系统，需要在描述／证据中明确对象 ID。
9. 导出 annotation JSON 和 CSV；JSON 保存后关闭／重新打开 HTML 并导入，确认内容与时间恢复。CSV 不用于恢复编辑，浏览器下载不会自动写回 HTML。
10. 导出选定区间的目标 CSV 与完整 interval bundle，检查 `gaze_targets`、行为事件、眼动数据、半开区间边界、会话一致性；不要把 gaze-only CSV 当作包含目标记录。

## 八、最终交付与通过规则

在副本外创建 `GazeFlow-0.2.0-exp.1-Windows-Acceptance-<日期>` 报告目录，保存：

- `ACCEPTANCE.md`：源／副本路径、Editor／设备／SDK／Python／FFmpeg 版本、固定 commit、修改记录、每项 PASS／FAIL／BLOCKED／NOT RUN。
- 安装前后依赖清单、原项目未修改的哈希对照、编译与测试结果／日志。
- 各测试 session 路径、状态统计、必要的本地截图、视频偏移检查、标注导出恢复结果。
- 错误、最小修复 diff、剩余问题、回滚步骤。不擅自提交到 GitHub，不上传实际 archive、眼动、声音或视频。

必须分别报告：① 原 pipeline 回归是否通过；② 官方适配器是否真实编译；③ 新目标空间映射是否通过；④ 缺失／断连恢复是否通过；⑤ HTML／导出是否通过。不能因为录到视频就把其他项全部写 PASS。

如果失败，保留原项目与原插件备份；只回滚副本。若只是依赖问题，仍可保留和检查原 pipeline，但不能把这当作 AOI 新功能通过。工作完成前不要更换“实验版”定位，也不要发布／更新 Unity 商店。
