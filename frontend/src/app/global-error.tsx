"use client";

import { useEffect } from "react";

/**
 * Last-resort error boundary — fires when the root layout itself throws.
 * Must render its own <html>/<body> per Next.js docs because the layout never
 * mounted. Keep it minimal: no providers, no Tailwind utilities that depend on
 * globals.css being loaded successfully.
 */
export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    // eslint-disable-next-line no-console
    console.error("[mythra] root error", error);
  }, [error]);

  return (
    <html lang="en">
      <body
        style={{
          fontFamily: "system-ui, -apple-system, sans-serif",
          background: "#06070d",
          color: "#e7e9ff",
          margin: 0,
          minHeight: "100vh",
          display: "grid",
          placeItems: "center",
          padding: "2rem",
        }}
      >
        <div style={{ maxWidth: "28rem", textAlign: "center" }}>
          <h1 style={{ fontSize: "1.5rem", fontWeight: 700 }}>Mythra crashed</h1>
          <p style={{ marginTop: "0.75rem", color: "#9ca3c4", fontSize: "0.9rem" }}>
            The application failed to load. Refresh to retry.
          </p>
          {error?.digest && (
            <p style={{ marginTop: "1rem", fontFamily: "monospace", fontSize: "0.75rem", color: "#6b7397" }}>
              ref: {error.digest}
            </p>
          )}
          <button
            onClick={reset}
            style={{
              marginTop: "1.5rem",
              padding: "0.65rem 1.5rem",
              borderRadius: "999px",
              background: "linear-gradient(90deg,#a855f7,#3b82f6)",
              color: "#fff",
              border: "none",
              fontWeight: 600,
              cursor: "pointer",
            }}
          >
            Try again
          </button>
        </div>
      </body>
    </html>
  );
}
