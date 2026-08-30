export const formatRelativeTime = (dateString: string) => {
  const date = new Date(dateString);

  const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

  const intervals = [
    { label: "tahun", value: 31536000 },
    { label: "bulan", value: 2592000 },
    { label: "hari", value: 86400 },
    { label: "jam", value: 3600 },
    { label: "menit", value: 60 },
  ];

  for (const interval of intervals) {
    const count = Math.floor(seconds / interval.value);

    if (count >= 1) {
      return `${count} ${interval.label} lalu`;
    }
  }

  return "Baru saja";
};
