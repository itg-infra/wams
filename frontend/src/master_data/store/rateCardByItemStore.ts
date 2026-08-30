// hooks/useRateCardVendors.ts

import { useState, useEffect, useCallback } from "react";
import type { RateCardByItemVendor } from "../types/rateCardByItem.type";
import { rateCardService } from "../services/rateCardByItemService";

interface UseRateCardVendorsResult {
    vendors: RateCardByItemVendor[];
    isLoading: boolean;
    error: string | null;
}

/**
 * Fetch vendor list from rate card by itemShadowId.
 * Returns empty array if itemShadowId is null (manual rows).
 */
export function useRateCardVendors(itemShadowId: number | null): UseRateCardVendorsResult {
    const [vendors, setVendors] = useState<RateCardByItemVendor[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const fetchVendors = useCallback(async () => {
        if (itemShadowId === null) {
            setVendors([]);
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const data = await rateCardService.getVendorsByItem(itemShadowId);
            setVendors(data);
        } catch (err) {
            console.error("Failed to fetch rate card vendors:", err);
            setError("Failed to load vendors");
            setVendors([]);
        } finally {
            setIsLoading(false);
        }
    }, [itemShadowId]);

    useEffect(() => {
        fetchVendors();
    }, [fetchVendors]);

    return { vendors, isLoading, error };
}