using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compliance.Intellisense
{
    public class Simple_Tables
    {
        public Dictionary<string, string> _tables = new Dictionary<string, string>()
        {
            {"table 1", "table_1" },
            {"table 2", "table_2" },
            {"table 2 first", "table_2_first"},
            {"table 2 second", "table_2_second"},
            {"table 2 syphon", "table_2_syphon"}
        };

        public Dictionary<string, string> _nominalTable = new Dictionary<string, string>()
        {
            {"Accruals and deferred income", "accruals_and_deferred_income" },
            {"Administrative expenses", "administrative_expenses"},
            {"Aggregated other reserves", "aggregated_other_reserves"},
            {"Aggregated other reserves - Brought forward", "aggregated_other_reserves_brought_forward" },
            {"Aggregated other reserves - Brought forward restated",  "aggregated_other_reserves_brought_forward_restated"},
            {"Aggregated other reserves - Cancellation of subscribed capital", "aggregated_other_reserves_cancellation_of_subscribed_capital"},
            {"Aggregated other reserves - Cancellation of treasury shares", "aggregated_other_reserves_cancellation_of_treasury_shares"},
            {"Aggregated other reserves - Carried forward", "aggregated_other_reserves_carried_forward"},
            {"Aggregated other reserves - Conversion of debt to equity", "aggregated_other_reserves_conversion_of_debt_to_equity"},
            {"Aggregated other reserves - Dividends paid and payable", "aggregated_other_reserves_dividends_paid"},
            {"Aggregated other reserves - Effects of changes in accounting policies", "aggregated_other_reserves_effects_of_changes_in_accounting_policies"},
            {"Aggregated other reserves - Equity settled share-based payments", "aggregated_other_reserves_equity_settled_share-based_payments"},
            {"Aggregated other reserves - Exercise of options, rights and warrants", "aggregated_other_reserves_exercise_of_options_rights_and_warrants"},
            {"Aggregated other reserves - Expired options, rights and warrants", "aggregated_other_reserves_expired_options_rights_and_warrants"}
        };

        public void Test_method_1(string name)
        {
        }

        public void Test_method_2(int num)
        {

        }
    }
}
