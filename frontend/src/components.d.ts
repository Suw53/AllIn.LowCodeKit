/**
 * 全局组件类型声明
 * 消除 Volar 对 main.ts 中全局注册组件的类型警告
 */
import type * as ElementPlusIconsVue from '@element-plus/icons-vue'

type IconComponents = {
  [K in keyof typeof ElementPlusIconsVue]: (typeof ElementPlusIconsVue)[K]
}

declare module 'vue' {
  export interface GlobalComponents extends IconComponents {}
}

export {}
