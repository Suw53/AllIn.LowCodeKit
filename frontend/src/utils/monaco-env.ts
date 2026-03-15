// Monaco Editor Web Worker 环境配置
// 必须在 monaco-editor 导入前执行，配置 Vite 打包的 worker 文件
import editorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker'

self.MonacoEnvironment = {
  // C# 没有专属 language worker，统一使用基础 editorWorker
  getWorker() {
    return new editorWorker()
  }
}
