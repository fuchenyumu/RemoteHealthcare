/**
 * SSL 证书配置辅助工具
 */

/**
 * 获取当前证书配置
 */
export const getCertificateConfig = () => {
  return window.__APP_CONFIG__ || {
    certificateFingerprint: '',
    certificateIssuer: '',
    ignoreCertificateErrors: true
  };
};

/**
 * 验证证书指纹
 * @param fingerprint 证书指纹（SHA-256，不含空格和冒号）
 * @returns 是否匹配
 */
export const verifyCertificateFingerprint = (fingerprint: string): boolean => {
  const config = getCertificateConfig();
  if (!config.certificateFingerprint) {
    return true; // 未配置指纹，跳过验证
  }

  // 标准化指纹格式（去除分隔符并转大写）
  const normalize = (str: string) => str.replace(/[:\s-]/g, '').toUpperCase();
  return normalize(fingerprint) === normalize(config.certificateFingerprint);
};

/**
 * 验证证书颁发者
 * @param issuer 证书颁发者
 * @returns 是否匹配
 */
export const verifyCertificateIssuer = (issuer: string): boolean => {
  const config = getCertificateConfig();
  if (!config.certificateIssuer) {
    return true; // 未配置颁发者，跳过验证
  }

  return issuer.includes(config.certificateIssuer);
};

/**
 * 是否应该忽略证书错误
 */
export const shouldIgnoreCertificateErrors = (): boolean => {
  const config = getCertificateConfig();
  return config.ignoreCertificateErrors || false;
};

/**
 * 获取证书配置信息（用于调试）
 */
export const getCertificateInfo = () => {
  const config = getCertificateConfig();

  return {
    hasFingerprint: !!config.certificateFingerprint,
    hasIssuer: !!config.certificateIssuer,
    ignoreErrors: config.ignoreCertificateErrors,
    isConfigured: !!(config.certificateFingerprint || config.certificateIssuer)
  };
};
