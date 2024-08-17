using Catalog.API.Models;
using System.Windows.Input;

namespace Catalog.API.Products.CreateProduct
{
    public record CreateProductCommand(string Name, List<string> Category, string Description, string ImageFile, decimal Price) : ICommand<Guid>;
    internal class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
    {
        private readonly IMapper _mapper;
        private readonly IDocumentSession _sesstion;
        public CreateProductCommandHandler(IMapper mapper, IDocumentSession session)
        {
            _mapper = mapper;
            _sesstion = session;
        }
        public async Task<Guid> Handle(CreateProductCommand command, CancellationToken cancellationToken)
        {
            if (command == null)
                throw new Exception("Yêu cầu không hợp lệ !");

            var product = _mapper.Map<Product>(command);

            _sesstion.Store(product);
            await _sesstion.SaveChangesAsync(cancellationToken);
            return product.Id;
        }
    }
}
