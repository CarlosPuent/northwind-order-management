import { createPinia } from "pinia";
import piniaPluginPersistedstate from "pinia-plugin-persistedstate";
import { store } from "quasar/wrappers";

export default store((/* { ssrContext } */) => {
  const pinia = createPinia();
  pinia.use(piniaPluginPersistedstate);
  return pinia;
});
