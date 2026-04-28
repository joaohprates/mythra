import { describe, expect, it } from "vitest";
import { durations, easings, fadeRise } from "@/lib/motion";

describe("motion tokens", () => {
  it("exposes a duration scale in ascending order", () => {
    const order = [durations.instant, durations.fast, durations.medium, durations.slow, durations.cinematic];
    const sorted = [...order].sort((a, b) => a - b);
    expect(order).toEqual(sorted);
  });

  it("exposes easing tuples of length 4 (cubic-bezier)", () => {
    Object.values(easings).forEach((curve) => {
      expect(curve).toHaveLength(4);
      curve.forEach((n) => expect(typeof n).toBe("number"));
    });
  });

  it("fadeRise variant covers hidden, visible, exit states", () => {
    expect(fadeRise.hidden).toBeDefined();
    expect(fadeRise.visible).toBeDefined();
    expect(fadeRise.exit).toBeDefined();
  });
});
