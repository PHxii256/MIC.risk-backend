// using System;
// using System.Collections.Generic;
// using System.ComponentModel.DataAnnotations.Schema;
// using System.Linq;
// using System.Threading.Tasks;


// namespace MIC.risk.Models
// {
//     public class Stock
//     {
//         public int Id { get; set; }
//         public string Symbol { get; set; } = string.Empty;
//         [ColumnAttribute(TypeName = "decimal(18,2)")]
//         public decimal Money { get; set; }
//         List<Comment> Comments = new List<Comment>();
//     }
// }