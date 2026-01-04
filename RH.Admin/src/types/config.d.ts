/**
 * 公共配置类型定义
 */
declare global {
  interface Window {
    __APP_CONFIG__?: {
      apiBaseUrl: string;
      webBaseUrl: string;
      httpsEnabled: boolean;
      apiPort: number;
      webPort: number;
      websocketUrl: string;
      sfuBaseUrl: string;
      certificateFingerprint: string;
      certificateIssuer: string;
      ignoreCertificateErrors: boolean;
    };
  }
}

export {};
