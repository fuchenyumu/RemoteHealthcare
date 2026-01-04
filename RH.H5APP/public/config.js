/**
 * 远程会诊评价系统 - H5 App 配置文件
 *
 * 此文件在打包后可以直接修改，无需重新构建
 * 部署时请根据实际情况修改以下配置
 */

window.__APP_CONFIG__ = {
  // ==================== 基础配置 ====================

  // API 基础地址
  // 示例：https://api.yourdomain.com 或 http://192.168.1.113:8828
  // 注意：如果使用 IIS 反向代理，使用前端域名 + /api 路径
  apiBaseUrl: 'https://192.168.1.113:8828',

  // Web 前端地址
  // 示例：https://www.yourdomain.com 或 http://192.168.1.113:8848
  webBaseUrl: 'https://192.168.1.113:8848',

  // 是否启用 HTTPS
  httpsEnabled: true,

  // API 端口（仅用于标识）
  apiPort: 8828,

  // Web 端口（仅用于标识）
  webPort: 8848,

  // ==================== API 配置 ====================

  // API 超时时间（毫秒）
  apiTimeout: 10000,

  // API 版本
  apiVersion: 'v1',

  // API 路径前缀
  apiPathPrefix: '/api',

  // ==================== SSL 证书配置 ====================

  // SSL 证书指纹（用于验证自签名证书）
  // 获取证书指纹的方法：
  // 1. 在浏览器中访问 https://192.168.1.113:8828
  // 2. 点击地址栏的锁图标 → 查看证书
  // 3. 在"详细信息"标签中找到"指纹"
  // 4. 将指纹（去除空格和冒号）填入下方
  certificateFingerprint: '',

  // 证书颁发者（用于验证）
  // 示例：CN=192.168.1.113 或 CN=*.yourdomain.com
  certificateIssuer: 'CN=192.168.1.113',

  // 是否在开发环境忽略证书错误
  // 生产环境建议设为 false
  ignoreCertificateErrors: true,

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

 场景 1: 开发环境（本地测试）
 apiBaseUrl: 'http://localhost:8828'
 webBaseUrl: 'http://localhost:8848'
 httpsEnabled: false

 场景 2: 生产环境 - IIS 反向代理（推荐）✅
 apiBaseUrl: 'https://www.example.com'
 webBaseUrl: 'https://www.example.com'
 httpsEnabled: true
 - 前端: https://www.example.com/h5app (IIS 443)
 - 后端内部: http://localhost:8808 (不对外暴露)
 - IIS 自动转发 /api/* 到后端

 场景 3: 生产环境 - 独立域名
 apiBaseUrl: 'https://api.example.com:8828'
 webBaseUrl: 'https://www.example.com'
 httpsEnabled: true

 场景 4: 内网 IP 部署（测试/内网）
 apiBaseUrl: 'http://192.168.1.113:8828'
 webBaseUrl: 'http://192.168.1.113:8848'
 httpsEnabled: false

 ==================== 调试建议 ====================

 1. 检查配置是否生效：
    打开浏览器控制台，输入：window.__APP_CONFIG__

 2. 检查 API 请求：
    浏览器控制台 → Network 标签 → 筛选 XHR

 3. 检查证书错误：
    浏览器控制台 → Console 标签 → 查找 CERT 相关错误

 4. 修改配置后：
    - 清除浏览器缓存（Ctrl + Shift + Delete）
    - 强制刷新页面（Ctrl + Shift + R）
    - 或关闭浏览器标签页重新打开
*/
