<script setup lang="ts">
import { ref, reactive, computed, watch } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { CopyDocument, Delete, Link, RefreshRight } from "@element-plus/icons-vue";
import {
  createShareLink,
  getShareLinks,
  toggleShareLink,
  deleteShareLink,
  type ShareLink
} from "@/api/rc/share";

interface Props {
  visible: boolean;
  consultationId: number | null;
  consultationStatus?: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  "update:visible": [value: boolean];
}>();

// unwrap函数：提取API响应数据
const unwrap = <T,>(response: any): T => {
  if (response && typeof response === "object" && "data" in response) {
    return (response as { data: T }).data;
  }
  return response as T;
};

const dialogVisible = computed({
  get: () => props.visible,
  set: (value) => emit("update:visible", value)
});

const shareLinks = ref<ShareLink[]>([]);
const loading = ref(false);
const creating = ref(false);

// 创建分享链接表单
const createForm = reactive({
  expireHours: 24,
  maxUsageCount: 0,
  visitorDisplayName: "外部专家",
  remark: ""
});

const canShare = computed(() => {
  return props.consultationStatus === "IN_PROGRESS" || props.consultationStatus === "SCHEDULED";
});

// 加载分享链接列表
const loadShareLinks = async () => {
  if (!props.consultationId) return;

  try {
    loading.value = true;
    const response = await getShareLinks(props.consultationId);
    shareLinks.value = unwrap<ShareLink[]>(response) || [];
  } catch (error) {
    console.error("Failed to load share links:", error);
    ElMessage.error("加载分享链接失败");
  } finally {
    loading.value = false;
  }
};

// 创建分享链接
const handleCreate = async () => {
  if (!props.consultationId) {
    ElMessage.warning("会诊ID不存在");
    return;
  }

  if (!canShare.value) {
    ElMessage.warning("只能分享进行中或已排期的会诊");
    return;
  }

  try {
    creating.value = true;
    const response = await createShareLink(props.consultationId, {
      consultationId: props.consultationId,
      expireHours: createForm.expireHours,
      maxUsageCount: createForm.maxUsageCount,
      visitorDisplayName: createForm.visitorDisplayName,
      remark: createForm.remark
    });

    const newLink = unwrap<ShareLink>(response);
    shareLinks.value.unshift(newLink);
    ElMessage.success("分享链接创建成功");

    // 重置表单
    createForm.expireHours = 24;
    createForm.maxUsageCount = 0;
    createForm.visitorDisplayName = "外部专家";
    createForm.remark = "";
  } catch (error) {
    console.error("Failed to create share link:", error);
    ElMessage.error("创建分享链接失败");
  } finally {
    creating.value = false;
  }
};

// 复制链接
const handleCopy = async (url: string) => {
  try {
    await navigator.clipboard.writeText(url);
    ElMessage.success("链接已复制到剪贴板");
  } catch (error) {
    ElMessage.error("复制失败，请手动复制");
  }
};

// 复制分享码
const handleCopyToken = async (token: string) => {
  try {
    await navigator.clipboard.writeText(token);
    ElMessage.success("分享码已复制到剪贴板");
  } catch (error) {
    ElMessage.error("复制失败，请手动复制");
  }
};

// 切换启用/禁用状态
const handleToggle = async (link: ShareLink) => {
  if (!props.consultationId) return;

  try {
    const response = await toggleShareLink(props.consultationId, link.id);
    link.isEnabled = unwrap<boolean>(response);
    ElMessage.success(link.isEnabled ? "已启用" : "已禁用");
  } catch (error) {
    console.error("Failed to toggle share link:", error);
    ElMessage.error("操作失败");
  }
};

// 删除分享链接
const handleDelete = async (link: ShareLink) => {
  if (!props.consultationId) return;

  try {
    await ElMessageBox.confirm(
      "确定要删除这个分享链接吗？删除后将无法访问。",
      "确认删除",
      {
        confirmButtonText: "确定",
        cancelButtonText: "取消",
        type: "warning"
      }
    );

    const response = await deleteShareLink(props.consultationId, link.id);
    if (unwrap<boolean>(response)) {
      shareLinks.value = shareLinks.value.filter(l => l.id !== link.id);
      ElMessage.success("删除成功");
    }
  } catch (error) {
    if (error !== "cancel") {
      console.error("Failed to delete share link:", error);
      ElMessage.error("删除失败");
    }
  }
};

// 监听对话框打开
watch(
  () => props.visible,
  (newVal) => {
    if (newVal && props.consultationId) {
      loadShareLinks();
    }
  }
);

// 判断是否已过期
const isExpired = (expireTime: string) => {
  return new Date(expireTime) < new Date();
};

// 判断是否已用完
const isUsedUp = (link: ShareLink) => {
  return link.maxUsageCount > 0 && link.usedCount >= link.maxUsageCount;
};
</script>

<template>
  <el-dialog
    v-model="dialogVisible"
    title="会诊分享链接管理"
    width="900px"
    @close="dialogVisible = false"
  >
    <div class="share-link-dialog">
      <!-- 创建表单 -->
      <div class="create-section">
        <h4>创建分享链接</h4>
        <el-alert
          v-if="!canShare"
          title="只能分享进行中或已排期的会诊"
          type="warning"
          :closable="false"
          style="margin-bottom: 16px"
        />

        <el-form :inline="true" :model="createForm" label-width="120px">
          <el-form-item label="有效期(小时)">
            <el-input-number
              v-model="createForm.expireHours"
              :min="1"
              :max="168"
              :step="1"
              :disabled="!canShare || creating"
            />
            <span style="margin-left: 8px; color: #909399; font-size: 12px">
              建议设置24-48小时
            </span>
          </el-form-item>

          <el-form-item label="最大使用次数">
            <el-input-number
              v-model="createForm.maxUsageCount"
              :min="0"
              :max="100"
              :step="1"
              :disabled="!canShare || creating"
            />
            <span style="margin-left: 8px; color: #909399; font-size: 12px">
              0表示不限制
            </span>
          </el-form-item>

          <el-form-item label="访客显示名称">
            <el-input
              v-model="createForm.visitorDisplayName"
              placeholder="外部专家"
              :disabled="!canShare || creating"
              style="width: 200px"
            />
          </el-form-item>

          <el-form-item label="备注">
            <el-input
              v-model="createForm.remark"
              placeholder="选填"
              :disabled="!canShare || creating"
              style="width: 200px"
            />
          </el-form-item>

          <el-form-item>
            <el-button
              type="primary"
              :icon="Link"
              :loading="creating"
              :disabled="!canShare"
              @click="handleCreate"
            >
              生成分享链接
            </el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-divider />

      <!-- 分享链接列表 -->
      <div class="links-section">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px">
          <h4>已创建的分享链接</h4>
          <el-button
            size="small"
            :icon="RefreshRight"
            @click="loadShareLinks"
            :loading="loading"
          >
            刷新
          </el-button>
        </div>

        <el-empty v-if="shareLinks.length === 0 && !loading" description="暂无分享链接" />

        <div v-else class="links-list">
          <div
            v-for="link in shareLinks"
            :key="link.id"
            class="link-item"
            :class="{
              'is-disabled': !link.isEnabled,
              'is-expired': isExpired(link.expireTime),
              'is-used-up': isUsedUp(link)
            }"
          >
            <div class="link-header">
              <div class="link-info">
                <el-tag
                  :type="link.isEnabled ? 'success' : 'info'"
                  size="small"
                  style="margin-right: 8px"
                >
                  {{ link.isEnabled ? "启用" : "禁用" }}
                </el-tag>
                <el-tag
                  v-if="isExpired(link.expireTime)"
                  type="danger"
                  size="small"
                  style="margin-right: 8px"
                >
                  已过期
                </el-tag>
                <el-tag
                  v-if="isUsedUp(link)"
                  type="warning"
                  size="small"
                >
                  已用完
                </el-tag>
                <span style="font-size: 12px; color: #909399">
                  创建于 {{ new Date(link.createTime).toLocaleString("zh-CN") }}
                </span>
              </div>
              <div class="link-actions">
                <el-button
                  size="small"
                  :type="link.isEnabled ? 'warning' : 'success'"
                  link
                  @click="handleToggle(link)"
                >
                  {{ link.isEnabled ? "禁用" : "启用" }}
                </el-button>
                <el-button
                  size="small"
                  type="danger"
                  link
                  :icon="Delete"
                  @click="handleDelete(link)"
                >
                  删除
                </el-button>
              </div>
            </div>

            <div class="link-content">
              <div class="link-field">
                <label>分享链接：</label>
                <el-input
                  :model-value="link.shareUrl"
                  readonly
                  size="small"
                  style="flex: 1"
                >
                  <template #append>
                    <el-button
                      :icon="CopyDocument"
                      @click="handleCopy(link.shareUrl)"
                    >
                      复制
                    </el-button>
                  </template>
                </el-input>
              </div>

              <div class="link-field">
                <label>分享码：</label>
                <el-input
                  :model-value="link.shareToken"
                  readonly
                  size="small"
                  style="flex: 1"
                >
                  <template #append>
                    <el-button
                      :icon="CopyDocument"
                      @click="handleCopyToken(link.shareToken)"
                    >
                      复制
                    </el-button>
                  </template>
                </el-input>
              </div>

              <div class="link-stats">
                <div class="stat-item">
                  <span class="label">使用次数：</span>
                  <span class="value">
                    {{ link.usedCount }}
                    <span v-if="link.maxUsageCount > 0">
                      / {{ link.maxUsageCount }}
                    </span>
                    <span v-else>
                      (不限制)
                    </span>
                  </span>
                </div>
                <div class="stat-item">
                  <span class="label">过期时间：</span>
                  <span class="value">
                    {{ new Date(link.expireTime).toLocaleString("zh-CN") }}
                  </span>
                </div>
                <div v-if="link.remark" class="stat-item">
                  <span class="label">备注：</span>
                  <span class="value">{{ link.remark }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 使用说明 -->
      <el-collapse style="margin-top: 16px">
        <el-collapse-item title="使用说明" name="usage">
          <ol style="padding-left: 20px; margin: 0; line-height: 1.8">
            <li>点击"生成分享链接"创建一个新的分享链接</li>
            <li>复制分享链接发送给第三方人员</li>
            <li>第三方人员打开链接即可无需登录参与会诊</li>
            <li>可以随时禁用或删除分享链接</li>
            <li>建议设置合理的有效期和使用次数以提高安全性</li>
          </ol>
        </el-collapse-item>
      </el-collapse>
    </div>
  </el-dialog>
</template>

<style scoped lang="scss">
.share-link-dialog {
  .create-section {
    margin-bottom: 16px;

    h4 {
      margin-bottom: 16px;
      font-size: 16px;
      font-weight: 600;
    }
  }

  .links-section {
    h4 {
      margin: 0;
      font-size: 16px;
      font-weight: 600;
    }
  }

  .links-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
    max-height: 500px;
    overflow-y: auto;
  }

  .link-item {
    border: 1px solid #dcdfe6;
    border-radius: 8px;
    padding: 12px;
    transition: all 0.3s;

    &:hover {
      box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
    }

    &.is-disabled {
      opacity: 0.6;
      background-color: #f5f7fa;
    }

    &.is-expired,
    &.is-used-up {
      border-color: #f56c6c;
      background-color: #fef0f0;
    }
  }

  .link-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
  }

  .link-info {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .link-actions {
    display: flex;
    gap: 8px;
  }

  .link-content {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .link-field {
    display: flex;
    align-items: center;
    gap: 8px;

    label {
      white-space: nowrap;
      font-weight: 500;
      color: #606266;
    }
  }

  .link-stats {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
    padding: 8px;
    background-color: #f5f7fa;
    border-radius: 4px;

    .stat-item {
      display: flex;
      flex-direction: column;
      gap: 4px;

      .label {
        font-size: 12px;
        color: #909399;
      }

      .value {
        font-size: 13px;
        color: #303133;
        font-weight: 500;
      }
    }
  }
}
</style>
