using InventoryApp.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace InventoryApp.Printing
{
    /// <summary>
    /// Sends raw ZPL bytes directly to any Windows printer driver (no print dialog).
    /// </summary>
    public static class LabelPrinter
    {
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int Level, ref DOCINFO pDocInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DOCINFO
        {
            [MarshalAs(UnmanagedType.LPTStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPTStr)] public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPTStr)] public string pDataType;
        }

        public static void PrintTag(string printerName, string shopName, string sku)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                System.Windows.MessageBox.Show(
                    $"Label Printer not configured.\nWould have printed tag for: {sku}\n\nSet a Label Printer in Settings.",
                    "Print Tag", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            // ZPL template  (50mm × 25mm label)
            var zpl = $@"^XA
^CF0,30
^FO20,10^FD{shopName}^FS
^FO20,45^BCN,50,N,N,N^FD{sku}^FS
^FO20,110^A0N,22,22^FD{sku}^FS
^XZ";
            SendRawToPrinter(printerName, zpl);
        }

        private static void SendRawToPrinter(string printerName, string zpl)
        {
            var bytes = Encoding.ASCII.GetBytes(zpl);
            var docInfo = new DOCINFO { pDocName = "JMS Label", pDataType = "RAW" };

            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero)) return;
            try
            {
                if (!StartDocPrinter(hPrinter, 1, ref docInfo)) return;
                StartPagePrinter(hPrinter);
                var ptr = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, ptr, bytes.Length);
                    WritePrinter(hPrinter, ptr, bytes.Length, out _);
                }
                finally { Marshal.FreeHGlobal(ptr); }
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            finally { ClosePrinter(hPrinter); }
        }
    }

    /// <summary>
    /// Silent receipt printing using System.Drawing.Printing.
    /// </summary>
    public static class ReceiptPrinter
    {
        public static void PrintReceipt(string printerName, string shopName, string address,
            string gstNumber, string customerName, DateTime date, IEnumerable<Models.CartItem> items,
            decimal subtotal, decimal discount, decimal cgst, decimal sgst,
            decimal grandTotal, string paymentMode)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                System.Windows.MessageBox.Show(
                    $"Receipt Printer not configured.\nTotal: ₹{grandTotal:N2}\n\nSet a Receipt Printer in Settings.",
                    "Print Receipt", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            using var doc = new System.Drawing.Printing.PrintDocument();
            doc.PrinterSettings.PrinterName = printerName;
            doc.PrintController = new System.Drawing.Printing.StandardPrintController();

            var lines = BuildReceiptLines(shopName, address, gstNumber, customerName, date,
                items, subtotal, discount, cgst, sgst, grandTotal, paymentMode);
            doc.PrintPage += (_, e) =>
            {
                if (e.Graphics == null) return;
                var font = new System.Drawing.Font("Courier New", 8);
                float y  = 10;
                foreach (var line in lines)
                {
                    e.Graphics.DrawString(line, font, System.Drawing.Brushes.Black, 10, y);
                    y += font.GetHeight();
                }
                font.Dispose();
            };
            doc.Print();
        }

        private static List<string> BuildReceiptLines(
            string shopName, string address, string gstNumber, string customerName,
            DateTime date, IEnumerable<Models.CartItem> items,
            decimal subtotal, decimal discount, decimal cgst, decimal sgst,
            decimal grandTotal, string paymentMode)
        {
            var lines = new List<string>();
            lines.Add(shopName.PadLeft((40 + shopName.Length) / 2));
            if (!string.IsNullOrWhiteSpace(address))   lines.Add(address);
            if (!string.IsNullOrWhiteSpace(gstNumber)) lines.Add($"GSTIN: {gstNumber}");
            lines.Add("");
            lines.Add($"Date    : {date:dd/MM/yyyy hh:mm tt}");
            lines.Add($"Customer: {customerName}");
            lines.Add(new string('-', 42));
            lines.Add($"{"Item",-20} {"Wt",6} {"Rate",7} {"Amt",7}");
            lines.Add(new string('-', 42));

            foreach (var item in items)
                lines.Add($"{item.Name[..Math.Min(item.Name.Length,18)],-18} {item.NetWt,6:N2} {item.Rate,7:N0} {item.FinalAmount,7:N0}");

            lines.Add(new string('-', 42));
            lines.Add($"{"Subtotal",-34} {subtotal,7:N2}");
            if (discount > 0)
                lines.Add($"{"Discount",-34}-{discount,6:N2}");
            lines.Add($"{"CGST",-34} {cgst,7:N2}");
            lines.Add($"{"SGST",-34} {sgst,7:N2}");
            lines.Add(new string('=', 42));
            lines.Add($"{"GRAND TOTAL",-34} {grandTotal,7:N2}");
            lines.Add($"Payment : {paymentMode}");
            lines.Add("");
            lines.Add("       Thank you! Visit again.       ");
            lines.Add("");
            return lines;
        }
    }
}
