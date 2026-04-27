const routes = [
  {
    path: "/",
    component: () => import("layouts/MainLayout.vue"),
    children: [
      {
        path: "",
        name: "dashboard",
        component: () => import("pages/DashboardPage.vue"),
        meta: { title: "Dashboard", icon: "dashboard" },
      },
      {
        path: "orders",
        name: "orders",
        component: () => import("pages/OrdersPage.vue"),
        meta: { title: "Orders", icon: "receipt_long" },
      },
      {
        path: "orders/new",
        name: "order-create",
        component: () => import("pages/OrderFormPage.vue"),
        meta: { title: "New Order", icon: "add_circle" },
      },
      {
        path: "orders/:id/edit",
        name: "order-edit",
        component: () => import("pages/OrderFormPage.vue"),
        meta: { title: "Edit Order", icon: "edit" },
      },
      {
        path: "orders/:id",
        name: "order-detail",
        component: () => import("pages/OrderDetailPage.vue"),
        meta: { title: "Order Detail", icon: "visibility" },
      },
    ],
  },
  {
    path: "/:catchAll(.*)*",
    component: () => import("pages/ErrorNotFound.vue"),
  },
];

export default routes;
