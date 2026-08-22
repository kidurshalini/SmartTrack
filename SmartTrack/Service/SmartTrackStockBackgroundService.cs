using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;

namespace SmartTrack.Services
{
    public class SmartTrackStockBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory
            _scopeFactory;

        public SmartTrackStockBackgroundService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(30),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAllStockAsync(
                        stoppingToken);
                }
                catch
                {
                    // Keep service alive.
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromHours(1),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task
            ProcessAllStockAsync(
                CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<
                        ApplicationDbContext>();

            var stockService =
                scope.ServiceProvider
                    .GetRequiredService<
                        SmartTrackStockService>();

            var households =
                await context
                    .UserHouseHoldDetails
                    .Select(x => new
                    {
                        x.UserId,
                        x.HouseHoldId
                    })
                    .Distinct()
                    .ToListAsync(
                        cancellationToken);

            foreach (var household in households)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var products =
                    await (
                        from item
                        in context.ReceiptItems

                        join receipt
                        in context.Receipts
                        on item.ReceiptId
                            equals receipt.ReceiptId

                        join householdUser
                        in context.UserHouseHoldDetails
                        on receipt.CreatedBy
                            equals householdUser.UserId

                        where
                            householdUser.HouseHoldId ==
                            household.HouseHoldId

                        where
                            item.ItemName != null

                        select item.ItemName
                    )
                    .Distinct()
                    .ToListAsync(
                        cancellationToken);

                foreach (var product in products)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(product))
                    {
                        continue;
                    }

                    var history =
                        await (
                            from item
                            in context.ReceiptItems

                            join receipt
                            in context.Receipts
                            on item.ReceiptId
                                equals receipt.ReceiptId

                            join householdUser
                            in context.UserHouseHoldDetails
                            on receipt.CreatedBy
                                equals householdUser.UserId

                            where
                                householdUser.HouseHoldId ==
                                household.HouseHoldId

                            where
                                item.ItemName != null

                            where
                                item.ItemName.ToLower() ==
                                product.ToLower()

                            orderby
                                receipt.PurchaseDate

                            select new SmartTrackPurchaseHistoryDto
                            {
                                ProductName =
                                    item.ItemName,

                                Quantity =
                                    item.Quantity,

                                PurchaseDate =
                                    receipt.PurchaseDate
                                        .ToString(
                                            "yyyy-MM-ddTHH:mm:ss"),

                                UnitPrice =
                                    (double)item.UnitPrice,

                                TotalPrice =
                                    (double)item.TotalPrice,

                                Category =
                                    "Unknown",

                                UserId =
                                    receipt.CreatedBy,

                                ReceiptId =
                                    receipt.ReceiptId
                            }
                        )
                        .ToListAsync(
                            cancellationToken);

                    if (history.Count == 0)
                    {
                        continue;
                    }

                    await stockService
                        .ProcessStockAsync(
                            household.UserId,
                            household.HouseHoldId,
                            product,
                            history);
                }
            }
        }
    }
}