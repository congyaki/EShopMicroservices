
namespace Catalog.API.Products.DeleteProducts
{

    public record DeleteProductsCommand(IEnumerable<Guid> Ids) : ICommand<DeleteProductResult>;
    public record DeleteProductResult(bool IsSuccess);
    public class DeleteProductsCommandHandler : ICommandHandler<DeleteProductsCommand, DeleteProductResult>
    {
        private readonly IDocumentSession _session;

        public DeleteProductsCommandHandler(IDocumentSession session)
        {
            _session = session;
        }

        public async Task<DeleteProductResult> Handle(DeleteProductsCommand command, CancellationToken cancellationToken)
        {
            _session.DeleteWhere<Product>(e => command.Ids.Contains(e.Id));

            await _session.SaveChangesAsync(cancellationToken);

            return new DeleteProductResult(true);
        }
    }
}
