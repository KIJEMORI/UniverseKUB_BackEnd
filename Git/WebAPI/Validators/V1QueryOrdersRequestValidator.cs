using FluentValidation;
using Models.Dto.V1.Requests;
using System.Data;

namespace WebAPI.Validators
{
    public class V1QueryOrdersRequestValidator : AbstractValidator<V1QueryOrdersRequest>
    {
        public V1QueryOrdersRequestValidator()
        {
           

            RuleFor(x => x.Ids)
                .NotEmpty().When(x => x.CustomerIds == null)
                .WithMessage("Ids should be not null if CustomerIds is null");

            RuleFor(x => x.CustomerIds)
                .NotEmpty().When(x => x.Ids == null)
                .WithMessage("CustomerIds should be not null if Ids is null");
            

            RuleForEach(x => x.Ids)
                .GreaterThan(0)
                .WithMessage("Ids should be greater than 0");

            RuleForEach(x => x.CustomerIds)
                .GreaterThan(0)
                .WithMessage("CustomerIds should be greater than 0");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page should be greater than 0");
            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("PageSize should be greater than 0");
        }
        


    }
}
