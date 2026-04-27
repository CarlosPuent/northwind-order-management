<template>
  <q-page padding>
    <!-- Loading -->
    <div v-if="loadingOrder" class="row justify-center q-pa-xl">
      <q-spinner-dots size="50px" color="primary" />
    </div>

    <template v-else>
      <!-- Header -->
      <div class="row items-center q-mb-lg">
        <q-btn flat round icon="arrow_back" color="primary" :to="{ name: 'orders' }" />
        <div class="q-ml-md col">
          <div class="text-h4 text-primary text-weight-bold">
            {{ isEdit ? `Edit Order #${route.params.id}` : 'New Order' }}
          </div>
          <div class="text-caption text-grey-7">
            {{ isEdit ? 'Modify order details and line items' : 'Create a new customer order with shipping details' }}
          </div>
        </div>
      </div>

      <!-- Form -->
      <div class="row q-gutter-md">
        <!-- Left column: Order info -->
        <div class="col-12 col-md-7">
          <!-- Customer & Employee -->
          <q-card flat bordered class="q-mb-md">
            <q-card-section>
              <div class="text-overline text-primary q-mb-md">ORDER INFORMATION</div>
              <div class="row q-gutter-md">
                <q-select
                  v-model="form.customerId"
                  :options="filteredCustomers"
                  option-value="id"
                  option-label="companyName"
                  emit-value
                  map-options
                  outlined
                  label="Customer *"
                  class="col"
                  use-input
                  input-debounce="300"
                  @filter="filterCustomers"
                  :rules="[val => !!val || 'Customer is required']"
                >
                  <template v-slot:option="scope">
                    <q-item v-bind="scope.itemProps">
                      <q-item-section>
                        <q-item-label>{{ scope.opt.companyName }}</q-item-label>
                        <q-item-label caption>{{ scope.opt.contactName }} · {{ scope.opt.city }}, {{ scope.opt.country }}</q-item-label>
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
                  class="col"
                  :rules="[val => !!val || 'Employee is required']"
                />
              </div>

              <div class="row q-gutter-md q-mt-sm">
                <q-input
                  v-model="form.orderDate"
                  outlined
                  type="date"
                  label="Order Date *"
                  class="col"
                  :rules="[val => !!val || 'Date is required']"
                />
                <q-input
                  v-model.number="form.freight"
                  outlined
                  type="number"
                  label="Freight ($)"
                  class="col"
                  min="0"
                  step="0.01"
                />
              </div>
            </q-card-section>
          </q-card>

          <!-- Shipping Address -->
          <q-card flat bordered class="q-mb-md">
            <q-card-section>
              <div class="text-overline text-primary q-mb-md">SHIPPING ADDRESS</div>

              <q-input
                v-model="form.shipName"
                outlined
                label="Recipient Name *"
                class="q-mb-md"
                :rules="[val => !!val || 'Recipient name is required']"
              />

              <q-input
                v-model="form.shipStreet"
                outlined
                label="Street Address *"
                class="q-mb-md"
                :rules="[val => !!val || 'Street is required']"
              />

              <div class="row q-gutter-md">
                <q-input
                  v-model="form.shipCity"
                  outlined
                  label="City *"
                  class="col"
                  :rules="[val => !!val || 'City is required']"
                />
                <q-input
                  v-model="form.shipRegion"
                  outlined
                  label="Region"
                  class="col"
                />
              </div>

              <div class="row q-gutter-md q-mt-sm">
                <q-input
                  v-model="form.shipPostalCode"
                  outlined
                  label="Postal Code"
                  class="col"
                />
                <q-input
                  v-model="form.shipCountry"
                  outlined
                  label="Country *"
                  class="col"
                  :rules="[val => !!val || 'Country is required']"
                />
              </div>

              <!-- Validate Address Button -->
              <div class="q-mt-md">
                <q-btn
                  color="secondary"
                  icon="location_on"
                  label="Validate Address"
                  outline
                  :loading="validatingAddress"
                  :disable="!canValidateAddress"
                  @click="validateAddress"
                />

                <!-- Validation result card -->
                <q-card v-if="geocodeResult" flat bordered class="q-mt-md bg-green-1">
                  <q-card-section class="q-pa-md">
                    <div class="row items-center q-gutter-sm">
                      <q-icon name="check_circle" color="positive" size="sm" />
                      <q-badge color="positive" label="VERIFIED" />
                    </div>
                    <div class="q-mt-sm text-body2">
                      {{ geocodeResult.standardizedAddress }}
                    </div>
                    <div class="text-caption text-grey-7 q-mt-xs">
                      Coordinates: {{ geocodeResult.latitude.toFixed(6) }}, {{ geocodeResult.longitude.toFixed(6) }}
                      · Type: {{ geocodeResult.placeType }}
                    </div>
                  </q-card-section>
                </q-card>

                <q-card v-if="geocodeError" flat bordered class="q-mt-md bg-red-1">
                  <q-card-section class="q-pa-md">
                    <div class="row items-center q-gutter-sm">
                      <q-icon name="error" color="negative" size="sm" />
                      <span class="text-negative text-weight-medium">{{ geocodeError }}</span>
                    </div>
                  </q-card-section>
                </q-card>
              </div>
            </q-card-section>
          </q-card>

          <!-- Line Items -->
          <q-card flat bordered class="q-mb-md">
            <q-card-section>
              <div class="row items-center q-mb-md">
                <div class="text-overline text-primary col">LINE ITEMS</div>
                <q-btn
                  flat
                  color="primary"
                  icon="add"
                  label="Add Product"
                  size="sm"
                  @click="addLine"
                />
              </div>

              <!-- No lines message -->
              <div v-if="form.lines.length === 0" class="text-center q-pa-lg text-grey-5">
                <q-icon name="shopping_cart" size="3em" class="q-mb-sm" />
                <div>No products added yet. Click "Add Product" to start.</div>
              </div>

              <!-- Line items -->
              <div
                v-for="(line, index) in form.lines"
                :key="index"
                class="row q-gutter-sm items-center q-mb-sm"
              >
                <q-select
                  v-model="line.productId"
                  :options="filteredProducts[index] || []"
                  option-value="id"
                  option-label="productName"
                  emit-value
                  map-options
                  outlined
                  dense
                  label="Product"
                  class="col"
                  use-input
                  input-debounce="300"
                  @filter="(val, update) => filterProducts(val, update, index)"
                  @update:model-value="onProductSelected(index)"
                >
                  <template v-slot:option="scope">
                    <q-item v-bind="scope.itemProps">
                      <q-item-section>
                        <q-item-label>{{ scope.opt.productName }}</q-item-label>
                        <q-item-label caption>${{ scope.opt.unitPrice.toFixed(2) }} · {{ scope.opt.unitsInStock }} in stock</q-item-label>
                      </q-item-section>
                    </q-item>
                  </template>
                </q-select>

                <q-input
                  v-model.number="line.quantity"
                  outlined
                  dense
                  type="number"
                  label="Qty"
                  style="width: 80px"
                  min="1"
                />

                <q-input
                  v-model.number="line.unitPrice"
                  outlined
                  dense
                  type="number"
                  label="Price"
                  style="width: 100px"
                  prefix="$"
                  readonly
                />

                <q-input
                  v-model.number="line.discount"
                  outlined
                  dense
                  type="number"
                  label="Discount"
                  style="width: 90px"
                  min="0"
                  max="1"
                  step="0.05"
                />

                <div style="width: 90px" class="text-right text-weight-bold">
                  ${{ lineTotal(line).toFixed(2) }}
                </div>

                <q-btn
                  flat
                  round
                  dense
                  icon="close"
                  color="negative"
                  @click="removeLine(index)"
                />
              </div>

              <!-- Totals -->
              <div v-if="form.lines.length > 0" class="q-mt-md">
                <q-separator class="q-mb-md" />
                <div class="row justify-end q-gutter-md">
                  <div class="text-right">
                    <div class="text-caption text-grey-7">Subtotal</div>
                    <div class="text-body1">${{ subtotal.toFixed(2) }}</div>
                  </div>
                  <div class="text-right">
                    <div class="text-caption text-grey-7">Freight</div>
                    <div class="text-body1">${{ (form.freight || 0).toFixed(2) }}</div>
                  </div>
                  <div class="text-right">
                    <div class="text-caption text-grey-7">Total</div>
                    <div class="text-h6 text-primary text-weight-bold">${{ grandTotal.toFixed(2) }}</div>
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Submit -->
          <div class="row justify-end q-gutter-sm q-mb-xl">
            <q-btn
              flat
              label="Cancel"
              color="grey-7"
              :to="{ name: 'orders' }"
            />
            <q-btn
              unelevated
              :label="isEdit ? 'Update Order' : 'Create Order'"
              color="primary"
              icon="save"
              :loading="saving"
              @click="submitOrder"
            />
          </div>
        </div>

        <!-- Right column: Map preview -->
        <div class="col-12 col-md-5">
          <q-card flat bordered class="sticky-card">
            <q-card-section>
              <div class="text-overline text-primary q-mb-md">DELIVERY LOCATION</div>

              <div v-if="geocodeResult" class="map-container">
                <iframe
                  :src="mapEmbedUrl"
                  width="100%"
                  height="350"
                  style="border: 0; border-radius: 8px;"
                  allowfullscreen
                  loading="lazy"
                  referrerpolicy="no-referrer-when-downgrade"
                ></iframe>

                <div class="q-mt-sm">
                  <q-badge
                    v-if="isHeavyFreightAccessible"
                    color="positive"
                    label="✓ Accessible for Heavy Freight"
                    class="q-mr-sm"
                  />
                  <q-badge
                    color="info"
                    :label="geocodeResult.placeType"
                  />
                </div>
              </div>

              <div v-else class="text-center q-pa-xl text-grey-5">
                <q-icon name="map" size="4em" class="q-mb-md" />
                <div class="text-body2">
                  Fill in the shipping address and click "Validate Address" to see the delivery location on the map.
                </div>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </template>
  </q-page>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useQuasar } from 'quasar'
import { api } from 'src/boot/axios'

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
const productCache = ref([])
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
  form.lines.push({
    productId: null,
    quantity: 1,
    unitPrice: 0,
    discount: 0,
  })
}

function removeLine (index) {
  form.lines.splice(index, 1)
}

function onProductSelected (index) {
  const line = form.lines[index]
  if (!line.productId) return

  // Find the product in cache to auto-fill the unit price.
  const allProducts = Object.values(filteredProducts.value).flat()
  const product = allProducts.find(p => p.id === line.productId)
  if (product) {
    line.unitPrice = product.unitPrice
  }
}

// ---- Filters for autocomplete ----

function filterCustomers (val, update) {
  update(() => {
    if (!val) {
      filteredCustomers.value = customers.value.slice(0, 20)
    } else {
      const needle = val.toLowerCase()
      filteredCustomers.value = customers.value.filter(
        c => c.companyName.toLowerCase().includes(needle)
      )
    }
  })
}

function filterProducts (val, update, index) {
  if (!val || val.length < 2) {
    update(() => { filteredProducts.value[index] = [] })
    return
  }
  api.get('/products/search', { params: { q: val } })
    .then(({ data }) => {
      update(() => { filteredProducts.value[index] = data })
    })
    .catch(() => {
      update(() => { filteredProducts.value[index] = [] })
    })
}

// ---- Address validation ----

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
  // Basic client-side validation.
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
      payload.shipperId = null
      await api.put(`/orders/${route.params.id}`, payload)
      $q.notify({ type: 'positive', message: `Order #${route.params.id} updated successfully`, position: 'top-right' })
    } else {
      const { data } = await api.post('/orders', payload)
      $q.notify({ type: 'positive', message: `Order #${data.id} created successfully`, position: 'top-right' })
    }

    router.push({ name: 'orders' })
  } catch (err) {
    // Handled by Axios interceptor.
  } finally {
    saving.value = false
  }
}

// ---- Load data ----

async function loadReferenceData () {
  try {
    const [custRes, empRes] = await Promise.all([
      api.get('/customers'),
      api.get('/employees'),
    ])
    customers.value = custRes.data
    filteredCustomers.value = custRes.data.slice(0, 20)
    employees.value = empRes.data
  } catch (err) {
    // Handled by interceptor.
  }
}

async function loadOrder () {
  if (!isEdit.value) return
  loadingOrder.value = true
  try {
    const { data } = await api.get(`/orders/${route.params.id}`)
    form.customerId = data.customerId
    form.employeeId = data.employeeId
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
    // Handled by interceptor.
  } finally {
    loadingOrder.value = false
  }
}

// ---- Init ----

onMounted(async () => {
  await loadReferenceData()
  await loadOrder()
})
</script>

<style scoped>
.sticky-card {
  position: sticky;
  top: 80px;
}

.map-container iframe {
  transition: opacity 0.3s ease;
}
</style>