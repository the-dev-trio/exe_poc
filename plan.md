## WPF Audit Gaps (Code Review)

Scope: static code review only. I did not run the app or test hardware devices.

### Stability / Crash Risk
- Backup from top bar (`MainViewModel.BackupNow`) has no try/catch; any IO error (permission, locked db, path missing) will throw and can crash.
- `DatabaseHelper.GetMetalRates()` uses `decimal.Parse` with user-stored settings; invalid values can throw and crash on app start.
- Auto-backup on exit silently swallows exceptions; failed backups are invisible to users.

### Inventory + SKU / Serial
- Inventory data model has only `SKU`; there is no serial-number field or UI to add items by serial number.
- POS scan input only searches by SKU (`GetItemBySKU`); barcode/serial support is missing.
- No validation for duplicate SKU entry on manual edits besides DB primary key; user feedback is minimal.

### POS / Checkout Flow
- Checkout does not validate required customer fields, payment mode, or cart integrity beyond “Cart.Count > 0”.
- No handling for partial failures: transaction is saved and items marked sold before printing; if receipt print fails, the sale still completes and stock is marked sold.
- No item-level making charge display or GST line per item; totals are simple aggregates only.

### Printing (Label + Receipt)
- Label printing is raw ZPL only; works only for ZPL-compatible printers. No fallback for non‑ZPL drivers.
- If `OpenPrinter` fails, the label print silently returns without user feedback.
- Receipt printing is plain 40‑column text on default printer settings (no A4 layout, no logo, no premium invoice formatting).
- Receipt lacks invoice number, GST number, customer address, item SKU, making charge, and weight breakdown beyond `NetWt`.
- A4 “premium looking” bill layout is not implemented; current output is a small thermal‑style receipt.

### Backup
- Backup “Now” uses a single static file copy; no file size checks, no verification, no retry, and no “last backup” status shown in UI.
- Auto‑backup path validation is minimal; no warning if AutoBackup enabled but path invalid.

### UX / Visual Consistency
- Sidebar toggle button remains in UI, but the sidebar width is fixed to 250; toggle action is now ineffective.
- Global button style forces white text; if any custom buttons use light backgrounds in future, text visibility could regress (no explicit contrast checks).

### Missing Checks / Observability
- No logging for print, backup, or database failures beyond basic message boxes.
- No health check for printer availability before attempting label/receipt print.
