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
      redirect: (to) => {
        const tab = Array.isArray(to.query.tab) ? to.query.tab[0] : to.query.tab
        if (tab === 'login' || tab === 'playwright') return '/global-config/automation'
        return '/global-config/system'
      }
    },
    {
      path: '/global-config/automation',
      name: 'AutomationConfig',
      component: () => import('@/views/AutomationConfigView.vue')
    },
    {
      path: '/global-config/system',
      name: 'SystemConfig',
      component: () => import('@/views/SystemConfigView.vue')
    },
    {
      path: '/automation/:menuId',
      name: 'Automation',
      component: () => import('@/views/AutomationView.vue')
    }
  ]
})

export default router
