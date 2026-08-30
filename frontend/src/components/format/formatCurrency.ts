export const formatNumber = (amount: number): string => {
  return new Intl.NumberFormat("id-ID").format(amount);
};

export const formatCurrency = (amount: number): string => {
  return `Rp ${formatNumber(amount)}`;
};
