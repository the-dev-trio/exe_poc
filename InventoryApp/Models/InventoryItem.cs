namespace InventoryApp.Models
{
    public enum PurityType { Gold24K, Gold22K, Gold18K, Silver }
    public enum MetalType { Gold, Silver }
    public enum PaymentMode { Cash, Card, UPI, OldGoldExchange }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public MetalType MetalType { get; set; } = MetalType.Gold;
    }

    public class InventoryItem
    {
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public PurityType Purity { get; set; } = PurityType.Gold22K;

        /// <summary>String shim for ComboBox binding in XAML.</summary>
        public string PurityStr
        {
            get => Purity.ToString();
            set { if (Enum.TryParse<PurityType>(value, out var p)) Purity = p; }
        }

        public decimal GrossWt { get; set; }
        public decimal StoneWt { get; set; }
        public decimal NetWt { get; set; }
        public int    Pcs { get; set; } = 1;
        public bool IsSold { get; set; }
        public decimal CostPrice { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TransactionRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string LineItemsJson { get; set; } = string.Empty;
    }

    public class CartItem
    {
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal NetWt { get; set; }
        public PurityType Purity { get; set; }
        public decimal Rate { get; set; }       // Today's metal rate
        public decimal MakingCharge { get; set; }
        public decimal FinalAmount => (NetWt * Rate) + MakingCharge;
    }

    public class MetalRates
    {
        public decimal Gold24K { get; set; }
        public decimal Gold22K { get; set; }
        public decimal Gold18K { get; set; }
        public decimal Silver { get; set; }

        public decimal GetRate(PurityType purity) => purity switch
        {
            PurityType.Gold24K => Gold24K,
            PurityType.Gold22K => Gold22K,
            PurityType.Gold18K => Gold18K,
            PurityType.Silver  => Silver,
            _ => 0
        };
    }

    public class PurchaseRecord
    {
        public int    Id            { get; set; }
        public DateTime Date        { get; set; } = DateTime.Now;
        public int    SupplierId    { get; set; }
        public string SupplierName  { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalWeight  { get; set; }
        public int    ItemCount     { get; set; }
        public string LineItemsJson { get; set; } = "[]";
    }
}
