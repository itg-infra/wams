"use client";

import { useEffect, useRef, useState, useCallback } from "react";
import { itemService } from "../master_data/services/itemService";
import type { Item } from "../master_data/types/item.types";

function ChevronDown() {
  return (
    <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
      <path
        d="M2.5 4.5L6 8L9.5 4.5"
        stroke="#6B7280"
        strokeWidth="1.3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

interface ItemComboBoxProps {
  id?: string;
  value: string; // id item yang terpilih
  displayLabel?: string; // teks yang ditampilkan saat tertutup (mis. "IT001 - Item A")
  onSelect: (item: Item) => void;
  placeholder?: string;
  limit?: number;
}

export default function ItemComboBox({
  id,
  value,
  displayLabel,
  onSelect,
  placeholder = "Pilih item",
  limit = 10,
}: ItemComboBoxProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<Item[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const containerRef = useRef<HTMLDivElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isFirstOpen = useRef(true);

  const fetchItems = useCallback(
    async (searchValue: string, pageValue: number) => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await itemService.getItems({
          search: searchValue,
          page: pageValue,
          limit,
        });
        setItems(response.data);
        setTotalPages(response.meta.totalPages);
        setPage(pageValue);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Gagal memuat item");
        setItems([]);
      } finally {
        setIsLoading(false);
      }
    },
    [limit],
  );

  // fetch page 1 setiap kali dropdown dibuka
  useEffect(() => {
    if (isOpen) {
      setSearch("");
      fetchItems("", 1);
      isFirstOpen.current = false;
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen]);

  // debounce search selama dropdown terbuka
  useEffect(() => {
    if (!isOpen || isFirstOpen.current) return;
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      fetchItems(search, 1);
    }, 400);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  // close saat klik di luar
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

  function handleSelect(item: Item) {
    onSelect(item);
    setIsOpen(false);
  }

  function handlePrev() {
    if (page <= 1 || isLoading) return;
    fetchItems(search, page - 1);
  }

  function handleNext() {
    if (page >= totalPages || isLoading) return;
    fetchItems(search, page + 1);
  }

  return (
    <div className="relative" ref={containerRef}>
      <button
        id={id}
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className="w-full h-9 pl-2 pr-7 rounded border border-gray-300 bg-white text-sm text-left text-gray-800 truncate focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
      >
        {displayLabel ? (
          displayLabel
        ) : (
          <span className="text-gray-400">{placeholder}</span>
        )}
      </button>

      <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center">
        <ChevronDown />
      </div>

      {isOpen && (
        <div className="absolute z-20 mt-1 w-72 rounded border border-gray-300 bg-white shadow-lg">
          <div className="p-2 border-b border-gray-200">
            <input
              autoFocus
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Cari item..."
              className="w-full h-8 px-2 rounded border border-gray-300 text-sm focus:outline-none focus:ring-1 focus:ring-[#2E277C]"
            />
          </div>

          <div className="max-h-56 overflow-y-auto">
            {isLoading && (
              <div className="px-3 py-2 text-sm text-gray-400">Memuat...</div>
            )}

            {!isLoading && error && (
              <div className="px-3 py-2 text-sm text-red-500">{error}</div>
            )}

            {!isLoading && !error && items.length === 0 && (
              <div className="px-3 py-2 text-sm text-gray-400">
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
                  className={`w-full text-left px-3 py-2 text-sm hover:bg-gray-100 ${
                    String(item.id) === value
                      ? "bg-[#EEF0FB] text-[#2E277C]"
                      : "text-gray-800"
                  }`}
                >
                  {item.itemCode} - {item.acctName}
                </button>
              ))}
          </div>

          <div className="flex items-center justify-between px-3 py-2 border-t border-gray-200 text-xs text-gray-500">
            <button
              type="button"
              onClick={handlePrev}
              disabled={page <= 1 || isLoading}
              className="px-2 py-1 rounded border border-gray-300 disabled:opacity-40"
            >
              Prev
            </button>
            <span>
              Page {page} / {totalPages}
            </span>
            <button
              type="button"
              onClick={handleNext}
              disabled={page >= totalPages || isLoading}
              className="px-2 py-1 rounded border border-gray-300 disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
