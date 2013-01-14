using System.Threading.Tasks;

namespace Fabrik.SimpleBus
{
    public interface IHandleAsync<TMessage>
    {
        Task HandleAsync(TMessage message);
    }
}
