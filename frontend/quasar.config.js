// Configuration for your app
// https://v2.quasar.dev/quasar-cli-vite/quasar-config-file

import { defineConfig } from "#q-app/wrappers";

export default defineConfig((/* ctx */) => {
  return {
    boot: ["axios"],

    css: ["app.scss"],

    extras: ["roboto-font", "material-icons"],

    build: {
      target: {
        browser: "baseline-widely-available",
        node: "node22",
      },

      vueRouterMode: "hash",
    },

    devServer: {
      open: true,
    },

    framework: {
      config: {
        notify: {
          position: "top-right",
          timeout: 4000,
        },
      },

      plugins: ["Notify", "Dialog", "Loading"],
    },

    animations: [],

    ssr: {
      prodPort: 3000,
      middlewares: ["render"],
      pwa: false,
    },

    pwa: {
      workboxMode: "GenerateSW",
    },

    cordova: {},

    capacitor: {
      hideSplashscreen: true,
    },

    electron: {
      preloadScripts: ["electron-preload"],
      inspectPort: 5858,
      bundler: "packager",
      packager: {},
      builder: {
        appId: "frontend",
      },
    },

    bex: {
      extraScripts: [],
    },
  };
});
