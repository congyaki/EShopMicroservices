using Catalog.API.Models;
using Marten.Linq.QueryHandlers;
using Marten.Pagination;

namespace Catalog.API.Products.GetProducts
{
    public record GetProductsQuery(int? pageNumber = 1, int? pageSize = 10) : IQuery<GetProductsResult>;

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
            var products = await _session.Query<Product>().ToPagedListAsync(query.pageNumber ?? 1, query.pageSize ?? 10, cancellationToken);
            var res = _mapper.Map<GetProductsResult>(products);
            return res;
        }
    }
}
