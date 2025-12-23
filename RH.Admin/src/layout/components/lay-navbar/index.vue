<script setup lang="ts">
import { ref } from "vue";
import { useNav } from "@/layout/hooks/useNav";
import LaySearch from "../lay-search/index.vue";
import LayNotice from "../lay-notice/index.vue";
import LayNavMix from "../lay-sidebar/NavMix.vue";
import LaySidebarFullScreen from "../lay-sidebar/components/SidebarFullScreen.vue";
import LaySidebarBreadCrumb from "../lay-sidebar/components/SidebarBreadCrumb.vue";
import LaySidebarTopCollapse from "../lay-sidebar/components/SidebarTopCollapse.vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { useRouter } from "vue-router";
import { useUserStoreHook } from "@/store/modules/user";
import { usePermissionStoreHook } from "@/store/modules/permission";
import { addPathMatch } from "@/router/utils";
import { demoUsers, type DemoUser } from "@/config/demoUsers";

import LogoutCircleRLine from "~icons/ri/logout-circle-r-line";
import Setting from "~icons/ri/settings-3-line";
import Edit from "~icons/ri/edit-2-line";
import UserSharedLine from "~icons/ri/user-shared-2-line";

const router = useRouter();
const switchingDemo = ref(false);

const switchDemoUser = async (user: DemoUser) => {
  const currentAccount = useUserStoreHook()?.currentUser?.account ?? "";
  if (currentAccount && currentAccount === user.account) {
    ElMessage.info(`已是当前账号：${user.label}（${user.account}）`);
    return;
  }

  try {
    await ElMessageBox.confirm(
      `确认切换为 ${user.label}（${user.account}）吗？\n切换后将刷新页面以应用新的权限与会诊角色。`,
      "演示账号切换",
      {
        type: "warning",
        confirmButtonText: "切换",
        cancelButtonText: "取消"
      }
    );
  } catch {
    return;
  }

  switchingDemo.value = true;
  try {
    await useUserStoreHook().login({
      account: user.account,
      password: user.password
    });
    // 复用登录页逻辑：重新生成菜单/权限（当前项目以静态路由为主，传空数组即可）
    usePermissionStoreHook().handleWholeMenus([]);
    addPathMatch();

    ElMessage.success(`已切换为：${user.label}`);
    // 彻底刷新以清理页面内状态（SignalR/WebSocket/RTC等）并确保 token 生效
    await router.replace({ path: "/remote/consultation" });
    window.location.reload();
  } catch (error: any) {
    ElMessage.error(error?.message ?? "切换失败");
  } finally {
    switchingDemo.value = false;
  }
};
const {
  layout,
  device,
  logout,
  onPanel,
  pureApp,
  username,
  userAvatar,
  avatarsStyle,
  toggleSideBar,
  changePassword
} = useNav();
</script>

<template>
  <div class="navbar bg-[#fff] shadow-xs shadow-[rgba(0,21,41,0.08)]">
    <LaySidebarTopCollapse
      v-if="device === 'mobile'"
      class="hamburger-container"
      :is-active="pureApp.sidebar.opened"
      @toggleClick="toggleSideBar"
    />

    <LaySidebarBreadCrumb
      v-if="layout !== 'mix' && device !== 'mobile'"
      class="breadcrumb-container"
    />

    <LayNavMix v-if="layout === 'mix'" />

    <div v-if="layout === 'vertical'" class="vertical-header-right">
      <!-- 菜单搜索 -->
      <LaySearch id="header-search" />
      <!-- 全屏 -->
      <LaySidebarFullScreen id="full-screen" />
      <!-- 消息通知 -->
      <LayNotice id="header-notice" />
      <!-- 退出登录 -->
      <el-dropdown trigger="click">
        <span class="select-none el-dropdown-link navbar-bg-hover">
          <img :src="userAvatar" :style="avatarsStyle" />
          <p v-if="username" class="dark:text-white">{{ username }}</p>
        </span>
        <template #dropdown>
          <el-dropdown-menu class="logout">
            <el-dropdown-item disabled>
              <IconifyIconOffline :icon="UserSharedLine" style="margin: 5px" />
              演示账号
            </el-dropdown-item>
            <el-dropdown-item
              v-for="user in demoUsers"
              :key="user.key"
              :disabled="switchingDemo"
              @click="switchDemoUser(user)"
            >
              <IconifyIconOffline :icon="UserSharedLine" style="margin: 5px" />
              {{ user.label }}
            </el-dropdown-item>
            <el-dropdown-item divided @click="changePassword">
              <IconifyIconOffline :icon="Edit" style="margin: 5px" />
              个人信息
            </el-dropdown-item>
            <el-dropdown-item @click="logout">
              <IconifyIconOffline
                :icon="LogoutCircleRLine"
                style="margin: 5px"
              />
              退出系统
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      <span
        class="set-icon navbar-bg-hover"
        title="打开系统配置"
        @click="onPanel"
      >
        <IconifyIconOffline :icon="Setting" />
      </span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.navbar {
  width: 100%;
  height: 48px;
  overflow: hidden;

  .hamburger-container {
    float: left;
    height: 100%;
    line-height: 48px;
    cursor: pointer;
  }

  .vertical-header-right {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    min-width: 280px;
    height: 48px;
    color: #000000d9;

    .el-dropdown-link {
      display: flex;
      align-items: center;
      justify-content: space-around;
      height: 48px;
      padding: 10px;
      color: #000000d9;
      cursor: pointer;

      p {
        font-size: 14px;
      }

      img {
        width: 22px;
        height: 22px;
        border-radius: 50%;
      }
    }
  }

  .breadcrumb-container {
    float: left;
    margin-left: 16px;
  }
}

.logout {
  width: 150px;

  ::v-deep(.el-dropdown-menu__item) {
    display: inline-flex;
    flex-wrap: wrap;
    min-width: 100%;
  }
}
</style>
