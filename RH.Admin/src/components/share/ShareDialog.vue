<template>
  <el-dialog
    v-model="visible"
    title="生成分享链接"
    width="650px"
    :close-on-click-modal="false"
  >
    <div v-if="!shareUrl" class="input-section">
      <el-form :model="form" label-width="100px">
        <el-form-item label="分享说明">
          <el-input
            v-model="form.message"
            type="textarea"
            :rows="3"
            placeholder="可选：添加说明信息，帮助接收者了解分享内容"
          />
        </el-form-item>
        <el-form-item label="有效期">
          <el-select v-model="form.expireDays" placeholder="选择有效期">
            <el-option label="1天" :value="1" />
            <el-option label="3天" :value="3" />
            <el-option label="7天" :value="7" />
            <el-option label="30天" :value="30" />
          </el-select>
        </el-form-item>
      </el-form>

      <div class="action-btns">
        <el-button @click="visible = false">取消</el-button>
        <el-button type="primary" :loading="generating" @click="generateLink">
          生成链接
        </el-button>
      </div>
    </div>

    <div v-else class="result-section">
      <!-- 分享链接 -->
      <div class="link-box">
        <div class="link-header">
          <span class="icon">🔗</span>
          <span class="title">分享链接</span>
        </div>
        <el-input
          :value="shareUrl"
          readonly
          type="textarea"
          :rows="2"
          class="link-input"
        />
        <el-button
          type="primary"
          size="small"
          class="copy-btn"
          @click="copyLink(shareUrl)"
        >
          复制链接
        </el-button>
      </div>

      <!-- 证书警告 -->
      <div v-if="showCertWarning" class="cert-warning">
        <el-alert
          title="⚠️ 首次访问需安装证书"
          type="warning"
          :closable="false"
          description="对方如果是首次访问，需要先安装信任证书。请将下方指引一起发送给对方。"
        >
          <template #default>
            <div class="alert-content">
              <p class="warning-text">
                <strong>重要提示：</strong>接收者必须先安装证书才能访问分享内容
              </p>
              <ol class="steps-list">
                <li>发送下方"证书安装指引"给对方</li>
                <li>对方按照指引安装证书</li>
                <li>安装完成后，再发送"分享链接"</li>
              </ol>
            </div>
          </template>
        </el-alert>

        <!-- 证书安装指引链接 -->
        <div class="link-box" style="margin-top: 15px">
          <div class="link-header">
            <span class="icon">📱</span>
            <span class="title">证书安装指引（首次访问必看）</span>
          </div>
          <el-input
            :value="installGuideUrl"
            readonly
            type="textarea"
            :rows="2"
            class="link-input"
          />
          <el-button
            type="success"
            size="small"
            class="copy-btn"
            @click="copyLink(installGuideUrl)"
          >
            复制指引
          </el-button>
        </div>

        <!-- 一键复制完整信息 -->
        <div class="quick-copy">
          <el-button type="warning" :icon="DocumentCopy" @click="copyFullInfo">
            一键复制完整信息（推荐）
          </el-button>
          <span class="tip">包含分享链接和安装指引，直接发送给对方即可</span>
        </div>

        <!-- 证书检测 -->
        <div class="cert-check">
          <el-divider />
          <p class="check-title">🔍 可选：检测对方是否已安装证书</p>
          <el-button :loading="checking" plain @click="checkCert">
            检测证书状态
          </el-button>
          <p class="check-tip">如果对方已安装证书，则无需再发送安装指引</p>
        </div>
      </div>

      <!-- 已安装证书的情况 -->
      <div v-else class="cert-ok">
        <el-alert
          title="✅ 检测到证书已安装"
          type="success"
          :closable="false"
          description="可以直接发送分享链接，无需安装指引"
        />
      </div>

      <!-- 底部操作 -->
      <div class="action-btns" style="margin-top: 20px">
        <el-button @click="reset">生成新链接</el-button>
        <el-button type="primary" @click="visible = false">完成</el-button>
      </div>
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from "vue";
import { ElMessage } from "element-plus";
import { DocumentCopy } from "@element-plus/icons-vue";
import {
  checkCertificate,
  getInstallGuideUrl,
  copyInstallGuide
} from "@/utils/cert-check";
import { createShareLink } from "@/api/rc/share";

interface ShareForm {
  message: string;
  expireDays: number;
}

const props = defineProps<{
  consultationId?: string;
}>();

const visible = ref(false);
const generating = ref(false);
const checking = ref(false);
const shareUrl = ref("");
const installGuideUrl = ref("");
const showCertWarning = ref(false);

const form = reactive<ShareForm>({
  message: "",
  expireDays: 7
});

// 打开对话框
const open = () => {
  visible.value = true;
  reset();
};

// 重置状态
const reset = () => {
  shareUrl.value = "";
  installGuideUrl.value = "";
  showCertWarning.value = false;
  form.message = "";
  form.expireDays = 7;
};

// 生成分享链接
const generateLink = async () => {
  if (!props.consultationId) {
    ElMessage.error("会诊ID不能为空");
    return;
  }

  generating.value = true;

  try {
    // 调用后端接口生成分享链接
    const response = await createShareLink(Number(props.consultationId), {
      consultationId: Number(props.consultationId),
      expireHours: form.expireDays * 24, // 将天数转换为小时
      remark: form.message || undefined
    });

    // 处理响应数据
    const data = response.data || response;
    shareUrl.value = data.shareUrl;
    installGuideUrl.value = getInstallGuideUrl(shareUrl.value);

    // 检测证书状态
    const certInstalled = await checkCertificate();
    showCertWarning.value = !certInstalled;

    if (showCertWarning.value) {
      ElMessage.warning({
        message: "检测到证书未安装，请将安装指引一起发送给对方",
        duration: 5000
      });
    } else {
      ElMessage.success("分享链接生成成功");
    }
  } catch (error: any) {
    console.error("生成分享链接失败:", error);
    const errorMsg =
      error.response?.data?.message || error.message || "生成失败";
    ElMessage.error(errorMsg);
  } finally {
    generating.value = false;
  }
};

// 复制链接
const copyLink = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text);
    ElMessage.success("已复制到剪贴板");
  } catch (error) {
    ElMessage.error("复制失败，请手动复制");
  }
};

// 复制完整信息
const copyFullInfo = async () => {
  await copyInstallGuide(shareUrl.value);
};

// 检测证书
const checkCert = async () => {
  checking.value = true;
  try {
    const installed = await checkCertificate();
    if (installed) {
      ElMessage.success("✅ 证书已安装！对方可以直接访问分享链接");
      showCertWarning.value = false;
    } else {
      ElMessage.warning("❌ 证书未安装或不受信任，请先发送安装指引");
    }
  } finally {
    checking.value = false;
  }
};

// 暴露方法供父组件调用
defineExpose({
  open
});
</script>

<style scoped lang="scss">
.input-section {
  padding: 10px 0;
}

.result-section {
  .link-box {
    background: #f8f9fa;
    padding: 15px;
    border-radius: 8px;
    margin-bottom: 15px;
    position: relative;

    .link-header {
      display: flex;
      align-items: center;
      margin-bottom: 10px;

      .icon {
        font-size: 20px;
        margin-right: 8px;
      }

      .title {
        font-weight: 500;
        color: #333;
      }
    }

    .link-input {
      margin-bottom: 10px;

      :deep(.el-textarea__inner) {
        font-size: 12px;
        font-family: monospace;
      }
    }

    .copy-btn {
      width: 100%;
    }
  }

  .cert-warning {
    background: #fff3cd;
    padding: 15px;
    border-radius: 8px;
    border: 1px solid #ffc107;

    .alert-content {
      padding: 10px 0;

      .warning-text {
        color: #856404;
        margin-bottom: 10px;
        line-height: 1.6;
      }

      .steps-list {
        margin-left: 20px;
        color: #856404;
        line-height: 1.8;

        li {
          margin: 5px 0;
        }
      }
    }

    .quick-copy {
      margin-top: 15px;
      text-align: center;

      .tip {
        display: block;
        margin-top: 8px;
        font-size: 12px;
        color: #666;
      }
    }

    .cert-check {
      text-align: center;

      .check-title {
        font-weight: 500;
        margin-bottom: 10px;
        color: #333;
      }

      .check-tip {
        margin-top: 8px;
        font-size: 12px;
        color: #666;
      }
    }
  }

  .cert-ok {
    margin: 15px 0;
  }
}

.action-btns {
  text-align: right;
  margin-top: 20px;
}
</style>
