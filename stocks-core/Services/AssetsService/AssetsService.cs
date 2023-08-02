using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using stocks.Clients.B3;
using stocks.Exceptions;
using stocks.Repositories;
using stocks_core.DTOs.B3;
using stocks_core.Models;
using stocks_core.Models.Responses;
using stocks_core.Requests.BigBang;
using stocks_core.Responses;
using stocks_core.Services.IncomeTaxes;
using stocks_infrastructure.Dtos;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_infrastructure.Repositories.Taxes;

namespace stocks.Services.IncomeTaxes;

public class AssetsService : IAssetsService
{
    private readonly IIncomeTaxesService incomeTaxesService;

    private readonly IGenericRepository<Account> genericRepositoryAccount;
    private readonly ITaxesRepository taxesRepository;
    private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

    private readonly IB3Client b3Client;
    private readonly ILogger<AssetsService> logger;

    public AssetsService(IIncomeTaxesService incomeTaxesService,
        IGenericRepository<Account> genericRepositoryAccount,
        ITaxesRepository incomeTaxesRepository,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client b3Client,
        ILogger<AssetsService> logger
        )
    {
        this.incomeTaxesService = incomeTaxesService;
        this.genericRepositoryAccount = genericRepositoryAccount;
        this.taxesRepository = incomeTaxesRepository;
        this.averageTradedPriceRepository = averageTradedPriceRepository;
        this.b3Client = b3Client;
        this.logger = logger;
    }

    #region Calcula todos os impostos de renda retroativos (Big Bang).
    public async Task BigBang(Guid accountId, List<BigBangRequest> request)
    {
        try
        {
            if (AlreadyExecutedBigBang(accountId))
            {
                logger.LogInformation("Big bang foi executado para o usuário {accountId}, mas ele já possui o preço médio e imposto de renda " +
                    $"calculado na base.", accountId);

                throw new InvalidBusinessRuleException("O preço médio e o imposto de renda para esse usuário já foram calculados.");
            }

            Account account = await genericRepositoryAccount.GetByIdAsync(accountId);

            // A B3 apenas possui dados a partir de 01/11/2019.
            string startDate = "2019-11-01";

            string lastMonth = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1)
                .AddMonths(-1)
                .ToString("yyyy-MM-dd");

            // var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, lastMonth, accountId);
            var b3Response = GetBigBangMockedDataBeforeB3Contract();

            var taxesAndAverageTradedPrices = await incomeTaxesService.Execute(b3Response, accountId);

            await SaveIncomeTaxes(taxesAndAverageTradedPrices, accountId);

            logger.LogInformation("Big Bang executado com sucesso para o usuário {accountId}.", accountId);
        }
        catch (Exception e)
        {
            logger.LogError("Uma exceção ocorreu ao executar o Big Bang do usuário {accountId}." +
                "{e.Message}", accountId, e.Message);

            throw;
        }
    }

    private static Movement.Root? GetBigBangMockedDataBeforeB3Contract()
    {
        Movement.Root? b3Response = new()
        {
            Data = new()
            {
                EquitiesPeriods = new()
                {
                    EquitiesMovements = new()
                }
            }
        };

        AddBigBangDataSet(b3Response);

        return b3Response;
    }

    private bool AlreadyExecutedBigBang(Guid accountId) =>
        averageTradedPriceRepository.AlreadyHasAverageTradedPrice(accountId);

    private async Task SaveIncomeTaxes((List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>) response, Guid accountId)
    {
        Account account = genericRepositoryAccount.GetById(accountId);

        List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes = new();
        CreateIncomeTaxes(response.Item1, incomeTaxes, account);

        List<AverageTradedPrice> averageTradedPrices = new();
        CreateAverageTradedPrices(response.Item2, averageTradedPrices, account);

        // TO-DO: unit of work
        await taxesRepository.AddAllAsync(incomeTaxes);
        await averageTradedPriceRepository.AddAllAsync(averageTradedPrices);
    }

    private static void CreateAverageTradedPrices(List<AverageTradedPriceDetails> response, List<AverageTradedPrice> averageTradedPrices, Account account)
    {
        foreach (var averageTradedPrice in response)
        {
            averageTradedPrices.Add(new AverageTradedPrice
            (
               averageTradedPrice.TickerSymbol,
               averageTradedPrice.AverageTradedPrice,
               averageTradedPrice.TradedQuantity,
               account,
               updatedAt: DateTime.UtcNow
            ));
        }
    }

    private static void CreateIncomeTaxes(List<AssetIncomeTaxes> assets, List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes, Account account)
    {
        foreach (var asset in assets)
        {
            if (MovementHadProfitOrLoss(asset))
            {
                incomeTaxes.Add(new stocks_infrastructure.Models.IncomeTaxes
                {
                    Month = asset.Month,
                    TotalTaxes = asset.Taxes,
                    TotalSold = asset.TotalSold,
                    SwingTradeProfit = asset.SwingTradeProfit,
                    DayTradeProfit = asset.DayTradeProfit,
                    TradedAssets = JsonConvert.SerializeObject(asset.TradedAssets),
                    Account = account,
                    AssetId = (int)asset.AssetTypeId,
                    CompesatedSwingTradeLoss = asset.SwingTradeProfit < 0 ? false : null,
                    CompesatedDayTradeLoss = asset.DayTradeProfit < 0 ? false : null
                });
            }
        }
    }

    private static bool MovementHadProfitOrLoss(AssetIncomeTaxes asset)
    {
        return asset.SwingTradeProfit != 0 || asset.DayTradeProfit != 0;
    }

    /// <summary>
    /// Retorna dados mockados da API da B3 para testes locais antes da contratação.
    /// Deve ser removido após a implementação do serviço de produção.
    /// </summary>
    private static void AddBigBangDataSet(Movement.Root response)
    {
        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 10.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 18.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 03)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA 11 Corporation Inc.",
            MovementType = "Venda",
            OperationValue = 12.54,
            UnitPrice = 12.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 08)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "IVVB11",
            CorporationName = "IVVB 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 245.65,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 09)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "IVVB11",
            CorporationName = "IVVB 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 246.65,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 09)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "IVVB11",
            CorporationName = "IVVB 11 Corporation Inc.",
            MovementType = "Venda",
            OperationValue = 304.54,
            UnitPrice = 304.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 10)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "KFOF11",
            CorporationName = "KFOF11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 231.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 16)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "KFOF11",
            CorporationName = "KFOF11 Corporation Inc.",
            MovementType = "Venda",
            OperationValue = 237.34,
            UnitPrice = 237.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 28)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            CorporationName = "Americanas S/A",
            MovementType = "Venda",
            OperationValue = 234.43,
            UnitPrice = 234.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 02, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            MovementType = "Compra",
            CorporationName = "Americanas S/A",
            OperationValue = 265.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 02, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            MovementType = "Compra",
            CorporationName = "Americanas S/A",
            OperationValue = 261.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 02, 01)
        });
    }

    #endregion

    #region Calcula o imposto de renda do mês atual.
    public async Task<MonthTaxesResponse> GetCurrentMonthTaxes(Guid accountId)
    {
        try
        {
            // Caso seja dia 1, não há como obter os dados do mês atual já que a B3 disponibiliza os dados em D-1.
            if (IsDayOne())
            {
                // Porém, sendo dia 1, o Worker já salvou os dados do mês passado na base.
                return await GetSpecifiedMonthTaxes(DateTime.Now.ToString("yyyy-MM"), accountId);
            }

            string startDate = DateTime.Now.ToString("yyyy-MM-01");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            Account account = await genericRepositoryAccount.GetByIdAsync(accountId);

            // var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, yesterday, account.Id);
            var b3Response = GetCurrentMonthMockedDataBeforeB3Contract();

            var response = await incomeTaxesService.Execute(b3Response, account.Id);

            return CurrentMonthToDto(response.Item1);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao calcular o imposto mensal devido. {e.Message}", e.Message);
            throw;
        }
    }

    private static Movement.Root? GetCurrentMonthMockedDataBeforeB3Contract()
    {
        Movement.Root? b3Response = new()
        {
            Data = new()
            {
                EquitiesPeriods = new()
                {
                    EquitiesMovements = new()
                }
            }
        };

        AddCurrentMonthSet(b3Response);

        return b3Response;
    }

    private static bool IsDayOne()
    {
        // D-1
        DateTime yesterday = DateTime.Now.AddDays(-1);
        return yesterday.Month < DateTime.Now.Month;
    }

    private static MonthTaxesResponse CurrentMonthToDto(List<AssetIncomeTaxes> assets)
    {
        double totalTaxes = assets.Select(x => x.Taxes).Sum();
        List<stocks_core.Responses.Asset> tradedAssets = new();

        foreach (var item in assets)
        {
            tradedAssets.Add(new stocks_core.Responses.Asset(
                item.AssetTypeId,
                item.AssetName,
                item.Taxes,
                item.TotalSold,
                item.SwingTradeProfit,
                item.DayTradeProfit,
                item.TradedAssets
            ));
        }

        return new MonthTaxesResponse(
            totalTaxes: totalTaxes,
            tradedAssets
        );
    }

    /// <summary>
    /// Retorna dados mockados da API da B3 para testes locais antes da contratação.
    /// Deve ser removido após a implementação do serviço de produção.
    /// </summary>
    private static void AddCurrentMonthSet(Movement.Root response)
    {
        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 19.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 34.65,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 03)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA11 Corporation Inc.",
            MovementType = "Venda",
            OperationValue = 10.43,
            UnitPrice = 10.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 08)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "VISC11",
            CorporationName = "VISC11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 231.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 16)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "VISC11",
            CorporationName = "VISC11 Corporation Inc.",
            MovementType = "Venda",
            OperationValue = 304.43,
            UnitPrice = 304.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 28)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            CorporationName = "Americanas S/A",
            MovementType = "Compra",
            OperationValue = 234.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 29)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            CorporationName = "Americanas S/A",
            MovementType = "Venda",
            OperationValue = 265.54,
            UnitPrice = 265.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 29)
        });
    }
    #endregion

    #region Calcula o imposto de renda do mês especificado.
    public async Task<MonthTaxesResponse> GetSpecifiedMonthTaxes(string month, Guid accountId)
    {
        try
        {
            if (WorkerDidNotSaveDataForThisMonthYet(month))
            {
                throw new InvalidBusinessRuleException("Para obter as informações de impostos do mês atual, acesse /assets/current.");
            }

            var response = await taxesRepository.GetSpecifiedMonthTaxes(System.Net.WebUtility.UrlDecode(month), accountId);

            return SpecifiedMonthToDto(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao calcular um imposto mensal devido especificado. {e.Message}", e.Message);
            throw;
        }
    }

    private static MonthTaxesResponse SpecifiedMonthToDto(IEnumerable<SpecifiedMonthTaxesDto> taxes)
    {
        double totalMonthTaxes = taxes.Select(x => x.Taxes).Sum();
        List<stocks_core.Responses.Asset> tradedAssets = new();

        foreach (var tax in taxes)
        {
            tradedAssets.Add(new stocks_core.Responses.Asset(
                (stocks_common.Enums.Asset)tax.AssetTypeId,
                tax.AssetName,
                tax.Taxes,
                tax.TotalSold,
                tax.SwingTradeProfit,
                tax.DayTradeProfit,
                JsonConvert.DeserializeObject<IEnumerable<OperationDetails>>(tax.TradedAssets)!
            ));
        }

        return new MonthTaxesResponse(
            totalMonthTaxes,
            tradedAssets
        );
    }

    private static bool WorkerDidNotSaveDataForThisMonthYet(string month)
    {
        string currentMonth = DateTime.Now.ToString("yyyy-MM");
        return month == currentMonth;
    }
    #endregion

    #region Calcula o imposto de renda do ano especificado
    public async Task<IEnumerable<YearTaxesResponse>> GetSpecifiedYearTaxes(string year, Guid accountId)
    {
        try
        {
            var response = await taxesRepository.GetSpecifiedYearTaxes(System.Net.WebUtility.UrlDecode(year), accountId);
            return ToSpecifiedYearDto(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao calcular um imposto mensal devido especificado. {e.Message}", e.Message);
            throw;
        }
    }

    private static IEnumerable<YearTaxesResponse> ToSpecifiedYearDto(IEnumerable<SpecifiedYearTaxesDto> taxes)
    {
        List<YearTaxesResponse> response = new();

        foreach (var item in taxes)
        {
            if (MonthAlreadyAdded(response, item)) continue;

            // Há registros duplicados para cada mês referente a cada ativo. O front-end
            // espera o valor total de imposto a ser pago no mês, e não para cada ativo. Por conta disso,
            // é feito o agrupamento.
            var taxesByMonth = taxes.Where(x => x.Month == item.Month);

            double totalTaxes = taxesByMonth.Select(x => x.Taxes).Sum();
            double totalSwingTradeProfit = taxesByMonth.Select(x => x.SwingTradeProfit).Sum();
            double totalDayTradeProfit = taxesByMonth.Select(x => x.DayTradeProfit).Sum();

            response.Add(new YearTaxesResponse(
                item.Month,
                totalTaxes,
                totalSwingTradeProfit,
                totalDayTradeProfit
            ));
        }

        return response;
    }

    private static bool MonthAlreadyAdded(IEnumerable<YearTaxesResponse> response, SpecifiedYearTaxesDto item)
    {
        return response.Select(x => x.Month).Contains(item.Month);
    }

    #endregion

    #region Altera um mês como pago/não pago
    public async Task SetMonthAsPaidOrUnpaid(string month, Guid accountId)
    {
        try
        {
            await taxesRepository.SetMonthAsPaidOrUnpaid(System.Net.WebUtility.UrlDecode(month), accountId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu uma exceção ao marcar um mês como pago/não pago. {message}", e.Message);
            throw;
        }
    }
    #endregion
}
