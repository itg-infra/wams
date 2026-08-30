import type { ReactNode } from "react";
import clsx from "clsx";

interface TabItem {
  key: string;
  label: string;
}

interface WorkOrderTabsProps {
  tabs: TabItem[];
  activeTab: string;
  onChange: (tab: string) => void;
  children: ReactNode;
}

export default function WorkOrderTabs({
  tabs,
  activeTab,
  onChange,
  children,
}: WorkOrderTabsProps) {
  return (
    <div className="w-full">
      {/* Tabs */}
      <div className="relative flex pl-6">
        {tabs.map((tab, index) => {
          const active = activeTab === tab.key;

          return (
            <button
              key={tab.key}
              onClick={() => onChange(tab.key)}
              style={{
                marginLeft: index === 0 ? 0 : -18,
              }}
              className={clsx(
                "relative rounded-t-[32px] px-14 font-semibold",
                active
                  ? "z-20 h-[68px] bg-[#D9E1EF]"
                  : "z-10 h-[56px] bg-[#DCDCDC]",
              )}
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* Content */}
      <div
        className="
          relative
          z-0
          -mt-0.5
          rounded-[18px]
          border
          border-[#B9C7DA]
          bg-[#D9E1EF]
          p-6
        "
      >
        {children}
      </div>
    </div>
  );
}
