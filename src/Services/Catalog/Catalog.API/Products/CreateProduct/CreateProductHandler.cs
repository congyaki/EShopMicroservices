using Catalog.API.Models;
using System.Windows.Input;

namespace Catalog.API.Products.CreateProduct
{
    public record CreateProductCommand(string Name, List<string> Category, string Description, string ImageFile, decimal Price) : ICommand<Guid>;
    public record CreateProductResult(Guid Id);
    internal class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
    {
        private readonly IMapper _mapper;
        private readonly IDocumentSession _session;
        public CreateProductCommandHandler(IMapper mapper, IDocumentSession session)
        {
            _mapper = mapper;
            _session = session;
        }
        public async Task<Guid> Handle(CreateProductCommand command, CancellationToken cancellationToken)
        {
            var product = _mapper.Map<Product>(command);

            _session.Store(product);
            await _session.SaveChangesAsync(cancellationToken);
            return product.Id;
        }
    }
}
