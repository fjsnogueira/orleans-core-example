﻿namespace MichalBialecki.com.OrleansCore.AccountTransfer.Grains
{
    public class BalanceUpdateMessage
    {
        public int AccountNumber { get; set; }

        public decimal Balance { get; set; }
    }
}
