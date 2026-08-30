import Swal from "sweetalert2";

interface ConfirmationDialogProps {
  title?: string;
  text?: string;
  confirmText?: string;
  cancelText?: string;
  confirmColor?: string;
  cancelColor?: string;
}

export const confirmationDialog = async ({
  title = "Are you sure?",
  text = "This action cannot be undone.",
  confirmText = "Yes",
  cancelText = "Cancel",
  confirmColor = "#ef4444",
  cancelColor = "#6b7280",
}: ConfirmationDialogProps = {}) => {
  const result = await Swal.fire({
    title,
    text,
    icon: "warning",
    showCancelButton: true,
    confirmButtonText: confirmText,
    cancelButtonText: cancelText,
    confirmButtonColor: confirmColor,
    cancelButtonColor: cancelColor,
    reverseButtons: true,
    focusCancel: true,
  });

  return result.isConfirmed;
};
