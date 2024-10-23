using Common.Exceptions;
using Core.Calculators;
using Core.Calculators.Assets;
using Core.Constants;
using Core.Models;
using Core.Models.B3;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.BonusShare;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json;
using static Core.Models.B3.Movement;

namespace Core.Services.B3ResponseCalculator
{
    public class B3ResponseCalculatorService : IB3ResponseCalculatorService
    {
        private IIncomeTaxesCalculator calculator;
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;
        private readonly IBonusShareRepository bonusShareRepository;
        private readonly ILogger<B3ResponseCalculatorService> logger;

        public B3ResponseCalculatorService(
            IIncomeTaxesCalculator calculator,
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IBonusShareRepository bonusShareRepository,
            ILogger<B3ResponseCalculatorService> logger)
        {
            this.calculator = calculator;
            this.averageTradedPriceRepository = averageTradedPriceRepository;
            this.bonusShareRepository = bonusShareRepository;
            this.logger = logger;
        }

        public async Task<InvestorMovementDetails?> Calculate(Root? b3Response, Guid accountId)
        {
            var movements = GetOnlyNecessaryMovements(b3Response);

            if (movements.IsNullOrEmpty())
                throw new NotFoundException("O usuário não possui nenhuma movimentação até então.");

            SetDayTradeMovementsAsDayTrade(movements);

            // A B3 não retorna todas as informações de bonificações que precisamos. Por isso, uma base externa e manualmente gerenciada em nossa base - a Fintz - é
            // utilizada.
            await SetBonusShareUnitPriceValue(movements);

            Dictionary<string, List<EquitMovement>> monthlyMovements = new();
            var monthsThatHadMovements = movements.Select(x => x.ReferenceDate.ToString("MM/yyyy")).Distinct();

            foreach (var month in monthsThatHadMovements)
            {
                var monthMovements = movements.Where(x => x.ReferenceDate.ToString("MM/yyyy") == month).ToList();
                monthlyMovements.Add(month, monthMovements);
            }

            InvestorMovementDetails? response = await CalculateTaxesAndAverageTradedPrices(monthlyMovements, accountId);
            return response;
        }

        private async Task SetBonusShareUnitPriceValue(List<EquitMovement> movements)
        {
            var bonusShareMovements = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.BonusShare));
            if (bonusShareMovements.IsNullOrEmpty()) return;

            foreach (var bonusShare in bonusShareMovements)
            {
                var bonusShareInformation = await bonusShareRepository.GetByTickerAndDate(
                    bonusShare.TickerSymbol,
                    bonusShare.ReferenceDate.Date);

                if (bonusShareInformation == null)
                {
                    logger.LogError("A B3 informou que no dia {date} o ticker {ticker} foi bonificado. " +
                        "Porém, o mesmo não foi encontrado na base da Fintz.", bonusShare.ReferenceDate, bonusShare.TickerSymbol);

                    throw new BadRequestException("O evento de bonificação da B3 não foi encontrado na base da Fintz.");
                }

                bonusShare.UnitPrice = bonusShareInformation.Price;
                bonusShare.OperationValue = bonusShareInformation.Price * bonusShare.EquitiesQuantity;
            }
        }

        private static List<EquitMovement> GetOnlyNecessaryMovements(Root? response)
        {
            if (response is null || response.Data is null) return Array.Empty<EquitMovement>().ToList();

            var movements = response.Data.EquitiesPeriods.EquitiesMovements;

            return movements.Where(x => x.IsBuy()|| x.IsSell() ||
                    x.MovementType.Equals(B3ResponseConstants.Split) ||
                    x.MovementType.Equals(B3ResponseConstants.ReverseSplit) ||
                    x.MovementType.Equals(B3ResponseConstants.BonusShare)).ToList();
        }

        private async Task<InvestorMovementDetails?> CalculateTaxesAndAverageTradedPrices(
            Dictionary<string,
            List<EquitMovement>> monthlyMovements,
            Guid accountId)
        {
            InvestorMovementDetails movementDetails = new();

            foreach (var monthMovements in monthlyMovements)
            {
                movementDetails.AverageTradedPrices.AddRange(await GetAverageTradedPricesIfAny(accountId, movementDetails.AverageTradedPrices));

                var stocks = monthMovements.Value.Where(x => x.ProductTypeName.Equals(B3ResponseConstants.Stocks));
                var etfs = monthMovements.Value.Where(x => x.ProductTypeName.Equals(B3ResponseConstants.ETFs));
                var fiis = monthMovements.Value.Where(x => x.ProductTypeName.Equals(B3ResponseConstants.FIIs));
                var bdrs = monthMovements.Value.Where(x => x.ProductTypeName.Equals(B3ResponseConstants.BDRs));
                var gold = monthMovements.Value.Where(x => x.ProductTypeName.Equals(B3ResponseConstants.Gold));
                var fundInvestments = monthMovements.Value.Where(x => x.ProductTypeName.Equals(B3ResponseConstants.InvestmentsFunds));

                // TODO factory design pattern

                if (stocks.Any())
                {
                    calculator = new StocksIncomeTaxes();
                    calculator.Execute(movementDetails, stocks, month: monthMovements.Key);
                }

                if (etfs.Any())
                {
                    calculator = new ETFsIncomeTaxes();
                    calculator.Execute(movementDetails, etfs, monthMovements.Key);
                }

                if (fiis.Any())
                {
                    calculator = new FIIsIncomeTaxes();
                    calculator.Execute(movementDetails, fiis, monthMovements.Key);
                }

                if (bdrs.Any())
                {
                    calculator = new BDRsIncomeTaxes();
                    calculator.Execute(movementDetails, bdrs, monthMovements.Key);
                }

                if (gold.Any())
                {
                    calculator = new GoldIncomeTaxes();
                    calculator.Execute(movementDetails, gold, monthMovements.Key);
                }

                if (fundInvestments.Any())
                {
                    calculator = new InvestmentsFundsIncomeTaxes();
                    calculator.Execute(movementDetails, fundInvestments, monthMovements.Key);
                }
            }

            return movementDetails;
        }

        /// <summary>
        /// Caso um investidor já possua o preço médio de um ticker salvo, é necessário usá-lo para calcular o novo preço médio desse ticker.
        /// </summary>
        /// <param name="movements">As movimentações daquele tipo de ativo.</param>
        private async Task<IEnumerable<AverageTradedPriceDetails>> GetAverageTradedPricesIfAny(
            Guid accountId,
            List<AverageTradedPriceDetails> averagePrices)
        {
            var allAverageTradedPrices = await averageTradedPriceRepository.GetAverageTradedPrices(accountId);
            var averageTradedPricesNotAddedYet = allAverageTradedPrices.Where(x => !averagePrices.Any(x => x.TickerSymbol == x.TickerSymbol)).ToList();

            return averageTradedPricesNotAddedYet
                .Select(x => new AverageTradedPriceDetails(x.Ticker, x.AverageTradedPrice, x.TotalBought, x.Quantity))
                .ToList();
        }

        /// <summary>
        /// Altera a propriedade booleana <c>EquitMovement.DayTraded</c> para verdadeiro em operações de venda day-trade.
        /// </summary>
        private static void SetDayTradeMovementsAsDayTrade(List<EquitMovement> movements)
        {
            var buys = movements.Where(x => x.IsBuy());
            var sells = movements.Where(x => x.IsSell());

            var dayTradeSellsOperationsIds = sells.Where(b => buys.Any(s =>
                s.ReferenceDate == b.ReferenceDate &&
                s.TickerSymbol == b.TickerSymbol
            )).Select(x => x.Id);

            foreach (var id in dayTradeSellsOperationsIds)
            {
                var dayTradeOperation = movements.Single(x => x.Id == id);
                dayTradeOperation.DayTraded = true;
            }
        }
    }
}
