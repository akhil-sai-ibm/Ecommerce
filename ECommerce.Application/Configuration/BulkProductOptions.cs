namespace ECommerce.Application.Configuration
{
    public class BulkProductOptions
    {
        public const string SectionName = "BulkProduct";
        
        public int BatchSize { get; set; } = 100;
        public int MaxFileSizeMb { get; set; } = 10;
    }
}
