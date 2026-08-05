import { useState, useCallback } from 'react';

export function useSelection(initialIds: string[] = []) {
    const [selectedIds, setSelectedIds] = useState<string[]>(initialIds);

    const toggleSelection = useCallback((id: string) => {
        setSelectedIds(prev =>
            prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
        );
    }, []);

    const selectAll = useCallback((ids: string[]) => {
        setSelectedIds(ids);
    }, []);

    const clearSelection = useCallback(() => {
        setSelectedIds([]);
    }, []);

    const isSelected = useCallback((id: string) => selectedIds.includes(id), [selectedIds]);

    return {
        selectedIds,
        toggleSelection,
        selectAll,
        clearSelection,
        isSelected
    };
}