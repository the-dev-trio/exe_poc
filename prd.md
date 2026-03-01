This is a comprehensive Product Requirements Document (PRD) designed to turn your prototype into a commercial-grade, sellable product.

Since you are targeting 30,000 paying customers, the focus here is on Simplicity (UX), Reliability (No crashes), and Speed (POS flow).

PRD: Portable Jewellery Management System (JMS)

Product Name: Reddy Jewellery Manager (White-label ready)
Format: Single Containerized EXE (Portable)
Database: SQLite (Embedded)
Technology: C# / WPF / SQLite

1. Global Layout & Navigation

Objective: maximize screen real estate for data while keeping navigation accessible.

Top Header Bar (Fixed Height: 60px):

Left: Brand Name text: "Reddy Jewellery" (Configurable in settings later for other clients).

Right: Date/Time, "Backup Now" button (Crucial for thumb drive safety), User Profile Icon.

Left Sidebar (Collapsible, Width: 200px):

Dashboard

POS / Sales (Hot Key: F1)

Inventory (Stock Management)

Purchases (Inward Stock)

Settings

Main Content Area: Dynamic rendering based on selection.

2. Dashboard (Home)

Objective: Instant visibility of business health and daily rate setting.

Layout Grid:

Top Row: Daily Metal Rates (Input Section)

UI: A horizontal card strip. User inputs today's rate here. These rates drive the valuation logic.

Inputs:

Gold 24K (per gram)

Gold 22K (per gram)

Gold 18K (per gram)

Silver (per gram)

Action: "Update Rates" Button. (Triggers recalculation of Total Asset Value).

Middle Row: Asset Valuation (Read Only)

Card 1: Total Stock Count: (e.g., "1,240 Pcs")

Card 2: Total Weight: (e.g., "Gold: 5.2kg" | "Silver: 12kg")

Card 3: Total Asset Value: (Calculated: 
∑
(
𝐼
𝑡
𝑒
𝑚
𝑊
𝑒
𝑖
𝑔
ℎ
𝑡
×
𝑃
𝑢
𝑟
𝑖
𝑡
𝑦
𝑅
𝑎
𝑡
𝑒
)
∑(ItemWeight×PurityRate)
). This is the "Money on the table" metric.

Bottom Row: Quick Actions

Large Buttons: "New Sale", "Add Stock", "Low Stock Alerts".

3. Purchases (Inward Stock) - The Missing Module

Objective: Record items coming from suppliers before they become "Inventory".

Layout:

Header: "New Purchase Entry"

Form:

Supplier Name (Dropdown/Add New)

Invoice Number

Purchase Date

Item Entry Grid (Editable Table):

Columns: Category (Ring/Chain), Sub-Category, Metal Type (Gold/Silver), Purity (22k/24k), Gross Wt, Stone Wt, Net Wt, Cost Price.

Footer:

"Save & Add to Inventory" Button: This commits the data to the Inventory table and auto-generates Serial Numbers (SKU) for each row.

4. Inventory Management (The Core)

Objective: Manage Items, Categories, and Print Tags.

A. Categories Management (Tab 1)

Left Panel: List of Categories (e.g., Rings, Bangles, Anklets).

Right Panel: CRUD for selected Category.

Add Name, Default Metal Type (Gold/Silver).

B. Stock Items (Tab 2)

Top Bar: Search (by SKU or Name), Filter by Category, Filter by Status (In Stock/Sold).

Data Grid:

Columns: Serial # (SKU), Name, Category, Purity, Net Weight, Status.

Action Button: "Add Single Item" (Modal Popup).

C. Add Item Modal & Label Printing

Flow:

User enters: Category, Product Name, Purity, Net Weight.

System Auto-generates: Serial Number (SKU) (e.g., RG-2023-001).

User clicks "Save & Print Tag".

System Action: Saves to DB -> Sends raw ZPL/TSPL command to connected Label Printer -> Closes Modal.

Label Output (Zebra/TSPL):

Line 1: Reddy Jewellery (Bold, Centered)

Line 2: [Barcode Graphics representing SKU]

Line 3: SKU: RG-001 (Human readable serial)

Note: No weight printed, as requested.

5. Sales / Quick POS

Objective: Fast checkout. No mouse needed preferably.

Layout: Two-column split.

Left Column (Cart):

Input Box (Top): Scan Barcode / Type SKU.

Logic: When scanned, item is added to grid.

Price Calculation:

(Net Weight 
×
×
 Today's Metal Rate for that purity) + (Making Charges - manually entered or % preset).

Grid: Item Name, Weight, Rate, Making Charge, Final Amount.

Total Footer: Subtotal, GST/Tax, Grand Total.

Right Column (Customer & Payment):

Customer Mobile (Search/Add).

Customer Name.

Payment Mode: Cash, Card, UPI, Old Gold Exchange.

"Complete Sale" Button (Big Green Button):

Marks items as "Sold" in DB.

Records Transaction.

Silent Print: Triggers Receipt Printer immediately (No preview).

6. Settings (Configuration)

Objective: Plug-and-play hardware setup.

A. Printer Configuration

Printer 1: Label Printer (for Tags)

Dropdown: List all installed Windows Printers.

Button: "Test Print (Zebra Code)".

Setting: Label Dimensions (e.g., 50mm x 12mm).

Printer 2: Receipt Printer (for Bills)

Dropdown: List all installed Windows Printers.

Button: "Test Print (Receipt)".

Checkbox: "Enable Silent Printing" (If checked, skips Windows Print Dialog).

B. Business Info

Shop Name (Prints on top of Bill/Tag).

Address / Phone.

Tax/GST Number.

C. Backup

Location: Default to \Backups folder on the Thumb Drive.

Auto-backup on Close: Toggle On/Off.

7. Technical Implementation Details (Crucial for Developer)
Silent Printing Logic (C#)

Do not use PrintDialog.

For Tags (Zebra/TSC): Use RawPrinterHelper class. Send raw ZPL (Zebra Programming Language) strings directly to the printer driver name.

Why? It aligns perfectly with your "fast/no popup" requirement.

For Receipts: Use System.Drawing.Printing.PrintDocument.

Set printDoc.PrintController = new StandardPrintController(); to suppress the printing status dialog.

Call printDoc.Print(); directly.

Database Schema (SQLite)

Settings: (Key, Value) - Stores current metal rates, printer names.

Categories: (ID, Name, Type).

Inventory: (SKU [PK], Name, CategoryID, PurityEnum, GrossWt, NetWt, IsSold [Bool], CostPrice).

Transactions: (ID, Date, CustomerID, TotalAmount, JSON_LineItems).

The "Sellable" Polish

To make this worth paying for:

Exception Handling: Wrap the DB connection in a global Try/Catch. If the thumb drive is pulled out, show a clean error "Drive Disconnected" rather than crashing.

Relative Paths: Connection string must be Data Source=InvData.db;Version=3;. Never use C:\Users\....

Single Instance: Ensure only one instance of the EXE can run to prevent DB corruption (Mutex).

8. Development Roadmap

Phase 1 (Data): Finalize SQLite schema. Build Inventory CRUD.

Phase 2 (Hardware): Implement RawPrinterHelper. Get the Zebra tag printing working silently. This is the hardest part to debug without hardware, so mock it well.

Phase 3 (POS): Build the Sales screen with rate calculation logic.

Phase 4 (Dashboard): Connect the "Total Value" queries.

Phase 5 (Polish): UI Styling (Metro/Modern look), Keyboard shortcuts (F1 for Sales, Esc to Close).

This structure ensures you cover the "missing" parts (Purchases) while adhering to the strict speed and printing requirements.