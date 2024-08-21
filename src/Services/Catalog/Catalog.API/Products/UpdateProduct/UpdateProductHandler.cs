namespace Catalog.API.Products.UpdateProduct
{
    public record UpdateProductResult(bool IsSuccess);
    public record UpdateProductCommand(Guid Id, string Name, List<string> Category, string Description, string ImageFile, decimal Price) : ICommand<UpdateProductResult>;
    internal class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, UpdateProductResult>
    {
        private readonly IMapper _mapper;
        private readonly IDocumentSession _session;

        public UpdateProductCommandHandler(IMapper mapper, IDocumentSession session)
        {
            _mapper = mapper;
            _session = session;
        }

        public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
        {
            var product = await _session.LoadAsync<Product>(command.Id, cancellationToken);
            if (product is null)
            {
                throw new ProductNotFoundException(command.Id);
            }

            _mapper.Map(command, product);

            _session.Update(product);
            await _session.SaveChangesAsync(cancellationToken);

            return new UpdateProductResult(true);
        }
    }
}
