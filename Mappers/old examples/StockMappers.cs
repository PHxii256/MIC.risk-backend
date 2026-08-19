// using MIC.risk.DTOs.Stock;
// using MIC.risk.Models;

// namespace MIC.risk.Mappers;

// public static class StockMappers
// {
//     public static StockDTO ToStockDto(this Stock stockModel)
//     {
//         return new StockDTO
//         {
//             Id = stockModel.Id,
//             Symbol = stockModel.Symbol,
//             Money = stockModel.Money
//         };
//     }

//     public static Stock ToStockFromCreateDto(this CreateStockDTO stockModel)
//     {
//         return new Stock
//         {
//             Symbol = stockModel.Symbol,
//             Money = stockModel.Money
//         };
//     }
// }