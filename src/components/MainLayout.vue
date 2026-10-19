<script setup lang="ts">
import { computed, useTemplateRef } from 'vue';
import { Pages, useGlobalState } from '@/composables/app-state';
import { convertFileSrc } from '@tauri-apps/api/core';
import IconRustLang from './IconRustLang.vue';
import IconVueJs from './IconVueJs.vue';
import SnowflakesEffect from './SnowflakesEffect.vue';

const appState = useGlobalState();
const {
  page,
  setPage,
  snowflakesEnabled,
  setSnowflakesEnabled,
  snowflakesSpeed,
  setSnowflakesSpeed,
  snowflakesDensity,
  setSnowflakesDensity,
  bgBlur,
  setBgBlur,
  bgOpacity,
  setBgOpacity,
  bgBrightness,
  setBgBrightness,
  bgImage,
  setBgImage,
  resetBgImage,
  isSettingsOpen,
  openSettings,
  closeSettings,
} = appState;

const fileInputRef = useTemplateRef<HTMLInputElement>('fileInputRef');

const effectiveBgUrl = computed(() => {
  if (bgImage.value) return bgImage.value;
  try {
    return convertFileSrc('d:/cdc/bg.png');
  } catch {
    return '';
  }
});

function handleFileSelect(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0];
  if (!file) return;
  const reader = new FileReader();
  reader.onload = () => {
    if (typeof reader.result === 'string') {
      setBgImage(reader.result);
    }
  };
  reader.readAsDataURL(file);
}
</script>

<template>
  <div class="relative flex flex-col h-dvh overflow-hidden bg-[#0a0b0e] text-[#dbdee1] select-none">
    <!-- Fixed Background Image Layer with Dynamic Blur -->
    <div
      class="fixed inset-0 pointer-events-none z-0 bg-cover bg-center bg-no-repeat transition-all duration-300"
      :style="{
        backgroundImage: `url('${effectiveBgUrl}')`,
        filter: `blur(${bgBlur}px) brightness(${bgBrightness}%)`,
        opacity: bgOpacity / 100,
        transform: 'scale(1.08)',
      }"
    />

    <!-- Liquid Glass Translucent Tint Overlay -->
    <div class="fixed inset-0 pointer-events-none z-0 bg-[#08090c]/45" />

    <!-- Atmospheric Snowflakes Particle Canvas -->
    <SnowflakesEffect
      :enabled="snowflakesEnabled"
      :speed="snowflakesSpeed"
      :density="snowflakesDensity"
    />

    <!-- Top Navigation Bar (Liquid Glass) -->
    <header class="relative z-30 bg-[#0e1017]/55 backdrop-blur-2xl border-b border-white/[0.12] shadow-[0_4px_30px_rgba(0,0,0,0.4)]">
      <div class="px-5 py-3 flex items-center justify-between">
        <!-- Brand -->
        <div class="flex items-center space-x-3">
          <div class="relative flex items-center justify-center p-2 rounded-2xl bg-gradient-to-br from-[#5865F2] to-[#404eed] shadow-lg shadow-[#5865F2]/30 border border-white/20">
            <img src="/logo.svg" alt="Discord Quest Completer" class="h-6 w-6 brightness-110 drop-shadow-sm" />
          </div>
          <div>
            <div class="flex items-center space-x-2">
              <span class="text-base font-bold text-white tracking-tight drop-shadow-sm">Discord Quest Completer</span>
              <span class="px-2 py-0.5 text-[10px] font-semibold bg-white/[0.08] text-[#dbdee1] border border-white/[0.12] rounded-full tracking-wider shadow-inner">v25.10.7</span>
            </div>
            <p class="text-[11px] text-[#949ba4] mt-0.5 font-normal">Discord Quest Simulator</p>
          </div>
        </div>

        <!-- Navigation Tabs & Settings -->
        <div class="flex items-center space-x-2">
          <nav class="flex items-center space-x-1 bg-black/40 backdrop-blur-xl p-1 rounded-2xl border border-white/[0.1] shadow-[inset_0_1px_1px_rgba(255,255,255,0.1)]">
            <button
              type="button"
              class="flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-semibold transition-all duration-200"
              :class="page === Pages.HOME
                ? 'bg-[#5865F2]/80 backdrop-blur-md text-white shadow-md shadow-[#5865F2]/35 border border-white/20'
                : 'text-[#949ba4] hover:text-[#dbdee1] hover:bg-white/[0.06]'"
              @click="setPage(Pages.HOME)"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
              <span>Quest Manager</span>
            </button>

            <button
              type="button"
              class="flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-semibold transition-all duration-200"
              :class="page === Pages.PLAYGROUND
                ? 'bg-[#5865F2]/80 backdrop-blur-md text-white shadow-md shadow-[#5865F2]/35 border border-white/20'
                : 'text-[#949ba4] hover:text-[#dbdee1] hover:bg-white/[0.06]'"
              @click="setPage(Pages.PLAYGROUND)"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 9l3 3-3 3m5 0h3M5 20h14a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span>Console & Logs</span>
            </button>
          </nav>

          <!-- Settings Button -->
          <button
            type="button"
            class="flex items-center justify-center p-2 rounded-2xl border transition-all duration-200"
            :class="isSettingsOpen
              ? 'bg-[#5865F2] text-white border-white/30 shadow-md shadow-[#5865F2]/40'
              : 'bg-black/40 backdrop-blur-xl text-[#949ba4] border-white/[0.1] hover:text-white hover:bg-white/[0.08] shadow-[inset_0_1px_1px_rgba(255,255,255,0.1)]'"
            @click="isSettingsOpen ? closeSettings() : openSettings()"
          >
            <svg class="w-4 h-4 transition-transform duration-300" :class="{ 'rotate-90': isSettingsOpen }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
          </button>
        </div>
      </div>
    </header>

    <!-- Main Content Viewport (Translucent to display liquid glass & background) -->
    <main class="relative z-20 flex-grow overflow-y-auto bg-transparent">
      <slot></slot>
    </main>

    <!-- Footer Bar (Liquid Glass) -->
    <footer class="relative z-30 bg-[#0e1017]/55 backdrop-blur-2xl border-t border-white/[0.1] px-5 py-2.5 flex items-center justify-between text-[11px] text-[#949ba4] shadow-[0_-4px_30px_rgba(0,0,0,0.4)]">
      <div class="flex items-center space-x-2">
        <span class="inline-block w-2 h-2 rounded-full bg-[#23a55a] shadow-[0_0_8px_rgba(35,165,90,0.8)] animate-pulse"></span>
        <span class="font-medium text-[#dbdee1]">Game Runner Active</span>
        <span>•</span>
        <span class="text-[#949ba4]">Pop-up Window</span>
      </div>
      <div class="flex items-center space-x-2">
        <span class="inline-flex items-center space-x-1 text-[#f2f3f5] font-semibold">
          <IconRustLang class="h-3.5 w-3.5 text-white" />
          <span>Rust</span>
        </span>
        <span>+</span>
        <span class="inline-flex items-center space-x-1 text-[#4FC08D] font-semibold">
          <IconVueJs class="h-3.5 w-3.5" />
          <span>Vue 3</span>
        </span>
      </div>
    </footer>

    <!-- Settings Modal Dialog -->
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0 scale-95"
      enter-to-class="opacity-100 scale-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100 scale-100"
      leave-to-class="opacity-0 scale-95"
    >
      <div
        v-if="isSettingsOpen"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-md"
        @click.self="closeSettings"
      >
        <div class="w-full max-w-lg bg-[#14161f]/95 backdrop-blur-3xl border border-white/15 rounded-3xl shadow-[0_20px_50px_rgba(0,0,0,0.8),inset_0_1px_1px_rgba(255,255,255,0.2)] max-h-[85vh] flex flex-col overflow-hidden">
          <!-- Modal Header -->
          <div class="flex items-center justify-between px-6 pt-5 pb-3 border-b border-white/[0.08] shrink-0">
            <div class="flex items-center space-x-2.5">
              <div class="p-2 rounded-xl bg-[#5865F2]/20 text-[#5865F2] border border-[#5865F2]/30 shadow-sm">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
                </svg>
              </div>
              <div>
                <h3 class="text-sm font-bold text-white tracking-wide">Atmosphere Settings</h3>
                <p class="text-[11px] text-[#949ba4]">Blur, wallpaper, and snowfall</p>
              </div>
            </div>

            <button
              type="button"
              class="p-1.5 rounded-lg text-[#949ba4] hover:text-white hover:bg-white/[0.06] transition-colors"
              @click="closeSettings"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Settings Items -->
          <div class="p-6 overflow-y-auto space-y-4 flex-1">
            <!-- Background Wallpaper & Blur Card -->
            <div class="p-4 rounded-2xl bg-[#10121a]/70 backdrop-blur-xl border border-white/[0.1] shadow-inner space-y-3.5">
              <div class="flex items-center justify-between">
                <div class="flex items-center space-x-3">
                  <div class="w-8 h-8 rounded-xl bg-purple-500/15 border border-purple-500/30 flex items-center justify-center text-purple-300">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                    </svg>
                  </div>
                  <div>
                    <span class="text-xs font-semibold text-white">Wallpaper & Liquid Blur</span>
                    <p class="text-[11px] text-[#949ba4]">Controls background blur and visibility</p>
                  </div>
                </div>

                <div class="flex items-center space-x-1.5">
                  <input
                    ref="fileInputRef"
                    type="file"
                    accept="image/*"
                    class="hidden"
                    @change="handleFileSelect"
                  />
                  <button
                    type="button"
                    class="px-2.5 py-1 rounded-xl text-[10px] font-semibold bg-white/[0.08] hover:bg-white/[0.14] text-white border border-white/15 transition-all shadow-sm"
                    @click="fileInputRef?.click()"
                  >
                    Upload Image
                  </button>
                  <button
                    v-if="bgImage"
                    type="button"
                    class="px-2 py-1 rounded-xl text-[10px] font-semibold text-red-400 hover:bg-red-500/15 border border-red-500/20 transition-all"
                    @click="resetBgImage"
                  >
                    Reset
                  </button>
                </div>
              </div>

              <!-- Blur Slider -->
              <div class="space-y-1.5 pt-1">
                <div class="flex justify-between text-[11px]">
                  <span class="text-[#949ba4]">Background Blur</span>
                  <span class="font-mono text-cyan-300 font-semibold">{{ bgBlur }}px</span>
                </div>
                <input
                  type="range"
                  min="0"
                  max="32"
                  step="1"
                  :value="bgBlur"
                  @input="setBgBlur(Number(($event.target as HTMLInputElement).value))"
                  class="w-full h-1.5 bg-white/10 rounded-lg appearance-none cursor-pointer accent-[#5865F2]"
                />
              </div>

              <!-- Opacity Slider -->
              <div class="space-y-1.5">
                <div class="flex justify-between text-[11px]">
                  <span class="text-[#949ba4]">Artwork Opacity</span>
                  <span class="font-mono text-cyan-300 font-semibold">{{ bgOpacity }}%</span>
                </div>
                <input
                  type="range"
                  min="15"
                  max="100"
                  step="5"
                  :value="bgOpacity"
                  @input="setBgOpacity(Number(($event.target as HTMLInputElement).value))"
                  class="w-full h-1.5 bg-white/10 rounded-lg appearance-none cursor-pointer accent-[#5865F2]"
                />
              </div>

              <!-- Brightness Slider -->
              <div class="space-y-1.5">
                <div class="flex justify-between text-[11px]">
                  <span class="text-[#949ba4]">Artwork Brightness</span>
                  <span class="font-mono text-cyan-300 font-semibold">{{ bgBrightness }}%</span>
                </div>
                <input
                  type="range"
                  min="20"
                  max="120"
                  step="5"
                  :value="bgBrightness"
                  @input="setBgBrightness(Number(($event.target as HTMLInputElement).value))"
                  class="w-full h-1.5 bg-white/10 rounded-lg appearance-none cursor-pointer accent-[#5865F2]"
                />
              </div>
            </div>

            <!-- Snowflakes Particle Card -->
            <div class="p-4 rounded-2xl bg-[#10121a]/70 backdrop-blur-xl border border-white/[0.1] shadow-inner space-y-3.5">
              <div class="flex items-center justify-between">
                <div class="flex items-center space-x-3">
                  <div class="w-8 h-8 rounded-xl bg-cyan-500/15 border border-cyan-500/30 flex items-center justify-center text-cyan-300">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v18m9-9H3m15.364-6.364l-12.728 12.728m0-12.728l12.728 12.728" />
                    </svg>
                  </div>
                  <div>
                    <div class="flex items-center space-x-2">
                      <span class="text-xs font-semibold text-white">Snowflakes</span>
                      <span
                        class="px-1.5 py-0.2 rounded-full text-[9px] font-bold uppercase tracking-wider"
                        :class="snowflakesEnabled ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/30' : 'bg-white/[0.06] text-[#949ba4]'"
                      >
                        {{ snowflakesEnabled ? 'On' : 'Off' }}
                      </span>
                    </div>
                    <p class="text-[11px] text-[#949ba4] mt-0.5">Particle snowfall</p>
                  </div>
                </div>

                <!-- Toggle Switch -->
                <button
                  type="button"
                  class="relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none"
                  :class="snowflakesEnabled ? 'bg-[#5865F2]' : 'bg-[#383a40]'"
                  @click="setSnowflakesEnabled(!snowflakesEnabled)"
                >
                  <span
                    class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-md ring-0 transition duration-200 ease-in-out"
                    :class="snowflakesEnabled ? 'translate-x-5' : 'translate-x-0'"
                  />
                </button>
              </div>

              <!-- Controls if enabled -->
              <div v-if="snowflakesEnabled" class="pt-2 border-t border-white/[0.06] space-y-2.5">
                <!-- Density Selector -->
                <div class="flex items-center justify-between">
                  <span class="text-[11px] text-[#949ba4]">Particle Density:</span>
                  <div class="flex items-center space-x-1 bg-black/40 p-0.5 rounded-xl border border-white/[0.08]">
                    <button
                      v-for="d in (['gentle', 'normal', 'blizzard'] as const)"
                      :key="d"
                      type="button"
                      class="px-2.5 py-0.5 rounded-lg text-[10px] font-semibold capitalize transition-all"
                      :class="snowflakesDensity === d
                        ? 'bg-[#5865F2] text-white shadow-sm border border-white/20'
                        : 'text-[#949ba4] hover:text-white'"
                      @click="setSnowflakesDensity(d)"
                    >
                      {{ d }}
                    </button>
                  </div>
                </div>

                <!-- Speed Selector -->
                <div class="flex items-center justify-between">
                  <span class="text-[11px] text-[#949ba4]">Drift Speed:</span>
                  <div class="flex items-center space-x-1 bg-black/40 p-0.5 rounded-xl border border-white/[0.08]">
                    <button
                      v-for="s in (['slow', 'normal', 'fast'] as const)"
                      :key="s"
                      type="button"
                      class="px-2.5 py-0.5 rounded-lg text-[10px] font-semibold capitalize transition-all"
                      :class="snowflakesSpeed === s
                        ? 'bg-[#5865F2] text-white shadow-sm border border-white/20'
                        : 'text-[#949ba4] hover:text-white'"
                      @click="setSnowflakesSpeed(s)"
                    >
                      {{ s }}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Background Dummy Automation Info Card -->
            <div class="p-4 rounded-2xl bg-[#10121a]/70 backdrop-blur-xl border border-white/[0.1] shadow-inner space-y-2">
              <div class="flex items-center space-x-3">
                <div class="w-8 h-8 rounded-xl bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center text-emerald-400">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                  </svg>
                </div>
                <div>
                  <div class="flex items-center space-x-2">
                    <span class="text-xs font-semibold text-white">Pop-up Window Mode</span>
                    <span class="px-1.5 py-0.2 rounded-full text-[9px] font-bold bg-[#23a55a]/15 text-[#23a55a] border border-[#23a55a]/30">Active</span>
                  </div>
                  <p class="text-[11px] text-[#949ba4] mt-0.5">Game dummy pop-up window is visible and active.</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="flex justify-end px-6 py-3.5 border-t border-white/[0.08] bg-[#0c0d12]/60 shrink-0">
            <button
              type="button"
              class="px-5 py-2 text-xs font-semibold rounded-xl bg-[#5865F2] hover:bg-[#4752c4] text-white shadow-md shadow-[#5865F2]/25 border border-white/20 transition-all cursor-pointer"
              @click="closeSettings"
            >
              Done
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>