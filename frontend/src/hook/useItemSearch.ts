import { useCallback, useEffect, useRef, useState } from "react";
import { itemService } from "../master_data/services/itemService";
import type { Item } from "../master_data/types/item.types";

const DEFAULT_LIMIT = 10;

export function useItemSearch(limit = DEFAULT_LIMIT) {
  const [items, setItems] = useState<Item[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);

  const searchRef = useRef("");
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const requestIdRef = useRef(0);

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

        if (requestId !== requestIdRef.current) return;

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

  // fetch awal: page 1, tanpa filter
  useEffect(() => {
    fetchPage("", 1, false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, []);

  const onSearchChange = useCallback(
    (searchValue: string) => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        fetchPage(searchValue, 1, false);
      }, 400);
    },
    [fetchPage],
  );

  const onEndReached = useCallback(() => {
    if (page >= totalPages || isLoading || isLoadingMore) return;
    fetchPage(searchRef.current, page + 1, true);
  }, [page, totalPages, isLoading, isLoadingMore, fetchPage]);

  return {
    items,
    isLoading,
    isLoadingMore,
    hasMore: page < totalPages,
    onSearchChange,
    onEndReached,
  };
}
