using System;

namespace SortThem
{
    public static class Messages
    {
        public static event Action<string> Shown;

        public static void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Shown?.Invoke(text);
        }
    }
}
