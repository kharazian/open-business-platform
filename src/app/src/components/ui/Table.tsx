import type { ReactNode } from "react";
import { cn } from "../../lib/cn";
import { useDesignTheme } from "../../context/useDesignTheme";

export type TableColumn<T> = {
  header: string;
  accessor?: keyof T;
  hideOnPrint?: boolean;
  render?: (row: T) => ReactNode;
};

type TableProps<T extends object> = {
  columns: Array<TableColumn<T>>;
  data?: T[];
  rows?: T[];
  getRowKey?: (row: T, index: number) => string;
  onRowClick?: (row: T) => void;
  selectedRowKey?: string | null;
};

export function Table<T extends object>({ columns, data, rows, getRowKey, onRowClick, selectedRowKey }: TableProps<T>) {
  const tableRows = data ?? rows ?? [];
  const { densityClasses } = useDesignTheme();

  return (
    <div className="w-full overflow-x-auto rounded-xl border border-border">
      <table className="min-w-full divide-y divide-border text-left text-sm">
        <thead className="bg-muted/70">
          <tr>
            {columns.map((column) => (
              <th
                className={cn("font-bold text-muted-foreground", densityClasses.tableCell)}
                data-print-hide={column.hideOnPrint ? "true" : undefined}
                key={column.header}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border bg-card/70">
          {tableRows.map((row, index) => {
            const rowKey = getRowKey?.(row, index) ?? String(index);
            return (
            <tr aria-selected={selectedRowKey ? rowKey === selectedRowKey : undefined} className={cn("transition hover:bg-muted/50", index % 2 ? "bg-muted/20" : "", onRowClick ? "cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary" : "", selectedRowKey === rowKey ? "bg-primary/10 ring-1 ring-inset ring-primary" : "")} key={rowKey} onClick={() => onRowClick?.(row)} onKeyDown={(event) => { if (onRowClick && (event.key === "Enter" || event.key === " ")) { event.preventDefault(); onRowClick(row); } }} tabIndex={onRowClick ? 0 : undefined}>
              {columns.map((column) => (
                <td
                  className={cn("text-foreground", densityClasses.tableCell)}
                  data-print-hide={column.hideOnPrint ? "true" : undefined}
                  key={column.header}
                >
                  {column.render ? column.render(row) : column.accessor ? String(row[column.accessor] ?? "") : null}
                </td>
              ))}
            </tr>
          );})}
        </tbody>
      </table>
    </div>
  );
}
