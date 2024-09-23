namespace Ordering.Domain.ValueObjects
{
    public record Payment
    {
        public string? CardName { get; } = default!;
        public string CardNumber { get; } = default!;
        public string Expiration { get; } = default!;
        public string CVV { get; } = default!;
        public int PaymentMethod { get; } = default!;

        protected Payment()
        {

        }

        private Payment(string cardName, string cardNumber, string expiration, string cvv, int paymentMethod)
        {
            CardName = cardName;
            CardNumber = cardNumber;
            Expiration = expiration;
            CVV = cvv;
            PaymentMethod = paymentMethod;
        }

        public static Payment Of(string cardName, string cardNumber, string expiration, string cvv, int paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(cardName))
            {
                throw new ArgumentException(cardName);
            }
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                throw new ArgumentException(cardNumber);
            }
            if (string.IsNullOrWhiteSpace(cvv))
            {
                throw new ArgumentException(cvv);
            }
            if (cvv.Length > 3)
            {
                throw new ArgumentOutOfRangeException(cvv);
            }
            
            return new Payment(cardName, cardNumber, expiration, cvv, paymentMethod);
        }

    }
}
