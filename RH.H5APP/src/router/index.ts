import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/evaluation/:id',
      name: 'Evaluation',
      component: () => import('../views/evaluation/index.vue'),
      props: true
    },
    {
      path: '/',
      redirect: '/evaluation/0'
    }
  ]
})

export default router

