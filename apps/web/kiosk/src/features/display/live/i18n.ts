import type { Code, Row } from './logic.ts'

/**
 * The words on the displays, in English and Sinhala. Station names and the notices' own text are not translated: they come from the
 * system as written. A display shows both languages at once: English first, with the Sinhala in a smaller line under it.
 */
export type Lang = 'en' | 'si'

export interface Words {
  departures: string
  arrivals: string
  time: string
  destination: string
  from: string
  platformShort: string
  status: string
  platform: (n: string) => string
  callingLabel: string
  nextTrain: (to: string, time: string, platform: string | null) => string
  min: string
  now: string
  noMoreTrains: string
  noMoreTrainsHint: string
  noMoreDepartures: string
  noMoreArrivals: string
  serviceUpdates: string
  live: (time: string) => string
  updatedUnavailable: (time: string) => string
  noUpdateSince: (time: string) => string
  unavailableBanner: string
  tomorrow: string
  page: (page: number, of: number) => string
  statuses: Record<Exclude<Code, 'expected'>, string>
  expected: (time: string) => string
  connecting: { title: string; detail: string; footer: string }
  unavailable: { title: string; detail: string; footer: string }
  unknownStation: { title: string; detail: string; footer: string }
}

const en: Words = {
  departures: 'Departures',
  arrivals: 'Arrivals',
  time: 'Time',
  destination: 'Destination',
  from: 'From',
  platformShort: 'Plat',
  status: 'Status',
  platform: (n) => `Platform ${n}`,
  callingLabel: 'Calling at',
  nextTrain: (to, time, platform) => `Next train to ${to}: ${time}${platform ? ` from platform ${platform}` : ''}`,
  min: 'min',
  now: 'Now',
  noMoreTrains: 'No more trains today',
  noMoreTrainsHint: 'Check the departures board for the first train tomorrow.',
  noMoreDepartures: 'No more departures today',
  noMoreArrivals: 'No more arrivals today',
  serviceUpdates: 'Service updates',
  live: (time) => `Live · updated ${time}`,
  updatedUnavailable: (time) => `Updated ${time} · live information unavailable`,
  noUpdateSince: (time) => `No update since ${time}`,
  unavailableBanner: 'Live information unavailable. Listen for announcements.',
  tomorrow: 'Tomorrow',
  page: (page, of) => `${page} / ${of}`,
  statuses: {
    atPlatform: 'At platform',
    onTime: 'On time',
    cancelled: 'Cancelled',
    platformChanged: 'Platform changed',
    scheduled: 'Scheduled',
    departed: 'Departed',
    arrived: 'Arrived',
  },
  expected: (time) => `Expected ${time}`,
  connecting: { title: 'Connecting…', detail: 'Getting the latest train times.', footer: 'Starting up' },
  unavailable: {
    title: 'Live information unavailable',
    detail: 'Listen for announcements. This screen keeps trying.',
    footer: 'Waiting for information',
  },
  unknownStation: {
    title: 'This display is not set up',
    detail: 'The station in the address is not known. Ask a technician.',
    footer: 'Waiting for information',
  },
}

const si: Words = {
  departures: 'පිටත්වීම්',
  arrivals: 'පැමිණීම්',
  time: 'වේලාව',
  destination: 'ගමනාන්තය',
  from: 'සිට',
  platformShort: 'වේදි.',
  status: 'තත්ත්වය',
  platform: (n) => `වේදිකාව ${n}`,
  callingLabel: 'නතර වන ස්ථාන',
  nextTrain: (to, time, platform) => `${to} වෙත ඊළඟ දුම්රිය: ${time}${platform ? ` · වේදිකාව ${platform}` : ''}`,
  min: 'මිනි.',
  now: 'දැන්',
  noMoreTrains: 'අද තවත් දුම්රිය නැත',
  noMoreTrainsHint: 'හෙට පළමු දුම්රිය සඳහා පිටත්වීම් පුවරුව බලන්න.',
  noMoreDepartures: 'අද තවත් පිටත්වීම් නැත',
  noMoreArrivals: 'අද තවත් පැමිණීම් නැත',
  serviceUpdates: 'සේවා දැනුම්දීම්',
  live: (time) => `සජීවී · යාවත්කාලීන ${time}`,
  updatedUnavailable: (time) => `යාවත්කාලීන ${time} · සජීවී තොරතුරු නොමැත`,
  noUpdateSince: (time) => `${time} සිට යාවත්කාලීන කිරීමක් නැත`,
  unavailableBanner: 'සජීවී තොරතුරු නොමැත. නිවේදන සවන් දෙන්න.',
  tomorrow: 'හෙට',
  page: (page, of) => `${page} / ${of}`,
  statuses: {
    atPlatform: 'වේදිකාවේ ඇත',
    onTime: 'නියමිත වේලාවට',
    cancelled: 'අවලංගුයි',
    platformChanged: 'වේදිකාව වෙනස් විය',
    scheduled: 'නියමිත',
    departed: 'පිටත් විය',
    arrived: 'පැමිණියේය',
  },
  expected: (time) => `${time} ට අපේක්ෂිතයි`,
  connecting: { title: 'සම්බන්ධ වෙමින්…', detail: 'නවතම දුම්රිය වේලාවන් ලබා ගනිමින්.', footer: 'ආරම්භ වෙමින්' },
  unavailable: {
    title: 'සජීවී තොරතුරු නොමැත',
    detail: 'නිවේදන සවන් දෙන්න. මෙම තිරය නැවත උත්සාහ කරයි.',
    footer: 'තොරතුරු බලාපොරොත්තුවෙන්',
  },
  unknownStation: {
    title: 'මෙම තිරය සකසා නැත',
    detail: 'ලිපිනයේ ඇති දුම්රිය ස්ථානය හඳුනා නොගැනේ. තාක්ෂණික නිලධාරියෙකුගෙන් විමසන්න.',
    footer: 'තොරතුරු බලාපොරොත්තුවෙන්',
  },
}

export const words: Record<Lang, Words> = { en, si }

/** The status of a row in the language being shown. */
export function rowLabel(lang: Lang, row: Pick<Row, 'code' | 'at'>): string {
  const w = words[lang]
  return row.code === 'expected' ? w.expected(row.at ?? '') : w.statuses[row.code]
}
