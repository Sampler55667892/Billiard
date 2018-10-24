namespace Assets.Scripts
{
    public interface INotifyStopping : UnityEngine.EventSystems.IEventSystemHandler
    {
        void OnNotifyStopping(string name);
    }
}
