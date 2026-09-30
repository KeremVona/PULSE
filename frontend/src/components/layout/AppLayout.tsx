import React from "react";
import { CdssBanner } from "@/components/common/CdssBanner";
import { Header } from "@/components/layout/Header";
import { Sidebar, NavigationTab } from "@/components/layout/Sidebar";
import { ToastContainer } from "@/components/common/ToastContainer";

interface AppLayoutProps {
  activeTab: NavigationTab;
  onTabChange: (tab: NavigationTab) => void;
  children: React.ReactNode;
}

export const AppLayout: React.FC<AppLayoutProps> = ({
  activeTab,
  onTabChange,
  children,
}) => {
  return (
    <div className="min-h-screen flex flex-col bg-slate-50 text-slate-900">
      {/* 1. Regulatory Non-Dismissible CDSS Advisory Banner */}
      <CdssBanner />

      {/* 2. Main Portal Header */}
      <Header />

      {/* 3. Main Workspace Shell (Sidebar + Main Workspace) */}
      <div className="flex-1 flex overflow-hidden">
        <Sidebar activeTab={activeTab} onTabChange={onTabChange} />

        <main className="flex-1 overflow-y-auto p-6 lg:p-8 bg-slate-50/80">
          <div className="max-w-7xl mx-auto space-y-6">{children}</div>
        </main>
      </div>

      {/* 4. Global Alert / RFC 7807 Error Toast System */}
      <ToastContainer />
    </div>
  );
};
