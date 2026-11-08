# CourtX · 半场配合原型

战术版入口：本机 `Windows_Tactics/CourtX Tactics.exe`。打开后选择“高位挡拆”或“高位接应 · 传切”，再选择 01 持球开局或 04 无球开局。Unity 场景为 `Assets/CourtX/Scenes/TacticalTraining.unity`。旧的自由练习仍在 `Windows/CourtX.exe` 和 `HalfCourt.unity`。

源码稳定版在 `main`，标签 `v0.1-free-practice`；战术开发在 `feature/tactical-training`。Git 仓库不提交 Unity 缓存、Windows 构建和原始录像。下载源码后用 Unity 2022.3.62f3c1 打开 `Unity` 文件夹；通过 `TacticalAuthoring.Build` 可输出独立 Windows 战术版。

| 战术版操作 | 输入 |
| --- | --- |
| 走 / 跑 | WASD；按住 Shift 加速 |
| 实时俯视 | 按住 Tab；移动与 NPC 更新继续进行，松开回第一人称 |
| 选传球目标 | 1 / 2，在另外两名进攻队友间选择；HUD 显示球衣号码 |
| 传球 | 持球时按住 Q 收球，松开传给所选队友；蓄力增加球速 |
| 要球 | 无球时 E；AI 判断距离和防守线，不能传时显示原因 |
| 重开当前组合 | R |
| 选战术 / 角色、暂停 | Esc 打开菜单；Esc 或“继续当前回合”恢复 |
| 收起帮助 | F1 |

你一直操控选定的角色，球权变化不会自动切换人物。无球时 E 是请求传球，不会把篮球直接召回。地面绿圈是当前建议位置；允许自行跑位。橙色准星提示传球线路有风险，绿色表示当前判断有空当，但移动中的防守人仍可能截断。

高位挡拆：04 到圈内站定 → 01 从右侧绕过掩护 → 04 顺下接球。高位接应：04 提到罚球线附近接球 → 01 从外侧切入 → 04 回传。两套都是局部 3v3 配合，不是完整五人 2–3 联防。本版以完成一次配合为目标，未加入投篮计分、篮板或完整比赛规则。

详细接口见 [战术接入说明](docs/tactical-integration.md)，研究与许可证记录见 [参考来源](docs/tactical-sources.md)，试玩证据见 [验证记录](docs/tactical-results.md)。

11 月 8 日传接球改进：[三轮迭代记录](docs/pass-receive-refinement.md)。传球现在带有跑动提前量；球出手后，接球光圈固定，临时反向跑可能接不到。防守人背身碰球会弹开，正面看见来球才可能接稳。无球 E 请求保留 3.5 秒，菜单暂停不消耗时间，球权变化或超时会取消。此前战术版保存在 `v0.2-tactics-before-refinement` 标签。

## 稳定自由练习版（保留）

双击同目录 `Windows/CourtX.exe` 进入半场。分发时一起复制整个 Windows 文件夹。这个版本用桌面第一人称视角检查站位、遮挡和距离。

| 操作 | 输入 |
| --- | --- |
| 前后左右移动 | WASD 或方向键 |
| 跑步 | 按住左或右 Shift，松开恢复走路 |
| 转头 | 单击画面后移动鼠标，或按住右键拖动 |
| 实时俯视 | 按住 Tab 查看，松开立即回第一人称，期间可以继续移动和跑步 |
| 蓄力抛球 | 按住 Q 收球蓄力，松开沿视线方向抛出，最长蓄力 1.2 秒 |
| 收回篮球 | E。抛出 6 秒后也会自动回到手边 |
| 回到持球人起点并收球 | R |
| 隐藏 / 显示操作提示 | F1 |
| 释放鼠标 | Esc |

在 Unity Hub 添加 `Unity` 文件夹，使用 2022.3.62f3c1 打开。双击 `Assets/CourtX/Scenes/HalfCourt.unity`，按 Play。项目使用 Built-in Render Pipeline。

## 场景与角色交接

球场宽 15 m、纵深 14 m，另设缓冲区。底线中点为世界原点，Y 轴朝上，从弧顶攻筐沿 +Z。1 单位对应 1 m。场地、场线、篮架和碰撞保存在 `Assets/CourtX/Prefabs/HalfCourt.prefab`。

场景里的 `Scenario - High screen right corner` 持有 `CourtLayout`。六个出生点位于其 `SpawnPoints` 子物体中。移动或旋转这些空物体可以编辑开局站位。根节点在脚底，蓝色 Z 轴指向角色面朝的方向。

| CourtRole | 预览站位 |
| --- | --- |
| BallHandler | 弧顶持球人，第一人称视角。俯视图用 YOU 和朝向箭头标出 |
| Screener | 左侧掩护队友，青色 04 |
| CornerShooter | 右侧底角队友，青色 05 |
| OnBallDefender | 持球人对位，橙色 11 |
| ScreenDefender | 掩护人对位，橙色 12 |
| HelpDefender | 弱侧协防人，橙色 13 |

正式接入时，禁用 `PreviewOnly - disable for team integration` 整个根节点，启用团队自己的摄像机、音频监听器和人物。球场和站位会保留。给角色的生成脚本拖入场景内的 `CourtLayout` 引用，再用角色枚举读取出生点。

```csharp
using CourtX;
// layout 为场景中的 CourtLayout；actorRoot 的原点在角色脚底。
layout.PlaceAtSpawn(actorRoot, CourtRole.Screener);
Transform playerStart = layout.GetSpawn(CourtRole.BallHandler);
// XR 接入时，以 playerStart 的位姿放置 VR Rig 的地面根节点。
```

`ScreenPoint`、`RollTarget`、`WeakSideCorner` 是可拖动的地面战术点，`RimTarget` 是篮筐中心。另一套战术可以复用同一个球场，另存场景并调整起点和战术点。

当前五名占位人保持静止。走路速度为 3 m/s，按住 Shift 时为 5.5 m/s，可在 Walkthrough 的 Inspector 中调整。预览篮球在右手侧自动运球，Q 收球和松手使用 Rigidbody 抛球，球会与球场碰撞。正式人物动画、队友接球、NPC 决策与头显输入由团队后续接入。

篮球和手臂也在 `PreviewOnly` 下。后续可以调用 `PreviewBasketball.BeginCharge()`、`Release()`、`Recall()`，并读取 `IsCharging`、`IsAirborne`、`Charge01`。关闭整组预览对象后即可接入团队正式角色。

首轮验场时可以先从弧顶横移两步，转头看右侧底角，再按 Tab 核对两名弱侧球员的相对位置。俯视图使用场上对象当前的位置，青色圆标为进攻方，橙色方标为防守方，号码和球衣一致。

最新录像为 `Preview/Playtests/controls-20261106.mp4`，包含走路、Shift 跑步、移动中按住/松开 Tab、右手运球和 Q 蓄力抛球。录像由 Unity Play 模式的同一套控制函数逐帧驱动，直接采集场景相机。独立程序还会显示可按 F1 收起的键位提示。此前的 `final.mp4` 是 11 月 2 日版本。
