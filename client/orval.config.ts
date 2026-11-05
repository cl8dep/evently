import { defineConfig } from 'orval'

// The spec is written by `dotnet build` (Debug) into client/openapi.json.
export default defineConfig({
  evently: {
    input: './openapi.json',
    output: {
      target: './src/api/generated/endpoints.ts',
      schemas: './src/api/generated/model',
      client: 'react-query',
      httpClient: 'axios',
      mode: 'single',
      clean: true,
      override: { mutator: { path: './src/api/fetcher.ts', name: 'fetcher' } },
    },
  },
})
