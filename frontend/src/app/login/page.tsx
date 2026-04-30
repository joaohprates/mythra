"use client";

import { motion } from "framer-motion";
import { Eye, EyeOff, Sparkles } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import { cn } from "@/lib/cn";

export default function LoginPage() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const setAuth = useAuthStore((s) => s.setAuth);
  const [mode, setMode] = useState<"login" | "register">("login");
  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (accessToken) router.replace("/");
  }, [accessToken, router]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      if (mode === "login") {
        const res = await api.post("/auth/login", { emailOrUsername, password });
        setAuth(res.data);
      } else {
        const res = await api.post("/auth/register", { email, username, password });
        setAuth(res.data);
      }
      router.replace("/");
    } catch (e) {
      const data = (e as { response?: { data?: { detail?: string; errors?: Record<string, string[]> } } }).response?.data;
      const msg =
        data?.detail ??
        (data?.errors ? Object.values(data.errors).flat().join(" ") : null) ??
        "Something went wrong. Please try again.";
      setError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <main className="relative grid min-h-screen place-items-center overflow-hidden px-6 py-12">
      <div className="pointer-events-none absolute inset-0 -z-10">
        <motion.div
          className="absolute -left-24 top-10 h-[420px] w-[420px] rounded-full bg-gradient-to-tr from-mythra-purple/40 to-transparent blur-3xl"
          animate={{ x: [0, 20, 0], y: [0, -10, 0], opacity: [0.7, 0.85, 0.7] }}
          transition={{ duration: 14, repeat: Infinity, ease: "easeInOut" }}
        />
        <motion.div
          className="absolute -right-32 bottom-0 h-[480px] w-[480px] rounded-full bg-gradient-to-tr from-mythra-blue/35 to-transparent blur-3xl"
          animate={{ x: [0, -25, 0], y: [0, 20, 0], opacity: [0.6, 0.85, 0.6] }}
          transition={{ duration: 16, repeat: Infinity, ease: "easeInOut" }}
        />
        <motion.div
          className="absolute left-1/2 top-1/2 h-[300px] w-[300px] -translate-x-1/2 -translate-y-1/2 rounded-full bg-gradient-to-tr from-mythra-magenta/30 to-transparent blur-3xl"
          animate={{ scale: [1, 1.15, 1], opacity: [0.55, 0.75, 0.55] }}
          transition={{ duration: 11, repeat: Infinity, ease: "easeInOut" }}
        />
      </div>

      <motion.div
        initial={{ opacity: 0, y: 32, filter: "blur(8px)" }}
        animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
        transition={{ duration: 0.9, ease: [0.16, 1, 0.3, 1] }}
        className="w-full max-w-md"
      >
        <div className="mb-8 flex items-center justify-center gap-3">
          <span className="grid h-12 w-12 place-items-center rounded-2xl bg-gradient-to-br from-[#a855f7] via-[#3b82f6] to-[#ec4899] mythra-glow-purple">
            <Sparkles size={20} className="text-white" />
          </span>
          <h1 className="text-3xl font-bold tracking-tight gradient-text">Mythra</h1>
        </div>

        <div className="glass rounded-3xl p-8 shadow-mythra-card">
          <h2 className="text-center text-xl font-semibold">
            {mode === "login" ? "Welcome back" : "Create your universe"}
          </h2>
          <p className="mt-1 text-center text-sm text-mythra-text-muted">
            {mode === "login"
              ? "Sign in to continue your story"
              : "Start collecting your worlds in one place"}
          </p>

          <form onSubmit={submit} className="mt-7 space-y-4">
            {mode === "login" ? (
              <Field
                label="Email or username"
                value={emailOrUsername}
                onChange={setEmailOrUsername}
                autoComplete="username"
                required
              />
            ) : (
              <>
                <Field label="Email" value={email} onChange={setEmail} type="email" autoComplete="email" required />
                <Field label="Username" value={username} onChange={setUsername} autoComplete="username" required />
              </>
            )}

            <div>
              <label className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-mythra-text-soft">
                Password
              </label>
              <div className="relative">
                <input
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  minLength={8}
                  autoComplete={mode === "login" ? "current-password" : "new-password"}
                  className="w-full rounded-xl border border-white/[0.06] bg-white/[0.03] px-4 py-3 pr-11 text-sm text-white placeholder-mythra-text-soft outline-none transition focus:border-mythra-purple/60 focus:bg-white/[0.06]"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-mythra-text-muted hover:text-white"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
              {mode === "register" && (
                <p className="mt-1.5 text-[11px] text-mythra-text-soft">
                  Min 8 characters · uppercase · lowercase · number
                </p>
              )}
            </div>

            {error && (
              <motion.p initial={{ opacity: 0, y: -6 }} animate={{ opacity: 1, y: 0 }} className="text-sm text-rose-300">
                {error}
              </motion.p>
            )}

            <button
              type="submit"
              disabled={submitting}
              className={cn(
                "relative inline-flex w-full items-center justify-center rounded-xl bg-gradient-to-r from-[#a855f7] via-[#7c3aed] to-[#3b82f6] py-3 text-sm font-semibold text-white shadow-[0_18px_40px_-15px_rgba(168,85,247,0.7)] transition-all",
                "hover:scale-[1.01] hover:shadow-[0_22px_55px_-15px_rgba(168,85,247,0.85)]",
                submitting && "cursor-wait opacity-80"
              )}
            >
              {submitting ? "Loading..." : mode === "login" ? "Sign in" : "Create account"}
            </button>
          </form>

          <button
            onClick={() => setMode((m) => (m === "login" ? "register" : "login"))}
            className="mt-5 w-full text-center text-xs text-mythra-text-muted transition hover:text-white"
          >
            {mode === "login" ? "Need an account? Create one →" : "Already have an account? Sign in →"}
          </button>
        </div>

        <p className="mt-6 text-center text-xs text-mythra-text-soft">
          Mythra is self-hosted. Your library, your rules.
        </p>
      </motion.div>
    </main>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
  autoComplete,
  required,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  type?: string;
  autoComplete?: string;
  required?: boolean;
}) {
  return (
    <div>
      <label className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-mythra-text-soft">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        autoComplete={autoComplete}
        className="w-full rounded-xl border border-white/[0.06] bg-white/[0.03] px-4 py-3 text-sm text-white placeholder-mythra-text-soft outline-none transition focus:border-mythra-purple/60 focus:bg-white/[0.06]"
      />
    </div>
  );
}
