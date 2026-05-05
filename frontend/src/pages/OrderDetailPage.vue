<template>
  <q-page padding>
    <div v-if="loading" class="row justify-center q-pa-xl">
      <q-spinner-dots size="50px" color="primary" />
    </div>

    <template v-else-if="order">
      <!-- Header -->
      <div class="row items-center q-mb-lg">
        <q-btn flat round icon="arrow_back" color="primary" :to="{ name: 'orders' }" />
        <div class="q-ml-md col">
          <div class="text-h4 text-primary text-weight-bold">Order #{{ order.id }}</div>
          <div class="text-caption text-grey-7">Placed on {{ formatDate(order.orderDate) }}</div>
        </div>
        <div class="col-auto q-gutter-sm row items-center">
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
            v-if="!order.isShipped"
            color="positive"
            icon="local_shipping"
            label="Mark as Shipped"
            unelevated
            @click="showShipDialog = true"
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

      <!-- Shipped info banner -->
      <q-banner v-if="order.isShipped" class="bg-green-1 q-mb-md" rounded>
        <template v-slot:avatar>
          <q-icon name="check_circle" color="positive" />
        </template>
        Shipped on {{ formatDate(order.shippedDate) }}
        <span v-if="order.shipperId"> via Shipper #{{ order.shipperId }}</span>
      </q-banner>

      <!-- Info Cards -->
      <div class="row q-gutter-md q-mb-lg">
        <q-card flat bordered class="col">
          <q-card-section>
            <div class="text-overline text-primary">SHIP TO</div>
            <div class="q-mt-sm text-body2 text-weight-medium">{{ order.shipName }}</div>
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
                <div class="col">{{ employeeName }}</div>
              </div>
              <div class="row q-mb-xs">
                <div class="col-5 text-grey-7">Shipper:</div>
                <div class="col">{{ shipperName }}</div>
              </div>
              <div class="row q-mb-xs" v-if="order.shippedDate">
                <div class="col-5 text-grey-7">Shipped:</div>
                <div class="col">{{ formatDate(order.shippedDate) }}</div>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Line Items -->
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
            <template v-slot:body-cell-productName="props">
              <q-td :props="props">
                <div class="text-weight-medium">{{ props.row.productName }}</div>
                <div class="text-caption text-grey-7">ID: {{ props.row.productId }}</div>
              </q-td>
            </template>
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

    <template v-else>
      <div class="text-center q-pa-xl text-grey-7">
        <q-icon name="error_outline" size="4em" class="q-mb-md" />
        <div class="text-h6">Order not found</div>
        <q-btn flat color="primary" label="Back to Orders" :to="{ name: 'orders' }" class="q-mt-md" />
      </div>
    </template>

    <!-- Mark as Shipped Dialog -->
    <q-dialog v-model="showShipDialog" persistent>
      <q-card style="min-width: 400px;">
        <q-card-section>
          <div class="text-h6">Mark Order as Shipped</div>
          <div class="text-caption text-grey-7 q-mt-xs">
            This action sets the ShippedDate and cannot be undone.
          </div>
        </q-card-section>
        <q-card-section class="q-gutter-md">
          <q-select
            v-model="shipForm.shipperId"
            :options="shippers"
            option-value="id"
            option-label="companyName"
            emit-value
            map-options
            outlined
            label="Shipper *"
            :rules="[val => !!val || 'Shipper is required']"
          >
            <template v-slot:option="scope">
              <q-item v-bind="scope.itemProps" dense>
                <q-item-section avatar>
                  <q-icon name="local_shipping" color="grey-7" />
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ scope.opt.companyName }}</q-item-label>
                  <q-item-label caption>{{ scope.opt.phone || 'No phone' }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
          </q-select>
          <q-input
            v-model="shipForm.shippedDate"
            outlined
            type="date"
            label="Shipped Date"
            hint="Leave empty to use today's date"
          />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey-7" v-close-popup />
          <q-btn
            unelevated
            label="Confirm Shipment"
            color="positive"
            icon="local_shipping"
            :loading="shipping"
            :disable="!shipForm.shipperId"
            @click="confirmShip"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useQuasar } from 'quasar'
import { api } from 'src/boot/axios'

const route = useRoute()
const $q = useQuasar()
const order = ref(null)
const loading = ref(true)
const shippers = ref([])
const employees = ref([])
const showShipDialog = ref(false)
const shipping = ref(false)

const shipForm = reactive({
  shipperId: null,
  shippedDate: null,
})

const lineColumns = [
  { name: 'productName', label: 'Product', field: 'productName', align: 'left' },
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

async function loadShippers () {
  try {
    const { data } = await api.get('/shippers')
    shippers.value = data
  } catch (err) {}
}

async function loadEmployees () {
  try {
    const { data } = await api.get('/employees')
    employees.value = data
  } catch (err) {}
}

// Helper functions to get names
const employeeName = computed(() => {
  if (!order.value) return '—'
  const emp = employees.value.find(e => e.id === order.value.employeeId)
  return emp ? emp.fullName : `Employee #${order.value.employeeId}`
})

const shipperName = computed(() => {
  if (!order.value || !order.value.shipperId) return 'Not assigned'
  const shipper = shippers.value.find(s => s.id === order.value.shipperId)
  return shipper ? shipper.companyName : `Shipper #${order.value.shipperId}`
})

async function confirmShip () {
  if (!shipForm.shipperId) return
  shipping.value = true
  try {
    const payload = {
      shipperId: shipForm.shipperId,
      shippedDate: shipForm.shippedDate || null,
    }
    const { data } = await api.post(`/orders/${order.value.id}/ship`, payload)
    order.value = data
    showShipDialog.value = false
    $q.notify({
      type: 'positive',
      message: `Order #${order.value.id} marked as shipped`,
      position: 'top-right',
    })
  } catch (err) {
    // Handled by interceptor
  } finally {
    shipping.value = false
  }
}

function downloadInvoice () {
  window.open(`http://localhost:5281/api/invoices/${order.value.id}`, '_blank')
}

function formatDate (dateStr) {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
  })
}

onMounted(async () => {
  await Promise.all([loadOrder(), loadShippers(), loadEmployees()])
})
</script>