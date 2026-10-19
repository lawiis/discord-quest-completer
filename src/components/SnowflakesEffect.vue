<template>
  <canvas
    ref="canvasRef"
    class="fixed inset-0 pointer-events-none z-10 transition-opacity duration-700"
    :class="enabled ? 'opacity-100' : 'opacity-0'"
  />
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue';

const props = withDefaults(defineProps<{
  enabled?: boolean;
  speed?: 'slow' | 'normal' | 'fast';
  density?: 'gentle' | 'normal' | 'blizzard';
}>(), {
  enabled: true,
  speed: 'normal',
  density: 'normal',
});

const canvasRef = ref<HTMLCanvasElement | null>(null);
type F = [number, number, number, number, number, number, number, number]; // x, y, r, vy, vx, phase, opacity, layer
let f: F[] = [];
let aid: number | null = null;
let w = 0, h = 0;

function mult() {
  return props.speed === 'slow' ? 0.55 : props.speed === 'fast' ? 1.6 : 1.0;
}

function getCount() {
  const base = props.density === 'gentle' ? 50 : props.density === 'blizzard' ? 165 : 95;
  return Math.min(base, Math.floor((w * h) / (props.density === 'blizzard' ? 5000 : 9000)));
}

function init() {
  if (!canvasRef.value) return;
  w = canvasRef.value.width = window.innerWidth;
  h = canvasRef.value.height = window.innerHeight;
  const n = getCount();
  f = [];
  for (let i = 0; i < n; i++) {
    const layer = Math.random() < 0.25 ? 2 : Math.random() < 0.65 ? 1 : 0; // 0=back, 1=mid, 2=front
    const r = layer === 2 ? Math.random() * 2.2 + 2.8 : layer === 1 ? Math.random() * 1.5 + 1.4 : Math.random() * 0.9 + 0.7;
    const vy = layer === 2 ? Math.random() * 0.9 + 0.8 : layer === 1 ? Math.random() * 0.6 + 0.4 : Math.random() * 0.35 + 0.2;
    const op = layer === 2 ? Math.random() * 0.3 + 0.65 : layer === 1 ? Math.random() * 0.3 + 0.45 : Math.random() * 0.25 + 0.2;
    f.push([
      Math.random() * w,
      Math.random() * h,
      r,
      vy,
      Math.random() * 0.02 + 0.008,
      Math.random() * 6.28,
      op,
      layer,
    ]);
  }
}

function render(t: number) {
  if (!canvasRef.value || !props.enabled) return;
  const c = canvasRef.value.getContext('2d');
  if (!c) return;
  c.clearRect(0, 0, w, h);
  const m = mult();
  for (let i = 0; i < f.length; i++) {
    const p = f[i];
    p[1] += p[3] * m;
    p[0] += Math.sin(t * p[4] + p[5]) * (p[7] === 2 ? 0.9 : 0.5) * m;
    if (p[1] > h + 10) { p[1] = -10; p[0] = Math.random() * w; }
    if (p[0] > w + 10) p[0] = -10; else if (p[0] < -10) p[0] = w + 10;
    c.save();
    c.beginPath();
    c.arc(p[0], p[1], p[2], 0, 6.28);
    c.fillStyle = `rgba(235,245,255,${p[6]})`;
    c.shadowBlur = p[7] ? p[2] * 2.5 : 0;
    c.shadowColor = 'rgba(200,230,255,0.6)';
    c.fill();
    c.restore();
  }
  aid = requestAnimationFrame(render);
}

function start() {
  if (aid) cancelAnimationFrame(aid);
  init();
  aid = requestAnimationFrame(render);
}

function stop() {
  if (aid) { cancelAnimationFrame(aid); aid = null; }
  const c = canvasRef.value?.getContext('2d');
  if (c) c.clearRect(0, 0, w, h);
}

watch(() => [props.enabled, props.density], ([en]) => en ? start() : stop());
onMounted(() => { window.addEventListener('resize', init, { passive: true }); if (props.enabled) start(); });
onUnmounted(() => { window.removeEventListener('resize', init); stop(); });
</script>
