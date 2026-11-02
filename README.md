# 事项簿
中文事项跟踪应用：一行一个事项，按列查看、筛选和编辑。数据保存在本地文档中的 JSON 文件，不使用浏览器 localStorage，也不上传到网络。

## Windows 使用（无需安装 Node.js 或 Go）
下载 `release/Shixiang-Tracker-windows-x64.exe`，双击运行。程序启动本机数据服务并自动在默认浏览器中打开界面。运行期间保留命令行窗口，页面显示保存成功后，关闭该窗口即可退出。若浏览器未自动打开，在浏览器中访问程序窗口显示的地址。

这是 Windows 64 位便携程序，不需要安装；界面通过浏览器显示，数据由 EXE 写入文件。未进行代码签名，Windows 可能显示未知发布者提示。

### 数据文件
- 默认路径：Windows 的“文档”文件夹下 `事项簿/tasks.json`（支持重定向到其他磁盘的文档目录）。页面底部和程序窗口会显示实际路径。
- 每次保存前保留上一版 `tasks.json.bak`，写入采用临时文件替换。请定期把整个 `事项簿` 文件夹备份到其他位置；上一版备份不替代长期备份。
- 更换浏览器、清除浏览器缓存不会删除事项。不要在应用运行期间手动编辑 JSON。恢复备份时先退出程序，将备份复制为 `tasks.json`，再启动。
- 支持环境变量 `TASK_TRACKER_DATA_DIR` 指定存储文件夹。多个窗口同时修改时，过期保存会被拒绝，以避免覆盖其他窗口数据；先复制未保存内容再刷新。
- 旧版浏览器存储数据不会自动迁移；当前版本的 Excel 导出不是导入恢复格式，完整备份请复制 JSON 文件。
- 数据服务只监听本机 `127.0.0.1:17643`，不对局域网开放。端口冲突时会报错，不会改用新的端口。

## 功能
- 点击新增直接在表格录入，默认参与人为王淞晖，默认状态进行中。
- 点击行选中；双击非输入区域或点击行末箭头，打开竖向编辑详情。
- 当前进展支持 Markdown 实时预览，可全部展开、选中展开或全部折叠；折叠模式下在详情中编辑完整进展。
- 表头逐列筛选，日期为包含起止日的范围；参与人用顿号、中文或英文逗号分隔，筛选某个人时仍显示完整名单。
- 新增文本、数字或日期字段，显示列面板控制可见字段。
- Excel 导出当前筛选结果和所有字段（包含隐藏列），保留完整参与人及原始 Markdown。
- 完成时自动记录实际完成时间，改回其他状态会清空该时间，再次完成记录新的时间。

## 开发和构建
需要 Node.js 20.19+ 或 22.12+、Go 1.23+。使用现有检出目录，不需要新建 Git worktree。

```sh
npm ci
npm test
npm run build
mkdir -p desktop/web
cp -R dist/. desktop/web/
cd desktop
go test -v ./...
go run .
```

Go 启动的服务直接提供构建后的页面。开发热更新可在保持 Go 服务运行的同时，在仓库根目录运行 `npm run dev -- --port 5173`；Vite 会将数据请求代理到本机 Go 服务。

在 Linux/macOS 中构建 Windows EXE：
```sh
./desktop/build.sh
```
输出到 `release/Shixiang-Tracker-windows-x64.exe`。前端文件嵌入 EXE，无需额外资源文件。Windows 可先构建前端并将 `dist` 内容复制到 `desktop/web`，再在 `desktop` 中执行 `go build -trimpath -ldflags="-s -w" -o ../release/Shixiang-Tracker-windows-x64.exe .`。

测试时可设置 `TASK_TRACKER_DATA_DIR` 到临时目录、`TASK_TRACKER_NO_BROWSER=1` 禁止自动打开浏览器。
