import { useAuth } from '~/apis/api'
import type { ApiResponse } from '~/apis/api'

interface CredentialCreateOptionsResponse {
  challenge: string
  rpId: string
  rpName: string
  userId: string
  userName: string
  userDisplayName: string
  timeout: number
  attestation: string
  pubKeyCredParams: string[]
  authenticatorSelection: string
}

interface AssertionOptionsResponse {
  challenge: string
  timeout: number
  rpId: string
  allowCredentials: Array<{
    type: string
    id: string
  }> | null
  userVerification: string
}

interface AuthResponse {
  success: boolean
  message: string
  accessToken: string
  refreshToken: string
}

export function usePasskey() {
  const $api = useNuxtApp().$backendApi
  const authStore = useAuth()

  // Base64URL encoding/decoding utilities
  function base64urlToBuffer(base64url: string): ArrayBuffer {
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/')
    const padLen = (4 - (base64.length % 4)) % 4
    const padded = base64.padEnd(base64.length + padLen, '=')
    const binary = atob(padded)
    const bytes = new Uint8Array(binary.length)
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i) & 0xff
    }
    return bytes.buffer
  }

  // function bufferToBase64url(buffer: ArrayBuffer): string {
  //   const bytes = new Uint8Array(buffer)
  //   let binary = ''
  //   for (let i = 0; i < bytes.length; i++) {
  //     binary += String.fromCharCode(bytes[i]!)
  //   }
  //   return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '')
  // }

  function bufferToBase64(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer)
    let binary = ''
    for (let i = 0; i < bytes.length; i++) {
      binary += String.fromCharCode(bytes[i]!)
    }
    return btoa(binary)
  }

  // Register passkey
  async function registerPasskey(email: string, displayName?: string) {
    try {
      // Step 1: Get challenge from backend
      const beginResponse = await $api<ApiResponse<CredentialCreateOptionsResponse>>('/auth/passkey/register/begin', {
        method: 'POST',
        body: { email, displayName }
      })

      if (!beginResponse.data) {
        throw new Error(beginResponse.message || 'Failed to start registration')
      }

      const options = beginResponse.data

      // Map pubKeyCredParams from string array to proper format
      const pubKeyCredParams: PublicKeyCredentialParameters[] = options.pubKeyCredParams.map((alg) => {
        const algMap: { [key: string]: number } = {
          ES256: -7,
          RS256: -257
        }
        return {
          type: 'public-key' as const,
          alg: algMap[alg] || -7
        }
      })

      // Convert userId string to ArrayBuffer
      const userIdBuffer = new TextEncoder().encode(options.userId)

      // Step 2: Create credential with WebAuthn
      const credential = await navigator.credentials.create({
        publicKey: {
          challenge: base64urlToBuffer(options.challenge),
          rp: {
            id: options.rpId,
            name: options.rpName
          },
          user: {
            id: userIdBuffer,
            name: options.userName,
            displayName: options.userDisplayName
          },
          pubKeyCredParams,
          timeout: options.timeout,
          attestation: options.attestation as AttestationConveyancePreference,
          authenticatorSelection: {
            requireResidentKey: false,
            residentKey: 'preferred' as ResidentKeyRequirement,
            userVerification: 'preferred' as UserVerificationRequirement
          }
        }
      }) as PublicKeyCredential

      if (!credential) {
        throw new Error('Failed to create credential')
      }

      const response = credential.response as AuthenticatorAttestationResponse

      // Step 3: Send credential to backend
      const completeResponse = await $api<AuthResponse>('/auth/passkey/register/complete', {
        method: 'POST',
        body: {
          email,
          id: credential.id,
          rawId: bufferToBase64(credential.rawId),
          type: credential.type,
          response: {
            attestationObject: bufferToBase64(response.attestationObject),
            clientDataJSON: bufferToBase64(response.clientDataJSON)
          }
        }
      })

      if (!completeResponse.success) {
        throw new Error(completeResponse.message || 'Registration failed')
      }

      // Store tokens
      authStore.setToken(completeResponse.accessToken)
      authStore.setRefreshToken(completeResponse.refreshToken)

      return { success: true, message: completeResponse.message }
    } catch (error: unknown) {
      console.error('Passkey registration error:', error)

      // Handle user cancellation
      if (error && typeof error === 'object' && 'name' in error && error.name === 'NotAllowedError') {
        throw new Error('Registration cancelled')
      }

      const errorMessage = error && typeof error === 'object' && 'message' in error ? String(error.message) : 'Failed to register passkey'
      throw new Error(errorMessage)
    }
  }

  // Login with passkey
  async function loginWithPasskey(email?: string) {
    try {
      // Step 1: Get challenge from backend
      const beginResponse = await $api<ApiResponse<AssertionOptionsResponse>>('/auth/passkey/login/begin', {
        method: 'POST',
        body: email ? { email } : {}
      })

      if (!beginResponse.data) {
        throw new Error(beginResponse.message || 'Failed to start login')
      }

      const options = beginResponse.data

      // Step 2: Get credential with WebAuthn
      const credential = await navigator.credentials.get({
        publicKey: {
          challenge: base64urlToBuffer(options.challenge),
          timeout: options.timeout,
          rpId: options.rpId,
          allowCredentials: options.allowCredentials
            ? options.allowCredentials.map(cred => ({
                type: cred.type as PublicKeyCredentialType,
                id: base64urlToBuffer(cred.id)
              }))
            : [],
          userVerification: options.userVerification as UserVerificationRequirement
        }
      }) as PublicKeyCredential

      if (!credential) {
        throw new Error('Failed to get credential')
      }

      const response = credential.response as AuthenticatorAssertionResponse

      // Step 3: Send assertion to backend
      const completeResponse = await $api<AuthResponse>('/auth/passkey/login/complete', {
        method: 'POST',
        body: {
          id: credential.id,
          rawId: bufferToBase64(credential.rawId),
          type: credential.type,
          response: {
            authenticatorData: bufferToBase64(response.authenticatorData),
            clientDataJSON: bufferToBase64(response.clientDataJSON),
            signature: bufferToBase64(response.signature),
            userHandle: response.userHandle ? bufferToBase64(response.userHandle) : undefined
          }
        }
      })

      if (!completeResponse.success) {
        throw new Error(completeResponse.message || 'Login failed')
      }

      // Store tokens
      authStore.setToken(completeResponse.accessToken)
      authStore.setRefreshToken(completeResponse.refreshToken)

      return { success: true, message: completeResponse.message }
    } catch (error: unknown) {
      console.error('Passkey login error:', error)

      // Handle user cancellation
      if (error && typeof error === 'object' && 'name' in error && error.name === 'NotAllowedError') {
        throw new Error('Login cancelled')
      }

      const errorMessage = error && typeof error === 'object' && 'message' in error ? String(error.message) : 'Failed to login with passkey'
      throw new Error(errorMessage)
    }
  }

  // Check if WebAuthn is supported
  function isPasskeySupported(): boolean {
    if (typeof window === 'undefined') return false
    return !!(
      window.PublicKeyCredential
      && navigator.credentials
      && typeof navigator.credentials.create === 'function'
      && typeof navigator.credentials.get === 'function'
    )
  }

  return {
    registerPasskey,
    loginWithPasskey,
    isPasskeySupported
  }
}
