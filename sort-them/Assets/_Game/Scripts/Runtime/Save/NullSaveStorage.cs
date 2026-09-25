namespace SortThem
{
    public class NullSaveStorage : ISaveStorage
    {
        public bool TryLoad(out byte[] data)
        {
            data = null;
            return false;
        }

        public void Stage(byte[] data) { }
        public void Commit() { }
        public void Clear() { }
    }
}
