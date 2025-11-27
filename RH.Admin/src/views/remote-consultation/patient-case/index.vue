<script setup lang="ts">
import { ref, reactive, computed, onMounted, h } from "vue";
import { ElMessage, ElMessageBox, FormInstance } from "element-plus";
import { VxeButton } from "vxe-pc-ui";
import { ReVxeGrid } from "@/components/ReVxeTable";
import { useRouter } from "vue-router";
import {
  getPatientCasePage,
  getPatientCaseDetail,
  createPatientCase,
  updatePatientCase,
  deletePatientCase
} from "@/api/rc/patientCase";
import { getDictionaryDataByCode } from "@/api/system/dictionary";
import type {
  PatientCaseQuery,
  PatientCaseSummary,
  PatientCaseDetail,
  PagedResult
} from "@/api/rc/types";

const unwrap = <T>(response: any): T => {
  if (response && typeof response === "object" && "data" in response) {
    return (response as { data: T }).data;
  }
  return response as T;
};

const gridRef = ref();
const searchForm = reactive<PatientCaseQuery>({
  pageIndex: 1,
  pageSize: 10,
  keyword: "",
  patientType: "",
  dataSource: ""
});

const dictSources = reactive({
  patientType: [] as any[],
  patientSex: [
    { code: "M", name: "男" },
    { code: "F", name: "女" }
  ],
  dataSource: [] as any[]
});

const patientTypeMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.patientType) ? dictSources.patientType : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});
const dataSourceMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.dataSource) ? dictSources.dataSource : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});

const columns = [
  { title: "患者ID", field: "id", minWidth: 120 },
  { title: "姓名", field: "patientName", minWidth: 110 },
  {
    title: "性别",
    field: "patientSex",
    minWidth: 80,
    slots: {
      default: ({ row }) => h("span", row.patientSex === "M" ? "男" : row.patientSex === "F" ? "女" : "-")
    }
  },
  {
    title: "患者类型",
    field: "patientType",
    minWidth: 110,
    slots: {
      default: ({ row }) => h("span", patientTypeMap.value?.[row.patientType] ?? row.patientType)
    }
  },
  { title: "联系电话", field: "phone", minWidth: 140 },
  {
    title: "数据来源",
    field: "dataSource",
    minWidth: 120,
    slots: {
      default: ({ row }) => h("span", dataSourceMap.value?.[row.dataSource] ?? row.dataSource)
    }
  },
  { title: "创建时间", field: "createTime", minWidth: 160 }
];

const tableActions = [
  {
    title: "操作",
    field: "operate",
    width: 220,
    fixed: "right",
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
  add: "remote.patientCase.add",
  edit: "remote.patientCase.edit",
  view: "remote.patientCase.view",
  delete: "remote.patientCase.delete"
};

const handleSearch = () => gridRef.value?.loadData();
const handleReset = () => {
  searchForm.keyword = "";
  searchForm.patientType = "";
  searchForm.dataSource = "";
  handleSearch();
};

const editDialogVisible = ref(false);
const editFormRef = ref<FormInstance>();
const editForm = reactive({
  id: undefined as number | undefined,
  patientName: "",
  patientSex: "",
  patientBirth: "",
  patientType: "",
  phone: "",
  idCard: "",
  contactAddress: "",
  gestationalWeeks: null as number | null,
  childAgeMonths: null as number | null,
  dataSource: "MANUAL",
  medicalSummary: "",
  allergies: ""
});

const editRules = {
  patientName: [{ required: true, message: "请输入姓名", trigger: "blur" }],
  patientSex: [{ required: true, message: "请选择性别", trigger: "change" }],
  patientType: [{ required: true, message: "请选择患者类型", trigger: "change" }]
};

const detailDrawerVisible = ref(false);
const detailData = ref<PatientCaseDetail | null>(null);
const detailLoading = ref(false);
const router = useRouter();
const gotoPackWithCase = () => {
  if (!detailData.value?.id) return;
  router.push({ path: "/remote/patient-pack", query: { caseId: detailData.value.id } });
};

const handleAdd = () => {
  Object.assign(editForm, {
    id: undefined,
    patientName: "",
    patientSex: "",
    patientBirth: "",
    patientType: "",
    phone: "",
    idCard: "",
    contactAddress: "",
    gestationalWeeks: null,
    childAgeMonths: null,
    dataSource: "MANUAL",
    medicalSummary: "",
    allergies: ""
  });
  editDialogVisible.value = true;
};

const handleEdit = async (row: PatientCaseSummary) => {
  const detail = unwrap<PatientCaseDetail>(await getPatientCaseDetail(row.id));
  Object.assign(editForm, {
    id: detail.id,
    patientName: detail.patientName,
    patientSex: detail.patientSex,
    patientBirth: detail.patientBirth ? detail.patientBirth.slice(0, 10) : "",
    patientType: detail.patientType,
    phone: detail.phone ?? "",
    idCard: detail.idCard ?? "",
    contactAddress: detail.contactAddress ?? "",
    gestationalWeeks: detail.gestationalWeeks ?? null,
    childAgeMonths: detail.childAgeMonths ?? null,
    dataSource: detail.dataSource ?? "MANUAL",
    medicalSummary: detail.medicalSummary ?? "",
    allergies: detail.allergies ?? ""
  });
  editDialogVisible.value = true;
};

const submitEdit = async () => {
  await editFormRef.value?.validate();
  const payload = {
    patientName: editForm.patientName,
    patientSex: editForm.patientSex,
    patientBirth: editForm.patientBirth ? new Date(editForm.patientBirth).toISOString() : null,
    patientType: editForm.patientType,
    phone: editForm.phone,
    idCard: editForm.idCard,
    contactAddress: editForm.contactAddress,
    gestationalWeeks: editForm.gestationalWeeks,
    childAgeMonths: editForm.childAgeMonths,
    dataSource: editForm.dataSource,
    medicalSummary: editForm.medicalSummary,
    allergies: editForm.allergies
  };
  if (editForm.id) {
    await updatePatientCase(editForm.id, payload);
  } else {
    await createPatientCase(payload);
  }
  ElMessage.success("保存成功");
  editDialogVisible.value = false;
  handleSearch();
};

const handleDelete = async (row: PatientCaseSummary) => {
  await ElMessageBox.confirm(`确定删除患者 ${row.patientName} 吗？`, "提示", { type: "warning" });
  await deletePatientCase(row.id);
  ElMessage.success("删除成功");
  handleSearch();
};

const handleView = async (row: PatientCaseSummary) => {
  detailDrawerVisible.value = true;
  detailLoading.value = true;
  try {
    detailData.value = unwrap<PatientCaseDetail>(await getPatientCaseDetail(row.id));
  } finally {
    detailLoading.value = false;
  }
};

const request = async params => {
  const page = unwrap<PagedResult<PatientCaseSummary>>(await getPatientCasePage({ ...searchForm, ...params }));
  return page;
};

onMounted(async () => {
  const [patientType, dataSource] = await Promise.all([
    getDictionaryDataByCode("RC_PATIENT_TYPE"),
    getDictionaryDataByCode("RC_DATA_SOURCE")
  ]);
  dictSources.patientType = unwrap<any[]>(patientType) ?? [];
  dictSources.dataSource = unwrap<any[]>(dataSource) ?? [];
});
</script>

<template>
  <div class="patient-case-page">
    <el-card shadow="never">
      <vxe-form
        :data="searchForm"
        :items="[
          {
            field: 'keyword',
            title: '关键字',
            span: 6,
            itemRender: { name: '$input', props: { placeholder: '姓名/证件号/电话' } }
          },
          {
            field: 'patientType',
            title: '患者类型',
            span: 6,
            itemRender: {
              name: '$select',
              props: {
                options: [{ label: '全部', value: '' }, ...dictSources.patientType.map(item => ({ label: item.name, value: item.code }))]
              }
            }
          },
          {
            field: 'dataSource',
            title: '数据来源',
            span: 6,
            itemRender: {
              name: '$select',
              props: {
                options: [{ label: '全部', value: '' }, ...dictSources.dataSource.map(item => ({ label: item.name, value: item.code }))]
              }
            }
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

    <el-dialog v-model="editDialogVisible" :title="editForm.id ? '编辑患者' : '新建患者'" width="700px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="120px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="姓名" prop="patientName">
              <el-input v-model="editForm.patientName" placeholder="请输入姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="性别" prop="patientSex">
              <el-select v-model="editForm.patientSex" placeholder="请选择性别">
                <el-option label="男" value="M" />
                <el-option label="女" value="F" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="出生日期">
              <el-date-picker v-model="editForm.patientBirth" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="患者类型" prop="patientType">
              <el-select v-model="editForm.patientType" placeholder="患者类型">
                <el-option v-for="item in dictSources.patientType" :key="item.code" :label="item.name" :value="item.code" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="联系电话">
              <el-input v-model="editForm.phone" placeholder="联系电话" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="证件号">
              <el-input v-model="editForm.idCard" placeholder="身份证/病历号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="联系地址">
          <el-input v-model="editForm.contactAddress" placeholder="联系地址" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="孕周(周)">
              <el-input-number v-model="editForm.gestationalWeeks" :precision="1" :step="0.5" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="儿童月龄">
              <el-input-number v-model="editForm.childAgeMonths" :min="0" :max="120" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="数据来源">
          <el-select v-model="editForm.dataSource">
            <el-option v-for="item in dictSources.dataSource" :key="item.code" :label="item.name" :value="item.code" />
          </el-select>
        </el-form-item>
        <el-form-item label="病情概述">
          <el-input v-model="editForm.medicalSummary" type="textarea" :rows="3" />
        </el-form-item>
        <el-form-item label="过敏史">
          <el-input v-model="editForm.allergies" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="detailDrawerVisible" size="50%" title="患者详情" :destroy-on-close="true">
      <el-skeleton v-if="detailLoading" :rows="8" animated />
      <el-descriptions v-else-if="detailData" border :column="2">
        <el-descriptions-item label="姓名">{{ detailData.patientName }}</el-descriptions-item>
        <el-descriptions-item label="性别">{{ detailData.patientSex === "M" ? "男" : detailData.patientSex === "F" ? "女" : "-" }}</el-descriptions-item>
        <el-descriptions-item label="患者类型">{{ patientTypeMap.value?.[detailData.patientType] ?? detailData.patientType }}</el-descriptions-item>
        <el-descriptions-item label="数据来源">{{ dataSourceMap.value?.[detailData.dataSource] ?? detailData.dataSource }}</el-descriptions-item>
        <el-descriptions-item label="出生日期">{{ detailData.patientBirth }}</el-descriptions-item>
        <el-descriptions-item label="电话">{{ detailData.phone }}</el-descriptions-item>
        <el-descriptions-item label="证件号">{{ detailData.idCard }}</el-descriptions-item>
        <el-descriptions-item label="孕周">{{ detailData.gestationalWeeks ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="儿童月龄">{{ detailData.childAgeMonths ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="地址" :span="2">{{ detailData.contactAddress }}</el-descriptions-item>
        <el-descriptions-item label="病情概述" :span="2">{{ detailData.medicalSummary }}</el-descriptions-item>
        <el-descriptions-item label="过敏史" :span="2">{{ detailData.allergies }}</el-descriptions-item>
      </el-descriptions>
      <div style="margin-top: 12px">
        <el-button type="primary" @click="gotoPackWithCase">一键打包患者数据</el-button>
      </div>
    </el-drawer>
  </div>
</template>

<style scoped>
.patient-case-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>

