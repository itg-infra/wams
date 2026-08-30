import { useLayoutEffect, useState } from "react";
import { useWarehouseController } from "../controllers/masterData/warehouseListController";
import { AlertCircle, ChevronDown, Loader2, MapPin, Package, Search } from "lucide-react";
import { createPortal } from "react-dom";

export function WarehouseSelector() {
  const {
    warehouses,
    selectedWarehouse,
    isLoading,
    error,
    isDropdownOpen,
    localSearch,
    hasMore,
    triggerRef,
    portalRef,
    handleToggleDropdown,
    handleSelectWarehouse,
    handleSearchChange,
    handleLoadMore,
  } = useWarehouseController();

  const [dropdownPos, setDropdownPos] = useState({
    top: 0,
    left: 0,
    width: 288,
  });

  useLayoutEffect(() => {
    if (!isDropdownOpen || !triggerRef.current) return;

    const updatePos = () => {
      const rect = triggerRef.current!.getBoundingClientRect();
      setDropdownPos({
        top: rect.bottom + 8,
        left: rect.left,
        width: 288,
      });
    };

    updatePos();
    window.addEventListener("resize", updatePos);
    window.addEventListener("scroll", updatePos, true);
    return () => {
      window.removeEventListener("resize", updatePos);
      window.removeEventListener("scroll", updatePos, true);
    };
  }, [isDropdownOpen, triggerRef]);

  const dropdownContent = (
    <div
      ref={portalRef}
      className="bg-white border border-gray-100 rounded-2xl shadow-xl overflow-hidden"
      style={{
        position: "fixed",
        top: dropdownPos.top,
        left: dropdownPos.left,
        width: dropdownPos.width,
        zIndex: 99999,
      }}
    >
      {/* Search */}
      <div className="p-3 border-b border-gray-100">
        <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
          <Search className="w-3.5 h-3.5 text-gray-400 shrink-0" />
          <input
            id="txt_SearchWarehouse"
            autoFocus
            type="text"
            value={localSearch}
            onChange={(e) => handleSearchChange(e.target.value)}
            placeholder="Search warehouse..."
            className="bg-transparent text-sm text-gray-700 placeholder-gray-400 outline-none w-full"
          />
        </div>
      </div>

      {/* List */}
      <div className="max-h-64 overflow-y-auto">
        {error && (
          <div className="flex items-center gap-2 px-4 py-3 text-sm text-red-500">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {isLoading && warehouses.length === 0 && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="w-5 h-5 text-indigo-500 animate-spin" />
          </div>
        )}

        {!isLoading && !error && warehouses.length === 0 && (
          <div className="px-4 py-6 text-center text-sm text-gray-400">
            No warehouse found
          </div>
        )}

        {warehouses.map((warehouse) => {
          const isSelected = selectedWarehouse?.id === warehouse.id;
          return (
            <button
              key={warehouse.id}
              onClick={() => handleSelectWarehouse(warehouse)}
              className={`w-full flex items-start gap-3 px-4 py-3 text-left hover:bg-gray-50 transition ${isSelected ? "bg-indigo-50" : ""}`}
            >
              <div
                className={`w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 ${isSelected ? "bg-indigo-100" : "bg-gray-100"}`}
              >
                <Package
                  className={`w-4 h-4 ${isSelected ? "text-indigo-600" : "text-gray-400"}`}
                />
              </div>
              <div className="flex-1 min-w-0">
                <p
                  className={`text-sm font-medium truncate ${isSelected ? "text-indigo-700" : "text-gray-700"}`}
                >
                  {warehouse.name}
                </p>
                <div className="flex items-center gap-1 mt-0.5">
                  <MapPin className="w-3 h-3 text-gray-400 shrink-0" />
                  <span className="text-xs text-gray-400 truncate">
                    {warehouse.location}
                  </span>
                </div>
              </div>
              {isSelected && (
                <div className="w-2 h-2 rounded-full bg-indigo-500 shrink-0 mt-2" />
              )}
            </button>
          );
        })}

        {hasMore && (
          <button
            onClick={handleLoadMore}
            disabled={isLoading}
            className="w-full flex items-center justify-center gap-2 py-3 text-sm text-indigo-600 hover:bg-indigo-50 transition border-t border-gray-100 disabled:opacity-50"
          >
            {isLoading ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              "Load more"
            )}
          </button>
        )}
      </div>
    </div>
  );

  return (
    <div>
      {/* Trigger */}
      <button
        id="btn_Warehouse"
        ref={triggerRef}
        onClick={handleToggleDropdown}
        className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 py-2 cursor-pointer hover:bg-gray-50 transition bg-white"
      >
        <div className="w-5 h-5 bg-indigo-50 rounded flex items-center justify-center">
          <Package className="w-3 h-3 text-indigo-500" />
        </div>
        <span className="text-sm font-medium text-gray-700 max-w-45 truncate">
          {selectedWarehouse ? selectedWarehouse.name : "Select Warehouse"}
        </span>
        <ChevronDown
          className={`w-4 h-4 text-gray-400 transition-transform duration-200 ${isDropdownOpen ? "rotate-180" : ""}`}
        />
      </button>

      {/* Portal */}
      {isDropdownOpen && createPortal(dropdownContent, document.body)}
    </div>
  );
}
