<template>
  <q-page padding>

    <!-- ── Header ─────────────────────────────────────────────────── -->
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h4 text-primary text-weight-bold">Dashboard</div>
        <div class="text-caption text-grey-6">Order analytics and shipment insights</div>
      </div>
      <div class="col-auto">
        <q-select
          v-model="selectedYear"
          :options="availableYears"
          :disable="loadingYears || availableYears.length === 0"
          dense outlined label="Year"
          style="min-width: 140px"
        >
          <template v-slot:prepend>
            <q-icon name="calendar_today" size="16px" />
          </template>
        </q-select>
      </div>
    </div>

    <!-- ── KPI Cards ──────────────────────────────────────────────── -->
    <div class="row q-col-gutter-md q-mb-lg">

      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="kpi-card kpi-card--orders">
          <q-card-section v-if="loading" class="row items-center no-wrap q-pa-md">
            <q-skeleton type="QAvatar" size="44px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="55%" class="q-mb-xs" />
              <q-skeleton type="text" width="35%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap q-pa-md">
            <q-avatar color="primary" text-color="white" size="44px">
              <q-icon name="shopping_cart" size="20px" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="kpi-label">Total Orders</div>
              <div class="kpi-value">{{ totalOrdersDisplay }}</div>
              <div v-if="ordersTrend !== null" class="kpi-trend q-mt-xs">
                <q-icon
                  :name="ordersTrend >= 0 ? 'arrow_upward' : 'arrow_downward'"
                  :color="ordersTrend >= 0 ? 'positive' : 'grey-5'"
                  size="12px"
                />
                <span :class="ordersTrend >= 0 ? 'text-positive' : 'text-grey-5'">
                  {{ ordersTrend >= 0 ? '+' : '' }}{{ ordersTrend }}% vs {{ selectedYear - 1 }}
                </span>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="kpi-card kpi-card--revenue">
          <q-card-section v-if="loading" class="row items-center no-wrap q-pa-md">
            <q-skeleton type="QAvatar" size="44px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="55%" class="q-mb-xs" />
              <q-skeleton type="text" width="35%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap q-pa-md">
            <q-avatar color="teal" text-color="white" size="44px">
              <q-icon name="attach_money" size="20px" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="kpi-label">Total Revenue</div>
              <div class="kpi-value">{{ totalRevenueDisplay }}</div>
              <div class="kpi-sub">Subtotals excl. freight</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="kpi-card kpi-card--country">
          <q-card-section v-if="loading" class="row items-center no-wrap q-pa-md">
            <q-skeleton type="QAvatar" size="44px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="55%" class="q-mb-xs" />
              <q-skeleton type="text" width="35%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap q-pa-md">
            <q-avatar color="deep-orange" text-color="white" size="44px">
              <q-icon name="public" size="20px" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="kpi-label">Top Country</div>
              <div class="kpi-value">{{ topCountryDisplay }}</div>
              <div v-if="topCountryOrders" class="kpi-sub">{{ topCountryOrders }} orders</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="kpi-card kpi-card--avg">
          <q-card-section v-if="loading" class="row items-center no-wrap q-pa-md">
            <q-skeleton type="QAvatar" size="44px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="55%" class="q-mb-xs" />
              <q-skeleton type="text" width="35%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap q-pa-md">
            <q-avatar color="positive" text-color="white" size="44px">
              <q-icon name="calendar_month" size="20px" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="kpi-label">Avg Orders / Month</div>
              <div class="kpi-value">{{ avgOrdersDisplay }}</div>
              <div class="kpi-sub">Over {{ ordersChartData.length }} months</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

    </div>

    <!-- ── Orders Over Time ───────────────────────────────────────── -->
    <div class="row q-col-gutter-md q-mb-md">
      <div class="col-12">
        <q-card bordered>
          <q-card-section class="row items-center q-pb-none">
            <div>
              <div class="text-subtitle1 text-weight-bold">Orders over time</div>
              <div class="text-caption text-grey-6">
                {{ selectedYear ? `Year ${selectedYear}` : 'All years' }}
                &nbsp;·&nbsp;
                Orders volume and revenue per month
              </div>
            </div>
          </q-card-section>
          <q-separator class="q-mt-sm" />

          <q-card-section v-if="loading">
            <q-skeleton type="rect" height="300px" />
          </q-card-section>

          <q-card-section v-else-if="ordersChartData.length === 0" class="flex flex-center" style="min-height:300px">
            <div class="text-grey-5 text-center">
              <q-icon name="analytics" size="56px" class="q-mb-sm" style="opacity:0.4" />
              <div class="text-subtitle2">No data available for this period</div>
            </div>
          </q-card-section>

          <q-card-section v-else class="q-pt-sm">
            <apexchart type="bar" height="300" :options="ordersChartOptions" :series="ordersChartSeries" />
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- ── Shipments · Top Customers · Recent Orders ──────────────── -->
    <div class="row q-col-gutter-md q-mb-md">

      <!-- Donut — col-4 -->
      <div class="col-12 col-md-4">
        <q-card bordered class="full-height">
          <q-card-section class="q-pb-none">
            <div class="text-subtitle1 text-weight-bold">Shipments by country</div>
            <div class="text-caption text-grey-6">
              {{ selectedYear ? `Year ${selectedYear}` : 'All years' }} · Top 10
            </div>
          </q-card-section>
          <q-separator class="q-mt-sm" />

          <q-card-section v-if="loading">
            <q-skeleton type="rect" height="320px" />
          </q-card-section>

          <q-card-section v-else-if="regionsData.length === 0" class="flex flex-center" style="min-height:320px">
            <div class="text-grey-5 text-center">
              <q-icon name="public_off" size="48px" style="opacity:0.4" />
              <div class="text-subtitle2 q-mt-sm">No data available</div>
            </div>
          </q-card-section>

          <q-card-section v-else>
            <apexchart
              type="donut" height="320"
              :options="regionsChartOptions"
              :series="regionsChartSeries"
            />
          </q-card-section>
        </q-card>
      </div>

      <!-- Top Customers — col-5 -->
      <div class="col-12 col-md-5">
        <q-card bordered class="full-height">
          <q-card-section class="q-pb-none">
            <div class="text-subtitle1 text-weight-bold">Top 5 customers by revenue</div>
            <div class="text-caption text-grey-6">
              {{ selectedYear ? `Year ${selectedYear}` : 'All years' }}
            </div>
          </q-card-section>
          <q-separator class="q-mt-sm" />

          <div v-if="loadingCustomers">
            <q-item v-for="i in 5" :key="i" class="q-py-sm">
              <q-item-section avatar><q-skeleton type="QAvatar" size="36px" /></q-item-section>
              <q-item-section>
                <q-skeleton type="text" width="70%" />
                <q-skeleton type="text" width="40%" />
              </q-item-section>
              <q-item-section side><q-skeleton type="text" width="64px" /></q-item-section>
            </q-item>
          </div>

          <div v-else-if="topCustomers.length === 0" class="flex flex-center" style="min-height:280px">
            <div class="text-grey-5 text-center">
              <q-icon name="people_outline" size="48px" style="opacity:0.4" />
              <div class="text-subtitle2 q-mt-sm">No customer data available</div>
            </div>
          </div>

          <q-list v-else class="q-pt-xs">
            <q-item
              v-for="(c, i) in topCustomers"
              :key="c.customerId"
              class="customer-row q-py-sm"
            >
              <q-item-section avatar>
                <q-avatar
                  :color="rankColor(i)"
                  text-color="white"
                  size="34px"
                  class="text-weight-bold"
                  style="font-size:13px"
                >{{ i + 1 }}</q-avatar>
              </q-item-section>

              <q-item-section>
                <q-item-label class="text-weight-medium" style="font-size:13px">
                  {{ c.companyName }}
                </q-item-label>
                <q-item-label caption>
                  {{ c.orderCount }} {{ c.orderCount === 1 ? 'order' : 'orders' }}
                </q-item-label>
                <div class="revenue-bar-track q-mt-xs">
                  <div class="revenue-bar-fill" :style="{ width: revenueBarWidth(c.totalRevenue) }" />
                </div>
              </q-item-section>

              <q-item-section side style="min-width:80px">
                <span class="text-weight-bold text-primary" style="font-size:14px">
                  ${{ (c.totalRevenue / 1000).toFixed(1) }}k
                </span>
              </q-item-section>
            </q-item>
          </q-list>
        </q-card>
      </div>

      <!-- Recent Orders — col-3 -->
      <div class="col-12 col-md-3">
        <q-card bordered class="full-height">
          <q-card-section class="q-pb-none">
            <div class="text-subtitle1 text-weight-bold">Recent orders</div>
            <div class="text-caption text-grey-6">Last 5 created</div>
          </q-card-section>
          <q-separator class="q-mt-sm" />

          <div v-if="loadingRecent">
            <q-item v-for="i in 5" :key="i" class="q-py-sm">
              <q-item-section>
                <q-skeleton type="text" width="60%" />
                <q-skeleton type="text" width="40%" />
              </q-item-section>
              <q-item-section side><q-skeleton type="rect" width="56px" height="20px" /></q-item-section>
            </q-item>
          </div>

          <q-list v-else>
            <q-item
              v-for="order in recentOrders"
              :key="order.id"
              class="recent-row q-py-sm"
              clickable
              :to="{ name: 'order-detail', params: { id: order.id } }"
            >
              <q-item-section>
                <q-item-label class="text-weight-medium" style="font-size:12px">
                  <span class="text-grey-6">#{{ order.id }}</span>
                  &nbsp;{{ order.customerId }}
                </q-item-label>
                <q-item-label caption>
                  {{ order.shipCity }}, {{ order.shipCountry }}
                </q-item-label>
                <q-item-label caption class="text-weight-bold text-primary">
                  ${{ order.total.toLocaleString('en-US', { maximumFractionDigits: 0 }) }}
                </q-item-label>
              </q-item-section>
              <q-item-section side>
                <q-badge
                  :color="order.isShipped ? 'positive' : 'warning'"
                  :label="order.isShipped ? 'Shipped' : 'Pending'"
                  style="font-size:10px"
                />
              </q-item-section>
            </q-item>
          </q-list>
        </q-card>
      </div>

    </div>

    <!-- ── Verified Delivery Locations Map ────────────────────────── -->
    <transition name="fade">
      <div class="row q-col-gutter-md" v-if="deliveryLocations.length > 0 || loadingMap">
        <div class="col-12">
          <q-card bordered>
            <q-card-section class="row items-center q-pb-none">
              <div>
                <div class="text-subtitle1 text-weight-bold">Verified delivery locations</div>
                <div class="text-caption text-grey-6">
                  {{ deliveryLocations.length }}
                  {{ deliveryLocations.length === 1 ? 'address' : 'addresses' }}
                  geocoded and validated with Google Maps
                </div>
              </div>
              <q-space />
              <div class="row items-center q-gutter-sm">
                <q-badge color="positive" outline>
                  <q-icon name="verified" size="12px" class="q-mr-xs" />
                  Google Maps Verified
                </q-badge>
              </div>
            </q-card-section>
            <q-separator class="q-mt-sm" />

            <q-card-section v-if="loadingMap" class="q-pa-none">
              <q-skeleton type="rect" height="240px" style="border-radius:0" />
            </q-card-section>

            <template v-else-if="staticMapUrl">
              <!-- Map image -->
              <div class="map-image-wrapper">
                <img
                  :src="staticMapUrl"
                  alt="Verified delivery locations"
                  class="map-image"
                />
                <!-- Marker count overlay -->
                <q-badge class="map-overlay-badge" color="negative" text-color="white">
                  <q-icon name="location_on" size="14px" />
                  {{ deliveryLocations.length }} location{{ deliveryLocations.length === 1 ? '' : 's' }}
                </q-badge>
              </div>

              <!-- Location chips -->
              <q-card-section class="q-pt-md q-pb-md">
                <div class="row q-gutter-sm">
                  <q-chip
                    v-for="loc in deliveryLocations"
                    :key="loc.orderId"
                    dense
                    square
                    color="primary"
                    text-color="white"
                    class="location-chip"
                  >
                    <q-icon name="location_on" size="12px" class="q-mr-xs" />
                    <strong>#{{ loc.orderId }}</strong>
                    &nbsp;·&nbsp;{{ loc.city }}, {{ loc.country }}
                    <q-tooltip>{{ loc.placeType }} · Validated {{ formatValidatedAt(loc.validatedAt) }}</q-tooltip>
                  </q-chip>
                </div>
              </q-card-section>
            </template>

          </q-card>
        </div>
      </div>
    </transition>

  </q-page>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { api } from 'src/boot/axios'
import apexchart from 'vue3-apexcharts'

// ─── State ────────────────────────────────────────────────────────
const loading         = ref(false)
const loadingYears    = ref(false)
const loadingCustomers = ref(false)
const loadingRecent   = ref(false)
const loadingMap      = ref(false)

const selectedYear       = ref(null)
const availableYears     = ref([])
const ordersRaw          = ref([])
const regionsData        = ref([])
const topCustomers       = ref([])
const recentOrders       = ref([])
const deliveryLocations  = ref([])
const previousYearOrders = ref([])

// ─── Formatters ───────────────────────────────────────────────────
const currencyFmt = new Intl.NumberFormat('en-US', {
  style: 'currency', currency: 'USD', maximumFractionDigits: 0
})
const numberFmt = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 })
const avgFmt    = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 })
const months    = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']

function formatValidatedAt(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

// ─── Computed: chart pipeline ─────────────────────────────────────
const ordersChartData = computed(() =>
  ordersRaw.value
    .filter(o => o.month >= 1 && o.month <= 12)
    .sort((a, b) => a.month - b.month)
)

const ordersChartSeries = computed(() => [
  { name: 'Orders',  data: ordersChartData.value.map(o => o.orderCount) },
  { name: 'Revenue', data: ordersChartData.value.map(o => o.totalRevenue) },
])

const ordersChartOptions = computed(() => ({
  chart: {
    type: 'bar', height: 300, toolbar: { show: false },
    animations: { enabled: true, easing: 'easeinout', speed: 400 }
  },
  plotOptions: { bar: { borderRadius: 5, columnWidth: '52%' } },
  stroke: { show: false },
  dataLabels: { enabled: false },
  legend: { position: 'top', fontSize: '12px' },
  colors: ['#1F3A5F', '#5B8DB8'],
  xaxis: {
    categories: ordersChartData.value.map(o => months[o.month - 1]),
    labels: { rotate: 0, style: { fontSize: '12px' } },
    axisBorder: { show: false },
    axisTicks: { show: false }
  },
  yaxis: [
    {
      title: { text: 'Orders', style: { fontWeight: 500, fontSize: '11px' } },
      labels: { formatter: v => numberFmt.format(v), style: { fontSize: '11px' } }
    },
    {
      opposite: true,
      title: { text: 'Revenue', style: { fontWeight: 500, fontSize: '11px' } },
      labels: { formatter: v => currencyFmt.format(v), style: { fontSize: '11px' } }
    }
  ],
  grid: { borderColor: 'rgba(0,0,0,0.06)', strokeDashArray: 4 },
  tooltip: {
    shared: true, intersect: false,
    y: {
      formatter: (val, { seriesIndex }) =>
        seriesIndex === 0 ? numberFmt.format(val) : currencyFmt.format(val)
    }
  }
}))

const regionsChartSeries = computed(() => regionsData.value.map(r => r.orderCount))

const regionsChartOptions = computed(() => ({
  chart: {
    type: 'donut', height: 320, toolbar: { show: false },
    animations: { enabled: true, easing: 'easeinout', speed: 400 }
  },
  labels: regionsData.value.map(r => r.country),
  legend: { position: 'bottom', fontSize: '11px', itemMargin: { horizontal: 4 } },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: v => numberFmt.format(v) + ' orders' } },
  plotOptions: { pie: { donut: { size: '65%' } } },
  stroke: { width: 2 }
}))

// ─── Computed: KPIs ───────────────────────────────────────────────
const totalOrders  = computed(() => ordersChartData.value.reduce((s, o) => s + o.orderCount, 0))
const totalRevenue = computed(() => ordersChartData.value.reduce((s, o) => s + o.totalRevenue, 0))
const avgOrders    = computed(() =>
  ordersChartData.value.length > 0 ? totalOrders.value / ordersChartData.value.length : 0
)
const topCountry = computed(() =>
  regionsData.value.length > 0 ? regionsData.value[0].country : '—'
)
const topCountryOrders = computed(() =>
  regionsData.value.length > 0 ? numberFmt.format(regionsData.value[0].orderCount) : null
)

const totalOrdersDisplay  = computed(() => numberFmt.format(totalOrders.value))
const totalRevenueDisplay = computed(() => currencyFmt.format(totalRevenue.value))
const avgOrdersDisplay    = computed(() => avgFmt.format(avgOrders.value))
const topCountryDisplay   = computed(() => topCountry.value)

const previousTotal = computed(() =>
  previousYearOrders.value.reduce((s, o) => s + o.orderCount, 0)
)
const ordersTrend = computed(() => {
  if (!selectedYear.value || previousTotal.value === 0) return null
  return Math.round(((totalOrders.value - previousTotal.value) / previousTotal.value) * 100)
})

// ─── Computed: static map URL ─────────────────────────────────────
const staticMapUrl = computed(() => {
  if (deliveryLocations.value.length === 0) return null
  const key = import.meta.env.VITE_GOOGLE_MAPS_API_KEY
  if (!key) return null

  const markerColor = 'red'
  const markers = deliveryLocations.value
    .map(l => `markers=color:${markerColor}|${l.latitude},${l.longitude}`)
    .join('&')

  const base = 'https://maps.googleapis.com/maps/api/staticmap'
  const size = 'size=1200x400&scale=2&maptype=roadmap'

  if (deliveryLocations.value.length === 1) {
    const { latitude, longitude } = deliveryLocations.value[0]
    return `${base}?center=${latitude},${longitude}&zoom=14&${size}&${markers}&key=${key}`
  }

  // Multiple markers — let Google auto-fit the viewport
  return `${base}?${size}&${markers}&key=${key}`
})

// ─── Computed: top customers bar widths ───────────────────────────
const maxRevenue = computed(() =>
  topCustomers.value.length > 0
    ? Math.max(...topCustomers.value.map(c => c.totalRevenue))
    : 1
)
function revenueBarWidth(revenue) {
  return `${Math.round((revenue / maxRevenue.value) * 100)}%`
}
function rankColor(i) {
  return ['primary', 'secondary', 'teal', 'grey-6', 'grey-5'][i] ?? 'grey-5'
}

// ─── Fetchers ─────────────────────────────────────────────────────
async function fetchYears() {
  loadingYears.value = true
  try {
    const { data } = await api.get('/analytics/available-years')
    availableYears.value = data.sort((a, b) => b - a)
    if (data.length > 0) selectedYear.value = data[0]
  } catch (_) {
  } finally {
    loadingYears.value = false
  }
}

async function fetchMainData(year) {
  loading.value = true
  try {
    const params = year ? { year } : {}
    const [ordersRes, regionsRes] = await Promise.all([
      api.get('/analytics/orders-over-time', { params }),
      api.get('/analytics/shipments-by-region', { params }),
    ])
    ordersRaw.value  = ordersRes.data
    regionsData.value = regionsRes.data
  } catch (_) {
  } finally {
    loading.value = false
  }
}

async function fetchTopCustomers(year) {
  loadingCustomers.value = true
  try {
    const params = year ? { year, limit: 5 } : { limit: 5 }
    const { data } = await api.get('/analytics/top-customers', { params })
    topCustomers.value = data
  } catch (_) {
  } finally {
    loadingCustomers.value = false
  }
}

async function fetchPreviousYear(year) {
  if (!year) return
  try {
    const { data } = await api.get('/analytics/orders-over-time', { params: { year: year - 1 } })
    previousYearOrders.value = data
  } catch (_) {}
}

async function fetchRecentOrders() {
  loadingRecent.value = true
  try {
    const { data } = await api.get('/orders', { params: { page: 1, pageSize: 5 } })
    recentOrders.value = data.items
  } catch (_) {
  } finally {
    loadingRecent.value = false
  }
}

async function fetchDeliveryLocations() {
  loadingMap.value = true
  try {
    const { data } = await api.get('/analytics/delivery-locations', { params: { limit: 50 } })
    deliveryLocations.value = data
  } catch (_) {
  } finally {
    loadingMap.value = false
  }
}

// ─── Year watch ───────────────────────────────────────────────────
watch(selectedYear, year => {
  if (!year) return
  fetchMainData(year)
  fetchTopCustomers(year)
  fetchPreviousYear(year)
})

// ─── Init ─────────────────────────────────────────────────────────
onMounted(() => {
  fetchRecentOrders()     // not year-dependent
  fetchDeliveryLocations() // not year-dependent
  fetchYears()            // triggers watch → fetches the rest
})
</script>

<style scoped>

/* ── KPI cards ─────────────────────────────────────────────── */
.kpi-card {
  transition: box-shadow 0.2s ease, transform 0.15s ease;
  border-left-width: 3px !important;
  border-left-style: solid !important;
}
.kpi-card:hover {
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.09);
  transform: translateY(-1px);
}

.kpi-card--orders  { border-left-color: #1F3A5F !important; }
.kpi-card--revenue { border-left-color: #009688 !important; }
.kpi-card--country { border-left-color: #BF360C !important; }
.kpi-card--avg     { border-left-color: #2D7A3E !important; }

.kpi-label {
  font-size: 11px;
  font-weight: 500;
  color: var(--q-color-grey-7, #616161);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}
.kpi-value {
  font-size: 1.55rem;
  font-weight: 700;
  line-height: 1.2;
  letter-spacing: -0.025em;
  color: var(--q-color-dark, #1A1A2E);
}
.kpi-sub {
  font-size: 11px;
  color: var(--q-color-grey-6, #757575);
  margin-top: 2px;
}
.kpi-trend {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: 11px;
  font-weight: 600;
  margin-top: 2px;
}

/* ── Customer list ─────────────────────────────────────────── */
.customer-row {
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
  transition: background 0.12s ease;
}
.customer-row:last-child { border-bottom: none; }
.customer-row:hover { background: rgba(0, 0, 0, 0.018); }

.revenue-bar-track {
  height: 3px;
  background: rgba(0, 0, 0, 0.07);
  border-radius: 2px;
  overflow: hidden;
}
.revenue-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #1F3A5F 0%, #5B8DB8 100%);
  border-radius: 2px;
  transition: width 0.7s cubic-bezier(.4,0,.2,1);
}

/* ── Recent orders ─────────────────────────────────────────── */
.recent-row {
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
  transition: background 0.12s ease;
}
.recent-row:last-child { border-bottom: none; }
.recent-row:hover { background: rgba(0, 0, 0, 0.018); }

/* ── Delivery map ──────────────────────────────────────────── */
.map-image-wrapper {
  position: relative;
  overflow: hidden;
}
.map-image {
  width: 100%;
  height: 240px;
  object-fit: cover;
  display: block;
}
.map-overlay-badge {
  position: absolute;
  bottom: 10px;
  left: 12px;
  font-size: 11px;
  font-weight: 600;
  border-radius: 4px;
  display: flex;
  align-items: center;
  gap: 4px;
}
.location-chip {
  font-size: 11px;
  border-radius: 4px !important;
}

/* ── Fade transition ───────────────────────────────────────── */
.fade-enter-active, .fade-leave-active { transition: opacity 0.4s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

</style>