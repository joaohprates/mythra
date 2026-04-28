"use client";

import { motion } from "framer-motion";
import { fadeRise } from "@/lib/motion";
import { cn } from "@/lib/cn";

export function PageScaffold({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <motion.main
      initial="hidden"
      animate="visible"
      exit="exit"
      variants={fadeRise}
      className={cn("relative mx-auto max-w-[1700px] px-6 pb-24 pt-8 lg:px-10", className)}
    >
      {children}
    </motion.main>
  );
}
