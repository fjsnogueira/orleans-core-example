﻿using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Transactions.Abstractions;
using AccountTransfer.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Text;
using Newtonsoft.Json;

[assembly: GenerateSerializer(typeof(AccountTransfer.Grains.Balance))]

namespace AccountTransfer.Grains
{
    [Serializable]
    public class Balance
    {
        public decimal Value { get; set; } = 1000;
    }

    public class AccountGrain : Grain, IAccountGrain
    {
        private readonly ITransactionalState<Balance> balance;

        private readonly IServiceBusClient serviceBusClient;

        public AccountGrain(
            IServiceBusClient serviceBusClient,
            [TransactionalState("balance")] ITransactionalState<Balance> balance)
        {
            this.serviceBusClient = serviceBusClient;
            this.balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        async Task IAccountGrain.Deposit(decimal amount)
        {
            try
            {
                this.balance.State.Value += amount;
                this.balance.Save();

                await NotifyBalanceUpdate();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        async Task IAccountGrain.Withdraw(decimal amount)
        {
            this.balance.State.Value -= amount;
            this.balance.Save();

            await NotifyBalanceUpdate();
        }

        Task<decimal> IAccountGrain.GetBalance()
        {
            return Task.FromResult(this.balance.State.Value);
        }

        private async Task NotifyBalanceUpdate()
        {
            var balanceUpdate = new BalanceUpdateMessage
            {
                AccountNumber = (int)this.GetPrimaryKeyLong(),
                Balance = this.balance.State.Value
            };

            var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(balanceUpdate)));
            await serviceBusClient.SendMessageAsync(message);
        }
    }
}
