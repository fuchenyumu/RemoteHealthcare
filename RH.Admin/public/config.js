/**
 * 系统配置文件
 *
 * 此文件在打包后可以直接修改，无需重新构建
 * 部署时请根据实际情况修改以下配置
 */

window.__APP_CONFIG__ = {
  // API 基础地址
  // 示例：https://api.yourdomain.com:8828 或 http://192.168.1.113:8828
  apiBaseUrl: 'http://192.168.1.113:8828',

  // Web 前端地址
  // 示例：https://www.yourdomain.com 或 http://192.168.1.113:8848
  webBaseUrl: 'http://192.168.1.113:8848',

  // 是否启用 HTTPS
  httpsEnabled: false,

  // API 端口
  apiPort: 8828,

  // Web 端口
  webPort: 8848,

  // WebSocket URL (用于 SFU 服务器)
  // 示例：wss://192.168.1.113:8000/ws/medical 或 ws://192.168.1.113:8000/ws/medical
  websocketUrl: 'ws://192.168.1.113:8000/ws/medical',

  // SFU 服务器地址
  // 示例：https://192.168.1.113:8000 或 http://192.168.1.113:8000
  sfuBaseUrl: 'http://192.168.1.113:8000',

  // SSL 证书配置（用于自签名证书的指纹验证）
  // 获取证书指纹的方法：
  // 1. 在浏览器中访问 https://192.168.1.113:8828
  // 2. 点击地址栏的锁图标 → 查看证书
  // 3. 在"详细信息"标签中找到"指纹"
  // 4. 将指纹（去除空格）填入下方
  certificateFingerprint: '',

  // 证书颁发者（用于验证）
  // 示例：CN=192.168.1.113 或 CN=*.yourdomain.com
  certificateIssuer: 'CN=192.168.1.113',

  // 是否在开发环境忽略证书错误
  // 生产环境建议设为 false
  ignoreCertificateErrors: true
};
