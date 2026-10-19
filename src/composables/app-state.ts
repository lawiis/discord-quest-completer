import { createGlobalState } from '@vueuse/core'
import { computed, ComputedRef, Ref, ShallowRef, shallowRef } from 'vue'

export const Pages = {
    HOME: 'home',
    PLAYGROUND: 'playground',
} as const
export type Pages = typeof Pages[keyof typeof Pages]
export interface AppLogObject {
    type: 'info' | 'error' | 'warning' | 'debug';
    message: string;
    timestamp: Date;
}
export interface UseGlobalStateReturn {
    page: ShallowRef<Pages>,
    count: ShallowRef<number>,
    doubleCount: ComputedRef<number>,
    setPage: (newPage: Pages) => void,
    increment: () => void,
    logs: ShallowRef<AppLogObject[]>,
    addLog: {
        (type: 'info' | 'error' | 'warning' | 'debug', newLog: string): void;
        (newLog: string): void;
    };
    clearLogs: () => void,
    snowflakesEnabled: ShallowRef<boolean>,
    setSnowflakesEnabled: (val: boolean) => void,
    toggleSnowflakes: () => void,
    snowflakesSpeed: ShallowRef<'slow' | 'normal' | 'fast'>,
    setSnowflakesSpeed: (speed: 'slow' | 'normal' | 'fast') => void,
    snowflakesDensity: ShallowRef<'gentle' | 'normal' | 'blizzard'>,
    setSnowflakesDensity: (density: 'gentle' | 'normal' | 'blizzard') => void,
    bgBlur: ShallowRef<number>,
    setBgBlur: (val: number) => void,
    bgOpacity: ShallowRef<number>,
    setBgOpacity: (val: number) => void,
    bgBrightness: ShallowRef<number>,
    setBgBrightness: (val: number) => void,
    bgImage: ShallowRef<string>,
    setBgImage: (val: string) => void,
    resetBgImage: () => void,
    isSettingsOpen: ShallowRef<boolean>,
    openSettings: () => void,
    closeSettings: () => void,
}
export const useGlobalState = createGlobalState(
  () => {
    // state
    const page = shallowRef<Pages>(Pages.HOME)

    const logs = shallowRef<AppLogObject[]>([])

    const count = shallowRef(0)

    const snowflakesEnabled = shallowRef<boolean>(
      localStorage.getItem('dqc_snow') !== 'false'
    )

    const snowflakesSpeed = shallowRef<'slow' | 'normal' | 'fast'>(
      (localStorage.getItem('dqc_speed') as 'slow' | 'normal' | 'fast') || 'normal'
    )

    const snowflakesDensity = shallowRef<'gentle' | 'normal' | 'blizzard'>(
      (localStorage.getItem('dqc_dens') as 'gentle' | 'normal' | 'blizzard') || 'normal'
    )

    const bgBlur = shallowRef<number>(
      Number(localStorage.getItem('dqc_blur') ?? 8)
    )

    const bgOpacity = shallowRef<number>(
      Number(localStorage.getItem('dqc_opac') ?? 65)
    )

    const bgBrightness = shallowRef<number>(
      Number(localStorage.getItem('dqc_bright') ?? 75)
    )

    const bgImage = shallowRef<string>(
      localStorage.getItem('dqc_bg') || ''
    )

    const isSettingsOpen = shallowRef(false)

    // getters
    const doubleCount = computed(() => count.value * 2)

    // actions
    function increment() {
      count.value++
    }

    function setPage(newPage: Pages) {
      page.value = newPage
    }

    function setSnowflakesEnabled(val: boolean) {
      snowflakesEnabled.value = val
      localStorage.setItem('dqc_snow', String(val))
    }

    function toggleSnowflakes() {
      setSnowflakesEnabled(!snowflakesEnabled.value)
    }

    function setSnowflakesSpeed(speed: 'slow' | 'normal' | 'fast') {
      snowflakesSpeed.value = speed
      localStorage.setItem('dqc_speed', speed)
    }

    function setSnowflakesDensity(density: 'gentle' | 'normal' | 'blizzard') {
      snowflakesDensity.value = density
      localStorage.setItem('dqc_dens', density)
    }

    function setBgBlur(val: number) {
      bgBlur.value = val
      localStorage.setItem('dqc_blur', String(val))
    }

    function setBgOpacity(val: number) {
      bgOpacity.value = val
      localStorage.setItem('dqc_opac', String(val))
    }

    function setBgBrightness(val: number) {
      bgBrightness.value = val
      localStorage.setItem('dqc_bright', String(val))
    }

    function setBgImage(val: string) {
      bgImage.value = val
      localStorage.setItem('dqc_bg', val)
    }

    function resetBgImage() {
      bgImage.value = ''
      localStorage.removeItem('dqc_bg')
    }

    function openSettings() {
      isSettingsOpen.value = true
    }

    function closeSettings() {
      isSettingsOpen.value = false
    }

    function addLog(type: string | 'info' | 'error' | 'warning' | 'debug' , newLog?: string) {
      if (!newLog) {
        newLog = type;
        type = 'info';
      }
      const formattedLog = `${newLog}`;
      logs.value.push({ type: type as 'info' | 'error' | 'warning' | 'debug', message: formattedLog, timestamp: new Date() });
    }

    function clearLogs() {
      logs.value = []
    }

    return {
        page,
        count, 
        doubleCount,
        setPage, 
        increment,
        logs,
        addLog,
        clearLogs,
        snowflakesEnabled,
        setSnowflakesEnabled,
        toggleSnowflakes,
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
    } as UseGlobalStateReturn
  }
)

