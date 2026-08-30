import { useEffect, useRef, useState } from "react";
import { ChevronDown, Search, Loader2 } from "lucide-react";
import type { Item } from "../master_data/types/item.types";
import { useItemController } from "../master_data/controller/itemController";

interface ItemSearchSelectProps {
  id?: string;
  value: string; // id item yang sedang dipilih
  onChange: (item: Item) => void;
  placeholder?: string;
}

export function ItemSearchSelect({
  id,
  value,
  onChange,
  placeholder = "Pilih item",
}: ItemSearchSelectProps) {
  const {
    items,
    isLoading,
    error,
    searchInput,
    page,
    total,
    totalPages,
    from,
    to,
    handleSearchChange,
    handlePrevPage,
    handleNextPage,
  } = useItemController();

  const [isOpen, setIsOpen] = useState(false);
  const [label, setLabel] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);

  // Simpan label item terpilih secara lokal supaya tetap tampil
  // walau item tsb tidak ada di halaman/hasil pencarian saat ini.
  useEffect(() => {
    const found = items.find((i) => i.id === value);
    if (found) {
      setLabel(`${found.itemCode} - ${found.acctName}`);
    } else if (!value) {
      setLabel("");
    }
  }, [items, value]);

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

  const handleSelect = (item: Item) => {
    setLabel(`${item.itemCode} - ${item.acctName}`);
    onChange(item);
    setIsOpen(false);
  };

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        id={id}
        onClick={() => setIsOpen((prev) => !prev)}
        className="h-11 w-full border border-[#D8DCE5] rounded-xl px-4 text-[14px] text-left text-[#222222] bg-white outline-none focus:ring-2 focus:ring-indigo-200 transition cursor-pointer flex items-center justify-between"
      >
        <span className={`truncate ${label ? "" : "text-[#7A7A7A]"}`}>
          {label || placeholder}
        </span>
        <ChevronDown className="w-3.5 h-3.5 text-[#7A7A7A] shrink-0 ml-2" />
      </button>

      {isOpen && (
        <div className="absolute z-20 mt-1 w-full bg-white border border-[#D8DCE5] rounded-xl shadow-lg overflow-hidden">
          {/* Search input */}
          <div className="p-2 border-b border-[#EEF0F6]">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[#7A7A7A]" />
              <input
                autoFocus
                type="text"
                value={searchInput}
                onChange={(e) => handleSearchChange(e.target.value)}
                placeholder="Cari kode / nama item..."
                className="h-9 w-full border border-[#D8DCE5] rounded-lg pl-8 pr-3 text-[13px] text-[#222222] bg-white outline-none focus:ring-2 focus:ring-indigo-200 transition"
              />
            </div>
          </div>

          {/* List */}
          <div className="max-h-56 overflow-y-auto">
            {isLoading && (
              <div className="flex items-center justify-center gap-2 py-6 text-[13px] text-[#7A7A7A]">
                <Loader2 className="w-4 h-4 animate-spin" />
                Memuat...
              </div>
            )}

            {!isLoading && error && (
              <div className="py-6 text-center text-[13px] text-red-500">
                {error}
              </div>
            )}

            {!isLoading && !error && items.length === 0 && (
              <div className="py-6 text-center text-[13px] text-[#7A7A7A]">
                Item tidak ditemukan
              </div>
            )}

            {!isLoading &&
              !error &&
              items.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => handleSelect(item)}
                  className={`w-full text-left px-3 py-2 text-[14px] hover:bg-[#EEF0F6] transition ${
                    item.id === value
                      ? "bg-[#EEF0F6] text-[#2E277C] font-medium"
                      : "text-[#222222]"
                  }`}
                >
                  <div className="truncate">
                    {item.itemCode} - {item.acctName}
                  </div>
                  <div className="truncate text-[12px] text-[#7A7A7A]">
                    {item.itemName}
                  </div>
                </button>
              ))}
          </div>

          {/* Pagination footer */}
          {!isLoading && !error && total > 0 && (
            <div className="flex items-center justify-between gap-2 px-3 py-2 border-t border-[#EEF0F6] text-[12px] text-[#7A7A7A]">
              <span>
                {from}-{to} dari {total}
              </span>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={handlePrevPage}
                  disabled={page <= 1}
                  className="px-2 py-1 rounded-md border border-[#D8DCE5] disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[#EEF0F6] transition"
                >
                  Prev
                </button>
                <span>
                  {page}/{totalPages}
                </span>
                <button
                  type="button"
                  onClick={handleNextPage}
                  disabled={page >= totalPages}
                  className="px-2 py-1 rounded-md border border-[#D8DCE5] disabled:opacity-40 disabled:cursor-not-allowed hover:bg-[#EEF0F6] transition"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
