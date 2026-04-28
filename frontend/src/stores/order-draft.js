import { defineStore } from "pinia";
import { ref } from "vue";

export const useOrderDraftStore = defineStore(
  "order-draft",
  () => {
    const draft = ref(null);
    const hasDraft = ref(false);

    function saveDraft(formData) {
      draft.value = JSON.parse(JSON.stringify(formData));
      hasDraft.value = true;
    }

    function clearDraft() {
      draft.value = null;
      hasDraft.value = false;
    }

    function getDraft() {
      return draft.value;
    }

    return { draft, hasDraft, saveDraft, clearDraft, getDraft };
  },
  {
    persist: true,
  },
);
