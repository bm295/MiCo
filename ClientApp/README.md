# MilkCO Angular client

See the [repository README](../README.md) for database setup, development, Docker, and hosted builds.

- `npm ci`: install locked dependencies
- `npm start`: development server on port 4200 with API proxy to port 5000
- `npm run build`: production output in `dist/milkco-client/browser`
- `npm run build:hosted`: build and copy browser assets to the .NET host
- `npm test`: client tests

The Piscina override keeps Angular's build worker dependency on a patched compatible version. Remove it when Angular 21 includes the patch upstream.
