import { ref, computed, watch } from 'vue'
import { api } from 'src/boot/axios'

export function useAddressValidation(form) {
  const validatingAddress = ref(false)
  const geocodeResult = ref(null)
  const geocodeError = ref(null)

  const canValidateAddress = computed(() =>
    form.shipStreet && form.shipCity && form.shipCountry
  )

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

  async function validateAddress() {
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
        },
      })
      geocodeResult.value = data
    } catch (err) {
      geocodeError.value = err.response?.data?.error || 'Address validation failed'
    } finally {
      validatingAddress.value = false
    }
  }

  return { geocodeResult, geocodeError, validatingAddress, canValidateAddress, validateAddress }
}
