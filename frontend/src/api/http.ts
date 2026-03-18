import axios, { type AxiosInstance, type AxiosRequestConfig } from 'axios'

// 后端API基础地址（本地.NET WebAPI）
const BASE_URL = 'http://localhost:5000'

/**
 * 自定义 axios 实例接口：
 * 拦截器已将 AxiosResponse<T> 解包为 T，
 * 这里覆盖方法签名让 TypeScript 知道返回值直接是 T 而非 AxiosResponse<T>
 */
interface Http extends AxiosInstance {
  get<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T>
  post<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>
  put<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>
  delete<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T>
  patch<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>
}

const http: Http = axios.create({
  baseURL: BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
}) as Http

// 响应拦截器，统一处理错误
http.interceptors.response.use(
  res => res.data,
  err => {
    const msg = err?.response?.data?.message || err.message || '请求失败'
    return Promise.reject(new Error(msg))
  }
)

/**
 * 下载二进制文件（Excel 等），自动从 Content-Disposition 读取文件名并触发浏览器下载
 * @param url 相对路径（如 /api/menus/1/data/template）
 * @param params 查询参数
 * @param fallbackName 若响应头无文件名时的备用名称
 */
export async function downloadFile(
  url: string,
  params?: Record<string, unknown>,
  fallbackName = 'download.xlsx'
): Promise<void> {
  const response = await axios.get(`${BASE_URL}${url}`, {
    responseType: 'blob',
    params,
    timeout: 60000
  })

  // 从 Content-Disposition 解析文件名
  const cd = response.headers['content-disposition'] || ''
  let filename = fallbackName
  const utf8Match = cd.match(/filename\*=UTF-8''(.+)/i)
  const asciiMatch = cd.match(/filename="?([^";\r\n]+)"?/i)
  if (utf8Match) filename = decodeURIComponent(utf8Match[1])
  else if (asciiMatch) filename = asciiMatch[1]

  const blobUrl = URL.createObjectURL(response.data as Blob)
  const a = document.createElement('a')
  a.href = blobUrl
  a.download = filename
  a.click()
  URL.revokeObjectURL(blobUrl)
}

export default http

