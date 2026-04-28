"use client";

import { motion } from "framer-motion";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { Folder, Plus, RefreshCw, Trash2 } from "lucide-react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import type { Library } from "@/lib/types";

export default function SettingsPage() {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);
  const clear = useAuthStore((s) => s.clear);
  const qc = useQueryClient();

  if (!accessToken) {
    if (typeof window !== "undefined") router.replace("/login");
    return null;
  }

  const libs = useQuery({
    queryKey: ["libraries"],
    queryFn: async () => (await api.get<Library[]>("/libraries")).data,
  });

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.h1
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-1 text-3xl font-bold tracking-tight md:text-4xl"
        >
          <span className="gradient-text">Settings</span>
        </motion.h1>
        <p className="text-sm text-mythra-text-muted">Manage profiles, libraries, and preferences.</p>

        <div className="mt-10 grid gap-8 lg:grid-cols-[260px_1fr]">
          <aside className="space-y-1 text-sm">
            <SidebarLink active>Libraries</SidebarLink>
            <SidebarLink>Profiles</SidebarLink>
            <SidebarLink>Account</SidebarLink>
            <SidebarLink>Playback</SidebarLink>
            <SidebarLink>Metadata</SidebarLink>
          </aside>

          <section>
            <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-semibold">Libraries</h2>
                <CreateLibraryButton onCreated={() => qc.invalidateQueries({ queryKey: ["libraries"] })} />
              </div>

              {libs.data && libs.data.length === 0 && (
                <p className="mt-6 text-sm text-mythra-text-muted">No libraries yet. Create one to start scanning content.</p>
              )}

              <ul className="mt-6 space-y-3">
                {(libs.data ?? []).map((lib) => (
                  <li key={lib.id} className="rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4">
                    <div className="flex items-center gap-3">
                      <span className="grid h-10 w-10 place-items-center rounded-full bg-gradient-to-br from-mythra-purple to-mythra-blue text-white">
                        <Folder size={16} />
                      </span>
                      <div className="min-w-0">
                        <p className="font-medium">{lib.name}</p>
                        <p className="text-xs text-mythra-text-soft">
                          {lib.kind} • {lib.folderCount} folder{lib.folderCount !== 1 && "s"} • {lib.lastScannedAt ? `last scan ${new Date(lib.lastScannedAt).toLocaleString()}` : "not scanned"}
                        </p>
                      </div>
                      <div className="ml-auto flex items-center gap-2">
                        <button
                          onClick={() => api.post(`/libraries/${lib.id}/scan`).then(() => qc.invalidateQueries({ queryKey: ["libraries"] }))}
                          className="inline-flex items-center gap-1 rounded-full border border-white/10 px-3 py-1.5 text-xs hover:bg-white/10"
                        >
                          <RefreshCw size={12} /> Scan
                        </button>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            </div>

            <div className="mt-8 rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur">
              <h2 className="text-lg font-semibold">Account</h2>
              <p className="mt-1 text-sm text-mythra-text-soft">Signed in as {user?.email}</p>
              <button
                onClick={() => { clear(); router.replace("/login"); }}
                className="mt-4 inline-flex items-center gap-2 rounded-full border border-rose-500/30 bg-rose-500/10 px-4 py-2 text-sm text-rose-200 hover:bg-rose-500/20"
              >
                <Trash2 size={14} /> Sign out
              </button>
            </div>
          </section>
        </div>
      </PageScaffold>
    </>
  );
}

function SidebarLink({ children, active }: { children: React.ReactNode; active?: boolean }) {
  return (
    <button
      className={
        "block w-full rounded-xl px-3 py-2 text-left transition " +
        (active ? "bg-gradient-to-r from-mythra-purple/25 to-mythra-blue/10 text-white" : "text-mythra-text-muted hover:bg-white/[0.04] hover:text-white")
      }
    >
      {children}
    </button>
  );
}

function CreateLibraryButton({ onCreated }: { onCreated: () => void }) {
  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");
  const [kind, setKind] = useState("Video");
  const [folder, setFolder] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const submit = async () => {
    if (!name || !folder) return;
    setSubmitting(true);
    try {
      await api.post("/libraries", {
        name,
        kind,
        description: null,
        folders: [folder],
      });
      setName("");
      setFolder("");
      setOpen(false);
      onCreated();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="inline-flex items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white"
      >
        <Plus size={14} /> New library
      </button>
      {open && (
        <motion.div
          initial={{ opacity: 0, y: -8 }}
          animate={{ opacity: 1, y: 0 }}
          className="mt-4 rounded-2xl border border-white/[0.06] bg-white/[0.04] p-4"
        >
          <div className="grid gap-3 md:grid-cols-3">
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Library name"
              className="rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm outline-none"
            />
            <select
              value={kind}
              onChange={(e) => setKind(e.target.value)}
              className="rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm"
            >
              {["Video", "Anime", "Manga", "Book", "Audiobook", "Music"].map((k) => (
                <option key={k} value={k} className="bg-black">{k}</option>
              ))}
            </select>
            <input
              value={folder}
              onChange={(e) => setFolder(e.target.value)}
              placeholder="Path to folder (e.g. C:\\Media\\Movies)"
              className="rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm outline-none"
            />
          </div>
          <div className="mt-3 flex gap-2">
            <button
              onClick={submit}
              disabled={submitting}
              className="rounded-full bg-white px-4 py-2 text-xs font-medium text-black"
            >
              {submitting ? "Creating…" : "Create"}
            </button>
            <button
              onClick={() => setOpen(false)}
              className="rounded-full border border-white/10 px-4 py-2 text-xs"
            >
              Cancel
            </button>
          </div>
        </motion.div>
      )}
    </>
  );
}
