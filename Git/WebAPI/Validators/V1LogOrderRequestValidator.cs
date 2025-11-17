using FluentValidation;
using Models.Dto.V1.Requests;

namespace WebAPI.Validators
{
    public class V1LogOrderRequestValidator : AbstractValidator<V1AuditLogOrderRequest>
    {
        public V1LogOrderRequestValidator()
        {
            // правило того, что заказы не могут быть null или пустыми
            RuleFor(x => x.Orders)
                .NotEmpty()
                .WithMessage("Orders should be not null");

            // правило для каждого заказа в массиве вызови OrderValidator
            RuleForEach(x => x.Orders)
                .SetValidator(new LogOrderValidator())
                .When(x => x.Orders is not null);
        }

        public class LogOrderValidator : AbstractValidator<V1AuditLogOrderRequest.LogOrder>
        {
            public LogOrderValidator()
            {
                RuleFor(x => x.OrderItemId)
                    .GreaterThan(0)
                    .WithMessage("OrderItemId should be greater than 0");

                RuleFor(x => x.OrderId)
                    .GreaterThan(0)
                    .WithMessage("OrderId should be greater than 0");

                RuleFor(x => x.CustomerId)
                    .GreaterThan(0)
                    .WithMessage("CustomerId should be greater than 0");

                RuleFor(x => x.OrderStatus)
                    .NotEmpty()
                    .WithMessage("OrderStatus should be not null");
              
            }
        }

        
    }
}
