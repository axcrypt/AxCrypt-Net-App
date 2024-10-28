Instructions for AxReports Financial reports
============================================

This Read Me should be part of a zip-archive with all the reports. The structure is:

SE---+
     |
     +--- YYYY-MM-YYYY-MM-MossVat.csv
     |
     +--- YYYY-MM-YYYY-MM-Rates.csv
     |
     +--- YYYY-MM-Revenue.csv
     |
     +--- YYYY-MM-Transactions.csv
     |
     + ...

US---+
     |
     +--- YYYY-MM-YYYY-MM-MossVat.csv
     |
     +--- YYYY-MM-YYYY-MM-Rates.csv
     |
     +--- YYYY-MM-Revenue.csv
     |
     +--- YYYY-MM-Transactions.csv
     |
     + ...

SE - contains Excel CSV-files formatted for Swedish decimal ",".
US - contains Excel CSV-files formatted for US decimal ".".

Use SE/US according to how your Excel is configured. If you open one, and the numbers
are not treated as numbers, try the other one.

Importing of data is idempotent, you can always re-import the same data. Nothing changes.

If a Stripe refund is made for a payment in past months which have already been reported,
the report will have to be re-run for that month. All payments for Stripe are recorded
at the original payment date, even if later refunded.

YYYY-MM-YYYY-MM-MossVat.csv
===========================

Contains a summary of EU VAT amounts for entry into the MOSS system.

It is up to the creator of the report to ensure that the period is relevant, i.e.
quaterly or whatever is appropriate.

The SEK amounts are calculated using the monthly average rates from the Swedish
Riksbank for each respective currency.

The EUR amounts are calculated using the ECB rate posted on the last day of the
period report is for.

The columns are:

Country       - The two letter ISO code for the country.
Period        - The period in the format YYYY-MM--YYYY-MMM.
Revenue (SEK) - The revenue excl. VAT in SEK.
VAT (SEK)     - The VAT in SEK (negative).
Revenue (EUR) - The revenue excl. VAT in EUR.

YYYY-MM-YYYY-MM-Rates.csv
=========================

This is a list of all currency rates used during report generation.

The columns are:

Source          - Where the rate is fetched from. RB for Riksbanken, ECB for European Central Bank.
From Currency   - The 3 letter ISO code for the currency to convert from.
To Currency     - The 3 letter ISO code for the currency to convert to.
Rate            - The amount to multiply From with to get To.
Sample Period   - How the rate is calculated (day or average).
Validity Period - The date range the rate has been used for.
Effective Date  - The date from which the date is effective. Most useful for day rates.

YYYY-MM-Revenue.csv
===================

A summary report for Swedish accounting.

The SEK amounts are calculated from the account currency using Riksbanken monthly average.

NOTE - Payments, withdrawals and transfers are included in the balance.

The columns are:

Payment Processor         - Stripe or PayPal etc.
Month                     - The month the record concerns in the format YYYY-MM
Account Currency          - The 3 letter ISO code for the currency the account balance is in.
Opening Balance (SEK)     - The virtual opening balance as Closing Balance - Net Sales to SEK @ last month end rate.
                            Transfers in the period are not taken into consideration.
Closing Balance (SEK)     - The closing balance as converted to SEK with the closing day rate.
Fees (SEK)                - The total fees paid to the payment processor in SEK.
Turnover SE (SEK)         - The sales value incl. fees but excl. VAT in Sweden.
VAT SE (SEK)              - The VAT in Sweden.
Turnover EU (SEK)         - The sales value incl. fees but excl. VAT in the EU, excluding Sweden.
VAT EU (SEK)              - The VAT in the EU.
Turnover Export (SEK)     - The sales value incl. fees outside EU.
Closing Balance           - The closing balance in the account currency.
Net Sales                 - The sum of all sales incl. VAT excl. Fees in the account currency.
Net Sales (SEK)           - The sum of all Turnover (SEK) + all VATs (SEK) - all Fees (SEK).
Exchange Difference (SEK) - The difference of Closing Balance (SEK) - Opening Balance (SEK) and Net Sales (SEK).

YYYY-MM-Transactions.csv
========================

This is the consolidated cooked list of transactions from the different payment processors. It can be used
for finding differences and producing other statistics.

The Payment and Account amounts are only included when they are provided directly from the payment processor.
Later processing will perform the required currency conversions if needed.

The columns are:

Id                - A payment processor specific unique identifier for the transaction.
Utc               - The date and time when the transaction was created.
Id Reference      - If this transaction is in relation to another one, it's id is here.
Payment Processor - Stripe or PayPal etc.
Description       - A free text field describing the transaction.
Type              - A payment processor-specific text field specifying the type of transaction.
Affects Revenue   - 'yes' if the transaction should be counted as revenue.
Affects Balance   - 'yes' if the transaction affects balance, but not necessarily revenue.
Payment Currency  - The 3 letter ISO code of the currency the payment was made in.
Payment Total     - The total amount of the payment, including fees and VAT.
Payment Fee       - The fee billed by the payment processor for the payment.
Payment vat       - The VAT that was charged with the payment.
Account Currency  - The 3 letter ISO code of the currency the account balance is in.
Account Total     - The total amount of the payment, including fees and VAT.
Account Fee       - The fee billed by the payment processor for the payment.
Account vat       - The VAT that was charged with the payment.
Account Balance   - The account balance after the transaction.
Country           - The two letter country code where the transaction is considered to be made. 

PayPal Activity Download and Import
===================================

If you are responsible for generating the reports, the raw data from PayPal must be imported.

To download the appropriate data from PayPal, sign in and goto Reports | Activity Download
The sign in must either be the main account or an agent, otherwise the activity download is
unavailable.

Select "All Transactions", "Past 6 months", "CSV", Click "Create Report". Wait for it. Then
Download.

Import "Download.CSV" with "File | Import".

Stripe Balance Download and Import
===================================

If you are responsible for generating the reports, the raw data from Stripe must be imported.

To download the appropriate data from Stripe, sign in and select "Balance", then "Transactions".

Export all, or custom about 6 months or so. Select UTC as Time Zone.

Import "balance_history.csv" with "File | Import".

