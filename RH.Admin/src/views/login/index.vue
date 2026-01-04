<script setup lang="ts">
import Motion from "./utils/motion";
import { useRouter } from "vue-router";
import { message } from "@/utils/message";
import { loginRules } from "./utils/rule";
import { useNav } from "@/layout/hooks/useNav";
import type { FormInstance } from "element-plus";
import { useLayout } from "@/layout/hooks/useLayout";
import { bg, avatar, illustration } from "./utils/static";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { ref, reactive, toRaw, onMounted, onBeforeUnmount } from "vue";
import { useDataThemeChange } from "@/layout/hooks/useDataThemeChange";
import { addPathMatch } from "@/router/utils";
import { usePermissionStoreHook } from "@/store/modules/permission";
import { encryptPassword } from "@/utils/password-encrypt";

import dayIcon from "@/assets/svg/day.svg?component";
import darkIcon from "@/assets/svg/dark.svg?component";
import Lock from "~icons/ri/lock-fill";
import User from "~icons/ri/user-3-fill";
import RefreshLine from "~icons/ri/refresh-line";
import { useUserStoreHook } from "@/store/modules/user";
import { getCaptcha, type CaptchaOutput } from "@/api/auth";

defineOptions({
  name: "Login"
});
const router = useRouter();
const loading = ref(false);
const ruleFormRef = ref<FormInstance>();

const { initStorage } = useLayout();
initStorage();

const { dataTheme, dataThemeChange } = useDataThemeChange();
dataThemeChange();
const { title } = useNav();

const ruleForm = reactive({
  account: "",
  password: "",
  captchaCode: "",
  captchaId: ""
});

const captchaUrl = ref("");

// 生成客户端标识（GUID），页面加载时生成一次，整个会话期间保持不变
const captchaId = ref(generateUUID());

// 生成 UUID 的辅助函数
function generateUUID(): string {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, c => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

// 获取验证码
const refreshCaptcha = async () => {
  try {
    // 使用固定的 captchaId，确保前端提交和后端验证使用的是同一个 ID
    const res = await getCaptcha(captchaId.value);
    if (res && res.id && res.img) {
      captchaUrl.value = res.img;
      ruleForm.captchaId = res.id;
      // 清空验证码输入框，避免用户输入旧验证码
      ruleForm.captchaCode = "";
    }
  } catch (error) {
    console.error("获取验证码失败:", error);
  }
};

const onLogin = async (formEl: FormInstance | undefined) => {
  if (!formEl) return;
  await formEl.validate(async (valid, fields) => {
    if (valid) {
      loading.value = true;
      try {
        // 对密码进行加密（明文密码 + 盐值 → MD5）
        const encryptedPassword = encryptPassword(ruleForm.password);

        // 创建登录数据对象（使用加密后的密码）
        const loginData = {
          ...ruleForm,
          password: encryptedPassword
        };

        const user = await useUserStoreHook().login(loginData);
        if (user) {
          usePermissionStoreHook().handleWholeMenus([]);
          addPathMatch();
          router.push("/");
          message("登录成功", { type: "success" });
        } else {
          message("登录失败");
          // 登录失败后刷新验证码
          refreshCaptcha();
        }
      } catch (error) {
        message(error.message, { type: "warning" });
        // 登录失败后刷新验证码
        refreshCaptcha();
      } finally {
        loading.value = false;
      }
    }
  });
};

/** 使用公共函数，避免`removeEventListener`失效 */
function onkeypress({ code }: KeyboardEvent) {
  if (code === "Enter") {
    onLogin(ruleFormRef.value);
  }
}

onMounted(() => {
  window.document.addEventListener("keypress", onkeypress);
  // 初始化验证码
  refreshCaptcha();
});

onBeforeUnmount(() => {
  window.document.removeEventListener("keypress", onkeypress);
});
</script>

<template>
  <div class="select-none">
    <img :src="bg" class="wave" />
    <div class="absolute flex-c right-5 top-3">
      <!-- 主题 -->
      <el-switch
        v-model="dataTheme"
        inline-prompt
        :active-icon="dayIcon"
        :inactive-icon="darkIcon"
        @change="dataThemeChange"
      />
    </div>
    <div class="login-container">
      <div class="img">
        <component :is="toRaw(illustration)" />
      </div>
      <div class="login-box">
        <div class="login-form">
          <avatar class="avatar" />
          <Motion>
            <h2 class="outline-none">{{ title }}</h2>
          </Motion>

          <el-form
            ref="ruleFormRef"
            :model="ruleForm"
            :rules="loginRules"
            size="large"
          >
            <Motion :delay="100">
              <el-form-item
                :rules="[
                  {
                    required: true,
                    message: '请输入账号',
                    trigger: 'blur'
                  }
                ]"
                prop="account"
              >
                <el-input
                  v-model="ruleForm.account"
                  clearable
                  placeholder="账号"
                  :prefix-icon="useRenderIcon(User)"
                />
              </el-form-item>
            </Motion>

            <Motion :delay="150">
              <el-form-item prop="password">
                <el-input
                  v-model="ruleForm.password"
                  clearable
                  show-password
                  placeholder="密码"
                  :prefix-icon="useRenderIcon(Lock)"
                />
              </el-form-item>
            </Motion>

            <Motion :delay="200">
              <el-form-item
                :rules="[
                  {
                    required: true,
                    message: '请输入验证码',
                    trigger: 'blur'
                  }
                ]"
                prop="captchaCode"
              >
                <div class="captcha-container">
                  <el-input
                    v-model="ruleForm.captchaCode"
                    clearable
                    placeholder="验证码"
                    style="flex: 1"
                  />
                  <div class="captcha-img-wrapper" @click="refreshCaptcha">
                    <img
                      v-if="captchaUrl"
                      :src="captchaUrl"
                      alt="验证码"
                      class="captcha-img"
                    />
                    <el-button
                      v-else
                      :icon="useRenderIcon(RefreshLine)"
                      circle
                    />
                    <div class="captcha-tip">点击刷新</div>
                  </div>
                </div>
              </el-form-item>
            </Motion>

            <Motion :delay="250">
              <el-button
                class="w-full mt-4"
                size="default"
                type="primary"
                :loading="loading"
                @click="onLogin(ruleFormRef)"
              >
                登录
              </el-button>
            </Motion>
          </el-form>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@import url("@/style/login.css");
</style>

<style lang="scss" scoped>
.captcha-container {
  display: flex;
  gap: 12px;
  width: 100%;
  align-items: center;

  .captcha-img-wrapper {
    position: relative;
    width: 120px;
    height: 40px;
    display: flex;
    align-items: center;
    justify-content: center;
    border: 1px solid var(--el-border-color);
    border-radius: 4px;
    cursor: pointer;
    overflow: hidden;
    background: #f5f5f5;
    transition: all 0.3s;

    &:hover {
      border-color: var(--el-color-primary);

      .captcha-tip {
        opacity: 1;
      }
    }

    .captcha-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
    }

    .captcha-tip {
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      background: rgba(0, 0, 0, 0.6);
      color: white;
      font-size: 10px;
      text-align: center;
      padding: 2px 0;
      opacity: 0;
      transition: opacity 0.3s;
    }
  }
}

:deep(.el-input-group__append, .el-input-group__prepend) {
  padding: 0;
}
</style>
