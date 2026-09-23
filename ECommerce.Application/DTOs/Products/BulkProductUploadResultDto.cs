using System.Collections.Generic;

namespace ECommerce.Application.DTOs.Products
{
    public class BulkProductUploadResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessfulRows { get; set; }
        public int FailedRows { get; set; }
        public int BatchesProcessed { get; set; }
        public List<BulkProductErrorDto> Errors { get; set; } = new List<BulkProductErrorDto>();
    }
}
