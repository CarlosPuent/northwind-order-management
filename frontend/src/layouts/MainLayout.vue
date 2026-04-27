<template>
  <q-layout view="lHh Lpr lFf">
    <!-- Header -->
    <q-header elevated class="bg-primary">
      <q-toolbar>
        <q-btn flat dense round icon="menu" @click="toggleLeftDrawer" />
        <q-toolbar-title class="text-weight-bold">
          NORTHWIND TRADERS
        </q-toolbar-title>
        <q-badge color="accent" class="q-mr-sm text-caption">
          Order Management
        </q-badge>
      </q-toolbar>
    </q-header>

    <!-- Sidebar -->
    <q-drawer v-model="leftDrawerOpen" show-if-above bordered class="bg-grey-1">
      <q-list>
        <q-item-label header class="text-grey-8 text-weight-bold q-pb-md">
          Navigation
        </q-item-label>

        <q-item
          v-for="link in navLinks"
          :key="link.name"
          :to="link.to"
          clickable
          v-ripple
          active-class="text-primary bg-blue-1"
        >
          <q-item-section avatar>
            <q-icon :name="link.icon" />
          </q-item-section>
          <q-item-section>
            <q-item-label>{{ link.label }}</q-item-label>
          </q-item-section>
        </q-item>
      </q-list>

      <!-- Footer of sidebar -->
      <template v-slot:mini>
        <q-scroll-area class="fit mini-slot cursor-pointer">
          <q-item
            v-for="link in navLinks"
            :key="link.name"
            :to="link.to"
            clickable
            v-ripple
          >
            <q-item-section avatar>
              <q-icon :name="link.icon" />
            </q-item-section>
          </q-item>
        </q-scroll-area>
      </template>
    </q-drawer>

    <!-- Main content -->
    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup>
import { ref } from 'vue'

const leftDrawerOpen = ref(false)

const navLinks = [
  { name: 'dashboard', label: 'Dashboard', icon: 'dashboard', to: '/' },
  { name: 'orders', label: 'Orders', icon: 'receipt_long', to: '/orders' },
  { name: 'new-order', label: 'New Order', icon: 'add_circle', to: '/orders/new' },
]

function toggleLeftDrawer () {
  leftDrawerOpen.value = !leftDrawerOpen.value
}
</script>