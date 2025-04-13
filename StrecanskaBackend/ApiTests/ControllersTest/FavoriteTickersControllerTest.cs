using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrecanskaBackend.Controllers;
using StrecanskaBackend.Models;
using StrecanskaBackend.Models.Tables;

namespace StrecanskaBackend.ControllersTest
{
    public class FavoriteTickersControllerTest
    {
        private AppDbContext GetDbContext()
        {
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
            return context;
        }

        [Fact]
        public async Task GetFavoriteTickers_ShouldReturnAll()
        {
            AppDbContext context = GetDbContext();
            FavoriteTickersController controller = new(context);

            ActionResult<IEnumerable<FavoriteTicker>> result = await controller.GetFavoriteTickers();

            // Assert
            result.Value.Should().HaveCount(1);
            result.Value!.First().Ticker.Should().Be("AAPL");
        }

        [Fact]
        public async Task GetFavoriteTicker_ShouldReturnCorrectTicker_WhenExists()
        {
            AppDbContext context = GetDbContext();
            FavoriteTickersController controller = new(context);

            ActionResult<FavoriteTicker> result = await controller.GetFavoriteTicker(1);

            result.Result.Should().BeNull();
            result.Value!.Name.Should().Be("Apple Inc.");
        }

        [Fact]
        public async Task GetFavoriteTicker_ShouldReturnNotFound_WhenNotExists()
        {
            AppDbContext context = GetDbContext();
            FavoriteTickersController controller = new(context);

            ActionResult<FavoriteTicker> result = await controller.GetFavoriteTicker(999);

            result.Result.Should().BeOfType<NotFoundResult>();
        }


        [Fact]
        public async Task DeleteFavoriteTicker_ShouldRemove_WhenExistsWithoutStockData()
        {
            AppDbContext context = GetDbContext();
            FavoriteTickersController controller = new(context);

            IActionResult result = await controller.DeleteFavoriteTicker(1);

            result.Should().BeOfType<NoContentResult>();
            context.FavoriteTickers.Any().Should().BeFalse();
        }

        [Fact]
        public async Task DeleteFavoriteTicker_ShouldRemoveTickerAndRelatedStockData_WhenTheyExist()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext context = new(options);

            context.FavoriteTickers.Add(new FavoriteTicker
            {
                Id = 1,
                Ticker = "TSLA",
                Name = "Tesla",
                Logo = "https://logo.clearbit.com/tesla.com"
            });

            context.StockDatas.Add(new StockData
            {
                Id = 1,
                FavoriteTickers_id = 1,
                Price = 999.99m,
                Date = DateTime.Now
            });

            context.SaveChanges();

            FavoriteTickersController controller = new(context);

            IActionResult result = await controller.DeleteFavoriteTicker(1);

            result.Should().BeOfType<NoContentResult>();
            context.FavoriteTickers.Any().Should().BeFalse();
            context.StockDatas.Any().Should().BeFalse();
        }

        [Fact]
        public async Task DeleteFavoriteTicker_ShouldReturnNotFound_WhenTickerDoesNotExist()
        {
            AppDbContext context = GetDbContext();
            FavoriteTickersController controller = new(context);

            IActionResult result = await controller.DeleteFavoriteTicker(999);

            result.Should().BeOfType<NotFoundResult>();
        }


        [Fact]
        public async Task PostFavoriteTicker_ShouldReturnBadRequest_WhenTickerIsEmpty()
        {
            AppDbContext context = GetDbContext();
            FavoriteTickersController controller = new(context);

            ActionResult<FavoriteTicker> result = await controller.PostFavoriteTicker("");

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            (result.Result as BadRequestObjectResult)!.Value.Should().Be("Ticker is empty.");
        }

        [Fact]
        public async Task PostFavoriteTicker_ShouldReturnBadRequest_WhenTickerDoesNotExistOnApi()
        {
            AppDbContext context = GetDbContext();
            FavoriteTickersController controller = new(context);

            ActionResult<FavoriteTicker> result = await controller.PostFavoriteTicker("NOTAREALTICKERXYZ");

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            (result.Result as BadRequestObjectResult)!.Value!.ToString()!
                .Should().Contain("not found");
        }

        [Fact]
        public async Task PostFavoriteTicker_ShouldReturnConflict_WhenTickerAlreadyExists()
        {
            AppDbContext context = GetDbContext();
            context.FavoriteTickers.Add(new FavoriteTicker
            {
                Ticker = "AAPL",
                Name = "Apple Inc.",
                Logo = "https://logo.clearbit.com/apple.com"
            });
            context.SaveChanges();

            FavoriteTickersController controller = new(context);

            ActionResult<FavoriteTicker> result = await controller.PostFavoriteTicker("AAPL");

            result.Result.Should().BeOfType<ConflictObjectResult>();
            (result.Result as ConflictObjectResult)!.Value.Should().Be("Ticker is already in favorites.");
        }

        [Fact]
        public async Task PostFavoriteTicker_ShouldCreateFavoriteTicker_WhenValidAndNotExists()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext context = new(options);
            FavoriteTickersController controller = new(context);

            ActionResult<FavoriteTicker> result = await controller.PostFavoriteTicker("MSFT");

            result.Result.Should().BeOfType<CreatedAtActionResult>();

            CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
            createdResult!.Value.Should().BeOfType<FavoriteTicker>();

            FavoriteTicker? ticker = createdResult.Value as FavoriteTicker;
            ticker!.Ticker.Should().Be("MSFT");
            context.FavoriteTickers.Count().Should().Be(1);
        }


        [Fact]
        public async Task ProcessTickers_ShouldReturnOk_WhenApiRespondsSuccessfully()
        {
            FavoriteTickersController controller = new(GetDbContext());

            List<TickerWithRating> tickers = new()
                    {
                new() { Ticker = "AAPL", Name = "Apple", Logo = "", Rating = 2 },
                new() { Ticker = "TSLA", Name = "Tesla", Logo = "", Rating = 7 }
            };

            ActionResult<List<TickerWithRecommendation>> result = await controller.ProcessTickers(tickers, 5);

            result.Result.Should().BeAssignableTo<ObjectResult>();

            ObjectResult? objectResult = result.Result as ObjectResult;

            if (objectResult!.StatusCode == 200)
            {
                objectResult.Should().BeOfType<OkObjectResult>();

                List<TickerWithRecommendation>? list = objectResult.Value as List<TickerWithRecommendation>;
                list!.Count.Should().Be(2);
            }
            else
            {
                objectResult.StatusCode.Should().NotBe(200);
                objectResult.Value!.ToString()!.Should().Contain("Nepodařilo se odeslat doporučení");
            }
        }

        [Fact]
        public async Task GetRatings_ShouldReturnBadRequest_WhenTickerListIsEmpty()
        {
            AppDbContext context = new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            FavoriteTickersController controller = new(context);

            ActionResult<List<TickerWithRating>> result = await controller.GetRatings(new List<string>());

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            (result.Result as BadRequestObjectResult)!.Value.Should().Be("Seznam tickerů je prázdný.");
        }

        [Fact]
        public async Task GetRatings_ShouldReturnObjectResult_WhenTickersAreValid()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext context = new(options);
            context.FavoriteTickers.Add(new FavoriteTicker
            {
                Ticker = "AAPL",
                Name = "Apple Inc.",
                Logo = "https://logo.clearbit.com/apple.com"
            });
            context.FavoriteTickers.Add(new FavoriteTicker
            {
                Ticker = "TSLA",
                Name = "Tesla Inc.",
                Logo = "https://logo.clearbit.com/tesla.com"
            });
            context.SaveChanges();

            FavoriteTickersController controller = new(context);

            ActionResult<List<TickerWithRating>> result = await controller.GetRatings(new List<string> { "AAPL", "TSLA" });

            result.Result.Should().BeAssignableTo<ObjectResult>();

            ObjectResult? objectResult = result.Result as ObjectResult;

            if (objectResult!.StatusCode == 200 || objectResult is OkObjectResult)
            {
                List<TickerWithRating>? list = objectResult.Value as List<TickerWithRating>;
                list.Should().NotBeNull();
                list!.Count.Should().Be(2);
                list.Any(r => r.Ticker == "AAPL").Should().BeTrue();
                list.Any(r => r.Ticker == "TSLA").Should().BeTrue();
            }
            else
            {
                objectResult.StatusCode.Should().NotBe(200);
                objectResult.Value!.ToString()!.Should().Contain("Nepodařilo se získat hodnocení");
            }
        }

        [Fact]
        public async Task GetFilteredPrices_ShouldReturnLatestPriceForEachTicker_WhenFilterIdIs1()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            AppDbContext context = new(options);

            FavoriteTicker ticker = new() { Id = 1, Ticker = "AAPL", Name = "Apple", Logo = "logo" };
            context.FavoriteTickers.Add(ticker);

            context.StockDatas.AddRange(
                new StockData { FavoriteTickers_id = 1, Price = 100, Date = DateTime.UtcNow.AddDays(-2) },
                new StockData { FavoriteTickers_id = 1, Price = 120, Date = DateTime.UtcNow }
            );
            context.SaveChanges();

            FavoriteTickersController controller = new(context);

            ActionResult<IEnumerable<TickerWithPrice>> result = await controller.GetFilteredPrices(1);
            OkObjectResult? okResult = result.Result as OkObjectResult;
            List<TickerWithPrice>? data = okResult!.Value as List<TickerWithPrice>;

            data.Should().HaveCount(1);
            data[0].LatestPrice.Should().Be(120);
        }


        [Fact]
        public async Task GetFilteredPrices_ShouldReturnTickersWith3DayGrowthOrStagnation_WhenFilterIdIs2()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext context = new(options);

            FavoriteTicker ticker = new()
            {
                Id = 1,
                Ticker = "TSLA",
                Name = "Tesla",
                Logo = "logo"
            };
            context.FavoriteTickers.Add(ticker);

            DateTime today = DateTime.UtcNow.Date;

            context.StockDatas.AddRange(
                new StockData { FavoriteTickers_id = 1, Price = 120, Date = today.AddDays(-2).AddHours(10) },
                new StockData { FavoriteTickers_id = 1, Price = 130, Date = today.AddDays(-1).AddHours(10) },
                new StockData { FavoriteTickers_id = 1, Price = 140, Date = today.AddHours(10) }
            );
            context.SaveChanges();

            FavoriteTickersController controller = new(context);
            ActionResult<IEnumerable<TickerWithPrice>> result = await controller.GetFilteredPrices(2);

            OkObjectResult? okResult = result.Result as OkObjectResult;
            List<TickerWithPrice>? data = okResult!.Value as List<TickerWithPrice>;

            data.Should().HaveCount(1);
            data[0].Ticker.Should().Be("TSLA");
            data[0].LatestPrice.Should().Be(140);
        }




        [Fact]
        public async Task GetFilteredPrices_ShouldReturnTickersWithMax2Declines_WhenFilterIdIs3()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            AppDbContext context = new(options);

            FavoriteTicker ticker = new() { Id = 1, Ticker = "NVDA", Name = "NVIDIA", Logo = "logo" };
            context.FavoriteTickers.Add(ticker);

            DateTime now = DateTime.UtcNow.Date;
            context.StockDatas.AddRange(
                new StockData { FavoriteTickers_id = 1, Price = 120, Date = now.AddDays(-4) },
                new StockData { FavoriteTickers_id = 1, Price = 115, Date = now.AddDays(-3) },
                new StockData { FavoriteTickers_id = 1, Price = 117, Date = now.AddDays(-2) },
                new StockData { FavoriteTickers_id = 1, Price = 113, Date = now.AddDays(-1) },
                new StockData { FavoriteTickers_id = 1, Price = 110, Date = now }
            );
            context.SaveChanges();

            FavoriteTickersController controller = new(context);
            ActionResult<IEnumerable<TickerWithPrice>> result = await controller.GetFilteredPrices(3);
            OkObjectResult? okResult = result.Result as OkObjectResult;
            List<TickerWithPrice>? data = okResult!.Value as List<TickerWithPrice>;

            data.Should().HaveCount(0);
        }

        [Fact]
        public async Task GetFilteredPrices_ShouldReturnBadRequestForInvalidFilter()
        {
            AppDbContext context = new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            FavoriteTickersController controller = new(context);
            ActionResult<IEnumerable<TickerWithPrice>> result = await controller.GetFilteredPrices(999);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            (result.Result as BadRequestObjectResult)!.Value.Should().Be("Neznámý filtr.");
        }




    }
}

