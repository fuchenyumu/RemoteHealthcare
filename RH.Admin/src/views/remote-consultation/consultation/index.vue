<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, h, watch } from "vue";
import dayjs from "dayjs";
import { ElMessage, ElMessageBox, FormInstance, ElTag } from "element-plus";
import { useRouter } from "vue-router";
import { VxeButton } from "vxe-pc-ui";
import { ReVxeGrid } from "@/components/ReVxeTable";
import {
  getConsultationPage,
  getConsultationDetail,
  createConsultation,
  updateConsultation,
  deleteConsultation,
  auditConsultation,
  scheduleConsultation,
  updateConsultationRtc,
  startConsultation,
  finishConsultation,
  closeConsultation
} from "@/api/rc/consultation";
import { getAttachmentPage, deleteAttachment } from "@/api/rc/attachment";
import {
  getMemberPage,
  deleteMember,
  createMember,
  updateMember
} from "@/api/rc/member";
import { getTimelinePage } from "@/api/rc/timeline";
import {
  getReportPage,
  saveReport,
  updateReport,
  signReport
} from "@/api/rc/report";
import { getPatientCasePage, getPatientCaseDetail } from "@/api/rc/patientCase";
import { getPatientPackPage } from "@/api/rc/patientPack";
import { getPageList as getUserPageList } from "@/api/system/user";
import { ReOrganizationTreeSelect } from "@/components/ReOrganizationTreeSelect";
import { startWorkflow, auditingWorkflow } from "@/api/workflow/instance";
import { getDefinitions } from "@/api/workflow/definition";
import { createConnectionAsync } from "@/utils/signalr";
import type { HubConnection } from "@microsoft/signalr";
import { getDictionaryDataByCode } from "@/api/system/dictionary";
import { getConsultationStatsOverview } from "@/api/rc/consultation";
import MedicalRtcConference from "../rtc/MedicalRtcConference.vue";
import { useUserStoreHook } from "@/store/modules/user";
import type {
  ConsultationSummary,
  ConsultationDetail,
  ConsultationAttachment,
  ConsultationMember,
  ConsultationTimeline,
  ConsultationReport,
  ConsultationQuery,
  PagedResult
} from "@/api/rc/types";

const gridRef = ref();
const detailDrawerVisible = ref(false);
const detailLoading = ref(false);
const detailData = ref<ConsultationDetail | null>(null);
const signalrConn = ref<HubConnection | null>(null);

const attachmentData = ref<ConsultationAttachment[]>([]);
const memberData = ref<ConsultationMember[]>([]);
const timelineData = ref<ConsultationTimeline[]>([]);
const reportData = ref<ConsultationReport | null>(null);

const dictSources = reactive({
  consultationStatus: [] as any[],
  emergencyLevel: [] as any[],
  packStatus: [] as any[],
  memberRole: [] as any[],
  memberStatus: [] as any[]
});

const overviewStats = ref<{ total: number; today: number; pendingReview: number; finished: number } | null>(null);
const loadOverviewStats = async () => {
  try {
    const res = await getConsultationStatsOverview();
    const data = unwrap<{ total: number; today: number; pendingReview: number; finished: number }>(res);
    overviewStats.value = data;
  } catch (e) {}
};

const unwrap = <T,>(response: any): T => {
  if (response && typeof response === "object" && "data" in response) {
    return (response as { data: T }).data;
  }
  return response as T;
};

const statusMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.consultationStatus)
    ? dictSources.consultationStatus
    : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});
const statusTagType = (code: string) => {
  switch (code) {
    case "PENDING_REVIEW":
      return "warning";
    case "WAIT_SCHEDULE":
      return "info";
    case "SCHEDULED":
      return "success";
    case "IN_PROGRESS":
      return "success";
    case "FINISHED":
      return "success";
    case "CLOSED":
      return "danger";
    default:
      return "info";
  }
};
const emergencyMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.emergencyLevel)
    ? dictSources.emergencyLevel
    : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});
const memberRoleMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.memberRole)
    ? dictSources.memberRole
    : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});
const memberStatusMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.memberStatus)
    ? dictSources.memberStatus
    : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});

const searchForm = reactive<ConsultationQuery>({
  pageIndex: 1,
  pageSize: 10,
  keyword: "",
  consultationStatus: "",
  emergencyLevel: ""
});

const initialEditForm = () => ({
  id: undefined as number | undefined,
  caseId: undefined as number | undefined,
  packId: undefined as number | undefined,
  applyOrgId: undefined as number | undefined,
  applyDoctorId: undefined as number | undefined,
  targetOrgId: undefined as number | undefined,
  targetDepartment: "",
  targetExpertId: undefined as number | undefined,
  purpose: "",
  emergencyLevel: "",
  desiredStartTime: "",
  desiredEndTime: ""
});

const editDialogVisible = ref(false);
const editFormRef = ref<FormInstance>();
const editForm = reactive(initialEditForm());
const editRules = {
  caseId: [{ required: true, message: "请选择患者", trigger: "change" }],
  applyOrgId: [
    { required: true, message: "请选择申请机构", trigger: "change" }
  ],
  applyDoctorId: [
    { required: true, message: "请选择申请医生", trigger: "change" }
  ],
  purpose: [{ required: true, message: "请输入会诊目的", trigger: "blur" }],
  emergencyLevel: [
    { required: true, message: "请选择紧急程度", trigger: "change" }
  ]
};

const auditDialogVisible = ref(false);
const auditForm = reactive({ consultationId: 0, approved: true, comment: "" });

const scheduleDialogVisible = ref(false);
const scheduleForm = reactive({
  consultationId: 0,
  scheduledStartTime: "",
  scheduledEndTime: "",
  meetingRoomNo: ""
});

const rtcDialogVisible = ref(false);
const rtcForm = reactive({
  consultationId: 0,
  meetingRoomNo: "",
  rtcChannelId: "",
  rtcVendor: ""
});

const reportDrawerVisible = ref(false);
const reportForm = reactive({
  consultationId: 0,
  summary: "",
  diagnosis: "",
  treatmentAdvice: "",
  followUpPlan: "",
  reportStatus: "DRAFT"
});

const rtcDrawerVisible = ref(false);
const rtcConsultationId = ref<number | null>(null);

const columns = [
  {
    title: "会诊编号",
    field: "id",
    minWidth: 120
  },
  {
    title: "患者姓名",
    field: "patientName",
    minWidth: 100
  },
  {
    title: "紧急程度",
    field: "emergencyLevel",
    minWidth: 100,
    slots: {
      default: ({ row }) =>
        h(
          "span",
          emergencyMap.value?.[row.emergencyLevel] ?? row.emergencyLevel
        )
    }
  },
  {
    title: "会诊状态",
    field: "consultationStatus",
    minWidth: 120,
    slots: {
      default: ({ row }) =>
        h(
          ElTag,
          { type: statusTagType(row.consultationStatus) },
          () =>
            statusMap.value?.[row.consultationStatus] ?? row.consultationStatus
        )
    }
  },
  {
    title: "申请医生",
    field: "applyDoctorName",
    minWidth: 120
  },
  {
    title: "目标医院",
    field: "targetOrgName",
    minWidth: 160
  },
  {
    title: "排期时间",
    field: "scheduledStartTime",
    minWidth: 180,
    slots: {
      default: ({ row }) =>
        row.scheduledStartTime
          ? `${row.scheduledStartTime.slice(0, 16)} ~ ${row.scheduledEndTime?.slice(0, 16) ?? ""}`
          : "-"
    }
  }
];

const tableActions = [
  {
    title: "操作",
    field: "operate",
    width: 320,
    align: "center",
    fixed: "right",
    slots: {
      default: ({ row }) => {
        return [
          h(VxeButton, {
            mode: "text",
            status: "primary",
            icon: "vxe-icon-file-txt",
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
            status: "warning",
            content: "审核",
            disabled: row.consultationStatus !== "PENDING_REVIEW",
            onClick: () => openAudit(row)
          }),
          h(VxeButton, {
            mode: "text",
            status: "warning",
            content: "排期",
            disabled: !["WAIT_SCHEDULE", "SCHEDULED"].includes(
              row.consultationStatus
            ),
            onClick: () => openSchedule(row)
          }),
          h(VxeButton, {
            mode: "text",
            status: "success",
            content: "开始",
            disabled: row.consultationStatus !== "SCHEDULED",
            onClick: () => confirmStart(row)
          }),
          h(VxeButton, {
            mode: "text",
            status: "success",
            content: "结束",
            disabled: row.consultationStatus !== "IN_PROGRESS",
            onClick: () => openFinish(row)
          }),
          h(VxeButton, {
            mode: "text",
            status: "danger",
            content: "关闭",
            disabled: row.consultationStatus === "CLOSED",
            onClick: () => openClose(row)
          }),
          h(VxeButton, {
            mode: "text",
            status: "danger",
            icon: "vxe-icon-delete",
            content: "删除",
            onClick: () => handleDelete(row)
          }),
          h(VxeButton, {
            mode: "text",
            status: "primary",
            content: "音视频",
            onClick: () => openRtcConsole(row)
          })
        ];
      }
    }
  }
];

const handleSearch = () => {
  gridRef.value?.loadData();
};

const handleReset = () => {
  searchForm.keyword = "";
  searchForm.consultationStatus = "";
  searchForm.emergencyLevel = "";
  handleSearch();
};

const functions = {
  add: "remote.consultation.add",
  edit: "remote.consultation.edit",
  view: "remote.consultation.view",
  delete: "remote.consultation.delete"
};

const handleAdd = () => {
  Object.assign(editForm, initialEditForm());
  fetchPatientOptions("");
  fetchPackOptions("");
  fetchUserOptions("", "apply");
  fetchUserOptions("", "expert");
  editDialogVisible.value = true;
};

const handleEdit = async (row: ConsultationSummary) => {
  const detail = unwrap<ConsultationDetail>(
    await getConsultationDetail(row.id)
  );
  Object.assign(editForm, {
    id: detail.id,
    caseId: detail.caseId,
    packId: detail.packId,
    applyOrgId: detail.applyOrgId,
    applyDoctorId: detail.applyDoctorId,
    targetOrgId: detail.targetOrgId,
    targetDepartment: detail.targetDepartment,
    targetExpertId: detail.targetExpertId,
    purpose: detail.purpose,
    emergencyLevel: detail.emergencyLevel,
    desiredStartTime: detail.desiredStartTime
      ? detail.desiredStartTime.slice(0, 16)
      : "",
    desiredEndTime: detail.desiredEndTime
      ? detail.desiredEndTime.slice(0, 16)
      : ""
  });
  await fetchPatientOptions("");
  await ensurePatientOption(detail.caseId);
  await fetchPackOptions("");
  await fetchUserOptions("", "apply");
  await fetchUserOptions("", "expert");
  editDialogVisible.value = true;
};

const openRtcConsole = (row: ConsultationSummary) => {
  rtcConsultationId.value = row.id;
  rtcDrawerVisible.value = true;
};

const submitEdit = async () => {
  await editFormRef.value?.validate();
  const payload = {
    ...editForm,
    desiredStartTime: editForm.desiredStartTime
      ? new Date(editForm.desiredStartTime).toISOString()
      : null,
    desiredEndTime: editForm.desiredEndTime
      ? new Date(editForm.desiredEndTime).toISOString()
      : null,
    snapshot: {
      caseId: editForm.caseId,
      packId: editForm.packId,
      applyOrgId: editForm.applyOrgId,
      applyDoctorId: editForm.applyDoctorId,
      targetOrgId: editForm.targetOrgId,
      targetDepartment: editForm.targetDepartment,
      targetExpertId: editForm.targetExpertId,
      purpose: editForm.purpose,
      emergencyLevel: editForm.emergencyLevel,
      desiredStartTime: editForm.desiredStartTime || null,
      desiredEndTime: editForm.desiredEndTime || null
    }
  };
  if (editForm.id) {
    await updateConsultation(editForm.id, payload);
  } else {
    const newId = unwrap<number>(await createConsultation(payload));
    try {
      const defs = await getDefinitions();
      const def = Array.isArray(defs) ? defs[0] : null;
      if (def && newId) {
        await startWorkflow(def.id, {
          businessType: "Consultation",
          businessId: newId,
          title: payload.purpose,
          emergencyLevel: payload.emergencyLevel,
          applyDoctorId: payload.applyDoctorId
        });
      }
    } catch (e) {}
  }
  ElMessage.success("保存成功");
  editDialogVisible.value = false;
  handleSearch();
};

const handleDelete = async (row: ConsultationSummary) => {
  await ElMessageBox.confirm(`确定删除会诊 ${row.id} 吗？`, "提示", {
    type: "warning"
  });
  await deleteConsultation(row.id);
  ElMessage.success("删除成功");
  handleSearch();
};

const openAudit = (row: ConsultationSummary) => {
  auditForm.consultationId = row.id;
  auditForm.approved = true;
  auditForm.comment = "";
  auditDialogVisible.value = true;
};

const submitAudit = async () => {
  await auditConsultation(auditForm.consultationId, {
    approved: auditForm.approved,
    comment: auditForm.comment
  });
  try {
    const detail = unwrap<ConsultationDetail>(
      await getConsultationDetail(auditForm.consultationId)
    );
    if (detail.instanceId) {
      await auditingWorkflow(detail.instanceId, {
        approved: auditForm.approved,
        comment: auditForm.comment
      });
    }
  } catch (e) {}
  ElMessage.success("审核完成");
  auditDialogVisible.value = false;
  handleSearch();
};

const openSchedule = (row: ConsultationSummary) => {
  scheduleForm.consultationId = row.id;
  scheduleForm.scheduledStartTime =
    row.scheduledStartTime?.slice(0, 16) ?? dayjs().format("YYYY-MM-DDTHH:mm");
  scheduleForm.scheduledEndTime =
    row.scheduledEndTime?.slice(0, 16) ??
    dayjs().add(1, "hour").format("YYYY-MM-DDTHH:mm");
  scheduleForm.meetingRoomNo = row.meetingRoomNo ?? "";
  scheduleDialogVisible.value = true;
};

const submitSchedule = async () => {
  await scheduleConsultation(
    scheduleForm.consultationId,
    {
      scheduledStartTime: new Date(scheduleForm.scheduledStartTime).toISOString(),
      scheduledEndTime: new Date(scheduleForm.scheduledEndTime).toISOString(),
      meetingRoomNo: scheduleForm.meetingRoomNo
    }
  );
  ElMessage.success("排期完成");
  scheduleDialogVisible.value = false;
  handleSearch();
};

const openRtc = (detail: ConsultationDetail) => {
  rtcForm.consultationId = detail.id;
  rtcForm.meetingRoomNo = detail.meetingRoomNo ?? "";
  rtcForm.rtcChannelId = detail.rtcChannelId ?? "";
  rtcForm.rtcVendor = detail.rtcVendor ?? "";
  rtcDialogVisible.value = true;
};

const submitRtc = async () => {
  await updateConsultationRtc({
    consultationId: rtcForm.consultationId,
    meetingRoomNo: rtcForm.meetingRoomNo,
    rtcChannelId: rtcForm.rtcChannelId,
    rtcVendor: rtcForm.rtcVendor
  });
  ElMessage.success("已更新音视频参数");
  rtcDialogVisible.value = false;
  if (detailData.value) await loadDetail(detailData.value.id);
};

const confirmStart = async (row: ConsultationSummary) => {
  await ElMessageBox.confirm("确认开始该会诊吗？", "提示", { type: "warning" });
  await startConsultation(row.id);
  ElMessage.success("会诊已开始");
  handleSearch();
};

const openFinish = (row: ConsultationSummary) => {
  finishForm.consultationId = row.id;
  finishForm.summary = "";
  finishDialogVisible.value = true;
};

const finishDialogVisible = ref(false);
const finishForm = reactive({ consultationId: 0, summary: "" });
const submitFinish = async () => {
  await finishConsultation(finishForm.consultationId, {
    summary: finishForm.summary
  });
  ElMessage.success("会诊已结束");
  finishDialogVisible.value = false;
  handleSearch();
};

const closeDialogVisible = ref(false);
const closeForm = reactive({ consultationId: 0, closeReason: "" });
const openClose = (row: ConsultationSummary) => {
  closeForm.consultationId = row.id;
  closeForm.closeReason = "";
  closeDialogVisible.value = true;
};
const submitClose = async () => {
  await closeConsultation(closeForm.consultationId, {
    closeReason: closeForm.closeReason
  });
  ElMessage.success("会诊已关闭");
  closeDialogVisible.value = false;
  handleSearch();
};

const handleView = async (row: ConsultationSummary) => {
  detailDrawerVisible.value = true;
  await loadDetail(row.id);
  await openSignalR(row.id);
};

const loadDetail = async (id: number) => {
  detailLoading.value = true;
  try {
    detailData.value = unwrap<ConsultationDetail>(
      await getConsultationDetail(id)
    );
    await Promise.all([
      loadAttachments(id),
      loadMembers(id),
      loadTimeline(id),
      loadReport(id)
    ]);
  } finally {
    detailLoading.value = false;
  }
};

const loadAttachments = async (consultationId: number) => {
  const result = unwrap<PagedResult<ConsultationAttachment>>(
    await getAttachmentPage({ consultationId, pageIndex: 1, pageSize: 50 })
  );
  attachmentData.value = result?.items ?? [];
};

const loadMembers = async (consultationId: number) => {
  const result = unwrap<PagedResult<ConsultationMember>>(
    await getMemberPage({ consultationId, pageIndex: 1, pageSize: 50 })
  );
  memberData.value = result?.items ?? [];
};

const loadTimeline = async (consultationId: number) => {
  const result = unwrap<PagedResult<ConsultationTimeline>>(
    await getTimelinePage({ consultationId, pageIndex: 1, pageSize: 100 })
  );
  timelineData.value = result?.items ?? [];
};

const loadReport = async (consultationId: number) => {
  const list = unwrap<PagedResult<ConsultationReport>>(
    await getReportPage({ consultationId, reportStatus: "" })
  );
  reportData.value = list?.items?.[0] ?? null;
};

const openSignalR = async (consultationId: number) => {
  try {
    if (signalrConn.value) {
      await signalrConn.value.stop();
      signalrConn.value = null;
    }
    const conn = await createConnectionAsync(`/consultation/${consultationId}`);
    conn.on("ConsultationStatusChanged", async payload => {
      if (detailData.value && payload?.consultationId === detailData.value.id) {
        await loadDetail(detailData.value.id);
        ElMessage.info("会诊状态已更新");
      }
    });
    conn.on("MemberJoined", async payload => {
      if (detailData.value && payload?.consultationId === detailData.value.id) {
        await loadMembers(detailData.value.id);
      }
    });
    conn.on("MemberLeft", async payload => {
      if (detailData.value && payload?.consultationId === detailData.value.id) {
        await loadMembers(detailData.value.id);
      }
    });
    conn.on("ReportSigned", async payload => {
      if (detailData.value && payload?.consultationId === detailData.value.id) {
        await loadReport(detailData.value.id);
        ElMessage.success("报告已签署");
      }
    });
    conn.on("AttachmentAdded", async payload => {
      if (detailData.value && payload?.consultationId === detailData.value.id) {
        await loadAttachments(detailData.value.id);
      }
    });
    conn.on("AttachmentDeleted", async payload => {
      if (detailData.value && payload?.consultationId === detailData.value.id) {
        await loadAttachments(detailData.value.id);
      }
    });
    signalrConn.value = conn;
  } catch (e) {}
};

watch(detailDrawerVisible, async v => {
  if (!v && signalrConn.value) {
    try {
      await signalrConn.value.stop();
    } catch (e) {}
    signalrConn.value = null;
  }
});

onUnmounted(async () => {
  if (signalrConn.value) {
    try {
      await signalrConn.value.stop();
    } catch (e) {}
    signalrConn.value = null;
  }
});

const openReportEditor = () => {
  if (!detailData.value) return;
  reportForm.consultationId = detailData.value.id;
  if (reportData.value) {
    Object.assign(reportForm, {
      consultationId: reportData.value.consultationId,
      summary: reportData.value.summary ?? "",
      diagnosis: reportData.value.diagnosis ?? "",
      treatmentAdvice: reportData.value.treatmentAdvice ?? "",
      followUpPlan: reportData.value.followUpPlan ?? "",
      reportStatus: reportData.value.reportStatus ?? "DRAFT"
    });
  } else {
    Object.assign(reportForm, {
      consultationId: detailData.value.id,
      summary: "",
      diagnosis: "",
      treatmentAdvice: "",
      followUpPlan: "",
      reportStatus: "DRAFT"
    });
  }
  reportDrawerVisible.value = true;
};

const submitReport = async () => {
  if (reportData.value && reportData.value.id) {
    await updateReport(reportData.value.id, reportForm);
  } else {
    await saveReport(reportForm);
  }
  ElMessage.success("报告已保存");
  reportDrawerVisible.value = false;
  await loadReport(reportForm.consultationId);
};

const submitSignReport = async (signerId: number) => {
  if (!detailData.value) return;
  await signReport({ consultationId: detailData.value.id, signerId });
  ElMessage.success("报告已签署");
  await loadReport(detailData.value.id);
};

const removeAttachment = async (attachment: ConsultationAttachment) => {
  await ElMessageBox.confirm("确定删除该附件吗？", "提示", { type: "warning" });
  await deleteAttachment(attachment.id);
  if (detailData.value) await loadAttachments(detailData.value.id);
};

const removeMember = async (member: ConsultationMember) => {
  await ElMessageBox.confirm("确定移除该成员吗？", "提示", { type: "warning" });
  await deleteMember(member.id);
  if (detailData.value) await loadMembers(detailData.value.id);
};

const memberDialogVisible = ref(false);
const memberForm = reactive({
  id: undefined as number | undefined,
  consultationId: 0,
  userId: undefined as number | undefined,
  orgId: undefined as number | undefined,
  roleCode: "",
  joinStatus: "PENDING",
  joinTime: ""
});

const openMemberEditor = (member?: ConsultationMember) => {
  if (!detailData.value) return;
  memberForm.consultationId = detailData.value.id;
  if (member) {
    Object.assign(memberForm, {
      id: member.id,
      userId: member.userId,
      orgId: member.orgId,
      roleCode: member.roleCode,
      joinStatus: member.joinStatus,
      joinTime: member.joinTime ? member.joinTime.slice(0, 16) : ""
    });
  } else {
    Object.assign(memberForm, {
      id: undefined,
      userId: undefined,
      orgId: undefined,
      roleCode: "",
      joinStatus: "PENDING",
      joinTime: ""
    });
  }
  memberDialogVisible.value = true;
};

const submitMember = async () => {
  const payload = {
    consultationId: memberForm.consultationId,
    userId: memberForm.userId,
    orgId: memberForm.orgId,
    roleCode: memberForm.roleCode,
    joinStatus: memberForm.joinStatus,
    joinTime: memberForm.joinTime
      ? new Date(memberForm.joinTime).toISOString()
      : null
  };
  if (memberForm.id) {
    await updateMember(memberForm.id, payload);
  } else {
    await createMember(payload);
  }
  memberDialogVisible.value = false;
  if (detailData.value) await loadMembers(detailData.value.id);
};

const consultationRequest = async params => {
  const page = unwrap<PagedResult<ConsultationSummary>>(
    await getConsultationPage({ ...searchForm, ...params })
  );
  return page;
};

const userStore = useUserStoreHook();
const currentUserId = computed(() => userStore.getCurrentUser?.id ?? 0);
const router = useRouter();
const gotoRtcDemo = (detail: ConsultationDetail) => {
  router.push({
    path: "/remote/rtc-demo",
    query: { consultationId: detail.id }
  });
};

const patientOptions = ref<Array<{ label: string; value: number }>>([]);
const patientLoading = ref(false);
const packOptions = ref<Array<{ label: string; value: number }>>([]);
const packLoading = ref(false);
const applyDoctorOptions = ref<Array<{ label: string; value: number }>>([]);
const applyDoctorLoading = ref(false);
const targetExpertOptions = ref<Array<{ label: string; value: number }>>([]);
const targetExpertLoading = ref(false);

const fetchPatientOptions = async (keyword = "") => {
  patientLoading.value = true;
  try {
    const page = unwrap<PagedResult<any>>(
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
  const exists = patientOptions.value.some(o => o.value === caseId);
  if (exists) return;
  try {
    const patient = unwrap<any>(await getPatientCaseDetail(caseId));
    const label = patient?.patientName
      ? `${patient.patientName}（ID:${caseId}）`
      : `ID:${caseId}`;
    patientOptions.value = [{ value: caseId, label }, ...patientOptions.value];
  } catch (e) {
    patientOptions.value = [
      { value: caseId, label: `ID:${caseId}` },
      ...patientOptions.value
    ];
  }
};

const fetchPackOptions = async (keyword = "") => {
  packLoading.value = true;
  try {
    const page = unwrap<PagedResult<any>>(
      await getPatientPackPage({ pageIndex: 1, pageSize: 20, keyword })
    );
    const items = page?.items ?? [];
    packOptions.value = items.map(item => ({
      value: item.id,
      label: `${item.packNo}（ID:${item.id}）`
    }));
  } finally {
    packLoading.value = false;
  }
};

const fetchUserOptions = async (
  keyword = "",
  target: "apply" | "expert" = "apply"
) => {
  const loading = target === "apply" ? applyDoctorLoading : targetExpertLoading;
  const options = target === "apply" ? applyDoctorOptions : targetExpertOptions;
  loading.value = true;
  try {
    const page = await getUserPageList({ pageIndex: 1, pageSize: 20, keyword });
    const data = unwrap<PagedResult<any>>(page);
    const items = data?.items ?? [];
    options.value = items.map(item => ({
      value: item.id,
      label: `${item.name}（账号:${item.account}）`
    }));
  } finally {
    loading.value = false;
  }
};

onMounted(async () => {
  const [status, emergency, packStatus, memberRole, memberStatus] =
    await Promise.all([
      getDictionaryDataByCode("RC_CONSULT_STATUS"),
      getDictionaryDataByCode("RC_EMERGENCY_LEVEL"),
      getDictionaryDataByCode("RC_PACK_STATUS"),
      getDictionaryDataByCode("RC_MEMBER_ROLE"),
      getDictionaryDataByCode("RC_MEMBER_STATUS")
    ]);
  dictSources.consultationStatus = unwrap<any[]>(status) ?? [];
  dictSources.emergencyLevel = unwrap<any[]>(emergency) ?? [];
  dictSources.packStatus = unwrap<any[]>(packStatus) ?? [];
  dictSources.memberRole = unwrap<any[]>(memberRole) ?? [];
  dictSources.memberStatus = unwrap<any[]>(memberStatus) ?? [];
  await loadOverviewStats();
});
</script>

<template>
  <div class="remote-consultation-page">
    <el-card shadow="never">
      <div class="stats-row">
        <div class="stat-item">
          <div class="stat-label">总会诊数</div>
          <div class="stat-value">{{ overviewStats?.total ?? '-' }}</div>
        </div>
        <div class="stat-item">
          <div class="stat-label">今日新增</div>
          <div class="stat-value">{{ overviewStats?.today ?? '-' }}</div>
        </div>
        <div class="stat-item">
          <div class="stat-label">待审核</div>
          <div class="stat-value">{{ overviewStats?.pendingReview ?? '-' }}</div>
        </div>
        <div class="stat-item">
          <div class="stat-label">已完成</div>
          <div class="stat-value">{{ overviewStats?.finished ?? '-' }}</div>
        </div>
        <el-button type="primary" size="small" @click="loadOverviewStats">刷新</el-button>
      </div>
    </el-card>
    <el-card shadow="never" class="query-card">
      <vxe-form
        :data="searchForm"
        :items="[
          {
            field: 'keyword',
            title: '关键字',
            span: 6,
            itemRender: {
              name: '$input',
              props: { placeholder: '患者/目的/科室' }
            }
          },
          {
            field: 'consultationStatus',
            title: '会诊状态',
            span: 6,
            itemRender: {
              name: '$select',
              props: {
                options: [
                  { label: '全部', value: '' },
                  ...dictSources.consultationStatus.map(d => ({
                    label: d.name,
                    value: d.code
                  }))
                ]
              }
            }
          },
          {
            field: 'emergencyLevel',
            title: '紧急程度',
            span: 6,
            itemRender: {
              name: '$select',
              props: {
                options: [
                  { label: '全部', value: '' },
                  ...dictSources.emergencyLevel.map(d => ({
                    label: d.name,
                    value: d.code
                  }))
                ]
              }
            }
          },
          {
            span: 6,
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
        :request="consultationRequest"
        :functions="functions"
        :searchParams="searchForm"
        @handleAdd="handleAdd"
      />
    </el-card>

    <el-dialog
      v-model="editDialogVisible"
      :title="editForm.id ? '编辑会诊' : '新建会诊'"
      width="600px"
      destroy-on-close
    >
      <el-form
        ref="editFormRef"
        :model="editForm"
        :rules="editRules"
        label-width="120px"
      >
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
        <el-form-item label="资料包">
          <el-select
            v-model="editForm.packId"
            filterable
            remote
            clearable
            placeholder="请输入资料包编号或ID"
            :remote-method="fetchPackOptions"
            :loading="packLoading"
            style="width: 100%"
          >
            <el-option
              v-for="item in packOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="申请机构" prop="applyOrgId">
          <ReOrganizationTreeSelect
            :modelValue="editForm.applyOrgId"
            @nodeClick="(n: any) => (editForm.applyOrgId = n.id)"
          />
        </el-form-item>
        <el-form-item label="申请医生" prop="applyDoctorId">
          <el-select
            v-model="editForm.applyDoctorId"
            filterable
            remote
            clearable
            placeholder="请输入医生姓名或账号"
            :remote-method="(q: string) => fetchUserOptions(q, 'apply')"
            :loading="applyDoctorLoading"
            style="width: 100%"
          >
            <el-option
              v-for="item in applyDoctorOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="目标机构">
          <ReOrganizationTreeSelect
            :modelValue="editForm.targetOrgId"
            @nodeClick="(n: any) => (editForm.targetOrgId = n.id)"
          />
        </el-form-item>
        <el-form-item label="目标科室">
          <el-input
            v-model="editForm.targetDepartment"
            placeholder="科室名称"
          />
        </el-form-item>
        <el-form-item label="目标专家">
          <el-select
            v-model="editForm.targetExpertId"
            filterable
            remote
            clearable
            placeholder="请输入专家姓名或账号"
            :remote-method="(q: string) => fetchUserOptions(q, 'expert')"
            :loading="targetExpertLoading"
            style="width: 100%"
          >
            <el-option
              v-for="item in targetExpertOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="会诊目的" prop="purpose">
          <el-input v-model="editForm.purpose" type="textarea" rows="3" />
        </el-form-item>
        <el-form-item label="紧急程度" prop="emergencyLevel">
          <el-select v-model="editForm.emergencyLevel" placeholder="请选择">
            <el-option
              v-for="item in dictSources.emergencyLevel"
              :key="item.code"
              :label="item.name"
              :value="item.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="期望开始">
          <el-date-picker
            v-model="editForm.desiredStartTime"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm"
          />
        </el-form-item>
        <el-form-item label="期望结束">
          <el-date-picker
            v-model="editForm.desiredEndTime"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="auditDialogVisible" title="会诊审核" width="460px">
      <el-form :model="auditForm" label-width="100px">
        <el-form-item label="审核结果">
          <el-radio-group v-model="auditForm.approved">
            <el-radio :label="true">通过</el-radio>
            <el-radio :label="false">驳回</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="意见">
          <el-input v-model="auditForm.comment" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="auditDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitAudit">提交</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="scheduleDialogVisible" title="安排会诊" width="460px">
      <el-form :model="scheduleForm" label-width="120px">
        <el-form-item label="开始时间">
          <el-date-picker
            v-model="scheduleForm.scheduledStartTime"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm"
          />
        </el-form-item>
        <el-form-item label="结束时间">
          <el-date-picker
            v-model="scheduleForm.scheduledEndTime"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm"
          />
        </el-form-item>
        <el-form-item label="会议室">
          <el-input v-model="scheduleForm.meetingRoomNo" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="scheduleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitSchedule">提交</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="rtcDialogVisible" title="音视频配置" width="460px">
      <el-form :model="rtcForm" label-width="120px">
        <el-form-item label="会议室">
          <el-input v-model="rtcForm.meetingRoomNo" />
        </el-form-item>
        <el-form-item label="频道ID">
          <el-input v-model="rtcForm.rtcChannelId" />
        </el-form-item>
        <el-form-item label="厂商">
          <el-input v-model="rtcForm.rtcVendor" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rtcDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitRtc">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="finishDialogVisible" title="结束会诊" width="420px">
      <el-form :model="finishForm" label-width="100px">
        <el-form-item label="总结">
          <el-input v-model="finishForm.summary" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="finishDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitFinish">结束</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="closeDialogVisible" title="关闭会诊" width="420px">
      <el-form :model="closeForm" label-width="100px">
        <el-form-item label="关闭原因">
          <el-input v-model="closeForm.closeReason" type="textarea" rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="closeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitClose">关闭</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="memberDialogVisible" title="成员信息" width="520px">
      <el-form :model="memberForm" label-width="120px">
        <el-form-item label="用户ID">
          <el-input-number
            v-model="memberForm.userId"
            :min="1"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="归属机构">
          <el-input-number
            v-model="memberForm.orgId"
            :min="1"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="成员角色">
          <el-select v-model="memberForm.roleCode" placeholder="角色">
            <el-option
              v-for="item in dictSources.memberRole"
              :key="item.code"
              :label="item.name"
              :value="item.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="参与状态">
          <el-select v-model="memberForm.joinStatus">
            <el-option
              v-for="item in dictSources.memberStatus"
              :key="item.code"
              :label="item.name"
              :value="item.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="加入时间">
          <el-date-picker
            v-model="memberForm.joinTime"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="memberDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitMember">保存</el-button>
      </template>
    </el-dialog>

    <el-drawer
      v-model="detailDrawerVisible"
      title="会诊详情"
      size="80%"
      :destroy-on-close="true"
    >
      <el-skeleton v-if="detailLoading" :rows="5" animated />
      <template v-else-if="detailData">
        <el-descriptions border :column="2" title="基础信息">
          <el-descriptions-item label="会诊编号">{{
            detailData.id
          }}</el-descriptions-item>
          <el-descriptions-item label="患者姓名">{{
            detailData.patientCase?.patientName
          }}</el-descriptions-item>
          <el-descriptions-item label="紧急程度">{{
            emergencyMap.value?.[detailData.emergencyLevel] ??
            detailData.emergencyLevel
          }}</el-descriptions-item>
          <el-descriptions-item label="状态">{{
            statusMap.value?.[detailData.consultationStatus] ??
            detailData.consultationStatus
          }}</el-descriptions-item>
          <el-descriptions-item label="申请医生">{{
            detailData.applyDoctorName
          }}</el-descriptions-item>
          <el-descriptions-item label="目标医院">{{
            detailData.targetOrgName
          }}</el-descriptions-item>
          <el-descriptions-item label="期望时间"
            >{{ detailData.desiredStartTime }} ~
            {{ detailData.desiredEndTime }}</el-descriptions-item
          >
          <el-descriptions-item label="排期时间"
            >{{ detailData.scheduledStartTime }} ~
            {{ detailData.scheduledEndTime }}</el-descriptions-item
          >
        </el-descriptions>

        <el-space style="margin: 16px 0">
          <el-button type="primary" @click="openRtc(detailData)"
            >音视频配置</el-button
          >
          <el-button type="primary" @click="openReportEditor"
            >编辑报告</el-button
          >
          <el-button
            type="success"
            :disabled="!currentUserId.value"
            @click="() => submitSignReport(currentUserId.value)"
            >签署报告</el-button
          >
          <el-button type="primary" @click="() => gotoRtcDemo(detailData)"
            >进入演示</el-button
          >
        </el-space>

        <el-tabs type="border-card">
          <el-tab-pane label="附件">
            <el-table :data="attachmentData" border>
              <el-table-column prop="fileName" label="文件名" min-width="200" />
              <el-table-column
                prop="attachmentType"
                label="类型"
                min-width="120"
              />
              <el-table-column
                prop="sourceSystem"
                label="来源"
                min-width="120"
              />
              <el-table-column
                prop="createTime"
                label="创建时间"
                min-width="160"
              />
              <el-table-column label="操作" width="120">
                <template #default="{ row }">
                  <el-button link type="danger" @click="removeAttachment(row)"
                    >删除</el-button
                  >
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
          <el-tab-pane label="参会成员">
            <el-button
              type="primary"
              size="small"
              @click="() => openMemberEditor()"
              >新增成员</el-button
            >
            <el-table :data="memberData" border style="margin-top: 12px">
              <el-table-column prop="userName" label="姓名" min-width="120" />
              <el-table-column prop="orgName" label="机构" min-width="160" />
              <el-table-column label="角色" min-width="120">
                <template #default="{ row }">{{
                  memberRoleMap.value?.[row.roleCode] ?? row.roleCode
                }}</template>
              </el-table-column>
              <el-table-column label="状态" min-width="120">
                <template #default="{ row }">{{
                  memberStatusMap.value?.[row.joinStatus] ?? row.joinStatus
                }}</template>
              </el-table-column>
              <el-table-column
                prop="joinTime"
                label="加入时间"
                min-width="160"
              />
              <el-table-column label="操作" width="160">
                <template #default="{ row }">
                  <el-button
                    link
                    type="primary"
                    @click="() => openMemberEditor(row)"
                    >编辑</el-button
                  >
                  <el-button link type="danger" @click="() => removeMember(row)"
                    >移除</el-button
                  >
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
          <el-tab-pane label="流程轨迹">
            <el-table :data="timelineData" border>
              <el-table-column prop="eventCode" label="事件" min-width="120" />
              <el-table-column
                prop="eventContent"
                label="内容"
                min-width="200"
              />
              <el-table-column
                prop="createByName"
                label="操作人"
                min-width="120"
              />
              <el-table-column prop="createTime" label="时间" min-width="160" />
            </el-table>
          </el-tab-pane>
          <el-tab-pane label="报告">
            <el-descriptions v-if="reportData" :column="1" border>
              <el-descriptions-item label="总结">{{
                reportData.summary
              }}</el-descriptions-item>
              <el-descriptions-item label="诊断">{{
                reportData.diagnosis
              }}</el-descriptions-item>
              <el-descriptions-item label="治疗建议">{{
                reportData.treatmentAdvice
              }}</el-descriptions-item>
              <el-descriptions-item label="随访计划">{{
                reportData.followUpPlan
              }}</el-descriptions-item>
              <el-descriptions-item label="状态">{{
                reportData.reportStatus
              }}</el-descriptions-item>
              <el-descriptions-item label="签署人">{{
                reportData.signerName
              }}</el-descriptions-item>
              <el-descriptions-item label="时间">{{
                reportData.signedTime
              }}</el-descriptions-item>
            </el-descriptions>
            <div v-else class="empty-report">暂无报告信息</div>
          </el-tab-pane>
        </el-tabs>
      </template>
    </el-drawer>

    <el-drawer v-model="reportDrawerVisible" title="编辑会诊报告" size="40%">
      <el-form :model="reportForm" label-width="120px">
        <el-form-item label="摘要">
          <el-input v-model="reportForm.summary" type="textarea" rows="3" />
        </el-form-item>
        <el-form-item label="诊断">
          <el-input v-model="reportForm.diagnosis" type="textarea" rows="3" />
        </el-form-item>
        <el-form-item label="治疗建议">
          <el-input
            v-model="reportForm.treatmentAdvice"
            type="textarea"
            rows="3"
          />
        </el-form-item>
        <el-form-item label="随访计划">
          <el-input
            v-model="reportForm.followUpPlan"
            type="textarea"
            rows="3"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="reportForm.reportStatus">
            <el-option label="草稿" value="DRAFT" />
            <el-option label="签署中" value="SIGNING" />
            <el-option label="已签署" value="SIGNED" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="reportDrawerVisible = false">取消</el-button>
        <el-button type="primary" @click="submitReport">保存</el-button>
      </template>
    </el-drawer>

    <el-drawer
      v-model="rtcDrawerVisible"
      size="90%"
      title="音视频会诊"
      destroy-on-close
    >
      <MedicalRtcConference
        v-if="rtcDrawerVisible && rtcConsultationId"
        :consultation-id="rtcConsultationId"
      />
    </el-drawer>
  </div>
</template>

<style scoped>
.remote-consultation-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.query-card {
  padding-bottom: 12px;
}

.empty-report {
  padding: 16px;
  color: var(--el-color-info);
}

.stats-row {
  display: flex;
  align-items: center;
  gap: 24px;
}
.stat-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.stat-label {
  color: var(--el-color-info);
}
.stat-value {
  font-size: 20px;
  font-weight: 600;
}
</style>
