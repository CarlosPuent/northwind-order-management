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
            :options="filteredCustomers"
            option-value="id"
            option-label="companyName"
            emit-value
            map-options
            clearable
            dense
            outlined
            label="Customer"
            class="col-12 col-sm-3"
            use-input
            input-debounce="300"
            @filter="filterCustomerOptions"
            @update:model-value="resetAndLoad"
          />
          <q-input
            v-model="filters.region"
            dense
            outlined
            label="Region / Country"
            clearable
            class="col-12 col-sm-2"
            @update:model-value="resetAndLoad"
          />
          <q-select
            v-model="filters.year"
            :options="yearOptions"
            clearable
            dense
            outlined
            label="Year"
            class="col-6 col-sm-1"
            style="min-width: 100px"
            @update:model-value="resetAndLoad"
          />
          <q-space />

          <!-- Export buttons -->
          <q-btn-group flat>
            <q-btn
              flat
              icon="table_chart"
              color="positive"
              label="Excel"
              size="sm"
              @click="exportToExcel"
              :disable="orders.length === 0"
            >
              <q-tooltip>Export current view to Excel</q-tooltip>
            </q-btn>
            <q-btn
              flat
              icon="picture_as_pdf"
              color="red-7"
              label="PDF"
              size="sm"
              @click="exportToPdf"
              :disable="orders.length === 0"
            >
              <q-tooltip>Export current view to PDF</q-tooltip>
            </q-btn>
          </q-btn-group>

          <q-btn
            flat
            icon="refresh"
            color="primary"
            @click="loadOrders"
          >
            <q-tooltip>Refresh</q-tooltip>
          </q-btn>
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
      <template v-slot:body-cell-status="props">
        <q-td :props="props">
          <q-badge
            :color="props.row.isShipped ? 'green' : 'orange'"
            :label="props.row.isShipped ? 'Shipped' : 'Pending'"
          />
        </q-td>
      </template>

      <template v-slot:body-cell-freight="props">
        <q-td :props="props">
          ${{ props.row.freight.toFixed(2) }}
        </q-td>
      </template>

      <template v-slot:body-cell-total="props">
        <q-td :props="props">
          <span class="text-weight-bold">${{ props.row.total.toFixed(2) }}</span>
        </q-td>
      </template>

      <template v-slot:body-cell-orderDate="props">
        <q-td :props="props">
          {{ formatDate(props.row.orderDate) }}
        </q-td>
      </template>

      <template v-slot:body-cell-actions="props">
        <q-td :props="props">
          <q-btn flat round dense icon="visibility" color="primary"
            :to="{ name: 'order-detail', params: { id: props.row.id } }">
            <q-tooltip>View</q-tooltip>
          </q-btn>
          <q-btn flat round dense icon="edit" color="grey-7"
            :to="{ name: 'order-edit', params: { id: props.row.id } }"
            :disable="props.row.isShipped">
            <q-tooltip>Edit</q-tooltip>
          </q-btn>
          <q-btn flat round dense icon="picture_as_pdf" color="red-7"
            @click="downloadInvoice(props.row.id)">
            <q-tooltip>Invoice PDF</q-tooltip>
          </q-btn>
          <q-btn flat round dense icon="delete" color="negative"
            :disable="props.row.isShipped"
            @click="confirmDelete(props.row)">
            <q-tooltip>Delete</q-tooltip>
          </q-btn>
        </q-td>
      </template>

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
import * as XLSX from 'xlsx'

const $q = useQuasar()

// ---- State ----
const orders = ref([])
const allOrders = ref([])
const loading = ref(false)
const customerOptions = ref([])
const filteredCustomers = ref([])
const yearOptions = ref([])

const filters = reactive({
  customerId: null,
  region: null,
  year: null,
})

const pagination = ref({
  page: 1,
  rowsPerPage: 15,
  rowsNumber: 0,
  sortBy: 'orderDate',
  descending: true,
})

// ---- Columns ----
const columns = [
  { name: 'id', label: 'Order #', field: 'id', align: 'left', sortable: true },
  { name: 'customerId', label: 'Customer', field: 'customerId', align: 'left', sortable: true },
  { name: 'shipCity', label: 'Ship City', field: 'shipCity', align: 'left' },
  { name: 'shipCountry', label: 'Country', field: 'shipCountry', align: 'left' },
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

    let items = data.items

    // Client-side year filter (backend doesn't have year param on orders endpoint)
    if (filters.year) {
      items = items.filter(o => {
        const d = new Date(o.orderDate)
        return d.getFullYear() === filters.year
      })
    }

    orders.value = items
    allOrders.value = items
    pagination.value.rowsNumber = data.totalCount
  } catch (err) {
    // Handled by interceptor
  } finally {
    loading.value = false
  }
}

async function loadCustomers () {
  try {
    const { data } = await api.get('/customers')
    customerOptions.value = data
    filteredCustomers.value = data.slice(0, 20)
  } catch (err) {}
}

async function loadYears () {
  try {
    const { data } = await api.get('/analytics/available-years')
    yearOptions.value = data
  } catch (err) {}
}

// ---- Filters ----
function resetAndLoad () {
  pagination.value.page = 1
  loadOrders()
}

function filterCustomerOptions (val, update) {
  update(() => {
    if (!val) {
      filteredCustomers.value = customerOptions.value.slice(0, 20)
    } else {
      const needle = val.toLowerCase()
      filteredCustomers.value = customerOptions.value.filter(
        c => c.companyName.toLowerCase().includes(needle)
      )
    }
  })
}

function onRequest (props) {
  pagination.value.page = props.pagination.page
  pagination.value.rowsPerPage = props.pagination.rowsPerPage
  pagination.value.sortBy = props.pagination.sortBy
  pagination.value.descending = props.pagination.descending
  loadOrders()
}

// ---- Export to Excel ----
function exportToExcel () {
  const exportData = orders.value.map(o => ({
    'Order #': o.id,
    'Customer': o.customerId,
    'Ship City': o.shipCity,
    'Country': o.shipCountry,
    'Order Date': formatDate(o.orderDate),
    'Status': o.isShipped ? 'Shipped' : 'Pending',
    'Freight': o.freight,
    'Total': o.total,
  }))

  const ws = XLSX.utils.json_to_sheet(exportData)

  // Set column widths for readability
  ws['!cols'] = [
    { wch: 10 }, { wch: 15 }, { wch: 18 }, { wch: 15 },
    { wch: 14 }, { wch: 10 }, { wch: 12 }, { wch: 12 },
  ]

  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Orders')
  XLSX.writeFile(wb, `Northwind_Orders_${new Date().toISOString().split('T')[0]}.xlsx`)

  $q.notify({
    type: 'positive',
    message: `Exported ${exportData.length} orders to Excel`,
    position: 'top-right',
  })
}

// ---- Export to PDF (print view) ----
function exportToPdf () {
  const rows = orders.value.map(o =>
    `<tr>
      <td>${o.id}</td>
      <td>${o.customerId}</td>
      <td>${o.shipCity}</td>
      <td>${o.shipCountry}</td>
      <td>${formatDate(o.orderDate)}</td>
      <td>${o.isShipped ? 'Shipped' : 'Pending'}</td>
      <td style="text-align:right">$${o.freight.toFixed(2)}</td>
      <td style="text-align:right"><strong>$${o.total.toFixed(2)}</strong></td>
    </tr>`
  ).join('')

  const html = `
    <html>
    <head>
      <title>Northwind Orders Report</title>
      <style>
        body { font-family: 'Segoe UI', Arial, sans-serif; padding: 30px; color: #1A1A2E; }
        h1 { color: #1F3A5F; font-size: 24px; margin-bottom: 4px; }
        .subtitle { color: #64748B; font-size: 13px; margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; font-size: 13px; }
        th { background: #1F3A5F; color: white; padding: 10px 12px; text-align: left; font-size: 12px; text-transform: uppercase; letter-spacing: 0.05em; }
        td { padding: 8px 12px; border-bottom: 1px solid #E2E8F0; }
        tr:nth-child(even) { background: #F8FAFC; }
        .footer { margin-top: 20px; font-size: 11px; color: #94A3B8; }
      </style>
    </head>
    <body>
      <h1>NORTHWIND TRADERS</h1>
      <div class="subtitle">Orders Report · Generated ${new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}</div>
      <table>
        <thead>
          <tr>
            <th>Order #</th><th>Customer</th><th>Ship City</th><th>Country</th>
            <th>Date</th><th>Status</th><th style="text-align:right">Freight</th><th style="text-align:right">Total</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
      <div class="footer">Northwind Traders · Order Management System · ${orders.value.length} orders</div>
    </body>
    </html>
  `

  const printWindow = window.open('', '_blank')
  printWindow.document.write(html)
  printWindow.document.close()
  printWindow.print()
}

// ---- Actions ----
function downloadInvoice (orderId) {
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
      $q.notify({ type: 'positive', message: `Order #${order.id} deleted`, position: 'top-right' })
      loadOrders()
    } catch (err) {}
  })
}

// ---- Helpers ----
function formatDate (dateStr) {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
  })
}

// ---- Init ----
onMounted(() => {
  loadOrders()
  loadCustomers()
  loadYears()
})
</script>