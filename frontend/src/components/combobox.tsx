"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { createPortal } from "react-dom";
import { itemService } from "../master_data/services/itemService";
import type { Item } from "../master_data/types/item.types";

const DEFAULT_LIMIT = 10;

function toOption(item: Item): ComboboxOption {
  return {
    value: item.id,
    label: `${item.itemCode} - ${item.acctName}`, // sesuaikan kalau field beda
  };
}

interface ItemSearchSelectProps {
  id?: string;
  value: string; // id item terpilih, "" kalau kosong
  onChange: (item: Item) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  limit?: number;
}

export function ItemSearchSelect({
  id,
  value,
  onChange,
  placeholder = "Pilih item",
  disabled = false,
  className,
  limit = DEFAULT_LIMIT,
}: ItemSearchSelectProps) {
  const [items, setItems] = useState<Item[]>([]);
  const [selectedItem, setSelectedItem] = useState<Item | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);

  const searchRef = useRef(""); // search term terakhir yang sudah di-fetch (dipakai saat load more)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const requestIdRef = useRef(0); // guard: response request lama tidak menimpa yang lebih baru

  const fetchPage = useCallback(
    async (searchValue: string, pageValue: number, append: boolean) => {
      const requestId = ++requestIdRef.current;
      searchRef.current = searchValue;

      if (append) setIsLoadingMore(true);
      else setIsLoading(true);

      try {
        const response = await itemService.getItems({
          search: searchValue,
          page: pageValue,
          limit,
        });

        if (requestId !== requestIdRef.current) return; // sudah keduluan request lebih baru

        setItems((prev) =>
          append ? [...prev, ...response.data] : response.data,
        );
        setTotalPages(response.meta.totalPages);
        setPage(pageValue);
      } catch (err) {
        console.error("Failed to fetch items", err);
        if (requestId === requestIdRef.current && !append) setItems([]);
      } finally {
        if (requestId === requestIdRef.current) {
          if (append) setIsLoadingMore(false);
          else setIsLoading(false);
        }
      }
    },
    [limit],
  );

  // fetch awal saat komponen mount: page 1, tanpa filter
  useEffect(() => {
    fetchPage("", 1, false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // sinkronkan label item terpilih (mis. saat edit mode: value datang dari luar
  // dan belum tentu ada di halaman `items` yang sedang di-fetch)
  useEffect(() => {
    if (!value) {
      setSelectedItem(null);
      return;
    }

    const found = items.find((i) => i.id === value);
    if (found) {
      setSelectedItem(found);
      return;
    }

    let cancelled = false;
    itemService
      .getItemDetail(value)
      .then((res) => {
        if (!cancelled) setSelectedItem(res.data);
      })
      .catch(() => {
        if (!cancelled) setSelectedItem(null);
      });

    return () => {
      cancelled = true;
    };
  }, [value, items]);

  const handleSearchChange = (searchValue: string) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      fetchPage(searchValue, 1, false);
    }, 400);
  };

  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, []);

  const handleEndReached = () => {
    if (page >= totalPages || isLoading || isLoadingMore) return;
    fetchPage(searchRef.current, page + 1, true);
  };

  const handleChange = (option: ComboboxOption | null) => {
    if (!option) return; // tombol clear tidak diteruskan; lihat catatan di bawah
    const selected = items.find((i) => i.id === option.value) ?? selectedItem;
    if (selected) onChange(selected);
  };

  // pastikan item yang sedang terpilih selalu ada di options,
  // walau dia bukan bagian dari halaman/hasil search yang sedang tampil
  const options: ComboboxOption[] = (() => {
    const base = items.map(toOption);
    if (selectedItem && !items.some((i) => i.id === selectedItem.id)) {
      return [toOption(selectedItem), ...base];
    }
    return base;
  })();

  return (
    <Combobox
      id={id}
      options={options}
      value={value || null}
      onChange={handleChange}
      onSearchChange={handleSearchChange}
      isLoading={isLoading}
      isLoadingMore={isLoadingMore}
      hasMore={page < totalPages}
      onEndReached={handleEndReached}
      placeholder={placeholder}
      disabled={disabled}
      className={className}
    />
  );
}

export interface ComboboxOption {
  value: string;
  label: string;
  sublabel?: string;
}

interface ComboboxProps {
  id?: string;
  options: ComboboxOption[];
  value: string | null;
  onChange: (option: ComboboxOption | null) => void;
  onSearchChange?: (search: string) => void;
  isLoading?: boolean;
  isLoadingMore?: boolean; // NEW: loading indicator khusus saat fetch halaman berikutnya
  hasMore?: boolean; // NEW: apakah masih ada data selanjutnya
  onEndReached?: () => void; // NEW: dipanggil saat scroll mendekati akhir list
  placeholder?: string;
  disabled?: boolean;
  className?: string;
}

interface DropdownPosition {
  top: number;
  left: number;
  width: number;
}

export function Combobox({
  id,
  options,
  value,
  onChange,
  onSearchChange,
  isLoading = false,
  isLoadingMore = false,
  hasMore = false,
  onEndReached,
  placeholder = "Search...",
  disabled = false,
  className = "",
}: ComboboxProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [dropdownPos, setDropdownPos] = useState<DropdownPosition | null>(null);

  const triggerRef = useRef<HTMLDivElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const selectedOption = options.find((o) => o.value === value) ?? null;
  const displayValue = isOpen ? search : (selectedOption?.label ?? "");

  const updatePosition = useCallback(() => {
    if (!triggerRef.current) return;
    const rect = triggerRef.current.getBoundingClientRect();
    setDropdownPos({
      top: rect.bottom + window.scrollY + 4,
      left: rect.left + window.scrollX,
      width: rect.width,
    });
  }, []);

  const openDropdown = () => {
    if (disabled) return;
    updatePosition();
    setIsOpen(true);
    setSearch("");
  };

  useEffect(() => {
    if (!isOpen) return;

    function handleClickOutside(e: MouseEvent) {
      const target = e.target as Node;
      if (
        triggerRef.current?.contains(target) ||
        dropdownRef.current?.contains(target)
      )
        return;
      setIsOpen(false);
      setSearch("");
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;
    window.addEventListener("scroll", updatePosition, true);
    window.addEventListener("resize", updatePosition);
    return () => {
      window.removeEventListener("scroll", updatePosition, true);
      window.removeEventListener("resize", updatePosition);
    };
  }, [isOpen, updatePosition]);

  const handleSelect = (option: ComboboxOption) => {
    onChange(option);
    setIsOpen(false);
    setSearch("");
  };

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation();
    onChange(null);
    setSearch("");
    setIsOpen(false);
  };

  // NEW: deteksi scroll mendekati akhir list -> trigger load more
  const handleListScroll = (e: React.UIEvent<HTMLDivElement>) => {
    if (!onEndReached || !hasMore || isLoadingMore || isLoading) return;

    const el = e.currentTarget;
    const threshold = 32; // px dari bawah
    const reachedBottom =
      el.scrollHeight - el.scrollTop - el.clientHeight < threshold;

    if (reachedBottom) {
      onEndReached();
    }
  };

  const dropdown =
    isOpen && dropdownPos
      ? createPortal(
          <div
            id={id ? `${id}_Options` : undefined}
            ref={dropdownRef}
            onScroll={handleListScroll} // NEW
            style={{
              position: "absolute",
              top: dropdownPos.top,
              left: dropdownPos.left,
              width: 250,
              zIndex: 9999,
            }}
            className="bg-white border border-gray-200 rounded-lg shadow-xl max-h-48 overflow-auto"
          >
            {isLoading ? (
              <div className="px-3 py-2 text-xs text-gray-500 text-center">
                Loading...
              </div>
            ) : options.length === 0 ? (
              <div className="px-3 py-2 text-xs text-gray-500 text-center">
                No results found
              </div>
            ) : (
              <>
                {options.map((option) => (
                  <button
                    key={option.value}
                    id={id ? `${id}_Option_${option.value}` : undefined}
                    type="button"
                    onMouseDown={(e) => e.preventDefault()}
                    onClick={() => handleSelect(option)}
                    className={`w-full text-left px-3 py-2 text-xs hover:bg-blue-50 flex flex-col gap-0.5 ${
                      option.value === value
                        ? "bg-blue-50 text-blue-700"
                        : "text-gray-800"
                    }`}
                  >
                    <span className="font-medium">{option.label}</span>
                    {option.sublabel && (
                      <span className="text-gray-400">{option.sublabel}</span>
                    )}
                  </button>
                ))}
                {isLoadingMore && (
                  <div className="px-3 py-2 text-[11px] text-gray-400 text-center">
                    Loading more...
                  </div>
                )}
              </>
            )}
          </div>,
          document.body,
        )
      : null;

  return (
    <div ref={triggerRef} className={`relative ${className}`}>
      <div
        className={`flex items-center bg-white border rounded-lg h-9 px-2 gap-1 ${
          disabled
            ? "bg-gray-50 border-gray-200 cursor-not-allowed"
            : isOpen
              ? "border-blue-500 ring-1 ring-blue-500"
              : "border-gray-300 hover:border-gray-400"
        }`}
      >
        <input
          id={id}
          type="text"
          value={displayValue}
          onFocus={openDropdown}
          onChange={(e) => {
            setSearch(e.target.value);
            onSearchChange?.(e.target.value);
          }}
          placeholder={placeholder}
          disabled={disabled}
          className="flex-1 text-xs outline-none bg-transparent min-w-0 text-gray-800 placeholder-gray-400 disabled:cursor-not-allowed"
        />
        {value && !disabled && (
          <button
            type="button"
            onMouseDown={(e) => e.preventDefault()}
            onClick={handleClear}
            className="text-gray-400 hover:text-gray-600 shrink-0"
          >
            <svg
              className="w-3 h-3"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        )}
        <svg
          className={`w-3 h-3 shrink-0 text-gray-400 transition-transform ${isOpen ? "rotate-180" : ""}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M19 9l-7 7-7-7"
          />
        </svg>
      </div>

      {dropdown}
    </div>
  );
}
