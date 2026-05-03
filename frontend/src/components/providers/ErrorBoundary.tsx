"use client";

import React from "react";
import { AlertTriangle, RefreshCw } from "lucide-react";

interface Props {
  children: React.ReactNode;
}
interface State {
  error: Error | null;
}

/**
 * Class-based error boundary used as the outermost UI guard. Anything that
 * escapes a render path lands here instead of unmounting the whole tree.
 *
 * Note: render errors during Server Component streaming go through
 * `app/error.tsx` and `app/global-error.tsx` instead — this boundary only
 * catches client-component render/effect errors.
 */
export class ErrorBoundary extends React.Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    // eslint-disable-next-line no-console
    console.error("[mythra] client error", error, info.componentStack);
  }

  reset = () => this.setState({ error: null });

  render() {
    if (this.state.error) {
      return (
        <main className="grid min-h-[80vh] place-items-center px-6">
          <div className="max-w-md text-center">
            <div className="mx-auto mb-6 grid h-20 w-20 place-items-center rounded-full border border-red-500/20 bg-red-500/10 text-red-400">
              <AlertTriangle size={36} />
            </div>
            <h1 className="text-2xl font-bold text-white">Mythra hit a snag</h1>
            <p className="mt-3 text-sm text-mythra-text-muted">
              A client-side error was caught before it could crash the app. You can keep using the rest of Mythra.
            </p>
            <p className="mt-3 text-[11px] text-mythra-text-soft font-mono break-all">
              {this.state.error.message}
            </p>
            <button
              onClick={this.reset}
              className="mt-6 inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-mythra-purple to-mythra-blue px-5 py-2.5 text-sm font-medium text-white"
            >
              <RefreshCw size={14} /> Dismiss
            </button>
          </div>
        </main>
      );
    }
    return this.props.children;
  }
}
