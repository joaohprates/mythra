import { create } from "zustand";

export type ToastKind = "success" | "error" | "info";

export interface ToastAction {
  label: string;
  onClick: () => void;
}

export interface Toast {
  id: string;
  kind: ToastKind;
  message: string;
  action?: ToastAction;
  duration?: number;
}

interface ToastsState {
  toasts: Toast[];
  push: (toast: Omit<Toast, "id">) => string;
  dismiss: (id: string) => void;
  clear: () => void;
}

export const useToasts = create<ToastsState>((set) => ({
  toasts: [],
  push: (toast) => {
    const id =
      typeof crypto !== "undefined" && crypto.randomUUID
        ? crypto.randomUUID()
        : `toast-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const next: Toast = { duration: 5000, ...toast, id };
    set((s) => ({ toasts: [...s.toasts, next] }));
    return id;
  },
  dismiss: (id) => set((s) => ({ toasts: s.toasts.filter((t) => t.id !== id) })),
  clear: () => set({ toasts: [] }),
}));
