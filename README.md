# CourtX 半场初版

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
