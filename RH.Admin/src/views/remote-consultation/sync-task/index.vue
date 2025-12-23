<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, h } from "vue";
import { ElMessage, ElMessageBox, FormInstance } from "element-plus";
import { VxeButton } from "vxe-pc-ui";
import { ReVxeGrid } from "@/components/ReVxeTable";
import {
  getSyncTaskPage,
  getSyncTask,
  createSyncTask,
  updateSyncTask,
  deleteSyncTask
} from "@/api/rc/syncTask";
import { getDictionaryDataByCode } from "@/api/system/dictionary";
import type { SyncTaskQuery, SyncTaskItem } from "@/api/rc/types";

const gridRef = ref();
const searchForm = reactive<SyncTaskQuery>({
  pageIndex: 1,
  pageSize: 10,
  keyword: "",
  taskType: "",
  taskStatus: ""
});

const dictSources = reactive({
  taskType: [] as any[],
  taskStatus: [] as any[]
});

const taskTypeMap = computed<Record<string, string>>(() =>
  Object.fromEntries(dictSources.taskType.map(item => [item.code, item.name]))
);
const taskStatusMap = computed<Record<string, string>>(() =>
  Object.fromEntries(dictSources.taskStatus.map(item => [item.code, item.name]))
);

const simulatingId = ref<number | null>(null);
let simulateTimer: number | null = null;
onUnmounted(() => {
  if (simulateTimer) {
    window.clearTimeout(simulateTimer);
    simulateTimer = null;
  }
});

const normalizePayload = (payload: unknown) => {
  if (payload && typeof payload === "string") {
    try {
      return JSON.parse(payload);
    } catch {
      return payload;
    }
  }
  return payload ?? {};
};

const simulateExecute = async (row: SyncTaskItem) => {
  if (!row?.id) return;
  if (row.taskStatus === "SUCCESS") {
    ElMessage.info("任务已完成，无需模拟执行");
    return;
  }
  if (simulatingId.value) {
    ElMessage.warning("已有模拟任务执行中，请稍后再试");
    return;
  }

  await ElMessageBox.confirm(
    `确认模拟执行任务 ${row.taskNo} 吗？\n将按流程：执行中 ->（约5秒）-> 成功`,
    "模拟执行",
    { type: "warning" }
  );

  simulatingId.value = row.id;
  try {
    // 获取详情，避免覆盖/清空 payload
    const detail = await getSyncTask(row.id);
    const requestPayload = normalizePayload(detail.requestPayload);
    const responsePayload = normalizePayload(detail.responsePayload);

    await updateSyncTask(row.id, {
      taskNo: detail.taskNo,
      taskType: detail.taskType,
      taskStatus: "PROCESSING",
      requestPayload,
      responsePayload,
      remark: detail.remark ?? ""
    });

    ElMessage.success("模拟执行中...");
    handleSearch();

    simulateTimer = window.setTimeout(async () => {
      try {
        await updateSyncTask(row.id, {
          taskNo: detail.taskNo,
          taskType: detail.taskType,
          taskStatus: "SUCCESS",
          requestPayload,
          responsePayload: {
            ok: true,
            message: "演示环境模拟同步成功",
            finishedAt: new Date().toISOString()
          },
          completedTime: new Date().toISOString(),
          remark: detail.remark ?? ""
        });
        ElMessage.success("模拟执行完成");
        handleSearch();
      } catch (e) {
        ElMessage.error("模拟执行完成更新失败");
      } finally {
        simulatingId.value = null;
        simulateTimer = null;
      }
    }, 5000);
  } catch (e) {
    simulatingId.value = null;
    throw e;
  }
};

const columns = [
  { title: "任务编号", field: "taskNo", minWidth: 160 },
  {
    title: "任务类型",
    field: "taskType",
    minWidth: 120,
    slots: {
      default: ({ row }) =>
        h("span", taskTypeMap.value[row.taskType] ?? row.taskType)
    }
  },
  {
    title: "任务状态",
    field: "taskStatus",
    minWidth: 120,
    slots: {
      default: ({ row }) =>
        h("span", taskStatusMap.value[row.taskStatus] ?? row.taskStatus)
    }
  },
  { title: "重试次数", field: "retryCount", minWidth: 100 },
  { title: "完成时间", field: "completedTime", minWidth: 160 },
  { title: "备注", field: "remark", minWidth: 160 }
];

const tableActions = [
  {
    title: "操作",
    field: "operate",
    width: 320,
    fixed: "right",
    align: "center",
    slots: {
      default: ({ row }) => [
        h(VxeButton, {
          mode: "text",
          status: "success",
          content: simulatingId.value === row.id ? "执行中..." : "执行(模拟)",
          disabled:
            row.taskStatus === "SUCCESS" ||
            row.taskStatus === "PROCESSING" ||
            (simulatingId.value !== null && simulatingId.value !== row.id),
          onClick: () => simulateExecute(row)
        }),
        h(VxeButton, {
          mode: "text",
          status: "primary",
          content: "查看",
          onClick: () => handleView(row)
        }),
        h(VxeButton, {
          mode: "text",
          status: "primary",
          content: "编辑",
          onClick: () => handleEdit(row)
        }),
        h(VxeButton, {
          mode: "text",
          status: "danger",
          content: "删除",
          onClick: () => handleDelete(row)
        })
      ]
    }
  }
];

const functions = {
  add: "remote.syncTask.add",
  edit: "remote.syncTask.edit",
  view: "remote.syncTask.view",
  delete: "remote.syncTask.delete"
};

const handleSearch = () => gridRef.value?.loadData();
const handleReset = () => {
  searchForm.keyword = "";
  searchForm.taskType = "";
  searchForm.taskStatus = "";
  handleSearch();
};

const editDialogVisible = ref(false);
const editFormRef = ref<FormInstance>();
const editForm = reactive({
  id: undefined as number | undefined,
  taskNo: "",
  taskType: "",
  taskStatus: "PENDING",
  requestPayload: "{}",
  responsePayload: "{}",
  remark: ""
});

const editRules = {
  taskNo: [{ required: true, message: "请输入任务编号", trigger: "blur" }],
  taskType: [{ required: true, message: "请选择任务类型", trigger: "change" }]
};

const detailDrawerVisible = ref(false);
const detailData = ref<SyncTaskItem | null>(null);

const handleAdd = () => {
  Object.assign(editForm, {
    id: undefined,
    taskNo: "",
    taskType: "",
    taskStatus: "PENDING",
    requestPayload: "{}",
    responsePayload: "{}",
    remark: ""
  });
  editDialogVisible.value = true;
};

const handleEdit = async (row: SyncTaskItem) => {
  const detail = await getSyncTask(row.id);
  Object.assign(editForm, {
    id: detail.id,
    taskNo: detail.taskNo,
    taskType: detail.taskType,
    taskStatus: detail.taskStatus,
    requestPayload: JSON.stringify(detail.requestPayload ?? {}, null, 2),
    responsePayload: JSON.stringify(detail.responsePayload ?? {}, null, 2),
    remark: detail.remark ?? ""
  });
  editDialogVisible.value = true;
};

const parseJson = (value: string) => {
  try {
    return value ? JSON.parse(value) : null;
  } catch (error) {
    ElMessage.error("JSON 格式错误");
    throw error;
  }
};

const submitEdit = async () => {
  await editFormRef.value?.validate();
  const payload = {
    taskNo: editForm.taskNo,
    taskType: editForm.taskType,
    taskStatus: editForm.taskStatus,
    requestPayload: parseJson(editForm.requestPayload),
    responsePayload: parseJson(editForm.responsePayload),
    remark: editForm.remark
  };
  if (editForm.id) {
    await updateSyncTask(editForm.id, payload);
  } else {
    await createSyncTask(payload);
  }
  ElMessage.success("保存成功");
  editDialogVisible.value = false;
  handleSearch();
};

const handleDelete = async (row: SyncTaskItem) => {
  await ElMessageBox.confirm(`确认删除任务 ${row.taskNo} 吗？`, "提示", {
    type: "warning"
  });
  await deleteSyncTask(row.id);
  ElMessage.success("删除成功");
  handleSearch();
};

const handleView = async (row: SyncTaskItem) => {
  detailDrawerVisible.value = true;
  detailData.value = await getSyncTask(row.id);
};

const request = async params => {
  const result = await getSyncTaskPage({ ...searchForm, ...params });
  return result;
};

onMounted(async () => {
  const [typeDict, statusDict] = await Promise.all([
    getDictionaryDataByCode("RC_SYNC_TYPE"),
    getDictionaryDataByCode("RC_SYNC_STATUS")
  ]);
  dictSources.taskType = typeDict ?? [];
  dictSources.taskStatus = statusDict ?? [];
});
</script>

<template>
  <div class="sync-task-page">
    <el-card shadow="never">
      <vxe-form
        :data="searchForm"
        :items="[
          {
            field: 'keyword',
            title: '关键字',
            span: 8,
            itemRender: {
              name: '$input',
              props: { placeholder: '任务编号/备注' }
            }
          },
          {
            field: 'taskType',
            title: '任务类型',
            span: 8,
            itemRender: {
              name: '$select',
              props: {
                options: [
                  { label: '全部', value: '' },
                  ...dictSources.taskType.map(item => ({
                    label: item.name,
                    value: item.code
                  }))
                ]
              }
            }
          },
          {
            field: 'taskStatus',
            title: '任务状态',
            span: 8,
            itemRender: {
              name: '$select',
              props: {
                options: [
                  { label: '全部', value: '' },
                  ...dictSources.taskStatus.map(item => ({
                    label: item.name,
                    value: item.code
                  }))
                ]
              }
            }
          },
          {
            span: 8,
            itemRender: {
              name: '$buttons',
              children: [
                {
                  props: {
                    type: 'submit',
                    icon: 'vxe-icon-search',
                    content: '查询',
                    status: 'primary'
                  },
                  events: { click: handleSearch }
                },
                {
                  props: {
                    type: 'reset',
                    icon: 'vxe-icon-undo',
                    content: '重置'
                  },
                  events: { click: handleReset }
                }
              ]
            }
          }
        ]"
      />
    </el-card>

    <el-card shadow="never">
      <ReVxeGrid
        ref="gridRef"
        :columns="columns"
        :customTableActions="tableActions"
        :request="request"
        :functions="functions"
        :searchParams="searchForm"
        @handleAdd="handleAdd"
      />
    </el-card>

    <el-dialog
      v-model="editDialogVisible"
      :title="editForm.id ? '编辑任务' : '新建任务'"
      width="720px"
    >
      <el-form
        ref="editFormRef"
        :model="editForm"
        :rules="editRules"
        label-width="120px"
      >
        <el-form-item label="任务编号" prop="taskNo">
          <el-input v-model="editForm.taskNo" placeholder="唯一编号" />
        </el-form-item>
        <el-form-item label="任务类型" prop="taskType">
          <el-select v-model="editForm.taskType" placeholder="选择任务类型">
            <el-option
              v-for="item in dictSources.taskType"
              :key="item.code"
              :label="item.name"
              :value="item.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="任务状态">
          <el-select v-model="editForm.taskStatus">
            <el-option
              v-for="item in dictSources.taskStatus"
              :key="item.code"
              :label="item.name"
              :value="item.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="请求负载">
          <el-input
            v-model="editForm.requestPayload"
            type="textarea"
            :rows="6"
            placeholder="JSON 字符串"
          />
        </el-form-item>
        <el-form-item label="响应负载">
          <el-input
            v-model="editForm.responsePayload"
            type="textarea"
            :rows="6"
            placeholder="JSON 字符串"
          />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="editForm.remark" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="detailDrawerVisible" title="任务详情" size="55%">
      <el-descriptions v-if="detailData" border :column="2">
        <el-descriptions-item label="任务编号">{{
          detailData.taskNo
        }}</el-descriptions-item>
        <el-descriptions-item label="任务类型">{{
          taskTypeMap.value[detailData.taskType] ?? detailData.taskType
        }}</el-descriptions-item>
        <el-descriptions-item label="任务状态">{{
          taskStatusMap.value[detailData.taskStatus] ?? detailData.taskStatus
        }}</el-descriptions-item>
        <el-descriptions-item label="重试次数">{{
          detailData.retryCount
        }}</el-descriptions-item>
        <el-descriptions-item label="完成时间" :span="2">{{
          detailData.completedTime
        }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{
          detailData.remark
        }}</el-descriptions-item>
      </el-descriptions>
      <el-divider>请求负载</el-divider>
      <el-input
        v-if="detailData"
        :model-value="JSON.stringify(detailData.requestPayload ?? {}, null, 2)"
        type="textarea"
        :rows="10"
        readonly
      />
      <el-divider>响应负载</el-divider>
      <el-input
        v-if="detailData"
        :model-value="JSON.stringify(detailData.responsePayload ?? {}, null, 2)"
        type="textarea"
        :rows="10"
        readonly
      />
    </el-drawer>
  </div>
</template>

<style scoped>
.sync-task-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>
