import { AlertCircle, CheckCircle, Info, XCircle, X } from "lucide-react";
import { cn } from "@/lib/utils";

interface AlertProps {
  variant?: "info" | "success" | "warning" | "error";
  title?: string;
  children: React.ReactNode;
  className?: string;
  onClose?: () => void;
}

function Alert({
  variant = "info",
  title,
  children,
  className,
  onClose,
}: AlertProps) {
  const variants = {
    info: {
      bg: "bg-blue-50 border-blue-200",
      icon: Info,
      iconColor: "text-blue-500",
      titleColor: "text-blue-800",
      textColor: "text-blue-700",
    },
    success: {
      bg: "bg-green-50 border-green-200",
      icon: CheckCircle,
      iconColor: "text-green-500",
      titleColor: "text-green-800",
      textColor: "text-green-700",
    },
    warning: {
      bg: "bg-yellow-50 border-yellow-200",
      icon: AlertCircle,
      iconColor: "text-yellow-500",
      titleColor: "text-yellow-800",
      textColor: "text-yellow-700",
    },
    error: {
      bg: "bg-red-50 border-red-200",
      icon: XCircle,
      iconColor: "text-red-500",
      titleColor: "text-red-800",
      textColor: "text-red-700",
    },
  };

  const config = variants[variant];
  const Icon = config.icon;

  return (
    <div
      className={cn("flex gap-3 p-4 rounded-lg border", config.bg, className)}
    >
      <Icon className={cn("w-5 h-5 flex-shrink-0 mt-0.5", config.iconColor)} />
      <div className="flex-1">
        {title && (
          <h4 className={cn("font-medium", config.titleColor)}>{title}</h4>
        )}
        <div className={cn("text-sm", config.textColor, title && "mt-1")}>
          {children}
        </div>
      </div>
      {onClose && (
        <button
          onClick={onClose}
          className={cn("flex-shrink-0", config.iconColor, "hover:opacity-70")}
        >
          <X className="w-4 h-4" />
        </button>
      )}
    </div>
  );
}

export { Alert };
