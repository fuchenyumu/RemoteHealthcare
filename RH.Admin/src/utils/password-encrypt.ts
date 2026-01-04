import MD5 from "crypto-js/md5";

/**
 * 密码加密配置
 * 注意：此盐值必须与后端配置保持一致
 */
const PASSWORD_SALT = "YinZhiXin@2025#RemoteHealthcare";

/**
 * 加密密码
 *
 * 加密流程（与后端保持一致）：
 * 1. 明文密码 → MD5 → 得到第一次加密结果
 * 2. 第一次加密结果 + 盐值 → MD5 → 得到最终加密结果
 *
 * 后端验证逻辑：
 * 1. 从数据库获取存储的密码（已做一次 MD5）
 * 2. 数据库密码 + 盐值 → MD5
 * 3. 与前端传来的密码比对
 *
 * @param plainPassword 明文密码
 * @returns MD5加密后的密码（小写）
 */
export function encryptPassword(plainPassword: string): string {
  if (!plainPassword) {
    return "";
  }

  // 第一步：明文密码 → MD5（模拟数据库存储的密码）
  const firstMd5 = MD5(plainPassword).toString();

  // 第二步：第一次 MD5 结果 + 盐值 → MD5（与后端验证逻辑一致）
  return MD5(firstMd5 + PASSWORD_SALT).toString();
}

/**
 * 验证密码强度
 * @param password 密码
 * @returns 强度等级：0-弱，1-中，2-强
 */
export function checkPasswordStrength(password: string): number {
  if (!password || password.length < 6) return 0;

  let strength = 0;

  // 包含数字
  if (/\d/.test(password)) strength++;

  // 包含小写字母
  if (/[a-z]/.test(password)) strength++;

  // 包含大写字母
  if (/[A-Z]/.test(password)) strength++;

  // 包含特殊字符
  if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) strength++;

  // 长度大于10
  if (password.length >= 10) strength++;

  // 转换为3级强度
  if (strength <= 2) return 0; // 弱
  if (strength <= 3) return 1; // 中
  return 2; // 强
}

/**
 * 获取密码强度描述
 * @param password 密码
 * @returns 强度描述文本
 */
export function getPasswordStrengthText(password: string): string {
  const strength = checkPasswordStrength(password);
  const texts = ["弱", "中等", "强"];
  return texts[strength] || "弱";
}
