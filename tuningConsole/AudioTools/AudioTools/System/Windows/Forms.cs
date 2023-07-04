namespace System.Windows
{
    internal class Forms
    {
        public static object DialogResult { get; internal set; }

        internal class FileDialog
        {
            public bool Multiselect { get; set; }
            public string Filter { get; set; }
            public string Title { get; set; }
        }
    }
}