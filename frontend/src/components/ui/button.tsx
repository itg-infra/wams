import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Slot } from "radix-ui"

import { cn } from "../../lib/utils"

// Brand accent: indigo #2B2469 (shared with PageHeader back-arrow and the sidebar).
const buttonVariants = cva(
  "inline-flex shrink-0 items-center justify-center gap-2 rounded-lg text-sm font-medium whitespace-nowrap transition-all outline-none cursor-pointer focus-visible:ring-2 focus-visible:ring-[#2B2469]/40 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        // Solid brand CTA — "Add", "Save", "Submit", primary confirms.
        primary:
          "bg-[#2B2469] text-white shadow-sm hover:bg-[#241e63]",
        // Outlined brand — secondary actions, "Add" on list pages that used an outline.
        secondary:
          "border border-[#2B2469] text-[#2B2469] bg-white hover:bg-indigo-50",
        // Neutral outline — "Cancel", "Sort By", quiet bordered actions.
        outline:
          "border border-gray-300 bg-white text-gray-700 hover:bg-gray-50",
        // Minimal — toolbar/icon-ish buttons, low-emphasis actions.
        ghost:
          "text-gray-600 hover:bg-gray-100 hover:text-gray-900",
        // Destructive — delete confirmations.
        destructive:
          "bg-red-500 text-white shadow-sm hover:bg-red-600 focus-visible:ring-red-500/40",
        // Inline text link.
        link: "text-[#2B2469] underline-offset-4 hover:underline",
      },
      size: {
        default: "h-9 px-4 py-2 has-[>svg]:px-3",
        xs: "h-6 gap-1 rounded-md px-2 text-xs has-[>svg]:px-1.5 [&_svg:not([class*='size-'])]:size-3",
        sm: "h-8 gap-1.5 rounded-md px-3 has-[>svg]:px-2.5",
        lg: "h-10 rounded-lg px-6 has-[>svg]:px-4",
        icon: "size-9",
        "icon-xs": "size-6 rounded-md [&_svg:not([class*='size-'])]:size-3",
        "icon-sm": "size-8",
        "icon-lg": "size-10",
      },
    },
    defaultVariants: {
      variant: "primary",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "primary",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button }
