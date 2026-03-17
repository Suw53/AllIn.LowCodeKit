import { ElLoading } from 'element-plus'

/**
 * 执行异步操作时显示全屏遮罩 loading，操作完成后自动关闭。
 * 错误向上抛出，由调用方处理。
 * @param fn   需要执行的异步函数
 * @param text 遮罩文字，默认"处理中…"
 */
export async function withLoading<T>(
  fn: () => Promise<T>,
  text = '处理中…'
): Promise<T> {
  const loading = ElLoading.service({
    fullscreen: true,
    text,
    background: 'rgba(0, 0, 0, 0.45)'
  })
  try {
    return await fn()
  } finally {
    loading.close()
  }
}
