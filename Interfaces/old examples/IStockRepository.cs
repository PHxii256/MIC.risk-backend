// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using MIC.risk.DTOs.Stock;
// using MIC.risk.Models;

// namespace MIC.risk.Interfaces
// {
//     public interface IStockRepository
//     {
//         Task<List<Stock>> GetAllAsync();
//         Task<Stock?> GetByIdAsync(int id);
//         Task<Stock> CreateAsync(Stock stockModel);
//         Task<Stock?> UpdateAsync(int id, UpdateStockDto stockDto);
//         Task<Stock?> DeleteAsync(int id);
//     }
// }