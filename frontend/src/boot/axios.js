import { boot } from "quasar/wrappers";
import axios from "axios";
import { Notify } from "quasar";

// In Docker, Nginx proxies /api/ to the backend container.
// In local dev, we call the backend directly.
const baseURL =
  process.env.VUE_APP_API_URL ||
  (window.location.port === "80" || window.location.port === ""
    ? "/api"
    : "http://localhost:5281/api");

const api = axios.create({
  baseURL,
  timeout: 15000,
  headers: {
    "Content-Type": "application/json",
  },
});

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
