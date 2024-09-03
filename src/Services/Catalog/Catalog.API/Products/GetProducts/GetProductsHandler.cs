using Catalog.API.Models;
using Marten.Linq.QueryHandlers;
using Marten.Pagination;

namespace Catalog.API.Products.GetProducts
{
    public record GetProductsQuery(int? PageNumber = 1, int? PageSize = 10) : IQuery<GetProductsResult>;

    public record GetProductsResult(IEnumerable<Product> Products);

    internal class GetProductsHandler : IQueryHandler<GetProductsQuery, GetProductsResult>
    {
        private readonly IDocumentSession _session;
        private readonly IMapper _mapper;
        public GetProductsHandler(IDocumentSession session, IMapper mapper)
        {
            _session = session;
            _mapper = mapper;
        }

        public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
        {
            var products = await _session.Query<Product>().ToPagedListAsync(query.PageNumber ?? 1, query.PageSize ?? 10, cancellationToken);
            var res = new GetProductsResult(products);
            return res;
        }
    }
}
