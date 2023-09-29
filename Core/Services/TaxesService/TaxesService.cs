using Api.Clients.B3;
using common.Helpers;
using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Core.Clients.InfoSimples;
using Core.Models;
using Core.Models.InfoSimples;
using Core.Models.Responses;
using Core.Requests.BigBang;
using Core.Responses;
using Core.Services.IncomeTaxes;
using Infrastructure.Dtos;
using Infrastructure.Models;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.Taxes;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Core.Services.TaxesService;

public class TaxesService : ITaxesService
{
    private readonly IIncomeTaxesService incomeTaxesService;

    private readonly IGenericRepository<Infrastructure.Models.Account> genericRepositoryAccount;
    private readonly IAccountRepository accountRepository;

    private readonly ITaxesRepository taxesRepository;
    private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

    private readonly IB3Client b3Client;
    private readonly IInfoSimplesClient infoSimplesClient;

    private readonly ILogger<TaxesService> logger;

    // https://farocontabil.com.br/codigosdarf.htm#:~:text=Imposto%20sobre%20ganhos%20l%C3%ADquidos%20em%20opera%C3%A7%C3%B5es%20em%20bolsa%20de%20valores%2C%20de%20mercadorias%2C%20de%20futuros%20e%20assemelhadas
    private const string DarfCode = "6015-01";


    /**
     * TODO Atualmente, para validar se um usuário ainda possui um plano válido para acessar os recursos,
     * está sendo feita uma validação manual SOMENTE nessa classe.
     * 
     * É necessário alterar esse processo para armazenar um bool de plano expirado no token JWT,
     * usando a técnica de refresh token. É necessário discutir se o refresh token será armazenado no client ou no server.
     * */

    public TaxesService(IIncomeTaxesService incomeTaxesService,
        IGenericRepository<Infrastructure.Models.Account> genericRepositoryAccount,
        IAccountRepository accountRepository,
        ITaxesRepository taxesRepository,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client b3Client,
        IInfoSimplesClient infoSimplesClient,
        ILogger<TaxesService> logger
        )
    {
        this.incomeTaxesService = incomeTaxesService;
        this.genericRepositoryAccount = genericRepositoryAccount;
        this.accountRepository = accountRepository;
        this.taxesRepository = taxesRepository;
        this.averageTradedPriceRepository = averageTradedPriceRepository;
        this.b3Client = b3Client;
        this.infoSimplesClient = infoSimplesClient;
        this.logger = logger;
    }

    #region Calcula o imposto de renda do mês atual.
    public async Task<TaxesDetailsResponse> GetCurrentMonthTaxes(Guid accountId)
    {
        try
        {
            // Caso seja dia 1, não há como obter os dados do mês atual já que a B3 disponibiliza os dados em D-1.
            if (IsDayOne())
            {
                // Porém, sendo dia 1, o Worker já salvou os dados do mês passado na base.
                return await Details(DateTime.Now.AddDays(-1).ToString("yyyy-MM"), accountId);
            }

            string startDate = DateTime.Now.ToString("yyyy-MM-01");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            Infrastructure.Models.Account account = await genericRepositoryAccount.GetByIdAsync(accountId);

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
                throw new ForbiddenException("O plano do usuário está expirado.");

            // var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, yesterday, account.Id);
            var b3Response = AddCurrentMonthSet();

            var response = await incomeTaxesService.GetB3ResponseDetails(b3Response, account.Id);            

            if (response is null || response.Assets.IsNullOrEmpty())
                throw new NotFoundException("Nenhuma movimentação foi feita no mês atual.");

            return ToTaxesDetailsResponse(response.Assets);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao calcular o imposto mensal devido. {e.Message}", e.Message);
            throw;
        }
    }

    private static bool IsDayOne()
    {
        // D-1
        DateTime yesterday = DateTime.Now.AddDays(-1);
        return yesterday.Month < DateTime.Now.Month;
    }

    private static TaxesDetailsResponse ToTaxesDetailsResponse(List<AssetIncomeTaxes> assets)
    {
        // O objeto de retorno é complexo o suficiente para não usar o AutoMapper?

        TaxesDetailsResponse response = new(
            totalTaxes: assets.Select(x => x.Taxes).Sum(),
            UtilsHelper.GetMonthAndYearName(DateTime.Now.ToString("MM/yyyy"))
        );

        var days = assets.SelectMany(x => x.TradedAssets.Select(x => x.Day).Distinct());

        foreach (var day in days)
        {
            List<Details> details = new();
            List<Movement> movements = new();            

            var tradedAssetsOnThisDay = assets.SelectMany(x => x.TradedAssets.Where(x => x.Day == day));            

            string weekDay = string.Empty;

            foreach (var tradedAsset in tradedAssetsOnThisDay)
            {
                if (weekDay.IsNullOrEmpty())
                {
                    string dayOfTheWeek = tradedAsset.Day.ToString();
                    weekDay = $"{tradedAsset.DayOfTheWeek}, dia {dayOfTheWeek}";
                }   

                details.Add(new Details(
                    tradedAsset.AssetTypeId,
                    tradedAsset.AssetType,
                    tradedAsset.MovementType,
                    tradedAsset.TickerSymbol,
                    tradedAsset.Total,
                    tradedAsset.Quantity
                ));
            }

            movements.Add(new Movement(weekDay, details));

            response.Movements.AddRange(
                movements
            );
        }

        return response;
    }

    /// <summary>
    /// Retorna dados mockados da API da B3 para testes locais antes da contratação.
    /// Deve ser removido após a implementação do serviço de produção.
    /// </summary>
    private static Models.B3.Movement.Root? AddCurrentMonthSet()
    {
        Models.B3.Movement.Root? response = new()
        {
            Data = new()
            {
                EquitiesPeriods = new()
                {
                    EquitiesMovements = new()
                }
            }
        };

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 19.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 34.65,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 03)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
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

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "VISC11",
            CorporationName = "VISC11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 231.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 29)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "VISC11",
            CorporationName = "VISC11 Corporation Inc.",
            MovementType = "Venda",
            OperationValue = 304.43,
            UnitPrice = 304.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 29)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            CorporationName = "Americanas S/A",
            MovementType = "Compra",
            OperationValue = 234.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 29)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
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

        return response;
    }
    #endregion

    #region Calcula o imposto de renda do mês especificado.
    public async Task<TaxesDetailsResponse> Details(string month, Guid accountId)
    {
        try
        {
            if (WorkerDidNotSaveDataForThisMonthYet(month))
            {
                throw new BadRequestException("Para obter as informações de impostos do mês atual, acesse /assets/current.");
            }

            var account = genericRepositoryAccount.GetById(accountId);
            if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
                throw new ForbiddenException("O plano do usuário está expirado.");

            var response = await taxesRepository.GetSpecifiedMonthTaxes(System.Net.WebUtility.UrlDecode(month), accountId);
            if (response.IsNullOrEmpty()) throw new NotFoundException("Nenhum imposto foi encontrado no mês especificado.");

            if (response.Select(x => x.Taxes).Sum() <= 0) throw new NotFoundException("Nenhum imposto foi encontrado no mês especificado.");

            return SpecifiedMonthTaxesDtoToTaxesDetailsResponse(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao calcular um imposto mensal devido especificado. {e.Message}", e.Message);
            throw;
        }
    }

    private static TaxesDetailsResponse SpecifiedMonthTaxesDtoToTaxesDetailsResponse(IEnumerable<SpecifiedMonthTaxesDto> assets)
    {
        TaxesDetailsResponse response = new(
            totalTaxes: assets.Select(x => x.Taxes).Sum(),
            year: UtilsHelper.GetMonthAndYearName(assets.ElementAt(0).Month)
        );

        var days = assets.SelectMany(x => x.SerializedTradedAssets.Select(x => x.Day).Distinct());

        foreach (var day in days)
        {
            List<Details> details = new();
            List<Movement> movements = new();

            var tradedAssetsOnThisDay = assets.SelectMany(x => x.SerializedTradedAssets.Where(x => x.Day == day));

            string weekDay = string.Empty;

            foreach (var tradedAsset in tradedAssetsOnThisDay)
            {
                if (weekDay.IsNullOrEmpty())
                {
                    string dayOfTheWeek = tradedAsset.Day.ToString();
                    weekDay = $"{tradedAsset.DayOfTheWeek}, dia {dayOfTheWeek}";
                }

                details.Add(new Details(
                    tradedAsset.AssetTypeId,
                    tradedAsset.AssetType,
                    tradedAsset.MovementType,
                    tradedAsset.TickerSymbol,
                    tradedAsset.Total,
                    tradedAsset.Quantity
                ));
            }

            movements.Add(new Movement(weekDay, details));

            response.Movements.AddRange(
                movements
            );
        }

        return response;
    }

    private static bool WorkerDidNotSaveDataForThisMonthYet(string month)
    {
        string currentMonth = DateTime.Now.ToString("yyyy-MM");
        return month == currentMonth;
    }
    #endregion

    #region Calcula o imposto de renda do ano especificado
    public async Task<IEnumerable<CalendarResponse>> GetCalendarTaxes(string year, Guid accountId)
    {
        try
        {
            var account = genericRepositoryAccount.GetById(accountId);
            if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
                throw new ForbiddenException("O plano do usuário está expirado.");

            var response = await taxesRepository.GetSpecifiedYearTaxes(System.Net.WebUtility.UrlDecode(year), accountId);

            return ToSpecifiedYearDto(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao calcular um imposto mensal devido especificado. {e.Message}", e.Message);
            throw;
        }
    }

    private static IEnumerable<CalendarResponse> ToSpecifiedYearDto(IEnumerable<SpecifiedYearTaxesDto> taxes)
    {
        List<CalendarResponse> response = new();

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

            response.Add(new CalendarResponse(
                UtilsHelper.GetMonthName(int.Parse(item.Month)),
                totalTaxes,
                totalSwingTradeProfit,
                totalDayTradeProfit
            ));
        }

        return response;
    }

    private static bool MonthAlreadyAdded(IEnumerable<CalendarResponse> response, SpecifiedYearTaxesDto item)
    {
        return response.Select(x => x.Month).Contains(UtilsHelper.GetMonthName(int.Parse(item.Month)));
    }
    #endregion

    #region Altera um mês como pago/não pago
    public async Task SetAsPaidOrUnpaid(string month, Guid accountId)
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

    #region Faz a sincronização com a B3.
    public async Task ExecuteB3Sync(Guid accountId, List<BigBangRequest> request)
    {
        Infrastructure.Models.Account? account = accountRepository.GetById(accountId);
        if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

        if (!AccountCanExecuteSyncing(account))
        {
            throw new BadRequestException($"O usuário {account.Id} tentou executar a sincronização mas não possui o e-mail validado" +
                " ou já sincronizou sua conta anteriormente.");
        }

#pragma warning disable CS0219 // Variable is assigned but its value is never used
        string startDate = "2019-11-01";
#pragma warning restore CS0219 // Variable is assigned but its value is never used

        string lastMonth = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1)
            .AddMonths(-1)
            .ToString("yyyy-MM-dd");

        // var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, lastMonth, accountId);
        var b3Response = GetBigBangMockedDataBeforeB3Contract();

        var response = await incomeTaxesService.GetB3ResponseDetails(b3Response, accountId);

        if (response is null) return;

        await SaveB3Data(response, account);

        account.Status = EnumHelper.GetEnumDescription(AccountStatus.SubscriptionValid);
        accountRepository.Update(account);

        logger.LogInformation("Big Bang executado com sucesso para o usuário {accountId}.", accountId);
    }

    private static bool AccountCanExecuteSyncing(Infrastructure.Models.Account account)
    {
        return account.Status == EnumHelper.GetEnumDescription(AccountStatus.EmailConfirmed);
    }

    private static Models.B3.Movement.Root? GetBigBangMockedDataBeforeB3Contract()
    {
        Models.B3.Movement.Root? b3Response = new()
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

    private async Task SaveB3Data(InvestorMovementDetails response, Infrastructure.Models.Account account)
    {
        List<Infrastructure.Models.IncomeTaxes> incomeTaxes = new();
        CreateIncomeTaxes(response.Assets, incomeTaxes, account);

        List<AverageTradedPrice> averageTradedPrices = new();
        CreateAverageTradedPrices(response.AverageTradedPrices, averageTradedPrices, account);

        // TODO unit of work
        await taxesRepository.AddAllAsync(incomeTaxes);
        await averageTradedPriceRepository.AddAllAsync(averageTradedPrices);        
    }

    private static void CreateAverageTradedPrices(List<AverageTradedPriceDetails> response, List<AverageTradedPrice> averageTradedPrices, Infrastructure.Models.Account account)
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

    private static void CreateIncomeTaxes(List<AssetIncomeTaxes> assets, List<Infrastructure.Models.IncomeTaxes> incomeTaxes, Infrastructure.Models.Account account)
    {
        foreach (var asset in assets)
        {
            if (MonthHadProfitOrLoss(asset))
            {
                incomeTaxes.Add(new Infrastructure.Models.IncomeTaxes
                {
                    Month = asset.Month,
                    Taxes = asset.Taxes,
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

    private static bool MonthHadProfitOrLoss(AssetIncomeTaxes asset)
    {
        return asset.SwingTradeProfit != 0 || asset.DayTradeProfit != 0;
    }

    /// <summary>
    /// Retorna dados mockados da API da B3 para testes locais antes da contratação.
    /// Deve ser removido após a implementação do serviço de produção.
    /// </summary>
    private static void AddBigBangDataSet(Models.B3.Movement.Root response)
    {
        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 10.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "BOVA11",
            CorporationName = "BOVA 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 18.43,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 03)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
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

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "IVVB11",
            CorporationName = "IVVB 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 245.65,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 09)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "ETF - Exchange Traded Fund",
            TickerSymbol = "IVVB11",
            CorporationName = "IVVB 11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 246.65,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 09)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
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

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "FII - Fundo de Investimento Imobiliário",
            TickerSymbol = "KFOF11",
            CorporationName = "KFOF11 Corporation Inc.",
            MovementType = "Compra",
            OperationValue = 231.34,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 01, 16)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
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

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
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

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
        {
            AssetType = "Ações",
            TickerSymbol = "AMER3",
            MovementType = "Compra",
            CorporationName = "Americanas S/A",
            OperationValue = 265.54,
            EquitiesQuantity = 1,
            ReferenceDate = new DateTime(2023, 02, 01)
        });

        response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
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

    #region Geração de DARF
    public async Task<DARFResponse> GenerateDARF(Guid accountId, string month, double? value)
    {
        var account = await genericRepositoryAccount.GetByIdAsync(accountId);

        if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
            throw new ForbiddenException("O plano do usuário está expirado.");

        var taxes = await taxesRepository.GetSpecifiedMonthTaxes(month, accountId);
        double totalTaxes = taxes.Select(x => x.Taxes).Sum();

        if (value is not null) totalTaxes += value.Value;

        if (taxes.IsNullOrEmpty() || taxes.Select(x => x.Taxes).Sum() <= 0)
            throw new NotFoundException("Nenhum imposto foi encontrado para esse mês, logo, a DARF não pode ser gerada.");        

        string taxesReferenceDate = taxes.Select(x => x.Month).First();
        string today = DateTime.Now.ToString("dd/MM/yyyy");

        var response = await infoSimplesClient.GenerateDARF(
            new GenerateDARFRequest
            (
                UtilsHelper.RemoveSpecialCharacters(account.CPF),
                account.BirthDate,
                $"Venda de ativos no mês {taxesReferenceDate}. Essa DARF foi gerada automaticamente " +
                $"pelo Stocks IR em {today}.",
                DarfCode,
                totalTaxes,
                taxesReferenceDate,
                today
            )
        );

        string? observation = null;

        if (response.Data[0].TotalTaxes.TotalWithFineAndInterests < 10)
        {
            observation = "Valor total da DARF é inferior ao valor mínimo de R$10,00. \n" +
                "Para pagá-la, adicione esse imposto em algum mês subsequente até que o valor total seja igual ou maior que R$10,00.";
        }

        var monthsToCompensate = await taxesRepository.GetTaxesLessThanMinimumRequired(accountId, month);

        return new DARFResponse(
            response.Data[0].BarCode,
            response.Data[0].TotalTaxes.TotalWithFineAndInterests,
            double.Parse(response.Data[0].TotalTaxes.Fine),
            double.Parse(response.Data[0].TotalTaxes.Interests),
            observation,
            monthsToCompensate
        );
    }
    #endregion
}
