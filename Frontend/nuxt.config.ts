// load dotenv variables
// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  modules: [
    '@nuxt/eslint',
    '@nuxt/ui',
    '@nuxt/image',
    '@nuxt/icon',
    '@pinia/nuxt',
    '@nuxtjs/google-fonts',
    'nuxt-icons',
    '@vite-pwa/nuxt'
  ],

  plugins: ['~/plugins/fetch', '~/plugins/audio.client'],
  $development: {
    runtimeConfig: {
      backendApiUri: 'http://10.8.0.3:8000/api',
      // backendApiUri: 'https://api.staging.ryusu.id/api',
      public: {
        backendApiUri: 'http://10.8.0.3:8000/api'
        // backendApiUri: 'https://api.staging.ryusu.id/api'
      }
    }
  },
  $production: {
    runtimeConfig: {
      // backendApiUri: 'https://api.staging.ryusu.id/api',
      backendApiUri: 'http://puretco-app-service/api',
      public: {
        backendApiUri: 'https://api.staging.ryusu.id/api'
      }
    }
  },

  devtools: {
    enabled: true
  },

  app: {
    head: {
      link: [
        { rel: 'icon', type: 'image/x-icon', href: '/favico.ico' }
      ],
      title: 'SIGMA'
    }
  },

  css: ['~/assets/css/main.css'],

  routeRules: {
    '/': { prerender: true }
  },
  devServer: {
    host: '0.0.0.0',
    port: 3000
  },

  compatibilityDate: '2025-01-15',

  eslint: {
    config: {
      stylistic: {
        commaDangle: 'never',
        braceStyle: '1tbs'
      }
    }
  },

  googleFonts: {
    families: {
      Poppins: [300, 400, 500, 600, 700]
    },
    display: 'swap'
  },
  icon: {
    serverBundle: {
      collections: ['lucide', 'simple-icons']
    }
  },
  pwa: {
    manifest: {
      short_name: 'SIGMA',
      lang: 'en',
      display: 'standalone',
      orientation: 'portrait',
      background_color: '#ffffff',
      icons: [
        {
          src: '/icons/pwa-64x64.png',
          sizes: '64x64',
          type: 'image/png'
        },
        {
          src: '/icons/pwa-192x192.png',
          sizes: '192x192',
          type: 'image/png'
        },
        {
          src: '/icons/pwa-512x512.png',
          sizes: '512x512',
          type: 'image/png'
        },
        {
          src: '/icons/maskable-icon-512x512.png',
          sizes: '512x512',
          type: 'image/png',
          purpose: 'maskable'
        }
      ]
    },
    workbox: {
      navigateFallback: '/',
      globPatterns: ['**/*.{js,css,html,png,svg,ico}'],
      importScripts: ['/sw-custom.js'],
      navigateFallbackDenylist: [/^\/auth/, /^\/api/, /\?code=/, /\?state=/],

      runtimeCaching: [
        {
          // Don't cache auth/API routes
          urlPattern: /^https:\/\/.*\/api\/.*/i,
          handler: 'NetworkOnly'
        },
        {
          // Don't cache OAuth callbacks
          urlPattern: /\/(auth|callback|login)/,
          handler: 'NetworkOnly'
        },
        {
          // Cache pages (except auth)
          urlPattern: /^https:\/\/.*/i,
          handler: 'NetworkFirst',
          options: {
            cacheName: 'pages-cache',
            expiration: {
              maxEntries: 50,
              maxAgeSeconds: 24 * 60 * 60 // 24 hours
            },
            networkTimeoutSeconds: 10
          }
        }
      ]
    },
    client: {
      installPrompt: true,
      periodicSyncForUpdates: 3600 // Check for updates every hour
    }
    // devOptions: {
    //   enabled: true,
    //   type: 'module'
    // }
  }
})
