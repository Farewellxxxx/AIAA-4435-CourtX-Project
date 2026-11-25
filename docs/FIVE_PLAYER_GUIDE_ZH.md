# CourtX 5v5 使用说明

## 启动

Windows、macOS 和 Android 的下载都在同一发布页。桌面版先完整解压 ZIP，Android 版在手机或平板上打开 APK 安装。[v0.7.0 发布页](https://github.com/Farewellxxxx/AIAA-4435-CourtX-Project/releases/tag/v0.7.0)

- **Windows x64**　双击 `Start-CourtX.cmd`。从仓库启动也可以双击根目录的同名文件。练习回放保存在 `CourtX.exe` 旁的 `Replays5v5` 文件夹。
- **Android（64 位 ARM）**　在手机或平板上打开下载的 APK，按系统安装提示操作，然后横屏启动 CourtX。左下摇杆控制跑位，在球场空白处滑动可以转头，准星瞄准队友或篮筐后点击出球按钮。每轮保存后可从「即时回放」打开，记录在此设备的应用数据中。
- **macOS（M 系列或 Intel）**　双击 `CourtX.app`。练习回放保存在当前用户的“资源库 → Application Support”中的游戏数据目录，文件夹名为 `Replays5v5`。首次打开时如提示无法验证开发者，可核对下载来源后按 Apple 的步骤单独允许此应用。这一版尚未经过 Apple 公证。[Apple 官方说明](https://support.apple.com/zh-cn/102445)

## 当前版本的三个入口

- **战术板**：左侧分别选择进攻战术和战术分支，点播放观看示范。分支表示这一轮要观察的防守反应。按右下方按钮以推荐角色开始，也可点“换角色”选择其他球员。
- **即时演练**：点「开始演练」即可进入球场。系统分配角色并抽取回合，练习者根据场上的跑位、球权和防守自行决定如何处理球。
- **即时回放**：查看每一轮正常 3D 练习的实际经过。

### 即时演练怎么操作

WASD 跑位，鼠标转头。左上角的小地图始终显示双方站位、球和你的朝向。演练前段队友自行展开配合。需要你接应或带球到关键区域时，地面会出现薄荷绿圈，上方有浮动箭头。目标在视野外时看屏幕边缘的方向提示，也可以按 F 辅助跑位。距目标中心约一米就能到位；如果已经在合适的位置，配合直接衔接。

轮到你处理球后，可以带球离开原来的接应区，边移动边观察。将准星对准队友或篮筐按 Q。传给有空位的队友后，回合会继续。球权结束时会显示场上的实际结果，例如「传球被 X1 截断，本轮失败」。点「下一轮即时演练」重新抽题，或到「即时回放」查看刚才的回合。Esc 可暂停，V 可提前结束并查看回放。

每轮从九个教学分支中等概率抽取一个，再独立按 50% 概率采用原防守或变体，因此允许重复抽到同一题。教学模式保留预设的传球和跑位路线。

研究者可在本轮回放 JSON 的末帧查看 `drill`。其中 `variant` 记录防守条件，`chosenTarget` 为实际选择（0–4 对应 01–05，−1 表示投篮，−2 表示尚未出手），`decisionTime` 是从获得决定权到出手的场上秒数。`decisionCorrect` 记录出手时是否抓住首选机会，`reasonableChoice` 包含其他可行选项。`received`、`interceptedBy`、`nextPassFrom`、`nextPassTo` 与 `outcome` 记录实际后续。截球人 5–9 对应 X1–X5。普通教学回放不参与这组判定。

角色是你代入的进攻球员，球权转移不会改变这个身份。五名进攻编号 01–05，五名防守编号 X1–X5。三套教学都由 01 开球。X1、X2 在上线，X3、X4 分守左、右侧下线，X5 居中护筐。战术板和第一人称使用同一份十人逐拍站位。

## 共用的战术板操作
播放/暂停、倍速、时间轴和前后节点均在球场下方。点击时间轴上方的菱形标记可跳至一个战术阶段或事件；“更多”里有起点、单步和选人后的额外操作。右侧显示这一拍的进攻目标，在回放和暂停布防中也可查看防守反应。短暂悬停球员可在球场内查看他的当前职责。

教学示范随本次战术实际结束。最后出现机会的人出手，也有持球人直接投篮的分支。球进后结束，传球被截断会显示中断原因。即时回放按该次实际操作时间播放。

常规演练和回放只显示球员最近 2.5 秒的轨迹，越旧越淡、越细。进入暂停布防后，亮圈标出刚刚自动补位的防守人，约 1.5 秒后消失；这个画面不显示原回放的拖尾。球在空中时有传球参考线，掩护形成时有掩护标志。

## 暂停帧内布防
1. 在演练或某轮真实回放的任意时刻暂停。
2. 拖动任一进攻或防守球员，可连续调整多人。
3. 被拖球员跟随鼠标。其他防守人按规则平滑移动到补位点，再次调整时会从当前画面位置转向。比赛时间、战术阶段和其他进攻球员保持在原帧。
4. 点击“撤销”回到上一次摆位；“恢复原站位”清除摆位，回到原来的这一帧；“恢复并播放”从原记录继续。

场上会标出“暂停布防”。你拖动的人自动固定在新位置，继续拖其他人时，之前摆好的人也保持不动。选中已固定的防守人后可以点“允许自动补防”。防守根据持球压迫、护筐、邻近接应点等职责分配，优先保护球侧和篮下。固定过多人可能留下弱侧空缺，悬停防守人可查看调整后的职责。

原教学演练和真实回放不被改写。成功、失误或传球飞行中的帧也可以摆位，原阶段和空中球进度保留；它不会偷偷重新开始一轮。选中进攻球员后仍可手动“设为持球者”，这是你明确改变球权的假设。

暂停布防时，“更多”里的“原片段单步”也会恢复原记录再前进；操作时间轴或前后节点同样回到原记录。这组工具用于推敲同一帧的站位，布防由教学规则计算。

## 选择历史练习

左侧按今天、昨天和更早的日期分组。进球的卡片写明你控制谁、谁投进，例如“我控 01 · 05 命中”。其他回合显示协作结果。下方是开始时间和时长。绿色卡片标出当前选择。可以滚轮浏览，或点选后用上下方向键切换。离开再返回即时回放时，仍打开上次选中的回合。

换电脑时，可将旧电脑 `Replays5v5` 中自己的五人制回放 JSON 文件复制到新电脑上的同名文件夹，位置见上面的启动说明。已有同名文件可跳过。早期回放保留当时的结束方式。

## 正常 3D 练习
选择「以角色开始练习」进入当前分支。三套战术来自 CourtLab「2–3 联防三种破法」。下面的箭头代表实际传球，表中最后拿球的人完成本轮投篮。

| 战术与推荐角色 | 防守反应 | 球怎么走 |
| --- | --- | --- |
| 中锋提中 · 05 | X5 守筐 | 01 → 02 → 05，05 在高位投 |
| 中锋提中 · 05 | X5 上提 | 01 → 02 → 05 → 04，04 在 X5 身后接球 |
| 中锋提中 · 05 | 下线收缩 | 01 → 02 → 05 → 03，03 在弱侧底角接球 |
| 后卫发起 · 01 | 下线协防 | 01 借 05 掩护进入空隙，传给底角 02 |
| 后卫发起 · 01 | 中路协防 | 01 借掩护后传给短顺下的 05 |
| 后卫发起 · 01 | 守角沉退 | 01 自己带球进入空隙，停下投篮 |
| 射手战术 · 01 | 掩护延误 | 01 → 02 → 01，01 从篮下借 05 的掩护绕到底角 |
| 射手战术 · 01 | 提前封角 | 01 → 02 → 05，05 掩护后接球 |
| 射手战术 · 01 | 底角夹击 | 01 → 02 → 01 → 02 → 03，底角回传后转移至弱侧 |

2D 球员身旁的橙色篮球与「持球」标签标出当前持球人，角色外圈标出你控制的人。传球时跟着空中的篮球看接球点。进入 3D 后，准星下方会显示「你持球」「无球 · 某号持球」「传球中」或「投篮中」。画面下方会随你选的角色提示跑位和出球时机。

用 WASD 跑向高亮圈，或按 F 辅助跑到本拍位置。进入练习后，准星固定在屏幕中央。移动鼠标就能转头、抬头或低头。持球时，把准星对准队友，看到“已瞄准”及队友号码后按 Q 传球。准星对准篮筐时按 Q 即可投篮。瞄空时按 Q 会继续持球。每次投篮都命中，球入网后本轮自动保存并打开即时回放。最后的机会由 AI 拿到时会自动出手，由玩家拿到时会等待玩家瞄筐按 Q。传球仍可能被截断。

左上角的小地图同样常驻，篮筐方向固定。转头时，自己的朝向标记会随视角转动。教学中的普通跑位到过即可，随后可以调整站位。Esc 暂停并释放鼠标，点“继续暂停的练习”后恢复鼠标转视角。C 在当前站位重新组织并由 01 开球，R 从预设重开，V 保存本轮并查看实际回放。

教学中，队友接到传错的球时，画面会提示这一拍该由谁接应，队友会把球回传。传球被截时，提示会指出哪名防守人切断了哪条线路，按 C 即可重新组织。单轮练习最长 5 分钟，可按 V 结束并查看回放。

## 记录与旧版本
程序会读取目录里所有有效的实际练习记录。保存失败时会提示并提供重试。无法读取的记录会跳过并显示数量。

旧的 Windows_Learning、Windows_LearningPolish、Windows_Readability 等程序及其 Replays 文件保持原样。五人制使用新记录格式，旧 3v3 回放继续在旧程序中查看。

本版包含正常第一人称桌面练习；未接入头显控制器或 3D What-if，也没有完整比赛计分、篮板与犯规裁判。

## 用 Unity 开发

项目使用 **Unity 中国版编辑器 2022.3.62f3c1**，修订号 `1623fc0bbb97`，由 [ProjectVersion.txt](../Unity/ProjectSettings/ProjectVersion.txt) 指定。

1. Windows 上双击仓库里的 **`Tools/Open-Unity.cmd`**。装有对应编辑器时，它会直接打开五人制场景。若只装了 Unity Hub，脚本会唤起这个版本的安装界面。首次安装 Hub 时，脚本会打开官方存档，请到「历史 Unity 版本」区域下载 **Unity Hub**。安装后登录、按 Hub 提示激活许可，再运行脚本。[Windows 版 Unity Hub 官方下载](https://public-cdn.cloud.unitychina.cn/hub/prod/UnityHubSetup.exe)
2. 手动安装时，在官网「历史 Unity 版本」的 **2022.3.62f3c1** 这一行选择「从 Unity Hub 下载」。该页顶部另列有「团结引擎」版本，当前工程使用的是页面下方带 `f3c1` 的 Unity 编辑器。若浏览器未能唤起 Hub，可直接下载对应的 Windows 编辑器，安装后在 Unity Hub 的 **Installs → Locate** 选中其 `Editor/Unity.exe`。[Unity 中国官方版本存档](https://unity.cn/releases/full/2022) · [该修订版的 Windows 64 位编辑器](https://download.unitychina.cn/download_unity/1623fc0bbb97/Windows64EditorInstaller/UnitySetup64.exe)
3. 安装完成后，再次双击 `Tools/Open-Unity.cmd`。也可以在 **Unity Hub → Projects → Add** 选中仓库内包含 `Assets` 的 **`Unity` 子目录**，在编辑器的 Project 窗口双击 `Assets/CourtX/Scenes/FivePlayerTraining.unity`。首次导入资源后，按顶部 ▶ 进入战术板。

### 构建与检查

在编辑器里选择 **CourtX → Build Windows Player**，或关闭已打开的此项目，从仓库根目录运行以下 PowerShell 命令。编译入口 `FivePlayerAuthoring.BuildPortable` 直接使用已提交的五人制场景。构建完成后，打开 `Unity/Builds/Windows/CourtX.exe`。命令行日志在 `Unity/Logs/CourtX-build.log`。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Open-Unity.ps1 -Build
```

若编辑器安装在自定义位置且尚未被 Hub 登记，请在命令末尾加 `-EditorPath "D:\Unity\Editor\Unity.exe"`，把示例路径替换为本机实际路径。下面的规则检查会核对三个战术的全部九种防守分支和五个可选角色。检查记录写入 `Preview/Playtests/FivePlayer/coach-checks.txt`。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_five_review.ps1 -Method FivePlayerReview.RunCoach -Label coach
```

**发布 macOS 或 Android 版本**

- macOS 使用 `FivePlayerTraining.unity` 构建包含 Intel 和 Apple silicon 的 `CourtX.app`。仓库根目录下运行 `python Tools/package_macos.py <CourtX.app路径> CourtX-macOS-Universal.zip --version 0.7.0`，打包脚本会保留执行权限并核对架构、资源和版本号。
- Android 需要编辑器的 Android Build Support、SDK、NDK 和 OpenJDK。发布者在本机环境设置 `COURTX_ANDROID_KEYSTORE`、`COURTX_ANDROID_KEY_ALIAS`、`COURTX_ANDROID_STORE_PASSWORD`、`COURTX_ANDROID_KEY_PASSWORD`，再用 Unity 参数 `-batchmode -quit -buildTarget Android -projectPath Unity -executeMethod FiveAndroidBuild.Build` 构建已签名的 ARM64 APK。默认输出到 `Unity/Builds/Android/CourtX-Android-arm64.apk`。
