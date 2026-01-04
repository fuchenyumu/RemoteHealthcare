/**
 * 生产环境配置文件 - H5 App
 *
 * 部署说明：
 * 1. 将此文件重命名为 config.js（或复制内容到 config.js）
 * 2. 修改为实际的生产环境域名和配置
 * 3. 部署后可直接修改，无需重新构建
 */

window.__APP_CONFIG__ = {
  // ==================== 基础配置 ====================

  // API 基础地址
  // ⚠️ IIS 反向代理模式：使用前端域名，/api 路径会被 IIS 转发到后端
  apiBaseUrl: 'https://www.example.com',

  // Web 前端地址
  webBaseUrl: 'https://www.example.com/h5app',

  // 是否启用 HTTPS
  httpsEnabled: true,

  // API 端口（仅用于标识）
  apiPort: 443,

  // Web 端口
  webPort: 443,

  // ==================== API 配置 ====================

  // API 超时时间（毫秒）
  apiTimeout: 15000,

  // API 版本
  apiVersion: 'v1',

  // API 路径前缀
  apiPathPrefix: '/api',

  // ==================== SSL 证书配置 ====================

  // SSL 证书指纹（用于验证自签名证书）
  // 生产环境使用 CA 证书（Let's Encrypt、DigiCert）时可以留空
  certificateFingerprint: '',

  // 证书颁发者（用于验证）
  // 生产环境 CA 证书示例：CN=DigiCert TLS RSA SHA256 2020 CA1
  // 自签名证书示例：CN=www.example.com
  certificateIssuer: '',

  // 是否忽略证书错误
  // ⚠️ 生产环境建议设为 false（使用有效证书）
  ignoreCertificateErrors: false,

  // ==================== 应用配置 ====================

  // 应用标题
  appTitle: '远程会诊评价',

  // 应用版本
  appVersion: '1.0.0',

  // 是否启用调试模式
  debugMode: false,

  // ==================== 业务配置 ====================

  // 评价类型配置
  evaluationTypes: {
    // 评价选项
    options: [
      { label: '非常满意', value: 5 },
      { label: '满意', value: 4 },
      { label: '一般', value: 3 },
      { label: '不满意', value: 2 },
      { label: '非常不满意', value: 1 }
    ],

    // 是否必填评价内容
    requireContent: true,

    // 评价内容最小长度
    contentMinLength: 10,

    // 评价内容最大长度
    contentMaxLength: 500
  },

  // ==================== 第三方服务配置 ====================

  // 微信配置（如果使用微信分享）
  wechat: {
    enabled: false,
    appId: '',
    appSecret: ''
  },

  // 支付配置（如果需要支付功能）
  payment: {
    enabled: false,
    provider: '' // 'wechat' | 'alipay' | 'both'
  }
};

/*
 ==================== 部署场景示例 ====================

 场景 1: IIS 反向代理（推荐）✅
 apiBaseUrl: 'https://www.example.com'
 webBaseUrl: 'https://www.example.com/h5app'
 - 前端: https://www.example.com/h5app (IIS 443)
 - 后端内部: http://localhost:8808 (不对外暴露)
 - IIS 自动转发 /api/* 到后端

 场景 2: 独立域名
 apiBaseUrl: 'https://api.example.com:8828'
 webBaseUrl: 'https://www.example.com/h5app'
 - 需要 CORS 配置
 - 需要两个 SSL 证书

 场景 3: 内网 IP 部署（测试/内网）
 apiBaseUrl: 'http://192.168.1.113:8828'
 webBaseUrl: 'http://192.168.1.113:8848/h5app'

 ==================== 调试建议 ====================

 1. 检查配置是否生效：
    打开浏览器控制台，输入：window.__APP_CONFIG__

 2. 检查 API 请求：
    浏览器控制台 → Network 标签 → 筛选 XHR

 3. 修改配置后：
    - 清除浏览器缓存
    - 强制刷新页面（Ctrl + Shift + R）
*/
