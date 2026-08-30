import { useEffect, useRef, useState } from "react";
import { ChevronDown } from "lucide-react";
import { useVendorStore } from "../master_data/store/vendorStore";

interface VendorComboboxProps {
  value: string; // cardCode terpilih
  onChange: (cardCode: string) => void;
  disabled?: boolean;
}

export function VendorCombobox({
  value,
  onChange,
  disabled,
}: VendorComboboxProps) {
  const { vendors, isLoading, isLoadingMore, hasMore, fetchVendors } =
    useVendorStore();

  const [isOpen, setIsOpen] = useState(false);
  const [keyword, setKeyword] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLDivElement>(null);

  // Fetch awal
  useEffect(() => {
    fetchVendors({ search: "", page: 1 });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Debounce search
  useEffect(() => {
    const timeout = setTimeout(() => {
      fetchVendors({ search: keyword, page: 1 }, { append: false });
    }, 400);
    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [keyword]);

  // Klik di luar -> close
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (
        containerRef.current &&
        !containerRef.current.contains(e.target as Node)
      ) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Lazy load saat scroll ke bawah list
  function handleScroll(e: React.UIEvent<HTMLDivElement>) {
    const el = e.currentTarget;
    const nearBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 40;
    if (nearBottom && hasMore && !isLoading && !isLoadingMore) {
      useVendorStore.getState().loadMoreVendors();
    }
  }

  const selectedVendor = vendors.find((v) => v.cardCode === value);

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        id="cmb_VendorCode"
        disabled={disabled}
        onClick={() => setIsOpen((prev) => !prev)}
        className="w-full h-10 pl-3 pr-8 rounded-md border border-gray-300 bg-white text-sm text-left text-gray-800 focus:outline-none focus:ring-1 focus:ring-[#2E277C] focus:border-[#2E277C] disabled:bg-gray-100 disabled:cursor-not-allowed"
      >
        {selectedVendor ? (
          `${selectedVendor.cardCode} - ${selectedVendor.cardName}`
        ) : (
          <span className="text-gray-400">Select</span>
        )}
      </button>
      <div className="pointer-events-none absolute inset-y-0 right-2.5 flex items-center">
        <ChevronDown />
      </div>

      {isOpen && (
        <div className="absolute z-20 mt-1 w-full rounded-md border border-gray-200 bg-white shadow-lg">
          <div className="p-2 border-b border-gray-100">
            <input
              autoFocus
              type="text"
              placeholder="Cari vendor..."
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              className="w-full h-9 px-2 rounded border border-gray-300 text-sm focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
            />
          </div>

          <div
            ref={listRef}
            onScroll={handleScroll}
            className="max-h-56 overflow-y-auto"
          >
            {vendors.length === 0 && !isLoading && (
              <div className="px-3 py-2 text-sm text-gray-400">
                Tidak ada data
              </div>
            )}

            {vendors.map((v) => (
              <button
                key={v.id}
                type="button"
                onClick={() => {
                  onChange(v.cardCode);
                  setIsOpen(false);
                }}
                className={`w-full text-left px-3 py-2 text-sm hover:bg-gray-100 ${
                  v.cardCode === value ? "bg-gray-50 font-medium" : ""
                }`}
              >
                {v.cardCode} - {v.cardName}
              </button>
            ))}

            {(isLoading || isLoadingMore) && (
              <div className="px-3 py-2 text-sm text-gray-400">Memuat...</div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
