"use client";

import { AnimatePresence, motion } from "framer-motion";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Folder, Plus, RefreshCw, Trash2, X, ChevronRight,
  Library, User, Settings2, Play, Globe, Database,
  FolderOpen, Check, Edit2, AlertTriangle, Package,
  Download, Upload, ToggleLeft, ToggleRight,
} from "lucide-react";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { FolderBrowser } from "@/components/settings/FolderBrowser";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import type { Library as LibraryType, LibraryFolder, Profile } from "@/lib/types";

type SectionId = "libraries" | "profiles" | "account" | "playback" | "language" | "metadata" | "addons";

const SECTIONS: { id: SectionId; label: string; icon: React.ReactNode }[] = [
  { id: "libraries", label: "Libraries",  icon: <Library size={15} /> },
  { id: "profiles",  label: "Profiles",   icon: <User size={15} /> },
  { id: "addons",    label: "Addons",     icon: <Package size={15} /> },
  { id: "language",  label: "Language",   icon: <Globe size={15} /> },
  { id: "playback",  label: "Playback",   icon: <Play size={15} /> },
  { id: "metadata",  label: "Metadata",   icon: <Database size={15} /> },
  { id: "account",   label: "Account",    icon: <Settings2 size={15} /> },
];

const LIBRARY_KINDS = ["Video", "Anime", "Manga", "Book", "Audiobook", "Music", "General"];

export default function SettingsPage() {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);
  const clear = useAuthStore((s) => s.clear);
  const qc = useQueryClient();

  const [section, setSection] = useState<SectionId>("libraries");
  const [editingLib, setEditingLib] = useState<LibraryType | null>(null);

  useEffect(() => {
    const hash = window.location.hash.replace("#", "") as SectionId;
    if (SECTIONS.find((s) => s.id === hash)) setSection(hash);
  }, []);

  const navigate = (id: SectionId) => {
    setSection(id);
    window.location.hash = id;
  };

  const libs = useQuery({
    queryKey: ["libraries"],
    queryFn: async () => (await api.get<LibraryType[]>("/libraries")).data,
    enabled: !!accessToken,
  });

  useEffect(() => {
    if (!accessToken) router.replace("/login");
  }, [accessToken, router]);

  if (!accessToken) return null;

  return (
    <>
      <Topbar />
      <PageScaffold>
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}>
          <h1 className="mb-1 text-3xl font-bold tracking-tight md:text-4xl">
            <span className="gradient-text">Settings</span>
          </h1>
          <p className="text-sm text-mythra-text-muted">Manage profiles, libraries, and preferences.</p>
        </motion.div>

        <div className="mt-10 grid gap-8 lg:grid-cols-[240px_1fr]">
          {/* Sidebar */}
          <aside className="space-y-0.5 text-sm">
            {SECTIONS.map((s) => (
              <button
                key={s.id}
                onClick={() => navigate(s.id)}
                className={
                  "flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-left transition-all " +
                  (section === s.id
                    ? "bg-gradient-to-r from-mythra-purple/25 to-mythra-blue/10 text-white font-medium"
                    : "text-mythra-text-muted hover:bg-white/[0.04] hover:text-white")
                }
              >
                {s.icon}
                {s.label}
                {section === s.id && <ChevronRight size={13} className="ml-auto opacity-50" />}
              </button>
            ))}
          </aside>

          {/* Content */}
          <main>
            <AnimatePresence mode="wait">
              <motion.div
                key={section}
                initial={{ opacity: 0, x: 10 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -10 }}
                transition={{ duration: 0.18 }}
              >
                {section === "libraries" && (
                  <LibrariesSection
                    libs={libs.data ?? []}
                    loading={libs.isLoading}
                    onRefresh={() => qc.invalidateQueries({ queryKey: ["libraries"] })}
                    onEdit={setEditingLib}
                  />
                )}
                {section === "profiles" && <ProfilesSection user={user} />}
                {section === "addons" && <AddonsSection />}
                {section === "language" && <LanguageSection />}
                {section === "playback" && <PlaybackSection />}
                {section === "account" && <AccountSection user={user} onSignOut={() => { clear(); router.replace("/login"); }} />}
                {section === "metadata" && <MetadataSection />}
              </motion.div>
            </AnimatePresence>
          </main>
        </div>
      </PageScaffold>

      {/* Library edit modal */}
      <AnimatePresence>
        {editingLib && (
          <LibraryEditModal
            lib={editingLib}
            onClose={() => setEditingLib(null)}
            onSaved={() => {
              qc.invalidateQueries({ queryKey: ["libraries"] });
              setEditingLib(null);
            }}
          />
        )}
      </AnimatePresence>
    </>
  );
}

// ════════════════════════════════════════════════════════════
// LIBRARIES SECTION
// ════════════════════════════════════════════════════════════

function LibrariesSection({
  libs, loading, onRefresh, onEdit,
}: {
  libs: LibraryType[];
  loading: boolean;
  onRefresh: () => void;
  onEdit: (lib: LibraryType) => void;
}) {
  const [creating, setCreating] = useState(false);

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Libraries</h2>
        <button
          onClick={() => setCreating(true)}
          className="inline-flex items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white"
        >
          <Plus size={14} /> New library
        </button>
      </div>

      <AnimatePresence>
        {creating && (
          <CreateLibraryForm
            onCreated={() => { onRefresh(); setCreating(false); }}
            onCancel={() => setCreating(false)}
          />
        )}
      </AnimatePresence>

      {loading && <p className="mt-6 text-sm text-mythra-text-muted">Loading…</p>}

      {!loading && libs.length === 0 && (
        <p className="mt-6 text-sm text-mythra-text-muted">No libraries yet. Create one to start scanning content.</p>
      )}

      <ul className="mt-6 space-y-3">
        {libs.map((lib) => (
          <LibraryRow key={lib.id} lib={lib} onRefreshed={onRefresh} onEdit={onEdit} />
        ))}
      </ul>
    </div>
  );
}

function LibraryRow({ lib, onRefreshed, onEdit }: { lib: LibraryType; onRefreshed: () => void; onEdit: (lib: LibraryType) => void }) {
  const [scanning, setScanning] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const scan = async () => {
    setScanning(true);
    try { await api.post(`/libraries/${lib.id}/scan`); onRefreshed(); }
    finally { setScanning(false); }
  };

  const del = async () => {
    if (!confirm(`Delete library "${lib.name}"? This won't delete files on disk.`)) return;
    setDeleting(true);
    try { await api.delete(`/libraries/${lib.id}`); onRefreshed(); }
    finally { setDeleting(false); }
  };

  return (
    <motion.li layout className="rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4">
      <div className="flex items-center gap-3">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-gradient-to-br from-mythra-purple to-mythra-blue text-white">
          <Folder size={16} />
        </span>
        <div className="min-w-0 flex-1">
          <p className="font-medium">
            {lib.name}
            {lib.isSystem && (
              <span className="ml-2 rounded-full bg-mythra-blue/20 px-2 py-0.5 text-[10px] text-mythra-blue">System</span>
            )}
          </p>
          <p className="truncate text-xs text-mythra-text-soft">
            {lib.kind} · {lib.folderCount} folder{lib.folderCount !== 1 ? "s" : ""}
            {lib.lastScannedAt ? ` · scanned ${new Date(lib.lastScannedAt).toLocaleString()}` : " · never scanned"}
          </p>
        </div>
        <div className="ml-auto flex items-center gap-1.5">
          <button onClick={() => onEdit(lib)} className="inline-flex items-center gap-1 rounded-full border border-white/10 px-3 py-1.5 text-xs hover:bg-white/10">
            <Edit2 size={12} /> Edit
          </button>
          <button onClick={scan} disabled={scanning} className="inline-flex items-center gap-1 rounded-full border border-white/10 px-3 py-1.5 text-xs hover:bg-white/10 disabled:opacity-50">
            <RefreshCw size={12} className={scanning ? "animate-spin" : ""} />
            {scanning ? "Scanning…" : "Scan"}
          </button>
          {!lib.isSystem && (
            <button onClick={del} disabled={deleting} className="inline-flex h-7 w-7 items-center justify-center rounded-full border border-rose-500/30 text-rose-400 hover:bg-rose-500/10 disabled:opacity-50">
              <Trash2 size={12} />
            </button>
          )}
        </div>
      </div>
    </motion.li>
  );
}

// ── Create library form ──────────────────────────────────────────────────────

function CreateLibraryForm({ onCreated, onCancel }: { onCreated: () => void; onCancel: () => void }) {
  const [name, setName] = useState("");
  const [kind, setKind] = useState("Video");
  const [folder, setFolder] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [browsingFolder, setBrowsingFolder] = useState(false);

  const submit = async () => {
    if (!name.trim() || !folder.trim()) return;
    setSubmitting(true);
    try {
      await api.post("/libraries", { name: name.trim(), kind, description: null, folders: [folder.trim()] });
      onCreated();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0, height: 0 }}
      animate={{ opacity: 1, height: "auto" }}
      exit={{ opacity: 0, height: 0 }}
      className="mt-4 overflow-hidden rounded-2xl border border-white/[0.06] bg-white/[0.04] p-4"
    >
      <div className="grid gap-3 md:grid-cols-2">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Library name"
          className="rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm outline-none placeholder:text-white/30"
        />
        <select
          value={kind}
          onChange={(e) => setKind(e.target.value)}
          className="rounded-xl border border-white/10 bg-[#0c0e1a] px-3 py-2 text-sm"
        >
          {LIBRARY_KINDS.map((k) => <option key={k} value={k}>{k}</option>)}
        </select>
      </div>
      <div className="mt-3 flex gap-2">
        <input
          value={folder}
          onChange={(e) => setFolder(e.target.value)}
          placeholder="Folder path (e.g. /media/movies)"
          className="flex-1 rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm font-mono outline-none placeholder:text-white/30"
        />
        <button
          onClick={() => setBrowsingFolder(true)}
          className="inline-flex items-center gap-1 rounded-xl border border-white/10 px-3 py-2 text-xs hover:bg-white/10"
          title="Browse folders"
        >
          <FolderOpen size={14} />
        </button>
      </div>
      <div className="mt-3 flex gap-2">
        <button
          onClick={submit}
          disabled={submitting || !name.trim() || !folder.trim()}
          className="rounded-full bg-white px-4 py-2 text-xs font-medium text-black disabled:opacity-50"
        >
          {submitting ? "Creating…" : "Create"}
        </button>
        <button onClick={onCancel} className="rounded-full border border-white/10 px-4 py-2 text-xs">
          Cancel
        </button>
      </div>

      <AnimatePresence>
        {browsingFolder && (
          <FolderBrowser
            initialPath={folder || "/"}
            onSelect={setFolder}
            onClose={() => setBrowsingFolder(false)}
          />
        )}
      </AnimatePresence>
    </motion.div>
  );
}

// ── Library edit modal ───────────────────────────────────────────────────────

function LibraryEditModal({ lib, onClose, onSaved }: { lib: LibraryType; onClose: () => void; onSaved: () => void }) {
  const [tab, setTab] = useState<"folders" | "extensions">("folders");
  const [folders, setFolders] = useState<LibraryFolder[]>(lib.folders ?? []);
  const [newPath, setNewPath] = useState("");
  const [extensions, setExtensions] = useState<string[]>(lib.allowedExtensions);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [browsingFolder, setBrowsingFolder] = useState(false);

  const addFolder = async () => {
    if (!newPath.trim()) return;
    try {
      await api.post(`/libraries/${lib.id}/folders`, { path: newPath.trim() });
      setFolders((prev) => [...prev, { id: crypto.randomUUID(), path: newPath.trim(), isActive: true }]);
      setNewPath("");
      onSaved();
    } catch { setError("Failed to add folder."); }
  };

  const removeFolder = async (folderId: string) => {
    try {
      await api.delete(`/libraries/${lib.id}/folders/${folderId}`);
      setFolders((prev) => prev.filter((f) => f.id !== folderId));
      onSaved();
    } catch { setError("Failed to remove folder."); }
  };

  const saveExtensions = async () => {
    setSaving(true); setError(null);
    try {
      await api.put(`/libraries/${lib.id}/extensions`, { extensions });
      onSaved();
    } catch { setError("Failed to save extensions."); }
    finally { setSaving(false); }
  };

  return (
    <motion.div
      initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4"
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <motion.div
        initial={{ scale: 0.94, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} exit={{ scale: 0.94, opacity: 0 }}
        className="w-full max-w-xl rounded-3xl border border-white/[0.08] bg-[#0c0e1a] p-6 shadow-2xl"
      >
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-semibold text-lg">{lib.name}</h2>
            <p className="text-xs text-mythra-text-muted">{lib.kind} library</p>
          </div>
          <button onClick={onClose} className="rounded-full p-1.5 hover:bg-white/10"><X size={16} /></button>
        </div>

        <div className="mt-4 flex gap-1 rounded-xl border border-white/[0.06] bg-white/[0.02] p-1">
          {(["folders", "extensions"] as const).map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={"flex-1 rounded-lg py-1.5 text-xs font-medium transition-all capitalize " + (tab === t ? "bg-white/10 text-white" : "text-mythra-text-muted hover:text-white")}
            >
              {t}
            </button>
          ))}
        </div>

        {error && (
          <div className="mt-3 flex items-center gap-2 rounded-xl bg-rose-500/10 border border-rose-500/20 px-3 py-2 text-xs text-rose-300">
            <AlertTriangle size={13} /> {error}
          </div>
        )}

        {tab === "folders" && (
          <div className="mt-4 space-y-2">
            {folders.length === 0 && <p className="text-xs text-mythra-text-muted">No folders configured yet.</p>}
            {folders.map((f) => (
              <div key={f.id} className="flex items-center gap-2 rounded-xl border border-white/[0.05] bg-white/[0.02] px-3 py-2.5">
                <FolderOpen size={13} className="shrink-0 text-mythra-text-muted" />
                <span className="flex-1 truncate text-xs font-mono">{f.path}</span>
                <span className={`text-[10px] ${f.isActive ? "text-emerald-400" : "text-rose-400"}`}>{f.isActive ? "active" : "inactive"}</span>
                <button onClick={() => removeFolder(f.id)} className="ml-1 rounded-full p-1 hover:bg-rose-500/10 text-rose-400">
                  <X size={12} />
                </button>
              </div>
            ))}
            <div className="flex gap-2 pt-1">
              <input
                value={newPath}
                onChange={(e) => setNewPath(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && addFolder()}
                placeholder="/path/to/folder"
                className="flex-1 rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-xs font-mono outline-none placeholder:text-white/20"
              />
              <button
                onClick={() => setBrowsingFolder(true)}
                className="rounded-xl border border-white/10 px-3 py-2 text-xs hover:bg-white/10"
                title="Browse"
              >
                <FolderOpen size={13} />
              </button>
              <button
                onClick={addFolder}
                disabled={!newPath.trim()}
                className="rounded-xl bg-white/10 px-3 py-2 text-xs hover:bg-white/15 disabled:opacity-40"
              >
                Add
              </button>
            </div>
          </div>
        )}

        {tab === "extensions" && (
          <div className="mt-4">
            <p className="mb-2 text-xs text-mythra-text-muted">
              Custom file extensions for this library. Leave empty to use defaults for {lib.kind}.
            </p>
            <ExtensionsEditor
              value={extensions}
              onChange={setExtensions}
              placeholder={lib.effectiveExtensions.slice(0, 6).join(", ") + "…"}
            />
            <div className="mt-3 flex items-center gap-2">
              <button
                onClick={saveExtensions}
                disabled={saving}
                className="inline-flex items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white disabled:opacity-50"
              >
                <Check size={12} /> {saving ? "Saving…" : "Save extensions"}
              </button>
              {extensions.length > 0 && (
                <button onClick={() => setExtensions([])} className="text-xs text-mythra-text-muted hover:text-white">
                  Reset to defaults
                </button>
              )}
            </div>
          </div>
        )}
      </motion.div>

      <AnimatePresence>
        {browsingFolder && (
          <FolderBrowser
            initialPath={newPath || "/"}
            onSelect={setNewPath}
            onClose={() => setBrowsingFolder(false)}
          />
        )}
      </AnimatePresence>
    </motion.div>
  );
}

// ── Extensions tag-input editor ──────────────────────────────────────────────

function ExtensionsEditor({ value, onChange, placeholder }: { value: string[]; onChange: (v: string[]) => void; placeholder?: string }) {
  const [input, setInput] = useState("");
  const add = () => {
    const raw = input.trim().toLowerCase();
    if (!raw) return;
    const ext = raw.startsWith(".") ? raw : "." + raw;
    if (!value.includes(ext)) onChange([...value, ext]);
    setInput("");
  };
  return (
    <div className="rounded-xl border border-white/10 bg-white/[0.02] p-3">
      <div className="flex flex-wrap gap-1.5 min-h-[24px]">
        {value.map((ext) => (
          <span key={ext} className="inline-flex items-center gap-1 rounded-full border border-white/[0.08] bg-white/[0.06] px-2.5 py-0.5 text-xs font-mono">
            {ext}
            <button onClick={() => onChange(value.filter((e) => e !== ext))} className="text-mythra-text-muted hover:text-white"><X size={10} /></button>
          </span>
        ))}
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " " || e.key === ",") { e.preventDefault(); add(); }
            if (e.key === "Backspace" && !input && value.length > 0) onChange(value.slice(0, -1));
          }}
          onBlur={add}
          placeholder={value.length === 0 ? (placeholder ?? "Add extensions…") : ""}
          className="min-w-[140px] flex-1 bg-transparent text-xs font-mono outline-none placeholder:text-white/20"
        />
      </div>
      <p className="mt-1.5 text-[10px] text-mythra-text-muted">Press Enter or Space to add. Backspace to remove last.</p>
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// PROFILES SECTION
// ════════════════════════════════════════════════════════════

function ProfilesSection({ user }: { user: { id?: string; profiles?: Profile[] } | null }) {
  const accessToken = useAuthStore((s) => s.accessToken);
  const setUser = useAuthStore((s) => s.setUser);
  const qc = useQueryClient();
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [isKid, setIsKid] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const profiles = user?.profiles ?? [];

  const createProfile = async () => {
    if (!newName.trim()) return;
    setError(null);
    try {
      await api.post("/auth/profiles", { name: newName.trim(), isKidFriendly: isKid, theme: "dark" });
      const me = (await api.get("/auth/me")).data;
      setUser(me);
      qc.invalidateQueries({ queryKey: ["me"] });
      setCreating(false);
      setNewName("");
      setIsKid(false);
    } catch { setError("Failed to create profile."); }
  };

  const deleteProfile = async (profileId: string) => {
    if (!confirm("Delete this profile? This will remove all watch history and preferences.")) return;
    try {
      await api.delete(`/auth/profiles/${profileId}`);
      const me = (await api.get("/auth/me")).data;
      setUser(me);
      qc.invalidateQueries({ queryKey: ["me"] });
    } catch { setError("Failed to delete profile."); }
  };

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Profiles</h2>
          <p className="text-sm text-mythra-text-muted mt-1">Separate watch histories and preferences per person.</p>
        </div>
        <button
          onClick={() => setCreating(true)}
          className="inline-flex items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white"
        >
          <Plus size={14} /> New profile
        </button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-xl bg-rose-500/10 border border-rose-500/20 px-3 py-2 text-xs text-rose-300">
          <AlertTriangle size={13} /> {error}
        </div>
      )}

      <AnimatePresence>
        {creating && (
          <motion.div
            initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: "auto" }} exit={{ opacity: 0, height: 0 }}
            className="overflow-hidden rounded-2xl border border-white/[0.06] bg-white/[0.04] p-4"
          >
            <div className="grid gap-3 sm:grid-cols-2">
              <input
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                placeholder="Profile name"
                className="rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm outline-none placeholder:text-white/30"
              />
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" checked={isKid} onChange={(e) => setIsKid(e.target.checked)} className="accent-purple-500" />
                Kid-friendly profile
              </label>
            </div>
            <div className="mt-3 flex gap-2">
              <button onClick={createProfile} disabled={!newName.trim()} className="rounded-full bg-white px-4 py-2 text-xs font-medium text-black disabled:opacity-50">
                Create
              </button>
              <button onClick={() => setCreating(false)} className="rounded-full border border-white/10 px-4 py-2 text-xs">
                Cancel
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {profiles.length === 0 && !creating && (
        <p className="text-sm text-mythra-text-muted">No profiles yet.</p>
      )}

      <ul className="space-y-3">
        {profiles.map((p) => (
          <motion.li key={p.id} layout className="flex items-center gap-3 rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4">
            <div className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-gradient-to-br from-mythra-purple to-mythra-blue text-white text-sm font-bold">
              {p.name[0]?.toUpperCase()}
            </div>
            <div className="flex-1">
              <p className="text-sm font-medium">{p.name}</p>
              <p className="text-xs text-mythra-text-muted">{p.isKidFriendly ? "Kid-friendly" : "Standard"}</p>
            </div>
            <button
              onClick={() => deleteProfile(p.id)}
              className="inline-flex h-7 w-7 items-center justify-center rounded-full border border-rose-500/30 text-rose-400 hover:bg-rose-500/10"
            >
              <Trash2 size={12} />
            </button>
          </motion.li>
        ))}
      </ul>
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// ADDONS SECTION
// ════════════════════════════════════════════════════════════

interface Addon {
  id: string;
  name: string;
  description?: string | null;
  kind: string;
  targetMediaKind: string;
  providerType: string;
  status: string;
  importedFrom?: string | null;
}

function AddonsSection() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const [importing, setImporting] = useState(false);
  const [importError, setImportError] = useState<string | null>(null);
  const qc = useQueryClient();

  const addons = useQuery({
    queryKey: ["addons"],
    queryFn: async () => (await api.get<Addon[]>("/addons")).data,
    enabled: !!accessToken,
  });

  const handleFileImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setImporting(true);
    setImportError(null);
    const formData = new FormData();
    formData.append("file", file);
    try {
      await api.post("/addons/import", formData, { headers: { "Content-Type": "multipart/form-data" } });
      qc.invalidateQueries({ queryKey: ["addons"] });
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { detail?: string } } }).response?.data?.detail;
      setImportError(msg ?? "Import failed. Check the file format.");
    } finally {
      setImporting(false);
      e.target.value = "";
    }
  };

  const exportAddon = async (id: string, name: string) => {
    try {
      const res = await api.get(`/addons/${id}/export`, { responseType: "blob" });
      const url = URL.createObjectURL(new Blob([res.data as BlobPart]));
      const a = document.createElement("a");
      a.href = url;
      a.download = `${name.replace(/\s+/g, "-")}.mythra-addon.json`;
      a.click();
      URL.revokeObjectURL(url);
    } catch { alert("Export failed."); }
  };

  const toggleAddon = async (id: string) => {
    try {
      await api.patch(`/addons/${id}/toggle`);
      qc.invalidateQueries({ queryKey: ["addons"] });
    } catch { alert("Failed to toggle addon."); }
  };

  const deleteAddon = async (id: string, name: string) => {
    if (!confirm(`Remove addon "${name}"?`)) return;
    try {
      await api.delete(`/addons/${id}`);
      qc.invalidateQueries({ queryKey: ["addons"] });
    } catch { alert("Failed to remove addon."); }
  };

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Addons</h2>
          <p className="text-sm text-mythra-text-muted mt-1">
            Import and export reusable media source configurations.
          </p>
        </div>
        <label className="inline-flex cursor-pointer items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white">
          <Upload size={14} /> {importing ? "Importing…" : "Import addon"}
          <input type="file" accept=".json,.mythra-addon.json" onChange={handleFileImport} className="hidden" disabled={importing} />
        </label>
      </div>

      {importError && (
        <div className="flex items-center gap-2 rounded-xl bg-rose-500/10 border border-rose-500/20 px-3 py-2 text-xs text-rose-300">
          <AlertTriangle size={13} /> {importError}
        </div>
      )}

      {addons.isLoading && <p className="text-sm text-mythra-text-muted">Loading addons…</p>}

      {!addons.isLoading && (addons.data?.length ?? 0) === 0 && (
        <div className="rounded-2xl border border-dashed border-white/10 p-8 text-center">
          <Package size={32} className="mx-auto mb-3 opacity-30" />
          <p className="text-sm text-mythra-text-muted">No addons installed.</p>
          <p className="mt-1 text-xs text-mythra-text-soft">
            Import a <code className="rounded bg-white/10 px-1">.mythra-addon.json</code> file shared by another user.
          </p>
        </div>
      )}

      <ul className="space-y-3">
        {(addons.data ?? []).map((addon) => (
          <motion.li key={addon.id} layout className="rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4">
            <div className="flex items-start gap-3">
              <span className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-gradient-to-br from-mythra-purple/40 to-mythra-blue/40 text-white">
                <Package size={15} />
              </span>
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm">{addon.name}</p>
                <p className="text-xs text-mythra-text-muted">
                  {addon.kind} · {addon.targetMediaKind} · {addon.providerType}
                </p>
                {addon.description && <p className="mt-1 text-xs text-mythra-text-soft line-clamp-2">{addon.description}</p>}
                <span className={`mt-1 inline-block rounded-full px-2 py-0.5 text-[10px] font-medium ${
                  addon.status === "Active" ? "bg-emerald-500/15 text-emerald-400" :
                  addon.status === "PendingSecrets" ? "bg-amber-500/15 text-amber-400" :
                  "bg-white/10 text-mythra-text-muted"
                }`}>
                  {addon.status}
                </span>
              </div>
              <div className="flex items-center gap-1.5 shrink-0">
                <button onClick={() => toggleAddon(addon.id)} title="Toggle" className="rounded-full border border-white/10 p-1.5 hover:bg-white/10">
                  {addon.status === "Active" ? <ToggleRight size={14} className="text-emerald-400" /> : <ToggleLeft size={14} />}
                </button>
                <button onClick={() => exportAddon(addon.id, addon.name)} title="Export" className="rounded-full border border-white/10 p-1.5 hover:bg-white/10">
                  <Download size={14} />
                </button>
                <button onClick={() => deleteAddon(addon.id, addon.name)} className="rounded-full border border-rose-500/30 p-1.5 text-rose-400 hover:bg-rose-500/10">
                  <Trash2 size={14} />
                </button>
              </div>
            </div>
          </motion.li>
        ))}
      </ul>
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// LANGUAGE SECTION
// ════════════════════════════════════════════════════════════

function LanguageSection() {
  const user = useAuthStore((s) => s.user);
  const [profileId] = useState<string | null>(user?.profiles?.[0]?.id ?? null);
  const [contentLang, setContentLang] = useState("");
  const [subtitleLang, setSubtitleLang] = useState("");
  const [audioLang, setAudioLang] = useState("");
  const [showOriginal, setShowOriginal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (!profileId) return;
    api.get(`/profiles/${profileId}/language`).then((r) => {
      const d = r.data as { preferredContentLanguage?: string; preferredSubtitleLanguage?: string; preferredAudioLanguage?: string; showOriginalTitle?: boolean };
      setContentLang(d.preferredContentLanguage ?? "");
      setSubtitleLang(d.preferredSubtitleLanguage ?? "");
      setAudioLang(d.preferredAudioLanguage ?? "");
      setShowOriginal(d.showOriginalTitle ?? false);
    }).catch(() => {});
  }, [profileId]);

  const save = async () => {
    if (!profileId) return;
    setSaving(true);
    try {
      await api.patch(`/profiles/${profileId}/language`, {
        preferredContentLanguage: contentLang || null,
        preferredSubtitleLanguage: subtitleLang || null,
        preferredAudioLanguage: audioLang || null,
        showOriginalTitle: showOriginal,
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } finally { setSaving(false); }
  };

  const LANGS = [
    { code: "", label: "System default" },
    { code: "en", label: "English" },
    { code: "pt-BR", label: "Portuguese (Brazil)" },
    { code: "pt", label: "Portuguese" },
    { code: "ja", label: "Japanese" },
    { code: "es", label: "Spanish" },
    { code: "fr", label: "French" },
    { code: "de", label: "German" },
    { code: "ko", label: "Korean" },
    { code: "zh", label: "Chinese" },
  ];

  const LangSelect = ({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) => (
    <div>
      <label className="mb-1 block text-xs text-mythra-text-muted">{label}</label>
      <select value={value} onChange={(e) => onChange(e.target.value)} className="w-full rounded-xl border border-white/10 bg-[#0c0e1a] px-3 py-2 text-sm">
        {LANGS.map((l) => <option key={l.code} value={l.code}>{l.label}</option>)}
      </select>
    </div>
  );

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div>
        <h2 className="text-lg font-semibold">Language Preferences</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">Control metadata display language and default audio/subtitle tracks.</p>
      </div>

      {!profileId && <p className="text-sm text-mythra-text-muted">No profile found. Create a profile first in the Profiles section.</p>}

      {profileId && (
        <>
          <div className="grid gap-4 sm:grid-cols-2">
            <LangSelect label="Metadata language" value={contentLang} onChange={setContentLang} />
            <LangSelect label="Default subtitle language" value={subtitleLang} onChange={setSubtitleLang} />
            <LangSelect label="Default audio language" value={audioLang} onChange={setAudioLang} />
            <div className="flex items-center gap-3 pt-6">
              <input id="show-original" type="checkbox" checked={showOriginal} onChange={(e) => setShowOriginal(e.target.checked)} className="h-4 w-4 rounded border border-white/20 bg-transparent accent-purple-500" />
              <label htmlFor="show-original" className="text-sm text-mythra-text-soft cursor-pointer">Always show original title</label>
            </div>
          </div>
          <button
            onClick={save}
            disabled={saving}
            className="inline-flex items-center gap-1.5 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            {saved ? <><Check size={14} /> Saved!</> : saving ? "Saving…" : "Save preferences"}
          </button>
        </>
      )}
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// PLAYBACK SECTION
// ════════════════════════════════════════════════════════════

function PlaybackSection() {
  const [quality, setQuality] = useState("auto");
  const [speed, setSpeed] = useState("1");
  const [autoSubtitles, setAutoSubtitles] = useState(true);
  const [saved, setSaved] = useState(false);

  const save = () => {
    // Persist to localStorage (backend endpoint can be wired later)
    localStorage.setItem("mythra_playback", JSON.stringify({ quality, speed, autoSubtitles }));
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  useEffect(() => {
    const stored = localStorage.getItem("mythra_playback");
    if (stored) {
      try {
        const { quality: q, speed: s, autoSubtitles: a } = JSON.parse(stored);
        if (q) setQuality(q);
        if (s) setSpeed(s);
        if (typeof a === "boolean") setAutoSubtitles(a);
      } catch { /* ignore */ }
    }
  }, []);

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div>
        <h2 className="text-lg font-semibold">Playback Preferences</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">Set default streaming quality and behavior.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">Default quality</label>
          <select value={quality} onChange={(e) => setQuality(e.target.value)} className="w-full rounded-xl border border-white/10 bg-[#0c0e1a] px-3 py-2 text-sm">
            <option value="auto">Auto (recommended)</option>
            <option value="4k">4K (2160p)</option>
            <option value="1080">1080p</option>
            <option value="720">720p</option>
            <option value="480">480p</option>
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">Default playback speed</label>
          <select value={speed} onChange={(e) => setSpeed(e.target.value)} className="w-full rounded-xl border border-white/10 bg-[#0c0e1a] px-3 py-2 text-sm">
            {["0.5", "0.75", "1", "1.25", "1.5", "1.75", "2"].map((s) => (
              <option key={s} value={s}>{s}×</option>
            ))}
          </select>
        </div>
        <div className="flex items-center gap-3">
          <input id="auto-sub" type="checkbox" checked={autoSubtitles} onChange={(e) => setAutoSubtitles(e.target.checked)} className="h-4 w-4 rounded accent-purple-500" />
          <label htmlFor="auto-sub" className="text-sm text-mythra-text-soft cursor-pointer">Enable subtitles automatically</label>
        </div>
      </div>

      <button
        onClick={save}
        className="inline-flex items-center gap-1.5 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2 text-sm font-medium text-white"
      >
        {saved ? <><Check size={14} /> Saved!</> : "Save preferences"}
      </button>
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// METADATA SECTION
// ════════════════════════════════════════════════════════════

function MetadataSection() {
  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-4">
      <div>
        <h2 className="text-lg font-semibold">Metadata Providers</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">External APIs used to enrich your library with posters, ratings, and descriptions.</p>
      </div>
      {[
        { name: "TMDB", desc: "Movies & TV shows", env: "TMDB_API_KEY", badge: "Video" },
        { name: "AniList", desc: "Anime & Manga", env: "ANILIST_TOKEN", badge: "Anime / Manga" },
        { name: "Google Books", desc: "Books & eBooks", env: "GOOGLE_BOOKS_API_KEY", badge: "Book" },
        { name: "MusicBrainz", desc: "Music & Audiobooks", env: "— (no key needed)", badge: "Audio" },
      ].map((p) => (
        <div key={p.name} className="flex items-center gap-3 rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4">
          <div className="flex-1">
            <p className="text-sm font-medium">{p.name}</p>
            <p className="text-xs text-mythra-text-muted">{p.desc}</p>
          </div>
          <span className="rounded-full bg-white/[0.06] px-2 py-0.5 text-[10px] text-mythra-text-soft">{p.badge}</span>
          <span className="text-xs font-mono text-mythra-text-muted">{p.env}</span>
        </div>
      ))}
      <p className="text-xs text-mythra-text-muted">
        Configure API keys via environment variables in your <code className="rounded bg-white/10 px-1">docker-compose.yml</code> or <code className="rounded bg-white/10 px-1">.env</code> file.
      </p>
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// ACCOUNT SECTION
// ════════════════════════════════════════════════════════════

function AccountSection({ user, onSignOut }: { user: { email?: string; username?: string } | null; onSignOut: () => void }) {
  const setUser = useAuthStore((s) => s.setUser);
  const [username, setUsername] = useState(user?.username ?? "");
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const saveProfile = async () => {
    setSaving(true); setError(null);
    try {
      const res = await api.patch("/auth/me", { username: username.trim() });
      setUser(res.data);
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } catch { setError("Failed to update account."); }
    finally { setSaving(false); }
  };

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div>
        <h2 className="text-lg font-semibold">Account</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">Manage your account details.</p>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-xl bg-rose-500/10 border border-rose-500/20 px-3 py-2 text-xs text-rose-300">
          <AlertTriangle size={13} /> {error}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">Email</label>
          <p className="rounded-xl border border-white/[0.05] bg-white/[0.02] px-3 py-2 text-sm text-mythra-text-soft">{user?.email}</p>
        </div>
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">Username</label>
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className="w-full rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm outline-none"
          />
        </div>
      </div>

      <div className="flex items-center gap-3">
        <button
          onClick={saveProfile}
          disabled={saving || !username.trim()}
          className="inline-flex items-center gap-1.5 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          {saved ? <><Check size={14} /> Saved!</> : saving ? "Saving…" : "Save changes"}
        </button>
        <button
          onClick={onSignOut}
          className="inline-flex items-center gap-2 rounded-full border border-rose-500/30 bg-rose-500/10 px-4 py-2 text-sm text-rose-200 hover:bg-rose-500/20"
        >
          <Trash2 size={14} /> Sign out
        </button>
      </div>
    </div>
  );
}
