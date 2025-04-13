using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrecanskaBackend.Controllers;
using StrecanskaBackend.Models;
using StrecanskaBackend.Models.Tables;

namespace StrecanskaBackend.ControllersTest
{
    public class StockDatasControllerTest
    {
        private AppDbContext GetDbContext()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            AppDbContext context = new(options);

            context.StockDatas.Add(new StockData { Id = 1, Price = 123.45m, Date = DateTime.Now, FavoriteTickers_id = 1 });
            context.SaveChanges();

            return context;
        }

        [Fact]
        public async Task GetStockDatas_ShouldReturnAllStockDatas()
        {
            // Arrange
            AppDbContext context = GetDbContext();
            StockDatasController controller = new(context);

            // Act
            ActionResult<IEnumerable<StockData>> result = await controller.GetStockDatas();

            // Assert
            result.Value.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetStockData_ShouldReturnStockData_WhenExists()
        {
            // Arrange
            AppDbContext context = GetDbContext();
            StockDatasController controller = new(context);

            // Act
            ActionResult<StockData> result = await controller.GetStockData(1);

            // Assert
            result.Result.Should().BeNull();
            result.Value!.Id.Should().Be(1);
        }

        [Fact]
        public async Task GetStockData_ShouldReturnNotFound_WhenNotExists()
        {
            AppDbContext context = GetDbContext();
            StockDatasController controller = new(context);

            ActionResult<StockData> result = await controller.GetStockData(999);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteStockData_ShouldDeleteAndReturnNoContent_WhenExists()
        {
            AppDbContext context = GetDbContext();
            StockDatasController controller = new(context);

            IActionResult result = await controller.DeleteStockData(1);

            result.Should().BeOfType<NoContentResult>();
            context.StockDatas.Any().Should().BeFalse();
        }

        [Fact]
        public async Task DeleteStockData_ShouldReturnNotFound_WhenNotExists()
        {
            AppDbContext context = GetDbContext();
            StockDatasController controller = new(context);

            IActionResult result = await controller.DeleteStockData(999);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateCurrentPricesAPI()
        {
            // Arrange
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext context = new(options);

            context.FavoriteTickers.Add(new FavoriteTicker
            {
                Id = 1,
                Ticker = "AAPL",
                Name = "Apple Inc.",
                Logo = "https://static2.finnhub.io/file/publicdatany/finnhubimage/stock_logo/AAPL.png"
            });

            context.SaveChanges();

            StockDatasController controller = new(context);

            IActionResult result = await controller.UpdateCurrentPrices();

            result.Should().BeOfType<OkObjectResult>();
            context.StockDatas.Count().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateCurrentPrices_ShouldSkip_WhenQuoteIsNullOrZero()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext context = new(options);

            context.FavoriteTickers.Add(new FavoriteTicker
            {
                Id = 1,
                Ticker = "---",
                Name = "Invalid Company",
                Logo = "https://example.com/logo.png"
            });

            context.SaveChanges();

            StockDatasController controller = new(context);

            IActionResult result = await controller.UpdateCurrentPrices();

            result.Should().BeOfType<OkObjectResult>();
            context.StockDatas.Count().Should().Be(0);
        }


    }
}
