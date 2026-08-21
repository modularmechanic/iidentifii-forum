import '@testing-library/jest-dom/vitest';
import { afterEach } from 'vitest';

// A session left behind by one test would sign the next one in by accident.
afterEach(() => localStorage.clear());
