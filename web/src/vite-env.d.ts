/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Shows the demo account hint on the login page outside of `pnpm dev` (set in the Docker build). */
  readonly VITE_SHOW_DEMO_ACCOUNT?: string;
}
