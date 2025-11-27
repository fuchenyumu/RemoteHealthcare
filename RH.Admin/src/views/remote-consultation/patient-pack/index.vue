<script setup lang="ts">
import { ref, reactive, computed, onMounted, h } from "vue";
import { ElMessage, ElMessageBox, FormInstance } from "element-plus";
import type { UploadRequestOptions } from "element-plus";
import { VxeButton } from "vxe-pc-ui";
import { ReVxeGrid } from "@/components/ReVxeTable";
import {
  getPatientPackPage,
  getPatientPack,
  createPatientPack,
  updatePatientPack,
  deletePatientPack,
  getPatientPackFiles,
  uploadPatientPackFile,
  deletePatientPackFile
} from "@/api/rc/patientPack";
import { getPatientCasePage, getPatientCaseDetail } from "@/api/rc/patientCase";
import { useRoute } from "vue-router";
import { getDictionaryDataByCode } from "@/api/system/dictionary";
import type {
  PatientPackQuery,
  PatientPack,
  PatientCaseDetail,
  PatientCaseSummary,
  PatientPackFile,
  PagedResult
} from "@/api/rc/types";
const unwrap = <T>(response: any): T => {
  if (response && typeof response === "object" && "data" in response) {
    return (response as { data: T }).data;
  }
  return response as T;
};


const gridRef = ref();
const searchForm = reactive<PatientPackQuery>({
  pageIndex: 1,
  pageSize: 10,
  caseId: undefined,
  packStatus: "",
  keyword: ""
});

const dictSources = reactive({
  packStatus: [] as any[]
});

const packFiles = ref<PatientPackFile[]>([]);
const fileLoading = ref(false);
const uploadLoading = ref(false);
const activePackId = ref<number | null>(null);
const uploadMeta = reactive({ imageType: "", sourceSystem: "" });

const packStatusMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.packStatus) ? dictSources.packStatus : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});

const formatSize = (size?: number | null) => {
  if (!size) return "0.00";
  return (size / 1024 / 1024).toFixed(2);
};

const resetAttachmentState = () => {
  packFiles.value = [];
  activePackId.value = null;
  fileLoading.value = false;
  uploadLoading.value = false;
};

const patientOptions = ref<Array<{ label: string; value: number }>>([]);
const patientLoading = ref(false);

const columns = [
  { title: "资料包编号", field: "packNo", minWidth: 160 },
  { title: "患者ID", field: "caseId", minWidth: 120 },
  {
    title: "状态",
    field: "packStatus",
    minWidth: 120,
    slots: { default: ({ row }) => h("span", packStatusMap.value?.[row.packStatus] ?? row.packStatus) }
  },
  { title: "文件数", field: "fileCount", minWidth: 100 },
  {
    title: "总大小(MB)",
    field: "totalSize",
    minWidth: 120,
    slots: { default: ({ row }) => h("span", formatSize(row.totalSize)) }
  },
  { title: "过期时间", field: "expireTime", minWidth: 160 },
  { title: "创建时间", field: "createTime", minWidth: 160 }
];

const tableActions = [
  {
    title: "操作",
    field: "operate",
    fixed: "right",
    width: 220,
    align: "center",
    slots: {
      default: ({ row }) => [
        h(
          VxeButton,
          {
            mode: "text",
            status: "primary",
            content: "查看",
            onClick: () => handleView(row)
          }
        ),
        h(
          VxeButton,
          {
            mode: "text",
            status: "primary",
            content: "编辑",
            onClick: () => handleEdit(row)
          }
        ),
        h(
          VxeButton,
          {
            mode: "text",
            status: "danger",
            content: "删除",
            onClick: () => handleDelete(row)
          }
        )
      ]
    }
  }
];

const functions = {
  add: "remote.patientPack.add",
  edit: "remote.patientPack.edit",
  view: "remote.patientPack.view",
  delete: "remote.patientPack.delete"
};

const handleSearch = () => gridRef.value?.loadData();
const handleReset = () => {
  searchForm.caseId = undefined;
  searchForm.packStatus = "";
  searchForm.keyword = "";
  handleSearch();
};

const editDialogVisible = ref(false);
const editFormRef = ref<FormInstance>();
const editForm = reactive({
  id: undefined as number | undefined,
  caseId: undefined as number | undefined,
  packStatus: "PENDING",
  expireTime: "",
  remark: "",
  packNo: "",
  fileCount: 0,
  totalSize: 0
});

const editRules = {
  caseId: [{ required: true, message: "请选择患者", trigger: "change" }],
  packStatus: [{ required: true, message: "请选择状态", trigger: "change" }]
};

const detailDrawerVisible = ref(false);
const detailData = ref<PatientPack | null>(null);
const detailPatient = ref<PatientCaseDetail | null>(null);

const resetEditForm = () => {
  Object.assign(editForm, {
    id: undefined,
    caseId: undefined,
    packStatus: "PENDING",
    expireTime: "",
    remark: "",
    packNo: "",
    fileCount: 0,
    totalSize: 0
  });
};

const fetchPatientOptions = async (keyword = "") => {
  patientLoading.value = true;
  try {
    const page = unwrap<PagedResult<PatientCaseSummary>>(
      await getPatientCasePage({ pageIndex: 1, pageSize: 20, keyword })
    );
    const items = page?.items ?? [];
    patientOptions.value = items.map(item => ({
      value: item.id,
      label: `${item.patientName}（ID:${item.id}）`
    }));
  } finally {
    patientLoading.value = false;
  }
};

const ensurePatientOption = async (caseId?: number) => {
  if (!caseId) return;
  const exists = patientOptions.value.some(option => option.value === caseId);
  if (exists) return;
  try {
    const patient = unwrap<PatientCaseDetail>(await getPatientCaseDetail(caseId));
    const label = patient?.patientName ? `${patient.patientName}（ID:${caseId}）` : `ID:${caseId}`;
    patientOptions.value = [{ value: caseId, label }, ...patientOptions.value];
  } catch (error) {
    patientOptions.value = [{ value: caseId, label: `ID:${caseId}` }, ...patientOptions.value];
  }
};

const refreshPackDetail = async (packId: number) => {
  const detail = unwrap<PatientPack>(await getPatientPack(packId));
  if (detailDrawerVisible.value && activePackId.value === packId) {
    detailData.value = detail;
  }
  return detail;
};

const loadPackFiles = async (packId: number) => {
  fileLoading.value = true;
  try {
    const files = unwrap<PatientPackFile[]>(await getPatientPackFiles(packId));
    packFiles.value = files ?? [];
  } finally {
    fileLoading.value = false;
  }
};

const handleDeleteFile = async (file: PatientPackFile) => {
  const packId = activePackId.value;
  if (!packId) return;
  await ElMessageBox.confirm(`确认删除附件 ${file.fileName} 吗？`, "提示", { type: "warning" });
  await deletePatientPackFile(packId, file.id);
  ElMessage.success("删除成功");
  const updated = await refreshPackDetail(packId);
  if (editDialogVisible.value && editForm.id === packId) {
    editForm.fileCount = updated.fileCount ?? 0;
    editForm.totalSize = updated.totalSize ?? 0;
  }
  await loadPackFiles(packId);
};

const handleFileUpload = async (options: UploadRequestOptions) => {
  const packId = activePackId.value;
  if (!packId) {
    options.onError?.(new Error("资料包未保存或未选中"));
    return;
  }
  const rawFile = options.file as File;
  const formData = new FormData();
  formData.append("file", rawFile);
  if (uploadMeta.imageType) formData.append("imageType", uploadMeta.imageType);
  if (uploadMeta.sourceSystem) formData.append("sourceSystem", uploadMeta.sourceSystem);
  uploadLoading.value = true;
  try {
    await uploadPatientPackFile(packId, formData);
    ElMessage.success("上传成功");
    const updated = await refreshPackDetail(packId);
    if (editDialogVisible.value && editForm.id === packId) {
      editForm.fileCount = updated.fileCount ?? 0;
      editForm.totalSize = updated.totalSize ?? 0;
    }
    await loadPackFiles(packId);
    options.onSuccess?.({}, rawFile);
  } catch (error) {
    options.onError?.(error as Error);
  } finally {
    uploadLoading.value = false;
  }
};

const handleAdd = async () => {
  resetEditForm();
  resetAttachmentState();
  patientOptions.value = [];
  detailPatient.value = null;
  await fetchPatientOptions();
  editDialogVisible.value = true;
};

const handleEdit = async (row: PatientPack) => {
  packFiles.value = [];
  const detail = unwrap<PatientPack>(await getPatientPack(row.id));
  Object.assign(editForm, {
    id: detail.id,
    caseId: detail.caseId,
    packStatus: detail.packStatus,
    expireTime: detail.expireTime ? detail.expireTime.slice(0, 16) : "",
    remark: detail.remark ?? "",
    packNo: detail.packNo,
    fileCount: detail.fileCount,
    totalSize: detail.totalSize
  });
  await fetchPatientOptions();
  await ensurePatientOption(detail.caseId);
  activePackId.value = detail.id;
  await loadPackFiles(detail.id);
  editDialogVisible.value = true;
};

const submitEdit = async () => {
  await editFormRef.value?.validate();
  if (!editForm.caseId) {
    ElMessage.warning("请选择患者");
    return;
  }
  const payload = {
    caseId: editForm.caseId,
    packStatus: editForm.packStatus,
    expireTime: editForm.expireTime ? new Date(editForm.expireTime).toISOString() : null,
    remark: editForm.remark
  };
  if (editForm.id) {
    await updatePatientPack(editForm.id, payload);
    const updated = await refreshPackDetail(editForm.id);
    editForm.fileCount = updated.fileCount ?? 0;
    editForm.totalSize = updated.totalSize ?? 0;
    if (activePackId.value === editForm.id) {
      await loadPackFiles(editForm.id);
    }
    ElMessage.success("保存成功");
    editDialogVisible.value = false;
    handleSearch();
  } else {
    const newId = unwrap<number>(await createPatientPack(payload));
    ElMessage.success("资料包已创建，请继续上传附件");
    handleSearch();
    const detail = await refreshPackDetail(newId);
    Object.assign(editForm, {
      id: detail.id,
      caseId: detail.caseId,
      packStatus: detail.packStatus,
      expireTime: detail.expireTime ? detail.expireTime.slice(0, 16) : "",
      remark: detail.remark ?? editForm.remark,
      packNo: detail.packNo,
      fileCount: detail.fileCount,
      totalSize: detail.totalSize
    });
    activePackId.value = newId;
    await loadPackFiles(newId);
  }
};

const handleDelete = async (row: PatientPack) => {
  await ElMessageBox.confirm(`确认删除资料包 ${row.packNo} 吗？`, "提示", { type: "warning" });
  await deletePatientPack(row.id);
  ElMessage.success("删除成功");
  if (activePackId.value === row.id) {
    resetAttachmentState();
  }
  handleSearch();
};

const handleView = async (row: PatientPack) => {
  detailDrawerVisible.value = true;
  packFiles.value = [];
  activePackId.value = row.id;
  await refreshPackDetail(row.id);
  detailPatient.value = null;
  if (detailData.value?.caseId) {
    try {
      const patientDetail = unwrap<PatientCaseDetail>(await getPatientCaseDetail(detailData.value.caseId));
      detailPatient.value = patientDetail;
    } catch (error) {
      detailPatient.value = null;
    }
  }
  await loadPackFiles(row.id);
};

const request = async params => {
  const payload = {
    ...searchForm,
    ...params,
    caseId: searchForm.caseId ? Number(searchForm.caseId) : undefined
  };
  const page = unwrap<PagedResult<PatientPack>>(await getPatientPackPage(payload));
  return page;
};

onMounted(async () => {
  const packStatus = unwrap<any[]>(await getDictionaryDataByCode("RC_PACK_STATUS"));
  dictSources.packStatus = packStatus ?? [];
  const route = useRoute();
  const caseIdFromQuery = Number(route.query.caseId || 0);
  if (caseIdFromQuery) {
    searchForm.caseId = caseIdFromQuery;
    handleSearch();
  }
});

const handleDetailClosed = () => {
  detailData.value = null;
  detailPatient.value = null;
  resetAttachmentState();
};

const handleEditClosed = () => {
  if (!detailDrawerVisible.value) {
    resetAttachmentState();
  }
};
</script>

<template>
  <div class="patient-pack-page">
    <el-card shadow="never">
      <vxe-form
        :data="searchForm"
        :items="[
          {
            field: 'caseId',
            title: '患者ID',
            span: 6,
            itemRender: { name: '$input', props: { placeholder: '患者ID' } }
          },
          {
            field: 'packStatus',
            title: '状态',
            span: 6,
            itemRender: {
              name: '$select',
              props: {
                options: [{ label: '全部', value: '' }, ...dictSources.packStatus.map(item => ({ label: item.name, value: item.code }))]
              }
            }
          },
          {
            field: 'keyword',
            title: '关键字',
            span: 6,
            itemRender: { name: '$input', props: { placeholder: '资料包编号/备注' } }
          },
          {
            span: 6,
            itemRender: {
              name: '$buttons',
              children: [
                {
                  props: { type: 'submit', icon: 'vxe-icon-search', content: '查询', status: 'primary' },
                  events: { click: handleSearch }
                },
                {
                  props: { type: 'reset', icon: 'vxe-icon-undo', content: '重置' },
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
      :title="editForm.id ? '编辑资料包' : '新建资料包'"
      width="640px"
      @closed="handleEditClosed"
    >
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="120px">
        <el-form-item label="患者" prop="caseId">
          <el-select
            v-model="editForm.caseId"
            filterable
            remote
            clearable
            placeholder="请输入患者姓名或ID"
            :remote-method="fetchPatientOptions"
            :loading="patientLoading"
            style="width: 100%"
          >
            <el-option
              v-for="item in patientOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="资料包编号">
          <el-input :model-value="editForm.packNo || '保存后自动生成'" disabled />
        </el-form-item>
        <el-form-item label="状态" prop="packStatus">
          <el-select v-model="editForm.packStatus">
            <el-option v-for="item in dictSources.packStatus" :key="item.code" :label="item.name" :value="item.code" />
          </el-select>
        </el-form-item>
        <el-form-item label="过期时间">
          <el-date-picker v-model="editForm.expireTime" type="datetime" value-format="YYYY-MM-DDTHH:mm" />
        </el-form-item>
        <el-form-item label="文件数">
          <el-input :model-value="editForm.fileCount" disabled />
        </el-form-item>
        <el-form-item label="总大小(MB)">
          <el-input :model-value="formatSize(editForm.totalSize)" disabled />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="editForm.remark" type="textarea" :rows="3" />
        </el-form-item>
        <template v-if="editForm.id">
          <el-divider>附件</el-divider>
          <div class="pack-file-ops">
            <el-upload
              :show-file-list="false"
              :http-request="handleFileUpload"
              :disabled="uploadLoading"
            >
              <el-button type="primary" size="small" :loading="uploadLoading">上传文件</el-button>
            </el-upload>
            <el-select v-model="uploadMeta.imageType" placeholder="影像类型" style="margin-left:8px;width:140px">
              <el-option label="CT" value="CT" />
              <el-option label="MRI" value="MRI" />
              <el-option label="US" value="US" />
              <el-option label="CR" value="CR" />
              <el-option label="DOC" value="DOC" />
              <el-option label="OTHER" value="OTHER" />
            </el-select>
            <el-select v-model="uploadMeta.sourceSystem" placeholder="来源系统" style="margin-left:8px;width:160px">
              <el-option label="PACS" value="PACS" />
              <el-option label="EMR" value="EMR" />
              <el-option label="HIS" value="HIS" />
              <el-option label="LIS" value="LIS" />
              <el-option label="OTHER" value="OTHER" />
            </el-select>
          </div>
          <el-table :data="packFiles" border :loading="fileLoading">
            <el-table-column prop="fileName" label="文件名" min-width="200" />
            <el-table-column label="大小(MB)" min-width="120">
              <template #default="{ row }">{{ formatSize(row.fileSize) }}</template>
            </el-table-column>
            <el-table-column prop="createTime" label="上传时间" min-width="160" />
            <el-table-column label="操作" width="100" align="center">
              <template #default="{ row }">
                <el-button link type="danger" @click="handleDeleteFile(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </template>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-drawer
      v-model="detailDrawerVisible"
      title="资料包详情"
      size="45%"
      @closed="handleDetailClosed"
    >
      <el-descriptions v-if="detailData" border :column="2">
        <el-descriptions-item label="资料包编号">{{ detailData.packNo }}</el-descriptions-item>
        <el-descriptions-item label="患者ID">{{ detailData.caseId }}</el-descriptions-item>
        <el-descriptions-item label="患者姓名">{{ detailPatient?.patientName ?? "-" }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ packStatusMap.value?.[detailData.packStatus] ?? detailData.packStatus }}</el-descriptions-item>
        <el-descriptions-item label="文件数">{{ detailData.fileCount }}</el-descriptions-item>
        <el-descriptions-item label="总大小(MB)">{{ formatSize(detailData.totalSize) }}</el-descriptions-item>
        <el-descriptions-item label="同步任务ID">{{ detailData.syncTaskId ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="过期时间" :span="2">{{ detailData.expireTime }}</el-descriptions-item>
        <el-descriptions-item label="创建时间" :span="2">{{ detailData.createTime }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remark }}</el-descriptions-item>
      </el-descriptions>
      <el-divider>附件</el-divider>
      <div class="pack-file-ops">
        <el-upload
          v-if="detailData"
          :show-file-list="false"
          :http-request="handleFileUpload"
          :disabled="uploadLoading"
        >
          <el-button type="primary" size="small" :loading="uploadLoading">上传文件</el-button>
        </el-upload>
        <el-select v-model="uploadMeta.imageType" placeholder="影像类型" style="margin-left:8px;width:140px">
          <el-option label="CT" value="CT" />
          <el-option label="MRI" value="MRI" />
          <el-option label="US" value="US" />
          <el-option label="CR" value="CR" />
          <el-option label="DOC" value="DOC" />
          <el-option label="OTHER" value="OTHER" />
        </el-select>
        <el-select v-model="uploadMeta.sourceSystem" placeholder="来源系统" style="margin-left:8px;width:160px">
          <el-option label="PACS" value="PACS" />
          <el-option label="EMR" value="EMR" />
          <el-option label="HIS" value="HIS" />
          <el-option label="LIS" value="LIS" />
          <el-option label="OTHER" value="OTHER" />
        </el-select>
      </div>
      <el-table :data="packFiles" border :loading="fileLoading" style="margin-top: 12px">
        <el-table-column prop="fileName" label="文件名" min-width="200" />
        <el-table-column label="大小(MB)" min-width="120">
          <template #default="{ row }">{{ formatSize(row.fileSize) }}</template>
        </el-table-column>
        <el-table-column prop="createTime" label="上传时间" min-width="160" />
        <el-table-column label="操作" width="100" align="center">
          <template #default="{ row }">
            <el-button link type="danger" @click="handleDeleteFile(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-drawer>
  </div>
</template>

<style scoped>
.patient-pack-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.pack-file-ops {
  margin-bottom: 12px;
}
</style>
