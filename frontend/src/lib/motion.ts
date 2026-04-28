import type { Transition, Variants } from "framer-motion";

export const durations = {
  instant: 0.08,
  fast: 0.18,
  medium: 0.32,
  slow: 0.54,
  cinematic: 0.9,
} as const;

export const easings = {
  outQuint: [0.22, 1, 0.36, 1] as [number, number, number, number],
  outExpo: [0.16, 1, 0.3, 1] as [number, number, number, number],
  inOut: [0.65, 0, 0.35, 1] as [number, number, number, number],
  spring: [0.34, 1.56, 0.64, 1] as [number, number, number, number],
} as const;

export const cinematicEntry: Transition = {
  duration: durations.cinematic,
  ease: easings.outExpo,
};

export const fadeRise: Variants = {
  hidden: { opacity: 0, y: 24, filter: "blur(8px)" },
  visible: { opacity: 1, y: 0, filter: "blur(0px)", transition: cinematicEntry },
  exit: { opacity: 0, y: -12, filter: "blur(6px)", transition: { duration: durations.medium, ease: easings.inOut } },
};

export const stagger = (delay = 0.06): Variants => ({
  hidden: {},
  visible: { transition: { staggerChildren: delay, delayChildren: 0.04 } },
});

export const cardHover = {
  rest: { scale: 1, y: 0, boxShadow: "0 0 0 rgba(0,0,0,0)" },
  hover: {
    scale: 1.04,
    y: -4,
    boxShadow: "0 24px 60px -20px rgba(168, 85, 247, 0.35)",
    transition: { duration: durations.fast, ease: easings.outQuint },
  },
  tap: { scale: 0.98, transition: { duration: durations.instant, ease: easings.inOut } },
} as const;

export const heroBackdrop: Variants = {
  hidden: { opacity: 0, scale: 1.06 },
  visible: { opacity: 1, scale: 1, transition: { duration: 1.2, ease: easings.outExpo } },
  exit: { opacity: 0, scale: 1.02, transition: { duration: 0.6, ease: easings.inOut } },
};

export const overlayGradient = {
  scrim: "linear-gradient(180deg, rgba(6,7,13,0) 0%, rgba(6,7,13,0.55) 55%, rgba(6,7,13,0.95) 100%)",
  side: "linear-gradient(90deg, rgba(6,7,13,0.95) 0%, rgba(6,7,13,0.4) 50%, rgba(6,7,13,0) 100%)",
};
