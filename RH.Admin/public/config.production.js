/**
 * 生产环境配置文件 - IIS 反向代理模式
 *
 * 部署架构：
 * 前端：https://www.example.com (IIS 443端口)
 * 后端：http://localhost:8808 (内部HTTP端口，通过IIS反向代理访问)
 *
 * 使用说明：
 * 1. 构建后直接修改 dist/config.js 文件
 * 2. 修改后刷新页面即可生效，无需重新构建
 */

window.__APP_CONFIG__ = {
  // ==================== 基础配置 ====================

  // API 基础地址
  // ⚠️ IIS 反向代理模式：使用前端域名，/api 路径会被 IIS 转发到后端
  apiBaseUrl: 'https://www.example.com',

  // Web 前端地址
  webBaseUrl: 'https://www.example.com',

  // 是否启用 HTTPS
  httpsEnabled: true,

  // API 端口（仅用于标识，实际由 IIS 处理）
  apiPort: 443,

  // Web 端口
  webPort: 443,

  // ==================== WebSocket 配置 ====================

  // WebSocket URL (用于 SignalR 和 SFU 服务器)
  // ⚠️ IIS 反向代理模式：使用 WSS 协议，通过前端域名访问
  websocketUrl: 'wss://www.example.com/signalr-hubs/medical',

  // SFU 服务器地址（独立部署，不经过 IIS）
  // 如果 SFU 也使用 IIS 代理，改为前端域名
  sfuBaseUrl: 'https://sfu.example.com:8000',

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
  ignoreCertificateErrors: false
};

/*
 ==================== 部署场景示例 ====================

 场景 1: 开发环境（本地测试）
 apiBaseUrl: 'https://localhost:8848'
 webBaseUrl: 'https://localhost:8848'
 websocketUrl: 'wss://localhost:8848/signalr-hubs/medical'

 场景 2: 生产环境 - IIS 反向代理（推荐）✅
 apiBaseUrl: 'https://www.example.com'
 webBaseUrl: 'https://www.example.com'
 websocketUrl: 'wss://www.example.com/signalr-hubs/medical'
 - 前端: https://www.example.com (IIS 443)
 - 后端内部: http://localhost:8808 (不对外暴露)
 - IIS 自动转发 /api/* 和 /signalr-hubs/* 到后端

 场景 3: 生产环境 - 前后端独立域名
 apiBaseUrl: 'https://api.example.com:8828'
 webBaseUrl: 'https://www.example.com'
 websocketUrl: 'wss://api.example.com:8828/signalr-hubs/medical'
 - 需要 CORS 配置
 - 需要两个 SSL 证书

 场景 4: 内网 IP 部署（测试/内网）
 apiBaseUrl: 'https://192.168.1.113'
 webBaseUrl: 'https://192.168.1.113'
 websocketUrl: 'wss://192.168.1.113/signalr-hubs/medical'

 ==================== 证书指纹获取方法 ====================

 方法 1: 浏览器（推荐）
 1. 访问 https://www.example.com
 2. 点击地址栏的锁图标
 3. "连接是安全的" → "证书有效"
 4. "详细信息" → "指纹"
 5. 复制指纹值（去除空格和冒号）

 方法 2: OpenSSL 命令
 openssl s_client -connect www.example.com:443 -servername www.example.com 2>/dev/null | openssl x509 -fingerprint -sha256 -noout

 方法 3: PowerShell
 $cert = [System.Net.ServicePointManager]::GetServerCertificate("www.example.com", 443)
 $cert.GetCertHashString("SHA256")

 ==================== 调试建议 ====================

 1. 检查配置是否生效：
    打开浏览器控制台，输入：window.__APP_CONFIG__

 2. 检查 API 请求：
    浏览器控制台 → Network 标签 → 筛选 XHR/WS

 3. 检查 WebSocket 连接：
    浏览器控制台 → Network 标签 → 筛选 WS

 4. 检查证书错误：
    浏览器控制台 → Console 标签 → 查找 CERT 相关错误
*/
