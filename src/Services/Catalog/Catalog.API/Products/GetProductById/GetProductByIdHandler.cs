using Catalog.API.Exceptions;
using Catalog.API.Models;

namespace Catalog.API.Products.GetProductById
{
    public record GetProductByIdQuery(Guid Id) : ICommand<GetProductByIdResult>;
    public record GetProductByIdResult(Product Product);
    internal class GetProductByIdQueryHandler : ICommandHandler<GetProductByIdQuery, GetProductByIdResult>
    {
        private readonly IMapper _mapper;
        private readonly IDocumentSession _session;
        public GetProductByIdQueryHandler(IMapper mapper, IDocumentSession session)
        {
            _mapper = mapper;
            _session = session;
        }
        public async Task<GetProductByIdResult> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
        {
            var product = await _session.LoadAsync<Product>(query.Id, cancellationToken);

            if (product is null)
            {
                throw new ProductNotFoundException(query.Id);
            }

            return _mapper.Map<GetProductByIdResult>(product);
        }
    }
}
