import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'

export interface Station {
  code: string
  name: string
  facilities: string[]
}

export const stationKeys = { all: ['stations'] as const }

/** The station list is reference data: public, small and rarely changes, so it is cached for ten minutes. */
export function useStations() {
  return useQuery({
    queryKey: stationKeys.all,
    queryFn: async () => (await api.get<{ items: Station[] }>('/network/stations')).items,
    staleTime: 10 * 60_000,
  })
}

/** code to name, for showing "Mill Lane" where the API sends "MIL". */
export function useStationNames(): (code: string) => string {
  const { data } = useStations()
  const names = new Map((data ?? []).map((station) => [station.code, station.name]))
  return (code) => names.get(code) ?? code
}
