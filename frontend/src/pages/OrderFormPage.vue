<template>
  <q-page class="bg-grey-1" padding>
    <!-- Loading -->
    <div v-if="loadingOrder" class="row justify-center q-pa-xl">
      <q-spinner-dots size="50px" color="primary" />
    </div>

    <div v-else class="page-container q-mx-auto" style="max-width: 1200px;">
      <!-- Header -->
      <div class="row items-center q-mb-xl q-mt-md">
        <q-btn flat round icon="arrow_back" color="grey-8" :to="{ name: 'orders' }" class="q-mr-sm" />
        <div class="col">
          <div class="text-h4 text-weight-bolder text-grey-9 tracking-tight">
            {{ isEdit ? `Edit Order #${route.params.id}` : 'New Order' }}
          </div>
          <div class="text-body2 text-grey-6 q-mt-xs">
            {{ isEdit ? 'Modify order details and line items' : 'Create a new customer order with shipping details' }}
          </div>
        </div>
        <div class="col-auto row q-gutter-sm">
          <q-btn flat no-caps label="Cancel" color="grey-7" :to="{ name: 'orders' }" class="rounded-button" />
          <q-btn
            unelevated
            no-caps
            :label="isEdit ? 'Update Order' : 'Create Order'"
            color="primary"
            class="rounded-button q-px-md"
            :loading="saving"
            @click="submitOrder"
          />
        </div>
      </div>

      <!-- Form -->
      <div class="row q-gutter-lg">
        <!-- Left column: Order info -->
        <div class="col-12 col-md-7">

          <!-- Customer & Employee -->
          <q-card flat class="mac-card q-mb-lg">
            <q-card-section class="q-pa-lg">
              <div class="section-title">Order Information</div>
              
              <div class="row q-col-gutter-md q-mb-md">
                <q-select
                  v-model="form.customerId"
                  :options="filteredCustomers"
                  option-value="id"
                  option-label="companyName"
                  emit-value
                  map-options
                  outlined
                  label="Customer *"
                  class="col-12 col-sm-6 elegant-input"
                  use-input
                  input-debounce="200"
                  @filter="filterCustomers"
                  :rules="[val => !!val || 'Customer is required']"
                  hide-bottom-space
                  tabindex="1"
                  :loading="customers.length === 0"
                  popup-content-class="order-form-dropdown"
                >
                  <template v-slot:selected-item="scope">
                    <span v-if="scope.opt && scope.opt.companyName">{{ scope.opt.companyName }}</span>
                    <span v-else-if="form.customerId" class="text-grey-7">{{ form.customerId }}</span>
                  </template>
                  <template v-slot:option="scope">
                    <q-item v-bind="scope.itemProps" dense>
                      <q-item-section avatar>
                        <q-avatar size="32px" color="grey-2" text-color="grey-8" font-size="13px" class="text-weight-bold">
                          {{ scope.opt.companyName?.charAt(0) }}
                        </q-avatar>
                      </q-item-section>
                      <q-item-section>
                        <q-item-label class="text-weight-medium text-grey-9">{{ scope.opt.companyName }}</q-item-label>
                        <q-item-label caption>{{ scope.opt.contactName }}</q-item-label>
                      </q-item-section>
                      <q-item-section side>
                        <q-item-label caption>{{ scope.opt.city }}, {{ scope.opt.country }}</q-item-label>
                      </q-item-section>
                    </q-item>
                  </template>
                </q-select>

                <q-select
                  v-model="form.employeeId"
                  :options="employees"
                  option-value="id"
                  option-label="fullName"
                  emit-value
                  map-options
                  outlined
                  label="Employee *"
                  class="col-12 col-sm-6 elegant-input"
                  :rules="[val => !!val || 'Employee is required']"
                  hide-bottom-space
                  tabindex="2"
                  :loading="employees.length === 0"
                >
                  <template v-slot:option="scope">
                    <q-item v-bind="scope.itemProps" dense>
                      <q-item-section avatar>
                        <q-avatar size="32px" color="blue-1" text-color="primary" font-size="13px" class="text-weight-bold">
                          {{ scope.opt.fullName?.charAt(0) }}
                        </q-avatar>
                      </q-item-section>
                      <q-item-section>
                        <q-item-label class="text-weight-medium text-grey-9">{{ scope.opt.fullName }}</q-item-label>
                        <q-item-label caption>{{ scope.opt.title || 'Employee' }}</q-item-label>
                      </q-item-section>
                    </q-item>
                  </template>
                </q-select>
              </div>

              <div class="row q-col-gutter-md">
                <q-select
                  v-model="form.shipperId"
                  :options="shippers"
                  option-value="id"
                  option-label="companyName"
                  emit-value
                  map-options
                  outlined
                  label="Shipper"
                  class="col-12 col-sm-4 elegant-input"
                  clearable
                  hide-bottom-space
                  tabindex="3"
                >
                  <template v-slot:option="scope">
                    <q-item v-bind="scope.itemProps" dense>
                      <q-item-section avatar>
                        <q-icon name="local_shipping" color="grey-6" size="xs" />
                      </q-item-section>
                      <q-item-section>
                        <q-item-label class="text-weight-medium text-grey-9">{{ scope.opt.companyName }}</q-item-label>
                        <q-item-label caption>{{ scope.opt.phone || 'No phone' }}</q-item-label>
                      </q-item-section>
                    </q-item>
                  </template>
                </q-select>

                <q-input
                  v-model="form.orderDate"
                  outlined
                  type="date"
                  label="Order Date *"
                  class="col-12 col-sm-4 elegant-input"
                  :rules="[val => !!val || 'Date is required']"
                  hide-bottom-space
                  tabindex="4"
                />

                <q-input
                  v-model.number="form.freight"
                  outlined
                  type="number"
                  label="Freight ($)"
                  class="col-12 col-sm-4 elegant-input"
                  min="0"
                  step="0.01"
                  hide-bottom-space
                  tabindex="5"
                />
              </div>
            </q-card-section>
          </q-card>

          <!-- Shipping Address -->
          <q-card flat class="mac-card q-mb-lg">
            <q-card-section class="q-pa-lg">
              <div class="section-title">Shipping Address</div>

              <div class="row q-col-gutter-md q-mb-md">
                <q-input
                  v-model="form.shipName"
                  outlined
                  label="Recipient Name *"
                  class="col-12 col-sm-6 elegant-input"
                  :rules="[val => !!val || 'Recipient name is required']"
                  hide-bottom-space
                  tabindex="6"
                />

                <q-input
                  v-model="form.shipStreet"
                  outlined
                  label="Street Address *"
                  class="col-12 col-sm-6 elegant-input"
                  :rules="[val => !!val || 'Street is required']"
                  hide-bottom-space
                  tabindex="7"
                />
              </div>

              <div class="row q-col-gutter-md q-mb-md">
                <q-input v-model="form.shipCity" outlined label="City *" class="col-12 col-sm-6 elegant-input"
                  :rules="[val => !!val || 'City is required']" hide-bottom-space tabindex="8" />
                <q-input v-model="form.shipRegion" outlined label="Region" class="col-12 col-sm-6 elegant-input" hide-bottom-space tabindex="9" />
              </div>

              <div class="row q-col-gutter-md">
                <q-input v-model="form.shipPostalCode" outlined label="Postal Code" class="col-12 col-sm-6 elegant-input" hide-bottom-space tabindex="10" />
                <q-input v-model="form.shipCountry" outlined label="Country *" class="col-12 col-sm-6 elegant-input"
                  :rules="[val => !!val || 'Country is required']" hide-bottom-space tabindex="11" />
              </div>

              <!-- Validate Address Button -->
              <div class="q-mt-lg">
                <q-btn
                  color="grey-9"
                  icon="location_on"
                  label="Validate Address"
                  outline
                  no-caps
                  class="rounded-button"
                  :loading="validatingAddress"
                  :disable="!canValidateAddress"
                  @click="validateAddress"
                />

                <div v-if="geocodeResult" class="validation-box success q-mt-md q-pa-md">
                  <div class="row items-center q-gutter-sm">
                    <q-icon name="check_circle" color="positive" size="20px" />
                    <span class="text-weight-bold text-positive text-subtitle2">ADDRESS VERIFIED</span>
                  </div>
                  <div class="q-mt-sm text-body2 text-grey-9">{{ geocodeResult.standardizedAddress }}</div>
                  <div class="text-caption text-grey-6 q-mt-xs">
                    {{ geocodeResult.latitude.toFixed(6) }}, {{ geocodeResult.longitude.toFixed(6) }} &nbsp;·&nbsp; {{ geocodeResult.placeType }}
                  </div>
                </div>

                <div v-if="geocodeError" class="validation-box error q-mt-md q-pa-md">
                  <div class="row items-center q-gutter-sm">
                    <q-icon name="error" color="negative" size="20px" />
                    <span class="text-negative text-weight-medium">{{ geocodeError }}</span>
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Line Items -->
          <q-card flat class="mac-card q-mb-lg">
            <q-card-section class="q-pa-lg">
              <div class="row items-center justify-between q-mb-md">
                <div class="section-title q-mb-none">Line Items</div>
                <q-btn outline no-caps color="grey-8" icon="add" label="Add Product" size="sm" class="rounded-button" @click="addLine" />
              </div>

              <!-- No lines message -->
              <div v-if="form.lines.length === 0" class="text-center q-pa-xl text-grey-5 bg-grey-1 rounded-borders dashed-border">
                <q-icon name="inventory_2" size="2.5em" class="q-mb-sm text-grey-4" />
                <div class="text-body2">No products added yet. Click "Add Product" to start.</div>
              </div>

              <!-- Line items table -->
              <template v-if="form.lines.length > 0">
                <!-- Table header -->
                <div class="row q-gutter-sm items-center q-pb-sm q-px-sm line-header">
                  <div class="col text-caption text-grey-5 text-weight-bold tracking-wide">PRODUCT</div>
                  <div style="width: 80px" class="text-caption text-grey-5 text-weight-bold tracking-wide">QTY</div>
                  <div style="width: 110px" class="text-caption text-grey-5 text-weight-bold tracking-wide">PRICE</div>
                  <div style="width: 90px" class="text-caption text-grey-5 text-weight-bold tracking-wide">DISC.</div>
                  <div style="width: 100px" class="text-caption text-grey-5 text-weight-bold tracking-wide text-right">TOTAL</div>
                  <div style="width: 32px"></div>
                </div>
                <q-separator color="grey-2" class="q-mb-sm" />

                <div
                  v-for="(line, index) in form.lines"
                  :key="index"
                  class="row q-gutter-sm items-center q-mb-sm line-row q-px-sm"
                >
                  <!-- Product select -->
                  <q-select
                    v-model="line.productId"
                    :options="filteredProducts[index] || []"
                    option-value="id"
                    option-label="productName"
                    emit-value
                    map-options
                    outlined
                    dense
                    label="Search product..."
                    class="col elegant-input"
                    use-input
                    input-debounce="200"
                    hide-bottom-space
                    @filter="(val, update) => filterProducts(val, update, index)"
                    @update:model-value="onProductSelected(index)"
                    :loading="products.length === 0"
                    popup-content-class="order-form-dropdown"
                  >
                    <template v-slot:option="scope">
                      <q-item v-bind="scope.itemProps" dense class="q-py-sm">
                        <q-item-section>
                          <q-item-label class="text-weight-medium text-grey-9">{{ scope.opt.productName }}</q-item-label>
                          <q-item-label caption class="text-grey-6">
                            ${{ scope.opt.unitPrice.toFixed(2) }} &nbsp;·&nbsp; {{ scope.opt.unitsInStock }} in stock
                          </q-item-label>
                        </q-item-section>
                        <q-item-section side>
                          <q-badge
                            rounded
                            :color="scope.opt.unitsInStock > 20 ? 'positive' : scope.opt.unitsInStock > 0 ? 'warning' : 'negative'"
                            :label="scope.opt.unitsInStock > 20 ? 'In stock' : scope.opt.unitsInStock > 0 ? 'Low' : 'Out'"
                            class="q-px-sm q-py-xs"
                          />
                        </q-item-section>
                      </q-item>
                    </template>
                    <template v-slot:no-option>
                      <q-item>
                        <q-item-section class="text-grey-5 text-center text-body2 q-pa-md">
                          No products found
                        </q-item-section>
                      </q-item>
                    </template>
                  </q-select>

                  <q-input
                    v-model.number="line.quantity"
                    outlined dense type="number" label="Qty"
                    style="width: 80px" min="1" hide-bottom-space
                    class="elegant-input text-center-input"
                  />

                  <q-input
                    v-model.number="line.unitPrice"
                    outlined dense type="number"
                    style="width: 110px" prefix="$" readonly
                    hide-bottom-space
                    class="elegant-input bg-grey-1"
                  />

                  <q-input
                    v-model.number="line.discount"
                    outlined dense type="number"
                    style="width: 90px" min="0" max="1" step="0.05"
                    hide-bottom-space
                    class="elegant-input text-center-input"
                  />

                  <div style="width: 100px" class="text-right text-subtitle2 text-weight-bold text-grey-9">
                    ${{ lineTotal(line).toFixed(2) }}
                  </div>

                  <div style="width: 32px" class="text-right">
                    <q-btn
                      flat round dense icon="close" color="grey-5" size="sm"
                      class="hover-red"
                      @click="removeLine(index)"
                    >
                      <q-tooltip class="bg-grey-9">Remove</q-tooltip>
                    </q-btn>
                  </div>
                </div>

                <!-- Totals -->
                <q-separator color="grey-2" class="q-my-lg" />
                <div class="row justify-end">
                  <div class="col-12 col-sm-5 col-md-6 bg-grey-1 q-pa-md rounded-borders">
                    <div class="row justify-between q-mb-sm">
                      <div class="text-body2 text-grey-6">Subtotal</div>
                      <div class="text-body2 text-weight-medium text-grey-9">${{ subtotal.toFixed(2) }}</div>
                    </div>
                    <div class="row justify-between q-mb-md">
                      <div class="text-body2 text-grey-6">Freight</div>
                      <div class="text-body2 text-weight-medium text-grey-9">${{ (form.freight || 0).toFixed(2) }}</div>
                    </div>
                    <q-separator color="grey-3" class="q-mb-sm" />
                    <div class="row justify-between items-center">
                      <div class="text-subtitle2 text-grey-8 text-weight-bold text-uppercase tracking-wide">Grand Total</div>
                      <div class="text-h5 text-primary text-weight-bolder">${{ grandTotal.toFixed(2) }}</div>
                    </div>
                  </div>
                </div>
              </template>
            </q-card-section>
          </q-card>
        </div>

        <!-- Right column: Map preview -->
        <div class="col-12 col-md-5">
          <q-card flat class="mac-card sticky-card">
            <q-card-section class="q-pa-lg">
              <div class="section-title">Delivery Location</div>

              <div v-if="geocodeResult" class="map-wrapper q-mt-md">
                <iframe
                  :src="mapEmbedUrl" width="100%" height="380"
                  class="map-iframe"
                  allowfullscreen loading="lazy"
                  referrerpolicy="no-referrer-when-downgrade"
                ></iframe>
                <div class="row q-gutter-sm q-mt-md items-center">
                  <q-badge v-if="isHeavyFreightAccessible" color="grey-9" class="q-px-sm q-py-xs text-weight-medium"
                    label="Heavy Freight Accessible" />
                  <q-badge color="grey-3" text-color="grey-8" class="q-px-sm q-py-xs text-weight-medium" :label="geocodeResult.placeType" />
                </div>
              </div>

              <div v-else class="text-center q-pa-xl text-grey-5 empty-map-container q-mt-md">
                <q-skeleton v-if="validatingAddress" type="rect" height="380px" class="rounded-borders" />
                <template v-else>
                  <q-icon name="place" size="3em" class="q-mb-md text-grey-4" />
                  <div class="text-body2 text-grey-6 q-px-md">
                    Complete the shipping address to generate the delivery map preview.
                  </div>
                </template>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </div>
  </q-page>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useQuasar } from 'quasar'
import { api } from 'src/boot/axios'
import { useOrderDraftStore } from 'src/stores/order-draft'

const route = useRoute()
const router = useRouter()
const $q = useQuasar()

const isEdit = computed(() => !!route.params.id)
const loadingOrder = ref(false)
const saving = ref(false)
const validatingAddress = ref(false)

// ---- Form state ----

const form = reactive({
  customerId: null,
  employeeId: null,
  shipperId: null,
  orderDate: new Date().toISOString().split('T')[0],
  freight: 0,
  shipName: '',
  shipStreet: '',
  shipCity: '',
  shipRegion: '',
  shipPostalCode: '',
  shipCountry: '',
  lines: [],
})

// ---- Reference data ----

const customers = ref([])
const filteredCustomers = ref([])
const employees = ref([])
const shippers = ref([])

// New products reference
const products = ref([])
const filteredProducts = ref({})

// ---- Geocoding ----

const geocodeResult = ref(null)
const geocodeError = ref(null)

const canValidateAddress = computed(() =>
  form.shipStreet && form.shipCity && form.shipCountry
)

const mapEmbedUrl = computed(() => {
  if (!geocodeResult.value) return ''
  const { latitude, longitude } = geocodeResult.value
  return `https://maps.google.com/maps?q=${latitude},${longitude}&z=15&output=embed`
})

const isHeavyFreightAccessible = computed(() => {
  if (!geocodeResult.value) return false
  const type = geocodeResult.value.placeType?.toLowerCase()
  return ['premise', 'subpremise', 'establishment'].includes(type)
})

// ---- Line item helpers ----

function lineTotal (line) {
  const gross = (line.unitPrice || 0) * (line.quantity || 0)
  return gross * (1 - (line.discount || 0))
}

const subtotal = computed(() =>
  form.lines.reduce((sum, line) => sum + lineTotal(line), 0)
)

const grandTotal = computed(() =>
  subtotal.value + (form.freight || 0)
)

function addLine () {
  form.lines.push({ productId: null, quantity: 1, unitPrice: 0, discount: 0 })
}

function removeLine (index) {
  form.lines.splice(index, 1)
}

function onProductSelected (index) {
  const line = form.lines[index]
  if (!line.productId) return
  
  // Find product from our global list of products
  const product = products.value.find(p => p.id === line.productId)
  if (product) {
    line.unitPrice = product.unitPrice
  }
}

// ---- Filters ----

function filterCustomers (val, update) {
  update(() => {
    if (!val) {
      filteredCustomers.value = customers.value.slice(0, 30)
    } else {
      const needle = val.toLowerCase()
      filteredCustomers.value = customers.value.filter(c =>
        c.companyName.toLowerCase().includes(needle) ||
        c.contactName?.toLowerCase().includes(needle) ||
        c.city?.toLowerCase().includes(needle) ||
        c.country?.toLowerCase().includes(needle)
      )
    }
  })
}

function filterProducts (val, update, index) {
  update(() => {
    // If no text, show first 50 products by default
    if (!val) {
      filteredProducts.value[index] = products.value.slice(0, 50)
      return
    }
    
    // Filter locally based on what the user types
    const needle = val.toLowerCase()
    filteredProducts.value[index] = products.value.filter(p => 
      p.productName.toLowerCase().includes(needle)
    )
  })
}

// ---- Debounced address validation ----
let debounceTimer = null

watch(
  () => [form.shipStreet, form.shipCity, form.shipCountry],
  () => {
    geocodeResult.value = null
    geocodeError.value = null
    if (debounceTimer) clearTimeout(debounceTimer)
    if (canValidateAddress.value) {
      debounceTimer = setTimeout(() => { validateAddress() }, 600)
    }
  }
)

async function validateAddress () {
  validatingAddress.value = true
  geocodeResult.value = null
  geocodeError.value = null
  try {
    const { data } = await api.get('/geocoding/validate', {
      params: {
        street: form.shipStreet,
        city: form.shipCity,
        region: form.shipRegion || undefined,
        postalCode: form.shipPostalCode || undefined,
        country: form.shipCountry,
      }
    })
    geocodeResult.value = data
  } catch (err) {
    geocodeError.value = err.response?.data?.error || 'Address validation failed'
  } finally {
    validatingAddress.value = false
  }
}

// ---- Submit ----

async function submitOrder () {
  if (!form.customerId || !form.employeeId || !form.orderDate) {
    $q.notify({ type: 'warning', message: 'Please fill in all required fields.', position: 'top-right' })
    return
  }
  if (form.lines.length === 0) {
    $q.notify({ type: 'warning', message: 'Please add at least one product.', position: 'top-right' })
    return
  }
  if (!form.shipName || !form.shipStreet || !form.shipCity || !form.shipCountry) {
    $q.notify({ type: 'warning', message: 'Please fill in the shipping address.', position: 'top-right' })
    return
  }

  saving.value = true
  try {
    const payload = {
      customerId: form.customerId,
      employeeId: form.employeeId,
      orderDate: form.orderDate,
      shipName: form.shipName,
      shipStreet: form.shipStreet,
      shipCity: form.shipCity,
      shipRegion: form.shipRegion || null,
      shipPostalCode: form.shipPostalCode || null,
      shipCountry: form.shipCountry,
      freight: form.freight || 0,
      lines: form.lines.map(l => ({
        productId: l.productId,
        quantity: l.quantity,
        discount: l.discount || 0,
      })),
    }

    if (isEdit.value) {
      payload.orderId = parseInt(route.params.id)
      payload.shipperId = form.shipperId || null
      await api.put(`/orders/${route.params.id}`, payload)
      $q.notify({ type: 'positive', message: `Order #${route.params.id} updated successfully`, position: 'top-right' })
    } else {
      const { data } = await api.post('/orders', payload)
      $q.notify({ type: 'positive', message: `Order #${data.id} created successfully`, position: 'top-right' })
    }

    router.push({ name: 'orders' })
    draftStore.clearDraft()
  } catch (err) {
    // Handled by Axios interceptor
  } finally {
    saving.value = false
  }
}

// ---- Load data ----

async function loadReferenceData () {
  try {
    const [custRes, empRes, shipRes, prodRes] = await Promise.all([
      api.get('/customers'),
      api.get('/employees'),
      api.get('/shippers'),
      api.get('/products') // Load all products upfront
    ])
    
    customers.value = custRes.data
    filteredCustomers.value = custRes.data.slice(0, 30)
    
    employees.value = empRes.data
    shippers.value = shipRes.data
    
    // Save products to local reference
    products.value = prodRes.data
  } catch (err) {
    // Handled by interceptor
  }
}

async function loadOrder () {
  if (!isEdit.value) return
  loadingOrder.value = true
  try {
    const { data } = await api.get(`/orders/${route.params.id}`)
    form.customerId = data.customerId
    form.employeeId = data.employeeId
    form.shipperId = data.shipperId || null
    form.orderDate = data.orderDate?.split('T')[0]
    form.freight = data.freight
    form.shipName = data.shipName
    form.shipStreet = data.shipStreet
    form.shipCity = data.shipCity
    form.shipRegion = data.shipRegion || ''
    form.shipPostalCode = data.shipPostalCode || ''
    form.shipCountry = data.shipCountry
    form.lines = data.lines.map(l => ({
      productId: l.productId,
      quantity: l.quantity,
      unitPrice: l.unitPrice,
      discount: l.discount,
    }))
  } catch (err) {
    // Handled by interceptor
  } finally {
    loadingOrder.value = false
  }
}

// ---- Auto-save draft ----
const draftStore = useOrderDraftStore()
let autoSaveTimer = null

function startAutoSave () {
  autoSaveTimer = setInterval(() => {
    if (!isEdit.value && form.customerId) {
      draftStore.saveDraft(form)
    }
  }, 3000)
}

// ---- Init ----
onMounted(async () => {
  await loadReferenceData()

  if (isEdit.value) {
    await loadOrder()
  } else if (draftStore.hasDraft && draftStore.getDraft()) {
    $q.dialog({
      title: 'Recover unsaved order?',
      message: 'You have an unsaved order draft from a previous session. Would you like to recover it?',
      cancel: 'Start fresh',
      ok: 'Recover draft',
      persistent: true,
      color: 'grey-9',
    }).onOk(() => {
      const saved = draftStore.getDraft()
      Object.assign(form, saved)
    }).onCancel(() => {
      draftStore.clearDraft()
    })
  }

  startAutoSave()
})
</script>

<style scoped>
/* Typography & Layout overrides */
.tracking-tight {
  letter-spacing: -0.02em;
}
.tracking-wide {
  letter-spacing: 0.05em;
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

.section-title {
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  color: #8e8e93;
  font-weight: 700;
  text-transform: uppercase;
  margin-bottom: 20px;
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

:deep(.text-center-input input) {
  text-align: center;
}

/* Buttons */
.rounded-button {
  border-radius: 8px;
  font-weight: 500;
}

/* Validation Boxes */
.validation-box {
  border-radius: 8px;
  border-left: 4px solid;
}
.validation-box.success {
  background-color: #f2fbf5;
  border-left-color: #34c759;
}
.validation-box.error {
  background-color: #fef5f5;
  border-left-color: #ff3b30;
}

/* Map area */
.sticky-card {
  position: sticky;
  top: 24px;
}
.map-iframe {
  border: 1px solid #e5e5ea;
  border-radius: 10px;
  background-color: #f9f9f9;
}
.empty-map-container {
  border: 1px dashed #d1d1d6;
  border-radius: 10px;
  background-color: #fafafa;
}

/* Line Items */
.dashed-border {
  border: 1px dashed #d1d1d6;
}
.line-row {
  border-radius: 8px;
  padding-top: 4px;
  padding-bottom: 4px;
  transition: background-color 0.2s ease;
}
.line-row:hover {
  background-color: #f9f9fa;
}

.hover-red:hover {
  color: #ff3b30 !important;
  background: rgba(255, 59, 48, 0.1);
}
</style>

<style>
/* Global dropdown styling */
.order-form-dropdown {
  border-radius: 8px !important;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08) !important;
  border: 1px solid rgba(0, 0, 0, 0.04);
  max-height: 350px !important;
}
</style>