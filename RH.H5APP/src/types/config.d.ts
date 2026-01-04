/**
 * 运行时配置类型定义
 *
 * 用于 public/config.js 的 TypeScript 类型检查
 */

declare global {
  interface Window {
    /**
     * 应用运行时配置
     * 在 public/config.js 中定义
     */
    __APP_CONFIG__?: AppConfig;
  }

  /**
   * 应用配置接口
   */
  interface AppConfig {
    // ==================== 基础配置 ====================

    /** API 基础地址 */
    apiBaseUrl: string;

    /** Web 前端地址 */
    webBaseUrl: string;

    /** 是否启用 HTTPS */
    httpsEnabled: boolean;

    /** API 端口 */
    apiPort: number;

    /** Web 端口 */
    webPort: number;

    // ==================== API 配置 ====================

    /** API 超时时间（毫秒） */
    apiTimeout: number;

    /** API 版本 */
    apiVersion: string;

    /** API 路径前缀 */
    apiPathPrefix: string;

    // ==================== SSL 证书配置 ====================

    /** SSL 证书指纹 */
    certificateFingerprint: string;

    /** 证书颁发者 */
    certificateIssuer: string;

    /** 是否忽略证书错误 */
    ignoreCertificateErrors: boolean;

    // ==================== 应用配置 ====================

    /** 应用标题 */
    appTitle: string;

    /** 应用版本 */
    appVersion: string;

    /** 是否启用调试模式 */
    debugMode: boolean;

    // ==================== 业务配置 ====================

    /** 评价类型配置 */
    evaluationTypes: {
      /** 评价选项 */
      options: Array<{
        label: string;
        value: number;
      }>;

      /** 是否必填评价内容 */
      requireContent: boolean;

      /** 评价内容最小长度 */
      contentMinLength: number;

      /** 评价内容最大长度 */
      contentMaxLength: number;
    };

    // ==================== 第三方服务配置 ====================

    /** 微信配置 */
    wechat: {
      /** 是否启用 */
      enabled: boolean;
      /** App ID */
      appId: string;
      /** App Secret */
      appSecret: string;
    };

    /** 支付配置 */
    payment: {
      /** 是否启用 */
      enabled: boolean;
      /** 支付提供商 */
      provider: 'wechat' | 'alipay' | 'both' | '';
    };
  }
}

export {};
