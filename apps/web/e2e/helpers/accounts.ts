import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

/**
 * The seeded accounts come from the Keycloak realm file, so a password is never written twice. These are development
 * accounts only (see apps/web/console/README.md).
 */
const realmFile = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../../infra/keycloak/smartrail-realm.json')

interface RealmUser {
  username: string
  email?: string
  credentials?: { value: string }[]
}

const users: RealmUser[] = JSON.parse(readFileSync(realmFile, 'utf-8').replace(/^﻿/, '')).users

export type Persona = 'admin' | 'controller' | 'controller2' | 'scheduler' | 'scheduler2' | 'driver' | 'passenger'

export function account(persona: Persona): { email: string; password: string } {
  const user = users.find((u) => u.username === persona)
  const password = user?.credentials?.[0]?.value
  if (!user?.email || !password) throw new Error(`No seeded account for "${persona}" in ${realmFile}`)
  return { email: user.email, password }
}
