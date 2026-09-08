# Unity 工程基线报告

## 编辑器

- 指定路径：`D:\Program Files\Editor\Unity.exe`
- 产品版本：Unity 2022.3.62f3c1
- Revision：`1623fc0bbb97`
- 当前许可证状态：未激活；批处理启动报告 `No valid Unity Editor license found`。

## 固定依赖

- Input System 1.12.0
- XR Interaction Toolkit 3.1.1
- XR Plug-in Management 4.5.1
- OpenXR Plugin 1.10.0
- PICO Unity Integration SDK 3.3.3：课程目录中尚未发现安装包，待提供本地 SDK 后导入。

## 课程资源

- 原始包：`..\..\Example\Unity\ganzhi.unitypackage`
- 离线导入状态：已按原始路径和 `.meta` 文件还原 855 个资源条目。
- 资源规模：`Assets` 下共 1710 个文件（包含资源及其 `.meta`）。
- 原始任务场景：`Assets/TirgamesAssets/Factory/Scenes/FactoryDay.unity`。
- 工作副本：`Assets/FireRescue/Scenes/FireRescue_Main.unity`，原始场景保持不变。
- 编辑器激活后仍需通过资源数据库重新导入，并验证 Inspector 引用。

## 首轮阻塞

1. Unity Licensing Client 能够启动，但没有 ULF 许可证和缓存登录令牌。
2. 因编辑器在加载项目前退出，尚未产生 Package Manager、脚本编译和场景加载日志。
3. 当前桌面控制接口未开放原生应用操作，Unity 登录和许可证激活需用户在界面完成。
4. 课程包包含 PICO 相关适配脚本和显示配置，但未包含 PICO Unity Integration SDK 3.3.3 本体。

## 激活后的首轮验证

1. 打开 `UnityFireRescue` 并等待 Package Manager 完成解析。
2. 确认所有固定版本包成功安装。
3. 重新通过 Unity 导入课程包，选择全部资源。
4. 打开课程原始场景，记录所有 Console 错误、缺失脚本和丢失引用。
5. 进入并退出一次 Play Mode，分别记录编译、XR、视频和网络状态。
