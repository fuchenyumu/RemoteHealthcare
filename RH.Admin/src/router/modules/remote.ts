import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import VideoChat from "~icons/mdi/video-account";
import MonitorDashboard from "~icons/mdi/monitor-dashboard";
import CalendarMonth from "~icons/mdi/calendar-month";

const Layout = () => import("@/layout/index.vue");

export default {
  path: "/remote",
  name: "remote",
  component: Layout,
  meta: {
    icon: useRenderIcon(VideoChat),
    title: "远程会诊",
    permissions: ["remote"],
    rank: 10
  },
  children: [
    {
      path: "/remote/dashboard",
      name: "remote_dashboard",
      meta: {
        icon: useRenderIcon(MonitorDashboard),
        title: "会诊驾驶舱",
        permissions: ["remote.dashboard"]
      },
      component: () => import("@/views/remote-consultation/dashboard/index.vue")
    },
    {
      path: "/remote/calendar",
      name: "remote_calendar",
      meta: {
        icon: useRenderIcon(CalendarMonth),
        title: "排期中心",
        permissions: ["remote.calendar"]
      },
      component: () => import("@/views/remote-consultation/calendar/index.vue")
    },
    {
      path: "/remote/consultation",
      name: "remote_consultation",
      meta: {
        title: "会诊管理",
        permissions: ["remote.consultation"]
      },
      component: () =>
        import("@/views/remote-consultation/consultation/index.vue")
    },
    {
      path: "/remote/patient-case",
      name: "remote_patient_case",
      meta: {
        title: "患者快照",
        permissions: ["remote.patientCase"]
      },
      component: () =>
        import("@/views/remote-consultation/patient-case/index.vue")
    },
    {
      path: "/remote/patient-pack",
      name: "remote_patient_pack",
      meta: {
        title: "资料包管理",
        permissions: ["remote.patientPack"]
      },
      component: () =>
        import("@/views/remote-consultation/patient-pack/index.vue")
    },
    {
      path: "/remote/sync-task",
      name: "remote_sync_task",
      meta: {
        title: "同步任务",
        permissions: ["remote.syncTask"]
      },
      component: () => import("@/views/remote-consultation/sync-task/index.vue")
    },
    {
      path: "/remote/rtc-demo",
      name: "remote_rtc_demo",
      meta: {
        title: "音视频演示",
        permissions: ["remote.rtcDemo"]
      },
      component: () => import("@/views/remote-consultation/rtc-demo/index.vue")
    }
  ]
} satisfies RouteConfigsTable;
