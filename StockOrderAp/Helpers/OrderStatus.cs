namespace StockOrderApp.Helpers
{
    public static class OrderStatus
    {
        public const string Preparing = "Hazırlanıyor";
        public const string Shipped = "Kargoya Verildi";
        public const string Completed = "Tamamlandı";
        public const string Canceled = "İptal";

        public static readonly string[] All =
        {
            Preparing, Shipped, Completed, Canceled
        };
    }
}
