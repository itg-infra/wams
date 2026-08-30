// import { useState } from "react";
// import { ChevronDown} from "lucide-react";

// // export interface SidebarSubItem {
// //     id: string;
// //     label: string;
// //     permission?: string;
// // }

// export interface SidebarSubItem {
//   id: string;
//   label: string;
//   permission?: string;
//   children?: SidebarSubItem[];
// }

// export interface SidebarItem {
//     id: string;
//     label: string;
//     icon: React.ReactNode;
//     children?: SidebarSubItem[];
//     permission?: string;
// }

// interface SidebarProps {
//     items: SidebarItem[];
//     activePage: string;
//     onNavigate: (id: string) => void;
//     bottomItems?: React.ReactNode;
//     header?: React.ReactNode;

//     collapsed?: boolean;
//     onToggleCollapse?: () => void;
// }

// // ─── Sidebar ──────────────────────────────────────────────────────────────────
// export default function Sidebar({
//   items,
//   activePage,
//   onNavigate,
//   bottomItems,
//   header,
//   collapsed = false,
// }: SidebarProps) {
//   const [openGroups, setOpenGroups] = useState<Record<string, boolean>>(() => {
//     const initial: Record<string, boolean> = {};
//     items.forEach((item) => {
//       if (item.children?.some((c) => c.id === activePage)) {
//         initial[item.id] = true;
//       }
//     });
//     return initial;
//   });

//   const toggleGroup = (id: string) =>
//     setOpenGroups((prev) => ({ ...prev, [id]: !prev[id] }));

//   const isActive = (id: string) => activePage === id;
//   const isParentActive = (item: SidebarItem) =>
//     item.children?.some((c) => c.id === activePage) ?? false;

//   return (
//     <aside
//       className={`
//         ${collapsed ? "w-20" : "w-56"}
//         bg-white border-r flex flex-col h-screen sticky top-0 shrink-0
//         transition-all duration-300
//     `}
//     >
//       {/* Header slot */}
//       {header && (
//         <div className="flex items-center justify-center px-4 py-5 border-b">
//           {header}
//         </div>
//       )}

//       {/* Nav */}
//       <nav className="flex-1 overflow-y-auto px-3 py-3 space-y-0.5">
//         {items.map((item) => {
//           const hasChildren = (item.children?.length ?? 0) > 0;
//           const isOpen = openGroups[item.id] ?? false;
//           const parentActive = isParentActive(item);

//           return (
//             <div key={item.id}>
//               {/* Parent row */}
//               <button
//                 onClick={() =>
//                   hasChildren ? toggleGroup(item.id) : onNavigate(item.id)
//                 }
//                 className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm transition-all duration-150 group
//         ${
//           !hasChildren && isActive(item.id)
//             ? "bg-indigo-50 text-indigo-700 font-medium"
//             : parentActive
//               ? "text-gray-800 font-medium"
//               : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
//         }
//     `}
//               >
//                 <span
//                   className={`shrink-0 ${
//                     (!hasChildren && isActive(item.id)) || parentActive
//                       ? "text-indigo-600"
//                       : "text-gray-400 group-hover:text-gray-500"
//                   }`}
//                 >
//                   {item.icon}
//                 </span>

//                 {!collapsed && (
//                   <>
//                     <span className="flex-1 text-left leading-none">
//                       {item.label}
//                     </span>

//                     {hasChildren && (
//                       <ChevronDown
//                         className={`w-4 h-4 shrink-0 text-gray-400 transition-transform duration-200 ${
//                           isOpen ? "rotate-180" : ""
//                         }`}
//                       />
//                     )}
//                   </>
//                 )}
//               </button>

//               {/* Submenu */}
//               {hasChildren && !collapsed && (
//                 <div
//                   className={`overflow-hidden transition-all duration-200 ease-in-out ${isOpen ? "max-h-96" : "max-h-0"}`}
//                 >
//                   <div className="mt-0.5 mb-1 space-y-0.5">
//                     {item.children!.map((child) => (
//                       <button
//                         key={child.id}
//                         onClick={() => onNavigate(child.id)}
//                         className={`w-full text-left pl-9 pr-3 py-2 rounded-lg text-sm transition-all duration-150
//                                                     ${
//                                                       isActive(child.id)
//                                                         ? "bg-indigo-50 text-indigo-700 font-medium"
//                                                         : "text-gray-500 hover:bg-gray-50 hover:text-gray-800"
//                                                     }`}
//                       >
//                         {child.label}
//                       </button>
//                     ))}
//                   </div>
//                 </div>
//               )}
//             </div>
//           );
//         })}
//       </nav>

//       {/* Bottom slot */}
//       {bottomItems && (
//         <div className="px-3 pb-4 pt-2 border-t">{bottomItems}</div>
//       )}
//     </aside>
//   );
// }

import { useMemo, useState } from "react";
import { ChevronDown } from "lucide-react";

export interface SidebarSubItem {
  id: string;
  label: string;
  permission?: string;
  children?: SidebarSubItem[];
  /** DOM id for this menu button, e.g. "trm_MenuMasterUser". */
  elementId?: string;
}

export interface SidebarItem {
  id: string;
  label: string;
  icon: React.ReactNode;
  children?: SidebarSubItem[];
  permission?: string;
  /** DOM id for this menu button, e.g. "trm_MenuMasterData". */
  elementId?: string;
}

interface SidebarProps {
  items: SidebarItem[];
  activePage: string;
  onNavigate: (id: string) => void;
  bottomItems?: React.ReactNode;
  header?: React.ReactNode;

  collapsed?: boolean;
  onToggleCollapse?: () => void;
}

interface SidebarSubmenuProps {
  items: SidebarSubItem[];
  activePage: string;
  onNavigate: (id: string) => void;
  level?: number;
}

function SidebarSubmenu({
  items,
  activePage,
  onNavigate,
  level = 1,
}: SidebarSubmenuProps) {
  const initialOpen = useMemo(() => {
    const result: Record<string, boolean> = {};

    const walk = (nodes: SidebarSubItem[]): boolean => {
      let hasActive = false;

      nodes.forEach((node) => {
        let childActive = false;

        if (node.children?.length) {
          childActive = walk(node.children);
          if (childActive) result[node.id] = true;
        }

        if (node.id === activePage || childActive) {
          hasActive = true;
        }
      });

      return hasActive;
    };

    walk(items);

    return result;
  }, [items, activePage]);

  const [openGroups, setOpenGroups] =
    useState<Record<string, boolean>>(initialOpen);

  const toggleGroup = (id: string) =>
    setOpenGroups((prev) => ({ ...prev, [id]: !prev[id] }));

  const isActive = (id: string) => activePage === id;

  const isParentActive = (item: SidebarSubItem): boolean => {
    if (!item.children) return false;

    return item.children.some(
      (child) =>
        child.id === activePage ||
        (child.children?.length ? isParentActive(child) : false),
    );
  };

  return (
    <>
      {items.map((child) => {
        const hasChildren = (child.children?.length ?? 0) > 0;
        const isOpen = openGroups[child.id] ?? false;
        const parentActive = isParentActive(child);

        return (
          <div key={child.id}>
            <button
              id={child.elementId}
              onClick={() =>
                hasChildren ? toggleGroup(child.id) : onNavigate(child.id)
              }
              className={`w-full flex items-center rounded-lg text-sm transition-all duration-150
              ${
                isActive(child.id)
                  ? "bg-[#EDF3FF] text-[#2B2469] font-medium"
                  : parentActive
                    ? "text-[#2B2469] font-medium"
                    : "text-gray-500 hover:bg-gray-50 hover:text-gray-800"
              }`}
              style={{
                paddingLeft: `${level * 24 + 12}px`,
                paddingRight: "12px",
                paddingTop: "8px",
                paddingBottom: "8px",
              }}
            >
              <span className="flex-1 text-left">{child.label}</span>

              {hasChildren && (
                <ChevronDown
                  className={`w-4 h-4 shrink-0 text-gray-400 transition-transform duration-200 ${
                    isOpen ? "rotate-180" : ""
                  }`}
                />
              )}
            </button>

            {hasChildren && (
              <div
                className={`overflow-hidden transition-all duration-200 ease-in-out ${
                  isOpen ? "max-h-96" : "max-h-0"
                }`}
              >
                <div className="mt-0.5 mb-1 space-y-0.5">
                  <SidebarSubmenu
                    items={child.children!}
                    activePage={activePage}
                    onNavigate={onNavigate}
                    level={level + 1}
                  />
                </div>
              </div>
            )}
          </div>
        );
      })}
    </>
  );
}

// ─── Sidebar ──────────────────────────────────────────────────────────────────

export default function Sidebar({
  items,
  activePage,
  onNavigate,
  bottomItems,
  header,
  collapsed = false,
}: SidebarProps) {
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {};

    const hasActive = (children?: SidebarSubItem[]): boolean => {
      if (!children) return false;

      return children.some(
        (child) =>
          child.id === activePage ||
          (child.children?.length ? hasActive(child.children) : false),
      );
    };

    items.forEach((item) => {
      if (hasActive(item.children)) {
        initial[item.id] = true;
      }
    });

    return initial;
  });

  const toggleGroup = (id: string) =>
    setOpenGroups((prev) => ({ ...prev, [id]: !prev[id] }));

  const isActive = (id: string) => activePage === id;

  const isParentActive = (item: SidebarItem): boolean => {
    const hasActive = (children?: SidebarSubItem[]): boolean => {
      if (!children) return false;

      return children.some(
        (child) =>
          child.id === activePage ||
          (child.children?.length ? hasActive(child.children) : false),
      );
    };

    return hasActive(item.children);
  };

  return (
    <aside
      className={`
        ${collapsed ? "w-20" : "w-56"}
        bg-white border-r flex flex-col h-screen sticky top-0 shrink-0
        transition-all duration-300
      `}
    >
      {header && (
        <div className="flex items-center justify-center px-4 py-5 border-b">
          {header}
        </div>
      )}

      <nav className="flex-1 overflow-y-auto px-3 py-3 space-y-0.5">
        {items.map((item) => {
          const hasChildren = (item.children?.length ?? 0) > 0;
          const isOpen = openGroups[item.id] ?? false;
          const parentActive = isParentActive(item);

          return (
            <div key={item.id}>
              <button
                id={item.elementId}
                onClick={() =>
                  hasChildren ? toggleGroup(item.id) : onNavigate(item.id)
                }
                className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-sm text-sm transition-all duration-150 group
                  ${
                    (!hasChildren && isActive(item.id)) || parentActive
                      ? "bg-[#2B2469] text-white font-medium"
                      : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
                  }`}
              >
                <span
                  className={`shrink-0 ${
                    (!hasChildren && isActive(item.id)) || parentActive
                      ? "text-white [&_img]:brightness-0 [&_img]:invert"
                      : "text-black group-hover:text-gray-500"
                  }`}
                >
                  {item.icon}
                </span>

                {!collapsed && (
                  <>
                    <span className="flex-1 text-left leading-none">
                      {item.label}
                    </span>

                    {hasChildren && (
                      <ChevronDown
                        className={`w-4 h-4 shrink-0 transition-transform duration-200 ${
                          (!hasChildren && isActive(item.id)) || parentActive
                            ? "text-white"
                            : "text-gray-400"
                        } ${isOpen ? "rotate-180" : ""}`}
                      />
                    )}
                  </>
                )}
              </button>

              {hasChildren && !collapsed && (
                <div
                  className={`overflow-hidden transition-all duration-200 ease-in-out ${
                    isOpen ? "max-h-96" : "max-h-0"
                  }`}
                >
                  <div className="mt-0.5 mb-1 space-y-0.5">
                    <SidebarSubmenu
                      items={item.children!}
                      activePage={activePage}
                      onNavigate={onNavigate}
                    />
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </nav>

      {bottomItems && (
        <div className="px-3 pb-4 pt-2 border-t">{bottomItems}</div>
      )}
    </aside>
  );
}