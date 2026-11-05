using FoodDiary.Telegram.Bot.Api.Generated.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramStarsTransportTests {
    [Fact]
    public void PreCheckoutPreservesPayerInvoiceAndIntegerStars() {
        TelegramStarsUpdateHttpRequest mapped = Assert.IsType<TelegramStarsUpdateHttpRequest>(TelegramBotWorker.MapStarsUpdate(new Update {
            Id = 10,
            PreCheckoutQuery = new PreCheckoutQuery {
                Id = "query",
                From = new User { Id = 123, FirstName = "Synthetic" },
                InvoicePayload = "invoice_owned",
                Currency = "XTR",
                TotalAmount = 500,
            },
        }));
        Assert.Multiple(() => {
            Assert.Equal("pre_checkout", mapped.Kind);
            Assert.Equal(123, mapped.TelegramUserId);
            Assert.Equal(500, mapped.TotalAmount);
            Assert.Equal("invoice_owned", mapped.InvoicePayload);
        });
    }

    [Fact]
    public void PaymentPreservesProviderExpiryAndFirstCharge() {
        DateTime paidAt = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        TelegramStarsUpdateHttpRequest mapped = Assert.IsType<TelegramStarsUpdateHttpRequest>(TelegramBotWorker.MapStarsUpdate(new Update {
            Id = 20,
            Message = new Message {
                Chat = new Chat { Id = 123, Type = ChatType.Private },
                Date = paidAt,
                SuccessfulPayment = new SuccessfulPayment {
                    InvoicePayload = "invoice_owned",
                    Currency = "XTR",
                    TotalAmount = 500,
                    TelegramPaymentChargeId = "charge",
                    ProviderPaymentChargeId = "",
                    IsRecurring = true,
                    IsFirstRecurring = true,
                    SubscriptionExpirationDate = paidAt.AddDays(30),
                },
            },
        }));
        Assert.Multiple(() => {
            Assert.Equal("payment", mapped.Kind);
            Assert.Equal("charge", mapped.PaymentChargeId);
            Assert.Equal(paidAt.AddDays(30), mapped.SubscriptionExpirationUtc);
            Assert.Equal(paidAt, mapped.PaymentOccurredAtUtc);
            Assert.True(mapped.IsRecurring);
            Assert.True(mapped.IsFirstRecurring);
        });
    }

    [Fact]
    public void CancellationAndFailedRenewalUseDedicatedSubscriptionUpdates() {
        TelegramStarsUpdateHttpRequest mapped = Assert.IsType<TelegramStarsUpdateHttpRequest>(TelegramBotWorker.MapStarsUpdate(new Update {
            Id = 30,
            Subscription = new BotSubscriptionUpdated {
                User = new User { Id = 123, FirstName = "Synthetic" },
                InvoicePayload = "invoice_owned",
                State = "canceled",
            },
        }));
        Assert.Multiple(() => {
            Assert.Equal("subscription", mapped.Kind);
            Assert.Equal(123, mapped.TelegramUserId);
            Assert.Equal("canceled", mapped.SubscriptionState);
        });
    }

    [Fact]
    public void RefundUsesPrivateChatOwnerEvenIfServiceMessageSenderIsTheBot() {
        TelegramStarsUpdateHttpRequest mapped = Assert.IsType<TelegramStarsUpdateHttpRequest>(TelegramBotWorker.MapStarsUpdate(new Update {
            Id = 40,
            Message = new Message {
                Chat = new Chat { Id = 123, Type = ChatType.Private },
                Date = DateTime.UnixEpoch,
                From = new User { Id = 999, IsBot = true, FirstName = "Bot" },
                RefundedPayment = new RefundedPayment {
                    InvoicePayload = "invoice_owned",
                    Currency = "XTR",
                    TotalAmount = 500,
                    TelegramPaymentChargeId = "charge",
                },
            },
        }));
        Assert.Equal(123, mapped.TelegramUserId);
        Assert.Equal("refund", mapped.Kind);
    }
}
