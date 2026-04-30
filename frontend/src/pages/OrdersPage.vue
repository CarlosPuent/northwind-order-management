<template>
  <q-page class="bg-grey-1" padding>
    <div class="page-container q-mx-auto" style="max-width: 1300px;">
      <!-- Header -->
      <div class="row items-center q-mb-xl q-mt-md">
        <div class="col">
          <div class="text-h4 text-weight-bolder text-grey-9 tracking-tight">Orders</div>
          <div class="text-body2 text-grey-6 q-mt-xs">Manage customer orders and shipments</div>
        </div>
        <div class="col-auto">
          <q-btn
            color="primary"
            icon="add"
            label="New Order"
            unelevated
            no-caps
            class="new-order-btn shadow-2"
            :to="{ name: 'order-create' }"
          />
        </div>
      </div>

      <!-- Filters -->
      <q-card flat class="mac-card q-mb-lg">
        <q-card-section class="q-pa-lg">
          <div class="row q-col-gutter-md items-center">
            
            <!-- Customer: ancho controlado por class (col-md-3 col-lg-4) para que se vean los demás filtros/botones -->
            <q-select
              v-model="filters.customerId"
              :options="filteredCustomers"
              option-value="id"
              option-label="companyName"
              emit-value
              map-options
              clearable
              outlined
              label="Customer"
              class="col-12 col-md-2 col-lg-3 elegant-input"
              use-input
              input-debounce="200"
              hide-bottom-space
              @filter="filterCustomerOptions"
              @update:model-value="resetAndLoad"
            >
              <template v-slot:selected-item="scope">
                <span class="text-grey-9 text-weight-medium">{{ scope.opt.companyName }}</span>
              </template>
              <template v-slot:option="scope">
                <q-item v-bind="scope.itemProps" dense class="q-py-sm">
                  <q-item-section>
                    <q-item-label class="text-weight-medium text-grey-9">{{ scope.opt.companyName }}</q-item-label>
                    <q-item-label caption class="text-grey-6">{{ scope.opt.city }}, {{ scope.opt.country }}</q-item-label>
                  </q-item-section>
                </q-item>
              </template>
            </q-select>

            <!-- Search: ancho controlado por class (col-md-3 col-lg-4) para que se vean los demás filtros/botones -->
            <q-input
              v-model="filters.region"
              outlined
              label="Search region, country, or city"
              clearable
              class="col-12 col-md-2 col-lg-3 elegant-input"
              debounce="400"
              hide-bottom-space
              @update:model-value="resetAndLoad"
            >
              <template v-slot:prepend>
                <q-icon name="search" size="xs" color="grey-6" />
              </template>
            </q-input>

            <!-- Status: Ancho fijo exacto -->
            <q-select
              v-model="filters.status"
              :options="statusOptions"
              emit-value
              map-options
              clearable
              outlined
              label="Status"
              class="col-6 col-md-auto elegant-input"
              style="width: 140px;"
              hide-bottom-space
              @update:model-value="resetAndLoad"
            >
              <template v-slot:selected-item="scope">
                <span class="text-grey-9 text-weight-medium">{{ scope.opt.label }}</span>
              </template>
            </q-select>

            <!-- Year: Ancho fijo exacto -->
            <q-select
              v-model="filters.year"
              :options="yearOptions"
              clearable
              outlined
              label="Year"
              class="col-6 col-md-auto elegant-input"
              style="width: 140px;"
              hide-bottom-space
              @update:model-value="resetAndLoad"
            />

            <!-- Export buttons: Toma el espacio exacto de los botones -->
            <div class="col-12 col-md-auto row q-gutter-sm justify-end items-center">
              <q-btn-group flat class="elegant-btn-group">
                <q-btn flat no-caps text-color="green-8" class="export-btn" @click="exportToExcel" :disable="orders.length === 0">
                  <q-icon name="table_chart" size="18px" class="q-mr-sm" />
                  <span class="text-weight-bold">Excel</span>
                  <q-tooltip class="bg-grey-9">Export to Excel</q-tooltip>
                </q-btn>
                <q-separator vertical color="grey-3" />
                <q-btn flat no-caps text-color="red-7" class="export-btn" @click="exportToPdf" :disable="orders.length === 0">
                  <q-icon name="picture_as_pdf" size="18px" class="q-mr-sm" />
                  <span class="text-weight-bold">PDF</span>
                  <q-tooltip class="bg-grey-9">Export to PDF</q-tooltip>
                </q-btn>
              </q-btn-group>

              <q-btn flat round color="grey-7" icon="refresh" class="refresh-btn q-ml-sm" @click="loadOrders">
                <q-tooltip class="bg-grey-9">Refresh Data</q-tooltip>
              </q-btn>
            </div>
          </div>
        </q-card-section>
      </q-card>

      <!-- Orders Table -->
      <q-table
        :rows="orders"
        :columns="columns"
        row-key="id"
        :loading="loading"
        v-model:pagination="pagination"
        @request="onRequest"
        :rows-per-page-options="[10, 15, 25, 50, 0]"
        flat
        class="mac-card elegant-table"
        binary-state-sort
      >
        <template v-slot:body-cell-id="props">
          <q-td :props="props">
            <span class="text-weight-bold text-grey-9">{{ props.row.id }}</span>
          </q-td>
        </template>

        <template v-slot:body-cell-customerId="props">
          <q-td :props="props">
            <span class="text-weight-medium text-grey-9">{{ props.row.customerId }}</span>
          </q-td>
        </template>

        <template v-slot:body-cell-status="props">
          <q-td :props="props">
            <div class="status-pill" :class="props.row.isShipped ? 'status-shipped' : 'status-pending'">
              {{ props.row.isShipped ? 'Shipped' : 'Pending' }}
            </div>
          </q-td>
        </template>

        <template v-slot:body-cell-freight="props">
          <q-td :props="props" class="text-grey-7 text-weight-medium">
            ${{ props.row.freight.toFixed(2) }}
          </q-td>
        </template>

        <template v-slot:body-cell-total="props">
          <q-td :props="props">
            <span class="text-weight-bold text-grey-9">${{ props.row.total.toFixed(2) }}</span>
          </q-td>
        </template>

        <template v-slot:body-cell-orderDate="props">
          <q-td :props="props" class="text-grey-7">
            {{ formatDate(props.row.orderDate) }}
          </q-td>
        </template>

        <template v-slot:body-cell-actions="props">
          <q-td :props="props">
            <div class="row justify-center no-wrap q-gutter-x-xs">
              <q-btn flat round dense icon="visibility" class="action-btn btn-view"
                :to="{ name: 'order-detail', params: { id: props.row.id } }">
                <q-tooltip class="bg-grey-9">View Details</q-tooltip>
              </q-btn>
              
              <q-btn flat round dense icon="edit" class="action-btn btn-edit"
                :to="{ name: 'order-edit', params: { id: props.row.id } }"
                :disable="props.row.isShipped">
                <q-tooltip class="bg-grey-9">Edit Order</q-tooltip>
              </q-btn>
              
              <q-btn flat round dense icon="receipt_long" class="action-btn btn-invoice"
                @click="downloadInvoice(props.row.id)">
                <q-tooltip class="bg-grey-9">Download Invoice</q-tooltip>
              </q-btn>
              
              <q-btn flat round dense icon="delete_outline" class="action-btn btn-delete"
                :disable="props.row.isShipped"
                @click="confirmDelete(props.row)">
                <q-tooltip class="bg-grey-9">Delete</q-tooltip>
              </q-btn>
            </div>
          </q-td>
        </template>

        <!-- Professional Pagination -->
        <template v-slot:bottom="scope">
          <div class="row full-width items-center q-py-sm">
            <div class="col-auto row items-center text-caption text-grey-7 q-gutter-sm">
              <span class="text-weight-medium">Rows per page:</span>
              <q-select
                v-model="scope.pagination.rowsPerPage"
                :options="[10, 15, 25, 50, { label: 'All', value: 0 }]"
                dense
                borderless
                options-dense
                class="rows-selector text-weight-bold"
                emit-value
                map-options
                @update:model-value="scope.pagination.page = 1; loadOrders()"
              />
            </div>
            
            <q-space />
            
            <div class="col-auto text-caption text-grey-8 q-mr-md">
              <span class="text-weight-bold">{{ ((scope.pagination.page - 1) * scope.pagination.rowsPerPage) + 1 }}</span>
              -
              <span class="text-weight-bold">{{ scope.pagination.rowsPerPage === 0 ? scope.pagination.rowsNumber : Math.min(scope.pagination.page * scope.pagination.rowsPerPage, scope.pagination.rowsNumber) }}</span>
              of
              <span class="text-weight-bold">{{ scope.pagination.rowsNumber }}</span>
            </div>

            <div class="col-auto row q-gutter-x-xs">
              <q-btn flat round dense icon="keyboard_double_arrow_left" color="grey-8" 
                :disable="scope.isFirstPage" @click="scope.firstPage" class="pagination-btn" />
              <q-btn flat round dense icon="chevron_left" color="grey-8" 
                :disable="scope.isFirstPage" @click="scope.prevPage" class="pagination-btn" />
              <q-btn flat round dense icon="chevron_right" color="grey-8" 
                :disable="scope.isLastPage" @click="scope.nextPage" class="pagination-btn" />
              <q-btn flat round dense icon="keyboard_double_arrow_right" color="grey-8" 
                :disable="scope.isLastPage" @click="scope.lastPage" class="pagination-btn" />
            </div>
          </div>
        </template>

        <template v-slot:no-data>
          <div class="full-width column flex-center text-grey-5 q-pa-xl bg-grey-1 rounded-borders q-my-md empty-state">
            <q-icon name="search_off" size="3em" class="q-mb-md text-grey-4" />
            <div class="text-subtitle1 text-weight-bold text-grey-8">No orders found</div>
            <div class="text-caption">Try adjusting your filters or search terms.</div>
          </div>
        </template>

        <template v-slot:loading>
          <q-inner-loading showing color="primary" />
        </template>
      </q-table>
    </div>
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
const loading = ref(false)
const customerOptions = ref([])
const filteredCustomers = ref([])
const yearOptions = ref([])

const statusOptions = [
  { label: 'Shipped', value: true },
  { label: 'Pending', value: false }
]

const filters = reactive({
  customerId: null,
  region: null,
  status: null,
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
  { name: 'actions', label: 'Actions', field: 'actions', align: 'center', sortable: false, style: 'width: 170px' },
]

// ---- Load data ----
async function loadOrders () {
  loading.value = true
  try {
    const params = {
      page: pagination.value.page,
      pageSize: pagination.value.rowsPerPage === 0 ? 10000 : pagination.value.rowsPerPage,
    }

    if (filters.customerId) params.customerId = filters.customerId
    if (filters.region) params.region = filters.region
    if (filters.status !== null) params.isShipped = filters.status

    const { data } = await api.get('/orders', { params })

    let items = data.items
    let total = data.totalCount

    // Client-side year filter
    if (filters.year) {
      items = items.filter(o => new Date(o.orderDate).getFullYear() === filters.year)
    }

    orders.value = items
    pagination.value.rowsNumber = total
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
      filteredCustomers.value = customerOptions.value.filter(c =>
        c.companyName.toLowerCase().includes(needle) ||
        c.city?.toLowerCase().includes(needle) ||
        c.country?.toLowerCase().includes(needle)
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

  const totalFreight = orders.value.reduce((sum, o) => sum + o.freight, 0)
  const grandTotal = orders.value.reduce((sum, o) => sum + o.total, 0)
  exportData.push({
    'Order #': '', 'Customer': '', 'Ship City': '', 'Country': '',
    'Order Date': '', 'Status': 'TOTALS',
    'Freight': totalFreight, 'Total': grandTotal,
  })

  const ws = XLSX.utils.json_to_sheet(exportData)
  ws['!cols'] = [
    { wch: 10 }, { wch: 18 }, { wch: 18 }, { wch: 15 },
    { wch: 14 }, { wch: 10 }, { wch: 12 }, { wch: 14 },
  ]

  const range = XLSX.utils.decode_range(ws['!ref'])
  for (let r = 1; r <= range.e.r; r++) {
    const fCell = ws[XLSX.utils.encode_cell({ r, c: 6 })]
    const tCell = ws[XLSX.utils.encode_cell({ r, c: 7 })]
    if (fCell) fCell.z = '$#,##0.00'
    if (tCell) tCell.z = '$#,##0.00'
  }

  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Orders')

  const meta = XLSX.utils.aoa_to_sheet([
    ['Northwind Traders — Orders Report'],
    ['Generated', new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })],
    ['Total Orders', orders.value.length],
    ['Filters Applied', [
      filters.customerId ? `Customer: ${filters.customerId}` : null,
      filters.region ? `Region: ${filters.region}` : null,
      filters.status !== null ? `Status: ${filters.status ? 'Shipped' : 'Pending'}` : null,
      filters.year ? `Year: ${filters.year}` : null,
    ].filter(Boolean).join(', ') || 'None'],
  ])
  meta['!cols'] = [{ wch: 20 }, { wch: 40 }]
  XLSX.utils.book_append_sheet(wb, meta, 'Report Info')

  XLSX.writeFile(wb, `Northwind_Orders_${new Date().toISOString().split('T')[0]}.xlsx`)
  $q.notify({ type: 'positive', message: `Exported ${orders.value.length} orders to Excel`, position: 'top-right' })
}

// ---- Export to PDF ----
function exportToPdf () {
  const totalFreight = orders.value.reduce((sum, o) => sum + o.freight, 0)
  const grandTotal = orders.value.reduce((sum, o) => sum + o.total, 0)

  const rows = orders.value.map(o =>
    `<tr>
      <td>${o.id}</td>
      <td>${o.customerId}</td>
      <td>${o.shipCity}</td>
      <td>${o.shipCountry}</td>
      <td>${formatDate(o.orderDate)}</td>
      <td><span class="badge ${o.isShipped ? 'shipped' : 'pending'}">${o.isShipped ? 'Shipped' : 'Pending'}</span></td>
      <td class="right">$${o.freight.toFixed(2)}</td>
      <td class="right bold">$${o.total.toFixed(2)}</td>
    </tr>`
  ).join('')

  const filtersText = [
    filters.customerId ? `Customer: ${filters.customerId}` : null,
    filters.region ? `Region: ${filters.region}` : null,
    filters.status !== null ? `Status: ${filters.status ? 'Shipped' : 'Pending'}` : null,
    filters.year ? `Year: ${filters.year}` : null,
  ].filter(Boolean).join(' · ') || 'No filters applied'

  const html = `<html><head><title>Northwind Orders Report</title>
    <style>
      *{margin:0;padding:0;box-sizing:border-box}
      body{font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;padding:40px;color:#1c1c1e}
      .header{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:8px}
      .brand{font-size:24px;font-weight:700;color:#1c1c1e;letter-spacing:-.02em}
      .brand-sub{font-size:12px;color:#8e8e93;}
      .report-title{text-align:right}
      .report-title h2{font-size:14px;color:#1c1c1e;text-transform:uppercase;letter-spacing:.05em;font-weight:600}
      .report-title .date{font-size:12px;color:#8e8e93;margin-top:4px}
      .accent-bar{height:1px;background:#e5e5ea;margin-bottom:24px;margin-top:24px}
      .meta{font-size:12px;color:#8e8e93;margin-bottom:24px}
      .meta span{font-weight:500;color:#1c1c1e}
      table{width:100%;border-collapse:collapse;font-size:12px;margin-bottom:32px}
      th{color:#8e8e93;padding:12px 8px;text-align:left;font-size:11px;text-transform:uppercase;letter-spacing:.05em;font-weight:600;border-bottom:1px solid #e5e5ea}
      td{padding:10px 8px;border-bottom:1px solid #f2f2f7;color:#3a3a3c}
      tr:last-child td:not(.totals-row td){border-bottom:none}
      .right{text-align:right}.bold{font-weight:600;color:#1c1c1e}
      .badge{display:inline-block;padding:4px 10px;border-radius:20px;font-size:10px;font-weight:600;letter-spacing:.02em}
      .badge.shipped{background:#e8f5e9;color:#2e7d32}
      .badge.pending{background:#fff3e0;color:#e65100}
      .totals-row{font-weight:600}
      .totals-row td{border-top:1px solid #1c1c1e;padding-top:16px;color:#1c1c1e}
      .footer{margin-top:32px;padding-top:16px;border-top:1px solid #e5e5ea;font-size:11px;color:#8e8e93;display:flex;justify-content:space-between}
      @media print{body{padding:20px}@page{margin:1cm}}
    </style></head><body>
    <div class="header"><div><div class="brand">Northwind Traders</div><div class="brand-sub">Order Management System</div></div>
    <div class="report-title"><h2>Orders Report</h2><div class="date">${new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}</div></div></div>
    <div class="accent-bar"></div>
    <div class="meta">${filtersText} &nbsp;·&nbsp; <span>${orders.value.length} orders found</span></div>
    <table><thead><tr><th>Order #</th><th>Customer</th><th>Ship City</th><th>Country</th><th>Date</th><th>Status</th><th class="right">Freight</th><th class="right">Total</th></tr></thead>
    <tbody>${rows}
    <tr class="totals-row"><td colspan="6" class="right">TOTALS</td><td class="right">$${totalFreight.toFixed(2)}</td><td class="right">$${grandTotal.toFixed(2)}</td></tr>
    </tbody></table>
    <div class="footer"><span>Confidential — Internal Use Only</span><span>Generated from Northwind Platform</span></div>
    </body></html>`

  const w = window.open('', '_blank')
  w.document.write(html)
  w.document.close()
  w.print()
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

<style scoped>
/* Typography & Layout */
.tracking-tight {
  letter-spacing: -0.02em;
}
.page-container {
  padding-bottom: 60px;
}

/* Apple-style Cards */
.mac-card {
  border-radius: 12px;
  border: 1px solid rgba(0, 0, 0, 0.06);
  background-color: #ffffff;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.02) !important;
}

/* Elegant Inputs */
:deep(.elegant-input .q-field__control) {
  border-radius: 8px;
  transition: all 0.2s ease;
}
:deep(.elegant-input.q-field--outlined .q-field__control:before) {
  border-color: #e5e5ea !important;
}
:deep(.elegant-input.q-field--outlined.q-field--focused .q-field__control:after) {
  border-width: 1px;
}

/* Button Groups */
.elegant-btn-group {
  border: 1px solid #e5e5ea;
  border-radius: 8px;
  overflow: hidden;
  background: white;
}

/* Buttons */
.new-order-btn {
  border-radius: 8px;
  font-weight: 600;
  letter-spacing: 0.02em;
}
.export-btn {
  transition: background-color 0.2s ease;
}
.refresh-btn {
  border: 1px solid #e5e5ea;
  background: white;
  transition: all 0.2s ease;
}
.refresh-btn:hover {
  background: #f2f2f7;
  color: var(--q-primary) !important;
  border-color: #d1d1d6;
}

.pagination-btn {
  border-radius: 6px;
  transition: background-color 0.2s ease;
}
.pagination-btn:hover {
  background-color: #f2f2f7;
  color: var(--q-primary) !important;
}

/* Custom Table Styling */
.elegant-table {
  border-radius: 12px;
  overflow: hidden;
}
:deep(.elegant-table th) {
  font-size: 11px;
  font-weight: 700;
  color: #64748B;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  border-bottom: 1px solid #e5e5ea !important;
  background-color: #fcfcfd;
  padding: 16px 24px;
}
:deep(.elegant-table tbody td) {
  font-size: 13px;
  border-bottom: 1px solid #f2f2f7 !important;
  padding: 12px 24px;
  color: #1c1c1e;
}
:deep(.elegant-table tbody tr:hover td) {
  background-color: #f8fafc !important;
}
:deep(.elegant-table .q-table__bottom) {
  border-top: 1px solid #e5e5ea;
  background-color: #ffffff;
}

/* Status Pills (Badges) */
.status-pill {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}
.status-shipped {
  background-color: #ecfdf5;
  color: #059669;
}
.status-pending {
  background-color: #fffbeb;
  color: #d97706;
}

/* Action Buttons */
.action-btn {
  transition: all 0.2s ease;
}
.btn-view {
  color: var(--q-primary); 
}
.btn-view:hover {
  background-color: #eff6ff !important;
}

.btn-edit {
  color: #64748B; 
}
.btn-edit:not(:disabled):hover {
  color: #f59e0b !important;
  background-color: #fffbeb !important;
}

.btn-invoice {
  color: #6366f1; 
}
.btn-invoice:hover {
  background-color: #eef2ff !important;
}

.btn-delete {
  color: #ef4444; 
}
.btn-delete:not(:disabled):hover {
  background-color: #fef2f2 !important;
}

/* Rows per page selector */
:deep(.rows-selector .q-field__control) {
  color: var(--q-primary);
}
:deep(.rows-selector .q-field__native) {
  color: var(--q-primary);
}

/* Empty State */
.empty-state {
  border: 1px dashed #d1d1d6;
  background-color: #fafafa;
}
</style>