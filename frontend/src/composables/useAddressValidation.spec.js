import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { reactive } from 'vue'
import { useAddressValidation } from './useAddressValidation'
import { api } from 'src/boot/axios'

vi.mock('src/boot/axios', () => ({
  api: { get: vi.fn() },
}))

describe('useAddressValidation', () => {
  let form

  beforeEach(() => {
    vi.useFakeTimers()
    vi.clearAllMocks()
    form = reactive({
      shipStreet: '',
      shipCity: '',
      shipCountry: '',
      shipRegion: '',
      shipPostalCode: '',
    })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('canValidateAddress is false when shipStreet is empty', () => {
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { canValidateAddress } = useAddressValidation(form)
    expect(canValidateAddress.value).toBeFalsy()
  })

  it('canValidateAddress is false when shipCity is empty', () => {
    form.shipStreet = '123 Main St'
    form.shipCountry = 'UK'
    const { canValidateAddress } = useAddressValidation(form)
    expect(canValidateAddress.value).toBeFalsy()
  })

  it('canValidateAddress is false when shipCountry is empty', () => {
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    const { canValidateAddress } = useAddressValidation(form)
    expect(canValidateAddress.value).toBeFalsy()
  })

  it('canValidateAddress is true when all three fields are filled', () => {
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { canValidateAddress } = useAddressValidation(form)
    expect(canValidateAddress.value).toBeTruthy()
  })

  it('validateAddress calls GET /geocoding/validate with correct params', async () => {
    api.get.mockResolvedValue({ data: { standardizedAddress: 'test', latitude: 0, longitude: 0, placeType: 'premise' } })
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { validateAddress } = useAddressValidation(form)
    await validateAddress()
    expect(api.get).toHaveBeenCalledWith('/geocoding/validate', {
      params: {
        street: '123 Main St',
        city: 'London',
        region: undefined,
        postalCode: undefined,
        country: 'UK',
      },
    })
  })

  it('validateAddress sets geocodeResult to API response data on success', async () => {
    const mockData = { standardizedAddress: '123 Main St, London', latitude: 51.5, longitude: -0.1, placeType: 'premise' }
    api.get.mockResolvedValue({ data: mockData })
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { validateAddress, geocodeResult } = useAddressValidation(form)
    await validateAddress()
    expect(geocodeResult.value).toEqual(mockData)
  })

  it('validateAddress sets geocodeError from err.response.data.error on API error', async () => {
    api.get.mockRejectedValue({ response: { data: { error: 'Address not found' } } })
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { validateAddress, geocodeError } = useAddressValidation(form)
    await validateAddress()
    expect(geocodeError.value).toBe('Address not found')
  })

  it('validateAddress sets fallback "Address validation failed" when error has no server detail', async () => {
    api.get.mockRejectedValue({})
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { validateAddress, geocodeError } = useAddressValidation(form)
    await validateAddress()
    expect(geocodeError.value).toBe('Address validation failed')
  })

  it('validateAddress clears previous geocodeResult before each new call', async () => {
    const mockData = { standardizedAddress: '123 Main St', latitude: 51.5, longitude: -0.1, placeType: 'premise' }
    api.get
      .mockResolvedValueOnce({ data: mockData })
      .mockRejectedValueOnce({})
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { validateAddress, geocodeResult } = useAddressValidation(form)
    await validateAddress()
    expect(geocodeResult.value).toEqual(mockData)
    await validateAddress()
    expect(geocodeResult.value).toBeNull()
  })

  it('validateAddress sets validatingAddress to true during the call and false after', async () => {
    let resolveCall
    api.get.mockReturnValue(new Promise(resolve => { resolveCall = resolve }))
    form.shipStreet = '123 Main St'
    form.shipCity = 'London'
    form.shipCountry = 'UK'
    const { validateAddress, validatingAddress } = useAddressValidation(form)
    const callPromise = validateAddress()
    expect(validatingAddress.value).toBe(true)
    resolveCall({ data: { standardizedAddress: 'test', latitude: 0, longitude: 0, placeType: 'premise' } })
    await callPromise
    expect(validatingAddress.value).toBe(false)
  })

  it('canValidateAddress remains false when only one or two fields are filled', () => {
    const { canValidateAddress } = useAddressValidation(form)
    form.shipStreet = '123 Main St'
    expect(canValidateAddress.value).toBeFalsy()
    form.shipCity = 'London'
    expect(canValidateAddress.value).toBeFalsy()
    form.shipCountry = 'UK'
    expect(canValidateAddress.value).toBeTruthy()
  })
})
