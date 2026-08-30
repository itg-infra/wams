import { useEffect, useState } from "react";
import { useActivityStore } from "../store/activityStore";

export function useActivityController(){
    const {
        activities,
        selectedActivity,
        isLoading,
        isDetailLoading,
        error,
        detailError,
        search,
        fetchActivity,
        fetchActivityDetail,
        setSearch,
        clearSelectedActivity,

    } = useActivityStore();

    const [searchInput, setSearchInput] = useState(search);

    useEffect(()=>{
        fetchActivity();
    }, [fetchActivity]);

    useEffect(()=>{
        const handler = setTimeout(()=> {
            fetchActivity();
        }, 400);

        return ()=> clearTimeout(handler);
    }, [searchInput, fetchActivity]);

    const handleSearchChange = (value: string)=>{
        setSearchInput(value);
        setSearch(value);
    }

    const handleGetDetail = async (id: string)=>{
        await fetchActivityDetail(id);
    };

    return{
        activities,
        selectedActivity,
        isLoading,
        isDetailLoading,
        error,
        detailError,
        searchInput,

        handleSearchChange,
        handleGetDetail,
        clearSelectedActivity,
    }
}