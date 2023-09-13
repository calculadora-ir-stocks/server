using Api.Clients.B3;
using Api.Exceptions;
using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Core.Clients.InfoSimples;
using Core.Models;
using Core.Models.B3;
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
    private const int DarfCode = 6015;


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
    public async Task<MonthTaxesResponse> GetCurrentMonthTaxes(Guid accountId)
    {
        try
        {
            // Caso seja dia 1, não há como obter os dados do mês atual já que a B3 disponibiliza os dados em D-1.
            if (IsDayOne())
            {
                // Porém, sendo dia 1, o Worker já salvou os dados do mês passado na base.
                return await GetTaxesByMonth(DateTime.Now.AddDays(-1).ToString("yyyy-MM"), accountId);
            }

            string startDate = DateTime.Now.ToString("yyyy-MM-01");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            Infrastructure.Models.Account account = await genericRepositoryAccount.GetByIdAsync(accountId);

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired)) 
                throw new InvalidBusinessRuleException("O plano do usuário está expirado.");

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionPaused))
                throw new InvalidBusinessRuleException("O plano do usuário está pausado por conta de uma falha de pagamento.");

            // var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, yesterday, account.Id);
            var b3Response = GetCurrentMonthMockedDataBeforeB3Contract();

            var response = await incomeTaxesService.GetB3ResponseDetails(b3Response, account.Id);            

            if (response is null || response.Assets is null)
                throw new RecordNotFoundException("Nenhuma movimentação foi feita no mês atual.");

            return CurrentMonthToDto(response.Assets);
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
        List<Core.Responses.Asset> tradedAssets = new();

        foreach (var item in assets)
        {
            tradedAssets.Add(new Core.Responses.Asset(
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

        /*
         * "A vida não é só isso."
         * 
         * Na verdade, essa frase é verdadeira em absolutamente qualquer cenário possível. A vida não se resume à ciência da computação
         * e isso vale para mim. Se você é um botânico, a vida não se resume a biologia; se você é um engenheiro,
         * a vida não se resume a engenharia; se você é um físico, a vida não se resume a matemática.
         * 
         * Todos os exemplos são únicos campos de estudo. Mesmo assim, a premissa prevalence mesmo se você for um polímata. Se
         * você for um cientista da computação, botânico, engenheiro e físico, ainda assim estará distante da sociologia, da filosofia
         * e da retórica - ou seja, a vida continua não sendo apenas isso.
         * 
         * Se a vida não é o que escolhemos nos dedicar a estudar, o que mais resta para sê-la?
         * 
         * Mesmo quando aproveitamos pequenos e especiais momentos, estamos deixando de lado muitas outras coisas - então a premissa
         * principal ainda prevalece - ou seja, a vida não é o seu campo de estudo nem os seus pequenos proveitos e lazeres.
         * 
         * Alguns argumentam que a vida é estudo, mas ainda resta o que ser estudado.
         * Alguns argumentam que a vida é aproveitar os momentos, mas ainda resta o que ser aproveitado.
         * Alguns argumentam que a vida é vencer, mas vencer é subjetivo quando já se vence.
         * Alguns argumentam que a vida é prazer, como Salomão, mas ainda sim o prazer é subjetivo.
         * 
         * Vai ver a vida é apenas sentir e refletir sobre ela.
         * */
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
    public async Task<MonthTaxesResponse> GetTaxesByMonth(string month, Guid accountId)
    {
        try
        {
            if (WorkerDidNotSaveDataForThisMonthYet(month))
            {
                throw new InvalidBusinessRuleException("Para obter as informações de impostos do mês atual, acesse /assets/current.");
            }

            var account = genericRepositoryAccount.GetById(accountId);

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
                throw new InvalidBusinessRuleException("O plano do usuário está expirado.");

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionPaused))
                throw new InvalidBusinessRuleException("O plano do usuário está pausado por conta de uma falha de pagamento.");

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
        List<Core.Responses.Asset> tradedAssets = new();

        foreach (var tax in taxes)
        {
            tradedAssets.Add(new Core.Responses.Asset(
                (Common.Enums.Asset)tax.AssetTypeId,
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
    public async Task<IEnumerable<YearTaxesResponse>> GetTaxesByYear(string year, Guid accountId)
    {
        try
        {
            var account = genericRepositoryAccount.GetById(accountId);

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
                throw new InvalidBusinessRuleException("O plano do usuário está expirado.");

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionPaused))
                throw new InvalidBusinessRuleException("O plano do usuário está pausado por conta de uma falha de pagamento.");

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
        try
        {
            Infrastructure.Models.Account? account = accountRepository.GetById(accountId);
            if (account is null) throw new RecordNotFoundException("Investidor", accountId.ToString());

            if (account.Status == EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.Synced))
            {
                logger.LogError("A sincronização com a B3 já foi executada para o usuário {accountId}, mas" +
                    "o Big Bang foi executado mesmo assim.", accountId);

                throw new InvalidBusinessRuleException($"A sincronização com a B3 já foi executada para o usuário {accountId}.");
            }

            if (AccountCanExecuteSyncing(account))
            {
                account.Status = EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.Syncing);
                accountRepository.Update(account);
            } else
            {
                throw new InvalidBusinessRuleException("Antes de executar o Big Bang é necessário " +
                    "confirmar o endereço de e-mail.");
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

            logger.LogInformation("Big Bang executado com sucesso para o usuário {accountId}.", accountId);
        }
        catch (Exception e)
        {
            logger.LogError("Uma exceção ocorreu ao executar o Big Bang do usuário {accountId}." +
                "{e.Message}", accountId, e.Message);

            throw;
        }
    }

    private static bool AccountCanExecuteSyncing(Infrastructure.Models.Account account)
    {
        return account.Status == EnumHelper.GetEnumDescription(AccountStatus.EmailConfirmed);
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

    private async Task SaveB3Data(InvestorMovementDetails response, Infrastructure.Models.Account account)
    {
        List<Infrastructure.Models.IncomeTaxes> incomeTaxes = new();
        CreateIncomeTaxes(response.Assets, incomeTaxes, account);

        List<AverageTradedPrice> averageTradedPrices = new();
        CreateAverageTradedPrices(response.AverageTradedPrices, averageTradedPrices, account);

        // TODO unit of work
        await taxesRepository.AddAllAsync(incomeTaxes);
        await averageTradedPriceRepository.AddAllAsync(averageTradedPrices);

        account.Status = EnumHelper.GetEnumDescription(Common.Enums.AccountStatus.Synced);
        accountRepository.Update(account);
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

    private static bool MonthHadProfitOrLoss(AssetIncomeTaxes asset)
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

    #region Geração de DARF
    public async Task GenerateDARF(Guid accountId, string month)
    {
        var taxes = await taxesRepository.GetSpecifiedMonthTaxes(month, accountId);

        if (taxes.IsNullOrEmpty())
            throw new RecordNotFoundException("Nenhum imposto foi encontrado para esse mês, logo, a DARF não pode ser gerada.");

        var account = await genericRepositoryAccount.GetByIdAsync(accountId);

        string taxesReferenceDate = taxes.Select(x => x.Month).First();
        string today = DateTime.Now.ToString("MM/yyyy");

        double totalTaxes = taxes.Select(x => x.Taxes).Sum();

        var response = await infoSimplesClient.GenerateDARF(
            new Models.InfoSimples.GenerateDARFRequest
            (                
                account.CPF,
                account.BirthDate,
                $"Venda de ativos no mês {taxesReferenceDate}. Essa DARF foi gerada automaticamente" +
                $"pelo Stocks IR em {today}.",
                DarfCode,
                totalTaxes,
                taxesReferenceDate,
                today
            )
        );
    }
    #endregion
}
