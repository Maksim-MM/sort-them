namespace SortThem
{
    public interface ISaveStorage
    {
        bool TryLoad(out byte[] data);
        void Save(byte[] data);
        void Clear();
    }
}
