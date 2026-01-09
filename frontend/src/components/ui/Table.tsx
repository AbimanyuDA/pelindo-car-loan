import { cn } from "@/lib/utils";

interface Column<T> {
  key: string;
  header: string;
  className?: string;
  render?: (item: T) => React.ReactNode;
}

interface TableProps<T> {
  columns: Column<T>[];
  data: T[];
  keyExtractor: (item: T) => string | number;
  isLoading?: boolean;
  emptyMessage?: string;
  onRowClick?: (item: T) => void;
  className?: string;
}

function Table<T>({
  columns,
  data,
  keyExtractor,
  isLoading = false,
  emptyMessage = "Tidak ada data",
  onRowClick,
  className,
}: TableProps<T>) {
  if (isLoading) {
    return (
      <div
        className={cn(
          "bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden",
          className
        )}
      >
        <div className="animate-pulse">
          <div className="h-12 bg-gray-100 hidden md:block" />
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-16 border-t border-gray-100">
              <div className="flex items-center h-full px-3 sm:px-6 gap-4">
                {columns.map((_, j) => (
                  <div key={j} className="h-4 bg-gray-200 rounded flex-1" />
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "bg-white rounded-xl shadow-sm border border-gray-100",
        className
      )}
    >
      {/* Desktop Table View */}
      <div className="hidden md:block overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="bg-gray-50 border-b border-gray-100">
              {columns.map((column) => (
                <th
                  key={column.key}
                  className={cn(
                    "px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider",
                    column.className
                  )}
                >
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {data.length === 0 ? (
              <tr>
                <td
                  colSpan={columns.length}
                  className="px-6 py-12 text-center text-gray-500"
                >
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              data.map((item) => (
                <tr
                  key={keyExtractor(item)}
                  className={cn(
                    "hover:bg-gray-50 transition-colors border-b border-gray-100",
                    onRowClick && "cursor-pointer"
                  )}
                  onClick={() => onRowClick?.(item)}
                >
                  {columns.map((column) => (
                    <td
                      key={column.key}
                      className={cn(
                        "px-6 py-4 text-sm text-gray-700",
                        column.className
                      )}
                    >
                      {column.render
                        ? column.render(item)
                        : ((item as Record<string, unknown>)[
                            column.key
                          ] as React.ReactNode)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Mobile Card View */}
      <div className="md:hidden space-y-3">
        {data.length === 0 ? (
          <div className="px-4 py-12 text-center text-gray-500">
            {emptyMessage}
          </div>
        ) : (
          <>
            {data.map((item) => {
              // Find indices of status and actions columns
              const statusColumnIndex = columns.findIndex(
                (col) => col.key === "status"
              );
              const actionsColumnIndex = columns.findIndex(
                (col) => col.key === "actions"
              );

              // Show first 2 columns only in main area
              const mainColumns = columns.slice(0, 2);

              const statusColumn =
                statusColumnIndex >= 0 ? columns[statusColumnIndex] : null;
              const actionsColumn =
                actionsColumnIndex >= 0 ? columns[actionsColumnIndex] : null;

              return (
                <div
                  key={keyExtractor(item)}
                  className="bg-white rounded-2xl border border-gray-200 shadow-sm hover:shadow-lg transition-all duration-300 overflow-hidden"
                >
                  {/* Header with Status Badge */}
                  <div className="bg-gradient-to-r from-gray-50 to-gray-100 px-4 py-3 border-b border-gray-200 flex items-center justify-between">
                    <p className="text-xs font-bold text-gray-600 uppercase tracking-widest">
                      Detail
                    </p>
                    {statusColumn && (
                      <div className="flex-shrink-0">
                        {statusColumn.render
                          ? statusColumn.render(item)
                          : ((item as Record<string, unknown>)[
                              statusColumn.key
                            ] as React.ReactNode)}
                      </div>
                    )}
                  </div>

                  {/* Main Content Area */}
                  <div className="px-4 py-4 space-y-4">
                    {mainColumns.map((column) => (
                      <div key={column.key}>
                        <p className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-2">
                          {column.header}
                        </p>
                        <div className="text-sm text-gray-900 font-semibold leading-relaxed">
                          {column.render
                            ? column.render(item)
                            : ((item as Record<string, unknown>)[
                                column.key
                              ] as React.ReactNode)}
                        </div>
                      </div>
                    ))}
                  </div>

                  {/* Actions Footer - Eye Catching */}
                  {actionsColumn && (
                    <div className="bg-gradient-to-r from-blue-50 to-indigo-50 px-4 py-4 border-t-2 border-blue-200 flex gap-2 justify-center">
                      <div className="flex gap-2 items-center">
                        {actionsColumn.render
                          ? actionsColumn.render(item)
                          : ((item as Record<string, unknown>)[
                              actionsColumn.key
                            ] as React.ReactNode)}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </>
        )}
      </div>
    </div>
  );
}

export { Table };
export type { Column, TableProps };
