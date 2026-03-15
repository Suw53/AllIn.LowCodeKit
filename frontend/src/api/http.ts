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

export default http
