<script setup lang="ts">
import { ref, computed, useTemplateRef, shallowRef, provide } from 'vue';
import { onClickOutside, refDebounced } from '@vueuse/core';
import { useFuse } from '@vueuse/integrations/useFuse';
import { invoke } from '@tauri-apps/api/core';
import { randomString } from '@/utils/random-string';
import { GameActionsProvider, GameExecutable, type Game } from '@/types/types';
import IconVerified from '@/components/IconVerified.vue';
import GameExecutables from '@/components/GameExecutables.vue';
import { GameActionsKey } from '@/constants/constants';
import { emit } from '@tauri-apps/api/event';
import { useFetchGameList } from '@/composables/fetch-gamelist';
import { UseFuseOptions } from '@vueuse/integrations';
import Fuse from 'fuse.js';
import { useGlobalState } from '@/composables/app-state';

type DialogKey = 
    'none' | 
    'rpc_message_1'|
    'no_game_selected';

const {
    gameDB,
    isLoadingBundled,
    isLoadingDiscord,
    isLoadingGH,
    fetchGameList,
    isReadyGH,
    isReadyBundled,
    isReadyDiscord,
    allFetchDone,
} = useFetchGameList();

const isAnyFetching = computed(() => isLoadingGH.value || isLoadingDiscord.value || isLoadingBundled.value);

const { addLog } = useGlobalState();

const dialogRef = useTemplateRef<HTMLDialogElement>('dialogRef');
const searchResultContainerRef = useTemplateRef<HTMLElement>('searchResultContainerRef');
const dialogMessage = ref('');
const isDialogOpen = ref(false);
const dialogKey = ref<DialogKey>('none');
const isConnectedToRPC = ref(false);
const isConnecting = ref(false);

// Background execution notification state
const backgroundNotify = ref<{
    show: boolean;
    gameName: string;
    exeName: string;
    timer?: ReturnType<typeof setTimeout>;
}>({
    show: false,
    gameName: '',
    exeName: '',
});

function triggerBackgroundNotify(gameName: string, exeName: string) {
    if (backgroundNotify.value.timer) clearTimeout(backgroundNotify.value.timer);
    backgroundNotify.value = {
        show: true,
        gameName,
        exeName,
        timer: setTimeout(() => { backgroundNotify.value.show = false; }, 6000),
    };

    try {
        if (typeof Notification !== 'undefined') {
            const send = () => new Notification(`Background: ${gameName}`, {
                body: `"${exeName}" window running. Pop-up active.`,
                icon: '/logo.svg',
            });
            if (Notification.permission === 'granted') send();
            else if (Notification.permission !== 'denied') {
                Notification.requestPermission().then(p => { if (p === 'granted') send(); });
            }
        }
    } catch {}
}

// Search functionality
const searchQuery = shallowRef('');
const debouncedSearchQuery = refDebounced(searchQuery, 250);

const searchResultsIsOpen = ref(false);
const isOnSearchResults = ref(false);

// Game status
const currentlyPlaying = ref<string | null>(null);

onClickOutside(searchResultContainerRef, () => {
    searchResultsIsOpen.value = false;
});

const ignoredSymbolsRegex = /[\u00A9\u2122\u00AE]/g;

const fuseOptions = computed<UseFuseOptions<Game>>(() => ({
    fuseOptions: {
        keys: [
            { name: 'name', weight: 0.7 },
            { name: 'aliases', weight: 0.2 },
            { name: 'executables.name', weight: 0.1 },
        ],
        getFn: (obj: any, path: string[] | string) => {
            const value = Fuse.config.getFn(obj, path);
            return typeof value === "string"
                ? value.replace(ignoredSymbolsRegex, "")
                : value;
        },
        isCaseSensitive: false,
        threshold: 0.45,
        includeScore: true,
        includeMatches: false,
    },
    resultLimit: 16,
    matchAllWhenSearchEmpty: false,
}));

const { results: searchResults } = useFuse(debouncedSearchQuery, gameDB, fuseOptions);

// Selected games list
const gameList = ref<Game[]>([]);
const selectedGameId = ref<string | null | undefined>(null);

const selectedGame = computed(() => {
    if (!selectedGameId.value) return null;
    return gameList.value.find(g => g.uid === selectedGameId.value) || null;
});

const runningGamesCount = computed(() => {
    return gameList.value.filter(g => g.is_running).length;
});

function closeSearchResults() {
    searchResultsIsOpen.value = false;
}
function openSearchResults() {
    searchResultsIsOpen.value = true;
}

function clearSearch() {
    searchQuery.value = '';
    closeSearchResults();
}

function clearAllGames() {
    gameList.value = [];
    selectedGameId.value = null;
}

function addGameToList(game: Game) {
    if (!gameList.value.some(g => g.id === game.id)) {
        const newGame = {
            uid: randomString(),
            ...game,
        };
        gameList.value.push(newGame);
        selectedGameId.value = newGame.uid;
    } else {
        const existing = gameList.value.find(g => g.id === game.id);
        if (existing) {
            selectedGameId.value = existing.uid;
        }
    }
    closeSearchResults();
}

const forceRerenderKey = ref(0);
function removeGameFromList(game: Game) {
    const gameId = game.uid;
    gameList.value = gameList.value.filter(g => g.uid !== gameId);
    if (selectedGame.value?.uid === gameId) { 
        selectedGameId.value = gameList.value[0]?.uid || null;
        forceRerenderKey.value++; 
    }
}

function selectGame(game: Game) {
    selectedGameId.value = game?.uid;
    searchResultsIsOpen.value = false;
}

function canCreateDummyGame(game: Game | null) {
    if (!game) return false;
    return !game.is_installed;
}

function canPlayGame(game: Game | null) {
    if (!game) return false;
    return (game.is_installed && !game.is_running) ?? false;
}

function isExecutableRunning(executable: GameExecutable) {
    return executable.is_running ?? false;
}

function isGameExecutableInstalled(executable: GameExecutable) {
    return executable.is_installed ?? false;
}

function isGameInstalled(game: Game | null) {
    if (!game) return false;
    return game.is_installed ?? false;
}

async function createDummyGame(game: Game | null, executable: GameExecutable) {
    if (!game) return false;
    const gameUid = game.uid;
    const gameToInstall = gameList.value.find(g => g.uid === gameUid);
    const executableItem = gameToInstall?.executables.find(exe => exe.name === executable.name);
    if (gameToInstall && executableItem) {
        const sec = executable.name.split(/\\|\//);
        const execName = executable.filename || sec[sec.length - 1] || executable.name;
        const execPath = executable.path ?? sec.slice(0, -1).join('/');
        const execSegments = executable.segments ?? sec.length;
        try {
            await invoke('create_fake_game', { 
                path: execPath,
                executable_name: execName,
                path_len: execSegments,
                app_id: Number(gameToInstall.id),
            });
        } catch (e) {
            console.warn('create_fake_game notice:', e);
        }
        gameToInstall.is_installed = true;
        executableItem.is_installed = true;
        return true;
    }
    return false;
}

async function installAndPlay({ game, executable }: { game: Game; executable: GameExecutable }) {
    if (!game) return;
    const gameCreated = await createDummyGame(game, executable);
    if (gameCreated) {
        playGame({ game, executable });
    } else {
        addLog('error', 'Failed to create game dummy');
    }
}

async function playGame({ game, executable }: { game: Game; executable: GameExecutable }) {
    if (!game) return;
    const gameUid = game.uid;
    try {
        const gameToPlay = gameList.value.find(g => g.uid === gameUid);
        const executableItem = gameToPlay?.executables.find(exe => exe.name === executable.name);
        if (gameToPlay && executableItem) {
            const sec = executable.name.split(/\\|\//);
            const execName = executable.filename || sec[sec.length - 1] || executable.name;
            const execPath = executable.path ?? sec.slice(0, -1).join('/');
            const execSegments = executable.segments ?? sec.length;

            addLog('info', `Playing game: ${game.name} (${execName})`);
            currentlyPlaying.value = game.id;

            await invoke('run_background_process', { 
                name: game.name,
                path: execPath,
                executable_name: execName,
                path_len: execSegments,
                app_id: Number(gameToPlay.id),
            });
            gameToPlay.is_running = true;
            executableItem.is_running = true;

            triggerBackgroundNotify(game.name, execName);
        }
    } catch (error) {
        console.error('Failed to launch game:', error);
        addLog('error', 'Failed to launch game: ' + String(error));
    }
}

async function stopPlaying({ game, executable }: { game: Game; executable: GameExecutable }) {
    if (!game) return;
    const gameUid = game.uid;
    currentlyPlaying.value = null;

    const gameToPlay = gameList.value.find(g => g.uid === gameUid);
    const executableItem = gameToPlay?.executables.find(exe => exe.name === executable.name);
    if (gameToPlay && executableItem) {
        const sec = executable.name.split(/\\|\//);
        const execName = executable.filename || sec[sec.length - 1] || executable.name;
        try {
            await invoke('stop_process', { exec_name: execName });
            emit('event_disconnect');
            addLog('info', `Stopped game process: ${game.name}`);
        } catch (error) {
            console.error('Failed to stop game process:', error);
            addLog('error', 'Failed to stop game process: ' + String(error));
        } finally {
            gameToPlay.is_running = false;
            executableItem.is_running = false;
        }
    }
}

async function handleTestRPC(game: Game | null) {
    let state = isConnectedToRPC.value ? 'disconnect' : 'connect';

    if (!game && state === 'connect') {
        showDialog('no_game_selected');
        return;
    }
    if (state === 'disconnect' || isConnecting.value) {
        emit('event_disconnect');
        isConnectedToRPC.value = false;
        if (game) game.is_running = false;
        currentlyPlaying.value = null;
        isConnecting.value = false;
        return;
    }
    showDialog('rpc_message_1');
}

async function continueRPCRisk(game: Game | null) {
    if (!game) return;
    const gameUid = game.uid;
    const gameToTest = gameList.value.find(g => g.uid === gameUid);
    if (gameToTest) {
        isConnecting.value = true;
        invoke('connect_to_discord_rpc_3', {
            activity_json: JSON.stringify({
                app_id: gameToTest.id,
            }),
            action: 'connect',
        })
        .then(() => {
            isConnectedToRPC.value = true;
            gameToTest.is_running = true;
            currentlyPlaying.value = gameToTest.id;
            isConnecting.value = false;
            triggerBackgroundNotify(gameToTest.name, 'Discord Gateway RPC');
        })
        .catch(err => {
            isConnecting.value = false;
            addLog('error', 'RPC connection error: ' + String(err));
        });

        hideDialog();
    }
}

function handleSearchBlur() {
    setTimeout(() => {
        if (!isOnSearchResults.value) {
            searchResultsIsOpen.value = false;
        }
    }, 200);
}

function showDialog(message: DialogKey) {
    isDialogOpen.value = true;
    dialogMessage.value = message;
    dialogKey.value = message;
    if (message) {
        dialogRef.value?.showModal();
    }
}

function hideDialog() {
    dialogRef.value?.close(); 
    dialogMessage.value = '';
    isDialogOpen.value = false;
}

provide<GameActionsProvider>(GameActionsKey, {
    canPlayGame,
    isGameInstalled,
    isExecutableRunning,
    isGameExecutableInstalled,
});
</script>

<template>
    <div class="max-w-7xl mx-auto px-6 py-6 space-y-6">
        <!-- Modern Backdrop Modal Dialog -->
        <dialog
            id="dialog"
            ref="dialogRef"
            class="dialogStyle fixed inset-0 m-auto p-0 bg-transparent rounded-3xl shadow-2xl border border-white/10 backdrop:bg-black/80 backdrop:backdrop-blur-md z-50 overflow-hidden"
        >
            <div class="bg-[#232428]/95 backdrop-blur-2xl p-6 max-w-md w-full border border-white/10 rounded-3xl shadow-2xl space-y-4">
                <div class="flex items-center space-x-3 text-[#f0b232]">
                    <div class="p-2.5 rounded-2xl bg-[#f0b232]/10 border border-[#f0b232]/20">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                    </div>
                    <h3 class="text-base font-bold text-white tracking-wide">
                        {{ dialogKey === 'rpc_message_1' ? 'Experimental Feature Warning' : 'Attention' }}
                    </h3>
                </div>

                <div class="text-xs text-[#dbdee1] leading-relaxed space-y-2">
                    <div v-if="dialogKey === 'rpc_message_1'" class="space-y-2">
                        <p class="text-[#f2f3f5]">Direct RPC sends game App ID packets to Discord socket.</p>
                        <p class="text-[#f0b232] bg-[#f0b232]/10 p-2 rounded-xl border border-[#f0b232]/20">
                            <strong>Caution:</strong> Artificial RPC updates may flag accounts.
                        </p>
                    </div>

                    <div v-if="dialogKey === 'no_game_selected'">
                        <p>No game selected. Please select a game from the left panel.</p>
                    </div>
                </div>

                <div class="flex justify-end space-x-2 pt-3 border-t border-white/[0.08]">
                    <button
                        type="button"
                        class="px-4 py-2 text-xs font-semibold rounded-xl bg-white/[0.06] hover:bg-white/[0.1] text-[#dbdee1] border border-white/[0.08] transition-all"
                        @click="hideDialog()"
                    >
                        {{ dialogKey === 'rpc_message_1' ? 'Cancel' : 'Understood' }}
                    </button>
                    
                    <button 
                        v-if="dialogKey === 'rpc_message_1'"
                        type="button"
                        class="px-4 py-2 text-xs font-semibold rounded-xl bg-[#f0b232] hover:bg-[#dfa225] text-black shadow-md transition-all font-bold"
                        @click="continueRPCRisk(selectedGame)"
                    >
                        Accept Risk & Connect
                    </button>
                </div>
            </div>
        </dialog>

        <!-- Top Status & Hero Search Bar (Liquid Glass) -->
        <div class="relative z-30 p-6 rounded-3xl bg-[#12141c]/55 backdrop-blur-2xl border border-white/[0.13] shadow-[0_8px_32px_0_rgba(0,0,0,0.4),inset_0_1px_1px_0_rgba(255,255,255,0.18)] space-y-4">
            <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                <div>
                    <h1 class="text-xl font-bold text-white tracking-tight flex items-center space-x-2.5">
                        <span>Quest Simulation Hub</span>
                        <span class="text-xs font-semibold text-cyan-300 bg-cyan-500/10 px-3 py-0.5 rounded-full border border-cyan-500/25">
                            {{ gameDB.length.toLocaleString() }} Detectable Games
                        </span>
                    </h1>
                    <p class="text-xs text-[#949ba4] mt-1">
                        Choose a verified title to simulate playtime. Processes launch with active pop-up window.
                    </p>
                </div>

                <!-- Sync Status Pill -->
                <div class="flex items-center space-x-2.5 bg-[#161719]/80 backdrop-blur-md px-3.5 py-1.5 rounded-2xl border border-white/[0.08] text-xs">
                    <div class="flex items-center space-x-2">
                        <span
                            class="w-2 h-2 rounded-full"
                            :class="isAnyFetching ? 'bg-[#f0b232] animate-ping' : 'bg-[#23a55a] shadow-[0_0_8px_rgba(35,165,90,0.8)]'"
                        ></span>
                        <span class="text-[#dbdee1] font-medium">
                            {{ isAnyFetching ? 'Syncing database...' : 'Database Ready' }}
                        </span>
                    </div>
                    <button
                        type="button"
                        class="ml-1 px-3 py-1 text-[11px] font-semibold bg-[#5865F2]/20 hover:bg-[#5865F2]/30 text-white border border-[#5865F2]/30 rounded-xl transition-all flex items-center space-x-1.5 shadow-sm"
                        :disabled="isAnyFetching"
                        @click="fetchGameList()"
                    >
                        <svg
                            class="w-3 h-3 text-[#5865F2]"
                            :class="{ 'animate-spin': isAnyFetching }"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                        >
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                        </svg>
                        <span>Sync</span>
                    </button>
                </div>
            </div>

            <!-- Search Bar with Dropdown -->
            <div class="relative z-40" ref="searchResultContainerRef">
                <div class="relative flex items-center">
                    <div class="absolute left-4 pointer-events-none text-[#949ba4]">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                    </div>

                    <input
                        v-model="searchQuery"
                        type="text"
                        placeholder="Search Discord verified games (e.g. Genshin Impact, Helldivers, Zenless Zone Zero, Roblox)..."
                        class="w-full pl-11 pr-24 py-3 bg-[#161719]/80 hover:bg-[#18191d]/90 focus:bg-[#18191d] border border-white/10 focus:border-[#5865F2] rounded-2xl text-sm text-white placeholder-[#949ba4] focus:outline-none focus:ring-4 focus:ring-[#5865F2]/20 transition-all shadow-inner"
                        @focus="openSearchResults"
                        @blur="handleSearchBlur"
                    />

                    <!-- Clear Search Button -->
                    <button
                        v-if="searchQuery"
                        type="button"
                        class="absolute right-4 text-[#949ba4] hover:text-white p-1 rounded-lg transition-colors"
                        @click="clearSearch"
                    >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>

                <!-- Dropdown Search Results -->
                <Transition
                    enter-active-class="transition duration-150 ease-out"
                    enter-from-class="transform scale-95 opacity-0"
                    enter-to-class="transform scale-100 opacity-100"
                    leave-active-class="transition duration-100 ease-in"
                    leave-from-class="transform scale-100 opacity-100"
                    leave-to-class="transform scale-95 opacity-0"
                >
                    <div
                        v-if="searchResultsIsOpen"
                        @click="isOnSearchResults = true"
                        class="absolute left-0 right-0 mt-2 z-50 bg-[#1b1c1e]/98 backdrop-blur-2xl border border-white/10 rounded-2xl shadow-2xl max-h-80 overflow-y-auto divide-y divide-white/[0.05]"
                    >
                        <div v-if="searchResults.length > 0">
                            <div
                                v-for="game in searchResults"
                                :key="game.item.id"
                                class="p-3.5 hover:bg-white/[0.04] transition-colors flex items-center justify-between group cursor-pointer"
                                @click="addGameToList(game.item)"
                            >
                                <div class="min-w-0 mr-4">
                                    <div class="flex items-center space-x-2">
                                        <span class="font-semibold text-white text-sm group-hover:text-[#5865F2] transition-colors truncate">
                                            {{ game.item.name }}
                                        </span>
                                        <IconVerified class="w-4 h-4 text-[#5865F2] flex-shrink-0" />
                                    </div>
                                    <div class="flex items-center space-x-2 text-xs text-[#949ba4] mt-1">
                                        <span class="font-mono text-[11px] bg-[#141517] px-2 py-0.5 rounded-md border border-white/[0.06]">ID: {{ game.item.id }}</span>
                                        <span>•</span>
                                        <span>{{ game.item.executables?.length || 0 }} binary signatures</span>
                                    </div>
                                </div>

                                <button
                                    type="button"
                                    class="flex-shrink-0 px-3.5 py-1.5 rounded-xl text-xs font-semibold bg-[#5865F2] hover:bg-[#4752c4] text-white shadow-md shadow-[#5865F2]/25 transition-all"
                                    @click.stop="addGameToList(game.item)"
                                >
                                    + Add Target
                                </button>
                            </div>
                        </div>

                        <div v-else class="p-6 text-center text-xs text-[#949ba4]">
                            <p class="font-semibold text-white mb-1">No matching games found</p>
                            <p>Try searching by official title or aliases.</p>
                        </div>
                    </div>
                </Transition>
            </div>
        </div>

        <!-- Main Workspace 2-Column Grid -->
        <div class="relative z-10 grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
            <!-- Left Column: Added Games Queue (5 cols) -->
            <div class="lg:col-span-5 bg-[#12141c]/55 backdrop-blur-2xl border border-white/[0.13] rounded-3xl p-5 shadow-[0_8px_32px_0_rgba(0,0,0,0.4),inset_0_1px_1px_0_rgba(255,255,255,0.15)] space-y-4">
                <div class="flex items-center justify-between pb-3 border-b border-white/[0.07]">
                    <div class="flex items-center space-x-2.5">
                        <h2 class="text-xs font-bold uppercase tracking-wider text-[#dbdee1]">
                            Target Games
                        </h2>
                        <span class="px-2 py-0.5 text-xs font-bold rounded-full bg-[#141517] text-[#5865F2] border border-white/[0.08]">
                            {{ gameList.length }}
                        </span>
                    </div>

                    <button
                        v-if="gameList.length > 0"
                        type="button"
                        class="text-xs text-[#949ba4] hover:text-[#f23f43] transition-colors"
                        @click="clearAllGames"
                    >
                        Clear List
                    </button>
                </div>

                <!-- Empty State -->
                <div
                    v-if="gameList.length === 0"
                    class="py-12 px-4 text-center border-2 border-dashed border-white/[0.08] rounded-2xl space-y-3"
                >
                    <div class="w-12 h-12 mx-auto rounded-2xl bg-[#161719] border border-white/[0.06] flex items-center justify-center text-[#949ba4]">
                        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                    </div>
                    <div>
                        <p class="text-sm font-semibold text-white">No quest targets yet</p>
                        <p class="text-xs text-[#949ba4] mt-1">Use the search bar above to select the game required by your Discord quest.</p>
                    </div>
                </div>

                <!-- Game Cards List -->
                <div v-else class="space-y-2.5 max-h-[58vh] overflow-y-auto pr-1">
                    <div
                        v-for="game in gameList"
                        :key="game.id"
                        class="p-3.5 rounded-2xl border transition-all duration-200 cursor-pointer flex items-center justify-between group"
                        :class="selectedGame?.uid === game.uid
                            ? 'bg-[#222538]/75 backdrop-blur-xl border-l-4 border-l-[#5865F2] border-white/20 shadow-[0_4px_20px_rgba(88,101,242,0.25),inset_0_1px_1px_rgba(255,255,255,0.15)]'
                            : 'bg-[#151722]/55 hover:bg-[#1d2030]/70 backdrop-blur-xl border-white/[0.08] hover:border-white/[0.18] shadow-[inset_0_1px_0_rgba(255,255,255,0.06)]'"
                        @click="selectGame(game)"
                    >
                        <div class="min-w-0 pr-3">
                            <div class="flex items-center space-x-2">
                                <span class="font-semibold text-sm text-white truncate" :class="{ 'text-[#5865F2]': selectedGame?.uid === game.uid }">
                                    {{ game.name }}
                                </span>
                                <IconVerified class="w-4 h-4 text-[#5865F2] flex-shrink-0" />
                            </div>

                            <div class="flex items-center space-x-2 text-xs mt-1">
                                <span
                                    v-if="game.is_running"
                                    class="inline-flex items-center space-x-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/15 text-emerald-400 border border-emerald-500/30"
                                >
                                    <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping"></span>
                                    <span>RUNNING</span>
                                </span>
                                <span v-else class="text-[11px] text-[#949ba4]">
                                    {{ game.executables?.length || 0 }} executables
                                </span>
                            </div>
                        </div>

                        <div class="flex items-center space-x-1.5 flex-shrink-0">
                            <button
                                v-if="!game.is_running"
                                type="button"
                                class="p-2 text-[#949ba4] hover:text-[#f23f43] rounded-xl hover:bg-white/[0.06] transition-colors"
                                title="Remove game"
                                @click.stop="removeGameFromList(game)"
                            >
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Right Column: Game Actions & Quest Trigger (7 cols) -->
            <div
                class="lg:col-span-7 bg-[#12141c]/55 backdrop-blur-2xl border border-white/[0.13] rounded-3xl p-6 shadow-[0_8px_32px_0_rgba(0,0,0,0.4),inset_0_1px_1px_0_rgba(255,255,255,0.15)] space-y-5 sticky top-4"
                :key="forceRerenderKey"
            >
                <!-- Header -->
                <div class="flex items-center justify-between pb-3 border-b border-white/[0.07]">
                    <h2 class="text-xs font-bold uppercase tracking-wider text-[#dbdee1]">
                        Quest Action Center
                    </h2>
                    <span
                        class="inline-flex items-center space-x-1.5 px-3 py-1 rounded-full text-[11px] font-semibold"
                        :class="currentlyPlaying
                            ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 shadow-[0_0_12px_rgba(16,185,129,0.2)]'
                            : 'bg-[#141517] text-[#949ba4] border border-white/[0.08]'"
                    >
                        <span v-if="currentlyPlaying" class="w-2 h-2 rounded-full bg-emerald-400 animate-ping"></span>
                        <span>{{ currentlyPlaying ? 'Game Window Active' : 'Standby' }}</span>
                    </span>
                </div>

                <!-- Unselected Game Placeholder -->
                <div v-if="!selectedGame" class="py-16 text-center text-[#949ba4] space-y-2">
                    <div class="w-12 h-12 mx-auto rounded-2xl bg-white/[0.04] border border-white/[0.08] flex items-center justify-center text-[#949ba4]">
                        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122" />
                        </svg>
                    </div>
                    <p class="text-sm font-semibold text-white">No Game Selected</p>
                    <p class="text-xs">Click any game in your target list on the left to launch dummy processes.</p>
                </div>

                <!-- Active Game Controls -->
                <div v-else class="space-y-5">
                    <!-- Game Info Header -->
                    <div class="p-4 rounded-2xl bg-[#14161f]/45 backdrop-blur-md border border-white/[0.09] space-y-2">
                        <div class="flex items-start justify-between">
                            <div>
                                <h3 class="text-lg font-bold text-white flex items-center space-x-2">
                                    <span>{{ selectedGame.name }}</span>
                                    <IconVerified class="w-4 h-4 text-[#5865F2]" />
                                </h3>
                                <p class="text-xs text-[#949ba4] font-mono mt-0.5">App ID: {{ selectedGame.id }}</p>
                            </div>
                        </div>

                        <!-- Aliases -->
                        <div v-if="selectedGame.aliases && selectedGame.aliases.length > 0" class="flex flex-wrap gap-1.5 pt-1">
                            <span
                                v-for="alias in selectedGame.aliases"
                                :key="alias"
                                class="text-[10px] font-mono px-2 py-0.5 rounded-lg bg-[#232428] text-[#949ba4] border border-white/[0.06]"
                            >
                                {{ alias }}
                            </span>
                        </div>
                    </div>

                    <!-- Recommended Method: Dummy Executable Runner -->
                    <div class="space-y-3">
                        <GameExecutables
                            :game="selectedGame"
                            @play="playGame"
                            @stop="stopPlaying"
                            @install_and_play="installAndPlay"
                        />
                    </div>

                    <!-- Live Quest Status Box with Tray Indicator -->
                    <div
                        class="p-4 rounded-2xl border transition-all duration-300"
                        :class="currentlyPlaying
                            ? 'bg-emerald-500/10 border-emerald-500/30 shadow-[0_0_20px_rgba(16,185,129,0.15)]'
                            : 'bg-[#14161f]/45 backdrop-blur-md border-white/[0.08]'"
                    >
                        <div class="flex items-center space-x-3">
                            <div
                                class="w-3 h-3 rounded-full flex-shrink-0"
                                :class="currentlyPlaying ? 'bg-emerald-400 shadow-[0_0_8px_rgba(16,185,129,0.8)] animate-pulse' : 'bg-[#949ba4]'"
                            ></div>
                            <div>
                                <p class="text-xs font-semibold" :class="currentlyPlaying ? 'text-emerald-300' : 'text-white'">
                                    {{ currentlyPlaying ? 'Discord Detection Active • Game Window Running' : 'No Process Currently Active' }}
                                </p>
                                <p class="text-[11px] text-[#949ba4] mt-0.5">
                                    {{ currentlyPlaying ? 'Pop-up window is visible and active.' : 'Launch executable to simulate playtime.' }}
                                </p>
                            </div>
                        </div>
                    </div>

                    <!-- Alternative: Direct Gateway RPC -->
                    <div class="pt-2 border-t border-white/[0.07] flex items-center justify-between">
                        <div>
                            <p class="text-xs font-semibold text-white">Direct RPC Simulation</p>
                            <p class="text-[10px] text-[#949ba4]">Alternative Discord RPC protocol</p>
                        </div>

                        <button
                            type="button"
                            class="px-4 py-1.5 rounded-xl text-xs font-semibold transition-all shadow-sm"
                            :class="isConnectedToRPC
                                ? 'bg-[#f23f43] hover:bg-[#da373c] text-white'
                                : 'bg-white/[0.06] hover:bg-white/[0.1] text-[#dbdee1] border border-white/[0.08]'"
                            @click="handleTestRPC(selectedGame)"
                        >
                            {{ isConnectedToRPC ? 'Disconnect RPC' : 'Test RPC Socket' }}
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Floating Glassmorphic Background Process Notification Toast -->
        <Transition
            enter-active-class="transition duration-300 ease-out"
            enter-from-class="transform translate-y-4 opacity-0 scale-95"
            enter-to-class="transform translate-y-0 opacity-100 scale-100"
            leave-active-class="transition duration-200 ease-in"
            leave-from-class="transform translate-y-0 opacity-100 scale-100"
            leave-to-class="transform translate-y-4 opacity-0 scale-95"
        >
            <div
                v-if="backgroundNotify.show"
                class="fixed bottom-6 right-6 z-50 max-w-sm w-full bg-[#12141c]/80 backdrop-blur-2xl border border-white/[0.15] rounded-2xl p-4 shadow-[0_8px_32px_0_rgba(0,0,0,0.5),inset_0_1px_1px_0_rgba(255,255,255,0.15)] flex items-start space-x-3 text-left"
            >
                <div class="p-2.5 rounded-xl bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 flex-shrink-0 shadow-[0_0_12px_rgba(16,185,129,0.3)]">
                    <svg class="w-4 h-4 animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                    </svg>
                </div>
                <div class="flex-grow min-w-0">
                    <div class="flex items-center justify-between">
                        <span class="text-xs font-bold text-white tracking-wide flex items-center space-x-1.5">
                            <span>Game Window Active</span>
                            <span class="px-1.5 py-0.2 rounded-full text-[9px] font-bold bg-[#5865F2]/20 text-[#5865F2] border border-[#5865F2]/30">POPUP</span>
                        </span>
                        <button
                            type="button"
                            class="text-[#949ba4] hover:text-white p-0.5 rounded transition-colors"
                            @click="backgroundNotify.show = false"
                        >
                            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    </div>
                    <p class="text-xs font-semibold text-[#5865F2] truncate mt-1">{{ backgroundNotify.gameName }}</p>
                    <p class="text-[11px] text-[#949ba4] mt-0.5 leading-relaxed">
                        Dummy window is visible. Discord is tracking progress.
                    </p>
                </div>
            </div>
        </Transition>
    </div>
</template>

<style scoped>
@reference "../theme/style.css";

.dialogStyle::backdrop {
    background-color: rgba(0, 0, 0, 0.75);
    backdrop-filter: blur(8px);
}
</style>