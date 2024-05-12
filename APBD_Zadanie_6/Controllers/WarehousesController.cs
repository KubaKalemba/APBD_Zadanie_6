using System.Data.SqlClient;
using APBD_Task_6.Models;
using Microsoft.AspNetCore.Mvc;
using Zadanie5.Services;

namespace Zadanie5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        private readonly string _connectionString;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
            _connectionString = "";
        }

        [HttpPost]
        public ActionResult AddProduct(ProductWarehouse product)
        {
            _warehouseService.AddProduct(product);
            return Ok();
        }
        
        [HttpPost("addProductToWarehouse")]
        public IActionResult AddProductToWarehouse([FromBody] ProductWarehouseRequest request)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                if (!ProductExists(connection, request.IdProduct) || !WarehouseExists(connection, request.IdWarehouse) || request.Amount <= 0)
                {
                    return BadRequest("Invalid product, warehouse or amount.");
                }

                if (PurchaseOrderExists(connection, request.IdProduct, request.Amount))
                {
                    return BadRequest("No correspoding order.");
                }

                if (OrderFulfilled(connection, request.IdProduct, request.Amount, request.CreatedAt))
                {
                    return BadRequest("The order has already been fulfilled.");
                }

                UpdateOrder(connection, request.IdProduct, request.Amount);

                var productWarehouseId = InsertProduct(connection, request.IdProduct, request.IdWarehouse, request.Amount, request.CreatedAt);

                return Ok(productWarehouseId);
            }
        }
        private static bool ProductExists(SqlConnection connection, int productId)
        {
            var command = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Id = @ProductId", connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            return (int)command.ExecuteScalar() > 0;
        }

        private static bool WarehouseExists(SqlConnection connection, int warehouseId)
        {
            var command = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Id = @WarehouseId", connection);
            command.Parameters.AddWithValue("@WarehouseId", warehouseId);
            return (int)command.ExecuteScalar() > 0;
        }

        private static bool PurchaseOrderExists(SqlConnection connection, int productId, int amount)
        {
            var command = new SqlCommand("SELECT COUNT(*) FROM ORDER WHERE Id = @ProductId AND amount=@amount",
                connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@amount", amount);
            return (int)command.ExecuteScalar() > 0;
        }

        private static bool OrderFulfilled(SqlConnection connection, int productId, int amount, DateTime createdAt)
        {
            var command = new SqlCommand("SELECT COUNT(*) FROM Product_Warehouse WHERE IdProduct = @ProductId AND Amount = @Amount", connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            var count = (int)command.ExecuteScalar();
            return count > 0;
        }

        private static void UpdateOrder(SqlConnection connection, int productId, int amount)
        {
            var command = new SqlCommand("UPDATE Orders SET FullfilledAt = @CurrentDateTime WHERE IdProduct = @ProductId AND Amount = @Amount", connection);
            command.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@Amount", amount);
            command.ExecuteNonQuery();
        }
        
        private static int InsertProduct(SqlConnection connection, int productId, int warehouseId, int amount, DateTime createdAt)
        {
            var productPrice = GetProductPrice(connection, productId);

            var command = new SqlCommand("INSERT INTO Product_Warehouse (IdProduct, IdWarehouse, Amount, Price, CreatedAt) VALUES (@ProductId, @WarehouseId, @Amount, @Price, @CreatedAt); SELECT SCOPE_IDENTITY();", connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@WarehouseId", warehouseId);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@Price", productPrice * amount);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);

            var productWarehouseId = Convert.ToInt32(command.ExecuteScalar());
            return productWarehouseId;
        }
        
        private static decimal GetProductPrice(SqlConnection connection, int productId)
        {
            var command = new SqlCommand("SELECT Price FROM Products WHERE Id = @ProductId", connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            var result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDecimal(result);
            }
            throw new Exception("Product price not found.");
        }
        
    }
    
    public class ProductWarehouseRequest
    {
        public int IdProduct { get; set; }
        public int IdWarehouse { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
