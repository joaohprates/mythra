"use client";

import { AnimatePresence, motion } from "framer-motion";
import { ChevronRight, Folder, FolderOpen, Home, Loader2, X } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { api } from "@/lib/api";

interface DirEntry {
  name: string;
  path: string;
  isReadable: boolean;
}

interface BrowseResult {
  current: string;
  parent: string | null;
  entries: DirEntry[];
}

interface Props {
  initialPath?: string;
  onSelect: (path: string) => void;
  onClose: () => void;
}

export function FolderBrowser({ initialPath = "/", onSelect, onClose }: Props) {
  const [current, setCurrent] = useState(initialPath);
  const [data, setData] = useState<BrowseResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [manualPath, setManualPath] = useState(initialPath);

  const browse = useCallback(async (path: string) => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.get<BrowseResult>("/filesystem/browse", { params: { path } });
      setData(res.data);
      setCurrent(res.data.current);
      setManualPath(res.data.current);
    } catch {
      setError(`Cannot access: ${path}`);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { browse(initialPath); }, [browse, initialPath]);

  const handleManualSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    browse(manualPath);
  };

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-[60] flex items-center justify-center bg-black/70 backdrop-blur-sm p-4"
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <motion.div
        initial={{ scale: 0.94, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        exit={{ scale: 0.94, opacity: 0 }}
        className="w-full max-w-lg rounded-3xl border border-white/[0.08] bg-[#0c0e1a] shadow-2xl flex flex-col"
        style={{ maxHeight: "80vh" }}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-5 pt-5 pb-3 border-b border-white/[0.06]">
          <div className="flex items-center gap-2">
            <FolderOpen size={18} className="text-mythra-purple" />
            <h3 className="font-semibold text-sm">Browse Folders</h3>
          </div>
          <button onClick={onClose} className="rounded-full p-1.5 hover:bg-white/10">
            <X size={15} />
          </button>
        </div>

        {/* Path bar */}
        <form onSubmit={handleManualSubmit} className="flex gap-2 px-4 py-3 border-b border-white/[0.06]">
          <input
            value={manualPath}
            onChange={(e) => setManualPath(e.target.value)}
            className="flex-1 rounded-xl border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-mono outline-none"
            placeholder="/path/to/folder"
          />
          <button
            type="submit"
            className="rounded-xl bg-white/10 px-3 py-1.5 text-xs hover:bg-white/15"
          >
            Go
          </button>
          {data?.parent && (
            <button
              type="button"
              onClick={() => browse(data.parent!)}
              className="rounded-xl bg-white/10 px-3 py-1.5 text-xs hover:bg-white/15"
            >
              ↑ Up
            </button>
          )}
          <button
            type="button"
            onClick={() => browse("/")}
            className="rounded-xl bg-white/10 px-2.5 py-1.5 text-xs hover:bg-white/15"
            title="Go to root"
          >
            <Home size={13} />
          </button>
        </form>

        {/* Directory listing */}
        <div className="flex-1 overflow-y-auto px-3 py-2 min-h-0">
          {loading && (
            <div className="flex items-center justify-center py-10">
              <Loader2 size={18} className="animate-spin text-mythra-text-muted" />
            </div>
          )}
          {error && (
            <p className="py-4 text-center text-xs text-rose-400">{error}</p>
          )}
          {!loading && !error && data && (
            <>
              {data.entries.length === 0 && (
                <p className="py-4 text-center text-xs text-mythra-text-muted">Empty directory</p>
              )}
              <ul className="space-y-0.5">
                {data.entries.map((entry) => (
                  <li key={entry.path}>
                    <button
                      onClick={() => entry.isReadable && browse(entry.path)}
                      disabled={!entry.isReadable}
                      className={
                        "flex w-full items-center gap-2 rounded-xl px-3 py-2 text-left text-xs transition-colors " +
                        (entry.isReadable
                          ? "hover:bg-white/[0.06] cursor-pointer"
                          : "opacity-40 cursor-not-allowed")
                      }
                    >
                      <Folder size={14} className="shrink-0 text-mythra-text-muted" />
                      <span className="flex-1 truncate font-mono">{entry.name}</span>
                      {entry.isReadable && <ChevronRight size={12} className="shrink-0 text-mythra-text-muted" />}
                    </button>
                  </li>
                ))}
              </ul>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between gap-3 border-t border-white/[0.06] px-5 py-4">
          <span className="truncate text-xs font-mono text-mythra-text-muted">{current}</span>
          <div className="flex gap-2 shrink-0">
            <button onClick={onClose} className="rounded-full border border-white/10 px-4 py-1.5 text-xs">
              Cancel
            </button>
            <button
              onClick={() => { onSelect(current); onClose(); }}
              className="rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-4 py-1.5 text-xs font-medium text-white"
            >
              Select
            </button>
          </div>
        </div>
      </motion.div>
    </motion.div>
  );
}
