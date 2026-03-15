import { createRouter, createWebHashHistory } from 'vue-router'

const router = createRouter({
  // 使用hash模式，兼容Tauri本地文件协议
  history: createWebHashHistory(),
  routes: [
    {
      path: '/',
      redirect: '/home'
    },
    {
      path: '/home',
      name: 'Home',
      component: () => import('@/views/HomeView.vue')
    },
    {
      path: '/module/:menuId',
      name: 'Module',
      component: () => import('@/views/ModuleView.vue')
    },
    {
      path: '/form-designer/:menuId',
      name: 'FormDesigner',
      component: () => import('@/views/FormDesignerView.vue')
    },
    {
      path: '/global-config',
      name: 'GlobalConfig',
      component: () => import('@/views/GlobalConfigView.vue')
    },
    {
      path: '/automation/:menuId',
      name: 'Automation',
      component: () => import('@/views/AutomationView.vue')
    }
  ]
})

export default router
