<template>
  <div class="space-y-3">
    <div class="flex items-center justify-between">
      <h4 class="text-xs font-semibold uppercase tracking-wider text-[#949ba4]">
        Detectable Executables ({{ filteredExecutables.length }})
      </h4>
      <span class="text-[11px] text-[#949ba4]">Launch to trigger Discord quest timer</span>
    </div>

    <div v-if="filteredExecutables.length === 0" class="text-center py-5 text-xs text-[#949ba4] bg-[#18191c]/50 rounded-2xl border border-white/[0.06]">
      No compatible Windows executables declared for this game.
    </div>

    <div v-else class="space-y-2">
      <div
        v-for="executable in filteredExecutables"
        :key="executable.name"
        class="group flex items-center justify-between p-3 rounded-2xl border transition-all duration-200"
        :class="gameActions?.isExecutableRunning(executable)
          ? 'border-emerald-500/40 bg-emerald-500/10 shadow-lg shadow-emerald-500/10'
          : 'border-white/[0.08] bg-[#14161f]/45 backdrop-blur-md hover:border-[#5865F2]/40 hover:bg-[#1c1f2e]/60'"
      >
        <!-- Executable Info -->
        <div class="flex items-center space-x-3 min-w-0 mr-3">
          <div class="flex-shrink-0">
            <span class="inline-flex items-center px-2 py-0.5 rounded-lg text-[10px] font-bold uppercase tracking-wide bg-[#232428] text-[#949ba4] border border-white/[0.08]">
              {{ executable.os || 'WIN' }}
            </span>
          </div>

          <div class="min-w-0">
            <div class="flex items-center space-x-2">
              <span class="font-mono text-xs font-semibold text-white truncate group-hover:text-[#5865F2] transition-colors">
                {{ getFilename(executable) }}
              </span>
              <span
                v-if="gameActions?.isExecutableRunning(executable)"
                class="inline-flex items-center space-x-1 px-2 py-0.5 text-[9px] font-bold text-emerald-400 bg-emerald-500/15 rounded-full border border-emerald-500/30 shadow-[0_0_8px_rgba(16,185,129,0.25)]"
              >
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping inline-block"></span>
                <span>RUNNING</span>
              </span>
            </div>
            <p v-if="getExecutablePath(executable)" class="text-[10px] text-[#949ba4] font-mono truncate mt-0.5" :title="getExecutablePath(executable)">
              {{ getExecutablePath(executable) }}
            </p>
          </div>
        </div>

        <!-- Action Button -->
        <button
          type="button"
          class="flex-shrink-0 flex items-center space-x-1.5 px-3.5 py-1.5 rounded-xl text-xs font-semibold shadow-md transition-all duration-200"
          :class="gameActions?.isExecutableRunning(executable)
            ? 'bg-[#f23f43] hover:bg-[#da373c] text-white shadow-[#f23f43]/20'
            : 'bg-[#5865F2] hover:bg-[#4752c4] text-white shadow-[#5865F2]/30 hover:scale-[1.02]'"
          @click="handleLaunch(executable)"
        >
          <svg v-if="gameActions?.isExecutableRunning(executable)" class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
            <rect x="4" y="4" width="12" height="12" rx="2" />
          </svg>
          <svg v-else class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z" clip-rule="evenodd" />
          </svg>
          <span>{{ gameActions?.isExecutableRunning(executable) ? 'Stop Process' : 'Run Dummy' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { EXECUTABLE_OS, GameActionsKey } from '@/constants/constants';
import { GameActionsProvider, type Game, type GameExecutable } from '@/types/types';
import { path } from '@tauri-apps/api';
import { computed, inject } from 'vue';

const props = defineProps<{
  game: Game;
}>();

const emit = defineEmits<{
  play: [{ game: Game; executable: GameExecutable }];
  stop: [{ game: Game; executable: GameExecutable }];
  install_and_play: [{ game: Game; executable: GameExecutable }];
}>();

const gameActions = inject<GameActionsProvider>(GameActionsKey);

const filteredExecutables = computed(() => {
  return props.game.executables.filter(executable => {
    return executable.os !== EXECUTABLE_OS.LINUX && executable.os !== EXECUTABLE_OS.DARWIN
      && !isValidPath(executable.name);
  });
});

function splitExecutableName(executable: GameExecutable) {
  const allSections = executable.name.split(/\\|\//);
  const last = executable.name.split(/\\|\//).pop();
  const name = last?.split('.').slice(0, -1).join('.') || last;
  return [
    ...allSections.slice(0, -1),
    name,
  ];
}

function getExecutablePath(executable: GameExecutable) {
  const allSections = executable.name.split(/\\|\//);
  return allSections.slice(0, -1).join('/');
}

function getFilename(executable: GameExecutable) {
  const last = executable.name.split(/\\|\//).pop();
  return last || executable.name;
}

function isValidPath(filePath: string) {
  const illegalChars = ['>', '<', ':', '"', '|', '?', '*'];
  return illegalChars.some(char => filePath.includes(char));
}

function handleLaunch(executable: GameExecutable) {
  const fullExe = {
    ...executable,
    path: getExecutablePath(executable),
    segments: splitExecutableName(executable).length,
    filename: getFilename(executable),
  };
  const p = { game: props.game, executable: fullExe };
  if (executable.is_running) {
    emit('stop', p);
  } else if (!gameActions?.isGameExecutableInstalled(executable)) {
    emit('install_and_play', p);
  } else {
    emit('play', p);
  }
}
</script>