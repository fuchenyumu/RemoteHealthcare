import axios from 'axios'
import { showToast } from 'vant'

/**
 * 获取 API 基础地址（优先使用 public/config.js）
 */
const getServiceBaseUrl = () => {
  if (window.__APP_CONFIG__?.apiBaseUrl) {
    return window.__APP_CONFIG__.apiBaseUrl
  }
  // 开发环境回退到相对路径（使用 Vite 代理）
  return ''
}

/**
 * 创建 Axios 实例
 */
const service = axios.create({
  baseURL: getServiceBaseUrl(),
  timeout: 10000
})

service.interceptors.response.use(
  response => {
    return response.data
  },
  error => {
    showToast(error.message || '请求失败')
    return Promise.reject(error)
  }
)

export default service

