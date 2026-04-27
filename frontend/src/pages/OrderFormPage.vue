<template>
  <q-page padding>
    <div class="text-h4 text-primary q-mb-md">
      {{ isEdit ? 'Edit Order' : 'New Order' }}
    </div>
    <p class="text-grey-7">Order form with Google Maps will appear here.</p>
  </q-page>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const isEdit = computed(() => !!route.params.id)
</script>