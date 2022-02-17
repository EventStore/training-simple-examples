using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingMessages
{
    public class AccountEvents
    {
        public class Created
        {
            public readonly Guid AccountId;
            public Created(Guid accountId)
            {
                AccountId = accountId;
            }
        }
        public class CashDeposited
        {
            public readonly Guid AccountId;
            public readonly int Amount;
            public CashDeposited(Guid accountId, int amount)
            {
                AccountId = accountId;
                Amount = amount;
            }
        }
        public class CashWithdrawn
        {
            public readonly Guid AccountId;
            public readonly int Amount;
            public CashWithdrawn(Guid accountId, int amount)
            {
                AccountId = accountId;
                Amount = amount;
            }
        }
    }
}
