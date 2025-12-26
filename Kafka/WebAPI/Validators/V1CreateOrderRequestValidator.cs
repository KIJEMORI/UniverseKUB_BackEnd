using FluentValidation;
using Models.Dto.V1.Requests;

namespace WebAPI.Validators
{
    public class V1CreateOrderRequestValidator : AbstractValidator<V1CreateOrderRequest>
    {
        public V1CreateOrderRequestValidator()
        {
            // правило того, что заказы не могут быть null или пустыми
            RuleFor(x => x.Orders)
                .NotEmpty();

            // правило для каждого заказа в массиве вызови OrderValidator
            RuleForEach(x => x.Orders)
                .SetValidator(new OrderValidator())
                .When(x => x.Orders is not null);
        }

        class OrderValidator : AbstractValidator<V1CreateOrderRequest.Order>
        {
            public OrderValidator()
            {
                // CustomerId в каждом заказе не должен быть меньше или равен 0
                RuleFor(x => x.CustomerId)
                    .GreaterThan(0)
                    .WithMessage("CustomerId should be greater than 0");

                // Не пустая строка в DeliveryAddress
                RuleFor(x => x.DeliveryAddress)
                    .NotEmpty()
                    .WithMessage("DeliveryAddress should be not null");

                // TotalPriceCents в каждом заказе не должен быть меньше или равен 0
                RuleFor(x => x.TotalPriceCents)
                    .GreaterThan(0)
                    .WithMessage("TotalPriceCents should be greater than 0");

                RuleFor(x => x.TotalPriceCurrency)
                    .NotEmpty()
                    .WithMessage("TotalPriceCurrency should be not null");

                // OrderItems в каждом заказе не должен быть null или пустым
                RuleFor(x => x.OrderItems)
                    .NotEmpty()
                    .WithMessage("OrderItems should be not null");

                // Для каждого OrderItem вызови OrderItemValidator
                RuleForEach(x => x.OrderItems)
                    .SetValidator(new OrderItemValidator())
                    .When(x => x.OrderItems is not null);

                // TotalPriceCents в каждом заказе должен быть равен сумме всех OrderItems.PriceCents * OrderItems.Quantity
                RuleFor(x => x)
                    .Must(x => x.TotalPriceCents == x.OrderItems.Sum(y => y.PriceCents * y.Quantity))
                    .When(x => x.OrderItems is not null)
                    .WithMessage("TotalPriceCents should be equal to sum of all OrderItems.PriceCents * OrderItems.Quantity");

                // Все PriceCurrency в OrderItems должны быть одинаковы
                RuleFor(x => x)
                    .Must(x => x.OrderItems.Select(r => r.PriceCurrency).Distinct().Count() == 1)
                    .When(x => x.OrderItems is not null)
                    .WithMessage("All OrderItems.PriceCurrency should be the same");

                // OrderItems.PriceCurrency хотя бы в первом OrderItem должен быть равен TotalPriceCurrency
                // Если во втором не равен, то предыдущее правило выкинуло ошибку
                RuleFor(x => x)
                    .Must(x => x.OrderItems.Select(r => r.PriceCurrency).First() == x.TotalPriceCurrency)
                    .When(x => x.OrderItems is not null)
                    .WithMessage("OrderItems.PriceCurrency should be the same as TotalPriceCurrency");
            }
        }

        // тут все просто
       class OrderItemValidator : AbstractValidator<V1CreateOrderRequest.OrderItem>
        {
            public OrderItemValidator()
            {
                RuleFor(x => x.ProductId)
                    .GreaterThan(0)
                    .WithMessage("ProductId should be greater than 0");

                RuleFor(x => x.PriceCents)
                    .GreaterThan(0)
                    .WithMessage("PriceCents should be greater than 0");

                RuleFor(x => x.PriceCurrency)
                    .NotEmpty()
                    .WithMessage("PriceCurrency should be not null");

                RuleFor(x => x.ProductTitle)
                    .NotEmpty()
                    .WithMessage("ProductTitle should be not null");

                RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity should be greater than 0");
            }
        }
    }
}
