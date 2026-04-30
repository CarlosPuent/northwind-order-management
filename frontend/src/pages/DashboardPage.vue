<template>
  <q-page padding>
    <!-- Header -->
    <div class="q-mb-md">
      <div class="row items-center q-col-gutter-md">
        <div class="col">
          <div class="text-h4 text-primary text-weight-bold">Dashboard</div>
          <div class="text-caption text-grey-7">Order analytics and shipment insights</div>
        </div>
        <div class="col-auto">
          <q-select
            v-model="selectedYear"
            :options="availableYears"
            :disable="loadingYears || availableYears.length === 0"
            dense
            outlined
            label="Year"
            style="min-width: 140px"
          />
        </div>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="row q-col-gutter-md q-mb-md">
      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="full-height">
          <q-card-section v-if="loading" class="row items-center no-wrap">
            <q-skeleton type="QAvatar" size="42px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="60%" />
              <q-skeleton type="text" width="40%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap">
            <q-avatar color="primary" text-color="white" size="42px">
              <q-icon name="shopping_cart" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="text-caption text-grey-7 text-weight-medium">Total Orders</div>
              <div class="kpi-value">{{ totalOrdersDisplay }}</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="full-height">
          <q-card-section v-if="loading" class="row items-center no-wrap">
            <q-skeleton type="QAvatar" size="42px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="60%" />
              <q-skeleton type="text" width="40%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap">
            <q-avatar color="secondary" text-color="white" size="42px">
              <q-icon name="attach_money" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="text-caption text-grey-7 text-weight-medium">Total Revenue</div>
              <div class="kpi-value">{{ totalRevenueDisplay }}</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="full-height">
          <q-card-section v-if="loading" class="row items-center no-wrap">
            <q-skeleton type="QAvatar" size="42px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="60%" />
              <q-skeleton type="text" width="40%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap">
            <q-avatar color="accent" text-color="white" size="42px">
              <q-icon name="public" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="text-caption text-grey-7 text-weight-medium">Top Country</div>
              <div class="kpi-value">{{ topCountryDisplay }}</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="full-height">
          <q-card-section v-if="loading" class="row items-center no-wrap">
            <q-skeleton type="QAvatar" size="42px" />
            <div class="q-ml-md full-width">
              <q-skeleton type="text" width="60%" />
              <q-skeleton type="text" width="40%" />
            </div>
          </q-card-section>
          <q-card-section v-else class="row items-center no-wrap">
            <q-avatar color="positive" text-color="white" size="42px">
              <q-icon name="calendar_month" />
            </q-avatar>
            <div class="q-ml-md">
              <div class="text-caption text-grey-7 text-weight-medium">Avg Orders / Month</div>
              <div class="kpi-value">{{ avgOrdersDisplay }}</div>
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- Charts -->
    <div class="row q-col-gutter-md">
      <div class="col-12 col-md-8">
        <q-card bordered class="full-height">
          <q-card-section class="row items-center">
            <div class="text-subtitle1 text-weight-bold">Orders over time</div>
            <q-space />
            <div class="text-caption text-grey-7">{{ selectedYear ? `Year ${selectedYear}` : 'All years' }}</div>
          </q-card-section>
          <q-separator />

          <q-card-section v-if="loading">
            <q-skeleton type="rect" height="320px" />
          </q-card-section>

          <q-card-section v-else-if="ordersChartData.length === 0" class="flex flex-center empty-state">
            <div class="text-grey-5 text-center">
              <q-icon name="analytics" size="48px" class="q-mb-sm" style="opacity: 0.5" />
              <div class="text-subtitle2">No data available for this period</div>
            </div>
          </q-card-section>

          <q-card-section v-else>
            <apexchart type="bar" height="320" :options="ordersChartOptions" :series="ordersChartSeries" />
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-md-4">
        <q-card bordered class="full-height">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold">Shipments by country</div>
            <div class="text-caption text-grey-7">{{ selectedYear ? `Year ${selectedYear}` : 'All years' }} · Top 10</div>
          </q-card-section>
          <q-separator />

          <q-card-section v-if="loading">
            <q-skeleton type="rect" height="320px" />
          </q-card-section>

          <q-card-section v-else-if="regionsData.length === 0" class="flex flex-center empty-state">
            <div class="text-grey-5 text-center">
              <q-icon name="public_off" size="48px" class="q-mb-sm" style="opacity: 0.5" />
              <div class="text-subtitle2">No regional data available</div>
            </div>
          </q-card-section>

          <q-card-section v-else>
            <apexchart type="donut" height="320" :options="regionsChartOptions" :series="regionsChartSeries" />
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { api } from 'src/boot/axios'
import apexchart from 'vue3-apexcharts'

// ---- State ----
const loading = ref(false)
const loadingYears = ref(false)
const selectedYear = ref(null)
const availableYears = ref([])
const ordersRaw = ref([])
const regionsData = ref([])

// ---- Formatters ----
const currencyFmt = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
const numberFmt = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 })
const avgFmt = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 })
const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

// ---- Computed: Orders chart data ----
const ordersChartData = computed(() =>
  ordersRaw.value
    .filter(o => o.month >= 1 && o.month <= 12)
    .sort((a, b) => a.month - b.month)
)

const ordersChartSeries = computed(() => [
  { name: 'Orders', data: ordersChartData.value.map(o => o.orderCount) },
  { name: 'Revenue', data: ordersChartData.value.map(o => o.totalRevenue) },
])

const ordersChartOptions = computed(() => ({
  chart: { type: 'bar', height: 320, toolbar: { show: false }, animations: { enabled: true, easing: 'easeinout', speed: 400 } },
  plotOptions: { bar: { borderRadius: 6, columnWidth: '55%' } },
  stroke: { show: false },
  dataLabels: { enabled: false },
  legend: { position: 'top' },
  colors: ['#1F3A5F', '#3D5A80'],
  xaxis: {
    categories: ordersChartData.value.map(o => months[o.month - 1]),
    labels: { rotate: 0 }
  },
  yaxis: [
    { title: { text: 'Orders', style: { fontWeight: 500 } }, labels: { formatter: v => numberFmt.format(v) } },
    { opposite: true, title: { text: 'Revenue', style: { fontWeight: 500 } }, labels: { formatter: v => currencyFmt.format(v) } },
  ],
  tooltip: {
    shared: true,
    intersect: false,
    y: {
      formatter: (val, { seriesIndex }) =>
        seriesIndex === 0 ? numberFmt.format(val) : currencyFmt.format(val)
    }
  },
}))

// ---- Computed: Regions chart data ----
const regionsChartSeries = computed(() => regionsData.value.map(r => r.orderCount))

const regionsChartOptions = computed(() => ({
  chart: { type: 'donut', height: 320, toolbar: { show: false }, animations: { enabled: true, easing: 'easeinout', speed: 400 } },
  labels: regionsData.value.map(r => r.country),
  legend: { position: 'bottom', fontSize: '12px' },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: v => numberFmt.format(v) + ' orders' } },
  plotOptions: { pie: { donut: { size: '68%' } } },
}))

// ---- Computed: KPIs (derived from the SAME filtered data) ----
const totalOrders = computed(() => ordersChartData.value.reduce((s, o) => s + o.orderCount, 0))
const totalRevenue = computed(() => ordersChartData.value.reduce((s, o) => s + o.totalRevenue, 0))
const avgOrders = computed(() => ordersChartData.value.length > 0 ? totalOrders.value / ordersChartData.value.length : 0)
const topCountry = computed(() => regionsData.value.length > 0 ? regionsData.value[0].country : '—')

const totalOrdersDisplay = computed(() => numberFmt.format(totalOrders.value))
const totalRevenueDisplay = computed(() => currencyFmt.format(totalRevenue.value))
const avgOrdersDisplay = computed(() => avgFmt.format(avgOrders.value))
const topCountryDisplay = computed(() => topCountry.value)

// ---- Data fetching ----
async function fetchYears () {
  loadingYears.value = true
  try {
    const { data } = await api.get('/analytics/available-years')
    availableYears.value = data.sort((a, b) => b - a)
    if (data.length > 0) {
      selectedYear.value = data[0]
    }
  } catch (err) {
    // Handled by interceptor
  } finally {
    loadingYears.value = false
  }
}

async function fetchDashboardData (year) {
  loading.value = true
  try {
    const params = year ? { year } : {}
    const [ordersRes, regionsRes] = await Promise.all([
      api.get('/analytics/orders-over-time', { params }),
      api.get('/analytics/shipments-by-region', { params }),
    ])
    ordersRaw.value = ordersRes.data
    regionsData.value = regionsRes.data
  } catch (err) {
    // Handled by interceptor
  } finally {
    loading.value = false
  }
}

// ---- Year change triggers both endpoints ----
watch(selectedYear, (year) => {
  if (year) {
    fetchDashboardData(year)
  }
})

// ---- Init ----
onMounted(() => {
  fetchYears()
})
</script>

<style scoped>
.kpi-value {
  font-size: 1.5rem;
  font-weight: 700;
  line-height: 1.2;
  letter-spacing: -0.02em;
}

.empty-state {
  min-height: 320px;
  background-color: rgba(0, 0, 0, 0.015);
  border-radius: 6px;
  border: 1px dashed rgba(0, 0, 0, 0.1);
}
</style>