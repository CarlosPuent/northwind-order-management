import { boot } from "quasar/wrappers";
import axios from "axios";
import { Notify } from "quasar";

const api = axios.create({
  baseURL: "http://localhost:5281/api",
  timeout: 15000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Response interceptor — converts backend ProblemDetails errors
// into Quasar notifications automatically. No component needs to
// handle HTTP errors manually.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const message =
      error.response?.data?.detail ||
      error.response?.data?.error ||
      error.response?.data?.title ||
      error.message ||
      "An unexpected error occurred";

    Notify.create({
      type: "negative",
      message,
      position: "top-right",
      timeout: 4000,
      actions: [{ icon: "close", color: "white" }],
    });

    return Promise.reject(error);
  },
);

export default boot(({ app }) => {
  app.config.globalProperties.$api = api;
});

export { api };
