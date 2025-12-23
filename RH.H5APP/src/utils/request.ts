import axios from 'axios'
import { showToast } from 'vant'

const service = axios.create({
  baseURL: '/api/v1',
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

