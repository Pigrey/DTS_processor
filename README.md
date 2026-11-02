# 事项簿 · Windows 原生桌面版

一行一个事项的本地事项跟踪软件。使用 **WPF 原生 Windows 窗口和控件**，无需浏览器、不启动网页或本地 HTTP 服务。Markdown 进展也由窗口内的原生富文本控件渲染。

## 下载和运行

下载 [Windows 64 位便携 EXE](https://github.com/Pigrey/DTS_processor/raw/refs/heads/main/release/Shixiang-Tracker-windows-x64.exe)，双击运行。关闭窗口即可退出，无需保留命令行窗口。

- 适用 Windows 10 / 11 x64；自带 .NET 运行时，无需安装 Node.js、Go 或 .NET。
- 未进行代码签名，Windows 可能显示未知发布者提示。
- 这是原生 Windows 程序；Linux 环境完成交叉编译和数据逻辑测试，原生窗口仍需在 Windows 上进行实际运行验收。

## 从旧版升级

**先完全退出旧版事项簿（包括之前的控制台窗口）**，再运行新版 EXE。旧版网页界面无需再使用。

新版直接读取原先的“文档/事项簿/tasks.json”，沿用原有事项、创建及完成时间、参与人、自定义字段和显示选项，无需手动导入。建议首次升级前复制备份整个 `事项簿` 文件夹。

## 功能

- 原生表格按行展示事项。新增后直接在表格编辑，默认进行中、参与人王淞晖，自动记录创建时间。
- 完成时记录实际完成时间，恢复进行中或挂起时清空完成时间。截止日期可留空。
- 双击事项或点击“事项详情”，以竖向表单查看、编辑或删除事项。
- 当前进展支持 Markdown 实时预览，可全部展开、选中展开、全部折叠。全部折叠模式通过详情编辑和查看完整进展。
- Markdown 支持标题、粗体、斜体、列表、引用、代码、链接文本、表格等；不下载远程图片、不执行 HTML。
- 每列有筛选控件；日期支持包含起止日的范围。参与人精确筛选某个人，仍保留显示该事项的全部参与人。
- 可新增文本、数字、日期字段，可选择显示列；隐藏不会删除数据。
- 导出当前筛选结果到 `.xlsx`，包含隐藏字段、全部参与人和原始 Markdown。字符串以文本存储，不会被当作 Excel 公式执行。

## 本地文件

- 数据保存在 Windows “文档”目录下的 `事项簿/tasks.json`，支持重定向的文档目录；主窗口底部显示实际路径。
- 输入后自动保存，关闭窗口前也会保存。每次保存采用临时文件替换，并在 `tasks.json.bak` 保留上一版本。
- 使用独占文件锁阻止多个新版程序同时修改同一文件。`tasks.json.lock` 是锁文件，无需手动删除。
- 更换浏览器、清除浏览器缓存不影响数据。不要在软件运行期间手动修改 JSON；恢复备份前先关闭软件，再将备份复制为 `tasks.json`。
- 建议定期复制整个数据文件夹到其他位置。Excel 为导出用途，完整恢复请使用 JSON 备份。
- 可用环境变量 `TASK_TRACKER_DATA_DIR` 指定其他数据文件夹。软件不会把事项上传到网络。

## 构建与测试

原生版源码在 `windows/`，需要 .NET 10 SDK。使用现有检出目录即可，不需要新建 Git worktree。

```sh
dotnet run --project windows/Tests/CoreTests.csproj -c Release
dotnet publish windows/Shixiang.csproj -c Release -r win-x64 -o release/native
```

也可以在 Linux/macOS 上运行 `./windows/build.sh`，会测试、交叉编译、更新 `release/Shixiang-Tracker-windows-x64.exe` 和 SHA256 校验文件。自包含单文件会在运行时解压所需原生运行库到用户临时目录，不需要管理员权限。

`src/` 与 `desktop/` 保留旧版网页/Go 实现供历史参考，当前下载的 EXE 使用 WPF 原生实现；不要用旧版 `desktop/build.sh` 构建当前桌面版。
