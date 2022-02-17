using BankingMessages;
using System;
using System.Collections.Generic;

namespace AccountDomain
{
    public class AccountEntity
    {
        //Private State 
        // Guidlines: 
        // only include items required for validating public behavoirs and raising events
        // do not include "passthrough" data
        // do not allow public access
        // only modified or set via Apply([event]) methods
        // never modified or set in the constructor or other public methods
        private Guid _id;
        private decimal _balance;

        //Public Behavoirs
        // All external input and/or requests for change are handled via these public methods and constructors
        // All inputs should be fully validated
        // All invatiants (aka business rules) should be enforced
        // After successful validation methods will Raise 0-N events to achieve the desired state change (Idempotent success may not raise any events)
        // May throw exceptions  
        public AccountEntity(Guid id)
        {
            if (id == Guid.Empty) { throw new ArgumentException("Empty ID is not allowed!"); }
            Raise(new AccountEvents.Created(id));
        }

        public void DepositCash(int amount)
        {
            if (amount <= 0) { throw new ArgumentOutOfRangeException("Can't deposit 0 or negative money"); }
            Raise(new AccountEvents.CashDeposited(_id, amount));
        }
        public void WithdrawCash(int amount)
        {
            if (amount <= 0) { throw new ArgumentOutOfRangeException("Can't withdraw 0 or negative money"); }
            if (_balance - amount < 0) { throw new ArgumentException("Overdraft!!!"); }
            Raise(new AccountEvents.CashWithdrawn(_id, amount));
        }

        //Apply methods
        // These "Apply" events to move the internal state forward
        // This is the only way to change internal state
        // Not all Events need to be handled
        // Should not throw exceptions, and should ignore new or outdated propeties 
        //   this supports additive schema versioning, much like explicitly specifing columns supports extending tables in relational models
        private void Apply(AccountEvents.Created @event)
        {
            _id = @event.AccountId;
        }
        private void Apply(AccountEvents.CashDeposited @event)
        {
            _balance += @event.Amount;
        }
        private void Apply(AccountEvents.CashWithdrawn @event)
        {
            _balance -= @event.Amount;
        }

        //Internal event tracking and hydration
        // we need to achieve four things here 
        //   Hydrate from persisted Events
        //   Track new events we've raised
        //   Provide access to Tracked events so they can be persisted
        //   Provide a public Apply method to allow refreshing 

        //Hydration constructor
        public AccountEntity(IEnumerable<object> history){
            foreach (var @event in history)
            {
                try
                {
                    Apply(@event as dynamic); //dotnet magic to cast to type
                }
                catch
                {
                    //ignore, if we don't have an apply method just drop it,
                    //this supports versioning by not failing on old or unused events
                }
            }
        }

        //new events
        private List<object> _newEvents = new List<object>(); 
        
        //Raise adds a new event and Applies it to current state
        private void Raise(object @event)
        {
            _newEvents.Add(@event);
            try
            {
                Apply(@event as dynamic); //dotnet magic to cast to type
            }
            catch 
            {
               //ignore, if we don't have an apply method just drop it,
               //this supports versioning by not failing on old or unused events
            }           
        }

        //Get the events for storage and clear the list
        public List<object> TakeEvents()
        {
            var @events = new List<object>(_newEvents);
            _newEvents.Clear();
            return @events;
        }

    }
}
