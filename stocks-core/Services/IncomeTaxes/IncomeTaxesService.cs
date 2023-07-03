using Microsoft.Extensions.Logging;
using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks_common.Models;
using stocks_core.Calculators;
using stocks_core.DTOs.B3;
using stocks_core.Models;
using stocks_core.Requests.BigBang;
using stocks_core.Requests.IncomeTaxes;
using stocks_core.Services.BigBang;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_infrastructure.Repositories.IncomeTaxes;

namespace stocks.Services.IncomeTaxes;

public class IncomeTaxesService : IIncomeTaxesService
{
    private readonly IIncomeTaxesCalculator incomeTaxCalculator;

    private readonly IGenericRepository<Account> genericRepositoryAccount;
    private readonly IIncomeTaxesRepository incomeTaxesRepository;
    private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

    private readonly IB3Client b3Client;

    private readonly ILogger<IncomeTaxesService> logger;

    public IncomeTaxesService(IIncomeTaxesCalculator incomeTaxCalculator,
        IGenericRepository<Account> genericRepositoryAccount,
        IIncomeTaxesRepository incomeTaxesRepository,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client b3Client,
        ILogger<IncomeTaxesService> logger
        )
    {
        this.incomeTaxCalculator = incomeTaxCalculator;
        this.genericRepositoryAccount = genericRepositoryAccount;
        this.incomeTaxesRepository = incomeTaxesRepository;
        this.averageTradedPriceRepository = averageTradedPriceRepository;
        this.b3Client = b3Client;
        this.logger = logger;
    }

    #region Calcula todos os impostos de renda retroativos.
    public async Task BigBang(Guid accountId, List<BigBangRequest> request)
    {
        try
        {
            if (AlreadyHasAverageTradedPrice(accountId))
            {
                logger.LogInformation($"Big bang foi executado para o usuário {accountId}, mas ele já possui o preço médio e imposto de renda " +
                    $"calculado na base.");
                return;
            }

            string minimumAllowedStartDate = "2019-11-01";

            /**
             * O fluxo funcionará começando pelo Big Bang, onde o imposto devido de todos os meses retroativos será salvo na base de dados.
             * O endpoint para calcular o imposto a ser pago no mês atual irá levar em consideração o primeiro dia do mês atual até D-1.
             * Quando o mês atual virar, o Job que irá rodar todo fim de mês irá calcular o imposto de renda a ser pago no mês em questão e irá salvar na base.
             * 
             * TODO: O Big Bang não deve salvar na base o imposto de renda a ser pago no mês atual se o mês ainda não encerrou. Se for dia 10, por exemplo,
             * deverá salvar na base apenas o imposto a ser pago no mês passado. Dessa forma, o endpoint de pagar o imposto do mês atual irá calcular do primeiro dia
             * útil até o dia 10 e, apenas no último dia do mês, o Job irá rodar e salvar na base de dados o imposto devido desse mês em questão.
             * Entretanto, o preço médio deverá sim ser calculado até D-1.
             * **/

            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            Movement.Root? response = new()
            {
                Data = new()
                {
                    EquitiesPeriods = new()
                    {
                        EquitiesMovements = new()
                    }
                }
            };

            AddBigBangDataSet(response);

            BigBang bigBang = new(incomeTaxCalculator, averageTradedPriceRepository);
            var taxesToBePaid = await bigBang.Execute(response, accountId);

            await SaveBigBang(taxesToBePaid, accountId);

            logger.LogInformation($"Big Bang executado com sucesso para o usuário {accountId}.");
        } catch (Exception e)
        {
            logger.LogError($"Uma exceção ocorreu ao executar o Big Bang do usuário {accountId}." +
                $"{e.Message}");

            throw;
        }
    }

    private static void AddBigBangDataSet(Movement.Root response)
    {
        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            MovementType = "Compra",
            OperationValue = 10.43,
            EquitiesQuantity = 2,
            ReferenceDate = new DateTime(2022, 01, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            MovementType = "Compra",
            OperationValue = 18.43,
            EquitiesQuantity = 3,
            ReferenceDate = new DateTime(2022, 01, 03)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            MovementType = "Venda",
            OperationValue = 16.54,
            EquitiesQuantity = 3,
            ReferenceDate = new DateTime(2022, 01, 08)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "KFOF11",
            MovementType = "Compra",
            OperationValue = 231.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2022, 01, 16)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "KFOF11",
            MovementType = "Venda",
            OperationValue = 237.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2022, 01, 28)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            MovementType = "Venda",
            OperationValue = 234.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2022, 02, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            MovementType = "Compra",
            OperationValue = 265.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2022, 02, 01)
        });
    }

    private bool AlreadyHasAverageTradedPrice(Guid accountId) =>
        averageTradedPriceRepository.AlreadyHasAverageTradedPrice(accountId);

    private async Task SaveBigBang((List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>) response, Guid accountId)
    {
        Account account = genericRepositoryAccount.GetById(accountId);

        List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes = new();
        CreateIncomeTaxes(response.Item1, incomeTaxes, account);

        List<AverageTradedPrice> averageTradedPrices = new();
        CreateAverageTradedPrices(response.Item2, averageTradedPrices, account);

        await incomeTaxesRepository.AddAllAsync(incomeTaxes);
        await averageTradedPriceRepository.AddAllAsync(averageTradedPrices);
    }

    private void CreateAverageTradedPrices(List<AverageTradedPriceDetails> response, List<AverageTradedPrice> averageTradedPrices, Account account)
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

    private void CreateIncomeTaxes(List<AssetIncomeTaxes> assets, List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes, Account account)
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
                    TradedAssets = asset.TradedAssets,
                    Account = account,
                    AssetId = (int)asset.AssetTypeId
                });
            }
        }
    }

    private bool MovementHadProfitOrLoss(AssetIncomeTaxes asset)
    {
        return asset.SwingTradeProfit != 0 || asset.DayTradeProfit != 0;
    }

    #endregion

    #region Calcula o imposto de renda mensal.
    public async Task<List<AssetIncomeTaxes>> CalculateCurrentMonthAssetsIncomeTaxes(AssetsIncomeTaxesRequest request)
    {
        try
        {
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            Account account = await genericRepositoryAccount.GetByIdAsync(request.AccountId);

            // var b3Response = await b3Client.GetAccountMovement(account.CPF, request.Month, yesterday, request.AccountId);

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

            AddBigCurrentMonthSet(b3Response);

            BigBang bigBang = new(incomeTaxCalculator, averageTradedPriceRepository);
            var response = await bigBang.Execute(b3Response, account.Id);

            return response.Item1;
        } catch (Exception e)
        {
            logger.LogError(e, $"Ocorreu um erro ao calcular o imposto mensal devido. {e.Message}");
            throw;
        }
    }

    private void AddBigCurrentMonthSet(Movement.Root response)
    {
        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            MovementType = "Compra",
            OperationValue = 19.54,
            EquitiesQuantity = 2,
            ReferenceDate = new DateTime(2023, 01, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            MovementType = "Compra",
            OperationValue = 34.65,
            EquitiesQuantity = 3,
            ReferenceDate = new DateTime(2023, 01, 03)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            MovementType = "Venda",
            OperationValue = 10.43,
            EquitiesQuantity = 6,
            ReferenceDate = new DateTime(2023, 01, 08)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "VISC11",
            MovementType = "Compra",
            OperationValue = 231.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 16)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "VISC11",
            MovementType = "Venda",
            OperationValue = 65.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 28)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            MovementType = "Compra",
            OperationValue = 234.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 02, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            MovementType = "Compra",
            OperationValue = 265.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 02, 01)
        });
    }
    #endregion
}
