"use client";

import { create } from "zustand";

/**
 * Network health store — tracks how many image loads have failed in the last
 * 60 seconds. When the failure count crosses a threshold, the
 * `<ConnectivityBanner />` surfaces a warning that some external resources
 * are being blocked by the user's network. The app keeps working; this is
 * purely informational.
 */

const WINDOW_MS = 60_000;
const THRESHOLD = 8;

interface NetworkHealthState {
  /** Timestamps (ms) of recent image errors, kept rolling over WINDOW_MS. */
  errors: number[];
  /** True when there have been more than THRESHOLD image errors in WINDOW_MS. */
  failureRateHigh: boolean;
  /** Record one image load error; trims old entries. */
  imageError(): void;
  /** Re-evaluate `failureRateHigh` (used by the banner's interval tick). */
  prune(): void;
  /** Test/utility reset. */
  reset(): void;
}

function compute(errors: number[]): boolean {
  return errors.length > THRESHOLD;
}

export const useNetworkHealth = create<NetworkHealthState>((set, get) => ({
  errors: [],
  failureRateHigh: false,
  imageError() {
    const now = Date.now();
    const next = [...get().errors, now].filter((t) => now - t <= WINDOW_MS);
    set({ errors: next, failureRateHigh: compute(next) });
  },
  prune() {
    const now = Date.now();
    const next = get().errors.filter((t) => now - t <= WINDOW_MS);
    if (next.length === get().errors.length && get().failureRateHigh === compute(next)) return;
    set({ errors: next, failureRateHigh: compute(next) });
  },
  reset() {
    set({ errors: [], failureRateHigh: false });
  },
}));
