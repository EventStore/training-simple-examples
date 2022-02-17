using BankingMessages;
using System;
using Xunit;

namespace AccountDomain.Tests
{
    public class AccountTests
    {

        [Fact]
        public void new_accounts_must_have_valid_id()
        {
            //given 
            var id = Guid.Empty;

            //validation check
            Assert.Throws<ArgumentException>(() => new AccountEntity(id));
        }
        [Fact]
        public void can_create_accounts()
        {
            //given
            var id = Guid.NewGuid();

            //when
            var account = new AccountEntity(id);


            //then
            var @events = account.TakeEvents();

            //n.b.: this also asserts that the exact number of expected elements is in the colection
            //i.e. no "extra" events are seen
            Assert.Collection(
                          events,
                          e =>
                          {
                              var created = Assert.IsType<AccountEvents.Created>(e);
                              Assert.Equal(id, created.AccountId);
                          });
        }

        [Fact]
        public void can_deposit_cash()
        {
            //given
            var accountId = Guid.NewGuid();
            var account = new AccountEntity(new object[] { new AccountEvents.Created(accountId) });
            var amount1 = 10;
            var amount2 = 15;

            //validation checks
            Assert.Throws<ArgumentOutOfRangeException>(() => account.DepositCash(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => account.DepositCash(-1));

            //when
            account.DepositCash(amount1);
            account.DepositCash(amount2);

            //then
            var @events = account.TakeEvents();

            //N.B. the Hydrated events are not present in the entity only the new ones we've raised 
            Assert.Collection(
                          events,
                          e =>
                          {
                              var deposit = Assert.IsType<AccountEvents.CashDeposited>(e);
                              Assert.Equal(amount1, deposit.Amount);
                              Assert.Equal(accountId, deposit.AccountId);
                          },
                           e =>
                           {
                               var deposit = Assert.IsType<AccountEvents.CashDeposited>(e);
                               Assert.Equal(amount2, deposit.Amount);
                               Assert.Equal(accountId, deposit.AccountId);
                           });
            //N.B.: there is no test for the "balance" of the account here
            // the balance has no part to play in accepting depositis, and the entity is only for enforcing rules and generating events not query
        }

        [Fact]
        public void can_withdraw_cash()
        {
            //Given
            var created = new AccountEvents.Created(Guid.NewGuid());
            var deposit10 = new AccountEvents.CashDeposited(created.AccountId, 10);

            var account = new AccountEntity(new object[] { created, deposit10, deposit10 });

            //validation checks
            Assert.Throws<ArgumentOutOfRangeException>(() => account.WithdrawCash(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => account.WithdrawCash(-1));

            //when

            var amount1 = 10;
            var amount2 = 5;
            account.WithdrawCash(amount1);
            account.WithdrawCash(amount2);

            //then
            var events = account.TakeEvents();

            Assert.Collection(
                          events,
                          e =>
                          {
                              var deposit = Assert.IsType<AccountEvents.CashWithdrawn>(e);
                              Assert.Equal(amount1, deposit.Amount);
                              Assert.Equal(created.AccountId, deposit.AccountId);
                          },
                           e =>
                           {
                               var deposit = Assert.IsType<AccountEvents.CashWithdrawn>(e);
                               Assert.Equal(amount2, deposit.Amount);
                               Assert.Equal(created.AccountId, deposit.AccountId);
                           });

        }
        //these tests check the business invariant preventing overdraft
        //this implictly tests the balance calculation while taking no dependency on it's implemenation
        //Activity: try changing the type of the _balance field in the account entity from int to decimal and re-run the tests.
        //Questions:
        // What code had to change?
        // How might using a value object for the amount make this event better?
        [Fact]
        public void cannot_withdraw_from_empty_account()
        {
            //Given
            var account = new AccountEntity(new object[] { new AccountEvents.Created(Guid.NewGuid()) });

            //When
            Assert.Throws<ArgumentException>(() => account.WithdrawCash(1));

            //Then
            var @events = account.TakeEvents();
            Assert.Empty(events);
        }

        [Fact]
        public void cannot_overdraw()
        {
            //Given
            var created = new AccountEvents.Created(Guid.NewGuid());
            var deposit10 = new AccountEvents.CashDeposited(created.AccountId, 10);

            var account = new AccountEntity(new object[] { created, deposit10, deposit10 });

            //When
            Assert.Throws<ArgumentException>(() => account.WithdrawCash(21));

            //Then
            var @events = account.TakeEvents();
            Assert.Empty(events);
        }

    }
}
