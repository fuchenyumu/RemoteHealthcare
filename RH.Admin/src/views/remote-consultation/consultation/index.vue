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
import { getEvaluationPage, type RcEvaluation } from "@/api/rc/evaluation";
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
import ShareLinkDialog from "./components/ShareLinkDialog.vue";
import stampUrl from "@/assets/demo/stamp.svg";
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
const evaluationData = ref<RcEvaluation[]>([]);

const qrDialogVisible = ref(false);
const qrUrl = ref("");
const openEvaluationQR = async (id: number) => {
  try {
    // 从接口获取会诊详情，后端会返回完整的 H5 评价链接
    const res = await getConsultationDetail(id);

    if (res.data && res.data.evaluationUrl) {
      // 使用接口返回的评价链接生成二维码
      qrUrl.value = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(res.data.evaluationUrl)}`;
      qrDialogVisible.value = true;
    } else {
      ElMessage.error("评价链接生成失败，请联系管理员");
    }
  } catch (error) {
    console.error("获取评价链接失败:", error);
    ElMessage.error("获取评价链接失败");
  }
};

// 分享链接对话框
const shareLinkDialogVisible = ref(false);
const shareLinkConsultationId = ref<number | null>(null);
const shareLinkConsultationStatus = ref<string>("");
const openShareLinkDialog = (row: ConsultationSummary) => {
  shareLinkConsultationId.value = row.id;
  shareLinkConsultationStatus.value = row.consultationStatus;
  shareLinkDialogVisible.value = true;
};

const dictSources = reactive({
  consultationStatus: [] as any[],
  emergencyLevel: [] as any[],
  packStatus: [] as any[],
  memberRole: [] as any[],
  memberStatus: [] as any[],
  timelineEvent: [] as any[]
});

const overviewStats = ref<{
  total: number;
  today: number;
  pendingReview: number;
  finished: number;
} | null>(null);
const loadOverviewStats = async () => {
  try {
    const res = await getConsultationStatsOverview();
    const data = unwrap<{
      total: number;
      today: number;
      pendingReview: number;
      finished: number;
    }>(res);
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

const timelineEventMap = computed<Record<string, string>>(() => {
  const list = Array.isArray(dictSources.timelineEvent)
    ? dictSources.timelineEvent
    : [];
  return Object.fromEntries(list.map(item => [item.code, item.name]));
});

const timelineDescending = ref(false);
const orderedTimelineData = computed(() => {
  const list = Array.isArray(timelineData.value) ? [...timelineData.value] : [];
  list.sort((a: any, b: any) => {
    const ta = a?.createTime ? new Date(a.createTime).getTime() : 0;
    const tb = b?.createTime ? new Date(b.createTime).getTime() : 0;
    return timelineDescending.value ? tb - ta : ta - tb;
  });
  return list;
});

const timelineItemType = (code: string) => {
  switch (code) {
    case "SUBMITTED":
      return "info";
    case "APPROVED":
      return "success";
    case "REJECTED":
      return "danger";
    case "SCHEDULED":
      return "warning";
    case "STARTED":
      return "primary";
    case "ENDED":
      return "info";
    case "SIGNED":
      return "success";
    case "CLOSED":
      return "danger";
    default:
      return "info";
  }
};

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

type ReportTemplate = {
  key: string;
  name: string;
  summary: string;
  diagnosis: string;
  treatmentAdvice: string;
  followUpPlan: string;
};

const reportTemplates: ReportTemplate[] = [
  {
    key: "OB_ULTRASOUND",
    name: "孕产｜产前超声会诊",
    summary:
      "主诉：孕中期超声提示异常回声，申请远程会诊。\n病史摘要：既往史无特殊，孕期随访规律。\n检查要点：胎儿结构筛查、羊水指数、胎盘位置。",
    diagnosis:
      "初步考虑：胎儿结构异常待排（建议结合系统超声/胎儿心超）。\n鉴别诊断：发育变异、伪影、局灶性钙化。",
    treatmentAdvice:
      "1) 建议48-72小时内复查高分辨率超声；\n2) 必要时完善胎儿心超/产前筛查；\n3) 加强孕期随访与风险宣教。",
    followUpPlan:
      "随访：1周内复诊；若出现腹痛/出血等症状及时就医。\n资料补充：上传复查超声报告与关键切面截图。"
  },
  {
    key: "PED_EMERGENCY",
    name: "儿科｜急症评估",
    summary:
      "主诉：发热/咳嗽/精神差，申请远程会诊。\n病史摘要：起病急，近期有上呼吸道感染接触史。\n检查要点：生命体征、呼吸情况、血常规/CRP。",
    diagnosis:
      "初步考虑：社区获得性肺炎/支气管炎待排。\n风险提示：需警惕重症肺炎、败血症风险。",
    treatmentAdvice:
      "1) 建议完善胸片/肺超（若条件允许）；\n2) 经验性抗感染需结合年龄体重与过敏史；\n3) 加强补液与对症处理（退热、雾化等）。",
    followUpPlan:
      "随访：24小时内复评；出现呼吸困难/嗜睡/抽搐等立即转上级医院。\n资料补充：上传检验结果、影像截图与用药记录。"
  },
  {
    key: "GENERAL",
    name: "普通｜远程咨询",
    summary:
      "主诉：一般健康咨询/复诊评估。\n病史摘要：既往史/用药史详见病历资料。\n检查要点：本次主要问题与既往资料核对。",
    diagnosis:
      "初步判断：建议结合现有资料进一步评估。\n需补充：关键检验/影像资料。",
    treatmentAdvice:
      "1) 建议按规范补齐资料（检查报告、影像关键帧）；\n2) 对症处理与生活方式建议；\n3) 如症状加重及时线下就诊。",
    followUpPlan: "随访：建议1-2周内复诊或按医嘱随访。"
  }
];

const selectedReportTemplate = ref<string>("");
const applyReportTemplate = async () => {
  const tpl = reportTemplates.find(t => t.key === selectedReportTemplate.value);
  if (!tpl) return;
  Object.assign(reportForm, {
    summary: tpl.summary,
    diagnosis: tpl.diagnosis,
    treatmentAdvice: tpl.treatmentAdvice,
    followUpPlan: tpl.followUpPlan
  });
  // 落地到数据库：应用模板即保存为草稿
  if (reportData.value && reportData.value.id) {
    await updateReport(reportData.value.id, reportForm);
  } else {
    await saveReport(reportForm);
  }
  ElMessage.success(`已套用并同步保存草稿：${tpl.name}`);
  if (detailData.value) await loadReport(detailData.value.id);
};

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
          }),
          h(VxeButton, {
            mode: "text",
            status: "success",
            icon: "vxe-icon-link",
            content: "分享",
            onClick: () => openShareLinkDialog(row)
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
  await scheduleConsultation(scheduleForm.consultationId, {
    scheduledStartTime: new Date(scheduleForm.scheduledStartTime).toISOString(),
    scheduledEndTime: new Date(scheduleForm.scheduledEndTime).toISOString(),
    meetingRoomNo: scheduleForm.meetingRoomNo
  });
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
      loadReport(id),
      loadEvaluation(id)
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

const loadEvaluation = async (consultationId: number) => {
  const list = unwrap<PagedResult<RcEvaluation>>(
    await getEvaluationPage({ consultationId })
  );
  evaluationData.value = list?.items ?? [];
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
  selectedReportTemplate.value = "";
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
  const [
    status,
    emergency,
    packStatus,
    memberRole,
    memberStatus,
    timelineEvent
  ] = await Promise.all([
    getDictionaryDataByCode("RC_CONSULT_STATUS"),
    getDictionaryDataByCode("RC_EMERGENCY_LEVEL"),
    getDictionaryDataByCode("RC_PACK_STATUS"),
    getDictionaryDataByCode("RC_MEMBER_ROLE"),
    getDictionaryDataByCode("RC_MEMBER_STATUS"),
    getDictionaryDataByCode("RC_TIMELINE_EVENT")
  ]);
  dictSources.consultationStatus = unwrap<any[]>(status) ?? [];
  dictSources.emergencyLevel = unwrap<any[]>(emergency) ?? [];
  dictSources.packStatus = unwrap<any[]>(packStatus) ?? [];
  dictSources.memberRole = unwrap<any[]>(memberRole) ?? [];
  dictSources.memberStatus = unwrap<any[]>(memberStatus) ?? [];
  dictSources.timelineEvent = unwrap<any[]>(timelineEvent) ?? [];
  await loadOverviewStats();
});
</script>

<template>
  <div class="remote-consultation-page">
    <el-card shadow="never">
      <div class="stats-row">
        <div class="stat-item">
          <div class="stat-label">总会诊数</div>
          <div class="stat-value">{{ overviewStats?.total ?? "-" }}</div>
        </div>
        <div class="stat-item">
          <div class="stat-label">今日新增</div>
          <div class="stat-value">{{ overviewStats?.today ?? "-" }}</div>
        </div>
        <div class="stat-item">
          <div class="stat-label">待审核</div>
          <div class="stat-value">
            {{ overviewStats?.pendingReview ?? "-" }}
          </div>
        </div>
        <div class="stat-item">
          <div class="stat-label">已完成</div>
          <div class="stat-value">{{ overviewStats?.finished ?? "-" }}</div>
        </div>
        <el-button type="primary" size="small" @click="loadOverviewStats"
          >刷新</el-button
        >
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
            :disabled="!currentUserId"
            @click="() => submitSignReport(currentUserId)"
            >签署报告</el-button
          >
          <el-button type="primary" @click="() => gotoRtcDemo(detailData)"
            >进入演示</el-button
          >
          <el-button
            type="warning"
            @click="() => openEvaluationQR(detailData.id)"
            >生成评价码</el-button
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
            <div class="timeline-toolbar">
              <el-switch
                v-model="timelineDescending"
                active-text="倒序"
                inactive-text="正序"
              />
            </div>
            <el-empty
              v-if="orderedTimelineData.length === 0"
              description="暂无轨迹"
            />
            <el-timeline v-else>
              <el-timeline-item
                v-for="item in orderedTimelineData"
                :key="item.id"
                :timestamp="item.createTime"
                :type="timelineItemType(item.eventCode)"
              >
                <div class="timeline-item">
                  <div class="timeline-item__header">
                    <el-tag
                      :type="timelineItemType(item.eventCode)"
                      size="small"
                    >
                      {{ timelineEventMap[item.eventCode] ?? item.eventCode }}
                    </el-tag>
                    <span class="timeline-item__content">
                      {{ item.eventContent || "-" }}
                    </span>
                  </div>
                  <div class="timeline-item__meta">
                    {{ item.createByName || "-" }}
                  </div>
                </div>
              </el-timeline-item>
            </el-timeline>
          </el-tab-pane>
          <el-tab-pane label="报告">
            <div v-if="reportData" class="report-view">
              <el-descriptions :column="1" border>
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
              <img
                v-if="['SIGNED', 'ARCHIVED'].includes(reportData.reportStatus)"
                class="report-stamp"
                :src="stampUrl"
                alt="signed"
              />
            </div>
            <div v-else class="empty-report">暂无报告信息</div>
          </el-tab-pane>
          <el-tab-pane label="评价反馈">
            <el-empty
              v-if="evaluationData.length === 0"
              description="暂无评价"
            />
            <div v-else class="evaluation-list">
              <el-card
                v-for="item in evaluationData"
                :key="item.id"
                shadow="never"
                class="mb-3"
              >
                <div class="flex items-center justify-between mb-2">
                  <div class="flex items-center gap-2">
                    <span class="font-bold">{{ item.patientName }}</span>
                    <el-rate
                      v-model="item.score"
                      disabled
                      show-score
                      text-color="#ff9900"
                    />
                  </div>
                  <span class="text-xs text-gray-400">{{
                    item.createTime
                  }}</span>
                </div>
                <div class="mb-2">
                  <el-tag
                    v-for="tag in item.tags.split(',')"
                    v-show="tag"
                    :key="tag"
                    size="small"
                    class="mr-1"
                  >
                    {{ tag }}
                  </el-tag>
                </div>
                <p class="text-sm text-gray-600">
                  {{ item.content || "无详细意见" }}
                </p>
              </el-card>
            </div>
          </el-tab-pane>
        </el-tabs>
      </template>
    </el-drawer>

    <el-dialog
      v-model="qrDialogVisible"
      title="患者评价二维码（手机扫码）"
      width="300px"
      align-center
    >
      <div class="flex flex-col items-center">
        <img :src="qrUrl" alt="QR Code" style="width: 200px; height: 200px" />
        <p class="mt-4 text-xs text-gray-500 text-center">
          请提示患者使用手机扫码填写会诊评价
        </p>
      </div>
    </el-dialog>

    <el-drawer v-model="reportDrawerVisible" title="编辑会诊报告" size="40%">
      <el-form :model="reportForm" label-width="120px">
        <el-form-item label="模板">
          <div class="flex items-center gap-2 w-full">
            <el-select
              v-model="selectedReportTemplate"
              placeholder="选择模板（演示用）"
              style="flex: 1"
              clearable
            >
              <el-option
                v-for="tpl in reportTemplates"
                :key="tpl.key"
                :label="tpl.name"
                :value="tpl.key"
              />
            </el-select>
            <el-button
              type="primary"
              plain
              :disabled="!selectedReportTemplate"
              @click="applyReportTemplate"
            >
              一键填充
            </el-button>
          </div>
        </el-form-item>
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

    <!-- 分享链接对话框 -->
    <ShareLinkDialog
      v-model:visible="shareLinkDialogVisible"
      :consultation-id="shareLinkConsultationId"
      :consultation-status="shareLinkConsultationStatus"
    />
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

.report-view {
  position: relative;
}

.report-stamp {
  position: absolute;
  right: 18px;
  bottom: 18px;
  width: 150px;
  opacity: 0.9;
  transform: rotate(-12deg);
  pointer-events: none;
}

.timeline-toolbar {
  display: flex;
  justify-content: flex-end;
  margin: 8px 0 16px;
}

.timeline-item__header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.timeline-item__content {
  color: var(--el-text-color-regular);
}

.timeline-item__meta {
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
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
