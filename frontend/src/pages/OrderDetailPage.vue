<template>
  <q-page padding>
    <!-- Loading -->
    <div v-if="loading" class="row justify-center q-pa-xl">
      <q-spinner-dots size="50px" color="primary" />
    </div>

    <!-- Content -->
    <template v-else-if="order">
      <!-- Header -->
      <div class="row items-center q-mb-lg">
        <q-btn flat round icon="arrow_back" color="primary" :to="{ name: 'orders' }" />
        <div class="q-ml-md col">
          <div class="text-h4 text-primary text-weight-bold">
            Order #{{ order.id }}
          </div>
          <div class="text-caption text-grey-7">
            Placed on {{ formatDate(order.orderDate) }}
          </div>
        </div>
        <div class="col-auto q-gutter-sm">
          <q-badge
            :color="order.isShipped ? 'green' : 'orange'"
            :label="order.isShipped ? 'Shipped' : 'Pending'"
            class="text-body2 q-pa-sm"
          />
          <q-btn
            v-if="!order.isShipped"
            color="grey-7"
            icon="edit"
            label="Edit"
            outline
            :to="{ name: 'order-edit', params: { id: order.id } }"
          />
          <q-btn
            color="red-7"
            icon="picture_as_pdf"
            label="Invoice PDF"
            unelevated
            @click="downloadInvoice"
          />
        </div>
      </div>

      <!-- Info Cards -->
      <div class="row q-gutter-md q-mb-lg">
        <q-card flat bordered class="col">
          <q-card-section>
            <div class="text-overline text-primary">SHIP TO</div>
            <div class="q-mt-sm text-body2">{{ order.shipName }}</div>
            <div class="text-body2">{{ order.shipStreet }}</div>
            <div class="text-body2">
              {{ order.shipCity }}{{ order.shipRegion ? ', ' + order.shipRegion : '' }}
              {{ order.shipPostalCode || '' }}
            </div>
            <div class="text-body2">{{ order.shipCountry }}</div>
          </q-card-section>
        </q-card>

        <q-card flat bordered class="col">
          <q-card-section>
            <div class="text-overline text-primary">ORDER INFO</div>
            <div class="q-mt-sm">
              <div class="row q-mb-xs">
                <div class="col-5 text-grey-7">Customer:</div>
                <div class="col text-weight-medium">{{ order.customerId }}</div>
              </div>
              <div class="row q-mb-xs">
                <div class="col-5 text-grey-7">Employee:</div>
                <div class="col">{{ order.employeeId }}</div>
              </div>
              <div class="row q-mb-xs">
                <div class="col-5 text-grey-7">Shipper:</div>
                <div class="col">{{ order.shipperId || 'Not assigned' }}</div>
              </div>
              <div class="row q-mb-xs" v-if="order.shippedDate">
                <div class="col-5 text-grey-7">Shipped:</div>
                <div class="col">{{ formatDate(order.shippedDate) }}</div>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Line Items Table -->
      <q-card flat bordered class="q-mb-lg">
        <q-card-section class="q-pb-none">
          <div class="text-overline text-primary">LINE ITEMS</div>
        </q-card-section>
        <q-card-section>
          <q-table
            :rows="order.lines"
            :columns="lineColumns"
            row-key="productId"
            flat
            hide-bottom
            :pagination="{ rowsPerPage: 0 }"
          >
            <template v-slot:body-cell-unitPrice="props">
              <q-td :props="props">${{ props.row.unitPrice.toFixed(2) }}</q-td>
            </template>
            <template v-slot:body-cell-discount="props">
              <q-td :props="props">
                {{ props.row.discount > 0 ? (props.row.discount * 100).toFixed(0) + '%' : '—' }}
              </q-td>
            </template>
            <template v-slot:body-cell-lineTotal="props">
              <q-td :props="props">
                <span class="text-weight-bold">${{ props.row.lineTotal.toFixed(2) }}</span>
              </q-td>
            </template>
          </q-table>
        </q-card-section>
      </q-card>

      <!-- Totals -->
      <div class="row justify-end q-mb-lg">
        <q-card flat bordered style="min-width: 300px;">
          <q-card-section>
            <div class="row q-mb-sm">
              <div class="col text-right text-grey-7">Subtotal:</div>
              <div class="col-4 text-right">${{ order.subTotal.toFixed(2) }}</div>
            </div>
            <div class="row q-mb-sm">
              <div class="col text-right text-grey-7">Freight:</div>
              <div class="col-4 text-right">${{ order.freight.toFixed(2) }}</div>
            </div>
            <q-separator class="q-my-sm" />
            <div class="row">
              <div class="col text-right text-weight-bold text-primary text-h6">TOTAL:</div>
              <div class="col-4 text-right text-weight-bold text-primary text-h6">
                ${{ order.total.toFixed(2) }}
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>
    </template>

    <!-- Not found -->
    <template v-else>
      <div class="text-center q-pa-xl text-grey-7">
        <q-icon name="error_outline" size="4em" class="q-mb-md" />
        <div class="text-h6">Order not found</div>
        <q-btn flat color="primary" label="Back to Orders" :to="{ name: 'orders' }" class="q-mt-md" />
      </div>
    </template>
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
  { name: 'productId', label: 'Product #', field: 'productId', align: 'left' },
  { name: 'unitPrice', label: 'Unit Price', field: 'unitPrice', align: 'right' },
  { name: 'quantity', label: 'Qty', field: 'quantity', align: 'center' },
  { name: 'discount', label: 'Discount', field: 'discount', align: 'center' },
  { name: 'lineTotal', label: 'Line Total', field: 'lineTotal', align: 'right' },
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