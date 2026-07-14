import { useState } from 'react';

export function useSelection(initialIds: string[] = []) {
    const [selectedIds, setSelectedIds] = useState<string[]>(initialIds);

    const toggleSelection = (id: string) => {
        setSelectedIds(prev =>
            prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
        );
    };

    const selectAll = (ids: string[]) => {
        setSelectedIds(ids);
    };

    const clearSelection = () => {
        setSelectedIds([]);
    };

    const isSelected = (id: string) => selectedIds.includes(id);

    return {
        selectedIds,
        toggleSelection,
        selectAll,
        clearSelection,
        isSelected
    };
}