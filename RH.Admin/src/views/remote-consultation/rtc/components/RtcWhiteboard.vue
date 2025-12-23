<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { ElButton, ElButtonGroup, ElMessage } from "element-plus";

import demoImageUrl from "@/assets/demo/demo-image-1.svg";

type ToolType = "pen" | "eraser";

const props = withDefaults(
  defineProps<{
    imageSrc?: string;
  }>(),
  {
    imageSrc: demoImageUrl
  }
);

const wrapRef = ref<HTMLDivElement | null>(null);
const canvasRef = ref<HTMLCanvasElement | null>(null);

const tool = ref<ToolType>("pen");
const isDrawing = ref(false);

const penColor = "#ff3b30";
const penWidth = 4;
const eraserWidth = 18;

let ctx: CanvasRenderingContext2D | null = null;
let resizeObserver: ResizeObserver | null = null;

const emit = defineEmits<{
  (e: "save", blob: Blob): void;
}>();

const hintText = computed(() => "本地标注（可保存至附件）");

function setupCanvas() {
  const wrap = wrapRef.value;
  const canvas = canvasRef.value;
  if (!wrap || !canvas) return;

  const width = Math.max(1, Math.floor(wrap.clientWidth));
  // 16:9 演示尺寸
  const height = Math.max(1, Math.floor(width * 0.5625));

  const dpr = window.devicePixelRatio || 1;
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;
  canvas.width = Math.floor(width * dpr);
  canvas.height = Math.floor(height * dpr);

  ctx = canvas.getContext("2d");
  if (!ctx) return;

  // 以 CSS 像素为坐标系
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
}

async function handleSave() {
  const canvas = canvasRef.value;
  if (!canvas || !ctx) return;

  // 创建临时离屏 Canvas 用于合成背景图和标注
  const offscreen = document.createElement("canvas");
  offscreen.width = canvas.width;
  offscreen.height = canvas.height;
  const offCtx = offscreen.getContext("2d");
  if (!offCtx) return;

  // 1. 绘制背景图
  const bgImg = document.querySelector(
    ".rtc-whiteboard__image"
  ) as HTMLImageElement;
  if (bgImg && bgImg.complete) {
    offCtx.drawImage(bgImg, 0, 0, offscreen.width, offscreen.height);
  }

  // 2. 绘制标注层
  offCtx.drawImage(canvas, 0, 0);

  // 3. 导出为 Blob
  offscreen.toBlob(blob => {
    if (blob) {
      emit("save", blob);
      ElMessage.success("已生成标注快照，正在上传...");
    }
  }, "image/png");
}

function clearCanvas() {
  const canvas = canvasRef.value;
  if (!canvas || !ctx) return;
  ctx.clearRect(0, 0, canvas.width, canvas.height);
}

function setTool(next: ToolType) {
  tool.value = next;
  ElMessage.info(next === "pen" ? "已切换画笔" : "已切换橡皮");
}

function getPoint(evt: PointerEvent) {
  const canvas = canvasRef.value;
  if (!canvas) return { x: 0, y: 0 };
  const rect = canvas.getBoundingClientRect();
  return {
    x: evt.clientX - rect.left,
    y: evt.clientY - rect.top
  };
}

function beginDraw(evt: PointerEvent) {
  if (!ctx) return;
  isDrawing.value = true;
  const { x, y } = getPoint(evt);
  ctx.beginPath();
  ctx.moveTo(x, y);
}

function moveDraw(evt: PointerEvent) {
  if (!ctx || !isDrawing.value) return;
  const { x, y } = getPoint(evt);

  if (tool.value === "pen") {
    ctx.globalCompositeOperation = "source-over";
    ctx.strokeStyle = penColor;
    ctx.lineWidth = penWidth;
  } else {
    // 橡皮：擦除已画内容
    ctx.globalCompositeOperation = "destination-out";
    ctx.lineWidth = eraserWidth;
  }

  ctx.lineTo(x, y);
  ctx.stroke();
}

function endDraw() {
  if (!ctx) return;
  isDrawing.value = false;
  ctx.closePath();
}

onMounted(() => {
  setupCanvas();
  const wrap = wrapRef.value;
  if (wrap && "ResizeObserver" in window) {
    resizeObserver = new ResizeObserver(() => {
      // 重新布局时清空标注，避免坐标缩放错位（演示版可接受）
      setupCanvas();
      clearCanvas();
    });
    resizeObserver.observe(wrap);
  }
});

onBeforeUnmount(() => {
  resizeObserver?.disconnect();
  resizeObserver = null;
});
</script>

<template>
  <div class="rtc-whiteboard">
    <div class="rtc-whiteboard__header">
      <div class="rtc-whiteboard__title">影像标注</div>
      <div class="rtc-whiteboard__hint">{{ hintText }}</div>
      <el-button-group>
        <el-button
          :type="tool === 'pen' ? 'primary' : 'default'"
          @click="() => setTool('pen')"
        >
          画笔
        </el-button>
        <el-button
          :type="tool === 'eraser' ? 'warning' : 'default'"
          @click="() => setTool('eraser')"
        >
          橡皮
        </el-button>
        <el-button type="danger" plain @click="clearCanvas">清空</el-button>
        <el-button type="success" @click="handleSave">保存快照</el-button>
      </el-button-group>
    </div>

    <div ref="wrapRef" class="rtc-whiteboard__stage">
      <img
        class="rtc-whiteboard__image"
        :src="props.imageSrc"
        alt="demo-image"
      />
      <canvas
        ref="canvasRef"
        class="rtc-whiteboard__canvas"
        @pointerdown="beginDraw"
        @pointermove="moveDraw"
        @pointerup="endDraw"
        @pointercancel="endDraw"
        @pointerleave="endDraw"
      />
    </div>
  </div>
</template>

<style scoped>
.rtc-whiteboard {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.rtc-whiteboard__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.rtc-whiteboard__title {
  font-weight: 600;
}

.rtc-whiteboard__hint {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  margin-right: auto;
}

.rtc-whiteboard__stage {
  position: relative;
  width: 100%;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: #0b1220;
}

.rtc-whiteboard__image {
  display: block;
  width: 100%;
  height: auto;
  user-select: none;
  pointer-events: none;
}

.rtc-whiteboard__canvas {
  position: absolute;
  inset: 0;
  touch-action: none;
  cursor: crosshair;
}
</style>
