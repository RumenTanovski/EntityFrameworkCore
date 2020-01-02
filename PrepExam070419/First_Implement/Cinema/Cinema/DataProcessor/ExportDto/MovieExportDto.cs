using System.Collections.Generic;

namespace Cinema.DataProcessor.ExportDto
{
    public class MovieExportDto
    {
        public string MovieName { get; set; }

        public string Rating { get; set; }

        public string TotalIncomes { get; set; }

        public ICollection<CustomerMovieExportDto> Customers { get; set; }
    }
}
