import { cpSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const source = fileURLToPath(new URL('../dist/milkco-client/browser/', import.meta.url));
const target = fileURLToPath(new URL('../../WebApplication/wwwroot/', import.meta.url));
mkdirSync(target, { recursive: true });
cpSync(source, target, { recursive: true });
