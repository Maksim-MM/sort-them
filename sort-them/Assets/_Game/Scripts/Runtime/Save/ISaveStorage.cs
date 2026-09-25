namespace SortThem
{
    public interface ISaveStorage
    {
        bool TryLoad(out byte[] data);
        void Stage(byte[] data);
        void Commit();
        void Clear();
    }
}
