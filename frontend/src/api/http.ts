import axios from 'axios'

// 后端API基础地址（本地.NET WebAPI）
const BASE_URL = 'http://localhost:5000'

const http = axios.create({
  baseURL: BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

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

