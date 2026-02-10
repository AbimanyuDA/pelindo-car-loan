import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/store/authStore";
import toast from "react-hot-toast";

interface NotificationMessage {
  type: string;
  level?: number;
  approvalType?: string;
  requestId?: number;
  status?: string;
  pemohonId?: string;
  pemohonName?: string;
  timestamp: string;
  message: string;
}

/**
 * Hook untuk subscribe ke Server-Sent Events (SSE) dan trigger refetch otomatis
 * @param endpoint - URL endpoint SSE (e.g., "/api/approvals/subscribe" atau "/api/loan-requests/subscribe")
 * @param queryKeys - Array of query keys untuk di-invalidate ketika ada update
 * @param onNotification - Optional callback function ketika notifikasi diterima
 */
export function useRealTimeUpdates(
  endpoint: string,
  queryKeys: string[],
  onNotification?: (message: NotificationMessage) => void
) {
  const queryClient = useQueryClient();
  const token = useAuthStore((state) => state.token);

  useEffect(() => {
    if (!token) {
      console.log("No token available, skipping SSE subscription");
      return;
    }

    let eventSource: EventSource | null = null;
    let reconnectTimeout: ReturnType<typeof setTimeout> | null = null;
    let attemptCount = 0;
    const maxAttempts = 5;
    const baseDelay = 1000; // 1 second

    const connect = () => {
      try {
        const apiBaseUrl = (import.meta as any).env?.VITE_API_URL || "/api";
        // Pass token as query parameter since EventSource doesn't support custom headers
        const fullUrl = `${apiBaseUrl}${endpoint}?token=${encodeURIComponent(token)}`;

        console.log(`[SSE] Connecting to ${fullUrl}`);

        eventSource = new EventSource(fullUrl);

        eventSource.onopen = () => {
          console.log("[SSE] Connection established");
          attemptCount = 0; // Reset attempt count on successful connection
        };

        eventSource.onmessage = (event) => {
          try {
            const message: NotificationMessage = JSON.parse(event.data);
            console.log("[SSE] Message received:", message);

            // Show toast notification
            toast.success(message.message);

            // Call optional callback
            if (onNotification) {
              onNotification(message);
            }

            // Invalidate queries to trigger refetch
            queryKeys.forEach((key) => {
              queryClient.invalidateQueries({ queryKey: [key] });
            });
          } catch (error) {
            console.error("[SSE] Error parsing message:", error);
          }
        };

        eventSource.onerror = (event) => {
          console.error("[SSE] Error occurred:", event);

          if (eventSource?.readyState === EventSource.CLOSED) {
            console.log("[SSE] Connection closed, attempting to reconnect...");
            eventSource?.close();
            eventSource = null;

            if (attemptCount < maxAttempts) {
              attemptCount++;
              const delay = baseDelay * Math.pow(2, attemptCount - 1); // Exponential backoff
              console.log(
                `[SSE] Reconnect attempt ${attemptCount}/${maxAttempts} in ${delay}ms`
              );

              reconnectTimeout = setTimeout(() => {
                connect();
              }, delay);
            } else {
              console.error(
                "[SSE] Max reconnection attempts reached, giving up"
              );
              toast.error("Real-time connection lost");
            }
          }
        };
      } catch (error) {
        console.error("[SSE] Error creating EventSource:", error);
        toast.error("Failed to connect to real-time updates");
      }
    };

    // Initial connection
    connect();

    // Cleanup function
    return () => {
      console.log("[SSE] Cleaning up subscription");
      if (reconnectTimeout) {
        clearTimeout(reconnectTimeout);
      }
      if (eventSource) {
        eventSource.close();
      }
    };
  }, [token, endpoint, queryKeys, queryClient, onNotification]);
}
