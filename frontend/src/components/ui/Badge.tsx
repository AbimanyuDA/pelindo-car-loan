import { cn, getStatusColor, getStatusLabel } from "@/lib/utils";

interface BadgeProps {
  status: string;
  className?: string;
  showLabel?: boolean;
}

function Badge({ status, className, showLabel = true }: BadgeProps) {
  const colorClass = getStatusColor(status);
  const label = showLabel ? getStatusLabel(status) : status;

  return (
    <span
      className={cn(
        "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium",
        colorClass,
        className
      )}
    >
      {label}
    </span>
  );
}

interface CustomBadgeProps {
  children: React.ReactNode;
  variant?: "default" | "success" | "warning" | "danger" | "info";
  className?: string;
}

function CustomBadge({
  children,
  variant = "default",
  className,
}: CustomBadgeProps) {
  const variants = {
    default: "bg-gray-100 text-gray-800",
    success: "bg-green-100 text-green-800",
    warning: "bg-yellow-100 text-yellow-800",
    danger: "bg-red-100 text-red-800",
    info: "bg-blue-100 text-blue-800",
  };

  return (
    <span
      className={cn(
        "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium",
        variants[variant],
        className
      )}
    >
      {children}
    </span>
  );
}

export { Badge, CustomBadge };
