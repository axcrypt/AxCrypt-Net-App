using AxCrypt.International;
using AxCrypt.International.Abstractions;
using AxCrypt.Reports.Implementation;
using AxCrypt.Reports.Model;
using AxCrypt.Reports.Model.Csv;
using AxCrypt.Reports.Report;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AxCrypt.Reports
{
    public static class Extensions
    {
        public static TransactionAmounts Merge(this TransactionAmounts main, TransactionAmounts other, IExchange exchange)
        {
            return new TransactionAmounts()
            {
                Currency = exchange.To,
                Total = main.Total ?? exchange.Round(other.Total),
                Fee = main.Fee ?? exchange.Round(other.Fee),
                Vat = main.Vat ?? exchange.Round(other.Vat),
            };
        }

        public static TransactionAmounts Exchange(this TransactionAmounts from, IExchange exchange)
        {
            return new TransactionAmounts()
            {
                Currency = exchange.To,
                Total = exchange.Round(from.Total),
                Fee = exchange.Round(from.Fee),
                Vat = exchange.Round(from.Vat),
            };
        }

        public static IExchange UseOrFromTransaction(this IExchange exchange, TransactionAmounts from, TransactionAmounts to)
        {
            if (!from.Total.HasValue || !to.Total.HasValue)
            {
                return exchange;
            }
            if (from.Currency != exchange.From || to.Currency != exchange.To)
            {
                return exchange;
            }
            return new TransactionRateExchange(from, to);
        }

        public static decimal? ZeroAsNull(this decimal value)
        {
            return (value == 0m ? new decimal?() : value);
        }

        private struct ColumnSums
        {
            public decimal BalanceOpen;
            public decimal BalanceClose;
            public decimal Fees;
            public decimal TurnoverSe;
            public decimal VatSe;
            public decimal TurnoverEu;
            public decimal VatEu;
            public decimal TurnoverExport;
            public decimal NetSales;
            public decimal ExchangeDifference;
        }

        public static IEnumerable<RevenueReportCsvRecord> ToCsv(this IEnumerable<RevenueCurrencySums> sums, LocaleInfo locale, DateMonth month)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(locale.CultureName);
            ExchangeCurrency toSek = new ExchangeCurrency(CurrencyInfo.SEK);
            ColumnSums sekSums = new ColumnSums();

            foreach (RevenueCurrencySums sum in sums)
            {
                decimal? netSalesInclVat = sum.TotalSums.Total.Value + sum.TotalSums.Fee;
                ColumnSums row = new ColumnSums()
                {
                    BalanceOpen = toSek.Daily((sum.Balance - netSalesInclVat.GetValueOrDefault()), sum.Account.Currency, month.FirstDay.AddDays(-1)),
                    BalanceClose = toSek.Daily(sum.Balance, sum.Account.Currency, month.LastDay),
                    Fees = sum.SekEuSums.Fee.Value + sum.SekSwedenSums.Fee.Value + sum.SekExportSums.Fee.Value,
                    TurnoverSe = sum.SekSwedenSums.Total.Value + sum.SekSwedenSums.Vat.Value,
                    VatSe = -sum.SekSwedenSums.Vat.Value,
                    TurnoverEu = sum.SekEuSums.Total.Value + sum.SekEuSums.Vat.Value,
                    VatEu = -sum.SekEuSums.Vat.Value,
                    TurnoverExport = sum.SekExportSums.Total.Value + sum.SekExportSums.Vat.Value,
                    NetSales = (sum.SekEuSums.Total + sum.SekSwedenSums.Total + sum.SekExportSums.Total + sum.SekEuSums.Fee + sum.SekSwedenSums.Fee + sum.SekExportSums.Fee).GetValueOrDefault(),
                };
                RevenueReportCsvRecord csv = new RevenueReportCsvRecord()
                {
                    Source = sum.Account.Source.Name,
                    Month = month.ToString(),
                    AccountCurrency = sum.Account.Currency.ToString(),
                    SekBalanceOpen = CurrencyInfo.SEK.Display(row.BalanceOpen, ci),
                    SekBalanceClose = CurrencyInfo.SEK.Display(row.BalanceClose, ci),
                    SekFees = CurrencyInfo.SEK.Display(row.Fees, ci),
                    SekSeTurnover = CurrencyInfo.SEK.Display(row.TurnoverSe, ci),
                    SekSeVat = CurrencyInfo.SEK.Display(row.VatSe, ci),
                    SekEuTurnover = CurrencyInfo.SEK.Display(row.TurnoverEu, ci),
                    SekEuVat = CurrencyInfo.SEK.Display(row.VatEu, ci),
                    SekExportTurnover = CurrencyInfo.SEK.Display(row.TurnoverExport, ci),
                    BalanceClose = sum.Account.Currency.Display(sum.Balance, ci),
                    NetSales = sum.Account.Currency.Display(netSalesInclVat, ci),
                    SekNetSales = CurrencyInfo.SEK.Display(row.NetSales, ci),
                    ExchangeDifference = CurrencyInfo.SEK.Display(row.BalanceClose - row.BalanceOpen - row.NetSales, ci),
                };

                sekSums.Fees += row.Fees;
                sekSums.NetSales += row.NetSales;
                sekSums.TurnoverEu += row.TurnoverEu;
                sekSums.TurnoverExport += row.TurnoverExport;
                sekSums.TurnoverSe += row.TurnoverSe;
                sekSums.VatEu += row.VatEu;
                sekSums.VatSe += row.VatSe;
                sekSums.ExchangeDifference += row.BalanceClose - row.BalanceOpen - row.NetSales;

                yield return csv;
            }

            RevenueReportCsvRecord csvSums = new RevenueReportCsvRecord()
            {
                Source = "Total",
                Month = month.ToString(),
                AccountCurrency = CurrencyInfo.SEK.ToString(),
                SekFees = CurrencyInfo.SEK.Display(sekSums.Fees, ci),
                SekSeTurnover = CurrencyInfo.SEK.Display(sekSums.TurnoverSe, ci),
                SekSeVat = CurrencyInfo.SEK.Display(sekSums.VatSe, ci),
                SekEuTurnover = CurrencyInfo.SEK.Display(sekSums.TurnoverEu, ci),
                SekEuVat = CurrencyInfo.SEK.Display(sekSums.VatEu, ci),
                SekExportTurnover = CurrencyInfo.SEK.Display(sekSums.TurnoverExport, ci),
                SekNetSales = CurrencyInfo.SEK.Display(sekSums.NetSales, ci),
                ExchangeDifference = CurrencyInfo.SEK.Display(sekSums.ExchangeDifference, ci),
            };
            yield return csvSums;
        }

        public static IEnumerable<MossReportCsvRecord> ToCsv(this IEnumerable<MossVatCountrySums> sums, LocaleInfo locale, DateMonthPeriod months)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(locale.CultureName);

            ExchangeCurrency toSek = new ExchangeCurrency(CurrencyInfo.SEK);

            foreach (MossVatCountrySums sum in sums)
            {
                MossReportCsvRecord csv = new MossReportCsvRecord()
                {
                    Country = sum.Country.CountryName,
                    Period = months.ToString(),
                    EurRevenue = CurrencyInfo.EUR.Display(sum.EurSums.Total.Value + sum.EurSums.Vat.Value, ci),
                    EurVat = CurrencyInfo.EUR.Display(-sum.EurSums.Vat.Value, ci),
                    SekRevenue = CurrencyInfo.SEK.Display(sum.SekSums.Total.Value + sum.SekSums.Vat.Value, ci),
                    SekVat = CurrencyInfo.SEK.Display(-sum.SekSums.Vat.Value, ci),
                };

                yield return csv;
            }
        }

        public static IEnumerable<TransactionReportCsvRecord> ToCsv(this IEnumerable<TransactionRecord> transactions, LocaleInfo locale)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(locale.CultureName);

            foreach (TransactionRecord transaction in transactions)
            {
                CurrencyInfo accountCurrency = transaction.AmountsAccount.Currency;
                CurrencyInfo paymentCurrency = transaction.AmountsPayment.Currency;
                TransactionReportCsvRecord csv = new TransactionReportCsvRecord()
                {
                    AccountBalance = accountCurrency.Display(transaction.AccountBalance, ci),
                    AccountCurrency = accountCurrency.ToString(),
                    AccountFee = accountCurrency.Display(transaction.AmountsAccount.Fee, ci),
                    AccountTotal = accountCurrency.Display(transaction.AmountsAccount.Total, ci),
                    AccountVat = accountCurrency.Display(transaction.AmountsAccount.Vat, ci),
                    AffectsBalance = transaction.AffectsBalance ? "yes" : string.Empty,
                    AffectsRevenue = transaction.AffectsRevenue ? "yes" : string.Empty,
                    Country = transaction.Country.CountryName,
                    Description = transaction.Description,
                    Id = transaction.Id,
                    IdReference = transaction.IdReference,
                    Month = new DateMonth(transaction.Utc).ToString(),
                    PaymentCurrency = paymentCurrency.ToString(),
                    PaymentFee = paymentCurrency.Display(transaction.AmountsPayment.Fee, ci),
                    PaymentTotal = paymentCurrency.Display(transaction.AmountsPayment.Total, ci),
                    PaymentVat = paymentCurrency.Display(transaction.AmountsPayment.Vat, ci),
                    Source = transaction.Source.Name,
                    Type = transaction.Type,
                    Utc = transaction.Utc.ToString("u"),
                };

                yield return csv;
            }
        }

        public static IEnumerable<ExchangeRateCsvRecord> ToCsv(this IEnumerable<ExchangeRate> rates, LocaleInfo locale)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(locale.CultureName);

            foreach (ExchangeRate rate in rates)
            {
                ExchangeRateCsvRecord csv = new ExchangeRateCsvRecord()
                {
                    Source = rate.Source,
                    EffectiveDate = rate.ValidityPeriod.LastDay.ToString("yyyy-MM-dd"),
                    FromCurrency = rate.From.ToString(),
                    ToCurrency = rate.To.ToString(),
                    ValidityPeriod = rate.ValidityPeriod.ToString(),
                    Rate = rate.Rate.ToString(),
                    SamplePeriod = rate.SamplePeriod.ToRateTypeString(),
                };

                yield return csv;
            }
        }

        private static string ToRateTypeString(this ExchangeRateSamplePeriod ratePeriodType)
        {
            switch (ratePeriodType)
            {
                case ExchangeRateSamplePeriod.Day:
                    return "Day";

                case ExchangeRateSamplePeriod.Month:
                    return "Monthly Average";

                default:
                    throw new InvalidOperationException($"Unexpected {nameof(ExchangeRateSamplePeriod)}");
            }
        }
    }
}