"use client";

import { AnimatePresence, motion } from "framer-motion";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Folder, Plus, RefreshCw, Trash2, X, ChevronRight,
  Library, User, Settings2, Play, Globe, Database,
  FolderOpen, Check, Edit2, AlertTriangle, Package,
  Download, Upload, ToggleLeft, ToggleRight, ShieldAlert,
} from "lucide-react";
import { useProfilePrefs } from "@/store/profile";
import { useLocaleStore, useTranslation } from "@/store/locale";
import { LOCALES, type Locale } from "@/lib/i18n";
import { Topbar } from "@/components/shell/Topbar";
import { PageScaffold } from "@/components/shell/PageScaffold";
import { FolderBrowser } from "@/components/settings/FolderBrowser";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/auth";
import type { Library as LibraryType, LibraryFolder, Profile } from "@/lib/types";

type SectionId = "libraries" | "profiles" | "account" | "playback" | "language" | "metadata" | "addons" | "adult";

const SECTION_IDS: SectionId[] = ["libraries", "profiles", "addons", "language", "playback", "metadata", "adult", "account"];
const SECTION_ICONS: Record<SectionId, React.ReactNode> = {
  libraries: <Library size={15} />,
  profiles:  <User size={15} />,
  addons:    <Package size={15} />,
  language:  <Globe size={15} />,
  playback:  <Play size={15} />,
  metadata:  <Database size={15} />,
  adult:     <ShieldAlert size={15} />,
  account:   <Settings2 size={15} />,
};

const LIBRARY_KINDS = ["Video", "Anime", "Manga", "Book", "General"];

export default function SettingsPage() {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);
  const clear = useAuthStore((s) => s.clear);
  const qc = useQueryClient();
  const t = useTranslation();

  const [section, setSection] = useState<SectionId>("libraries");
  const [editingLib, setEditingLib] = useState<LibraryType | null>(null);

  const SECTIONS = SECTION_IDS.map((id) => ({
    id,
    label: t(`settings.nav.${id}` as Parameters<typeof t>[0]),
    icon: SECTION_ICONS[id],
  }));

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
            <span className="gradient-text">{t("settings.title")}</span>
          </h1>
          <p className="text-sm text-mythra-text-muted">{t("settings.subtitle")}</p>
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
                {section === "adult"   && <AdultContentSection />}
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
  const t = useTranslation();

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">{t("settings.libs.title")}</h2>
        <button
          onClick={() => setCreating(true)}
          className="inline-flex items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white"
        >
          <Plus size={14} /> {t("settings.libs.newLib")}
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

      {loading && <p className="mt-6 text-sm text-mythra-text-muted">{t("common.loading")}</p>}

      {!loading && libs.length === 0 && (
        <p className="mt-6 text-sm text-mythra-text-muted">{t("settings.libs.noLibs")}</p>
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
  const t = useTranslation();

  const scan = async () => {
    setScanning(true);
    try { await api.post(`/libraries/${lib.id}/scan`); onRefreshed(); }
    finally { setScanning(false); }
  };

  const del = async () => {
    if (!confirm(t("settings.libs.deleteConfirm", { name: lib.name }))) return;
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
              <span className="ml-2 rounded-full bg-mythra-blue/20 px-2 py-0.5 text-[10px] text-mythra-blue">{t("settings.libs.system")}</span>
            )}
          </p>
          <p className="truncate text-xs text-mythra-text-soft">
            {lib.kind} · {lib.folderCount} folder{lib.folderCount !== 1 ? "s" : ""}
            {lib.lastScannedAt ? ` · scanned ${new Date(lib.lastScannedAt).toLocaleString()}` : ` · ${t("settings.libs.neverScanned")}`}
          </p>
        </div>
        <div className="ml-auto flex items-center gap-1.5">
          <button onClick={() => onEdit(lib)} className="inline-flex items-center gap-1 rounded-full border border-white/10 px-3 py-1.5 text-xs hover:bg-white/10">
            <Edit2 size={12} /> {t("settings.libs.edit")}
          </button>
          <button onClick={scan} disabled={scanning} className="inline-flex items-center gap-1 rounded-full border border-white/10 px-3 py-1.5 text-xs hover:bg-white/10 disabled:opacity-50">
            <RefreshCw size={12} className={scanning ? "animate-spin" : ""} />
            {scanning ? t("settings.libs.scanning") : t("settings.libs.scan")}
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
  const t = useTranslation();

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
          placeholder={t("settings.libs.name")}
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
          placeholder={t("settings.libs.folderPath")}
          className="flex-1 rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm font-mono outline-none placeholder:text-white/30"
        />
        <button
          onClick={() => setBrowsingFolder(true)}
          className="inline-flex items-center gap-1 rounded-xl border border-white/10 px-3 py-2 text-xs hover:bg-white/10"
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
          {submitting ? t("settings.libs.creating") : t("settings.libs.create")}
        </button>
        <button onClick={onCancel} className="rounded-full border border-white/10 px-4 py-2 text-xs">
          {t("action.cancel")}
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
  const [folders, setFolders] = useState<LibraryFolder[]>(lib.folders ?? []);
  const [newPath, setNewPath] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [browsingFolder, setBrowsingFolder] = useState(false);
  const t = useTranslation();

  const addFolder = async () => {
    if (!newPath.trim()) return;
    setError(null);
    try {
      await api.post(`/libraries/${lib.id}/folders`, { path: newPath.trim() });
      setFolders((prev) => [...prev, { id: crypto.randomUUID(), path: newPath.trim(), isActive: true }]);
      setNewPath("");
      onSaved();
    } catch { setError(t("settings.libs.failedAdd")); }
  };

  const removeFolder = async (folderId: string) => {
    setError(null);
    try {
      await api.delete(`/libraries/${lib.id}/folders/${folderId}`);
      setFolders((prev) => prev.filter((f) => f.id !== folderId));
      onSaved();
    } catch { setError(t("settings.libs.failedRemove")); }
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

        {error && (
          <div className="mt-3 flex items-center gap-2 rounded-xl bg-rose-500/10 border border-rose-500/20 px-3 py-2 text-xs text-rose-300">
            <AlertTriangle size={13} /> {error}
          </div>
        )}

        <div className="mt-4 space-y-2">
          {folders.length === 0 && <p className="text-xs text-mythra-text-muted">{t("settings.libs.noFolders")}</p>}
          {folders.map((f) => (
            <div key={f.id} className="flex items-center gap-2 rounded-xl border border-white/[0.05] bg-white/[0.02] px-3 py-2.5">
              <FolderOpen size={13} className="shrink-0 text-mythra-text-muted" />
              <span className="flex-1 truncate text-xs font-mono">{f.path}</span>
              <span className={`text-[10px] ${f.isActive ? "text-emerald-400" : "text-rose-400"}`}>
                {f.isActive ? "active" : "inactive"}
              </span>
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
            >
              <FolderOpen size={13} />
            </button>
            <button
              onClick={addFolder}
              disabled={!newPath.trim()}
              className="rounded-xl bg-white/10 px-3 py-2 text-xs hover:bg-white/15 disabled:opacity-40"
            >
              {t("settings.libs.addFolder")}
            </button>
          </div>
        </div>
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
  const t = useTranslation();

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
    } catch { setError(t("settings.profiles.createFailed")); }
  };

  const deleteProfile = async (profileId: string) => {
    if (!confirm(t("settings.profiles.deleteConfirm"))) return;
    try {
      await api.delete(`/auth/profiles/${profileId}`);
      const me = (await api.get("/auth/me")).data;
      setUser(me);
      qc.invalidateQueries({ queryKey: ["me"] });
    } catch { setError(t("settings.profiles.deleteFailed")); }
  };

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">{t("settings.profiles.title")}</h2>
          <p className="text-sm text-mythra-text-muted mt-1">{t("settings.profiles.subtitle")}</p>
        </div>
        <button
          onClick={() => setCreating(true)}
          className="inline-flex items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white"
        >
          <Plus size={14} /> {t("settings.profiles.newProfile")}
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
                placeholder={t("settings.profiles.namePh")}
                className="rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-sm outline-none placeholder:text-white/30"
              />
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" checked={isKid} onChange={(e) => setIsKid(e.target.checked)} className="accent-purple-500" />
                {t("settings.profiles.kidFriendly")}
              </label>
            </div>
            <div className="mt-3 flex gap-2">
              <button onClick={createProfile} disabled={!newName.trim()} className="rounded-full bg-white px-4 py-2 text-xs font-medium text-black disabled:opacity-50">
                {t("settings.libs.create")}
              </button>
              <button onClick={() => setCreating(false)} className="rounded-full border border-white/10 px-4 py-2 text-xs">
                {t("action.cancel")}
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {profiles.length === 0 && !creating && (
        <p className="text-sm text-mythra-text-muted">{t("settings.profiles.noProfiles")}</p>
      )}

      <ul className="space-y-3">
        {profiles.map((p) => (
          <motion.li key={p.id} layout className="flex items-center gap-3 rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4">
            <div className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-gradient-to-br from-mythra-purple to-mythra-blue text-white text-sm font-bold">
              {p.name[0]?.toUpperCase()}
            </div>
            <div className="flex-1">
              <p className="text-sm font-medium">{p.name}</p>
              <p className="text-xs text-mythra-text-muted">{p.isKidFriendly ? t("settings.profiles.kidLabel") : t("settings.profiles.standard")}</p>
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
  const t = useTranslation();

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
      setImportError(msg ?? t("settings.addons.importFailed"));
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
    } catch { alert(t("settings.addons.exportFailed")); }
  };

  const toggleAddon = async (id: string) => {
    try {
      await api.patch(`/addons/${id}/toggle`);
      qc.invalidateQueries({ queryKey: ["addons"] });
    } catch { alert(t("settings.addons.toggleFailed")); }
  };

  const deleteAddon = async (id: string, name: string) => {
    if (!confirm(t("settings.addons.removeConfirm", { name }))) return;
    try {
      await api.delete(`/addons/${id}`);
      qc.invalidateQueries({ queryKey: ["addons"] });
    } catch { alert(t("settings.addons.removeFailed")); }
  };

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">{t("settings.nav.addons")}</h2>
          <p className="text-sm text-mythra-text-muted mt-1">
            {t("settings.addons.subtitle")}
          </p>
        </div>
        <label className="inline-flex cursor-pointer items-center gap-1 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-2 text-xs font-medium text-white">
          <Upload size={14} /> {importing ? t("action.importing") : t("settings.addons.import")}
          <input type="file" accept=".json,.mythra-addon.json" onChange={handleFileImport} className="hidden" disabled={importing} />
        </label>
      </div>

      {importError && (
        <div className="flex items-center gap-2 rounded-xl bg-rose-500/10 border border-rose-500/20 px-3 py-2 text-xs text-rose-300">
          <AlertTriangle size={13} /> {importError}
        </div>
      )}

      {addons.isLoading && <p className="text-sm text-mythra-text-muted">{t("common.loading")}</p>}

      {!addons.isLoading && (addons.data?.length ?? 0) === 0 && (
        <div className="rounded-2xl border border-dashed border-white/10 p-8 text-center">
          <Package size={32} className="mx-auto mb-3 opacity-30" />
          <p className="text-sm text-mythra-text-muted">{t("settings.addons.empty")}</p>
          <p className="mt-1 text-xs text-mythra-text-soft">
            {t("settings.addons.emptyHint")}
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
  const t = useTranslation();
  const [profileId] = useState<string | null>(user?.profiles?.[0]?.id ?? null);
  const [contentLang, setContentLang] = useState("");
  const [subtitleLang, setSubtitleLang] = useState("");
  const [audioLang, setAudioLang] = useState("");
  const [showOriginal, setShowOriginal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const { locale, setLocale } = useLocaleStore();

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

  const MEDIA_LANGS = [
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
        {MEDIA_LANGS.map((l) => <option key={l.code} value={l.code}>{l.label}</option>)}
      </select>
    </div>
  );

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      {/* App interface language */}
      <div>
        <h2 className="text-lg font-semibold">{t("settings.interfaceLang")}</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">{t("settings.interfaceLang.body")}</p>
      </div>

      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-5">
        {LOCALES.map((loc) => (
          <button
            key={loc.code}
            onClick={() => setLocale(loc.code as Locale)}
            className={
              "flex flex-col items-center gap-1.5 rounded-2xl border p-3 text-sm font-medium transition-all " +
              (locale === loc.code
                ? "border-mythra-purple/50 bg-mythra-purple/10 text-white"
                : "border-white/[0.05] bg-white/[0.02] text-mythra-text-muted hover:border-white/10 hover:text-white")
            }
          >
            <span className="text-xl">{loc.flag}</span>
            <span className="text-xs">{loc.label}</span>
            {locale === loc.code && (
              <span className="h-1 w-1 rounded-full bg-mythra-purple" />
            )}
          </button>
        ))}
      </div>

      <div className="h-px bg-white/[0.06]" />

      {/* Content language preferences (server-side) */}
      <div>
        <h2 className="text-lg font-semibold">{t("settings.contentLang")}</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">{t("settings.contentLang.body")}</p>
      </div>

      {!profileId && <p className="text-sm text-mythra-text-muted">{t("settings.contentLang.noProfile")}</p>}

      {profileId && (
        <>
          <div className="grid gap-4 sm:grid-cols-2">
            <LangSelect label={t("settings.contentLang.metadata")} value={contentLang} onChange={setContentLang} />
            <LangSelect label={t("settings.contentLang.subtitles")} value={subtitleLang} onChange={setSubtitleLang} />
            <LangSelect label={t("settings.contentLang.audio")} value={audioLang} onChange={setAudioLang} />
            <div className="flex items-center gap-3 pt-6">
              <input id="show-original" type="checkbox" checked={showOriginal} onChange={(e) => setShowOriginal(e.target.checked)} className="h-4 w-4 rounded border border-white/20 bg-transparent accent-purple-500" />
              <label htmlFor="show-original" className="text-sm text-mythra-text-soft cursor-pointer">{t("settings.contentLang.original")}</label>
            </div>
          </div>
          <button
            onClick={save}
            disabled={saving}
            className="inline-flex items-center gap-1.5 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            {saved ? <><Check size={14} /> {t("settings.contentLang.saved")}</> : saving ? t("settings.contentLang.saving") : t("settings.contentLang.save")}
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
  const t = useTranslation();

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
        <h2 className="text-lg font-semibold">{t("settings.playback.title")}</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">{t("settings.playback.subtitle")}</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">{t("settings.playback.quality")}</label>
          <select value={quality} onChange={(e) => setQuality(e.target.value)} className="w-full rounded-xl border border-white/10 bg-[#0c0e1a] px-3 py-2 text-sm">
            <option value="auto">Auto (recommended)</option>
            <option value="4k">4K (2160p)</option>
            <option value="1080">1080p</option>
            <option value="720">720p</option>
            <option value="480">480p</option>
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">{t("settings.playback.speed")}</label>
          <select value={speed} onChange={(e) => setSpeed(e.target.value)} className="w-full rounded-xl border border-white/10 bg-[#0c0e1a] px-3 py-2 text-sm">
            {["0.5", "0.75", "1", "1.25", "1.5", "1.75", "2"].map((s) => (
              <option key={s} value={s}>{s}×</option>
            ))}
          </select>
        </div>
        <div className="flex items-center gap-3">
          <input id="auto-sub" type="checkbox" checked={autoSubtitles} onChange={(e) => setAutoSubtitles(e.target.checked)} className="h-4 w-4 rounded accent-purple-500" />
          <label htmlFor="auto-sub" className="text-sm text-mythra-text-soft cursor-pointer">{t("settings.playback.autoSubs")}</label>
        </div>
      </div>

      <button
        onClick={save}
        className="inline-flex items-center gap-1.5 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2 text-sm font-medium text-white"
      >
        {saved ? <><Check size={14} /> Saved!</> : t("action.save")}
      </button>
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// METADATA SECTION
// ════════════════════════════════════════════════════════════

function MetadataSection() {
  const t = useTranslation();
  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-4">
      <div>
        <h2 className="text-lg font-semibold">{t("settings.metadata.title")}</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">{t("settings.metadata.subtitle")}</p>
      </div>
      {[
        { name: "TMDB", desc: "Movies & TV shows", env: "Metadata__TmdbApiKey", badge: "Video" },
        { name: "AniList", desc: "Anime & Manga", env: "— (no key needed)", badge: "Anime / Manga" },
        { name: "Open Library", desc: "Books & eBooks", env: "— (no key needed)", badge: "Book" },
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
        {t("settings.metadata.hint")}
      </p>
    </div>
  );
}

// ════════════════════════════════════════════════════════════
// ADULT CONTENT SECTION
// ════════════════════════════════════════════════════════════

function AdultContentSection() {
  const { showAdultContent, setShowAdultContent, isHydrated } = useProfilePrefs();
  const t = useTranslation();
  const mounted = isHydrated;

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div>
        <h2 className="text-lg font-semibold flex items-center gap-2">
          <ShieldAlert size={18} className="text-red-400" />
          {t("settings.nav.adult")}
        </h2>
        <p className="mt-1 text-sm text-mythra-text-muted">
          {t("adult.subtitle")}
        </p>
      </div>

      <div className="flex items-center gap-4 rounded-2xl border border-white/[0.05] bg-white/[0.02] p-4">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium">{t("settings.adult.show")}</p>
          <p className="mt-0.5 text-xs text-mythra-text-muted">
            {t("settings.adult.showDesc")}
          </p>
        </div>
        <button
          onClick={() => setShowAdultContent(!showAdultContent)}
          disabled={!mounted}
          aria-pressed={mounted && showAdultContent}
          className={
            "relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent " +
            "transition-colors duration-200 focus-visible:outline-none disabled:opacity-60 " +
            (mounted && showAdultContent ? "bg-red-500" : "bg-white/20")
          }
        >
          <span
            className={
              "pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 " +
              (mounted && showAdultContent ? "translate-x-5" : "translate-x-0")
            }
          />
        </button>
      </div>

      {mounted && showAdultContent && (
        <motion.div
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: "auto" }}
          className="overflow-hidden rounded-2xl border border-red-500/20 bg-red-500/[0.05] p-4"
        >
          <div className="flex items-start gap-3">
            <ShieldAlert size={16} className="mt-0.5 shrink-0 text-red-400" />
            <div>
              <p className="text-sm font-medium text-red-300">{t("settings.adult.enabled")}</p>
              <p className="mt-0.5 text-xs text-mythra-text-muted">
                {t("settings.adult.enabledDesc")}
              </p>
            </div>
          </div>
        </motion.div>
      )}
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
  const t = useTranslation();

  const saveProfile = async () => {
    setSaving(true); setError(null);
    try {
      const res = await api.patch("/auth/me", { username: username.trim() });
      setUser(res.data);
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } catch { setError(t("settings.account.failed")); }
    finally { setSaving(false); }
  };

  return (
    <div className="rounded-3xl border border-white/[0.06] bg-white/[0.03] p-6 backdrop-blur space-y-6">
      <div>
        <h2 className="text-lg font-semibold">{t("settings.account")}</h2>
        <p className="mt-1 text-sm text-mythra-text-muted">{t("settings.account.subtitle")}</p>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-xl bg-rose-500/10 border border-rose-500/20 px-3 py-2 text-xs text-rose-300">
          <AlertTriangle size={13} /> {error}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">{t("settings.account.email")}</label>
          <p className="rounded-xl border border-white/[0.05] bg-white/[0.02] px-3 py-2 text-sm text-mythra-text-soft">{user?.email}</p>
        </div>
        <div>
          <label className="mb-1 block text-xs text-mythra-text-muted">{t("settings.account.username")}</label>
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
          {saved ? <><Check size={14} /> Saved!</> : saving ? t("settings.libs.saving") : t("settings.account.save")}
        </button>
        <button
          onClick={onSignOut}
          className="inline-flex items-center gap-2 rounded-full border border-rose-500/30 bg-rose-500/10 px-4 py-2 text-sm text-rose-200 hover:bg-rose-500/20"
        >
          <Trash2 size={14} /> {t("settings.account.signOut")}
        </button>
      </div>
    </div>
  );
}
