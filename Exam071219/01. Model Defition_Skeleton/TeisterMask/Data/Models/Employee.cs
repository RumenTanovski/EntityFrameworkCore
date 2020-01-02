using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeisterMask.Data.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(40, MinimumLength =3)]
        [RegularExpression(@"^[a-zA-Z\d]+$")]
        public string  Username{ get; set; }

        [Required]
        [EmailAddress]
        public string Email  { get; set; }
        
        [Required]
        [RegularExpression("^[0-9]{3}-[0-9]{3}-[0-9]{4}$")]
        public string Phone { get; set; }

        public ICollection<EmployeeTask> EmployeesTasks { get; set; }
        = new HashSet<EmployeeTask>();
                
    }
}
