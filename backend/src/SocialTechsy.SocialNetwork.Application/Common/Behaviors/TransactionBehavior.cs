using MediatR;
using SocialTechsy.SocialNetwork.Application.Interfaces;

namespace SocialTechsy.SocialNetwork.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that wraps handlers for ITransactionalRequest in a database transaction.
/// Ensures all repository operations in Vote and AcceptAnswer handlers are atomic.
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
            return await next();

        return await _unitOfWork.ExecuteInTransactionAsync(async ct => await next(), cancellationToken);
    }
}
