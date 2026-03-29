// Monaco Editor worker 环境必须最先加载
import '@/utils/monaco-env'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import VxeUI from 'vxe-pc-ui'
import 'vxe-pc-ui/lib/style.css'
import VXETable from 'vxe-table'
import 'vxe-table/lib/style.css'
import router from './router'
import App from './App.vue'
import './style.css'
import { useThemeStore } from '@/stores/themeStore'
import { useAppConfigStore } from '@/stores/appConfigStore'

const app = createApp(App)

// 注册 Element Plus 所有图标
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

app.use(createPinia())
app.use(ElementPlus)
app.use(VxeUI)
app.use(VXETable)
app.use(router)

app.mount('#app')

// 启动时加载持久化配置（mount 后 Pinia 已就绪）
useThemeStore().loadTheme()
useAppConfigStore().loadAppConfig()
