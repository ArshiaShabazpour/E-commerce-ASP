using System.Text.Json.Serialization;
namespace ECommerceApp.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RefundMethod
    {
        Original,   
        PayPal,
        Stripe,
        BankTransfer,
        Manual
    }
}