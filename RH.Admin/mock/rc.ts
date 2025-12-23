import type { MockMethod } from "vite-plugin-fake-server";

let consultationIdSeq = 1000;
let reportIdSeq = 2000;
const consultations: any[] = [];
const reports: any[] = [];
const timelines: Record<number, any[]> = {};
const patientCases: any[] = [
  { id: 1, patientName: "张三", patientType: "ADULT" },
  { id: 2, patientName: "李四", patientType: "CHILD" }
];
const patientPacks: any[] = [
  {
    id: 100,
    caseId: 1,
    packNo: "PK-001",
    packStatus: "PENDING",
    fileCount: 0,
    totalSize: 0
  },
  {
    id: 101,
    caseId: 2,
    packNo: "PK-002",
    packStatus: "PENDING",
    fileCount: 0,
    totalSize: 0
  }
];

const paged = (items: any[], pageIndex: number, pageSize: number) => {
  const start = (pageIndex - 1) * pageSize;
  const end = start + pageSize;
  return {
    items: items.slice(start, end),
    total: items.length,
    pageIndex,
    pageSize
  };
};

export default [
  {
    url: "/api/v1/rc-consultation",
    method: "post",
    response: ({ body }) => {
      const id = ++consultationIdSeq;
      const now = new Date().toISOString();
      const item = {
        id,
        consultationStatus: "PENDING_REVIEW",
        createTime: now,
        ...body
      };
      consultations.unshift(item);
      timelines[id] = timelines[id] || [];
      timelines[id].unshift({
        eventCode: "Created",
        eventContent: "创建会诊",
        createByName: body.applyDoctorId || "",
        createTime: now
      });
      return { data: id };
    }
  },
  {
    url: "/api/v1/rc-patient-case/paged-list",
    method: "get",
    response: ({ query }) => {
      const { pageIndex = 1, pageSize = 20, keyword = "" } = query as any;
      const list = patientCases.filter(
        x =>
          !keyword ||
          String(x.patientName).includes(keyword) ||
          String(x.id).includes(keyword)
      );
      return { data: paged(list, Number(pageIndex), Number(pageSize)) };
    }
  },
  {
    url: "/api/v1/rc-patient-case/:id",
    method: "get",
    response: ({ url }) => {
      const id = Number(url.split("/").pop());
      const item = patientCases.find(x => x.id === id) || null;
      return { data: item };
    }
  },
  {
    url: "/api/v1/rc-patient-pack/paged-list",
    method: "get",
    response: ({ query }) => {
      const { pageIndex = 1, pageSize = 20, keyword = "" } = query as any;
      const list = patientPacks.filter(
        x =>
          !keyword ||
          String(x.packNo).includes(keyword) ||
          String(x.id).includes(keyword)
      );
      return { data: paged(list, Number(pageIndex), Number(pageSize)) };
    }
  },
  {
    url: "/api/v1/rc-patient-pack/:id",
    method: "get",
    response: ({ url }) => {
      const id = Number(url.split("/").pop());
      const item = patientPacks.find(x => x.id === id) || null;
      return { data: item };
    }
  },
  {
    url: "/api/v1/rc-consultation-attachment/paged-list",
    method: "get",
    response: ({ query }) => {
      const { consultationId, pageIndex = 1, pageSize = 50 } = query as any;
      const items = [];
      return { data: paged(items, Number(pageIndex), Number(pageSize)) };
    }
  },
  {
    url: "/api/v1/rc-consultation-member/paged-list",
    method: "get",
    response: ({ query }) => {
      const { consultationId, pageIndex = 1, pageSize = 50 } = query as any;
      const items = [];
      return { data: paged(items, Number(pageIndex), Number(pageSize)) };
    }
  },
  {
    url: "/api/v1/rc-consultation/paged-list",
    method: "get",
    response: ({ query }) => {
      const {
        pageIndex = 1,
        pageSize = 10,
        keyword = "",
        consultationStatus = ""
      } = query as any;
      const list = consultations.filter(
        x =>
          (!keyword ||
            String(x.purpose).includes(keyword) ||
            String(x.targetDepartment).includes(keyword)) &&
          (!consultationStatus || x.consultationStatus === consultationStatus)
      );
      return { data: paged(list, Number(pageIndex), Number(pageSize)) };
    }
  },
  {
    url: "/api/v1/rc-consultation/:id",
    method: "get",
    response: ({ url }) => {
      const id = Number(url.split("/").pop());
      const item = consultations.find(x => x.id === id) || null;
      return { data: item };
    }
  },
  {
    url: "/api/v1/rc-consultation/:id",
    method: "put",
    response: ({ url, body }) => {
      const id = Number(url.split("/").pop());
      const idx = consultations.findIndex(x => x.id === id);
      if (idx >= 0) consultations[idx] = { ...consultations[idx], ...body };
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation/:id",
    method: "delete",
    response: ({ url }) => {
      const id = Number(url.split("/").pop());
      const idx = consultations.findIndex(x => x.id === id);
      if (idx >= 0) consultations.splice(idx, 1);
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation/audit",
    method: "post",
    response: ({ body }) => {
      const { consultationId, approved } = body;
      const c = consultations.find(x => x.id === consultationId);
      if (c) {
        c.consultationStatus = approved ? "WAIT_SCHEDULE" : "CLOSED";
        const now = new Date().toISOString();
        timelines[consultationId].unshift({
          eventCode: approved ? "AuditApproved" : "AuditRejected",
          eventContent: body.comment || "",
          createByName: "",
          createTime: now
        });
      }
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation/schedule",
    method: "post",
    response: ({ body }) => {
      const {
        consultationId,
        scheduledStartTime,
        scheduledEndTime,
        meetingRoomNo
      } = body;
      const c = consultations.find(x => x.id === consultationId);
      if (c) {
        c.scheduledStartTime = scheduledStartTime;
        c.scheduledEndTime = scheduledEndTime;
        c.meetingRoomNo = meetingRoomNo;
        c.consultationStatus = "SCHEDULED";
        const now = new Date().toISOString();
        timelines[consultationId].unshift({
          eventCode: "Scheduled",
          eventContent: meetingRoomNo || "",
          createByName: "",
          createTime: now
        });
      }
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation/rtc",
    method: "post",
    response: ({ body }) => {
      const { consultationId, rtcChannelId, rtcVendor, meetingRoomNo } = body;
      const c = consultations.find(x => x.id === consultationId);
      if (c) {
        c.rtcChannelId = rtcChannelId;
        c.rtcVendor = rtcVendor;
        c.meetingRoomNo = meetingRoomNo || c.meetingRoomNo;
      }
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation/start",
    method: "post",
    response: ({ body }) => {
      const { consultationId } = body;
      const c = consultations.find(x => x.id === consultationId);
      if (c) {
        c.consultationStatus = "IN_PROGRESS";
        const now = new Date().toISOString();
        timelines[consultationId].unshift({
          eventCode: "Started",
          eventContent: "",
          createByName: "",
          createTime: now
        });
      }
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation/finish",
    method: "post",
    response: ({ body }) => {
      const { consultationId, summary } = body;
      const c = consultations.find(x => x.id === consultationId);
      if (c) {
        c.consultationStatus = "FINISHED";
        const now = new Date().toISOString();
        timelines[consultationId].unshift({
          eventCode: "Finished",
          eventContent: summary || "",
          createByName: "",
          createTime: now
        });
      }
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation/close",
    method: "post",
    response: ({ body }) => {
      const { consultationId, closeReason } = body;
      const c = consultations.find(x => x.id === consultationId);
      if (c) {
        c.consultationStatus = "CLOSED";
        const now = new Date().toISOString();
        timelines[consultationId].unshift({
          eventCode: "Closed",
          eventContent: closeReason || "",
          createByName: "",
          createTime: now
        });
      }
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation-report",
    method: "post",
    response: ({ body }) => {
      const id = ++reportIdSeq;
      reports.unshift({ id, ...body });
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation-report/:id",
    method: "put",
    response: ({ url, body }) => {
      const id = Number(url.split("/").pop());
      const idx = reports.findIndex(x => x.id === id);
      if (idx >= 0) reports[idx] = { ...reports[idx], ...body };
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation-report/sign",
    method: "post",
    response: ({ body }) => {
      const { consultationId, signerId } = body;
      const r = reports.find(x => x.consultationId === consultationId);
      if (r) {
        r.reportStatus = "SIGNED";
        r.signerId = signerId;
        r.signerName = String(signerId);
        r.signedTime = new Date().toISOString();
      }
      return { data: true };
    }
  },
  {
    url: "/api/v1/rc-consultation-report/paged-list",
    method: "get",
    response: ({ query }) => {
      const { consultationId } = query as any;
      const list = reports.filter(x => x.consultationId == consultationId);
      return {
        data: {
          items: list,
          total: list.length,
          pageIndex: 1,
          pageSize: list.length
        }
      };
    }
  },
  {
    url: "/api/v1/rc-consultation-timeline/paged-list",
    method: "get",
    response: ({ query }) => {
      const { consultationId, pageIndex = 1, pageSize = 100 } = query as any;
      const list = timelines[Number(consultationId)] || [];
      return { data: paged(list, Number(pageIndex), Number(pageSize)) };
    }
  }
] as MockMethod[];
