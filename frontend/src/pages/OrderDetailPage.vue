<template>
  <q-page class="bg-grey-1" padding>
    <div class="page-container q-mx-auto" style="max-width: 1200px;">
      <!-- Loading -->
      <div v-if="loading" class="row justify-center q-pa-xl">
        <q-spinner-dots size="50px" color="primary" />
      </div>

      <!-- Content -->
      <template v-else-if="order">
        <!-- Header -->
        <div class="row items-center justify-between q-mb-xl q-mt-md">
          <div class="row items-center col">
            <q-btn flat round icon="arrow_back" color="grey-8" :to="{ name: 'orders' }" class="q-mr-sm" />
            <div>
              <div class="text-h4 text-weight-bolder text-grey-9 tracking-tight row items-center">
                Order #{{ order.id }}
                <div class="q-ml-md status-pill" :class="order.isShipped ? 'status-shipped' : 'status-pending'">
                  {{ order.isShipped ? 'Shipped' : 'Pending' }}
                </div>
              </div>
              <div class="text-body2 text-grey-6 q-mt-xs">
                Placed on {{ formatDate(order.orderDate) }}
              </div>
            </div>
          </div>
          
          <div class="col-auto row q-gutter-sm items-center">
            <q-btn
              v-if="!order.isShipped"
              outline
              no-caps
              color="grey-8"
              class="rounded-button hover-primary bg-white"
              :to="{ name: 'order-edit', params: { id: order.id } }"
            >
              <q-icon name="edit" size="xs" class="q-mr-sm" />
              <span>Edit Order</span>
            </q-btn>
            <q-btn
              unelevated
              no-caps
              color="red-7"
              class="rounded-button shadow-2"
              @click="downloadInvoice"
            >
              <q-icon name="picture_as_pdf" size="xs" class="q-mr-sm" />
              <span>Invoice PDF</span>
            </q-btn>
          </div>
        </div>

        <!-- Info Cards -->
        <div class="row q-col-gutter-lg q-mb-lg">
          <!-- Ship To Card -->
          <div class="col-12 col-md-6">
            <q-card flat class="mac-card full-height">
              <q-card-section class="q-pa-lg">
                <div class="section-title">Shipping Details</div>
                <div class="q-mt-md">
                  <div class="text-h6 text-grey-9 text-weight-bold q-mb-sm">{{ order.shipName }}</div>
                  <div class="text-body1 text-grey-8">{{ order.shipStreet }}</div>
                  <div class="text-body1 text-grey-8">
                    {{ order.shipCity }}{{ order.shipRegion ? ', ' + order.shipRegion : '' }} {{ order.shipPostalCode || '' }}
                  </div>
                  <div class="text-body1 text-grey-8">{{ order.shipCountry }}</div>
                </div>
              </q-card-section>
            </q-card>
          </div>

          <!-- Order Info Card -->
          <div class="col-12 col-md-6">
            <q-card flat class="mac-card full-height">
              <q-card-section class="q-pa-lg">
                <div class="section-title">Order Information</div>
                <div class="q-mt-md">
                  <div class="info-row q-mb-md">
                    <div class="info-label">Customer</div>
                    <div class="info-value text-weight-medium">{{ order.customerId }}</div>
                  </div>
                  <q-separator color="grey-2" class="q-my-sm" />
                  <div class="info-row q-mb-md">
                    <div class="info-label">Employee</div>
                    <div class="info-value">{{ order.employeeId }}</div>
                  </div>
                  <q-separator color="grey-2" class="q-my-sm" />
                  <div class="info-row q-mb-md">
                    <div class="info-label">Shipper</div>
                    <div class="info-value">{{ order.shipperId || 'Not assigned' }}</div>
                  </div>
                  <q-separator color="grey-2" class="q-my-sm" v-if="order.shippedDate" />
                  <div class="info-row" v-if="order.shippedDate">
                    <div class="info-label">Date Shipped</div>
                    <div class="info-value">{{ formatDate(order.shippedDate) }}</div>
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>

        <!-- Line Items Table -->
        <q-card flat class="mac-card q-mb-lg">
          <q-card-section class="q-pa-lg q-pb-md">
            <div class="section-title q-mb-none">Line Items</div>
          </q-card-section>
          
          <q-table
            :rows="order.lines"
            :columns="lineColumns"
            row-key="productId"
            flat
            class="elegant-table no-border-radius"
            hide-bottom
            :pagination="{ rowsPerPage: 0 }"
          >
            <template v-slot:body-cell-productId="props">
              <q-td :props="props">
                <span class="text-weight-medium text-grey-9">Product #{{ props.row.productId }}</span>
              </q-td>
            </template>
            <template v-slot:body-cell-unitPrice="props">
              <q-td :props="props" class="text-grey-7">
                ${{ props.row.unitPrice.toFixed(2) }}
              </q-td>
            </template>
            <template v-slot:body-cell-quantity="props">
              <q-td :props="props">
                <q-badge color="grey-2" text-color="grey-9" class="q-px-sm q-py-xs text-weight-bold">
                  {{ props.row.quantity }}
                </q-badge>
              </q-td>
            </template>
            <template v-slot:body-cell-discount="props">
              <q-td :props="props" :class="props.row.discount > 0 ? 'text-orange-8 text-weight-medium' : 'text-grey-5'">
                {{ props.row.discount > 0 ? '-' + (props.row.discount * 100).toFixed(0) + '%' : '—' }}
              </q-td>
            </template>
            <template v-slot:body-cell-lineTotal="props">
              <q-td :props="props">
                <span class="text-weight-bold text-grey-9">${{ props.row.lineTotal.toFixed(2) }}</span>
              </q-td>
            </template>
          </q-table>
        </q-card>

        <!-- Totals -->
        <div class="row justify-end q-mb-xl">
          <div class="col-12 col-sm-6 col-md-4">
            <q-card flat class="mac-card bg-white">
              <q-card-section class="q-pa-lg">
                <div class="row justify-between items-center q-mb-md">
                  <div class="text-body1 text-grey-6">Subtotal</div>
                  <div class="text-body1 text-weight-medium text-grey-9">${{ order.subTotal.toFixed(2) }}</div>
                </div>
                <div class="row justify-between items-center q-mb-lg">
                  <div class="text-body1 text-grey-6">Freight</div>
                  <div class="text-body1 text-weight-medium text-grey-9">${{ order.freight.toFixed(2) }}</div>
                </div>
                
                <q-separator color="grey-3" class="q-mb-md" />
                
                <div class="row justify-between items-end">
                  <div>
                    <div class="text-subtitle2 text-grey-8 text-weight-bold text-uppercase tracking-wide">Grand Total</div>
                    <div class="text-caption text-grey-5 q-mt-xs">USD</div>
                  </div>
                  <div class="text-h4 text-primary text-weight-bolder tracking-tight">
                    ${{ order.total.toFixed(2) }}
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </template>

      <!-- Not found -->
      <template v-else>
        <div class="full-width column flex-center text-grey-5 q-pa-xl bg-white mac-card q-my-xl">
          <q-icon name="error_outline" size="4em" class="q-mb-md text-grey-4" />
          <div class="text-h6 text-weight-bold text-grey-8">Order Not Found</div>
          <div class="text-body2 q-mb-lg">The order you are looking for does not exist or has been removed.</div>
          <q-btn unelevated no-caps color="primary" class="rounded-button shadow-2 q-px-lg" label="Back to Orders" :to="{ name: 'orders' }" />
        </div>
      </template>
    </div>
  </q-page>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { api } from 'src/boot/axios'

const route = useRoute()
const order = ref(null)
const loading = ref(true)

const lineColumns = [
  { name: 'productId', label: 'Product', field: 'productId', align: 'left' },
  { name: 'unitPrice', label: 'Unit Price', field: 'unitPrice', align: 'right' },
  { name: 'quantity', label: 'Qty', field: 'quantity', align: 'center' },
  { name: 'discount', label: 'Discount', field: 'discount', align: 'center' },
  { name: 'lineTotal', label: 'Total', field: 'lineTotal', align: 'right' },
]

async function loadOrder () {
  loading.value = true
  try {
    const { data } = await api.get(`/orders/${route.params.id}`)
    order.value = data
  } catch (err) {
    order.value = null
  } finally {
    loading.value = false
  }
}

function downloadInvoice () {
  window.open(`http://localhost:5281/api/invoices/${order.value.id}`, '_blank')
}

function formatDate (dateStr) {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

onMounted(loadOrder)
</script>

<style scoped>
/* Typography & Layout */
.tracking-tight {
  letter-spacing: -0.02em;
}
.tracking-wide {
  letter-spacing: 0.05em;
}
.page-container {
  padding-bottom: 40px;
}

/* Apple-style Cards */
.mac-card {
  border-radius: 12px;
  border: 1px solid rgba(0, 0, 0, 0.06);
  background-color: #ffffff;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.02) !important;
}

.section-title {
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  color: #8e8e93;
  font-weight: 700;
  text-transform: uppercase;
}

/* Info Rows */
.info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.info-label {
  color: #64748B;
  font-size: 0.875rem;
}
.info-value {
  color: #1c1c1e;
  font-size: 0.875rem;
  text-align: right;
}

/* Buttons */
.rounded-button {
  border-radius: 8px;
  font-weight: 600;
  letter-spacing: 0.01em;
}
.hover-primary:hover {
  border-color: var(--q-primary) !important;
  color: var(--q-primary) !important;
  background-color: #f8fbff !important;
}

/* Custom Table Styling */
.elegant-table {
  border-radius: 12px;
}
.no-border-radius {
  border-radius: 0 0 12px 12px !important;
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
  padding: 14px 24px;
  color: #1c1c1e;
}
:deep(.elegant-table tbody tr:hover td) {
  background-color: #f8fafc !important;
}
:deep(.elegant-table tbody tr:last-child td) {
  border-bottom: none !important;
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
</style>