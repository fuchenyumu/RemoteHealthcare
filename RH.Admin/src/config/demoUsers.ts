export type DemoUserRole = "HOST" | "EXPERT" | "OBSERVER";

export interface DemoUser {
  key: string;
  label: string;
  account: string;
  password: string;
  intendedRole: DemoUserRole;
}

/**
 * 演示账号配置（用于“功能流程演示”）
 *
 * 注意：
 * - 这里存放的是演示环境账号，生产环境请移除或通过后端/环境变量注入
 * - 默认密码为 123456（后端登录会进行 MD5 校验）
 */
export const demoUsers: DemoUser[] = [
  {
    key: "demo-host",
    label: "主持人",
    account: "demo_host",
    password: "123456",
    intendedRole: "HOST"
  },
  {
    key: "demo-expert",
    label: "专家",
    account: "demo_expert",
    password: "123456",
    intendedRole: "EXPERT"
  },
  {
    key: "demo-observer",
    label: "观察员",
    account: "demo_observer",
    password: "123456",
    intendedRole: "OBSERVER"
  }
];
