<template>
  <q-page padding>
    <!-- Header -->
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h4 text-primary text-weight-bold">Orders</div>
        <div class="text-caption text-grey-7">Manage customer orders and shipments</div>
      </div>
      <div class="col-auto">
        <q-btn
          color="primary"
          icon="add_circle"
          label="New Order"
          unelevated
          :to="{ name: 'order-create' }"
        />
      </div>
    </div>

    <!-- Filters -->
    <q-card flat bordered class="q-mb-md">
      <q-card-section class="q-pa-sm">
        <div class="row q-gutter-sm items-center">
          <q-select
            v-model="filters.customerId"
            :options="customerOptions"
            option-value="id"
            option-label="companyName"
            emit-value
            map-options
            clearable
            dense
            outlined
            label="Filter by Customer"
            class="col-3"
            @update:model-value="loadOrders"
          />
          <q-input
            v-model="filters.region"
            dense
            outlined
            label="Filter by Region"
            clearable
            class="col-2"
            @update:model-value="loadOrders"
          />
          <q-space />
          <q-btn
            flat
            icon="refresh"
            label="Refresh"
            color="primary"
            @click="loadOrders"
          />
        </div>
      </q-card-section>
    </q-card>

    <!-- Orders Table -->
    <q-table
      :rows="orders"
      :columns="columns"
      row-key="id"
      :loading="loading"
      :pagination="pagination"
      @request="onRequest"
      flat
      bordered
      binary-state-sort
    >
      <!-- Status chip -->
      <template v-slot:body-cell-status="props">
        <q-td :props="props">
          <q-badge
            :color="props.row.isShipped ? 'green' : 'orange'"
            :label="props.row.isShipped ? 'Shipped' : 'Pending'"
          />
        </q-td>
      </template>

      <!-- Freight formatted -->
      <template v-slot:body-cell-freight="props">
        <q-td :props="props">
          ${{ props.row.freight.toFixed(2) }}
        </q-td>
      </template>

      <!-- Total formatted -->
      <template v-slot:body-cell-total="props">
        <q-td :props="props">
          <span class="text-weight-bold">${{ props.row.total.toFixed(2) }}</span>
        </q-td>
      </template>

      <!-- Order date formatted -->
      <template v-slot:body-cell-orderDate="props">
        <q-td :props="props">
          {{ formatDate(props.row.orderDate) }}
        </q-td>
      </template>

      <!-- Actions column -->
      <template v-slot:body-cell-actions="props">
        <q-td :props="props">
          <q-btn
            flat
            round
            dense
            icon="visibility"
            color="primary"
            :to="{ name: 'order-detail', params: { id: props.row.id } }"
          >
            <q-tooltip>View details</q-tooltip>
          </q-btn>
          <q-btn
            flat
            round
            dense
            icon="edit"
            color="grey-7"
            :to="{ name: 'order-edit', params: { id: props.row.id } }"
            :disable="props.row.isShipped"
          >
            <q-tooltip>{{ props.row.isShipped ? 'Cannot edit shipped order' : 'Edit' }}</q-tooltip>
          </q-btn>
          <q-btn
            flat
            round
            dense
            icon="picture_as_pdf"
            color="red-7"
            @click="downloadInvoice(props.row.id)"
          >
            <q-tooltip>Download Invoice PDF</q-tooltip>
          </q-btn>
          <q-btn
            flat
            round
            dense
            icon="delete"
            color="negative"
            :disable="props.row.isShipped"
            @click="confirmDelete(props.row)"
          >
            <q-tooltip>{{ props.row.isShipped ? 'Cannot delete shipped order' : 'Delete' }}</q-tooltip>
          </q-btn>
        </q-td>
      </template>

      <!-- No data -->
      <template v-slot:no-data>
        <div class="full-width row flex-center text-grey-7 q-gutter-sm q-pa-lg">
          <q-icon name="inbox" size="2em" />
          <span>No orders found</span>
        </div>
      </template>
    </q-table>
  </q-page>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useQuasar } from 'quasar'
import { api } from 'src/boot/axios'

const $q = useQuasar()

// ---- State ----

const orders = ref([])
const loading = ref(false)
const customerOptions = ref([])

const filters = reactive({
  customerId: null,
  region: null,
})

const pagination = ref({
  page: 1,
  rowsPerPage: 10,
  rowsNumber: 0,
  sortBy: 'orderDate',
  descending: true,
})

// ---- Columns ----

const columns = [
  { name: 'id', label: 'Order #', field: 'id', align: 'left', sortable: true },
  { name: 'customerId', label: 'Customer', field: 'customerId', align: 'left', sortable: true },
  { name: 'shipCity', label: 'Ship City', field: 'shipCity', align: 'left' },
  { name: 'shipCountry', label: 'Ship Country', field: 'shipCountry', align: 'left' },
  { name: 'orderDate', label: 'Order Date', field: 'orderDate', align: 'left', sortable: true },
  { name: 'status', label: 'Status', field: 'isShipped', align: 'center' },
  { name: 'freight', label: 'Freight', field: 'freight', align: 'right' },
  { name: 'total', label: 'Total', field: 'total', align: 'right' },
  { name: 'actions', label: 'Actions', field: 'actions', align: 'center' },
]

// ---- Load data ----

async function loadOrders () {
  loading.value = true
  try {
    const { data } = await api.get('/orders', {
      params: {
        page: pagination.value.page,
        pageSize: pagination.value.rowsPerPage,
        customerId: filters.customerId || undefined,
        region: filters.region || undefined,
      }
    })
    orders.value = data.items
    pagination.value.rowsNumber = data.totalCount
  } catch (err) {
    // Error is handled by the Axios interceptor (Quasar Notify).
  } finally {
    loading.value = false
  }
}

async function loadCustomers () {
  try {
    const { data } = await api.get('/customers')
    customerOptions.value = data
  } catch (err) {
    // Silent — filter dropdown just won't populate.
  }
}

// ---- Table pagination handler ----

function onRequest (props) {
  pagination.value.page = props.pagination.page
  pagination.value.rowsPerPage = props.pagination.rowsPerPage
  pagination.value.sortBy = props.pagination.sortBy
  pagination.value.descending = props.pagination.descending
  loadOrders()
}

// ---- Actions ----

function downloadInvoice (orderId) {
  // Open the invoice PDF in a new tab — the browser handles the download.
  window.open(`http://localhost:5281/api/invoices/${orderId}`, '_blank')
}

function confirmDelete (order) {
  $q.dialog({
    title: 'Delete Order',
    message: `Are you sure you want to delete Order #${order.id}? This action cannot be undone.`,
    cancel: true,
    persistent: true,
    color: 'negative',
  }).onOk(async () => {
    try {
      await api.delete(`/orders/${order.id}`)
      $q.notify({
        type: 'positive',
        message: `Order #${order.id} deleted successfully`,
        position: 'top-right',
      })
      loadOrders()
    } catch (err) {
      // Handled by interceptor.
    }
  })
}

// ---- Helpers ----

function formatDate (dateStr) {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

// ---- Init ----

onMounted(() => {
  loadOrders()
  loadCustomers()
})
</script>