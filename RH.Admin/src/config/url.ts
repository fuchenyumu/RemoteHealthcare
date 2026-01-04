/**
 * URL配置中心
 * 统一管理所有URL和端口配置,避免硬编码
 */

// 环境变量
const env = import.meta.env;

// 基础URL配置
export const urlConfig = {
  // API基础URL
  apiBaseUrl: env.VITE_BASE_URL || 'https://192.168.1.113:8828',

  // Web前端URL
  webBaseUrl: env.VITE_WEB_BASE_URL || 'https://192.168.1.113:8848',

  // API端口
  apiPort: env.VITE_API_PORT || '8828',

  // Web端口
  webPort: env.VITE_PORT || '8848',

  // 是否启用HTTPS
  httpsEnabled: env.VITE_HTTPS_ENABLED === 'true'
} as const;

// WebSocket配置
export const wsConfig = {
  // SignalR Hub基础路径
  signalrHubPath: '/signalr-hubs',

  // 是否使用WSS (WebSocket Secure)
  useWss: urlConfig.httpsEnabled
} as const;

// 导出便捷方法
/**
 * 获取完整的API URL
 * @param path API路径,如 'users/login'
 * @returns 完整URL,如 'https://192.168.1.113:8828/api/v1/users/login'
 */
export const getApiUrl = (path: string = ''): string => {
  const baseUrl = urlConfig.apiBaseUrl.replace(/\/$/, '');
  const apiPath = path.replace(/^\//, '');
  return `${baseUrl}/api/v1/${apiPath}`;
};

/**
 * 获取SignalR Hub URL
 * @param hub Hub名称或路径,如 '/medical'
 * @returns Hub路径,如 '/signalr-hubs/medical'
 */
export const getSignalrUrl = (hub: string = ''): string => {
  return `${wsConfig.signalrHubPath}${hub}`;
};

/**
 * 获取WebSocket URL (用于非SignalR的WebSocket连接)
 * @param path WebSocket路径
 * @returns 完整WebSocket URL
 */
export const getWebSocketUrl = (path: string): string => {
  const baseUrl = urlConfig.apiBaseUrl;
  const wsProtocol = baseUrl.startsWith('https') ? 'wss://' : 'ws://';
  const url = baseUrl.replace(/^https?:\/\//, '');
  return `${wsProtocol}${url}${path}`;
};

/**
 * 检查当前是否在HTTPS环境
 */
export const isHttps = (): boolean => {
  return urlConfig.httpsEnabled || window.location.protocol === 'https:';
};

/**
 * 获取当前协议 (http:// 或 https://)
 */
export const getProtocol = (): string => {
  return isHttps() ? 'https://' : 'http://';
};

// 默认导出
export default urlConfig;
