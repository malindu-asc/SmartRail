import Keycloak from 'keycloak-js'
import { AUTH_CONFIG } from '@/shared/api'

let instance: Keycloak | null = null

/** One Keycloak client for the page. Sign-in uses the authorization-code flow with PKCE. */
export function getKeycloak(): Keycloak {
  instance ??= new Keycloak({ url: AUTH_CONFIG.url, realm: AUTH_CONFIG.realm, clientId: AUTH_CONFIG.clientId })
  return instance
}

let started: Promise<boolean> | null = null

/** Starts sign-in once (React strict mode runs effects twice) and resolves when the person is signed in. */
export function startSignIn(): Promise<boolean> {
  started ??= getKeycloak().init({ onLoad: 'login-required', pkceMethod: 'S256', checkLoginIframe: false })
  return started
}

/** A token that is valid for at least 30 more seconds, refreshed if needed. */
export async function currentToken(): Promise<string | undefined> {
  const keycloak = getKeycloak()
  try {
    await keycloak.updateToken(30)
  } catch {
    await keycloak.login()
  }
  return keycloak.token
}

export function signOutOfKeycloak(): void {
  void getKeycloak().logout({ redirectUri: window.location.origin })
}
