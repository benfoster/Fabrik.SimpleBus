
namespace Fabrik.SimpleBus
{
    public interface IHandle<TMessage>
    {
        void Handle(TMessage message);
    }
}
