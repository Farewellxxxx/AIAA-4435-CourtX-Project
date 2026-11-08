# 传球、拦球与接球迭代 · 2026-11-08

本轮在现有 `feature/tactical-training` 上做了三轮「复现 → 修改 → 试玩 → 复查」，保留两套 3v3 配合、固定人物身份、WASD / Shift / 实时 Tab 和原有团队接口。旧自由练习继续使用 `Windows/CourtX.exe`；此前战术源码可通过 `v0.2-tactics-before-refinement` 回退，11 月 7 日独立程序压缩包也保留。

| 轮次 | 发现的问题 | 改进与复查 |
| --- | --- | --- |
| 1：防守触球 | 背身碰球也直接获得球权；低帧率下可能由数组顺序决定谁先抢到 | 连续检测移动球与防守体积，取最早接触。朝向来球的手部可以接稳；盲区身体碰撞会弹开。落球继续下落、反弹，R 重开。接触测试从 7 项失败变为 14 项全部通过，覆盖 15 / 30 / 60 fps |
| 2：移动接球 | 球传向队友旧位置，AI 接球时突然站住 | 按速度预判固定接球点，提前量最多 2.5 m。AI 迎向落点，玩家看光圈自行接应；出手后不追踪人物。接球范围 .78 m，接稳后 .16 秒过渡到手。专项累计 21 项通过，反向跑仍能漏接 |
| 3：要球与反馈 | 旧请求留存、菜单时间消耗请求、换球权后误传、无蓄力也能调用人类释放接口 | 请求绑定当前持球人、有效 3.5 秒、菜单冻结倒计时、超时与球权变化明确取消。预测提示与实际传球共用落点/弧线/防守体积，蓄力提示使用对应球速。专项累计 26 项通过 |

每轮都跑了两套战术 × 两种角色的完整场景路线，当前 45 项检查全部通过；旧自由练习 28 项也通过。测试是实际 Unity 场景和 CharacterController 操作，并包含明确分开的边界夹具，不是模拟假对象。人类路线测试改为出手前判断空当，并主动走向可见接球点；没有削弱拦截判定来让路线过关。

## 本机文件

- 当前程序：`Windows_Tactics/CourtX Tactics.exe`（分发需整个文件夹）。
- 本轮目录：`Preview/Playtests/Tactics/Refinement-20261108/`。
- `round1.mp4`、`round2.mp4`、`final.mp4`：30 fps 实际场景相机录像；相机采集不包含 OnGUI 面板。
- `round1-final-checks.txt`、`round2-complete-checks.txt`、`round3-green-checks.txt`：专项记录。
- `round1-checks.txt`、`round2-checks.txt`、`final-checks.txt`：完整路线和控制回归；TRACE 记录出手时位置。
- `*-red-checks.txt` 保留修改前失败证据；`*-deflection.png` 为身体挡球的固定复现场景。

## 仍然保留的原型边界

这仍是一次半场配合练习：没有新增投篮计分、抢落球、篮板、犯规或攻防转换。球被挡掉后回合结束，按 R 重试；动画是程序化占位动作。绿色传球提示是基于当前运动的风险判断，防守人之后改变方向仍可能截断。

接口和参数见 [接入说明](tactical-integration.md)。

## 独立审阅与最终核验

独立审阅发现 1 个重要边界问题：最后一帧球已到达，防守人却仍按完整一帧移动来判定，导致 15 fps 的迟到防守可能抢断；接球人同样会被误判。补充的 Unity 测试重现了 3 项失败。修复后，球、防守人、接球人都按同一到达时刻计算，**31 项专项 + 45 项完整回归 + 28 项旧版回归全部通过**。没有未处理的审阅问题。

- [最终专项记录](verification/refinement-interactions-final.txt)、[完整路线记录](verification/refinement-routes-final.txt)、[旧版回归](verification/refinement-free-practice.txt)。
- 原自由练习场景、预制体、控制/篮球脚本和 ProjectSettings 与稳定标签比较无差异。
- Windows 构建成功，并实际启动最终程序检查菜单。三轮界面已通过原生输入检查：无球要球、AI 传球和接稳、受阻提示与倒计时、菜单切换。`native-request-ui.mp4` 与 [要球界面](verification/refinement-request-ui.jpg) 保留证据；这段 UI 录像录于最终时刻边界修复前，界面代码未再变化。
- `final.mp4` 是审阅修复后的 1280×720 / 30 fps / 57.4 秒录像，已完整解码和抽帧检查。[录像缩略图](verification/refinement-routes.png)。
- 分发包：本轮目录内 `CourtX-Tactics-20261108.zip`，包含完整 `Windows_Tactics` 文件夹。SHA-256：`298AE1C453D578CE460E2266EA5205F639D529944F8A983B8AAA509D8BA7D93C`。

本次执行保留训练范围：挡掉的球落地后按 R 重开。因此它仍不支持争抢篮板和对手快攻；这是继续复用当前半场配合结构的取舍。审阅没有单独评价的打包启动和动作画面，已由上述构建、原生界面检查和录像检查补足。
