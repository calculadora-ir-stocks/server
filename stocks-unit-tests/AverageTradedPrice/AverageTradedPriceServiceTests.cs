using Moq;
using stocks.Clients.B3;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_unit_tests.Builders;

namespace stocks_unit_tests.AverageTradedPrice
{
    public class AverageTradedPriceServiceTests
    {
        private readonly Mock<IAverageTradedPriceRepostory> _repository;
        private readonly Mock<IB3Client> _client;

        public AverageTradedPriceServiceTests()
        {
            _repository = new Mock<IAverageTradedPriceRepostory>();
            _client = new Mock<IB3Client>();
        }

        [Fact(DisplayName ="Deve calcular o preço médio de um ativo.")]
        public async void Should_calculate_average_traded_price_of_one_asset_correctly()
        {            
            _repository.Setup(r => r.AccountAlreadyHasAverageTradedPrice(It.IsAny<Guid>())).Returns(false);

            Movement.Root movements = new MovementBuilder()
                .WithBuy((13 * 2), 2, "PETR4")
                .WithBuy((16 * 8), 8, "PETR4")
                .WithSell((14 * 5), 5, "PETR4")
                .Build();

            double averageTradedPrice = 16.8;

            var task = Task.Run(() => movements);

            _client.Setup(c => c.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(task);

            var clientResponse = await _client.Object.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            var buyOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Buy);

            var sellOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                .Where(x => x.MovementType == B3ServicesConstants.Sell);

            var splitsOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Split);

            var response = AverageTradedPriceService.CalculateAverageTradedPrice(buyOperations, sellOperations, splitsOperations);

            Assert.Equal(averageTradedPrice, response["PETR4"].AverageTradedPrice);
        }

        [Fact(DisplayName = "Deve calcular o preço médio de dois ativos diferentes.")]
        public async void Should_calculate_average_traded_price_of_two_different_assets_correctly()
        {
            _repository.Setup(r => r.AccountAlreadyHasAverageTradedPrice(It.IsAny<Guid>())).Returns(false);

            Movement.Root movements = new MovementBuilder()
                .WithBuy((13 * 2), 2, "PETR4")
                .WithSell((14 * 1), 1, "PETR4")
                .WithBuy((12 * 3), 3, "GOOGL34")
                .WithBuy((9 * 8), 8, "GOOGL34")
                .WithBuy((15 * 2), 2, "GOOGL34")
                .Build();

            double averageTradedPricePETR4 = 12;
            double averageTradedPriceGOOGL34 = 10.62;

            var task = Task.Run(() => movements);

            _client.Setup(c => c.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(task);

            var clientResponse = await _client.Object.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            var buyOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Buy);

            var sellOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                .Where(x => x.MovementType == B3ServicesConstants.Buy);

            var splitsOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Split);

            var response = AverageTradedPriceService.CalculateAverageTradedPrice(buyOperations, sellOperations, splitsOperations);

            Assert.Equal(averageTradedPricePETR4, response["PETR4"].AverageTradedPrice);
            Assert.Equal(averageTradedPriceGOOGL34, response["GOOGL34"].AverageTradedPrice);
        }

        [Fact(DisplayName = "Deve retornar nada caso o investidor tenha vendido todos os ativos (ordem crescente).")]
        public async void Should_return_empty_if_investor_sold_all_assets()
        {
            _repository.Setup(r => r.AccountAlreadyHasAverageTradedPrice(It.IsAny<Guid>())).Returns(false);

            // Bought and sold all assets
            Movement.Root movements = new MovementBuilder()
                .WithBuy((13 * 2), 2, "PETR4")
                .WithBuy((14 * 1), 1, "PETR4")
                .WithSell((15 * 3), 3, "PETR4")
                .Build();

            var task = Task.Run(() => movements);

            _client.Setup(c => c.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(task);

            var clientResponse = await _client.Object.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            var buyOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Buy);

            var sellOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                .Where(x => x.MovementType == B3ServicesConstants.Sell);

            var splitsOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Split);

            var response = AverageTradedPriceService.CalculateAverageTradedPrice(buyOperations, sellOperations, splitsOperations);

            Assert.Empty(response);
        }

        [Fact(DisplayName = "Deve retornar nada caso o investidor tenha vendido todos os ativos (ordem decrescente).")]
        public async void Should_return_empty_if_investor_sold_all_assets_and_movements_are_inverted()
        {
            _repository.Setup(r => r.AccountAlreadyHasAverageTradedPrice(It.IsAny<Guid>())).Returns(false);

            // Bought and sold all assets
            Movement.Root movements = new MovementBuilder()
                .WithSell((15 * 3), 3, "PETR4")
                .WithBuy((13 * 2), 2, "PETR4")
                .WithBuy((14 * 1), 1, "PETR4")
                .Build();

            var task = Task.Run(() => movements);

            _client.Setup(c => c.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(task);

            var clientResponse = await _client.Object.GetAccountMovement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

            var buyOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Buy);

            var sellOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                .Where(x => x.MovementType == B3ServicesConstants.Sell);

            var splitsOperations = clientResponse.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Split);

            var response = AverageTradedPriceService.CalculateAverageTradedPrice(buyOperations, sellOperations, splitsOperations);

            Assert.Empty(response);
        }
    }
}
