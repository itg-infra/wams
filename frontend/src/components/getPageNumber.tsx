  export function getPageNumbers(current: number, total: number): (number | "...")[] {
    const delta = 1; // jumlah halaman di kiri/kanan current
    const range: (number | "...")[] = [];

    const start = Math.max(2, current - delta);
    const end = Math.min(total - 1, current + delta);

    range.push(1);

    if (start > 2) {
      range.push("...");
    }

    for (let i = start; i <= end; i++) {
      range.push(i);
    }

    if (end < total - 1) {
      range.push("...");
    }

    if (total > 1) {
      range.push(total);
    }

    return range;
  }