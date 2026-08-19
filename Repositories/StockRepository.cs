// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using MIC.risk.Data;
// using MIC.risk.DTOs.Stock;
// using MIC.risk.Interfaces;
// using MIC.risk.Models;
// using Microsoft.EntityFrameworkCore;

// namespace MIC.risk.Repositories
// {
//     public class StockRepository : IStockRepository
//     {
//         private readonly ApplicationDBContext _context;
//         public StockRepository(ApplicationDBContext context)
//         {
//             _context = context;
//         }

//         public async Task<Stock> CreateAsync(Stock stockModel)
//         {
//             await _context.Stock.AddAsync(stockModel);
//             await _context.SaveChangesAsync();
//             return stockModel;
//         }

//         public async Task<Stock?> DeleteAsync(int id)
//         {
//             var stockModel = await _context.Stock.FirstOrDefaultAsync(x => x.Id == id);

//             if (stockModel == null)
//             {
//                 return null;
//             }

//             _context.Stock.Remove(stockModel);
//             await _context.SaveChangesAsync();
//             return stockModel;
//         }

//         public async Task<List<Stock>> GetAllAsync()
//         {
//             return await _context.Stock.ToListAsync();
//         }

//         public async Task<Stock?> GetByIdAsync(int id)
//         {
//             return await _context.Stock.FindAsync(id);
//         }

//         public async Task<Stock?> UpdateAsync(int id, UpdateStockDto stockDto)
//         {
//             var existingStock = await _context.Stock.FirstOrDefaultAsync(x => x.Id == id);

//             if (existingStock == null)
//             {
//                 return null;
//             }

//             existingStock.Symbol = stockDto.Symbol;
//             existingStock.Money = stockDto.Money;

//             await _context.SaveChangesAsync();

//             return existingStock;
//         }
//     }
// }