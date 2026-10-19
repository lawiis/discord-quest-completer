<template>
  <div class="max-w-7xl mx-auto px-6 py-6 space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-white tracking-tight">Console & Diagnostic Logs</h1>
        <p class="text-xs text-[#949ba4] mt-0.5">Real-time internal IPC events, Discord RPC handshakes, and process launch logs.</p>
      </div>

      <div class="flex items-center space-x-2">
        <button
          type="button"
          class="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold transition-all border border-white/10 cursor-pointer"
          :class="showConfig ? 'bg-white/10 text-white' : 'bg-[#1e1f22] text-[#949ba4] hover:text-white'"
          @click="showConfig = !showConfig"
        >
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
          </svg>
          <span>{{ showConfig ? 'Hide RPC Config' : 'Edit Custom RPC' }}</span>
        </button>

        <button
          type="button"
          class="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-xl text-xs font-semibold transition-all shadow-md cursor-pointer"
          :class="isConnected
            ? 'bg-[#f23f43] hover:bg-[#da373c] text-white shadow-[#f23f43]/20 border border-red-500/30'
            : 'bg-[#5865F2] hover:bg-[#4752c4] text-white shadow-[#5865F2]/25 border border-white/20'"
          @click="discordTest"
        >
          <span class="w-2 h-2 rounded-full" :class="isConnected ? 'bg-white animate-pulse' : 'bg-[#23a55a]'"></span>
          <span>{{ isConnected ? 'Disconnect RPC Test' : 'Test Custom RPC' }}</span>
        </button>

        <button
          type="button"
          class="px-3 py-1.5 rounded-xl text-xs font-semibold bg-[#2b2d31]/80 hover:bg-[#383a40] text-[#dbdee1] border border-white/10 transition-colors cursor-pointer"
          @click="clearLogs"
        >
          Clear Console
        </button>
      </div>
    </div>

    <!-- Custom RPC Configuration Card (Liquid Glass) -->
    <div
      v-if="showConfig"
      class="rounded-3xl bg-[#10121a]/75 backdrop-blur-2xl border border-white/15 p-5 shadow-[0_12px_40px_rgba(0,0,0,0.4),inset_0_1px_1px_rgba(255,255,255,0.15)] space-y-4"
    >
      <div class="flex items-center justify-between pb-3 border-b border-white/[0.08]">
        <div class="flex items-center space-x-2.5">
          <div class="w-8 h-8 rounded-xl bg-[#5865F2]/20 border border-[#5865F2]/30 flex items-center justify-center text-[#5865F2]">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
              <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994.021-.041.001-.09-.041-.106a13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.929 1.793 8.18 1.793 12.061 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.893.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.028zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z" />
            </svg>
          </div>
          <div>
            <h2 class="text-xs font-bold text-white tracking-wide">Custom Discord RPC Payload</h2>
            <p class="text-[11px] text-[#949ba4]">Customize Rich Presence activity. Empty fields fall back to default.</p>
          </div>
        </div>

        <button
          type="button"
          class="px-2.5 py-1 rounded-xl text-[10px] font-semibold text-[#949ba4] hover:text-white bg-white/[0.05] hover:bg-white/[0.1] border border-white/10 transition-all cursor-pointer"
          @click="resetToDefaults"
        >
          Reset Defaults
        </button>
      </div>

      <!-- Quick Preset Selector -->
      <div class="flex items-center space-x-2 pb-2">
        <span class="text-[11px] text-[#949ba4] font-medium flex-shrink-0">Quick Preset:</span>
        <div class="flex flex-wrap gap-1.5">
          <button
            v-for="preset in GAME_PRESETS"
            :key="preset.id"
            type="button"
            class="px-2.5 py-1 rounded-lg text-[10px] font-semibold transition-all border cursor-pointer"
            :class="customRpc.appId === preset.id
              ? 'bg-[#5865F2] text-white border-white/20 shadow-sm'
              : 'bg-black/30 text-[#949ba4] hover:text-white border-white/10'"
            @click="applyPreset(preset)"
          >
            {{ preset.name }}
          </button>
        </div>
      </div>

      <!-- Grid: Form + Live Discord Preview -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <!-- Form Fields -->
        <div class="lg:col-span-2 grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <!-- Client ID -->
          <div class="space-y-1">
            <label class="block text-[11px] font-medium text-[#dbdee1]">
              Application / Client ID
              <span class="text-[9px] text-[#949ba4] font-normal">(Default: Genshin Impact)</span>
            </label>
            <input
              v-model="customRpc.appId"
              type="text"
              :placeholder="DEFAULT_CONFIG.appId"
              class="w-full px-3 py-1.5 rounded-xl bg-black/40 border border-white/10 text-xs text-white placeholder-white/20 focus:border-[#5865F2] focus:outline-none focus:ring-1 focus:ring-[#5865F2] transition-colors"
            />
          </div>

          <!-- Activity Type -->
          <div class="space-y-1">
            <label class="block text-[11px] font-medium text-[#dbdee1]">
              Activity Type
            </label>
            <select
              v-model.number="customRpc.activityKind"
              class="w-full px-3 py-1.5 rounded-xl bg-black/40 border border-white/10 text-xs text-white focus:border-[#5865F2] focus:outline-none transition-colors"
            >
              <option :value="0">Playing (0)</option>
              <option :value="2">Listening (2)</option>
              <option :value="3">Watching (3)</option>
              <option :value="5">Competing (5)</option>
            </select>
          </div>

          <!-- Details -->
          <div class="space-y-1">
            <label class="block text-[11px] font-medium text-[#dbdee1]">
              Details
              <span class="text-[9px] text-[#949ba4] font-normal">(Line 1 info)</span>
            </label>
            <input
              v-model="customRpc.details"
              type="text"
              :placeholder="DEFAULT_CONFIG.details"
              class="w-full px-3 py-1.5 rounded-xl bg-black/40 border border-white/10 text-xs text-white placeholder-white/20 focus:border-[#5865F2] focus:outline-none focus:ring-1 focus:ring-[#5865F2] transition-colors"
            />
          </div>

          <!-- State -->
          <div class="space-y-1">
            <label class="block text-[11px] font-medium text-[#dbdee1]">
              State
              <span class="text-[9px] text-[#949ba4] font-normal">(Line 2 info)</span>
            </label>
            <input
              v-model="customRpc.state"
              type="text"
              :placeholder="DEFAULT_CONFIG.state"
              class="w-full px-3 py-1.5 rounded-xl bg-black/40 border border-white/10 text-xs text-white placeholder-white/20 focus:border-[#5865F2] focus:outline-none focus:ring-1 focus:ring-[#5865F2] transition-colors"
            />
          </div>

          <!-- Large Image Key -->
          <div class="space-y-1">
            <label class="block text-[11px] font-medium text-[#dbdee1]">
              Large Image Key
              <span class="text-[9px] text-[#949ba4] font-normal">(Optional Discord Asset)</span>
            </label>
            <input
              v-model="customRpc.largeImageKey"
              type="text"
              placeholder="e.g. logo or icon"
              class="w-full px-3 py-1.5 rounded-xl bg-black/40 border border-white/10 text-xs text-white placeholder-white/20 focus:border-[#5865F2] focus:outline-none focus:ring-1 focus:ring-[#5865F2] transition-colors"
            />
          </div>

          <!-- Large Image Text -->
          <div class="space-y-1">
            <label class="block text-[11px] font-medium text-[#dbdee1]">
              Large Image Tooltip Text
            </label>
            <input
              v-model="customRpc.largeImageText"
              type="text"
              :placeholder="DEFAULT_CONFIG.largeImageText"
              class="w-full px-3 py-1.5 rounded-xl bg-black/40 border border-white/10 text-xs text-white placeholder-white/20 focus:border-[#5865F2] focus:outline-none focus:ring-1 focus:ring-[#5865F2] transition-colors"
            />
          </div>

          <!-- Time Elapsed -->
          <div class="space-y-1 sm:col-span-2">
            <label class="block text-[11px] font-medium text-[#dbdee1]">
              Simulated Elapsed Time
              <span class="text-[9px] text-[#949ba4] font-normal">(e.g. "1h 30m", "45m", "0m")</span>
            </label>
            <input
              v-model="customRpc.timeElapsed"
              type="text"
              placeholder="1h 30m"
              class="w-full px-3 py-1.5 rounded-xl bg-black/40 border border-white/10 text-xs text-white placeholder-white/20 focus:border-[#5865F2] focus:outline-none focus:ring-1 focus:ring-[#5865F2] transition-colors"
            />
          </div>
        </div>

        <!-- Live Discord Activity Preview -->
        <div class="p-4 rounded-2xl bg-black/45 border border-white/10 flex flex-col justify-between">
          <div class="space-y-3">
            <div class="flex items-center justify-between pb-2 border-b border-white/[0.06]">
              <span class="text-[10px] font-bold uppercase tracking-wider text-[#949ba4]">Discord Activity Preview</span>
              <span class="w-2 h-2 rounded-full" :class="isConnected ? 'bg-[#23a55a] animate-pulse' : 'bg-[#949ba4]/40'"></span>
            </div>

            <div class="space-y-1 bg-white/[0.03] p-3 rounded-xl border border-white/[0.05]">
              <div class="text-[10px] font-bold uppercase tracking-wider text-[#5865F2]">
                {{ activityKindLabel }}
              </div>
              <div class="text-xs font-bold text-white truncate">
                {{ appNamePreview }}
              </div>
              <div class="text-[11px] text-[#dbdee1] truncate">
                {{ effectiveDetails }}
              </div>
              <div class="text-[11px] text-[#949ba4] truncate">
                {{ effectiveState }}
              </div>
              <div class="text-[10px] text-[#23a55a] font-mono pt-0.5 flex items-center space-x-1">
                <span>🎮</span>
                <span>{{ effectiveTimeElapsed }}</span>
              </div>
            </div>
          </div>

          <div class="pt-3 border-t border-white/[0.06] flex items-center justify-between text-[10px] text-[#949ba4]">
            <span>App ID:</span>
            <span class="font-mono text-cyan-300 truncate max-w-[130px]">{{ effectiveAppId }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Terminal Console Window -->
    <div class="rounded-2xl border border-white/10 bg-[#111214]/90 backdrop-blur-xl shadow-xl overflow-hidden font-mono text-xs">
      <!-- Terminal Header -->
      <div class="bg-[#1e1f22]/90 px-4 py-2.5 border-b border-white/[0.06] flex items-center justify-between select-none">
        <div class="flex items-center space-x-2">
          <div class="flex space-x-1.5">
            <span class="w-3 h-3 rounded-full bg-[#f23f43]/80 inline-block"></span>
            <span class="w-3 h-3 rounded-full bg-[#f0b232]/80 inline-block"></span>
            <span class="w-3 h-3 rounded-full bg-[#23a55a]/80 inline-block"></span>
          </div>
          <span class="text-[11px] font-sans font-semibold text-[#949ba4] ml-2">App Diagnostic Output</span>
        </div>

        <span class="text-[10px] text-[#949ba4] bg-[#2b2d31] px-2 py-0.5 rounded border border-[#3f4147]">
          {{ logs.length }} events recorded
        </span>
      </div>

      <!-- Logs Body -->
      <div class="p-4 max-h-[55vh] overflow-y-auto space-y-1.5">
        <div v-if="logs.length === 0" class="py-16 text-center text-[#949ba4] font-sans text-xs">
          <svg class="w-8 h-8 mx-auto mb-2 text-[#3f4147]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 9l3 3-3 3m5 0h3M5 20h14a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <p class="font-medium text-[#dbdee1]">Console is clean</p>
          <p class="text-[11px] text-[#949ba4] mt-0.5">Application logs, process lifecycle events, and RPC errors will stream here.</p>
        </div>

        <div
          v-for="(log, index) in logs"
          :key="index"
          class="flex items-start space-x-2 py-1 px-2 rounded hover:bg-[#1e1f22]/50 transition-colors leading-relaxed"
        >
          <span class="text-[#949ba4] text-[11px] flex-shrink-0 select-none font-mono">
            {{ formatTime(log.timestamp) }}
          </span>

          <span
            class="px-1.5 py-0.2 rounded text-[10px] font-bold uppercase tracking-wider flex-shrink-0"
            :class="{
              'bg-[#5865F2]/20 text-[#5865F2] border border-[#5865F2]/30': log.type === 'info',
              'bg-[#f23f43]/20 text-[#f23f43] border border-[#f23f43]/30': log.type === 'error',
              'bg-[#f0b232]/20 text-[#f0b232] border border-[#f0b232]/30': log.type === 'warning',
              'bg-[#23a55a]/20 text-[#23a55a] border border-[#23a55a]/30': log.type === 'debug'
            }"
          >
            {{ log.type }}
          </span>

          <span class="text-[#f2f3f5] break-all">
            {{ log.message }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { invoke } from '@tauri-apps/api/core';
import { emit } from '@tauri-apps/api/event';
import { useGlobalState } from '@/composables/app-state';

const GAME_PRESETS = [
  { name: 'Genshin Impact', id: '762434991303950386', details: 'Exploring Teyvat', state: 'Completing Quests' },
  { name: 'VALORANT', id: '700136079562375258', details: 'Competitive', state: 'In Match' },
  { name: 'Honkai: Star Rail', id: '1121201675240210523', details: 'Astral Express', state: 'Exploring' },
  { name: 'Minecraft', id: '1402418491272986635', details: 'Survival Mode', state: 'Building' },
  { name: 'Fortnite', id: '1402418703554842694', details: 'Battle Royale', state: 'In Lobby' },
];

const DEFAULT_CONFIG = {
  appId: '762434991303950386', // Genshin Impact
  details: 'Discord Quest Completer',
  state: 'Simulating Playtime',
  activityKind: 0,
  largeImageKey: '',
  largeImageText: 'Discord Quest Completer',
  timeElapsed: '1h 30m',
};

const showConfig = ref(true);
const isConnected = ref(false);
const { logs, clearLogs, addLog } = useGlobalState();

const savedConfigStr = localStorage.getItem('dqc_custom_rpc');
let initialConfig = { ...DEFAULT_CONFIG };
if (savedConfigStr) {
  try {
    const parsed = JSON.parse(savedConfigStr);
    // Sanitize away old hardcoded troll appId
    if (parsed.appId === '1361728268088381706') {
      parsed.appId = DEFAULT_CONFIG.appId;
    }
    initialConfig = { ...DEFAULT_CONFIG, ...parsed };
  } catch (e) {
    console.error('Failed to parse saved custom RPC config:', e);
  }
}
const customRpc = ref(initialConfig);

watch(
  customRpc,
  (val) => {
    localStorage.setItem('dqc_custom_rpc', JSON.stringify(val));
  },
  { deep: true }
);

function applyPreset(preset: (typeof GAME_PRESETS)[number]) {
  customRpc.value.appId = preset.id;
  customRpc.value.details = preset.details;
  customRpc.value.state = preset.state;
  addLog('info', `Applied preset: ${preset.name} (App ID: ${preset.id})`);
}

function resetToDefaults() {
  customRpc.value = { ...DEFAULT_CONFIG };
  localStorage.removeItem('dqc_custom_rpc');
  addLog('info', 'Custom RPC reset to default preset (Genshin Impact).');
}

const effectiveAppId = computed(() => customRpc.value.appId?.trim() || DEFAULT_CONFIG.appId);
const effectiveDetails = computed(() => customRpc.value.details?.trim() || DEFAULT_CONFIG.details);
const effectiveState = computed(() => customRpc.value.state?.trim() || DEFAULT_CONFIG.state);
const effectiveTimeElapsed = computed(() => customRpc.value.timeElapsed?.trim() || DEFAULT_CONFIG.timeElapsed);

const appNamePreview = computed(() => {
  const match = GAME_PRESETS.find((p) => p.id === effectiveAppId.value);
  if (match) return match.name;
  return `App (${effectiveAppId.value.slice(0, 8)}...)`;
});

const activityKindLabel = computed(() => {
  switch (Number(customRpc.value.activityKind)) {
    case 2:
      return 'Listening to';
    case 3:
      return 'Watching';
    case 5:
      return 'Competing in';
    default:
      return 'Playing a game';
  }
});

function formatTime(timestamp: number | string | Date) {
  const d = new Date(timestamp);
  return d.toTimeString().split(' ')[0] + '.' + String(d.getMilliseconds()).padStart(3, '0');
}

function discordTest() {
  if (isConnected.value) {
    emit('event_disconnect');
    isConnected.value = false;
    addLog('info', 'Disconnected Custom Discord RPC.');
    return;
  }

  const appId = effectiveAppId.value;
  const details = effectiveDetails.value;
  const state = effectiveState.value;
  const activityKind = Number(customRpc.value.activityKind ?? DEFAULT_CONFIG.activityKind);
  const largeKey = customRpc.value.largeImageKey?.trim() || undefined;
  const largeText = customRpc.value.largeImageText?.trim() || (largeKey ? DEFAULT_CONFIG.largeImageText : undefined);

  const payload: Record<string, any> = {
    app_id: appId,
    details: details,
    state: state,
    activity_kind: activityKind,
    timestamp: createAgoTimestamp(effectiveTimeElapsed.value),
  };

  if (largeKey) {
    payload.largeImageKey = largeKey;
    if (largeText) {
      payload.largeImageText = largeText;
    }
  }

  addLog('info', `Testing Custom RPC -> App ID: ${appId} | Details: "${details}" | State: "${state}"`);

  invoke('connect_to_discord_rpc_3', {
    activity_json: JSON.stringify(payload),
    action: 'connect',
  })
    .then(() => {
      isConnected.value = true;
      addLog('info', 'Discord RPC connection established successfully.');
    })
    .catch((err) => {
      isConnected.value = false;
      addLog('error', 'RPC connection failed: ' + String(err));
    });
}

function createAgoTimestamp(input: string) {
  let h = 0;
  let m = 0;
  input.split(' ').forEach((t) => {
    if (t.includes('h')) h = parseInt(t) || 0;
    if (t.includes('m')) m = parseInt(t) || 0;
  });
  return Math.floor((Date.now() - (h * 3600 + m * 60) * 1000) / 1000);
}
</script>