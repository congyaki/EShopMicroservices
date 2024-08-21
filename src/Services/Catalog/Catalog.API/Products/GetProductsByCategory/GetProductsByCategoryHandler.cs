using Marten.Linq.QueryHandlers;

namespace Catalog.API.Products.GetProductsByCategory
{
    public record GetProductsByCategoryResult(IEnumerable<Product> Producs);

    public record GetProductsByCategoryQuery(string Category) : IQuery<GetProductsByCategoryResult>;
    public class GetProductsByCategoryQueryHandler : IQueryHandler<GetProductsByCategoryQuery, GetProductsByCategoryResult>
    {
        private readonly IMapper _mapper;
        private readonly IDocumentSession _session;

        public GetProductsByCategoryQueryHandler(IMapper mapper, IDocumentSession session)
        {
            _mapper = mapper;
            _session = session;
        }

        public async Task<GetProductsByCategoryResult> Handle(GetProductsByCategoryQuery query, CancellationToken cancellationToken)
        {
            var products = await _session.Query<Product>().Where(e => e.Category.Contains(query.Category)).ToListAsync(cancellationToken);

            return new GetProductsByCategoryResult(products);
        }
    }
}
