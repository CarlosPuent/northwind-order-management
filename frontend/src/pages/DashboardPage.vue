<template>
  <q-page padding>
    <div class="q-mb-md">
      <div class="row items-center q-col-gutter-md">
        <div class="col">
          <div class="text-h4 text-primary text-weight-medium">Dashboard</div>
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
          >
            <template v-slot:loading>
              <q-spinner color="primary" />
            </template>
          </q-select>
        </div>
      </div>
    </div>

    <div class="row q-col-gutter-md q-mb-md">
      <div class="col-12 col-sm-6 col-md-3">
        <q-card bordered class="full-height shadow-1">
          <q-card-section v-if="loadingOrders" class="row items-center no-wrap">
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
        <q-card bordered class="full-height shadow-1">
          <q-card-section v-if="loadingOrders" class="row items-center no-wrap">
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
        <q-card bordered class="full-height shadow-1">
          <q-card-section v-if="loadingRegions" class="row items-center no-wrap">
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
        <q-card bordered class="full-height shadow-1">
          <q-card-section v-if="loadingOrders" class="row items-center no-wrap">
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

    <div class="row q-col-gutter-md">
      <div class="col-12 col-md-8">
        <q-card bordered class="full-height shadow-1">
          <q-card-section class="row items-center">
            <div class="text-subtitle1 text-weight-bold">Orders Over Time</div>
            <q-space />
            <div class="text-caption text-grey-7">
              {{ selectedYearLabel }}
            </div>
          </q-card-section>
          <q-separator />
          
          <q-card-section v-if="loadingOrders">
            <q-skeleton type="rect" height="320px" />
          </q-card-section>
          
          <q-card-section v-else-if="chartOrdersData.length === 0" class="flex flex-center empty-state">
            <div class="text-grey-5 text-center">
              <q-icon name="analytics" size="48px" class="q-mb-sm opacity-50" />
              <div class="text-subtitle2">No activity data available for this period.</div>
            </div>
          </q-card-section>
          
          <q-card-section v-else>
            <apexchart
              type="bar"
              height="320"
              :options="ordersChartOptions"
              :series="ordersChartSeries"
            />
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-md-4">
        <q-card bordered class="full-height shadow-1">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold">Shipments by Country</div>
            <div class="text-caption text-grey-7">Top 10 distributions</div>
          </q-card-section>
          <q-separator />
          
          <q-card-section v-if="loadingRegions">
            <q-skeleton type="rect" height="320px" />
          </q-card-section>
          
          <q-card-section v-else-if="topRegions.length === 0" class="flex flex-center empty-state">
            <div class="text-grey-5 text-center">
              <q-icon name="public_off" size="48px" class="q-mb-sm opacity-50" />
              <div class="text-subtitle2">No regional data available.</div>
            </div>
          </q-card-section>
          
          <q-card-section v-else>
            <apexchart
              type="donut"
              height="320"
              :options="regionsChartOptions"
              :series="regionsChartSeries"
            />
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup>
import { computed, onMounted, onBeforeUnmount, ref, watch } from "vue";
import { watchDebounced } from "@vueuse/core";
import { getCssVar } from "quasar";
import { api } from "boot/axios";
import apexchart from "vue3-apexcharts";

// --- TELEMETRY & LOGGING ---
const logger = {
  error: (context, error) => {
    if (process.env.NODE_ENV !== "production") {
      console.error(`[${context}]`, error);
    } else {
      // Integración para servicios de telemetría (e.g., Sentry, Datadog)
    }
  }
};

// --- TENANT CONTEXT (SaaS Architecture) ---
// En un entorno de producción, estos valores deben ser provistos por 
// un gestor de estado global (e.g., const { locale, currencyCode } = useTenantStore())
const tenantConfig = {
  locale: "en-US",
  currencyCode: "USD" 
};

// --- STATE MANAGEMENT ---
const loadingYears = ref(false);
const loadingOrders = ref(false);
const loadingRegions = ref(false);

const selectedYear = ref(null);
const availableYears = ref([]);
const ordersData = ref([]);
const regionData = ref([]);
const chartColors = ref(['#1976D2', '#26A69A']); 

// --- CACHE & NETWORK CONTROL ---
const ordersCache = new Map();
const MAX_CACHE_SIZE = 5;
let abortController = null;
const STORAGE_KEY_YEAR = "dashboard-selected-year";

function updateOrdersCache(year, payload) {
  if (ordersCache.size >= MAX_CACHE_SIZE && !ordersCache.has(year)) {
    const oldestKey = ordersCache.keys().next().value;
    ordersCache.delete(oldestKey);
  }
  ordersCache.set(year, payload);
}

// --- FORMATTERS & UTILS ---
const monthLabels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

const currencyFormatter = new Intl.NumberFormat(tenantConfig.locale, { 
  style: "currency", 
  currency: tenantConfig.currencyCode, 
  maximumFractionDigits: 2 
});

const numberFormatter = new Intl.NumberFormat(tenantConfig.locale, { 
  maximumFractionDigits: 0 
});

const avgNumberFormatter = new Intl.NumberFormat(tenantConfig.locale, { 
  maximumFractionDigits: 1 
});

function toMonthLabel(monthNumber) {
  if (typeof monthNumber !== "number" || monthNumber < 1 || monthNumber > 12) return "";
  return monthLabels[monthNumber - 1];
}

// --- DATA PIPELINE ---
const validOrders = computed(() => {
  const raw = Array.isArray(ordersData.value) ? ordersData.value : [];
  return raw
    .map(item => ({
      month: Number(item?.month),
      orderCount: Number(item?.orderCount) || 0,
      totalRevenue: Number(item?.totalRevenue) || 0,
    }))
    .filter(item => Number.isFinite(item.month) && item.month >= 1 && item.month <= 12);
});

const aggregatedOrders = computed(() => {
  const byMonth = new Map();
  for (const item of validOrders.value) {
    const current = byMonth.get(item.month) || { orderCount: 0, totalRevenue: 0 };
    byMonth.set(item.month, {
      orderCount: current.orderCount + item.orderCount,
      totalRevenue: current.totalRevenue + item.totalRevenue,
    });
  }
  
  return Array.from(byMonth.entries())
    .sort(([monthA], [monthB]) => monthA - monthB)
    .map(([month, data]) => ({ month, ...data }));
});

const topRegions = computed(() => {
  const raw = Array.isArray(regionData.value) ? regionData.value : [];
  return raw
    .map(r => ({
      country: String(r?.country ?? "").trim(),
      orderCount: Number(r?.orderCount) || 0,
    }))
    .filter(r => r.country.length > 0)
    .sort((a, b) => b.orderCount - a.orderCount)
    .slice(0, 10);
});

// --- MICRO-OPTIMIZED CHART DATA EXTRACTIONS ---
const chartCategories = computed(() => aggregatedOrders.value.map(x => toMonthLabel(x.month)));
const chartOrdersData = computed(() => aggregatedOrders.value.map(x => x.orderCount));
const chartRevenueData = computed(() => aggregatedOrders.value.map(x => x.totalRevenue));
const monthCount = computed(() => aggregatedOrders.value.length);

const regionLabels = computed(() => topRegions.value.map(x => x.country));
const regionSeriesData = computed(() => topRegions.value.map(x => x.orderCount));

// --- KPI AGGREGATIONS ---
const totalOrders = computed(() => chartOrdersData.value.reduce((sum, v) => sum + v, 0));
const totalRevenue = computed(() => chartRevenueData.value.reduce((sum, v) => sum + v, 0));
const avgOrders = computed(() => (monthCount.value ? totalOrders.value / monthCount.value : 0));
const topCountry = computed(() => (topRegions.value.length > 0 ? topRegions.value[0].country : "—"));

const totalOrdersDisplay = computed(() => numberFormatter.format(totalOrders.value));
const totalRevenueDisplay = computed(() => currencyFormatter.format(totalRevenue.value));
const topCountryDisplay = computed(() => topCountry.value);
const avgOrdersDisplay = computed(() => avgNumberFormatter.format(avgOrders.value));
const selectedYearLabel = computed(() => (selectedYear.value ? `Year ${selectedYear.value}` : ""));

// --- CHART OPTIMIZATION: STATE MUTATION CONTROL ---
const baseOrdersChartConfig = {
  chart: { type: "bar", height: 320, toolbar: { show: false }, animations: { enabled: true, easing: "easeinout", speed: 450 } },
  plotOptions: { bar: { borderRadius: 6, columnWidth: "55%" } },
  stroke: { show: false },
  dataLabels: { enabled: false },
  legend: { position: "top" },
  yaxis: [
    { title: { text: "Orders", style: { fontWeight: 500 } }, labels: { formatter: (val) => numberFormatter.format(Number(val) || 0) } },
    { opposite: true, title: { text: "Revenue", style: { fontWeight: 500 } }, labels: { formatter: (val) => currencyFormatter.format(Number(val) || 0) } },
  ],
  tooltip: {
  shared: true,
  intersect: false, 
  y: {
    formatter: (val, { seriesIndex }) => {
      const numeric = Number(val) || 0;
      return seriesIndex === 0
        ? numberFormatter.format(numeric)
        : currencyFormatter.format(numeric);
    }
  }
},
};

const baseRegionsChartConfig = {
  chart: { type: "donut", height: 320, toolbar: { show: false }, animations: { enabled: true, easing: "easeinout", speed: 450 } },
  legend: { position: "bottom", fontSize: "12px" },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: (val) => numberFormatter.format(Number(val) || 0) } },
  plotOptions: { pie: { donut: { size: "68%" } } },
};

const ordersChartSeries = computed(() => [
  { name: "Orders", data: chartOrdersData.value },
  { name: "Revenue", data: chartRevenueData.value },
]);
const regionsChartSeries = computed(() => regionSeriesData.value);

const ordersChartOptions = ref({
  ...baseOrdersChartConfig,
  colors: chartColors.value,
  xaxis: { categories: [], labels: { rotate: 0 } },
});

const regionsChartOptions = ref({
  ...baseRegionsChartConfig,
  labels: [],
});

watch(chartCategories, (newCategories) => {
  ordersChartOptions.value.xaxis.categories = newCategories;
});

watch(regionLabels, (newLabels) => {
  regionsChartOptions.value.labels = newLabels;
});

// --- DOMAIN LOGIC & API INTEGRATION ---
async function fetchYears() {
  loadingYears.value = true;
  try {
    const { data } = await api.get("/analytics/available-years");
    const years = Array.isArray(data) ? data.map(Number).filter(Number.isFinite).sort((a, b) => b - a) : [];
    availableYears.value = years;
    
    if (years.length > 0) {
      const persistedYear = Number(localStorage.getItem(STORAGE_KEY_YEAR));
      return years.includes(persistedYear) ? persistedYear : years[0];
    }
  } catch (error) {
    logger.error("Dashboard:fetchYears", error);
  } finally {
    loadingYears.value = false;
  }
  return null;
}

async function fetchOrders(year) {
  if (!year) return;

  if (ordersCache.has(year)) {
    ordersData.value = ordersCache.get(year);
    return;
  }

  if (abortController) abortController.abort();
  abortController = new AbortController();

  loadingOrders.value = true;
  try {
    const { data } = await api.get("/analytics/orders-over-time", {
      params: { year },
      signal: abortController.signal,
    });
    
    const payload = Array.isArray(data) ? data : [];
    updateOrdersCache(year, payload);
    ordersData.value = payload;
  } catch (error) {
    if (error.name !== "CanceledError" && error.message !== "canceled") {
      logger.error(`Dashboard:fetchOrders:${year}`, error);
    }
  } finally {
    loadingOrders.value = false;
  }
}

async function fetchRegions() {
  loadingRegions.value = true;
  try {
    const { data } = await api.get("/analytics/shipments-by-region");
    regionData.value = Array.isArray(data) ? data : [];
  } catch (error) {
    logger.error("Dashboard:fetchRegions", error);
  } finally {
    loadingRegions.value = false;
  }
}

// --- EVENT ORCHESTRATION ---
watchDebounced(
  selectedYear,
  (year) => {
    if (year) {
      localStorage.setItem(STORAGE_KEY_YEAR, year.toString());
      fetchOrders(year);
    }
  },
  { debounce: 150 }
);

// --- LIFECYCLE HOOKS ---
onMounted(async () => {
  const primary = getCssVar("primary");
  const secondary = getCssVar("secondary");
  if (primary && secondary) {
    chartColors.value = [primary, secondary];
    ordersChartOptions.value.colors = [primary, secondary];
  }

  fetchRegions(); 
  const initialYear = await fetchYears();
  
  if (initialYear) {
  
    selectedYear.value = initialYear;
  }
});

onBeforeUnmount(() => {
  if (abortController) abortController.abort();
});
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

.opacity-50 {
  opacity: 0.5;
}
</style>